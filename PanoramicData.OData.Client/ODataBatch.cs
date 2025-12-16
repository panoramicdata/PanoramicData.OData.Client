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
}
