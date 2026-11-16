namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Unit tests for ODataClient disposal behaviour.
/// </summary>
public class ODataClientDisposeTests : MockedODataClientTestBase
{
	/// <summary>
	/// Disposing a client that owns its HttpClient is safe, including twice.
	/// </summary>
	[Fact]
	public void Dispose_OwnedHttpClient_CanBeCalledTwice()
	{
		var client = new ODataClient(new ODataClientOptions { BaseUrl = MockBaseUrl });

		client.Dispose();
		var act = client.Dispose;

		act.Should().NotThrow();
	}

	/// <summary>
	/// Disposing a client with an external HttpClient leaves that HttpClient usable, and is safe twice.
	/// </summary>
	[Fact]
	public async Task Dispose_ExternalHttpClient_DoesNotDisposeHttpClient()
	{
		SetupResponse(HttpStatusCode.OK, """{"value":[]}""");
		var client = CreateClient(_ => { });

		client.Dispose();
		var act = client.Dispose;

		act.Should().NotThrow();
		var response = await MockHttpClient.GetAsync("test", CancellationToken);
		response.StatusCode.Should().Be(HttpStatusCode.OK);
	}

	/// <summary>
	/// The using pattern disposes cleanly with an owned and an external HttpClient.
	/// </summary>
	[Fact]
	public void UsingPattern_DisposesCorrectly()
	{
		using (var owned = new ODataClient(new ODataClientOptions { BaseUrl = MockBaseUrl }))
		{
			owned.Should().NotBeNull();
		}

		using (var external = CreateClient(_ => { }))
		{
			external.Should().NotBeNull();
		}

		MockHttpClient.Should().NotBeNull();
	}
}
