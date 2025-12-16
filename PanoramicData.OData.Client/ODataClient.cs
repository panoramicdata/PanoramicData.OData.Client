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
	/// Gets a single entity by key along with its ETag for concurrency control.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <typeparam name="TKey">The key type.</typeparam>
	/// <param name="key">The entity key.</param>
	/// <param name="query">Optional query builder for additional options like $select, $expand.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A response containing the entity and its ETag.</returns>
	public async Task<ODataSingleResponse<T>> GetByKeyWithETagAsync<T, TKey>(
		TKey key,
		ODataQueryBuilder<T>? query = null,
		CancellationToken cancellationToken = default) where T : class
	{
		query ??= For<T>();
		query.Key(key);

		var url = query.BuildUrl();
		_logger.LogDebug("GetByKeyWithETagAsync<{Type}> - Key: {Key}, URL: {Url}", typeof(T).Name, key, url);

		var request = CreateRequest(HttpMethod.Get, url, query.CustomHeaders);

		var response = await SendWithRetryAsync(request, cancellationToken).ConfigureAwait(false);
		await EnsureSuccessAsync(response, url, cancellationToken).ConfigureAwait(false);

		var result = new ODataSingleResponse<T>
		{
			Value = await response.Content.ReadFromJsonAsync<T>(_jsonOptions, cancellationToken).ConfigureAwait(false)
		};

		// Extract ETag from response headers
		if (response.Headers.ETag is not null)
		{
			result.ETag = response.Headers.ETag.ToString();
			_logger.LogDebug("GetByKeyWithETagAsync<{Type}> - ETag: {ETag}", typeof(T).Name, result.ETag);
		}

		return result;
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
		=> UpdateAsync<T, object>(entitySet, key, patchValues, etag: null, headers, cancellationToken);

	/// <summary>
	/// Updates an entity using PATCH with optimistic concurrency via ETag.
	/// </summary>
	/// <param name="entitySet">The entity set name.</param>
	/// <param name="key">The entity key.</param>
	/// <param name="patchValues">The values to update.</param>
	/// <param name="etag">The ETag value for concurrency control. If provided, an If-Match header is sent.</param>
	/// <param name="headers">Optional additional headers.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The updated entity.</returns>
	/// <exception cref="ODataConcurrencyException">Thrown when the server returns 412 Precondition Failed.</exception>
	public Task<T> UpdateAsync<T>(
		string entitySet,
		object key,
		object patchValues,
		string? etag,
		IReadOnlyDictionary<string, string>? headers = null,
		CancellationToken cancellationToken = default) where T : class
		=> UpdateAsync<T, object>(entitySet, key, patchValues, etag, headers, cancellationToken);

	/// <summary>
	/// Updates an entity using PATCH with strongly-typed key.
	/// </summary>
	public Task<T> UpdateAsync<T, TKey>(
		string entitySet,
		TKey key,
		object patchValues,
		IReadOnlyDictionary<string, string>? headers = null,
		CancellationToken cancellationToken = default) where T : class
		=> UpdateAsync<T, TKey>(entitySet, key, patchValues, etag: null, headers, cancellationToken);

	/// <summary>
	/// Updates an entity using PATCH with strongly-typed key and optimistic concurrency via ETag.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <typeparam name="TKey">The key type.</typeparam>
	/// <param name="entitySet">The entity set name.</param>
	/// <param name="key">The entity key.</param>
	/// <param name="patchValues">The values to update.</param>
	/// <param name="etag">The ETag value for concurrency control. If provided, an If-Match header is sent.</param>
	/// <param name="headers">Optional additional headers.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The updated entity.</returns>
	/// <exception cref="ODataConcurrencyException">Thrown when the server returns 412 Precondition Failed.</exception>
	public async Task<T> UpdateAsync<T, TKey>(
		string entitySet,
		TKey key,
		object patchValues,
		string? etag,
		IReadOnlyDictionary<string, string>? headers = null,
		CancellationToken cancellationToken = default) where T : class
	{
		var url = $"{entitySet}({FormatKey(key)})";
		_logger.LogDebug("UpdateAsync<{Type}> - URL: {Url}, ETag: {ETag}", typeof(T).Name, url, etag ?? "(none)");

		var request = CreateRequest(new HttpMethod("PATCH"), url, headers);
		request.Content = JsonContent.Create(patchValues, options: _jsonOptions);

		// Add If-Match header for concurrency control
		if (!string.IsNullOrEmpty(etag))
		{
			request.Headers.TryAddWithoutValidation("If-Match", etag);
			_logger.LogDebug("UpdateAsync<{Type}> - Added If-Match header: {ETag}", typeof(T).Name, etag);
		}

		var response = await SendWithRetryAsync(request, cancellationToken).ConfigureAwait(false);
		await EnsureSuccessAsync(response, url, etag, cancellationToken).ConfigureAwait(false);

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
	public Task DeleteAsync(
		string entitySet,
		object key,
		IReadOnlyDictionary<string, string>? headers = null,
		CancellationToken cancellationToken = default)
		=> DeleteAsync(entitySet, key, etag: null, headers, cancellationToken);

	/// <summary>
	/// Deletes an entity with optimistic concurrency via ETag.
	/// </summary>
	/// <param name="entitySet">The entity set name.</param>
	/// <param name="key">The entity key.</param>
	/// <param name="etag">The ETag value for concurrency control. If provided, an If-Match header is sent.</param>
	/// <param name="headers">Optional additional headers.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <exception cref="ODataConcurrencyException">Thrown when the server returns 412 Precondition Failed.</exception>
	public async Task DeleteAsync(
		string entitySet,
		object key,
		string? etag,
		IReadOnlyDictionary<string, string>? headers = null,
		CancellationToken cancellationToken = default)
	{
		var url = $"{entitySet}({FormatKey(key)})";
		_logger.LogDebug("DeleteAsync - EntitySet: {EntitySet}, Key: {Key}, URL: {Url}, ETag: {ETag}", entitySet, key, url, etag ?? "(none)");

		var request = CreateRequest(HttpMethod.Delete, url, headers);

		// Add If-Match header for concurrency control
		if (!string.IsNullOrEmpty(etag))
		{
			request.Headers.TryAddWithoutValidation("If-Match", etag);
			_logger.LogDebug("DeleteAsync - Added If-Match header: {ETag}", etag);
		}

		var response = await SendWithRetryAsync(request, cancellationToken).ConfigureAwait(false);
		await EnsureSuccessAsync(response, url, etag, cancellationToken).ConfigureAwait(false);
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

	#region Singleton Operations

	/// <summary>
	/// Gets a singleton entity (an entity without a key, like /Me or /Company).
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="singletonName">The singleton name (e.g., "Me", "Company").</param>
	/// <param name="headers">Optional additional headers.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The singleton entity.</returns>
	public async Task<T?> GetSingletonAsync<T>(
		string singletonName,
		IReadOnlyDictionary<string, string>? headers = null,
		CancellationToken cancellationToken = default) where T : class
	{
		_logger.LogDebug("GetSingletonAsync<{Type}> - Singleton: {Singleton}", typeof(T).Name, singletonName);

		var request = CreateRequest(HttpMethod.Get, singletonName, headers);
		var response = await SendWithRetryAsync(request, cancellationToken).ConfigureAwait(false);
		await EnsureSuccessAsync(response, singletonName, cancellationToken).ConfigureAwait(false);

		return await response.Content.ReadFromJsonAsync<T>(_jsonOptions, cancellationToken).ConfigureAwait(false);
	}

	/// <summary>
	/// Gets a singleton entity along with its ETag for concurrency control.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="singletonName">The singleton name (e.g., "Me", "Company").</param>
	/// <param name="headers">Optional additional headers.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A response containing the singleton entity and its ETag.</returns>
	public async Task<ODataSingleResponse<T>> GetSingletonWithETagAsync<T>(
		string singletonName,
		IReadOnlyDictionary<string, string>? headers = null,
		CancellationToken cancellationToken = default) where T : class
	{
		_logger.LogDebug("GetSingletonWithETagAsync<{Type}> - Singleton: {Singleton}", typeof(T).Name, singletonName);

		var request = CreateRequest(HttpMethod.Get, singletonName, headers);
		var response = await SendWithRetryAsync(request, cancellationToken).ConfigureAwait(false);
		await EnsureSuccessAsync(response, singletonName, cancellationToken).ConfigureAwait(false);

		var result = new ODataSingleResponse<T>
		{
			Value = await response.Content.ReadFromJsonAsync<T>(_jsonOptions, cancellationToken).ConfigureAwait(false)
		};

		if (response.Headers.ETag is not null)
		{
			result.ETag = response.Headers.ETag.ToString();
			_logger.LogDebug("GetSingletonWithETagAsync<{Type}> - ETag: {ETag}", typeof(T).Name, result.ETag);
		}

		return result;
	}

	/// <summary>
	/// Updates a singleton entity using PATCH.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="singletonName">The singleton name (e.g., "Me", "Company").</param>
	/// <param name="patchValues">The values to update.</param>
	/// <param name="headers">Optional additional headers.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The updated singleton entity.</returns>
	public Task<T> UpdateSingletonAsync<T>(
		string singletonName,
		object patchValues,
		IReadOnlyDictionary<string, string>? headers = null,
		CancellationToken cancellationToken = default) where T : class
		=> UpdateSingletonAsync<T>(singletonName, patchValues, etag: null, headers, cancellationToken);

	/// <summary>
	/// Updates a singleton entity using PATCH with optimistic concurrency via ETag.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="singletonName">The singleton name (e.g., "Me", "Company").</param>
	/// <param name="patchValues">The values to update.</param>
	/// <param name="etag">The ETag value for concurrency control.</param>
	/// <param name="headers">Optional additional headers.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The updated singleton entity.</returns>
	/// <exception cref="ODataConcurrencyException">Thrown when the server returns 412 Precondition Failed.</exception>
	public async Task<T> UpdateSingletonAsync<T>(
		string singletonName,
		object patchValues,
		string? etag,
		IReadOnlyDictionary<string, string>? headers = null,
		CancellationToken cancellationToken = default) where T : class
	{
		_logger.LogDebug("UpdateSingletonAsync<{Type}> - Singleton: {Singleton}, ETag: {ETag}", typeof(T).Name, singletonName, etag ?? "(none)");

		var request = CreateRequest(new HttpMethod("PATCH"), singletonName, headers);
		request.Content = JsonContent.Create(patchValues, options: _jsonOptions);

		if (!string.IsNullOrEmpty(etag))
		{
			request.Headers.TryAddWithoutValidation("If-Match", etag);
		}

		var response = await SendWithRetryAsync(request, cancellationToken).ConfigureAwait(false);
		await EnsureSuccessAsync(response, singletonName, etag, cancellationToken).ConfigureAwait(false);

		// Handle 204 No Content response by fetching the updated entity
		if (response.StatusCode == HttpStatusCode.NoContent)
		{
			_logger.LogDebug("UpdateSingletonAsync<{Type}> - Received 204 No Content, fetching updated singleton", typeof(T).Name);
			var getRequest = CreateRequest(HttpMethod.Get, singletonName, headers);
			var getResponse = await SendWithRetryAsync(getRequest, cancellationToken).ConfigureAwait(false);
			await EnsureSuccessAsync(getResponse, singletonName, cancellationToken).ConfigureAwait(false);
			return await getResponse.Content.ReadFromJsonAsync<T>(_jsonOptions, cancellationToken).ConfigureAwait(false)
				?? throw new ODataClientException("Failed to deserialize updated singleton after refetch");
		}

		return await response.Content.ReadFromJsonAsync<T>(_jsonOptions, cancellationToken).ConfigureAwait(false)
			?? throw new ODataClientException("Failed to deserialize updated singleton");
	}

	#endregion

	#region Stream/Media Entity Operations

	/// <summary>
	/// Gets the binary stream content of a media entity.
	/// OData V4 media entities store binary content accessed via /$value.
	/// </summary>
	/// <typeparam name="TKey">The key type.</typeparam>
	/// <param name="entitySet">The entity set name.</param>
	/// <param name="key">The entity key.</param>
	/// <param name="headers">Optional additional headers.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A stream containing the binary content.</returns>
	public async Task<Stream> GetStreamAsync<TKey>(
		string entitySet,
		TKey key,
		IReadOnlyDictionary<string, string>? headers = null,
		CancellationToken cancellationToken = default)
	{
		var url = $"{entitySet}({FormatKey(key)})/$value";
		_logger.LogDebug("GetStreamAsync - URL: {Url}", url);

		var request = CreateRequest(HttpMethod.Get, url, headers);
		var response = await SendWithRetryAsync(request, cancellationToken).ConfigureAwait(false);
		await EnsureSuccessAsync(response, url, cancellationToken).ConfigureAwait(false);

		return await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
	}

	/// <summary>
	/// Sets the binary stream content of a media entity.
	/// </summary>
	/// <typeparam name="TKey">The key type.</typeparam>
	/// <param name="entitySet">The entity set name.</param>
	/// <param name="key">The entity key.</param>
	/// <param name="stream">The stream containing the binary content.</param>
	/// <param name="contentType">The MIME content type of the stream.</param>
	/// <param name="headers">Optional additional headers.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	public async Task SetStreamAsync<TKey>(
		string entitySet,
		TKey key,
		Stream stream,
		string contentType = "application/octet-stream",
		IReadOnlyDictionary<string, string>? headers = null,
		CancellationToken cancellationToken = default)
	{
		var url = $"{entitySet}({FormatKey(key)})/$value";
		_logger.LogDebug("SetStreamAsync - URL: {Url}, ContentType: {ContentType}", url, contentType);

		var request = CreateRequest(HttpMethod.Put, url, headers);
		request.Content = new StreamContent(stream);
		request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);

		var response = await SendWithRetryAsync(request, cancellationToken).ConfigureAwait(false);
		await EnsureSuccessAsync(response, url, cancellationToken).ConfigureAwait(false);
	}

	/// <summary>
	/// Gets a named stream property from an entity.
	/// OData V4 supports named stream properties for entities with multiple binary properties.
	/// </summary>
	/// <typeparam name="TKey">The key type.</typeparam>
	/// <param name="entitySet">The entity set name.</param>
	/// <param name="key">The entity key.</param>
	/// <param name="propertyName">The name of the stream property.</param>
	/// <param name="headers">Optional additional headers.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A stream containing the binary content.</returns>
	public async Task<Stream> GetStreamPropertyAsync<TKey>(
		string entitySet,
		TKey key,
		string propertyName,
		IReadOnlyDictionary<string, string>? headers = null,
		CancellationToken cancellationToken = default)
	{
		var url = $"{entitySet}({FormatKey(key)})/{propertyName}";
		_logger.LogDebug("GetStreamPropertyAsync - URL: {Url}", url);

		var request = CreateRequest(HttpMethod.Get, url, headers);
		var response = await SendWithRetryAsync(request, cancellationToken).ConfigureAwait(false);
		await EnsureSuccessAsync(response, url, cancellationToken).ConfigureAwait(false);

		return await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
	}

	/// <summary>
	/// Sets a named stream property on an entity.
	/// </summary>
	/// <typeparam name="TKey">The key type.</typeparam>
	/// <param name="entitySet">The entity set name.</param>
	/// <param name="key">The entity key.</param>
	/// <param name="propertyName">The name of the stream property.</param>
	/// <param name="stream">The stream containing the binary content.</param>
	/// <param name="contentType">The MIME content type of the stream.</param>
	/// <param name="headers">Optional additional headers.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	public async Task SetStreamPropertyAsync<TKey>(
		string entitySet,
		TKey key,
		string propertyName,
		Stream stream,
		string contentType = "application/octet-stream",
		IReadOnlyDictionary<string, string>? headers = null,
		CancellationToken cancellationToken = default)
	{
		var url = $"{entitySet}({FormatKey(key)})/{propertyName}";
		_logger.LogDebug("SetStreamPropertyAsync - URL: {Url}, ContentType: {ContentType}", url, contentType);

		var request = CreateRequest(HttpMethod.Put, url, headers);
		request.Content = new StreamContent(stream);
		request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);

		var response = await SendWithRetryAsync(request, cancellationToken).ConfigureAwait(false);
		await EnsureSuccessAsync(response, url, cancellationToken).ConfigureAwait(false);
	}

	#endregion

	#region Delta Queries

	/// <summary>
	/// Gets changes since the last query using a delta link.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="deltaLink">The delta link from a previous query response.</param>
	/// <param name="headers">Optional additional headers.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A delta response containing added/modified entities and deleted entity IDs.</returns>
	public async Task<ODataDeltaResponse<T>> GetDeltaAsync<T>(
		string deltaLink,
		IReadOnlyDictionary<string, string>? headers = null,
		CancellationToken cancellationToken = default) where T : class
	{
		_logger.LogDebug("GetDeltaAsync<{Type}> - DeltaLink: {DeltaLink}", typeof(T).Name, deltaLink);

		var request = CreateRequest(HttpMethod.Get, deltaLink, headers);
		var response = await SendWithRetryAsync(request, cancellationToken).ConfigureAwait(false);
		await EnsureSuccessAsync(response, deltaLink, cancellationToken).ConfigureAwait(false);

		var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

		_logger.LogDebug("GetDeltaAsync<{Type}> - Response received, content length: {Length}", typeof(T).Name, content.Length);

		return ParseDeltaResponse<T>(content);
	}

	/// <summary>
	/// Gets all changes since the last query, following pagination.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="deltaLink">The delta link from a previous query response.</param>
	/// <param name="headers">Optional additional headers.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A delta response containing all added/modified entities and deleted entity IDs.</returns>
	public async Task<ODataDeltaResponse<T>> GetAllDeltaAsync<T>(
		string deltaLink,
		IReadOnlyDictionary<string, string>? headers = null,
		CancellationToken cancellationToken = default) where T : class
	{
		var allResults = new ODataDeltaResponse<T>();
		var url = deltaLink;

		_logger.LogDebug("GetAllDeltaAsync<{Type}> - Initial DeltaLink: {DeltaLink}", typeof(T).Name, deltaLink);

		var pageCount = 0;
		do
		{
			cancellationToken.ThrowIfCancellationRequested();

			pageCount++;
			_logger.LogDebug("GetAllDeltaAsync<{Type}> - Fetching page {Page}, URL: {Url}", typeof(T).Name, pageCount, url);

			var response = await GetDeltaAsync<T>(url, headers, cancellationToken).ConfigureAwait(false);
			allResults.Value.AddRange(response.Value);
			allResults.Deleted.AddRange(response.Deleted);

			// Preserve count from first page
			if (response.Count.HasValue && !allResults.Count.HasValue)
			{
				allResults.Count = response.Count;
			}

			// Keep the final delta link
			if (!string.IsNullOrEmpty(response.DeltaLink))
			{
				allResults.DeltaLink = response.DeltaLink;
			}

			_logger.LogDebug("GetAllDeltaAsync<{Type}> - Page {Page} returned {Count} items, {Deleted} deleted",
				typeof(T).Name, pageCount, response.Value.Count, response.Deleted.Count);

			url = response.NextLink;
		}
		while (!string.IsNullOrEmpty(url));

		_logger.LogDebug("GetAllDeltaAsync<{Type}> - Complete. Total items: {Total}, Total deleted: {Deleted}",
			typeof(T).Name, allResults.Value.Count, allResults.Deleted.Count);

		return allResults;
	}

	private ODataDeltaResponse<T> ParseDeltaResponse<T>(string content) where T : class
	{
		using var doc = JsonDocument.Parse(content);
		var result = new ODataDeltaResponse<T>();

		// Parse value array, separating normal entities from deleted ones
		if (doc.RootElement.TryGetProperty("value", out var valueElement))
		{
			foreach (var item in valueElement.EnumerateArray())
			{
				// Check if this is a deleted entity annotation
				if (item.TryGetProperty("@removed", out var removedElement) ||
					item.TryGetProperty("@odata.removed", out removedElement))
				{
					var deletedEntity = new ODataDeletedEntity();

					// Get the reason if present
					if (removedElement.TryGetProperty("reason", out var reasonElement))
					{
						deletedEntity.Reason = reasonElement.GetString();
					}

					// Get the ID
					if (item.TryGetProperty("@odata.id", out var idElement))
					{
						deletedEntity.Id = idElement.GetString();
					}
					else if (item.TryGetProperty("id", out idElement))
					{
						deletedEntity.Id = idElement.GetString();
					}

					result.Deleted.Add(deletedEntity);
					_logger.LogDebug("ParseDeltaResponse - Found deleted entity: {Id}, Reason: {Reason}",
						deletedEntity.Id, deletedEntity.Reason);
				}
				else
				{
					// Normal entity - deserialize it
					var entity = JsonSerializer.Deserialize<T>(item.GetRawText(), _jsonOptions);
					if (entity is not null)
					{
						result.Value.Add(entity);
					}
				}
			}

			_logger.LogDebug("ParseDeltaResponse<{Type}> - Parsed {Count} entities, {Deleted} deleted",
				typeof(T).Name, result.Value.Count, result.Deleted.Count);
		}

		// Parse count
		if (doc.RootElement.TryGetProperty("@odata.count", out var countElement))
		{
			result.Count = countElement.GetInt64();
		}

		// Parse nextLink (for paging within delta)
		if (doc.RootElement.TryGetProperty("@odata.nextLink", out var nextLinkElement))
		{
			result.NextLink = nextLinkElement.GetString();
		}

		// Parse deltaLink (for next delta query)
		if (doc.RootElement.TryGetProperty("@odata.deltaLink", out var deltaLinkElement))
		{
			result.DeltaLink = deltaLinkElement.GetString();
		}

		return result;
	}

	#endregion

	#region Entity References ($ref)

	/// <summary>
	/// Adds a reference to a collection navigation property.
	/// POST {entitySet}({key})/{navigationProperty}/$ref
	/// Body: { "@odata.id": "{targetEntitySet}({targetKey})" }
	/// </summary>
	/// <param name="entitySet">The source entity set name.</param>
	/// <param name="key">The source entity key.</param>
	/// <param name="navigationProperty">The navigation property name.</param>
	/// <param name="targetEntitySet">The target entity set name.</param>
	/// <param name="targetKey">The target entity key.</param>
	/// <param name="headers">Optional additional headers.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	public async Task AddReferenceAsync(
		string entitySet,
		object key,
		string navigationProperty,
		string targetEntitySet,
		object targetKey,
		IReadOnlyDictionary<string, string>? headers = null,
		CancellationToken cancellationToken = default)
	{
		var url = $"{entitySet}({FormatKey(key)})/{navigationProperty}/$ref";
		var targetUrl = $"{targetEntitySet}({FormatKey(targetKey)})";

		_logger.LogDebug("AddReferenceAsync - URL: {Url}, Target: {Target}", url, targetUrl);

		var request = CreateRequest(HttpMethod.Post, url, headers);
		request.Content = JsonContent.Create(new { Id = targetUrl }, options: _actionJsonOptions);

		// Override the content to use @odata.id property name
		var refBody = $"{{\"@odata.id\": \"{targetUrl}\"}}";
		request.Content = new StringContent(refBody, Encoding.UTF8, "application/json");

		var response = await SendWithRetryAsync(request, cancellationToken).ConfigureAwait(false);
		await EnsureSuccessAsync(response, url, cancellationToken).ConfigureAwait(false);
	}

	/// <summary>
	/// Removes a reference from a collection navigation property.
	/// DELETE {entitySet}({key})/{navigationProperty}/$ref?$id={targetEntitySet}({targetKey})
	/// </summary>
	/// <param name="entitySet">The source entity set name.</param>
	/// <param name="key">The source entity key.</param>
	/// <param name="navigationProperty">The navigation property name.</param>
	/// <param name="targetEntitySet">The target entity set name.</param>
	/// <param name="targetKey">The target entity key.</param>
	/// <param name="headers">Optional additional headers.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	public async Task RemoveReferenceAsync(
		string entitySet,
		object key,
		string navigationProperty,
		string targetEntitySet,
		object targetKey,
		IReadOnlyDictionary<string, string>? headers = null,
		CancellationToken cancellationToken = default)
	{
		var targetUrl = $"{targetEntitySet}({FormatKey(targetKey)})";
		var encodedTargetUrl = Uri.EscapeDataString(targetUrl);
		var url = $"{entitySet}({FormatKey(key)})/{navigationProperty}/$ref?$id={encodedTargetUrl}";

		_logger.LogDebug("RemoveReferenceAsync - URL: {Url}", url);

		var request = CreateRequest(HttpMethod.Delete, url, headers);

		var response = await SendWithRetryAsync(request, cancellationToken).ConfigureAwait(false);
		await EnsureSuccessAsync(response, url, cancellationToken).ConfigureAwait(false);
	}

	/// <summary>
	/// Sets a single-valued navigation property reference.
	/// PUT {entitySet}({key})/{navigationProperty}/$ref
	/// Body: { "@odata.id": "{targetEntitySet}({targetKey})" }
	/// </summary>
	/// <param name="entitySet">The source entity set name.</param>
	/// <param name="key">The source entity key.</param>
	/// <param name="navigationProperty">The navigation property name.</param>
	/// <param name="targetEntitySet">The target entity set name.</param>
	/// <param name="targetKey">The target entity key.</param>
	/// <param name="headers">Optional additional headers.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	public async Task SetReferenceAsync(
		string entitySet,
		object key,
		string navigationProperty,
		string targetEntitySet,
		object targetKey,
		IReadOnlyDictionary<string, string>? headers = null,
		CancellationToken cancellationToken = default)
	{
		var url = $"{entitySet}({FormatKey(key)})/{navigationProperty}/$ref";
		var targetUrl = $"{targetEntitySet}({FormatKey(targetKey)})";

		_logger.LogDebug("SetReferenceAsync - URL: {Url}, Target: {Target}", url, targetUrl);

		var request = CreateRequest(HttpMethod.Put, url, headers);
		var refBody = $"{{\"@odata.id\": \"{targetUrl}\"}}";
		request.Content = new StringContent(refBody, Encoding.UTF8, "application/json");

		var response = await SendWithRetryAsync(request, cancellationToken).ConfigureAwait(false);
		await EnsureSuccessAsync(response, url, cancellationToken).ConfigureAwait(false);
	}

	/// <summary>
	/// Deletes a single-valued navigation property reference.
	/// DELETE {entitySet}({key})/{navigationProperty}/$ref
	/// </summary>
	/// <param name="entitySet">The source entity set name.</param>
	/// <param name="key">The source entity key.</param>
	/// <param name="navigationProperty">The navigation property name.</param>
	/// <param name="headers">Optional additional headers.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	public async Task DeleteReferenceAsync(
		string entitySet,
		object key,
		string navigationProperty,
		IReadOnlyDictionary<string, string>? headers = null,
		CancellationToken cancellationToken = default)
	{
		var url = $"{entitySet}({FormatKey(key)})/{navigationProperty}/$ref";

		_logger.LogDebug("DeleteReferenceAsync - URL: {Url}", url);

		var request = CreateRequest(HttpMethod.Delete, url, headers);

		var response = await SendWithRetryAsync(request, cancellationToken).ConfigureAwait(false);
		await EnsureSuccessAsync(response, url, cancellationToken).ConfigureAwait(false);
	}

	#endregion

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

		// Extract ETag from response headers
		if (response.Headers.ETag is not null)
		{
			result.ETag = response.Headers.ETag.ToString();
			_logger.LogDebug("GetAsync<{Type}> - ETag: {ETag}", typeof(T).Name, result.ETag);
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
