using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PanoramicData.OData.Client.Converters;
using PanoramicData.OData.Client.Exceptions;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PanoramicData.OData.Client;

/// <summary>
/// A lightweight OData client for REST API communication.
/// </summary>
public partial class ODataClient : IDisposable
{
	private readonly HttpClient _httpClient;
	private readonly ODataClientOptions _options;
	private readonly ILogger _logger;
	private readonly bool _ownsHttpClient;
	private bool _disposed;

	private readonly JsonSerializerOptions _jsonOptions = new()
	{
		PropertyNameCaseInsensitive = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		Converters =
		{
			new JsonStringEnumConverter(),
			new ODataDateTimeConverter(),
			new ODataNullableDateTimeConverter()
		}
	};

	// Add a field for action-specific JSON options after _jsonOptions
	private readonly JsonSerializerOptions _actionJsonOptions = new()
	{
		PropertyNameCaseInsensitive = true,
		// Note: Do NOT use PropertyNamingPolicy for actions - OData requires Pascal case matching the EDM model
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		Converters =
		{
			new JsonStringEnumConverter(),
			new ODataDateTimeConverter(),
			new ODataNullableDateTimeConverter()
		}
	};

	/// <summary>
	/// Creates a new OData client.
	/// </summary>
	public ODataClient(ODataClientOptions options)
	{
		_options = options ?? throw new ArgumentNullException(nameof(options));
		_logger = options.Logger ?? NullLogger.Instance;

		_logger.LogDebug("ODataClient initialized with BaseUrl: {BaseUrl}", options.BaseUrl);

		if (options.HttpClient is not null)
		{
			_httpClient = options.HttpClient;
			_ownsHttpClient = false;
			_logger.LogDebug("Using provided HttpClient with BaseAddress: {BaseAddress}", _httpClient.BaseAddress);
		}
		else
		{
			_httpClient = new HttpClient
			{
				BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/"),
				Timeout = options.Timeout
			};
			_ownsHttpClient = true;
			_logger.LogDebug("Created new HttpClient with BaseAddress: {BaseAddress}", _httpClient.BaseAddress);
		}

		_jsonOptions = options.JsonSerializerOptions ?? _jsonOptions;
	}

	#region Private Helper Methods

	private HttpRequestMessage CreateRequest(
		HttpMethod method,
		string url,
		IReadOnlyDictionary<string, string>? headers = null)
	{
		var request = new HttpRequestMessage(method, url);

		_logger.LogDebug("CreateRequest - {Method} {Url}", method, url);

		// Apply custom headers from options
		_options.ConfigureRequest?.Invoke(request);

		// Apply query-specific headers
		if (headers is not null)
		{
			foreach (var header in headers)
			{
				request.Headers.TryAddWithoutValidation(header.Key, header.Value);
				_logger.LogDebug("CreateRequest - Adding header: {Key}={Value}", header.Key, header.Value);
			}
		}

		return request;
	}

	private async Task<HttpResponseMessage> SendWithRetryAsync(
		HttpRequestMessage request,
		CancellationToken cancellationToken)
	{
		var retryCount = 0;
		HttpResponseMessage? lastResponse = null;

		_logger.LogDebug("SendWithRetryAsync - Starting request to {Url}", request.RequestUri);

		while (retryCount <= _options.RetryCount)
		{
			var result = await TrySendRequestAsync(request, retryCount, cancellationToken).ConfigureAwait(false);
			if (result.ShouldReturn)
			{
				return result.Response!;
			}

			lastResponse = result.Response;
			retryCount++;

			if (retryCount <= _options.RetryCount)
			{
				await Task.Delay(_options.RetryDelay, cancellationToken).ConfigureAwait(false);
			}
		}

		return lastResponse ?? throw new ODataClientException("Request failed after all retries");
	}

	private async Task<(bool ShouldReturn, HttpResponseMessage? Response)> TrySendRequestAsync(
		HttpRequestMessage request,
		int retryCount,
		CancellationToken cancellationToken)
	{
		try
		{
			var requestToSend = retryCount == 0 ? request : await CloneRequestAsync(request).ConfigureAwait(false);

			_logger.LogDebug("SendWithRetryAsync - Sending {Method} request to {Url} (attempt {Attempt})",
				requestToSend.Method, requestToSend.RequestUri, retryCount + 1);

			var response = await _httpClient.SendAsync(requestToSend, cancellationToken).ConfigureAwait(false);

			_logger.LogDebug("SendWithRetryAsync - Received {StatusCode} from {Url}",
				response.StatusCode, request.RequestUri);

			if (response.IsSuccessStatusCode || (int)response.StatusCode < 500)
			{
				return (true, response);
			}

			LogRetryWarning(request, response.StatusCode, retryCount);
			return (false, response);
		}
		catch (HttpRequestException ex) when (retryCount < _options.RetryCount)
		{
			LogRetryException(request, ex, retryCount, "failed with exception");
		}
		catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested && retryCount < _options.RetryCount)
		{
			LogRetryException(request, ex, retryCount, "timed out");
		}

