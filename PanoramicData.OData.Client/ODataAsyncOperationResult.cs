namespace PanoramicData.OData.Client;

/// <summary>
/// Result of an operation that may have been processed asynchronously.
/// </summary>
/// <typeparam name="T">The result type.</typeparam>
public class ODataAsyncOperationResult<T>
{
	/// <summary>
	/// Gets whether the operation is being processed asynchronously.
	/// If true, use <see cref="AsyncOperation"/> to monitor and retrieve the result.
	/// If false, the result is available in <see cref="SynchronousResult"/>.
	/// </summary>
	public bool IsAsync { get; init; }

	/// <summary>
	/// Gets the async operation handle for monitoring. Only set when <see cref="IsAsync"/> is true.
	/// </summary>
	public ODataAsyncOperation<T>? AsyncOperation { get; init; }

	/// <summary>
	/// Gets the synchronous result. Only set when <see cref="IsAsync"/> is false.
	/// </summary>
	public T? SynchronousResult { get; init; }

	/// <summary>
	/// Gets the result, waiting for async completion if necessary.
	/// </summary>
	/// <param name="timeout">Maximum time to wait for async completion.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The result.</returns>
	public async Task<T?> GetResultAsync(
		TimeSpan? timeout = null,
		CancellationToken cancellationToken = default)
	{
		if (!IsAsync)
		{
			return SynchronousResult;
		}

		return await AsyncOperation!.WaitForCompletionAsync(timeout, cancellationToken).ConfigureAwait(false);
	}
}
