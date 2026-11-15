namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Tests that omitting an optional <c>parameters</c> argument behaves as passing <c>null</c>.
/// </summary>
public class ODataClientOmittedParametersTests : MockedODataClientTestBase
{
	/// <summary>
	/// An action called without parameters sends no request body.
	/// </summary>
	[Fact]
	public async Task CallActionAsync_WithoutParameters_SendsNoBody()
	{
		HttpRequestMessage? sent = null;
		SetupResponse(request =>
		{
			sent = request;
			return new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("{}", Encoding.UTF8, "application/json")
			};
		});

		await Client.CallActionAsync<object>("Products/Discount", cancellationToken: CancellationToken);

		sent.Should().NotBeNull();
		sent!.Content.Should().BeNull();
	}

	/// <summary>
	/// A function called without parameters builds a URL without a parameter list.
	/// </summary>
	[Fact]
	public void Function_WithoutParameters_BuildsAnEmptyParameterList()
	{
		Client.For<Product>("Products").Function("Top").BuildUrl().Should().Be("Products/Top()");
		new FluentODataQueryBuilder(Client, "Products", NullLogger.Instance).Function("Top").BuildUrl().Should().Be("Products/Top()");
	}
}
