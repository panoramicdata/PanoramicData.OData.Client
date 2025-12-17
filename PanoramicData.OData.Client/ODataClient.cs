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

		LoggerMessages.ClientInitialized(_logger, options.BaseUrl);

		if (options.HttpClient is not null)
		{
			_httpClient = options.HttpClient;
			_ownsHttpClient = false;
			LoggerMessages.UsingProvidedHttpClient(_logger, _httpClient.BaseAddress);
		}
		else
		{
			_httpClient = new HttpClient
			{
				BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/"),
				Timeout = options.Timeout
			};
			_ownsHttpClient = true;
			LoggerMessages.CreatedHttpClient(_logger, _httpClient.BaseAddress);
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

		LoggerMessages.CreateRequest(_logger, method, url);

		// Apply custom headers from options
		_options.ConfigureRequest?.Invoke(request);

		// Apply query-specific headers
		if (headers is not null)
		{
			foreach (var header in headers)
			{
				request.Headers.TryAddWithoutValidation(header.Key, header.Value);
				LoggerMessages.AddingHeader(_logger, header.Key, header.Value);
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

		LoggerMessages.StartingRequest(_logger, request.RequestUri);

		while (retryCount <= _options.RetryCount)
		{
			var (ShouldReturn, Response) = await TrySendRequestAsync(request, retryCount, cancellationToken).ConfigureAwait(false);
			if (ShouldReturn)
			{
				return Response!;
			}

			lastResponse = Response;
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

			LoggerMessages.SendingRequest(_logger, requestToSend.Method, requestToSend.RequestUri, retryCount + 1);

			// Log full request details at Trace level
			await LogRequestTraceAsync(requestToSend, cancellationToken).ConfigureAwait(false);

			var response = await _httpClient.SendAsync(requestToSend, cancellationToken).ConfigureAwait(false);

			LoggerMessages.ReceivedResponse(_logger, response.StatusCode, request.RequestUri);

			// Log full response details at Trace level
			await LogResponseTraceAsync(response, cancellationToken).ConfigureAwait(false);

			if (response.IsSuccessStatusCode || (int)response.StatusCode < 500)
			{
				return (true, response);
			}

			LoggerMessages.RetryWarning(_logger, request.RequestUri, response.StatusCode, retryCount + 1, _options.RetryCount + 1);
			return (false, response);
		}
		catch (HttpRequestException ex) when (retryCount < _options.RetryCount)
		{
			LoggerMessages.RetryException(_logger, ex, request.RequestUri, "failed with exception", retryCount + 1, _options.RetryCount + 1);
		}
		catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested && retryCount < _options.RetryCount)
		{
			LoggerMessages.RetryException(_logger, ex, request.RequestUri, "timed out", retryCount + 1, _options.RetryCount + 1);
		}

		return (false, null);
	}

	private async Task LogRequestTraceAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		if (!_logger.IsEnabled(LogLevel.Trace))
		{
			return;
		}

		var sb = new StringBuilder();
		sb.AppendLine("=== HTTP Request ===");
		sb.AppendLine(CultureInfo.InvariantCulture, $"{request.Method} {request.RequestUri}");

		sb.AppendLine("--- Request Headers ---");
		foreach (var header in request.Headers)
		{
			sb.AppendLine(CultureInfo.InvariantCulture, $"{header.Key}: {string.Join(", ", header.Value)}");
		}

		if (request.Content is not null)
		{
			foreach (var header in request.Content.Headers)
			{
				sb.AppendLine(CultureInfo.InvariantCulture, $"{header.Key}: {string.Join(", ", header.Value)}");
			}

			sb.AppendLine("--- Request Body ---");
			var body = await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
			sb.AppendLine(body);
		}

		LoggerMessages.LogRequestTrace(_logger, sb.ToString());
	}

	private async Task LogResponseTraceAsync(HttpResponseMessage response, CancellationToken cancellationToken)
	{
		if (!_logger.IsEnabled(LogLevel.Trace))
		{
			return;
		}

		var sb = new StringBuilder();
		sb.AppendLine("=== HTTP Response ===");
		sb.AppendLine(CultureInfo.InvariantCulture, $"Status: {(int)response.StatusCode} {response.ReasonPhrase}");

		sb.AppendLine("--- Response Headers ---");
		foreach (var header in response.Headers)
		{
			sb.AppendLine(CultureInfo.InvariantCulture, $"{header.Key}: {string.Join(", ", header.Value)}");
		}

		foreach (var header in response.Content.Headers)
		{
			sb.AppendLine(CultureInfo.InvariantCulture, $"{header.Key}: {string.Join(", ", header.Value)}");
		}

		sb.AppendLine("--- Response Body ---");
		var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
		sb.AppendLine(body);

		LoggerMessages.LogResponseTrace(_logger, sb.ToString());
	}

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

		if (!response.IsSuccessStatusCode)
		{
			ThrowForErrorStatusCode(response, requestUrl, requestETag, statusCode, responseBody);
		}

		ThrowForUnexpectedHtmlResponse(response, requestUrl, statusCode, responseBody);
	}

	private void ThrowForErrorStatusCode(
		HttpResponseMessage response,
		string requestUrl,
		string? requestETag,
		int statusCode,
		string responseBody)
	{
		LoggerMessages.RequestFailed(_logger, requestUrl, response.StatusCode, responseBody);

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

	private void ThrowForUnexpectedHtmlResponse(
		HttpResponseMessage response,
		string requestUrl,
		int statusCode,
		string responseBody)
	{
		var contentType = response.Content.Headers.ContentType?.MediaType ?? "unknown";
		var isXmlContentType = contentType.Contains("xml", StringComparison.OrdinalIgnoreCase);
		var isMetadataRequest = requestUrl.Contains("$metadata", StringComparison.OrdinalIgnoreCase);

		if (responseBody.Length > 0 &&
			responseBody.TrimStart().StartsWith('<') &&
			!isXmlContentType &&
			!isMetadataRequest)
		{
			var preview = responseBody.Length > 500 ? responseBody[..500] + "..." : responseBody;

			LoggerMessages.UnexpectedHtmlResponse(_logger, requestUrl, contentType, preview);

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
