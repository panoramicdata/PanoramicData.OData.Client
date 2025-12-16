using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Json;
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
	private readonly string _monitorUrl;
	private readonly JsonSerializerOptions _jsonOptions;
	private readonly ILogger _logger;
	private readonly TimeSpan _pollInterval;

	private ODataAsyncOperationStatus _status = ODataAsyncOperationStatus.Pending;
	private T? _result;
	private string? _errorMessage;

	internal ODataAsyncOperation(
		HttpClient httpClient,
		string monitorUrl,
		JsonSerializerOptions jsonOptions,
		ILogger logger,
		TimeSpan? pollInterval = null)
	{
		_httpClient = httpClient;
		_monitorUrl = monitorUrl;
		_jsonOptions = jsonOptions;
		_logger = logger;
		_pollInterval = pollInterval ?? TimeSpan.FromSeconds(5);
	}

	/// <summary>
	/// Gets the URL used to monitor the operation status.
	/// </summary>
	public string MonitorUrl => _monitorUrl;

	/// <summary>
	/// Gets the current status of the operation.
	/// </summary>
	public ODataAsyncOperationStatus Status => _status;

	/// <summary>
	/// Gets the result of the operation once completed.
	/// </summary>
	public T? Result => _result;

	/// <summary>
	/// Gets the error message if the operation failed.
	/// </summary>
	public string? ErrorMessage => _errorMessage;

	/// <summary>
	/// Gets whether the operation has completed (successfully or with error).
	/// </summary>
	public bool IsCompleted => _status is ODataAsyncOperationStatus.Completed or ODataAsyncOperationStatus.Failed;

	/// <summary>
	/// Polls the operation status once and updates the status.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>True if the operation is still running, false if completed or failed.</returns>
	public async Task<bool> PollAsync(CancellationToken cancellationToken = default)
	{
		if (IsCompleted)
		{
			return false;
		}

		_logger.LogDebug("ODataAsyncOperation - Polling status at {Url}", _monitorUrl);

		var request = new HttpRequestMessage(HttpMethod.Get, _monitorUrl);
		var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

		_logger.LogDebug("ODataAsyncOperation - Poll response: {StatusCode}", response.StatusCode);

		if (response.StatusCode == HttpStatusCode.Accepted)
		{
			// Still running
			_status = ODataAsyncOperationStatus.Running;

			// Check for updated location
			if (response.Headers.Location is not null)
			{
				_logger.LogDebug("ODataAsyncOperation - Updated monitor URL: {Url}", response.Headers.Location);
			}

			return true;
		}

		if (response.IsSuccessStatusCode)
		{
			// Completed successfully
			_status = ODataAsyncOperationStatus.Completed;
			var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

			if (!string.IsNullOrEmpty(content))
			{
				try
				{
					_result = JsonSerializer.Deserialize<T>(content, _jsonOptions);
				}
				catch (JsonException ex)
				{
					_logger.LogWarning(ex, "ODataAsyncOperation - Failed to deserialize result");
				}
			}

			_logger.LogDebug("ODataAsyncOperation - Operation completed successfully");
			return false;
		}

		// Failed
		_status = ODataAsyncOperationStatus.Failed;
		_errorMessage = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
		_logger.LogWarning("ODataAsyncOperation - Operation failed: {Error}", _errorMessage);
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

		_logger.LogDebug("ODataAsyncOperation - Waiting for completion, timeout: {Timeout}", timeout?.ToString() ?? "indefinite");

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

		if (_status == ODataAsyncOperationStatus.Failed)
		{
			throw new ODataAsyncOperationException("Async operation failed", _monitorUrl, _errorMessage);
		}

		return _result;
	}

	/// <summary>
	/// Attempts to cancel the async operation.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>True if the cancellation request was accepted.</returns>
	public async Task<bool> TryCancelAsync(CancellationToken cancellationToken = default)
	{
		if (IsCompleted)
		{
			return false;
		}

		_logger.LogDebug("ODataAsyncOperation - Attempting to cancel at {Url}", _monitorUrl);

		var request = new HttpRequestMessage(HttpMethod.Delete, _monitorUrl);
		var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

		if (response.IsSuccessStatusCode)
		{
			_status = ODataAsyncOperationStatus.Cancelled;
			_logger.LogDebug("ODataAsyncOperation - Cancellation accepted");
			return true;
		}

		_logger.LogWarning("ODataAsyncOperation - Cancellation not accepted: {StatusCode}", response.StatusCode);
		return false;
	}
}

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

/// <summary>
/// Exception thrown when an async OData operation fails.
/// </summary>
public class ODataAsyncOperationException : Exception
{
	/// <summary>
	/// Creates a new async operation exception.
	/// </summary>
	public ODataAsyncOperationException(string message, string monitorUrl, string? errorDetails)
		: base(message)
	{
		MonitorUrl = monitorUrl;
		ErrorDetails = errorDetails;
	}

	/// <summary>
	/// Gets the monitor URL for the failed operation.
	/// </summary>
	public string MonitorUrl { get; }

	/// <summary>
	/// Gets the error details from the server.
	/// </summary>
	public string? ErrorDetails { get; }
}
