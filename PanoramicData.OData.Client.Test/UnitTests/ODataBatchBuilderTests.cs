namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Unit tests for ODataBatchBuilder and ODataChangesetBuilder.
/// </summary>
public class ODataBatchBuilderTests : TestBase, IDisposable
{
	private readonly Mock<HttpMessageHandler> _mockHandler;
	private readonly HttpClient _httpClient;
	private readonly ODataClient _client;

	/// <summary>
	/// Initializes a new instance of the test class with mocked dependencies.
	/// </summary>
	public ODataBatchBuilderTests()
	{
		_mockHandler = new Mock<HttpMessageHandler>();
		_httpClient = new HttpClient(_mockHandler.Object)
		{
			BaseAddress = new Uri("https://test.odata.org/")
		};
		_client = new ODataClient(new ODataClientOptions
		{
			BaseUrl = "https://test.odata.org/",
			HttpClient = _httpClient,
			Logger = NullLogger.Instance,
			RetryCount = 0
		});
	}

	/// <inheritdoc/>
	public void Dispose()
	{
		_client.Dispose();
		_httpClient.Dispose();
		GC.SuppressFinalize(this);
	}

	#region Get Operation Tests

	/// <summary>
	/// Tests Get operation with integer key.
	/// </summary>
	[Fact]
	public void Get_WithIntKey_AddsOperation()
	{
		// Act
		var builder = _client.CreateBatch()
			.Get<TestProduct, int>("Products", 123);

		// Assert
		builder.Items.Should().ContainSingle();
		var operations = builder.GetAllOperations().ToList();
		operations.Should().ContainSingle();
		operations[0].OperationType.Should().Be(ODataBatchOperationType.Get);
		operations[0].Url.Should().Be("Products(123)");
		operations[0].ResultType.Should().Be<TestProduct>();
	}

	/// <summary>
	/// Tests Get operation with string key.
	/// </summary>
	[Fact]
	public void Get_WithStringKey_FormatsWithQuotes()
	{
		// Act
		var builder = _client.CreateBatch()
			.Get<TestProduct>("Products", "abc");

		// Assert
		var operations = builder.GetAllOperations().ToList();
		operations[0].Url.Should().Be("Products('abc')");
	}

	/// <summary>
	/// Tests Get operation with Guid key.
	/// </summary>
	[Fact]
	public void Get_WithGuidKey_FormatsCorrectly()
	{
		// Arrange
		var guid = Guid.Parse("12345678-1234-1234-1234-123456789012");

		// Act
		var builder = _client.CreateBatch()
			.Get<TestProduct, Guid>("Products", guid);

		// Assert
		var operations = builder.GetAllOperations().ToList();
		operations[0].Url.Should().Be("Products(12345678-1234-1234-1234-123456789012)");
	}

	#endregion

	#region Create Operation Tests

	/// <summary>
	/// Tests Create operation.
	/// </summary>
	[Fact]
	public void Create_AddsOperation()
	{
		// Arrange
		var product = new TestProduct { Id = 0, Name = "New Product" };

		// Act
		var builder = _client.CreateBatch()
			.Create("Products", product);

		// Assert
		var operations = builder.GetAllOperations().ToList();
		operations.Should().ContainSingle();
		operations[0].OperationType.Should().Be(ODataBatchOperationType.Create);
		operations[0].Url.Should().Be("Products");
		operations[0].Body.Should().Be(product);
		operations[0].ResultType.Should().Be<TestProduct>();
	}

	#endregion

	#region Update Operation Tests

	/// <summary>
	/// Tests Update operation without ETag.
	/// </summary>
	[Fact]
	public void Update_WithoutETag_AddsOperation()
	{
		// Act
		var builder = _client.CreateBatch()
			.Update<TestProduct>("Products", 1, new { Name = "Updated" });

		// Assert
		var operations = builder.GetAllOperations().ToList();
		operations.Should().ContainSingle();
		operations[0].OperationType.Should().Be(ODataBatchOperationType.Update);
		operations[0].Url.Should().Be("Products(1)");
		operations[0].ETag.Should().BeNull();
	}

