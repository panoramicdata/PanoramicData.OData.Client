namespace PanoramicData.OData.Client;

/// <summary>
/// FluentODataQueryBuilder - execution methods.
/// </summary>
public partial class FluentODataQueryBuilder
{

	/// <summary>
	/// Executes the query and returns a single page of results as dictionaries.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>An ODataResponse containing the results as dictionaries.</returns>
	/// <remarks>
	/// <para><b>New API pattern (recommended):</b></para>
	/// <code>
	/// var query = client.For&lt;Product&gt;("Products").Top(10);
	/// var response = await client.GetAsync(query, cancellationToken);
	/// </code>
	/// </remarks>
	public Task<ODataResponse<Dictionary<string, object?>>> GetAsync([Optional] CancellationToken cancellationToken)
		=> _client.GetFluentAsync(this, cancellationToken);

	/// <summary>
	/// Executes the query and returns the raw JSON response as a JsonDocument.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A JsonDocument containing the raw OData response. The caller is responsible for disposing the document.</returns>
	/// <remarks>
	/// <para>This method returns the raw JSON response without any parsing into typed objects.</para>
	/// <para>The returned JsonDocument must be disposed by the caller.</para>
	/// <example>
	/// <code>
	/// using var json = await client.For("incidents").Top(10).GetJsonAsync(ct);
	/// foreach (var item in json.RootElement.GetProperty("value").EnumerateArray())
	/// {
	///     Console.WriteLine(item.GetProperty("title").GetString());
	/// }
	/// </code>
	/// </example>
	/// </remarks>
	public Task<JsonDocument> GetJsonAsync([Optional] CancellationToken cancellationToken)
		=> _client.GetFluentJsonAsync(this, cancellationToken);

	/// <summary>
	/// Executes the query and returns all pages of results as dictionaries.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>An ODataResponse containing all results as dictionaries.</returns>
	/// <remarks>
	/// <para><b>New API pattern (recommended):</b></para>
	/// <code>
	/// var query = client.For&lt;Product&gt;("Products").Filter("Price gt 100");
	/// var response = await client.GetAllAsync(query, cancellationToken);
	/// </code>
	/// </remarks>
	public Task<ODataResponse<Dictionary<string, object?>>> GetAllAsync([Optional] CancellationToken cancellationToken)
		=> _client.GetAllFluentAsync(this, cancellationToken);

	/// <summary>
	/// Executes the query and returns all matching entries as dictionaries.
	/// This is a legacy compatibility alias for <see cref="GetAllAsync"/>.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A collection of dictionaries representing the matching entities.</returns>
	public async Task<IEnumerable<IDictionary<string, object?>>> FindEntriesAsync([Optional] CancellationToken cancellationToken)
	{
		var response = await GetAllAsync(cancellationToken).ConfigureAwait(false);
		return response.Value.Cast<IDictionary<string, object?>>();
	}

	/// <summary>
	/// Executes the query and returns a single entry as a dictionary.
	/// Requires Key() to have been called.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The entry as a dictionary, or null if not found.</returns>
	/// <remarks>
	/// <para><b>New API pattern (recommended):</b></para>
	/// <code>
	/// var product = await client.GetByKeyAsync&lt;Product, int&gt;(123, cancellationToken: ct);
	/// </code>
	/// </remarks>
	public Task<Dictionary<string, object?>?> GetEntryAsync([Optional] CancellationToken cancellationToken)
		=> _client.GetEntryFluentAsync(BuildUrl(), CustomHeaders, cancellationToken);

	/// <summary>
	/// Executes the query and returns the first matching entry as a dictionary.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The first matching entry as a dictionary, or null if not found.</returns>
	/// <remarks>
	/// <para><b>New API pattern (recommended):</b></para>
	/// <code>
	/// var query = client.For&lt;Product&gt;("Products").Filter("Name eq 'Widget'");
	/// var product = await client.GetFirstOrDefaultAsync(query, cancellationToken);
	/// </code>
	/// </remarks>
	public async Task<Dictionary<string, object?>?> GetFirstOrDefaultAsync([Optional] CancellationToken cancellationToken)
	{
		Top(1);
		var response = await GetAsync(cancellationToken).ConfigureAwait(false);
		return response.Value.FirstOrDefault();
	}

	/// <summary>
	/// Deletes the entity identified by the key set on this query builder.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A task representing the delete operation.</returns>
	/// <remarks>
	/// <para><b>New API pattern (recommended):</b></para>
	/// <code>
	/// await client.DeleteAsync("Products", 123, cancellationToken);
	/// </code>
	/// </remarks>
	public Task DeleteAsync([Optional] CancellationToken cancellationToken)
		=> _client.DeleteByUrlAsync(BuildUrl(), CustomHeaders, cancellationToken);

	/// <summary>
	/// Deletes the entity identified by the key set on this query builder.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A task representing the delete operation.</returns>
	/// <remarks>
	/// <para>This is an alias for <see cref="DeleteAsync"/> to match Simple.OData.Client naming conventions.</para>
	/// <example>
	/// <code>
	/// await client.For("incidents")
	///     .Key(entityId)
	///     .DeleteEntryAsync(cancellationToken);
	/// </code>
	/// </example>
	/// </remarks>
	public Task DeleteEntryAsync([Optional] CancellationToken cancellationToken)
		=> DeleteAsync(cancellationToken);

}
