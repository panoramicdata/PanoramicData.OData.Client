namespace PanoramicData.OData.Client;

/// <summary>
/// ODataClient - count and single-entity queries.
/// </summary>
public partial class ODataClient
{
	/// <summary>
	/// Asynchronously gets the total number of entities of the specified type in the data store.
	/// </summary>
	/// <typeparam name="T">The type of entity to count. Must be a reference type.</typeparam>
	/// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the total number of entities of type T.</returns>
	public Task<long> GetCountAsync<T>(
	CancellationToken cancellationToken) where T : class
		=> GetCountCoreAsync<T>(null, cancellationToken);

	/// <summary>
	/// Gets only the count of entities matching the query, without retrieving the entities.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="query">The query builder (optional filter, etc.).</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The count of matching entities.</returns>
	public Task<long> GetCountAsync<T>(
		[Optional] ODataQueryBuilder<T>? query,
		[Optional] CancellationToken cancellationToken) where T : class
		=> GetCountCoreAsync(query, cancellationToken);

	private async Task<long> GetCountCoreAsync<T>(
		ODataQueryBuilder<T>? query,
		CancellationToken cancellationToken) where T : class
	{
		query ??= For<T>();
		var baseUrl = query.BuildUrl();
		var typeName = typeof(T).Name;

		// Append /$count to the entity set path
		var url = baseUrl.Contains('?')
			? baseUrl.Replace("?", "/$count?")
			: baseUrl + "/$count";

		LoggerMessages.GetCountAsync(_logger, typeName, url);

		var content = await SendForContentAsync(HttpMethod.Get, url, query.CustomHeaders, cancellationToken).ConfigureAwait(false);

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
		[Optional] CancellationToken cancellationToken) where T : class
	{
		// When a key is set, the endpoint returns a single JSON object (not a {"value":[...]} collection).
		// Use the single-entity deserialization path to match the response shape.
		if (query.HasKey)
		{
			return await GetKeyedEntityAsync(query, _options.IgnoreResourceNotFoundException, cancellationToken).ConfigureAwait(false);
		}

		// Collection path: $top=1 to fetch only one item
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
		[Optional] CancellationToken cancellationToken) where T : class
	{
		// When a key is set, the endpoint returns a single JSON object directly
		if (query.HasKey)
		{
			return await GetKeyedEntityAsync(query, returnNullWhenNotFound: false, cancellationToken).ConfigureAwait(false)
				?? throw new InvalidOperationException("Sequence contains no elements");
		}

		// Collection path: $top=2 to detect multiple matches
		query.Top(2);
		var response = await GetAsync(query, cancellationToken).ConfigureAwait(false);

		return response.Value.Count == 0
			? throw new InvalidOperationException("Sequence contains no elements")
			: SingleOrDefaultFrom(response.Value)!;
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
		[Optional] CancellationToken cancellationToken) where T : class
	{
		// Fetch up to 2 to detect multiple matches
		query.Top(2);

		var response = await GetAsync(query, cancellationToken).ConfigureAwait(false);

		return SingleOrDefaultFrom(response.Value);
	}

	private async Task<T?> GetKeyedEntityAsync<T>(ODataQueryBuilder<T> query, bool returnNullWhenNotFound, CancellationToken cancellationToken) where T : class
	{
		var url = query.BuildUrl();
		LoggerMessages.GetAsync(_logger, typeof(T).Name, url);
		var request = CreateRequest(HttpMethod.Get, url, query.CustomHeaders);
		var httpResponse = await SendWithRetryAsync(request, cancellationToken).ConfigureAwait(false);
		if (returnNullWhenNotFound && httpResponse.StatusCode == HttpStatusCode.NotFound)
		{
			return null;
		}

		await EnsureSuccessAsync(httpResponse, url, cancellationToken).ConfigureAwait(false);
		return await httpResponse.Content.ReadFromJsonAsync<T>(_jsonOptions, cancellationToken).ConfigureAwait(false);
	}

	private static T? SingleOrDefaultFrom<T>(List<T> values) where T : class
		=> values.Count switch
		{
			0 => null,
			1 => values[0],
			_ => throw new InvalidOperationException("Sequence contains more than one element")
		};
}
