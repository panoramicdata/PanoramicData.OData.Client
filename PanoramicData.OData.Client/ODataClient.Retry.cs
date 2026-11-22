using System.Runtime.ExceptionServices;

namespace PanoramicData.OData.Client;

/// <summary>
/// ODataClient - retry handling.
/// </summary>
public partial class ODataClient
{
	private async Task<HttpResponseMessage> SendWithRetryAsync(
		HttpRequestMessage request,
		CancellationToken cancellationToken)
	{
		ThrowIfReadOnlyRefuses(request);

		var retryCount = 0;
		HttpResponseMessage? lastResponse = null;

		LoggerMessages.StartingRequest(_logger, request.RequestUri);

		while (retryCount <= _options.RetryCount)
		{
			var (ShouldReturn, Response) = await TrySendRequestAsync(request, retryCount, cancellationToken).ConfigureAwait(false);
			if (ShouldReturn)
			{
				return Response!;
			}

			lastResponse = Response;
			retryCount++;

			if (retryCount <= _options.RetryCount)
			{
				await Task.Delay(GetRetryDelay(lastResponse), cancellationToken).ConfigureAwait(false);
			}
		}

		return lastResponse ?? throw new ODataClientException("Request failed after all retries");
	}

	/// <summary>
	/// Whether a status code represents a transient failure that is worth retrying, for a request
	/// using the given method.
	/// </summary>
	/// <remarks>
	/// 408 and 429 are retryable for <em>any</em> method, because in both cases the server rejected
	/// the request <em>without processing it</em> - a 408 means it was never fully received, a 429
	/// that it was refused outright - so repeating it cannot duplicate any effect.
	///
	/// Of the 5xx statuses only 502, 503 and 504 are retried, and only for an idempotent method (issue #43). A 504 in particular
	/// means the opposite of a 408: the request reached the server, the server began work, and a
	/// proxy gave up waiting - so the work may still be running. 502 and 503 can likewise be
	/// returned after an upstream has accepted a request. Retrying a POST in that state either
	/// duplicates its effect or repeats expensive work whose first result is still on its way.
	///
	/// This is the rule nginx itself applies: it will not retry a non-idempotent request to another
	/// upstream unless explicitly configured with <c>non_idempotent</c>.
	///
	/// Every other 4xx is a genuine rejection and must not be retried. 409 in particular is a routine
	/// "already exists" outcome for callers that create-or-overwrite, and retrying it would be wrong.
	/// </remarks>
	private static bool IsRetryableStatusCode(HttpStatusCode statusCode, HttpMethod method)
		=> statusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests
			|| (IsTransientServerError(statusCode) && IsIdempotent(method));

	/// <summary>
	/// Whether a 5xx reports an upstream that may recover (502, 503, 504) rather than the server's own answer.
	/// </summary>
	/// <remarks>
	/// A 500 or 501 is the server saying what happened, and asking again gets the same answer: retrying it
	/// only keeps the caller waiting, for example behind a loading overlay, before the error it was always going to get.
	/// </remarks>
	private static bool IsTransientServerError(HttpStatusCode statusCode)
		=> statusCode is HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout;

	/// <summary>
	/// Whether repeating a request with this method is guaranteed to have the same effect as making
	/// it once, per RFC 9110 section 9.2.2.
	/// </summary>
	/// <remarks>
	/// POST and PATCH are the exceptions. DELETE is idempotent despite appearances: deleting the
	/// same resource twice leaves the same state, even though the second call may report 404.
	/// </remarks>
	private static bool IsIdempotent(HttpMethod method)
		=> method != HttpMethod.Post && method != HttpMethod.Patch;

	/// <summary>
	/// How long to wait before the next attempt, honouring a Retry-After header when the server sends one.
	/// </summary>
	/// <remarks>
	/// Retrying a 429 on a fixed delay while ignoring Retry-After amplifies the very condition that
	/// produced it, so the server's own figure wins where it gives one, bounded by
	/// <see cref="ODataClientOptions.MaximumRetryAfterDelay"/>.
	/// </remarks>
	private TimeSpan GetRetryDelay(HttpResponseMessage? response)
	{
		if (_options.MaximumRetryAfterDelay <= TimeSpan.Zero)
		{
			return _options.RetryDelay;
		}

		var serverRequestedDelay = response?.Headers.RetryAfter switch
		{
			{ Delta: { } delta } => delta,
			{ Date: { } date } => date - DateTimeOffset.UtcNow,
			_ => (TimeSpan?)null
		};

		if (serverRequestedDelay is not { } requested || requested <= TimeSpan.Zero)
		{
			return _options.RetryDelay;
		}

		return requested > _options.MaximumRetryAfterDelay ? _options.MaximumRetryAfterDelay : requested;
	}

