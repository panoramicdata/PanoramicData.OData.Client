namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Tests that <see cref="ODataClientOptions.IsReadOnly"/> refuses writes before any request is sent.
/// </summary>
public class ODataClientReadOnlyTests : MockedODataClientTestBase
{
	private const string ProductsJson = """{ "value": [ { "ID": 1, "Name": "Widget" } ] }""";

	private int _requestsSent;

	/// <summary>Builds a client with IsReadOnly set over a counting stub handler.</summary>
	public ODataClientReadOnlyTests()
		: base(options => options.IsReadOnly = true)
	{
		SetupCountedResponse(ProductsJson, "application/json");
	}

	/// <summary>IsReadOnly is off unless a caller sets it.</summary>
	[Fact]
	public void IsReadOnly_DefaultsToFalse()
		=> new ODataClientOptions { BaseUrl = MockBaseUrl }.IsReadOnly.Should().BeFalse();

	/// <summary>Reads still reach the server.</summary>
	[Fact]
	public async Task Get_IsSentWhenReadOnly()
	{
		var response = await Client.GetAsync(Client.For<Product>("Products"), CancellationToken);

		response.Value.Should().ContainSingle();
		_requestsSent.Should().Be(1);
	}

	/// <summary>The write operations that must be refused.</summary>
	public static TheoryData<string> WriteCases => ["create", "patch", "delete", "action", "addReference", "putStream"];

	/// <summary>Each write throws before the handler sees a request.</summary>
	[Theory]
	[MemberData(nameof(WriteCases))]
	public async Task Write_IsRefusedBeforeSending(string write)
	{
		var act = () => RunWriteAsync(Client, write);

		var exception = await act.Should().ThrowAsync<InvalidOperationException>();
		exception.Which.Message.Should().Contain("read-only");
		_requestsSent.Should().Be(0);
	}

	/// <summary>A batch of only GETs is allowed through.</summary>
	[Fact]
	public async Task Batch_OfGetsOnly_IsSent()
	{
		SetupCountedResponse(string.Empty, "multipart/mixed; boundary=b");

		var batch = Client.CreateBatch().Get<Product>("Products", 1);

		// The stub body is not a real batch response, so only whether a request left is asserted.
		_ = await Record.ExceptionAsync(() => batch.ExecuteAsync(CancellationToken));

		_requestsSent.Should().Be(1);
	}

	/// <summary>A batch holding any change, loose or in a changeset, is refused.</summary>
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Batch_WithAChange_IsRefused(bool inChangeset)
	{
		var batch = Client.CreateBatch();
		batch = inChangeset
			? batch.Changeset(changeset => changeset.Create("Products", new Product { Name = "Widget" }))
			: batch.Delete("Products", 1);

		var act = () => batch.ExecuteAsync(CancellationToken);

		await act.Should().ThrowAsync<InvalidOperationException>();
		_requestsSent.Should().Be(0);
	}

	/// <summary>The option is read on every request.</summary>
	[Fact]
	public async Task ChangingTheOptionAfterConstruction_TakesEffectOnTheNextRequest()
	{
		var options = new ODataClientOptions { BaseUrl = MockBaseUrl, HttpClient = MockHttpClient, RetryCount = 0 };
		using var client = new ODataClient(options);

		await client.DeleteAsync("Products", 1, cancellationToken: CancellationToken);
		options.IsReadOnly = true;
		var act = () => client.DeleteAsync("Products", 1, cancellationToken: CancellationToken);

		await act.Should().ThrowAsync<InvalidOperationException>();
		_requestsSent.Should().Be(1);
	}

	private void SetupCountedResponse(string content, string mediaType)
		=> SetupResponse(_ =>
		{
			_requestsSent++;
			return new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(content, Encoding.UTF8, mediaType)
			};
		});

	private static async Task RunWriteAsync(ODataClient client, string write)
	{
		using var stream = new MemoryStream([1]);
		await (write switch
		{
			"create" => client.CreateAsync("Products", new Product { Name = "Widget" }, cancellationToken: CancellationToken),
			"patch" => client.UpdateAsync<Product>("Products", 1, new { Name = "Gadget" }, cancellationToken: CancellationToken),
			"delete" => client.DeleteAsync("Products", 1, cancellationToken: CancellationToken),
			"action" => client.CallActionAsync<object>("Products/Discount", cancellationToken: CancellationToken),
			"addReference" => client.AddReferenceAsync("Products", 1, "Category", "Categories", 2, cancellationToken: CancellationToken),
			_ => client.SetStreamAsync("Products", 1, stream, cancellationToken: CancellationToken)
		});
	}
}
