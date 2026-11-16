namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Unit tests for ODataClient construction and option handling.
/// </summary>
public class ODataClientConstructorTests : MockedODataClientTestBase
{
	private const string NorthwindBaseUrl = "https://services.odata.org/V4/Northwind/Northwind.svc";

	/// <summary>
	/// Tests that null options are rejected.
	/// </summary>
	[Fact]
	public void Constructor_NullOptions_ThrowsArgumentNullException()
	{
		var act = () => new ODataClient(null!);

		act.Should().Throw<ArgumentNullException>()
			.WithParameterName("options");
	}

	/// <summary>
	/// Tests that a provided HttpClient is the one used to send requests.
	/// </summary>
	[Fact]
	public async Task Constructor_WithHttpClient_UsesProvidedClient()
	{
		SetupCapturingResponse(new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent("""{"value":[]}""", System.Text.Encoding.UTF8, "application/json")
		});

		await Client.GetAsync(Client.For<TestEntity>("TestEntities"), CancellationToken);

		CapturedRequest.Should().NotBeNull();
		CapturedRequest!.RequestUri!.ToString().Should().Contain("TestEntities");
	}

	/// <summary>
	/// Tests that a client can be built with each option set on its own.
	/// </summary>
	[Fact]
	public void Constructor_WithEachOption_BuildsAClient()
	{
		Action<ODataClientOptions>[] configurations =
		[
			_ => { },
			options => options.Logger = new Mock<ILogger>().Object,
			options => options.Timeout = TimeSpan.FromSeconds(30),
			options => options.JsonSerializerOptions = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = false },
			options => options.MetadataCacheDuration = TimeSpan.FromHours(1),
			options => options.RetryCount = 5,
			options => options.RetryDelay = TimeSpan.FromSeconds(2)
		];

		foreach (var configure in configurations)
		{
			var options = new ODataClientOptions { BaseUrl = MockBaseUrl };
			configure(options);

			using var client = new ODataClient(options);

			client.Should().NotBeNull();
		}
	}

	/// <summary>
	/// Tests that a provided HttpClient gets its BaseAddress from options only when it has none.
	/// Regression test for passing an HttpClient without BaseAddress causing "Invalid request URI".
	/// </summary>
	[Fact]
	public void Constructor_HttpClientBaseAddress_SetOnlyWhenMissing()
	{
		using var withoutAddress = new HttpClient();
		using var withAddress = new HttpClient { BaseAddress = new Uri(NorthwindBaseUrl + "/") };

		using var first = new ODataClient(new ODataClientOptions { BaseUrl = NorthwindBaseUrl, HttpClient = withoutAddress });
		using var second = new ODataClient(new ODataClientOptions { BaseUrl = NorthwindBaseUrl, HttpClient = withAddress });

		withoutAddress.BaseAddress!.ToString().Should().Be(NorthwindBaseUrl + "/");
		withAddress.BaseAddress!.ToString().Should().Be(NorthwindBaseUrl + "/");
	}

	private sealed class TestEntity
	{
		public int Id { get; set; }
	}
}
