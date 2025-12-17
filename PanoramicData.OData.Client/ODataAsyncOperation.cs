using Microsoft.Extensions.Logging;
using PanoramicData.OData.Client.Exceptions;
using System.Net;
using System.Text.Json;

namespace PanoramicData.OData.Client;

/// <summary>
/// Represents an OData async operation that can be monitored for completion.
/// OData V4 supports long-running operations via the Respond-Async preference.
/// </summary>
/// <typeparam name="T">The expected result type.</typeparam>
public class ODataAsyncOperation<T>
{
	private readonly HttpClient _httpClient;
	private readonly JsonSerializerOptions _jsonOptions;
	private readonly ILogger _logger;
	private readonly TimeSpan _pollInterval;

	internal ODataAsyncOperation(
		HttpClient httpClient,
		string monitorUrl,
		JsonSerializerOptions jsonOptions,
		ILogger logger,
		TimeSpan? pollInterval = null)
	{
		_httpClient = httpClient;
		MonitorUrl = monitorUrl;
		_jsonOptions = jsonOptions;
		_logger = logger;
		_pollInterval = pollInterval ?? TimeSpan.FromSeconds(5);
	}

	/// <summary>
	/// Gets the URL used to monitor the operation status.
	/// </summary>
	public string MonitorUrl { get; }

	/// <summary>
	/// Gets the current status of the operation.
	/// </summary>
	public ODataAsyncOperationStatus Status { get; private set; } = ODataAsyncOperationStatus.Pending;

	/// <summary>
	/// Gets the result of the operation once completed.
	/// </summary>
	public T? Result { get; private set; }

	/// <summary>
	/// Gets the error message if the operation failed.
	/// </summary>
	public string? ErrorMessage { get; private set; }

	/// <summary>
	/// Gets whether the operation has completed (successfully or with error).
	/// </summary>
	public bool IsCompleted => Status is ODataAsyncOperationStatus.Completed or ODataAsyncOperationStatus.Failed;

	/// <summary>
	/// Polls the operation status once and updates the status.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>True if the operation is still running, false if completed or failed.</returns>
	public async Task<bool> PollAsync(CancellationToken cancellationToken)
	{
		if (IsCompleted)
		{
			return false;
		}

		LoggerMessages.AsyncOperationPolling(_logger, MonitorUrl);

		var request = new HttpRequestMessage(HttpMethod.Get, MonitorUrl);
		var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

		LoggerMessages.AsyncOperationPollResponse(_logger, response.StatusCode);

		if (response.StatusCode == HttpStatusCode.Accepted)
		{
			// Still running
			Status = ODataAsyncOperationStatus.Running;

			// Check for updated location
			if (response.Headers.Location is not null)
			{
				LoggerMessages.AsyncOperationUpdatedUrl(_logger, response.Headers.Location);
			}

			return true;
		}

		if (response.IsSuccessStatusCode)
		{
			// Completed successfully
			Status = ODataAsyncOperationStatus.Completed;
			var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

			if (!string.IsNullOrEmpty(content))
			{
				try
				{
					Result = JsonSerializer.Deserialize<T>(content, _jsonOptions);
				}
				catch (JsonException ex)
				{
					LoggerMessages.AsyncOperationDeserializeFailed(_logger, ex);
				}
			}

			LoggerMessages.AsyncOperationCompleted(_logger);
			return false;
		}

		// Failed
		Status = ODataAsyncOperationStatus.Failed;
		ErrorMessage = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
		LoggerMessages.AsyncOperationFailed(_logger, ErrorMessage);
		return false;
	}

	/// <summary>
	/// Waits for the operation to complete, polling periodically.
	/// </summary>
	/// <param name="timeout">Maximum time to wait. If null, waits indefinitely.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The operation result.</returns>
	/// <exception cref="TimeoutException">Thrown if the operation doesn't complete within the timeout.</exception>
	/// <exception cref="ODataAsyncOperationException">Thrown if the operation fails.</exception>
	public async Task<T?> WaitForCompletionAsync(
		TimeSpan? timeout = null,
		CancellationToken cancellationToken = default)
	{
		var startTime = DateTime.UtcNow;

		LoggerMessages.AsyncOperationWaiting(_logger, timeout?.ToString() ?? "indefinite");

		while (!IsCompleted)
		{
			cancellationToken.ThrowIfCancellationRequested();

			if (timeout.HasValue && DateTime.UtcNow - startTime > timeout.Value)
			{
				throw new TimeoutException($"Async operation did not complete within {timeout.Value}");
			}

			await PollAsync(cancellationToken).ConfigureAwait(false);

			if (!IsCompleted)
			{
				await Task.Delay(_pollInterval, cancellationToken).ConfigureAwait(false);
			}
		}

		if (Status == ODataAsyncOperationStatus.Failed)
		{
			throw new ODataAsyncOperationException("Async operation failed", MonitorUrl, ErrorMessage);
		}

		return Result;
	}

	/// <summary>
	/// Attempts to cancel the async operation.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>True if the cancellation request was accepted.</returns>
	public async Task<bool> TryCancelAsync(CancellationToken cancellationToken)
	{
		if (IsCompleted)
		{
			return false;
		}

		LoggerMessages.AsyncOperationCancelling(_logger, MonitorUrl);

		var request = new HttpRequestMessage(HttpMethod.Delete, MonitorUrl);
		var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

		if (response.IsSuccessStatusCode)
		{
			Status = ODataAsyncOperationStatus.Cancelled;
			LoggerMessages.AsyncOperationCancelled(_logger);
			return true;
		}

		LoggerMessages.AsyncOperationCancelNotAccepted(_logger, response.StatusCode);
		return false;
	}
}
