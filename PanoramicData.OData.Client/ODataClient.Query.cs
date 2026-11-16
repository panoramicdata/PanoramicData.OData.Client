namespace PanoramicData.OData.Client;

/// <summary>
/// ODataClient - Query operations.
/// </summary>
public partial class ODataClient
{
	/// <summary>
	/// Creates a query builder for the specified entity set.
	/// </summary>
	public ODataQueryBuilder<T> For<T>() where T : class
		=> For<T>(GetEntitySetName<T>());

	/// <summary>
	/// Creates a query builder for the specified entity set name.
	/// </summary>
	public ODataQueryBuilder<T> For<T>(string entitySetName) where T : class
		=> new(this, entitySetName, _logger);

	/// <summary>
	/// Executes a query and returns all matching entities, following pagination.
	/// </summary>
	public async Task<ODataResponse<T>> GetAllAsync<T>(
		ODataQueryBuilder<T> query,
		CancellationToken cancellationToken) where T : class
	{
		var allResults = new ODataResponse<T>();
		var url = query.BuildUrl();
		var typeName = typeof(T).Name;

		LoggerMessages.GetAllAsyncInitial(_logger, typeName, url);

		await FetchPagesAsync(
			url,
			(pageUrl, pageNumber) =>
			{
				LoggerMessages.GetAllAsyncFetchingPage(_logger, typeName, pageNumber, pageUrl);
				return GetAsync<T>(pageUrl, query.CustomHeaders, cancellationToken);
			},
			page => page.NextLink,
			(page, pageNumber) =>
			{
				allResults.Value.AddRange(page.Value);
				allResults.Count ??= page.Count;
				LoggerMessages.GetAllAsyncPageReturned(_logger, typeName, pageNumber, page.Value.Count, allResults.Value.Count);
			},
			cancellationToken).ConfigureAwait(false);

		LoggerMessages.GetAllAsyncComplete(_logger, typeName, allResults.Value.Count, allResults.Count);

		return allResults;
	}

	/// <summary>
	/// Executes a query and returns a single page of results.
	/// </summary>
	public async Task<ODataResponse<T>> GetAsync<T>(
		ODataQueryBuilder<T> query,
		[Optional] CancellationToken cancellationToken) where T : class
	{
		var url = query.BuildUrl();
		LoggerMessages.GetAsync(_logger, typeof(T).Name, url);
		return await GetAsync<T>(url, query.CustomHeaders, cancellationToken).ConfigureAwait(false);
	}

	/// <summary>
	/// Gets a single entity by key.
	/// </summary>
	public Task<T?> GetByKeyAsync<T, TKey>(
		TKey key,
		[Optional] ODataQueryBuilder<T>? query,
		[Optional] CancellationToken cancellationToken) where T : class
		=> GetByKeyCoreAsync(key, query, _options.IgnoreResourceNotFoundException, cancellationToken);

	/// <summary>
	/// Gets a single entity by key, returning <c>null</c> if the resource is not found (HTTP 404).
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <typeparam name="TKey">The key type.</typeparam>
	/// <param name="key">The entity key.</param>
	/// <param name="query">Optional query builder for additional options like $select, $expand.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The entity, or <c>null</c> if not found.</returns>
	public Task<T?> GetByKeyOrDefaultAsync<T, TKey>(
		TKey key,
		[Optional] ODataQueryBuilder<T>? query,
		[Optional] CancellationToken cancellationToken) where T : class
		=> GetByKeyCoreAsync(key, query, returnNullWhenNotFound: true, cancellationToken);

	private async Task<T?> GetByKeyCoreAsync<T, TKey>(
		TKey key,
		ODataQueryBuilder<T>? query,
		bool returnNullWhenNotFound,
		CancellationToken cancellationToken) where T : class
	{
		query ??= For<T>();
		query.Key(key);

		var url = query.BuildUrl();
		LoggerMessages.GetByKeyAsync(_logger, typeof(T).Name, key!, url);

		var request = CreateRequest(HttpMethod.Get, url, query.CustomHeaders);

		var response = await SendWithRetryAsync(request, cancellationToken).ConfigureAwait(false);

		if (returnNullWhenNotFound && response.StatusCode == HttpStatusCode.NotFound)
		{
			return null;
		}

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
		[Optional] ODataQueryBuilder<T>? query,
		[Optional] CancellationToken cancellationToken) where T : class
	{
		query ??= For<T>();
		query.Key(key);

		var url = query.BuildUrl();
		var typeName = typeof(T).Name;
		LoggerMessages.GetByKeyWithETagAsync(_logger, typeName, key!, url);

		var request = CreateRequest(HttpMethod.Get, url, query.CustomHeaders);

		var response = await SendWithRetryAsync(request, cancellationToken).ConfigureAwait(false);

		if (_options.IgnoreResourceNotFoundException && response.StatusCode == HttpStatusCode.NotFound)
		{
			return new ODataSingleResponse<T> { Value = null };
		}

		await EnsureSuccessAsync(response, url, cancellationToken).ConfigureAwait(false);

		var result = new ODataSingleResponse<T>
		{
			Value = await response.Content.ReadFromJsonAsync<T>(_jsonOptions, cancellationToken).ConfigureAwait(false)
		};

		// Extract ETag from response headers
		if (response.Headers.ETag is not null)
		{
			result.ETag = response.Headers.ETag.ToString();
			LoggerMessages.GetByKeyWithETagResult(_logger, typeName, result.ETag);
		}

		return result;
	}
	/// <summary>
	/// Gets the raw JSON response for a query URL.
	/// </summary>
	public async Task<JsonDocument> GetRawAsync(
		string url,
		[Optional] IReadOnlyDictionary<string, string>? headers,
		[Optional] CancellationToken cancellationToken)
	{
		var content = await SendForContentAsync(HttpMethod.Get, url, headers, cancellationToken).ConfigureAwait(false);
		return JsonDocument.Parse(content);
	}

	private async Task<ODataResponse<T>> GetAsync<T>(
		string url,
		[Optional] IReadOnlyDictionary<string, string>? headers,
		[Optional] CancellationToken cancellationToken) where T : class
	{
		var response = await SendRequestAsync(HttpMethod.Get, url, headers, cancellationToken).ConfigureAwait(false);
		var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
		var typeName = typeof(T).Name;

		LoggerMessages.GetAsyncResponseReceived(_logger, typeName, content.Length);

		using var doc = JsonDocument.Parse(content);
		var result = new ODataResponse<T>();

		// Parse value array
		if (doc.RootElement.TryGetProperty("value", out var valueElement))
		{
			result.Value = JsonSerializer.Deserialize<List<T>>(valueElement.GetRawText(), _jsonOptions) ?? [];
			LoggerMessages.GetAsyncParsedItems(_logger, typeName, result.Value.Count);
		}

		var paging = ODataPagingAnnotations.Read(doc.RootElement);
		result.Count = paging.Count;
		result.NextLink = paging.NextLink;
		result.DeltaLink = paging.DeltaLink;

		if (result.Count.HasValue)
		{
			LoggerMessages.GetAsyncODataCount(_logger, typeName, result.Count.Value);
		}

		if (result.NextLink is not null)
		{
			LoggerMessages.GetAsyncNextLink(_logger, typeName, result.NextLink);
		}

		// Extract ETag from response headers
		if (response.Headers.ETag is not null)
		{
			result.ETag = response.Headers.ETag.ToString();
			LoggerMessages.GetAsyncETag(_logger, typeName, result.ETag);
		}

		return result;
	}
}
