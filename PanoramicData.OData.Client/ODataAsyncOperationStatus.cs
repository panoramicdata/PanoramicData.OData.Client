namespace PanoramicData.OData.Client;

/// <summary>
/// Status of an async OData operation.
/// </summary>
public enum ODataAsyncOperationStatus
{
	/// <summary>
	/// The operation has been submitted but not yet started.
	/// </summary>
	Pending,

	/// <summary>
	/// The operation is currently running.
	/// </summary>
	Running,

	/// <summary>
	/// The operation completed successfully.
	/// </summary>
	Completed,

	/// <summary>
	/// The operation failed.
	/// </summary>
	Failed,

	/// <summary>
	/// The operation was cancelled.
	/// </summary>
	Cancelled
}
