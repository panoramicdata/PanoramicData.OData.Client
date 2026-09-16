namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Tests that a non-idempotent request is not retried in the cases where the server may already have
/// processed it (issue #43).
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ODataClientRetryStatusCodeTests"/> establishes that 408, 429 and 5xx are all retried.
/// That is right for a GET. It is not right for a POST, and the distinction is the whole point of
/// this class: 408 and 429 mean the server rejected the request <em>without processing it</em>,
/// whereas a 504 means it began work and a proxy stopped waiting. Repeating a POST in that state
/// duplicates work that is still in flight.
/// </para>
/// <para>
/// The failure that prompted this was a single POST starting a two-minute AI investigation behind a
/// proxy with a 60-second read timeout. With the default retry count it ran six times, and the user
/// waited six minutes to be told it had failed.
/// </para>
/// </remarks>
public class ODataClientRetryIdempotencyTests : TestBase
{
	private const string ProductJson = """
	{
		"@odata.context": "https://test.odata.org/$metadata#Products/$entity",
		"ID": 1,
		"Name": "Widget",
		"Price": 9.99
	}
	""";

	private static HttpResponseMessage Ok()
		=> new(HttpStatusCode.OK) { Content = new StringContent(ProductJson, Encoding.UTF8, "application/json") };

	/// <summary>
	/// Builds a client whose transport returns the given status for every call, recording how many
	/// requests were attempted. Deliberately never succeeds, so the count is the number of attempts
	/// the retry policy chose to make.
	/// </summary>
	private static (ODataClient Client, HttpClient HttpClient, Func<int> AttemptCount) CreateAlwaysFailingClient(
		HttpStatusCode status)
	{
		var attempts = 0;

		var handler = new MockHttpMessageHandler(_ =>
		{
			attempts++;

			return new HttpResponseMessage(status)
			{
				Content = new StringContent("{}", Encoding.UTF8, "application/json")
			};
		});

		var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.odata.org/") };

		var options = new ODataClientOptions
		{
			BaseUrl = "https://test.odata.org/",
			HttpClient = httpClient,
			Logger = NullLogger.Instance,
			RetryCount = 3,
			RetryDelay = TimeSpan.FromMilliseconds(1)
		};

		return (new ODataClient(options), httpClient, () => attempts);
	}

	private static Product NewProduct() => new() { Id = 1, Name = "Widget" };

	/// <summary>
	/// A POST that receives a 5xx is attempted once. The server may have processed it.
	/// </summary>
	[Theory]
	[InlineData(HttpStatusCode.InternalServerError)]   // 500
	[InlineData(HttpStatusCode.BadGateway)]            // 502
	[InlineData(HttpStatusCode.ServiceUnavailable)]    // 503
	[InlineData(HttpStatusCode.GatewayTimeout)]        // 504 - the case that prompted this
	public async Task Post_ServerError_IsNotRetried(HttpStatusCode serverError)
	{
		var (client, httpClient, attemptCount) = CreateAlwaysFailingClient(serverError);

		try
		{
			try
			{
				_ = await client.CreateAsync("Products", NewProduct(), cancellationToken: TestContext.Current.CancellationToken);
			}
			catch (Exception)
			{
				// The failure surfaces to the caller, which is correct. The assertion is the point.
			}

			attemptCount().Should().Be(
				1,
				"a POST may already have been processed, so repeating it duplicates work or effect");
		}
		finally
		{
			client.Dispose();
			httpClient.Dispose();
		}
	}

	/// <summary>
	/// A POST that receives 408 or 429 is still retried: in both cases the server demonstrably did
	/// not process it, which is the reasoning that admitted them in the first place.
	/// </summary>
	[Theory]
	[InlineData(HttpStatusCode.RequestTimeout)]   // 408 - never fully received
	[InlineData(HttpStatusCode.TooManyRequests)]  // 429 - refused outright
	public async Task Post_RejectedWithoutProcessing_IsStillRetried(HttpStatusCode rejectedStatus)
	{
		var (client, httpClient, attemptCount) = CreateAlwaysFailingClient(rejectedStatus);

		try
		{
			try
			{
				_ = await client.CreateAsync("Products", NewProduct(), cancellationToken: TestContext.Current.CancellationToken);
			}
			catch (Exception)
			{
				// Expected - it never succeeds. The attempt count is what matters.
			}

			attemptCount().Should().Be(
				4,
				"the server did not process the request, so retrying it is safe even for a POST");
		}
		finally
		{
			client.Dispose();
			httpClient.Dispose();
		}
	}

	/// <summary>
	/// A GET still retries on 5xx exactly as before. This is the regression guard: the fix must not
	/// have narrowed retries for idempotent traffic, which is the overwhelming majority.
	/// </summary>
	[Theory]
	[InlineData(HttpStatusCode.InternalServerError)]
	[InlineData(HttpStatusCode.GatewayTimeout)]
	public async Task Get_ServerError_IsStillRetried(HttpStatusCode serverError)
	{
		var (client, httpClient, attemptCount) = CreateAlwaysFailingClient(serverError);

		try
		{
			try
			{
				_ = await client.GetAsync(
					new ODataQueryBuilder<Product>("Products", NullLogger.Instance),
					TestContext.Current.CancellationToken);
			}
			catch (Exception)
			{
				// Expected - it never succeeds.
			}

			attemptCount().Should().Be(4, "a GET is idempotent, so 5xx retries are unchanged");
		}
		finally
		{
			client.Dispose();
			httpClient.Dispose();
		}
	}
}
