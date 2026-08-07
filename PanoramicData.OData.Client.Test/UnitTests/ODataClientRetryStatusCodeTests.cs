using System.Diagnostics;

namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Tests for which HTTP status codes are treated as transient and retried, and for how long the client
/// waits between attempts.
/// </summary>
/// <remarks>
/// 408 and 429 were previously not retried: the client returned immediately for anything below 500. Both
/// are cases where the server rejected the request without processing it, so retrying is safe even for
/// methods that are not idempotent, and a caller behind a proxy that emits 408 had no recourse.
///
/// The negative cases matter at least as much as the positive ones. A 409 is a routine "already exists"
/// outcome for callers that create-or-overwrite, and retrying it would be actively wrong.
/// </remarks>
public class ODataClientRetryStatusCodeTests : TestBase
{
	private const string ProductsJson = """
	{
		"@odata.context": "https://test.odata.org/$metadata#Products",
		"value": [ { "ID": 1, "Name": "Widget", "Price": 9.99 } ]
	}
	""";

	private static HttpResponseMessage Ok()
		=> new(HttpStatusCode.OK) { Content = new StringContent(ProductsJson, Encoding.UTF8, "application/json") };

	private static ODataQueryBuilder<Product> ProductsQuery() => new("Products", NullLogger.Instance);

	/// <summary>
	/// Builds a client whose transport returns the given statuses in order, then 200 for every later call,
	/// recording how many requests were actually attempted.
	/// </summary>
	private static (ODataClient Client, HttpClient HttpClient, Func<int> AttemptCount) CreateClient(
		HttpStatusCode[] failuresThenSuccess,
		TimeSpan? retryDelay = null,
		TimeSpan? retryAfter = null,
		TimeSpan? maximumRetryAfterDelay = null)
	{
		var attempts = 0;

		var handler = new MockHttpMessageHandler(_ =>
		{
			var index = attempts++;

			if (index >= failuresThenSuccess.Length)
			{
				return Ok();
			}

			var response = new HttpResponseMessage(failuresThenSuccess[index])
			{
				Content = new StringContent("{}", Encoding.UTF8, "application/json")
			};

			if (retryAfter is { } delta)
			{
				response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(delta);
			}

			return response;
		});

		var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.odata.org/") };

		var options = new ODataClientOptions
		{
			BaseUrl = "https://test.odata.org/",
			HttpClient = httpClient,
			Logger = NullLogger.Instance,
			RetryCount = 3,
			RetryDelay = retryDelay ?? TimeSpan.FromMilliseconds(1)
		};

		if (maximumRetryAfterDelay is { } maximum)
		{
			options.MaximumRetryAfterDelay = maximum;
		}

		return (new ODataClient(options), httpClient, () => attempts);
	}

	/// <summary>
	/// A transient status is retried and the call then succeeds.
	/// </summary>
	[Theory]
	[InlineData(HttpStatusCode.RequestTimeout)]        // 408 - the case that prompted this
	[InlineData(HttpStatusCode.TooManyRequests)]       // 429
	[InlineData(HttpStatusCode.InternalServerError)]   // 500 - must not regress
	[InlineData(HttpStatusCode.BadGateway)]            // 502
	[InlineData(HttpStatusCode.ServiceUnavailable)]    // 503
	[InlineData(HttpStatusCode.GatewayTimeout)]        // 504
	public async Task TransientStatus_IsRetried_AndSucceeds(HttpStatusCode transientStatus)
	{
		var (client, httpClient, attemptCount) = CreateClient([transientStatus]);

		try
		{
			var response = await client.GetAsync(ProductsQuery(), TestContext.Current.CancellationToken);

			response.Value.Should().ContainSingle("the retry should have succeeded on the second attempt");
			attemptCount().Should().Be(2, "one failure then one success");
		}
		finally
		{
			client.Dispose();
			httpClient.Dispose();
		}
	}

	/// <summary>
	/// A genuine rejection is returned to the caller on the first attempt, never retried.
	/// </summary>
	[Theory]
	[InlineData(HttpStatusCode.BadRequest)]    // 400
	[InlineData(HttpStatusCode.Unauthorized)]  // 401
	[InlineData(HttpStatusCode.Forbidden)]     // 403
	[InlineData(HttpStatusCode.NotFound)]      // 404
	[InlineData(HttpStatusCode.Conflict)]      // 409 - routine "already exists"; retrying would be wrong
	[InlineData(HttpStatusCode.Gone)]          // 410
	public async Task GenuineRejection_IsNotRetried(HttpStatusCode rejectionStatus)
	{
		var (client, httpClient, attemptCount) = CreateClient([rejectionStatus]);

		try
		{
			// The status surfaces to the caller (as a result or an exception); what matters here is that
			// it was not attempted a second time.
			try
			{
				_ = await client.GetAsync(ProductsQuery(), TestContext.Current.CancellationToken);
			}
			catch (Exception)
			{
				// Expected for several of these; the assertion below is the point of the test.
			}

			attemptCount().Should().Be(1, "a genuine rejection must be returned to the caller, not retried");
		}
		finally
		{
			client.Dispose();
			httpClient.Dispose();
		}
	}