	/// <summary>
	/// Tests Update operation with ETag.
	/// </summary>
	[Fact]
	public void Update_WithETag_SetsETag()
	{
		// Act
		var builder = _client.CreateBatch()
			.Update<TestProduct, int>("Products", 1, new { Name = "Updated" }, "\"etag-value\"");

		// Assert
		var operations = builder.GetAllOperations().ToList();
		operations[0].ETag.Should().Be("\"etag-value\"");
	}

	#endregion

	#region Delete Operation Tests

	/// <summary>
	/// Tests Delete operation without ETag.
	/// </summary>
	[Fact]
	public void Delete_WithoutETag_AddsOperation()
	{
		// Act
		var builder = _client.CreateBatch()
			.Delete("Products", 1);

		// Assert
		var operations = builder.GetAllOperations().ToList();
		operations.Should().ContainSingle();
		operations[0].OperationType.Should().Be(ODataBatchOperationType.Delete);
		operations[0].Url.Should().Be("Products(1)");
	}

	/// <summary>
	/// Tests Delete operation with ETag.
	/// </summary>
	[Fact]
	public void Delete_WithETag_SetsETag()
	{
		// Act
		var builder = _client.CreateBatch()
			.Delete<int>("Products", 1, "\"etag\"");

		// Assert
		var operations = builder.GetAllOperations().ToList();
		operations[0].ETag.Should().Be("\"etag\"");
	}

	/// <summary>
	/// Tests Delete with long key.
	/// </summary>
	[Fact]
	public void Delete_WithLongKey_FormatsCorrectly()
	{
		// Act
		var builder = _client.CreateBatch()
			.Delete<long>("Products", 9876543210L);

		// Assert
		var operations = builder.GetAllOperations().ToList();
		operations[0].Url.Should().Be("Products(9876543210)");
	}

	#endregion

	#region Changeset Tests

	/// <summary>
	/// Tests Changeset with multiple operations.
	/// </summary>
	[Fact]
	public void Changeset_WithOperations_AddsChangeset()
	{
		// Act
		var builder = _client.CreateBatch()
			.Changeset(cs => cs
				.Create("Products", new TestProduct { Name = "New" })
				.Update<TestProduct>("Products", 1, new { Name = "Updated" })
				.Delete("Products", 2));

		// Assert
		builder.Items.Should().ContainSingle();
		builder.Items[0].Should().BeOfType<ODataChangeset>();

		var operations = builder.GetAllOperations().ToList();
		operations.Should().HaveCount(3);
		operations[0].OperationType.Should().Be(ODataBatchOperationType.Create);
		operations[1].OperationType.Should().Be(ODataBatchOperationType.Update);
		operations[2].OperationType.Should().Be(ODataBatchOperationType.Delete);
	}

	/// <summary>
	/// Tests Changeset Create operation.
	/// </summary>
	[Fact]
	public void Changeset_Create_AddsOperation()
	{
		// Arrange
		var product = new TestProduct { Name = "New" };

		// Act
		var builder = _client.CreateBatch()
			.Changeset(cs => cs.Create("Products", product));

		// Assert
		var operations = builder.GetAllOperations().ToList();
		operations[0].OperationType.Should().Be(ODataBatchOperationType.Create);
		operations[0].Body.Should().Be(product);
	}

	/// <summary>
	/// Tests Changeset Update with ETag.
	/// </summary>
	[Fact]
	public void Changeset_UpdateWithETag_SetsETag()
	{
		// Act
		var builder = _client.CreateBatch()
			.Changeset(cs => cs.Update<TestProduct, int>("Products", 1, new { Name = "Updated" }, "\"etag\""));

		// Assert
		var operations = builder.GetAllOperations().ToList();
		operations[0].ETag.Should().Be("\"etag\"");
	}

	/// <summary>
	/// Tests Changeset Delete with ETag.
	/// </summary>
	[Fact]
	public void Changeset_DeleteWithETag_SetsETag()
	{
		// Act
		var builder = _client.CreateBatch()
			.Changeset(cs => cs.Delete<int>("Products", 1, "\"etag\""));

		// Assert
		var operations = builder.GetAllOperations().ToList();
		operations[0].ETag.Should().Be("\"etag\"");
	}

