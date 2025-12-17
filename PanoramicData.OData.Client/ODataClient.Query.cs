using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

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
		var typeName = typeof(T).Name;

		LoggerMessages.GetAllAsyncInitial(_logger, typeName, url);

		var pageCount = 0;
		do
		{
			// Check for cancellation at the start of each iteration
			cancellationToken.ThrowIfCancellationRequested();

			pageCount++;
			LoggerMessages.GetAllAsyncFetchingPage(_logger, typeName, pageCount, url);

			var response = await GetAsync<T>(url, query.CustomHeaders, cancellationToken).ConfigureAwait(false);
			allResults.Value.AddRange(response.Value);

			// Only set count from first page (or if a later page has count and first didn't)
			if (response.Count.HasValue && !allResults.Count.HasValue)
			{
				allResults.Count = response.Count;
			}

			LoggerMessages.GetAllAsyncPageReturned(_logger, typeName, pageCount, response.Value.Count, allResults.Value.Count);

			url = response.NextLink;
		}
		while (!string.IsNullOrEmpty(url));

		LoggerMessages.GetAllAsyncComplete(_logger, typeName, allResults.Value.Count, allResults.Count);

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
		LoggerMessages.GetAsync(_logger, typeof(T).Name, url);
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
		LoggerMessages.GetByKeyAsync(_logger, typeof(T).Name, key!, url);

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
		var typeName = typeof(T).Name;
		LoggerMessages.GetByKeyWithETagAsync(_logger, typeName, key!, url);

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
			LoggerMessages.GetByKeyWithETagResult(_logger, typeName, result.ETag);
		}

		return result;
	}

	/// <summary>
	/// Calls an OData function and returns the result.
	/// </summary>
	public async Task<TResult?> CallFunctionAsync<T, TResult>(
		ODataQueryBuilder<T> query,
		CancellationToken cancellationToken = default) where T : class
	{
		var url = query.BuildUrl();
		LoggerMessages.CallFunctionAsync(_logger, typeof(T).Name, typeof(TResult).Name, url);

		var request = CreateRequest(HttpMethod.Get, url, query.CustomHeaders);

		var response = await SendWithRetryAsync(request, cancellationToken).ConfigureAwait(false);
		await EnsureSuccessAsync(response, url, cancellationToken).ConfigureAwait(false);

		var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
		LoggerMessages.CallFunctionAsyncResponseLength(_logger, content.Length);

		// Try to parse as OData response first
		try
		{
			using var doc = JsonDocument.Parse(content);

			// Check if it's a collection result
			if (doc.RootElement.TryGetProperty("value", out var valueElement))
			{
				LoggerMessages.CallFunctionAsyncParsingValue(_logger);
				return JsonSerializer.Deserialize<TResult>(valueElement.GetRawText(), _jsonOptions);
			}

			// Otherwise deserialize the whole thing
			LoggerMessages.CallFunctionAsyncParsingEntire(_logger, typeof(TResult).Name);
			return JsonSerializer.Deserialize<TResult>(content, _jsonOptions);
		}
		catch (Exception)
		{
			LoggerMessages.CallFunctionAsyncFallback(_logger);
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
		LoggerMessages.CallActionAsync(_logger, typeof(TResult).Name, actionUrl);

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
			LoggerMessages.CallActionAsyncNoContent(_logger);
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

	private async Task<ODataResponse<T>> GetAsync<T>(
		string url,
		IReadOnlyDictionary<string, string>? headers = null,
		CancellationToken cancellationToken = default) where T : class
	{
		var request = CreateRequest(HttpMethod.Get, url, headers);
		var response = await SendWithRetryAsync(request, cancellationToken).ConfigureAwait(false);
		await EnsureSuccessAsync(response, url, cancellationToken).ConfigureAwait(false);

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

		// Parse count
		if (doc.RootElement.TryGetProperty("@odata.count", out var countElement))
		{
			result.Count = countElement.GetInt64();
			LoggerMessages.GetAsyncODataCount(_logger, typeName, result.Count.Value);
		}

		// Parse nextLink
		if (doc.RootElement.TryGetProperty("@odata.nextLink", out var nextLinkElement))
		{
			result.NextLink = nextLinkElement.GetString();
			LoggerMessages.GetAsyncNextLink(_logger, typeName, result.NextLink);
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
			LoggerMessages.GetAsyncETag(_logger, typeName, result.ETag);
		}

		return result;
	}

	/// <summary>
	/// Asynchronously gets the total number of entities of the specified type in the data store.
	/// </summary>
	/// <typeparam name="T">The type of entity to count. Must be a reference type.</typeparam>
	/// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the total number of entities of type T.</returns>
	public Task<long> GetCountAsync<T>(
	CancellationToken cancellationToken) where T : class
		=> GetCountAsync<T>(null, cancellationToken);

	/// <summary>
	/// Gets only the count of entities matching the query, without retrieving the entities.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="query">The query builder (optional filter, etc.).</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The count of matching entities.</returns>
	public async Task<long> GetCountAsync<T>(
		ODataQueryBuilder<T>? query = null,
		CancellationToken cancellationToken = default) where T : class
	{
		query ??= For<T>();
		var baseUrl = query.BuildUrl();
		var typeName = typeof(T).Name;

		// Append /$count to the entity set path
		var url = baseUrl.Contains('?')
			? baseUrl.Replace("?", "/$count?")
			: baseUrl + "/$count";

		LoggerMessages.GetCountAsync(_logger, typeName, url);

		var request = CreateRequest(HttpMethod.Get, url, query.CustomHeaders);

		var response = await SendWithRetryAsync(request, cancellationToken).ConfigureAwait(false);
		await EnsureSuccessAsync(response, url, cancellationToken).ConfigureAwait(false);

		var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

		if (long.TryParse(content.Trim(), out var count))
		{
			LoggerMessages.GetCountAsyncResult(_logger, typeName, count);
			return count;
		}

		throw new InvalidOperationException($"Could not parse count response: {content}");
	}

	/// <summary>
	/// Gets the first entity matching the query, or null if no match.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="query">The query builder.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The first matching entity, or null.</returns>
	public async Task<T?> GetFirstOrDefaultAsync<T>(
		ODataQueryBuilder<T> query,
		CancellationToken cancellationToken = default) where T : class
	{
		// Ensure we only fetch one item
		query.Top(1);

		var response = await GetAsync(query, cancellationToken).ConfigureAwait(false);
		return response.Value.FirstOrDefault();
	}

	/// <summary>
	/// Gets a single entity matching the query. Throws if zero or more than one match.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="query">The query builder.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The single matching entity.</returns>
	/// <exception cref="InvalidOperationException">Thrown if zero or more than one entity matches.</exception>
	public async Task<T> GetSingleAsync<T>(
		ODataQueryBuilder<T> query,
		CancellationToken cancellationToken = default) where T : class
	{
		// Fetch up to 2 to detect multiple matches
		query.Top(2);

		var response = await GetAsync(query, cancellationToken).ConfigureAwait(false);

		return response.Value.Count switch
		{
			0 => throw new InvalidOperationException("Sequence contains no elements"),
			1 => response.Value[0],
			_ => throw new InvalidOperationException("Sequence contains more than one element")
		};
	}

	/// <summary>
	/// Gets a single entity matching the query, or null if no match. Throws if more than one match.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="query">The query builder.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The single matching entity, or null.</returns>
	/// <exception cref="InvalidOperationException">Thrown if more than one entity matches.</exception>
	public async Task<T?> GetSingleOrDefaultAsync<T>(
		ODataQueryBuilder<T> query,
		CancellationToken cancellationToken = default) where T : class
	{
		// Fetch up to 2 to detect multiple matches
		query.Top(2);

		var response = await GetAsync(query, cancellationToken).ConfigureAwait(false);

		return response.Value.Count switch
		{
			0 => default,
			1 => response.Value[0],
			_ => throw new InvalidOperationException("Sequence contains more than one element")
		};
	}

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
}
