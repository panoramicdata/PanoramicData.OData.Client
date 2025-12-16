namespace PanoramicData.OData.Client.Exceptions;

/// <summary>
/// Exception thrown when an async OData operation fails.
/// </summary>
/// <remarks>
/// Creates a new async operation exception.
/// </remarks>
public class ODataAsyncOperationException(string message, string monitorUrl, string? errorDetails) : Exception(message)
{

	/// <summary>
	/// Gets the monitor URL for the failed operation.
	/// </summary>
	public string MonitorUrl { get; } = monitorUrl;

	/// <summary>
	/// Gets the error details from the server.
	/// </summary>
	public string? ErrorDetails { get; } = errorDetails;
}
