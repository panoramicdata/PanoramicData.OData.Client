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
		LoggerMessages.CallActionAsyncWithPreferAsync(_logger, typeof(TResult).Name, actionUrl);

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
				LoggerMessages.CallActionAsyncNoLocationHeader(_logger);
				throw new InvalidOperationException("Server accepted async request but did not provide a monitor URL");
			}

			LoggerMessages.CallActionAsyncMonitorUrl(_logger, monitorUrl);

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

		LoggerMessages.CallActionAsyncCompletedSync(_logger);

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

		LoggerMessages.ExecuteBatchAsyncWithPreferAsync(_logger, batch.Items.Count);

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

			LoggerMessages.ExecuteBatchAsyncMonitorUrl(_logger, monitorUrl);

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
