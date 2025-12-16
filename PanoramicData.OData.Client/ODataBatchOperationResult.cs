namespace PanoramicData.OData.Client;

/// <summary>
/// Represents the result of a single operation within a batch response.
/// </summary>
public class ODataBatchOperationResult
{
	/// <summary>
	/// Gets or sets the operation ID this result corresponds to.
	/// </summary>
	public string OperationId { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the HTTP status code of the operation.
	/// </summary>
	public int StatusCode { get; set; }

	/// <summary>
	/// Gets or sets whether the operation was successful.
	/// </summary>
	public bool IsSuccess => StatusCode >= 200 && StatusCode < 300;

	/// <summary>
	/// Gets or sets the response body as a string.
	/// </summary>
	public string? ResponseBody { get; set; }

	/// <summary>
	/// Gets or sets the deserialized result (if applicable).
	/// </summary>
	public object? Result { get; set; }

	/// <summary>
	/// Gets or sets any error message if the operation failed.
	/// </summary>
	public string? ErrorMessage { get; set; }

	/// <summary>
	/// Gets or sets the response headers.
	/// </summary>
	public Dictionary<string, string> Headers { get; set; } = [];
}
