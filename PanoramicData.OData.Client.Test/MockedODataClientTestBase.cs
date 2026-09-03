using Moq.Language.Flow;

namespace PanoramicData.OData.Client.Test;

/// <summary>
/// Base class for tests that drive an <see cref="ODataClient"/> over a mocked
/// <see cref="HttpMessageHandler"/>, owning the fixture and the Moq incantation needed to
/// stub the handler's protected SendAsync.
/// </summary>
public abstract class MockedODataClientTestBase : TestBase, IDisposable
{
	/// <summary>
	/// Base address the mocked client is pointed at. No request ever leaves the process.
	/// </summary>
	protected const string MockBaseUrl = "https://test.odata.org/";

	private bool _disposed;

	/// <summary>
	/// The mocked handler behind <see cref="Client"/>. Stub it with the Setup helpers below,
	/// and assert against it with <see cref="VerifyRequestsSent"/>.
	/// </summary>
	protected Mock<HttpMessageHandler> MockHandler { get; }

	/// <summary>
	/// The <see cref="HttpClient"/> wrapping <see cref="MockHandler"/>.
	/// </summary>
	protected HttpClient MockHttpClient { get; }

	/// <summary>
	/// A client configured against <see cref="MockHandler"/> with retries switched off.
	/// </summary>
	protected ODataClient Client { get; }

	/// <summary>
	/// Initializes the fixture with the default options.
	/// </summary>
	protected MockedODataClientTestBase()
		: this(null)
	{
	}

	/// <summary>
	/// Initializes the fixture, letting a derived class adjust the options first.
	/// </summary>
	/// <param name="configureOptions">
	/// Applied to the default options before <see cref="Client"/> is constructed. The defaults
	/// point at <see cref="MockBaseUrl"/>, log to <see cref="NullLogger"/> and disable retries,
	/// so a test that cares about none of those need not restate them.
	/// </param>
	protected MockedODataClientTestBase(Action<ODataClientOptions>? configureOptions)
	{
		MockHandler = new Mock<HttpMessageHandler>();
		MockHttpClient = new HttpClient(MockHandler.Object)
		{
			BaseAddress = new Uri(MockBaseUrl)
		};

		var options = new ODataClientOptions
		{
			BaseUrl = MockBaseUrl,
			HttpClient = MockHttpClient,
			Logger = NullLogger.Instance,
			RetryCount = 0
		};

		configureOptions?.Invoke(options);

		Client = new ODataClient(options);
	}

	/// <summary>
	/// Builds an additional client over the same mocked handler, for tests that need options
	/// other than the defaults <see cref="Client"/> was built with. The caller owns it.
	/// </summary>
	protected ODataClient CreateClient(Action<ODataClientOptions> configureOptions)
	{
		ArgumentNullException.ThrowIfNull(configureOptions);

		var options = new ODataClientOptions
		{
			BaseUrl = MockBaseUrl,
			HttpClient = MockHttpClient,
			Logger = NullLogger.Instance,
			RetryCount = 0
		};

		configureOptions(options);

		return new ODataClient(options);
	}

	/// <summary>
	/// Begins a stub of the handler's protected SendAsync, for the cases the typed helpers
	/// below do not cover.
	/// </summary>
	protected ISetup<HttpMessageHandler, Task<HttpResponseMessage>> SetupSendAsync()
		=> MockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>());

	/// <summary>
	/// Answers every request with <paramref name="response"/>.
	/// </summary>
	protected void SetupResponse(HttpResponseMessage response)
		=> SetupSendAsync().ReturnsAsync(response);

	/// <summary>
	/// Answers every request with <paramref name="content"/> under <paramref name="statusCode"/>.
	/// </summary>
	protected void SetupResponse(HttpStatusCode statusCode, string content, string mediaType = "application/json")
		=> SetupSendAsync().ReturnsAsync(() => new HttpResponseMessage(statusCode)
		{
			Content = new StringContent(content, Encoding.UTF8, mediaType)
		});

	/// <summary>
	/// Answers every request by invoking <paramref name="responder"/>, which sees the request.
	/// </summary>
	protected void SetupResponse(Func<HttpRequestMessage, HttpResponseMessage> responder)
		=> MockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync((HttpRequestMessage request, CancellationToken _) => responder(request));

	/// <summary>
	/// Answers successive requests with successive <paramref name="responses"/>; once they run
	/// out the last one is repeated.
	/// </summary>
	protected void SetupResponses(params Func<HttpResponseMessage>[] responses)
	{
		ArgumentOutOfRangeException.ThrowIfZero(responses.Length);

		var callCount = 0;
		SetupSendAsync().ReturnsAsync(() =>
		{
			var index = Math.Min(callCount, responses.Length - 1);
			callCount++;
			return responses[index]();
		});
	}

	/// <summary>
	/// Answers every request with <paramref name="response"/>, recording each request sent.
	/// </summary>
	/// <returns>The list the requests are recorded into, in the order they were sent.</returns>
	protected List<HttpRequestMessage> CaptureRequests(HttpResponseMessage response)
	{
		var captured = new List<HttpRequestMessage>();
		SetupResponse(request =>
		{
			captured.Add(request);
			return response;
		});

		return captured;
	}

	/// <summary>
	/// Asserts how many requests reached the handler.
	/// </summary>
	protected void VerifyRequestsSent(Times times)
		=> MockHandler.Protected().Verify(
			"SendAsync",
			times,
			ItExpr.IsAny<HttpRequestMessage>(),
			ItExpr.IsAny<CancellationToken>());

	/// <inheritdoc/>
	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Disposes the client and the HTTP client behind it.
	/// </summary>
	protected virtual void Dispose(bool disposing)
	{
		if (_disposed)
		{
			return;
		}

		if (disposing)
		{
			Client.Dispose();
			MockHttpClient.Dispose();
		}

		_disposed = true;
	}
}
