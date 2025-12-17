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
	/// Gets whether any operations in the batch failed.
	/// </summary>
	public bool HasErrors => Results.Any(r => !r.IsSuccess);

	/// <summary>
	/// Gets the results that failed.
	/// </summary>
	public IEnumerable<ODataBatchOperationResult> FailedResults => Results.Where(r => !r.IsSuccess);

	/// <summary>
	/// Gets the result at the specified index.
	/// </summary>
	/// <param name="index">The zero-based index of the operation.</param>
	/// <returns>The operation result.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when index is out of range.</exception>
	public ODataBatchOperationResult this[int index] => Results[index];

	/// <summary>
	/// Gets the typed result at the specified index.
	/// </summary>
	/// <typeparam name="T">The expected result type.</typeparam>
	/// <param name="index">The zero-based index of the operation.</param>
	/// <returns>The typed result.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when index is out of range.</exception>
	/// <exception cref="InvalidOperationException">Thrown when the result is not of the expected type.</exception>
	public T GetResult<T>(int index)
	{
		var result = Results[index];
		if (result.Result is T typed)
		{
			return typed;
		}

		if (result.Result is null)
		{
			throw new InvalidOperationException($"Operation at index {index} has no result. Status: {result.StatusCode}");
		}

		throw new InvalidOperationException($"Operation at index {index} result is {result.Result.GetType().Name}, expected {typeof(T).Name}");
	}

	/// <summary>
	/// Tries to get the typed result at the specified index.
	/// </summary>
	/// <typeparam name="T">The expected result type.</typeparam>
	/// <param name="index">The zero-based index of the operation.</param>
	/// <param name="result">The typed result if successful.</param>
	/// <returns>True if the result was successfully retrieved; otherwise, false.</returns>
	public bool TryGetResult<T>(int index, out T? result)
	{
		result = default;

		if (index < 0 || index >= Results.Count)
		{
			return false;
		}

		if (Results[index].Result is T typed)
		{
			result = typed;
			return true;
		}

		return false;
	}

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
