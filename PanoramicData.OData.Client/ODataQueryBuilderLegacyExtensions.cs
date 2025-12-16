namespace PanoramicData.OData.Client;

/// <summary>
/// Legacy compatibility extension methods for ODataQueryBuilder.
/// These methods are provided for backward compatibility with Simple.OData.Client fluent API pattern.
/// </summary>
public static class ODataQueryBuilderLegacyExtensions
{
	// Static reference to the client - must be set before using legacy methods
	[ThreadStatic]
	private static ODataClient? _currentClient;

	/// <summary>
	/// Sets the current client for legacy extension methods.
	/// This must be called before using FindEntriesAsync, FindEntryAsync, or DeleteEntryAsync on the query builder.
	/// </summary>
	/// <param name="client">The OData client to use for executing queries.</param>
	/// <remarks>
	/// This is a workaround for backward compatibility. The new API pattern passes the query to the client:
	/// <code>
	/// var results = await client.GetAsync(query, cancellationToken);
	/// </code>
	/// </remarks>
	[Obsolete("This is a compatibility shim. Use client.GetAsync(query, cancellationToken) instead of query.FindEntriesAsync().")]
	public static void SetCurrentClient(ODataClient client) => _currentClient = client;

	/// <summary>
	/// Executes the query and returns matching entries.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="query">The query builder.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A collection of matching entities.</returns>
	/// <remarks>
	/// <para>This method is provided for backward compatibility with Simple.OData.Client.</para>
	/// <para><b>Migration:</b> Use <c>client.GetAsync(query, cancellationToken)</c> or <c>client.GetAllAsync(query, cancellationToken)</c> instead.</para>
	/// <para>Example:</para>
	/// <code>
	/// // Old pattern (deprecated):
	/// var results = await query.FindEntriesAsync(cancellationToken);
	///
	/// // New pattern:
	/// var response = await client.GetAsync(query, cancellationToken);
	/// var results = response.Value;
	///
	/// // Or to get all pages:
	/// var response = await client.GetAllAsync(query, cancellationToken);
	/// var results = response.Value;
	/// </code>
	/// </remarks>
	[Obsolete("Use client.GetAsync(query, cancellationToken) or client.GetAllAsync(query, cancellationToken) instead. " +
		"The new API separates query building from execution: var response = await client.GetAsync(query, ct); var items = response.Value;",
		error: true)]
	public static async Task<IEnumerable<T>> FindEntriesAsync<T>(
		this ODataQueryBuilder<T> query,
		CancellationToken cancellationToken) where T : class
	{
		var client = _currentClient ?? throw new InvalidOperationException(
			"No ODataClient has been set. Call ODataQueryBuilderLegacyExtensions.SetCurrentClient(client) first, " +
			"or better yet, migrate to the new pattern: client.GetAsync(query, cancellationToken)");

		var response = await client.GetAllAsync(query, cancellationToken).ConfigureAwait(false);
		return response.Value;
	}

	/// <summary>
	/// Executes the query and returns a single matching entry.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="query">The query builder.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The matching entity, or null if not found.</returns>
	/// <remarks>
	/// <para>This method is provided for backward compatibility with Simple.OData.Client.</para>
	/// <para><b>Migration:</b> Use <c>client.GetFirstOrDefaultAsync(query, cancellationToken)</c> instead.</para>
	/// <para>Example:</para>
	/// <code>
	/// // Old pattern (deprecated):
	/// var entry = await query.FindEntryAsync(cancellationToken);
	///
	/// // New pattern:
	/// var entry = await client.GetFirstOrDefaultAsync(query, cancellationToken);
	///
	/// // Or for key-based lookup:
	/// var entry = await client.GetByKeyAsync&lt;Product, int&gt;(123, cancellationToken: cancellationToken);
	/// </code>
	/// </remarks>
	[Obsolete("Use client.GetFirstOrDefaultAsync(query, cancellationToken) or client.GetByKeyAsync<T, TKey>(key, cancellationToken) instead. " +
		"The new API separates query building from execution.",
		error: true)]
	public static async Task<T?> FindEntryAsync<T>(
		this ODataQueryBuilder<T> query,
		CancellationToken cancellationToken) where T : class
	{
		var client = _currentClient ?? throw new InvalidOperationException(
			"No ODataClient has been set. Call ODataQueryBuilderLegacyExtensions.SetCurrentClient(client) first, " +
			"or better yet, migrate to the new pattern: client.GetFirstOrDefaultAsync(query, cancellationToken)");

		return await client.GetFirstOrDefaultAsync(query, cancellationToken).ConfigureAwait(false);
	}

