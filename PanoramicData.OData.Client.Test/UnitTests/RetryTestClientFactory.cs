namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Builds clients over a scripted handler for the retry tests.
/// </summary>
internal static class RetryTestClientFactory
{
	/// <summary>
	/// Creates a client that answers the Nth attempt (from zero) with <paramref name="respond"/>.
	/// </summary>
	internal static (ODataClient Client, HttpClient HttpClient, Func<int> AttemptCount) Create(
		Func<int, HttpResponseMessage> respond,
		TimeSpan retryDelay,
		Action<ODataClientOptions>? configure)
	{
		var attempts = 0;
		var handler = new MockHttpMessageHandler(_ => respond(attempts++));
		var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.odata.org/") };

		var options = new ODataClientOptions
		{
			BaseUrl = "https://test.odata.org/",
			HttpClient = httpClient,
			Logger = NullLogger.Instance,
			RetryCount = 3,
			RetryDelay = retryDelay
		};

		configure?.Invoke(options);

		return (new ODataClient(options), httpClient, () => attempts);
	}

	/// <summary>
	/// A JSON response with the given status.
	/// </summary>
	internal static HttpResponseMessage JsonResponse(HttpStatusCode status)
		=> new(status) { Content = new StringContent("{}", Encoding.UTF8, "application/json") };
}
