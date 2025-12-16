using System.Globalization;
using System.Text.Json;

namespace PanoramicData.OData.Client;

/// <summary>
/// Builds and executes OData batch requests.
/// </summary>
public class ODataBatchBuilder
{
	private readonly ODataClient _client;
	private readonly List<object> _items = []; // Can be ODataBatchOperation or ODataChangeset
	private readonly JsonSerializerOptions _jsonOptions;

	internal ODataBatchBuilder(ODataClient client, JsonSerializerOptions jsonOptions)
	{
		_client = client;
		_jsonOptions = jsonOptions;
	}

	/// <summary>
	/// Adds a GET operation to retrieve an entity by key.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <typeparam name="TKey">The key type.</typeparam>
	/// <param name="entitySet">The entity set name.</param>
	/// <param name="key">The entity key.</param>
	/// <returns>The operation ID for retrieving the result.</returns>
	public string Get<T, TKey>(string entitySet, TKey key)
	{
		var operation = new ODataBatchOperation
		{
			OperationType = ODataBatchOperationType.Get,
			Url = $"{entitySet}({FormatKey(key)})",
			ResultType = typeof(T)
		};
		_items.Add(operation);
		return operation.Id;
	}

	/// <summary>
	/// Adds a GET operation to retrieve an entity by key.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="entitySet">The entity set name.</param>
	/// <param name="key">The entity key.</param>
	/// <returns>The operation ID for retrieving the result.</returns>
	public string Get<T>(string entitySet, object key)
		=> Get<T, object>(entitySet, key);

	/// <summary>
	/// Adds a POST operation to create an entity.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="entitySet">The entity set name.</param>
	/// <param name="entity">The entity to create.</param>
	/// <returns>The operation ID for retrieving the result.</returns>
	public string Create<T>(string entitySet, T entity) where T : class
	{
		var operation = new ODataBatchOperation
		{
			OperationType = ODataBatchOperationType.Create,
			Url = entitySet,
			Body = entity,
			ResultType = typeof(T)
		};
		_items.Add(operation);
		return operation.Id;
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
	/// <returns>The operation ID for retrieving the result.</returns>
	public string Update<T, TKey>(string entitySet, TKey key, object patchValues, string? etag = null) where T : class
	{
		var operation = new ODataBatchOperation
		{
			OperationType = ODataBatchOperationType.Update,
			Url = $"{entitySet}({FormatKey(key)})",
			Body = patchValues,
			ResultType = typeof(T),
			ETag = etag
		};
		_items.Add(operation);
		return operation.Id;
	}

	/// <summary>
	/// Adds a PATCH operation to update an entity.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="entitySet">The entity set name.</param>
	/// <param name="key">The entity key.</param>
	/// <param name="patchValues">The values to update.</param>
	/// <param name="etag">Optional ETag for concurrency control.</param>
	/// <returns>The operation ID for retrieving the result.</returns>
	public string Update<T>(string entitySet, object key, object patchValues, string? etag = null) where T : class
		=> Update<T, object>(entitySet, key, patchValues, etag);

	/// <summary>
	/// Adds a DELETE operation to remove an entity.
	/// </summary>
	/// <typeparam name="TKey">The key type.</typeparam>
	/// <param name="entitySet">The entity set name.</param>
	/// <param name="key">The entity key.</param>
	/// <param name="etag">Optional ETag for concurrency control.</param>
	/// <returns>The operation ID for retrieving the result.</returns>
	public string Delete<TKey>(string entitySet, TKey key, string? etag = null)
	{
		var operation = new ODataBatchOperation
		{
			OperationType = ODataBatchOperationType.Delete,
			Url = $"{entitySet}({FormatKey(key)})",
			ETag = etag
		};
		_items.Add(operation);
		return operation.Id;
	}

	/// <summary>
	/// Adds a DELETE operation to remove an entity.
	/// </summary>
	/// <param name="entitySet">The entity set name.</param>
	/// <param name="key">The entity key.</param>
	/// <param name="etag">Optional ETag for concurrency control.</param>
	/// <returns>The operation ID for retrieving the result.</returns>
	public string Delete(string entitySet, object key, string? etag = null)
		=> Delete<object>(entitySet, key, etag);

	/// <summary>
	/// Creates a new changeset for atomic operations.
	/// All operations within a changeset either all succeed or all fail.
	/// </summary>
	/// <returns>A changeset builder.</returns>
	public ODataChangesetBuilder CreateChangeset()
	{
		var changeset = new ODataChangeset();
		_items.Add(changeset);
		return new ODataChangesetBuilder(changeset, _jsonOptions);
	}

	/// <summary>
	/// Gets all operations in this batch (flattened, including from changesets).
	/// </summary>
	public IEnumerable<ODataBatchOperation> GetAllOperations()
	{
		foreach (var item in _items)
		{
			if (item is ODataBatchOperation op)
			{
				yield return op;
			}
			else if (item is ODataChangeset changeset)
			{
				foreach (var csOp in changeset.Operations)
				{
					yield return csOp;
				}
			}
		}
	}

	/// <summary>
	/// Gets the items in this batch (operations and changesets).
	/// </summary>
	public IReadOnlyList<object> Items => _items;

	/// <summary>
	/// Executes the batch request and returns the results.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The batch response containing all operation results.</returns>
	public Task<ODataBatchResponse> ExecuteAsync(CancellationToken cancellationToken = default)
		=> _client.ExecuteBatchAsync(this, cancellationToken);

	private static string FormatKey<TKey>(TKey key) => key switch
	{
		int i => i.ToString(CultureInfo.InvariantCulture),
		long l => l.ToString(CultureInfo.InvariantCulture),
		Guid g => g.ToString(),
		string s => $"'{s.Replace("'", "''")}'",
		_ => key?.ToString() ?? throw new ArgumentException("Invalid key value")
	};
}