		return (false, null);
	}

	private void LogRetryWarning(HttpRequestMessage request, HttpStatusCode statusCode, int retryCount) =>
		_logger.LogWarning(
			"Request to {Url} failed with {StatusCode}, attempt {Attempt}/{MaxRetries}",
			request.RequestUri,
			statusCode,
			retryCount + 1,
			_options.RetryCount + 1);

	private void LogRetryException(HttpRequestMessage request, Exception ex, int retryCount, string reason) =>
		_logger.LogWarning(
			ex,
			"Request to {Url} {Reason}, attempt {Attempt}/{MaxRetries}",
			request.RequestUri,
			reason,
			retryCount + 1,
			_options.RetryCount + 1);

	private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage request)
	{
		var clone = new HttpRequestMessage(request.Method, request.RequestUri);

		foreach (var header in request.Headers)
		{
			clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
		}

		if (request.Content is not null)
		{
			var content = await request.Content.ReadAsStringAsync().ConfigureAwait(false);
			clone.Content = new StringContent(content, Encoding.UTF8, request.Content.Headers.ContentType?.MediaType ?? "application/json");
		}

		return clone;
	}

	private Task EnsureSuccessAsync(
		HttpResponseMessage response,
		string requestUrl,
		CancellationToken cancellationToken)
		=> EnsureSuccessAsync(response, requestUrl, requestETag: null, cancellationToken);

	private async Task EnsureSuccessAsync(
		HttpResponseMessage response,
		string requestUrl,
		string? requestETag,
		CancellationToken cancellationToken)
	{
		var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
		var statusCode = (int)response.StatusCode;

		// Check for error status codes
		if (!response.IsSuccessStatusCode)
		{
			_logger.LogError("Request to {Url} failed with status {StatusCode}: {ResponseBody}",
				requestUrl, response.StatusCode, responseBody);

			// Handle 412 Precondition Failed (concurrency conflict)
			if (response.StatusCode == HttpStatusCode.PreconditionFailed)
			{
				var currentETag = response.Headers.ETag?.ToString();
				throw new ODataConcurrencyException(
					$"Concurrency conflict: The entity was modified since it was last retrieved. Request URL: {requestUrl}",
					requestUrl,
					requestETag,
					currentETag,
					responseBody);
			}

			throw response.StatusCode switch
			{
				HttpStatusCode.NotFound => new ODataNotFoundException($"Resource not found: {requestUrl}", requestUrl),
				HttpStatusCode.Unauthorized => new ODataUnauthorizedException("Unauthorized", responseBody, requestUrl),
				HttpStatusCode.Forbidden => new ODataForbiddenException("Forbidden", responseBody, requestUrl),
				_ => new ODataClientException($"Request failed with status {response.StatusCode}", statusCode, responseBody, requestUrl)
			};
		}

		// Check for HTML content returned with success status (misconfigured server/proxy)
		// Skip this check for $metadata endpoint which returns XML, and for any XML content types
		var contentType = response.Content.Headers.ContentType?.MediaType ?? "unknown";
		var isXmlContentType = contentType.Contains("xml", StringComparison.OrdinalIgnoreCase);
		var isMetadataRequest = requestUrl.Contains("$metadata", StringComparison.OrdinalIgnoreCase);

		if (responseBody.Length > 0 &&
			responseBody.TrimStart().StartsWith('<') &&
			!isXmlContentType &&
			!isMetadataRequest)
		{
			var preview = responseBody.Length > 500 ? responseBody[..500] + "..." : responseBody;

			_logger.LogError(
				"Request to {Url} returned HTML content instead of JSON (Content-Type: {ContentType}). Response preview: {Preview}",
				requestUrl, contentType, preview);

			throw new ODataClientException(
				$"Expected JSON response but received HTML. The server may have returned an error page, login redirect, or the endpoint doesn't exist. Content-Type: {contentType}, URL: {requestUrl}",
				statusCode,
				responseBody,
				requestUrl);
		}
	}

	private static string FormatKey<TKey>(TKey key) => key switch
	{
		int i => i.ToString(CultureInfo.InvariantCulture),
		long l => l.ToString(CultureInfo.InvariantCulture),
		Guid g => g.ToString(),
		string s => $"'{s.Replace("'", "''")}'",
		_ => key?.ToString() ?? throw new ArgumentException("Invalid key value")
	};

	#endregion

	#region IDisposable

	/// <summary>
	/// Releases the unmanaged resources used by the <see cref="ODataClient"/> and optionally releases the managed resources.
	/// </summary>
	/// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
	protected virtual void Dispose(bool disposing)
	{
		if (!_disposed)
		{
			if (disposing && _ownsHttpClient)
			{
				_httpClient.Dispose();
			}

			_disposed = true;
		}
	}

	/// <inheritdoc />
	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	#endregion
}
