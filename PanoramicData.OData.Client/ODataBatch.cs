namespace PanoramicData.OData.Client;

/// <summary>
/// Represents a single operation within a batch request.
/// </summary>
public class ODataBatchOperation
{
	/// <summary>
	/// Gets or sets the unique identifier for this operation within the batch.
	/// </summary>
	public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];

	/// <summary>
	/// Gets or sets the type of operation.
	/// </summary>
	public ODataBatchOperationType OperationType { get; set; }

	/// <summary>
	/// Gets or sets the relative URL for the operation.
	/// </summary>
	public string Url { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the request body for POST/PATCH operations.
	/// </summary>
	public object? Body { get; set; }

	/// <summary>
	/// Gets or sets custom headers for this operation.
	/// </summary>
	public Dictionary<string, string> Headers { get; set; } = [];

	/// <summary>
	/// Gets or sets the expected result type for deserialization.
	/// </summary>
	public Type? ResultType { get; set; }

	/// <summary>
	/// Gets or sets the ETag for concurrency control (Update/Delete).
	/// </summary>
	public string? ETag { get; set; }
	internal static ODataBatchOperation ForGet<T>(string entitySet, object? key) => new()
	{
		OperationType = ODataBatchOperationType.Get,
		Url = EntityUrl(entitySet, key),
		ResultType = typeof(T)
	};

	internal static ODataBatchOperation ForCreate<T>(string entitySet, T entity) => new()
	{
		OperationType = ODataBatchOperationType.Create,
		Url = entitySet,
		Body = entity,
		ResultType = typeof(T)
	};

	internal static ODataBatchOperation ForUpdate<T>(string entitySet, object? key, object patchValues, string? etag) => new()
	{
		OperationType = ODataBatchOperationType.Update,
		Url = EntityUrl(entitySet, key),
		Body = patchValues,
		ResultType = typeof(T),
		ETag = etag
	};

	internal static ODataBatchOperation ForDelete(string entitySet, object? key, string? etag) => new()
	{
		OperationType = ODataBatchOperationType.Delete,
		Url = EntityUrl(entitySet, key),
		ETag = etag
	};

	private static string EntityUrl(string entitySet, object? key)
		=> $"{entitySet}({ODataLiteral.FormatKey(key)})";
}