	/// <summary>
	/// Deletes the entry identified by the key set on this query builder.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="query">The query builder with a key set.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A task representing the delete operation.</returns>
	/// <remarks>
	/// <para>This method is provided for backward compatibility with Simple.OData.Client.</para>
	/// <para><b>Migration:</b> Use <c>client.DeleteAsync(entitySet, key, cancellationToken)</c> instead.</para>
	/// <para>Example:</para>
	/// <code>
	/// // Old pattern (deprecated):
	/// await client.For&lt;Product&gt;().Key(123).DeleteEntryAsync(cancellationToken);
	///
	/// // New pattern:
	/// await client.DeleteAsync("Products", 123, cancellationToken);
	/// </code>
	/// </remarks>
	[Obsolete("Use client.DeleteAsync(entitySet, key, cancellationToken) instead. " +
		"Example: await client.DeleteAsync(\"Products\", 123, cancellationToken);",
		error: true)]
	public static async Task DeleteEntryAsync<T>(
		this ODataQueryBuilder<T> query,
		CancellationToken cancellationToken) where T : class
	{
		var client = _currentClient ?? throw new InvalidOperationException(
			"No ODataClient has been set. Call ODataQueryBuilderLegacyExtensions.SetCurrentClient(client) first, " +
			"or better yet, migrate to the new pattern: client.DeleteAsync(entitySet, key, cancellationToken)");

		// Build the URL and execute DELETE
		var url = query.BuildUrl();

		// Use reflection or a method to delete - we need to call the internal method
		await client.DeleteByUrlAsync(url, query.CustomHeaders, cancellationToken).ConfigureAwait(false);
	}

	/// <summary>
	/// Continues fetching entries from the next page.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="query">The query builder.</param>
	/// <param name="nextPageLink">The next page URL from a previous response.</param>
	/// <param name="annotations">Feed annotations (for compatibility - not used in new API).</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A collection of entities from the next page.</returns>
	/// <remarks>
	/// <para>This method is provided for backward compatibility with Simple.OData.Client.</para>
	/// <para><b>Migration:</b> Use <c>client.GetAllAsync(query, cancellationToken)</c> which handles pagination automatically,
	/// or check <c>response.NextLink</c> and fetch manually.</para>
	/// <para>Example:</para>
	/// <code>
	/// // Old pattern (deprecated):
	/// var annotations = new ODataFeedAnnotations();
	/// var page1 = await query.FindEntriesAsync(annotations, cancellationToken);
	/// if (annotations.NextPageLink != null)
	/// {
	///     var page2 = await query.FindEntriesAsync(annotations.NextPageLink, annotations, cancellationToken);
	/// }
	///
	/// // New pattern (automatic pagination):
	/// var allResults = await client.GetAllAsync(query, cancellationToken);
	///
	/// // Or manual pagination:
	/// var response = await client.GetAsync(query, cancellationToken);
	/// while (response.NextLink != null)
	/// {
	///     response = await client.GetAsync(response.NextLink, cancellationToken);
	/// }
	/// </code>
	/// </remarks>
	[Obsolete("Use client.GetAllAsync(query, cancellationToken) for automatic pagination, " +
		"or use response.NextLink with client.GetAsync() for manual control.",
		error: true)]
	public static async Task<IEnumerable<T>> FindEntriesAsync<T>(
		this ODataQueryBuilder<T> query,
		Uri nextPageLink,
		ODataFeedAnnotations annotations,
		CancellationToken cancellationToken) where T : class
	{
		var client = _currentClient ?? throw new InvalidOperationException(
			"No ODataClient has been set. Call ODataQueryBuilderLegacyExtensions.SetCurrentClient(client) first, " +
			"or better yet, migrate to client.GetAllAsync(query, cancellationToken) for automatic pagination.");

		var response = await client.GetAsync(query, cancellationToken).ConfigureAwait(false);

		// Update annotations for compatibility
		annotations.NextPageLink = response.NextLink != null ? new Uri(response.NextLink, UriKind.RelativeOrAbsolute) : null;
		annotations.Count = response.Count;

		return response.Value;
	}
}

/// <summary>
/// Feed annotations for backward compatibility with Simple.OData.Client.
/// </summary>
/// <remarks>
/// <para>This class is provided for backward compatibility.</para>
/// <para><b>Migration:</b> Use <see cref="ODataResponse{T}"/> properties directly:</para>
/// <list type="bullet">
/// <item><c>NextPageLink</c> → <c>response.NextLink</c></item>
/// <item><c>Count</c> → <c>response.Count</c></item>
/// <item><c>DeltaLink</c> → <c>response.DeltaLink</c></item>
/// </list>
/// </remarks>
[Obsolete("Use ODataResponse<T> properties instead: response.NextLink, response.Count, response.DeltaLink", error: true)]
public class ODataFeedAnnotations
{
	/// <summary>
	/// Gets or sets the next page link for server-driven paging.
	/// </summary>
	/// <remarks>Use <c>ODataResponse&lt;T&gt;.NextLink</c> instead.</remarks>
	public Uri? NextPageLink { get; set; }

	/// <summary>
	/// Gets or sets the total count of entities.
	/// </summary>
	/// <remarks>Use <c>ODataResponse&lt;T&gt;.Count</c> instead.</remarks>
	public long? Count { get; set; }

	/// <summary>
	/// Gets or sets the delta link for change tracking.
	/// </summary>
	/// <remarks>Use <c>ODataResponse&lt;T&gt;.DeltaLink</c> instead.</remarks>
	public Uri? DeltaLink { get; set; }
}
