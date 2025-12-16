using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Json;

namespace PanoramicData.OData.Client;

/// <summary>
/// ODataClient - Async long-running operations.
/// </summary>
public partial class ODataClient
{
	/// <summary>
	/// Calls an OData action with async preference, returning an async operation handle.
	/// The server may return 202 Accepted with a monitor URL, or complete synchronously.
	/// </summary>
	/// <typeparam name="TResult">The expected result type.</typeparam>
	/// <param name="actionUrl">The action URL.</param>
	/// <param name="parameters">Optional action parameters.</param>
	/// <param name="pollInterval">Interval between status polls. Default is 5 seconds.</param>
	/// <param name="headers">Optional additional headers.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>An async operation handle that can be used to monitor and retrieve the result.</returns>
	public async Task<ODataAsyncOperationResult<TResult>> CallActionAsyncWithPreferAsync<TResult>(
		string actionUrl,
		object? parameters = null,
		TimeSpan? pollInterval = null,
		IReadOnlyDictionary<string, string>? headers = null,
		CancellationToken cancellationToken = default)
	{
		_logger.LogDebug("CallActionAsyncWithPreferAsync<{ResultType}> - URL: {Url}", typeof(TResult).Name, actionUrl);

		var request = CreateRequest(HttpMethod.Post, actionUrl, headers);

		// Add Respond-Async preference
		request.Headers.TryAddWithoutValidation("Prefer", "respond-async");

		if (parameters is not null)
		{
			request.Content = JsonContent.Create(parameters, options: _actionJsonOptions);
		}

		var response = await SendWithRetryAsync(request, cancellationToken).ConfigureAwait(false);

		// Check if server accepted async processing
		if (response.StatusCode == HttpStatusCode.Accepted)
		{
			var monitorUrl = response.Headers.Location?.ToString();

			if (string.IsNullOrEmpty(monitorUrl))
			{
				_logger.LogWarning("CallActionAsyncWithPreferAsync - 202 Accepted but no Location header");
				throw new InvalidOperationException("Server accepted async request but did not provide a monitor URL");
			}

			_logger.LogDebug("CallActionAsyncWithPreferAsync - Async operation started, monitor URL: {Url}", monitorUrl);

			var asyncOp = new ODataAsyncOperation<TResult>(
				_httpClient,
				monitorUrl,
				_jsonOptions,
				_logger,
				pollInterval);

			return new ODataAsyncOperationResult<TResult>
			{
				IsAsync = true,
				AsyncOperation = asyncOp
			};
		}

		// Server completed synchronously
		await EnsureSuccessAsync(response, actionUrl, cancellationToken).ConfigureAwait(false);

		TResult? result = default;
		if (response.StatusCode != HttpStatusCode.NoContent)
		{
			result = await response.Content.ReadFromJsonAsync<TResult>(_jsonOptions, cancellationToken).ConfigureAwait(false);
		}

		_logger.LogDebug("CallActionAsyncWithPreferAsync - Completed synchronously");

		return new ODataAsyncOperationResult<TResult>
		{
			IsAsync = false,
			SynchronousResult = result
		};
	}

	/// <summary>
	/// Calls an OData action and waits for completion, using async preference if the server supports it.
	/// </summary>
	/// <typeparam name="TResult">The expected result type.</typeparam>
	/// <param name="actionUrl">The action URL.</param>
	/// <param name="parameters">Optional action parameters.</param>
	/// <param name="timeout">Maximum time to wait for completion.</param>
	/// <param name="pollInterval">Interval between status polls. Default is 5 seconds.</param>
	/// <param name="headers">Optional additional headers.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The action result.</returns>
	public async Task<TResult?> CallActionAndWaitAsync<TResult>(
		string actionUrl,
		object? parameters = null,
		TimeSpan? timeout = null,
		TimeSpan? pollInterval = null,
		IReadOnlyDictionary<string, string>? headers = null,
		CancellationToken cancellationToken = default)
	{
		var result = await CallActionAsyncWithPreferAsync<TResult>(
			actionUrl,
			parameters,
			pollInterval,
			headers,
			cancellationToken).ConfigureAwait(false);

		if (!result.IsAsync)
		{
			return result.SynchronousResult;
		}

		return await result.AsyncOperation!.WaitForCompletionAsync(timeout, cancellationToken).ConfigureAwait(false);
	}

	/// <summary>
	/// Creates a batch request with async preference.
	/// If the server supports async batch processing, returns an async operation handle.
	/// </summary>
	/// <param name="batch">The batch builder containing operations to execute.</param>
	/// <param name="pollInterval">Interval between status polls. Default is 5 seconds.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>An async operation result containing either the batch response or an async handle.</returns>
	public async Task<ODataAsyncOperationResult<ODataBatchResponse>> ExecuteBatchAsyncWithPreferAsync(
		ODataBatchBuilder batch,
		TimeSpan? pollInterval = null,
		CancellationToken cancellationToken = default)
	{
		var batchBoundary = $"batch_{Guid.NewGuid():N}";

		_logger.LogDebug("ExecuteBatchAsyncWithPreferAsync - Executing batch with {Count} items", batch.Items.Count);

		var content = BuildBatchContent(batch, batchBoundary);

		var request = new HttpRequestMessage(HttpMethod.Post, "$batch")
		{
			Content = content
		};

		// Add Respond-Async preference
		request.Headers.TryAddWithoutValidation("Prefer", "respond-async");

		_options.ConfigureRequest?.Invoke(request);

		var response = await SendWithRetryAsync(request, cancellationToken).ConfigureAwait(false);

		// Check if server accepted async processing
		if (response.StatusCode == HttpStatusCode.Accepted)
		{
			var monitorUrl = response.Headers.Location?.ToString();

			if (string.IsNullOrEmpty(monitorUrl))
			{
				throw new InvalidOperationException("Server accepted async request but did not provide a monitor URL");
			}

			_logger.LogDebug("ExecuteBatchAsyncWithPreferAsync - Async operation started, monitor URL: {Url}", monitorUrl);

			var asyncOp = new ODataAsyncOperation<ODataBatchResponse>(
				_httpClient,
				monitorUrl,
				_jsonOptions,
				_logger,
				pollInterval);

			return new ODataAsyncOperationResult<ODataBatchResponse>
			{
				IsAsync = true,
				AsyncOperation = asyncOp
			};
		}

		// Server completed synchronously
		await EnsureSuccessAsync(response, "$batch", cancellationToken).ConfigureAwait(false);

		var responseContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
		var contentType = response.Content.Headers.ContentType?.ToString() ?? "";

		var batchResponse = ParseBatchResponse(responseContent, contentType, batch);

		return new ODataAsyncOperationResult<ODataBatchResponse>
		{
			IsAsync = false,
			SynchronousResult = batchResponse
		};
	}
}

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
