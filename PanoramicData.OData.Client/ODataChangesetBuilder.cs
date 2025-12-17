using System.Globalization;
using System.Text.Json;

namespace PanoramicData.OData.Client;

/// <summary>
/// Builds operations within a changeset.
/// </summary>
public class ODataChangesetBuilder
{
	private readonly ODataChangeset _changeset;
	private readonly JsonSerializerOptions _jsonOptions;

	internal ODataChangesetBuilder(ODataChangeset changeset, JsonSerializerOptions jsonOptions)
	{
		_changeset = changeset;
		_jsonOptions = jsonOptions;
		_ = _jsonOptions; // Reserved for future use
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
		var operation = new ODataBatchOperation
		{
			OperationType = ODataBatchOperationType.Create,
			Url = entitySet,
			Body = entity,
			ResultType = typeof(T)
		};
		_changeset.Operations.Add(operation);
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
	public ODataChangesetBuilder Update<T, TKey>(string entitySet, TKey key, object patchValues, string? etag = null) where T : class
	{
		var operation = new ODataBatchOperation
		{
			OperationType = ODataBatchOperationType.Update,
			Url = $"{entitySet}({FormatKey(key)})",
			Body = patchValues,
			ResultType = typeof(T),
			ETag = etag
		};
		_changeset.Operations.Add(operation);
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
	public ODataChangesetBuilder Update<T>(string entitySet, object key, object patchValues, string? etag = null) where T : class
		=> Update<T, object>(entitySet, key, patchValues, etag);

	/// <summary>
	/// Adds a DELETE operation to remove an entity.
	/// </summary>
	/// <typeparam name="TKey">The key type.</typeparam>
	/// <param name="entitySet">The entity set name.</param>
	/// <param name="key">The entity key.</param>
	/// <param name="etag">Optional ETag for concurrency control.</param>
	/// <returns>This builder for method chaining.</returns>
	public ODataChangesetBuilder Delete<TKey>(string entitySet, TKey key, string? etag = null)
	{
		var operation = new ODataBatchOperation
		{
			OperationType = ODataBatchOperationType.Delete,
			Url = $"{entitySet}({FormatKey(key)})",
			ETag = etag
		};
		_changeset.Operations.Add(operation);
		return this;
	}

	/// <summary>
	/// Adds a DELETE operation to remove an entity.
	/// </summary>
	/// <param name="entitySet">The entity set name.</param>
	/// <param name="key">The entity key.</param>
	/// <param name="etag">Optional ETag for concurrency control.</param>
	/// <returns>This builder for method chaining.</returns>
	public ODataChangesetBuilder Delete(string entitySet, object key, string? etag = null)
		=> Delete<object>(entitySet, key, etag);

	private static string FormatKey<TKey>(TKey key) => key switch
	{
		int i => i.ToString(CultureInfo.InvariantCulture),
		long l => l.ToString(CultureInfo.InvariantCulture),
		Guid g => g.ToString(),
		string s => $"'{s.Replace("'", "''")}'",
		_ => key?.ToString() ?? throw new ArgumentException("Invalid key value")
	};
}
