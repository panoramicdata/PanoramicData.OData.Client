namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Unit tests for OData entity reference ($ref) support.
/// </summary>
public class ODataClientReferenceTests : MockedODataClientTestBase
{
	#region AddReferenceAsync Tests

	/// <summary>
	/// Tests that AddReferenceAsync sends POST to correct URL.
	/// </summary>
	[Fact]
	public async Task AddReferenceAsync_SendsPostToRefUrl()
	{
		// Arrange
		HttpRequestMessage? capturedRequest = null;

		SetupSendAsync()
			.Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NoContent));

		// Act
		await Client.AddReferenceAsync("People", "scott", "Friends", "People", "john", cancellationToken: CancellationToken.None);

		// Assert
		capturedRequest.Should().NotBeNull();
		capturedRequest!.Method.Should().Be(HttpMethod.Post);
		capturedRequest.RequestUri!.PathAndQuery.Should().Be("/People('scott')/Friends/$ref");
	}

	/// <summary>
	/// Tests that AddReferenceAsync sends correct body with @odata.id.
	/// </summary>
	[Fact]
	public async Task AddReferenceAsync_SendsODataIdInBody()
	{
		// Arrange
		string? capturedBody = null;

		SetupSendAsync()
			.Callback<HttpRequestMessage, CancellationToken>(async (req, ct) =>
			{
				capturedBody = await req.Content!.ReadAsStringAsync(ct);
			})
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NoContent));

		// Act
		await Client.AddReferenceAsync("People", "scott", "Friends", "People", "john", cancellationToken: CancellationToken.None);

		// Assert
		capturedBody.Should().NotBeNull();
		capturedBody.Should().Contain("@odata.id");
		capturedBody.Should().Contain("People('john')");
	}

	/// <summary>
	/// Tests that AddReferenceAsync works with integer keys.
	/// </summary>
	[Fact]
	public async Task AddReferenceAsync_WithIntegerKeys_FormatsCorrectly()
	{
		// Arrange
		HttpRequestMessage? capturedRequest = null;

		SetupSendAsync()
			.Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NoContent));

		// Act
		await Client.AddReferenceAsync("Orders", 1, "Products", "Products", 42, cancellationToken: CancellationToken.None);

		// Assert
		capturedRequest.Should().NotBeNull();
		capturedRequest!.RequestUri!.PathAndQuery.Should().Be("/Orders(1)/Products/$ref");
	}

	#endregion

	#region RemoveReferenceAsync Tests

	/// <summary>
	/// Tests that RemoveReferenceAsync sends DELETE with $id query parameter.
	/// </summary>
	[Fact]
	public async Task RemoveReferenceAsync_SendsDeleteWithIdParam()
	{
		// Arrange
		HttpRequestMessage? capturedRequest = null;

		SetupSendAsync()
			.Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NoContent));

		// Act
		await Client.RemoveReferenceAsync("People", "scott", "Friends", "People", "john", cancellationToken: CancellationToken.None);

		// Assert
		capturedRequest.Should().NotBeNull();
		capturedRequest!.Method.Should().Be(HttpMethod.Delete);
		capturedRequest.RequestUri!.PathAndQuery.Should().Contain("/People('scott')/Friends/$ref?$id=");
		capturedRequest.RequestUri.PathAndQuery.Should().Contain("People");
		capturedRequest.RequestUri.PathAndQuery.Should().Contain("john");
	}

	/// <summary>
	/// Tests that RemoveReferenceAsync works with integer keys.
	/// </summary>
	[Fact]
	public async Task RemoveReferenceAsync_WithIntegerKeys_FormatsCorrectly()
	{
		// Arrange
		HttpRequestMessage? capturedRequest = null;

		SetupSendAsync()
			.Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NoContent));

		// Act
		await Client.RemoveReferenceAsync("Orders", 1, "Products", "Products", 42, cancellationToken: CancellationToken.None);

		// Assert
		capturedRequest.Should().NotBeNull();
		capturedRequest!.RequestUri!.PathAndQuery.Should().Contain("/Orders(1)/Products/$ref?$id=");
	}

	#endregion

	#region SetReferenceAsync Tests

	/// <summary>
	/// Tests that SetReferenceAsync sends PUT to correct URL.
	/// </summary>
	[Fact]
	public async Task SetReferenceAsync_SendsPutToRefUrl()
	{
		// Arrange
		HttpRequestMessage? capturedRequest = null;

		SetupSendAsync()
			.Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NoContent));

		// Act
		await Client.SetReferenceAsync("People", "scott", "BestFriend", "People", "john", cancellationToken: CancellationToken.None);

		// Assert
		capturedRequest.Should().NotBeNull();
		capturedRequest!.Method.Should().Be(HttpMethod.Put);
		capturedRequest.RequestUri!.PathAndQuery.Should().Be("/People('scott')/BestFriend/$ref");
	}

	/// <summary>
	/// Tests that SetReferenceAsync sends correct body with @odata.id.
	/// </summary>
	[Fact]
	public async Task SetReferenceAsync_SendsODataIdInBody()
	{
		// Arrange
		string? capturedBody = null;

		SetupSendAsync()
			.Callback<HttpRequestMessage, CancellationToken>(async (req, ct) =>
			{
				capturedBody = await req.Content!.ReadAsStringAsync(ct);
			})
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NoContent));

		// Act
		await Client.SetReferenceAsync("People", "scott", "BestFriend", "People", "john", cancellationToken: CancellationToken.None);

		// Assert
		capturedBody.Should().NotBeNull();
		capturedBody.Should().Contain("@odata.id");
		capturedBody.Should().Contain("People('john')");
	}

	#endregion

	#region DeleteReferenceAsync Tests

	/// <summary>
	/// Tests that DeleteReferenceAsync sends DELETE to $ref URL.
	/// </summary>
	[Fact]
	public async Task DeleteReferenceAsync_SendsDeleteToRefUrl()
	{
		// Arrange
		HttpRequestMessage? capturedRequest = null;

		SetupSendAsync()
			.Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NoContent));

		// Act
		await Client.DeleteReferenceAsync("People", "scott", "BestFriend", cancellationToken: CancellationToken.None);

		// Assert
		capturedRequest.Should().NotBeNull();
		capturedRequest!.Method.Should().Be(HttpMethod.Delete);
		capturedRequest.RequestUri!.PathAndQuery.Should().Be("/People('scott')/BestFriend/$ref");
	}

	/// <summary>
	/// Tests that DeleteReferenceAsync works with integer keys.
	/// </summary>
	[Fact]
	public async Task DeleteReferenceAsync_WithIntegerKey_FormatsCorrectly()
	{
		// Arrange
		HttpRequestMessage? capturedRequest = null;

		SetupSendAsync()
			.Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NoContent));

		// Act
		await Client.DeleteReferenceAsync("Orders", 1, "Customer", cancellationToken: CancellationToken.None);

		// Assert
		capturedRequest.Should().NotBeNull();
		capturedRequest!.RequestUri!.PathAndQuery.Should().Be("/Orders(1)/Customer/$ref");
	}

	#endregion
}
