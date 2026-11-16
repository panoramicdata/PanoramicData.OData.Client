namespace PanoramicData.OData.Client;

/// <summary>
/// Builds operations within a changeset.
/// </summary>
public class ODataChangesetBuilder
{
	private readonly ODataChangeset _changeset;

	internal ODataChangesetBuilder(ODataChangeset changeset)
	{
		_changeset = changeset;
	}

	/// <summary>
	/// Adds a POST operation to create an entity.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="entitySet">The entity set name.</param>
	/// <param name="entity">The entity to create.</param>
	/// <returns>This builder for method chaining.</returns>
	public ODataChangesetBuilder Create<T>(string entitySet, T entity) where T : class
	{
		_changeset.Operations.Add(ODataBatchOperation.ForCreate(entitySet, entity));
		return this;
	}

	/// <summary>
	/// Adds a PATCH operation to update an entity.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <typeparam name="TKey">The key type.</typeparam>
	/// <param name="entitySet">The entity set name.</param>
	/// <param name="key">The entity key.</param>
	/// <param name="patchValues">The values to update.</param>
	/// <param name="etag">Optional ETag for concurrency control.</param>
	/// <returns>This builder for method chaining.</returns>
	public ODataChangesetBuilder Update<T, TKey>(string entitySet, TKey key, object patchValues, [Optional] string? etag) where T : class
	{
		_changeset.Operations.Add(ODataBatchOperation.ForUpdate<T>(entitySet, key, patchValues, etag));
		return this;
	}

	/// <summary>
	/// Adds a PATCH operation to update an entity.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="entitySet">The entity set name.</param>
	/// <param name="key">The entity key.</param>
	/// <param name="patchValues">The values to update.</param>
	/// <param name="etag">Optional ETag for concurrency control.</param>
	/// <returns>This builder for method chaining.</returns>
	public ODataChangesetBuilder Update<T>(string entitySet, object key, object patchValues, [Optional] string? etag) where T : class
		=> Update<T, object>(entitySet, key, patchValues, etag);

	/// <summary>
	/// Adds a DELETE operation to remove an entity.
	/// </summary>
	/// <typeparam name="TKey">The key type.</typeparam>
	/// <param name="entitySet">The entity set name.</param>
	/// <param name="key">The entity key.</param>
	/// <param name="etag">Optional ETag for concurrency control.</param>
	/// <returns>This builder for method chaining.</returns>
	public ODataChangesetBuilder Delete<TKey>(string entitySet, TKey key, [Optional] string? etag)
	{
		_changeset.Operations.Add(ODataBatchOperation.ForDelete(entitySet, key, etag));
		return this;
	}

	/// <summary>
	/// Adds a DELETE operation to remove an entity.
	/// </summary>
	/// <param name="entitySet">The entity set name.</param>
	/// <param name="key">The entity key.</param>
	/// <param name="etag">Optional ETag for concurrency control.</param>
	/// <returns>This builder for method chaining.</returns>
	public ODataChangesetBuilder Delete(string entitySet, object key, [Optional] string? etag)
		=> Delete<object>(entitySet, key, etag);
}
