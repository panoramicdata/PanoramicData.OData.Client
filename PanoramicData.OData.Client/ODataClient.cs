using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PanoramicData.OData.Client.Converters;
using PanoramicData.OData.Client.Exceptions;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PanoramicData.OData.Client;

/// <summary>
/// A lightweight OData client for REST API communication.
/// </summary>
public class ODataClient : IDisposable
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

	/// <summary>
	/// Creates a query builder for the specified entity set.
	/// </summary>
	public ODataQueryBuilder<T> For<T>() where T : class
		=> For<T>(GetEntitySetName<T>());

	/// <summary>
	/// Creates a query builder for the specified entity set name.
	/// </summary>
	public ODataQueryBuilder<T> For<T>(string entitySetName) where T : class
		=> new(entitySetName, _logger);

	/// <summary>
	/// Executes a query and returns all matching entities, following pagination.
	/// </summary>
	public async Task<ODataResponse<T>> GetAllAsync<T>(
		ODataQueryBuilder<T> query,
		CancellationToken cancellationToken) where T : class
	{
		var allResults = new ODataResponse<T>();
		var url = query.BuildUrl();

		_logger.LogDebug("GetAllAsync<{Type}> - Initial URL: {Url}", typeof(T).Name, url);

		var pageCount = 0;
		do
		{
			// Check for cancellation at the start of each iteration
			cancellationToken.ThrowIfCancellationRequested();

			pageCount++;
			_logger.LogDebug("GetAllAsync<{Type}> - Fetching page {Page}, URL: {Url}", typeof(T).Name, pageCount, url);

			var response = await GetAsync<T>(url, query.CustomHeaders, cancellationToken).ConfigureAwait(false);
			allResults.Value.AddRange(response.Value);

			// Only set count from first page (or if a later page has count and first didn't)
			if (response.Count.HasValue && !allResults.Count.HasValue)
			{
				allResults.Count = response.Count;
			}

			_logger.LogDebug("GetAllAsync<{Type}> - Page {Page} returned {Count} items, total so far: {Total}",
				typeof(T).Name, pageCount, response.Value.Count, allResults.Value.Count);

			url = response.NextLink;
		}
		while (!string.IsNullOrEmpty(url));

		_logger.LogDebug("GetAllAsync<{Type}> - Complete. Total items: {Total}, Count header: {Count}",
			typeof(T).Name, allResults.Value.Count, allResults.Count);

		return allResults;
	}

	/// <summary>
	/// Executes a query and returns a single page of results.
	/// </summary>
	public async Task<ODataResponse<T>> GetAsync<T>(
		ODataQueryBuilder<T> query,
		CancellationToken cancellationToken = default) where T : class
	{
		var url = query.BuildUrl();
		_logger.LogDebug("GetAsync<{Type}> - URL: {Url}", typeof(T).Name, url);
		return await GetAsync<T>(url, query.CustomHeaders, cancellationToken).ConfigureAwait(false);
	}

	/// <summary>
	/// Gets a single entity by key.
	/// </summary>
	public async Task<T?> GetByKeyAsync<T, TKey>(
		TKey key,
		ODataQueryBuilder<T>? query = null,
		CancellationToken cancellationToken = default) where T : class
	{
		query ??= For<T>();
		query.Key(key);

		var url = query.BuildUrl();
		_logger.LogDebug("GetByKeyAsync<{Type}> - Key: {Key}, URL: {Url}", typeof(T).Name, key, url);

		var request = CreateRequest(HttpMethod.Get, url, query.CustomHeaders);

		var response = await SendWithRetryAsync(request, cancellationToken).ConfigureAwait(false);
		await EnsureSuccessAsync(response, url, cancellationToken).ConfigureAwait(false);

		return await response.Content.ReadFromJsonAsync<T>(_jsonOptions, cancellationToken).ConfigureAwait(false);
	}

	/// <summary>
	/// Creates a new entity.
	/// </summary>
	public async Task<T> CreateAsync<T>(
		string entitySet,
		T entity,
		IReadOnlyDictionary<string, string>? headers = null,
		CancellationToken cancellationToken = default) where T : class
	{
		var url = entitySet;
		_logger.LogDebug("CreateAsync<{Type}> - EntitySet: {EntitySet}", typeof(T).Name, entitySet);

		var request = CreateRequest(HttpMethod.Post, url, headers);
		request.Content = JsonContent.Create(entity, options: _jsonOptions);

		// Log the request body for debugging
		if (_logger.IsEnabled(LogLevel.Debug))
		{
			var requestBody = await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
			_logger.LogDebug("CreateAsync<{Type}> - Request body: {RequestBody}", typeof(T).Name, requestBody);
		}

		var response = await SendWithRetryAsync(request, cancellationToken).ConfigureAwait(false);
		await EnsureSuccessAsync(response, url, cancellationToken).ConfigureAwait(false);

		return await response.Content.ReadFromJsonAsync<T>(_jsonOptions, cancellationToken).ConfigureAwait(false)
			?? throw new ODataClientException("Failed to deserialize created entity");
	}

	/// <summary>
	/// Updates an entity using PATCH.
	/// </summary>
	public Task<T> UpdateAsync<T>(
		string entitySet,
		object key,
		object patchValues,
		IReadOnlyDictionary<string, string>? headers = null,
		CancellationToken cancellationToken = default) where T : class
		=> UpdateAsync<T, object>(entitySet, key, patchValues, headers, cancellationToken);

	/// <summary>
	/// Updates an entity using PATCH with strongly-typed key.
	/// </summary>
	public async Task<T> UpdateAsync<T, TKey>(
		string entitySet,
		TKey key,
		object patchValues,
		IReadOnlyDictionary<string, string>? headers = null,
		CancellationToken cancellationToken = default) where T : class
	{
		var url = $"{entitySet}({FormatKey(key)})";
		_logger.LogDebug("UpdateAsync<{Type}> - URL: {Url}", typeof(T).Name, url);

		var request = CreateRequest(new HttpMethod("PATCH"), url, headers);
		request.Content = JsonContent.Create(patchValues, options: _jsonOptions);

		var response = await SendWithRetryAsync(request, cancellationToken).ConfigureAwait(false);
		await EnsureSuccessAsync(response, url, cancellationToken).ConfigureAwait(false);

		// Handle 204 No Content response by fetching the updated entity
		if (response.StatusCode == HttpStatusCode.NoContent)
		{
			_logger.LogDebug("UpdateAsync<{Type}> - Received 204 No Content, fetching updated entity", typeof(T).Name);
			var getRequest = CreateRequest(HttpMethod.Get, url, headers);
			var getResponse = await SendWithRetryAsync(getRequest, cancellationToken).ConfigureAwait(false);
			await EnsureSuccessAsync(getResponse, url, cancellationToken).ConfigureAwait(false);
			return await getResponse.Content.ReadFromJsonAsync<T>(_jsonOptions, cancellationToken).ConfigureAwait(false)
				?? throw new ODataClientException("Failed to deserialize updated entity after refetch");
		}

		return await response.Content.ReadFromJsonAsync<T>(_jsonOptions, cancellationToken).ConfigureAwait(false)
			?? throw new ODataClientException("Failed to deserialize updated entity");
	}

	/// <summary>
	/// Deletes an entity.
	/// </summary>
	public async Task DeleteAsync(
		string entitySet,
		object key,
		IReadOnlyDictionary<string, string>? headers = null,
		CancellationToken cancellationToken = default)
	{
		var url = $"{entitySet}({FormatKey(key)})";
		_logger.LogDebug("DeleteAsync - EntitySet: {EntitySet}, Key: {Key}, URL: {Url}", entitySet, key, url);

		var request = CreateRequest(HttpMethod.Delete, url, headers);

		var response = await SendWithRetryAsync(request, cancellationToken).ConfigureAwait(false);
		await EnsureSuccessAsync(response, url, cancellationToken).ConfigureAwait(false);
	}

	/// <summary>
	/// Calls an OData function and returns the result.
	/// </summary>
	public async Task<TResult?> CallFunctionAsync<T, TResult>(
		ODataQueryBuilder<T> query,
		CancellationToken cancellationToken = default) where T : class
	{
		var url = query.BuildUrl();
		_logger.LogDebug("CallFunctionAsync<{Type}, {ResultType}> - URL: {Url}", typeof(T).Name, typeof(TResult).Name, url);

		var request = CreateRequest(HttpMethod.Get, url, query.CustomHeaders);

		var response = await SendWithRetryAsync(request, cancellationToken).ConfigureAwait(false);
		await EnsureSuccessAsync(response, url, cancellationToken).ConfigureAwait(false);

		var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
		_logger.LogDebug("CallFunctionAsync - Response content length: {Length}", content.Length);

		// Try to parse as OData response first
		try
		{
			using var doc = JsonDocument.Parse(content);

			// Check if it's a collection result
			if (doc.RootElement.TryGetProperty("value", out var valueElement))
			{
				_logger.LogDebug("CallFunctionAsync - Parsing 'value' property from response");
				return JsonSerializer.Deserialize<TResult>(valueElement.GetRawText(), _jsonOptions);
			}

			// Otherwise deserialize the whole thing
			_logger.LogDebug("CallFunctionAsync - Parsing entire response as {ResultType}", typeof(TResult).Name);
			return JsonSerializer.Deserialize<TResult>(content, _jsonOptions);
		}
		catch (Exception ex)
		{
			_logger.LogDebug(ex, "CallFunctionAsync - Failed to parse as OData, trying direct deserialization");
			return JsonSerializer.Deserialize<TResult>(content, _jsonOptions);
		}
	}

	/// <summary>
	/// Calls an OData action (POST).
	/// </summary>
	public async Task<TResult?> CallActionAsync<TResult>(
		string actionUrl,
		object? parameters = null,
		IReadOnlyDictionary<string, string>? headers = null,
		CancellationToken cancellationToken = default)
	{
		_logger.LogDebug("CallActionAsync<{ResultType}> - URL: {Url}", typeof(TResult).Name, actionUrl);

		var request = CreateRequest(HttpMethod.Post, actionUrl, headers);

		if (parameters is not null)
		{
			// Use action-specific JSON options that preserve Pascal case for OData action parameters
			request.Content = JsonContent.Create(parameters, options: _actionJsonOptions);
		}

		var response = await SendWithRetryAsync(request, cancellationToken).ConfigureAwait(false);
		await EnsureSuccessAsync(response, actionUrl, cancellationToken).ConfigureAwait(false);

		if (response.StatusCode == HttpStatusCode.NoContent)
		{
			_logger.LogDebug("CallActionAsync - Received NoContent response");
			return default;
		}

		return await response.Content.ReadFromJsonAsync<TResult>(_jsonOptions, cancellationToken).ConfigureAwait(false);
	}

	/// <summary>
	/// Gets the raw JSON response for a query URL.
	/// </summary>
	public async Task<JsonDocument> GetRawAsync(
		string url,
		IReadOnlyDictionary<string, string>? headers = null,
		CancellationToken cancellationToken = default)
	{
		var request = CreateRequest(HttpMethod.Get, url, headers);
		var response = await SendWithRetryAsync(request, cancellationToken).ConfigureAwait(false);
		await EnsureSuccessAsync(response, url, cancellationToken).ConfigureAwait(false);

		var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
		return JsonDocument.Parse(content);
	}

	#region Private Methods

	private async Task<ODataResponse<T>> GetAsync<T>(
		string url,
		IReadOnlyDictionary<string, string>? headers = null,
		CancellationToken cancellationToken = default) where T : class
	{
		var request = CreateRequest(HttpMethod.Get, url, headers);
		var response = await SendWithRetryAsync(request, cancellationToken).ConfigureAwait(false);
		await EnsureSuccessAsync(response, url, cancellationToken).ConfigureAwait(false);

		var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

		_logger.LogDebug("GetAsync<{Type}> - Response received, content length: {Length}", typeof(T).Name, content.Length);

		using var doc = JsonDocument.Parse(content);
		var result = new ODataResponse<T>();

		// Parse value array
		if (doc.RootElement.TryGetProperty("value", out var valueElement))
		{
			result.Value = JsonSerializer.Deserialize<List<T>>(valueElement.GetRawText(), _jsonOptions) ?? [];
			_logger.LogDebug("GetAsync<{Type}> - Parsed {Count} items from 'value' array", typeof(T).Name, result.Value.Count);
		}

		// Parse count
		if (doc.RootElement.TryGetProperty("@odata.count", out var countElement))
		{
			result.Count = countElement.GetInt64();
			_logger.LogDebug("GetAsync<{Type}> - @odata.count: {Count}", typeof(T).Name, result.Count);
		}

		// Parse nextLink
		if (doc.RootElement.TryGetProperty("@odata.nextLink", out var nextLinkElement))
		{
			result.NextLink = nextLinkElement.GetString();
			_logger.LogDebug("GetAsync<{Type}> - @odata.nextLink: {NextLink}", typeof(T).Name, result.NextLink);
		}

		// Parse deltaLink
		if (doc.RootElement.TryGetProperty("@odata.deltaLink", out var deltaLinkElement))
		{
			result.DeltaLink = deltaLinkElement.GetString();
		}

		return result;
	}

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

	private async Task EnsureSuccessAsync(
		HttpResponseMessage response,
		string requestUrl,
		CancellationToken cancellationToken)
	{
		var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
		var statusCode = (int)response.StatusCode;

		// Check for error status codes
		if (!response.IsSuccessStatusCode)
		{
			_logger.LogError("Request to {Url} failed with status {StatusCode}: {ResponseBody}",
				requestUrl, response.StatusCode, responseBody);

			throw response.StatusCode switch
			{
				HttpStatusCode.NotFound => new ODataNotFoundException($"Resource not found: {requestUrl}", requestUrl),
				HttpStatusCode.Unauthorized => new ODataUnauthorizedException("Unauthorized", responseBody, requestUrl),
				HttpStatusCode.Forbidden => new ODataForbiddenException("Forbidden", responseBody, requestUrl),
				_ => new ODataClientException($"Request failed with status {response.StatusCode}", statusCode, responseBody, requestUrl)
			};
		}

		// Check for HTML content returned with success status (misconfigured server/proxy)
		if (responseBody.Length > 0 && responseBody.TrimStart().StartsWith('<'))
		{
			var preview = responseBody.Length > 500 ? responseBody[..500] + "..." : responseBody;
			var contentType = response.Content.Headers.ContentType?.MediaType ?? "unknown";

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

	private static string GetEntitySetName<T>()
	{
		var name = typeof(T).Name;

		// Simple pluralization
		if (name.EndsWith('y'))
		{
			return name[..^1] + "ies";
		}

		if (name.EndsWith('s'))
		{
			return name;
		}

		return name + "s";
	}

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