	#endregion

	#region Fluent Chaining Tests

	/// <summary>
	/// Tests fluent chaining of all operation types.
	/// </summary>
	[Fact]
	public void FluentChaining_AllOperationTypes_Works()
	{
		// Act
		var builder = _client.CreateBatch()
			.Get<TestProduct>("Products", 1)
			.Create("Products", new TestProduct { Name = "New" })
			.Update<TestProduct>("Products", 2, new { Name = "Updated" })
			.Delete("Products", 3)
			.Changeset(cs => cs
				.Create("Categories", new TestCategory { Name = "NewCat" })
				.Delete("Categories", 1));

		// Assert
		builder.Items.Should().HaveCount(5); // 4 individual ops + 1 changeset
		var allOps = builder.GetAllOperations().ToList();
		allOps.Should().HaveCount(6); // 4 + 2 in changeset
	}

	/// <summary>
	/// Tests GetAllOperations returns operations in order.
	/// </summary>
	[Fact]
	public void GetAllOperations_ReturnsInOrder()
	{
		// Act
		var builder = _client.CreateBatch()
			.Get<TestProduct>("Products", 1)
			.Get<TestProduct>("Products", 2)
			.Get<TestProduct>("Products", 3);

		// Assert
		var operations = builder.GetAllOperations().ToList();
		operations[0].Url.Should().Contain("1");
		operations[1].Url.Should().Contain("2");
		operations[2].Url.Should().Contain("3");
	}

	#endregion

	#region ExecuteAsync Tests

	/// <summary>
	/// Tests ExecuteAsync sends batch request.
	/// </summary>
	[Fact]
	public async Task ExecuteAsync_SendsBatchRequest()
	{
		// Arrange
		HttpRequestMessage? capturedRequest = null;
		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
			.ReturnsAsync(CreateBatchResponse());

		// Act
		await _client.CreateBatch()
			.Get<TestProduct>("Products", 1)
			.ExecuteAsync(CancellationToken);

		// Assert
		capturedRequest.Should().NotBeNull();
		capturedRequest!.Method.Should().Be(HttpMethod.Post);
		capturedRequest.RequestUri!.ToString().Should().Contain("$batch");
	}

	/// <summary>
	/// Tests ExecuteAsync returns batch response.
	/// </summary>
	[Fact]
	public async Task ExecuteAsync_ReturnsBatchResponse()
	{
		// Arrange
		SetupBatchMockResponse();

		// Act
		var response = await _client.CreateBatch()
			.Get<TestProduct>("Products", 1)
			.ExecuteAsync(CancellationToken);

		// Assert
		response.Should().NotBeNull();
		response.Results.Should().NotBeNull();
	}

	#endregion

	#region Key Formatting Tests

	/// <summary>
	/// Tests string key with single quote is escaped.
	/// </summary>
	[Fact]
	public void Key_StringWithSingleQuote_Escaped()
	{
		// Act
		var builder = _client.CreateBatch()
			.Get<TestProduct>("Products", "O'Brien");

		// Assert
		var operations = builder.GetAllOperations().ToList();
		operations[0].Url.Should().Be("Products('O''Brien')");
	}

	#endregion

	#region Helper Methods

	private void SetupBatchMockResponse() => _mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(CreateBatchResponse());

	private static HttpResponseMessage CreateBatchResponse()
	{
		var batchBoundary = "batch_response";
		var content = """
			--batch_response
			Content-Type: application/http
			Content-Transfer-Encoding: binary

			HTTP/1.1 200 OK
			Content-Type: application/json

			{"ID": 1, "Name": "Test"}
			--batch_response--
			""";

		var response = new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent(content, System.Text.Encoding.UTF8, $"multipart/mixed; boundary={batchBoundary}")
		};

		return response;
	}

	#endregion

	#region Test Classes

	private sealed class TestProduct
	{
		public int Id { get; set; }

		public string Name { get; set; } = string.Empty;
	}

	private sealed class TestCategory
	{
		public int Id { get; set; }

		public string Name { get; set; } = string.Empty;
	}

	#endregion
}
