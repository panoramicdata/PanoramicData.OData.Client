namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Unit tests for OData entity reference operations ($ref).
/// </summary>
public class ODataClientReferenceTests : MockedODataClientTestBase
{
	/// <summary>
	/// Tests that AddReferenceAsync posts to the collection $ref URL, with string and integer keys.
	/// </summary>
	[Fact]
	public async Task AddReferenceAsync_SendsPostToRefUrl()
	{
		SetupCapturingResponse(new HttpResponseMessage(HttpStatusCode.NoContent));

		await Client.AddReferenceAsync("People", "scott", "Friends", "People", "john", cancellationToken: CancellationToken.None);
		AssertRequest(HttpMethod.Post, "/People('scott')/Friends/$ref");

		SetupCapturingResponse(new HttpResponseMessage(HttpStatusCode.NoContent));

		await Client.AddReferenceAsync("Orders", 1, "Products", "Products", 42, cancellationToken: CancellationToken.None);
		AssertRequest(HttpMethod.Post, "/Orders(1)/Products/$ref");
	}

	/// <summary>
	/// Tests that RemoveReferenceAsync deletes with an $id parameter, with string and integer keys.
	/// </summary>
	[Fact]
	public async Task RemoveReferenceAsync_SendsDeleteWithIdParam()
	{
		SetupCapturingResponse(new HttpResponseMessage(HttpStatusCode.NoContent));

		await Client.RemoveReferenceAsync("People", "scott", "Friends", "People", "john", cancellationToken: CancellationToken.None);

		AssertRequest(HttpMethod.Delete, "/People('scott')/Friends/$ref?$id=");
		CapturedRequest!.RequestUri!.PathAndQuery.Should().Contain("People").And.Contain("john");

		SetupCapturingResponse(new HttpResponseMessage(HttpStatusCode.NoContent));

		await Client.RemoveReferenceAsync("Orders", 1, "Products", "Products", 42, cancellationToken: CancellationToken.None);
		AssertRequest(HttpMethod.Delete, "/Orders(1)/Products/$ref?$id=");
	}

	/// <summary>
	/// Tests that SetReferenceAsync puts to the single-valued $ref URL.
	/// </summary>
	[Fact]
	public async Task SetReferenceAsync_SendsPutToRefUrl()
	{
		SetupCapturingResponse(new HttpResponseMessage(HttpStatusCode.NoContent));

		await Client.SetReferenceAsync("People", "scott", "BestFriend", "People", "john", cancellationToken: CancellationToken.None);

		AssertRequest(HttpMethod.Put, "/People('scott')/BestFriend/$ref");
	}

	/// <summary>
	/// Tests that DeleteReferenceAsync deletes the single-valued $ref URL, with string and integer keys.
	/// </summary>
	[Fact]
	public async Task DeleteReferenceAsync_SendsDeleteToRefUrl()
	{
		SetupCapturingResponse(new HttpResponseMessage(HttpStatusCode.NoContent));

		await Client.DeleteReferenceAsync("People", "scott", "BestFriend", cancellationToken: CancellationToken.None);
		AssertRequest(HttpMethod.Delete, "/People('scott')/BestFriend/$ref");

		SetupCapturingResponse(new HttpResponseMessage(HttpStatusCode.NoContent));

		await Client.DeleteReferenceAsync("Orders", 1, "Customer", cancellationToken: CancellationToken.None);
		AssertRequest(HttpMethod.Delete, "/Orders(1)/Customer/$ref");
	}

	/// <summary>
	/// Tests that add and set send the target as an @odata.id body.
	/// </summary>
	[Fact]
	public async Task AddAndSetReference_SendODataIdInBody()
	{
		string? capturedBody = null;
		SetupSendAsync()
			.Callback<HttpRequestMessage, CancellationToken>(async (request, ct) => capturedBody = await request.Content!.ReadAsStringAsync(ct))
			.ReturnsAsync(() => new HttpResponseMessage(HttpStatusCode.NoContent));

		await Client.AddReferenceAsync("People", "scott", "Friends", "People", "john", cancellationToken: CancellationToken.None);
		AssertODataIdBody(capturedBody);

		capturedBody = null;
		await Client.SetReferenceAsync("People", "scott", "BestFriend", "People", "john", cancellationToken: CancellationToken.None);
		AssertODataIdBody(capturedBody);
	}

	private static void AssertODataIdBody(string? body)
		=> body.Should().NotBeNull().And.Contain("@odata.id").And.Contain("People('john')");

	private void AssertRequest(HttpMethod method, string expectedPathAndQuery)
	{
		CapturedRequest.Should().NotBeNull();
		CapturedRequest!.Method.Should().Be(method);
		var pathAndQuery = CapturedRequest.RequestUri!.PathAndQuery;
		if (expectedPathAndQuery.Contains('?', StringComparison.Ordinal))
		{
			pathAndQuery.Should().StartWith(expectedPathAndQuery);
		}
		else
		{
			pathAndQuery.Should().Be(expectedPathAndQuery);
		}
	}
}
