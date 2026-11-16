namespace PanoramicData.OData.Client;

/// <summary>
/// ODataQueryBuilder - execution methods.
/// </summary>
public partial class ODataQueryBuilder<T> where T : class
{
	/// <summary>
	/// Executes the query and returns a single page of results.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>An ODataResponse containing the results.</returns>
	/// <exception cref="InvalidOperationException">Thrown if this query builder was not created from an ODataClient.</exception>
	public Task<ODataResponse<T>> GetAsync([Optional] CancellationToken cancellationToken)
	{
		EnsureClientAvailable();
		return _client!.GetAsync(this, cancellationToken);
	}

	/// <summary>
	/// Executes the query and returns all pages of results, following pagination.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>An ODataResponse containing all results.</returns>
	/// <exception cref="InvalidOperationException">Thrown if this query builder was not created from an ODataClient.</exception>
	public Task<ODataResponse<T>> GetAllAsync([Optional] CancellationToken cancellationToken)
	{
		EnsureClientAvailable();
		return _client!.GetAllAsync(this, cancellationToken);
	}

	/// <summary>
	/// Executes the query and returns all matching entities, following pagination.
	/// This is a Simple.OData.Client-compatible alias for <see cref="GetAllAsync"/> that returns the
	/// entities directly without wrapping them in an <see cref="ODataResponse{T}"/>.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>All matching entities.</returns>
	/// <exception cref="InvalidOperationException">Thrown if this query builder was not created from an ODataClient.</exception>
	public async Task<IEnumerable<T>> FindEntriesAsync([Optional] CancellationToken cancellationToken)
	{
		var response = await GetAllAsync(cancellationToken).ConfigureAwait(false);
		return response.Value;
	}

	/// <summary>
	/// Gets the first entity matching the query, or null if no match.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The first matching entity, or null.</returns>
	/// <exception cref="InvalidOperationException">Thrown if this query builder was not created from an ODataClient.</exception>
	public Task<T?> GetFirstOrDefaultAsync([Optional] CancellationToken cancellationToken)
	{
		EnsureClientAvailable();
		return _client!.GetFirstOrDefaultAsync(this, cancellationToken);
	}

	/// <summary>
	/// Gets a single entity matching the query. Throws if zero or more than one match.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The single matching entity.</returns>
	/// <exception cref="InvalidOperationException">Thrown if this query builder was not created from an ODataClient, or if zero or more than one entity matches.</exception>
	public Task<T> GetSingleAsync([Optional] CancellationToken cancellationToken)
	{
		EnsureClientAvailable();
		return _client!.GetSingleAsync(this, cancellationToken);
	}

	/// <summary>
	/// Gets a single entity matching the query, or null if no match. Throws if more than one match.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The single matching entity, or null.</returns>
	/// <exception cref="InvalidOperationException">Thrown if this query builder was not created from an ODataClient, or if more than one entity matches.</exception>
	public Task<T?> GetSingleOrDefaultAsync([Optional] CancellationToken cancellationToken)
	{
		EnsureClientAvailable();
		return _client!.GetSingleOrDefaultAsync(this, cancellationToken);
	}

	/// <summary>
	/// Gets only the count of entities matching the query, without retrieving the entities.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The count of matching entities.</returns>
	/// <exception cref="InvalidOperationException">Thrown if this query builder was not created from an ODataClient.</exception>
	public Task<long> GetCountAsync([Optional] CancellationToken cancellationToken)
	{
		EnsureClientAvailable();
		return _client!.GetCountAsync(this, cancellationToken);
	}

	private void EnsureClientAvailable()
	{
		if (_client is null)
		{
			throw new InvalidOperationException(
				"This query builder was not created from an ODataClient instance. " +
				"Use client.For<T>() to create a query builder that supports direct execution, " +
				"or pass this query to the client's GetAsync/GetAllAsync methods.");
		}
	}
}
