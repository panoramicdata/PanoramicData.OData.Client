namespace PanoramicData.OData.Client;

/// <summary>
/// Represents the response from a batch request.
/// </summary>
public class ODataBatchResponse
{
	/// <summary>
	/// Gets or sets the list of operation results.
	/// </summary>
	public List<ODataBatchOperationResult> Results { get; set; } = [];

	/// <summary>
	/// Gets whether all operations in the batch succeeded.
	/// </summary>
	public bool AllSucceeded => Results.Count > 0 && Results.All(r => r.IsSuccess);

	/// <summary>
	/// Gets the results that failed.
	/// </summary>
	public IEnumerable<ODataBatchOperationResult> FailedResults => Results.Where(r => !r.IsSuccess);

	/// <summary>
	/// Gets the result for a specific operation by ID.
	/// </summary>
	/// <param name="operationId">The operation ID.</param>
	/// <returns>The operation result, or null if not found.</returns>
	public ODataBatchOperationResult? GetResult(string operationId)
		=> Results.FirstOrDefault(r => r.OperationId == operationId);

	/// <summary>
	/// Gets the typed result for a specific operation by ID.
	/// </summary>
	/// <typeparam name="T">The expected result type.</typeparam>
	/// <param name="operationId">The operation ID.</param>
	/// <returns>The typed result, or default if not found or wrong type.</returns>
	public T? GetResult<T>(string operationId)
	{
		var result = GetResult(operationId);
		return result?.Result is T typed ? typed : default;
	}
}