	private async Task<(bool ShouldReturn, HttpResponseMessage? Response)> TrySendRequestAsync(
		HttpRequestMessage request,
		int retryCount,
		CancellationToken cancellationToken)
	{
		try
		{
			return await SendAttemptAsync(request, retryCount, cancellationToken).ConfigureAwait(false);
		}
		catch (HttpRequestException ex)
		{
			RethrowIfRetriesExhausted(ex, request.RequestUri, "failed with exception", retryCount);
		}
		catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
		{
			// A client-side timeout is the strongest signal there is that the server is still
			// working on the request, so repeating a non-idempotent one is the least safe retry of
			// all - it duplicates work that is still in flight (issue #43).
			if (!IsIdempotent(request.Method))
			{
				LoggerMessages.RetriesExhaustedException(_logger, ex, request.RequestUri, "timed out", 1);
				throw;
			}

			RethrowIfRetriesExhausted(ex, request.RequestUri, "timed out", retryCount);
		}

		return (false, null);
	}

	/// <summary>
	/// Sends one attempt and reports whether its outcome is final.
	/// </summary>
	private async Task<(bool ShouldReturn, HttpResponseMessage? Response)> SendAttemptAsync(
		HttpRequestMessage request,
		int retryCount,
		CancellationToken cancellationToken)
	{
		var requestToSend = retryCount == 0 ? request : await CloneRequestAsync(request).ConfigureAwait(false);

		LoggerMessages.SendingRequest(_logger, requestToSend.Method, requestToSend.RequestUri, retryCount + 1);

		// Log full request details at Trace level
		await LogRequestTraceAsync(requestToSend, cancellationToken).ConfigureAwait(false);

		var response = await _httpClient.SendAsync(requestToSend, cancellationToken).ConfigureAwait(false);

		LoggerMessages.ReceivedResponse(_logger, response.StatusCode, request.RequestUri);

		// Log full response details at Trace level
		await LogResponseTraceAsync(response, cancellationToken).ConfigureAwait(false);

		if (response.IsSuccessStatusCode || !IsRetryableStatusCode(response.StatusCode, request.Method))
		{
			return (true, response);
		}

		// Individual failed attempts are logged at the configurable RetryAttemptLogLevel
		// (Information by default); a single Warning is logged once retries are exhausted, so
		// transient failures that recover do not flood the logs.
		if (retryCount < _options.RetryCount)
		{
			LogRetryAttemptStatus(request.RequestUri, response.StatusCode, retryCount + 1);
		}
		else
		{
			LoggerMessages.RetriesExhaustedStatus(_logger, request.RequestUri, response.StatusCode, _options.RetryCount + 1);
		}

		return (false, response);
	}

	/// <summary>
	/// Rethrows a failed attempt's exception once no retries remain; otherwise logs the attempt
	/// and returns so the caller can try again.
	/// </summary>
	private void RethrowIfRetriesExhausted(Exception ex, Uri? url, string reason, int retryCount)
	{
		if (retryCount >= _options.RetryCount)
		{
			LoggerMessages.RetriesExhaustedException(_logger, ex, url, reason, _options.RetryCount + 1);

			// Capture/Throw rather than 'throw ex', so the original stack trace survives being
			// rethrown from a different frame than the one that caught it.
			ExceptionDispatchInfo.Capture(ex).Throw();
		}

		LogRetryAttemptException(ex, url, reason, retryCount + 1);
	}

	// The per-attempt log level is configurable at runtime (ODataClientOptions.RetryAttemptLogLevel),
	// so these cannot use the compile-time LoggerMessage pattern. CA1848 is suppressed because the
	// dynamic level rules out the source-generated delegates; the IsEnabled guard avoids the
	// allocation cost when the level is disabled, and retries are off the hot path by definition.
#pragma warning disable CA1848 // Use the LoggerMessage delegates
	private void LogRetryAttemptStatus(Uri? url, HttpStatusCode statusCode, int attempt)
	{
		if (_options.RetryAttemptLogLevel != LogLevel.None && _logger.IsEnabled(_options.RetryAttemptLogLevel))
		{
			_logger.Log(
				_options.RetryAttemptLogLevel,
				new EventId(18, nameof(LogRetryAttemptStatus)),
				"Request to {Url} failed with {StatusCode}, attempt {Attempt}/{MaxRetries}",
				url,
				statusCode,
				attempt,
				_options.RetryCount + 1);
		}
	}

	private void LogRetryAttemptException(Exception ex, Uri? url, string reason, int attempt)
	{
		if (_options.RetryAttemptLogLevel != LogLevel.None && _logger.IsEnabled(_options.RetryAttemptLogLevel))
		{
			_logger.Log(
				_options.RetryAttemptLogLevel,
				new EventId(19, nameof(LogRetryAttemptException)),
				ex,
				"Request to {Url} {Reason}, attempt {Attempt}/{MaxRetries}",
				url,
				reason,
				attempt,
				_options.RetryCount + 1);
		}
	}
#pragma warning restore CA1848 // Use the LoggerMessage delegates
}