	/// <summary>
	/// Retries stop at RetryCount rather than continuing indefinitely.
	/// </summary>
	[Fact]
	public async Task TransientStatus_IsRetriedUpToRetryCount_ThenGivesUp()
	{
		// Four failures against RetryCount = 3 means the last attempt still fails.
		var failures = new[]
		{
			HttpStatusCode.RequestTimeout,
			HttpStatusCode.RequestTimeout,
			HttpStatusCode.RequestTimeout,
			HttpStatusCode.RequestTimeout
		};

		var (client, httpClient, attemptCount) = CreateClient(failures);

		try
		{
			try
			{
				_ = await client.GetAsync(ProductsQuery(), TestContext.Current.CancellationToken);
			}
			catch (Exception)
			{
				// The failure surfacing is expected; the attempt count is what is under test.
			}

			attemptCount().Should().Be(4, "the initial attempt plus RetryCount (3) retries, and no more");
		}
		finally
		{
			client.Dispose();
			httpClient.Dispose();
		}
	}

	/// <summary>
	/// A server-supplied Retry-After is used in preference to the configured RetryDelay.
	/// </summary>
	[Fact]
	public async Task RetryAfter_IsPreferredOverRetryDelay()
	{
		// A long RetryDelay with a short Retry-After: honouring the header should complete quickly.
		var (client, httpClient, _) = CreateClient(
			[HttpStatusCode.TooManyRequests],
			retryDelay: TimeSpan.FromSeconds(30),
			retryAfter: TimeSpan.FromMilliseconds(50));

		try
		{
			var stopwatch = Stopwatch.StartNew();
			_ = await client.GetAsync(ProductsQuery(), TestContext.Current.CancellationToken);
			stopwatch.Stop();

			stopwatch.Elapsed.Should().BeLessThan(
				TimeSpan.FromSeconds(10),
				"the 50 ms Retry-After should be used rather than the 30 second RetryDelay");
		}
		finally
		{
			client.Dispose();
			httpClient.Dispose();
		}
	}

	/// <summary>
	/// An excessive Retry-After is bounded by MaximumRetryAfterDelay rather than honoured in full.
	/// </summary>
	[Fact]
	public async Task RetryAfter_IsBoundedByMaximumRetryAfterDelay()
	{
		// A hostile Retry-After of an hour, bounded to 50 ms, must not stall the caller.
		var (client, httpClient, _) = CreateClient(
			[HttpStatusCode.TooManyRequests],
			retryDelay: TimeSpan.FromMilliseconds(1),
			retryAfter: TimeSpan.FromHours(1),
			maximumRetryAfterDelay: TimeSpan.FromMilliseconds(50));

		try
		{
			var stopwatch = Stopwatch.StartNew();
			_ = await client.GetAsync(ProductsQuery(), TestContext.Current.CancellationToken);
			stopwatch.Stop();

			stopwatch.Elapsed.Should().BeLessThan(
				TimeSpan.FromSeconds(10),
				"a one hour Retry-After must be capped rather than honoured");
		}
		finally
		{
			client.Dispose();
			httpClient.Dispose();
		}
	}

	/// <summary>
	/// Setting MaximumRetryAfterDelay to zero opts out of Retry-After entirely.
	/// </summary>
	[Fact]
	public async Task RetryAfter_IsIgnoredWhenMaximumIsZero()
	{
		// Opting out: Retry-After of an hour, maximum of zero, so RetryDelay applies instead.
		var (client, httpClient, _) = CreateClient(
			[HttpStatusCode.TooManyRequests],
			retryDelay: TimeSpan.FromMilliseconds(1),
			retryAfter: TimeSpan.FromHours(1),
			maximumRetryAfterDelay: TimeSpan.Zero);

		try
		{
			var stopwatch = Stopwatch.StartNew();
			_ = await client.GetAsync(ProductsQuery(), TestContext.Current.CancellationToken);
			stopwatch.Stop();

			stopwatch.Elapsed.Should().BeLessThan(
				TimeSpan.FromSeconds(10),
				"a zero maximum should disable Retry-After entirely and fall back to RetryDelay");
		}
		finally
		{
			client.Dispose();
			httpClient.Dispose();
		}
	}
}
