namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Unit tests for ODataClient async long-running operations.
/// </summary>
public class ODataClientAsyncTests : TestBase, IDisposable
{
	private readonly Mock<HttpMessageHandler> _mockHandler;
	private readonly HttpClient _httpClient;
	private readonly ODataClient _client;

	/// <summary>
	/// Initializes a new instance of the test class with mocked dependencies.
	/// </summary>
	public ODataClientAsyncTests()
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

	#region CallActionAsyncWithPreferAsync Tests

	/// <summary>
	/// Tests CallActionAsyncWithPreferAsync when server completes synchronously.
	/// </summary>
	[Fact]
	public async Task CallActionAsyncWithPreferAsync_SyncCompletion_ReturnsResult()
	{
		// Arrange
		var responseJson = """{"Success": true, "Value": 42}""";
		SetupMockResponse(HttpStatusCode.OK, responseJson);

		// Act
		var result = await _client.CallActionAsyncWithPreferAsync<ActionResult>(
			"Products(1)/DoWork",
			new { Param = "test" },
			cancellationToken: CancellationToken);

		// Assert
		result.IsAsync.Should().BeFalse();
		result.SynchronousResult.Should().NotBeNull();
		result.SynchronousResult!.Success.Should().BeTrue();
		result.SynchronousResult.Value.Should().Be(42);
		result.AsyncOperation.Should().BeNull();
	}

	/// <summary>
	/// Tests CallActionAsyncWithPreferAsync when server returns 202 Accepted.
	/// </summary>
	[Fact]
	public async Task CallActionAsyncWithPreferAsync_AsyncAccepted_ReturnsAsyncOperation()
	{
		// Arrange
		var response = new HttpResponseMessage(HttpStatusCode.Accepted);
		response.Headers.Location = new Uri("https://test.odata.org/async-monitor/12345");

		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(response);

		// Act
		var result = await _client.CallActionAsyncWithPreferAsync<ActionResult>(
			"Products(1)/LongRunningAction",
			cancellationToken: CancellationToken);

		// Assert
		result.IsAsync.Should().BeTrue();
		result.AsyncOperation.Should().NotBeNull();
		result.SynchronousResult.Should().BeNull();
	}

	/// <summary>
	/// Tests CallActionAsyncWithPreferAsync throws when 202 returned without Location header.
	/// </summary>
	[Fact]
	public async Task CallActionAsyncWithPreferAsync_AsyncWithoutLocation_ThrowsInvalidOperationException()
	{
		// Arrange
		var response = new HttpResponseMessage(HttpStatusCode.Accepted);
		// No Location header

		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(response);

		// Act
		var act = async () => await _client.CallActionAsyncWithPreferAsync<ActionResult>(
			"Products(1)/LongRunningAction",
			cancellationToken: CancellationToken);

		// Assert
		await act.Should().ThrowAsync<InvalidOperationException>()
			.WithMessage("*monitor URL*");
	}

	/// <summary>
	/// Tests CallActionAsyncWithPreferAsync sends Prefer header.
	/// </summary>
	[Fact]
	public async Task CallActionAsyncWithPreferAsync_SendsPreferHeader()
	{
		// Arrange
		HttpRequestMessage? capturedRequest = null;
		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("""{}""", System.Text.Encoding.UTF8, "application/json")
			});

		// Act
		await _client.CallActionAsyncWithPreferAsync<ActionResult>("Products(1)/Action", cancellationToken: CancellationToken);

		// Assert
		capturedRequest.Should().NotBeNull();
		capturedRequest!.Headers.TryGetValues("Prefer", out var preferValues).Should().BeTrue();
		preferValues.Should().Contain("respond-async");
	}

	/// <summary>
	/// Tests CallActionAsyncWithPreferAsync handles 204 No Content.
	/// </summary>
	[Fact]
	public async Task CallActionAsyncWithPreferAsync_NoContent_ReturnsDefaultResult()
	{
		// Arrange
		SetupMockResponse(HttpStatusCode.NoContent, "");

		// Act
		var result = await _client.CallActionAsyncWithPreferAsync<ActionResult>(
			"Products(1)/VoidAction",
			cancellationToken: CancellationToken);

		// Assert
		result.IsAsync.Should().BeFalse();
		result.SynchronousResult.Should().BeNull();
	}

	#endregion

	#region CallActionAndWaitAsync Tests

	/// <summary>
	/// Tests CallActionAndWaitAsync returns result for synchronous completion.
	/// </summary>
	[Fact]
	public async Task CallActionAndWaitAsync_SyncCompletion_ReturnsResult()
	{
		// Arrange
		var responseJson = """{"Success": true, "Value": 100}""";
		SetupMockResponse(HttpStatusCode.OK, responseJson);

		// Act
		var result = await _client.CallActionAndWaitAsync<ActionResult>(
			"Products(1)/QuickAction",
			cancellationToken: CancellationToken);

		// Assert
		result.Should().NotBeNull();
		result!.Success.Should().BeTrue();
		result.Value.Should().Be(100);
	}

	#endregion

	#region ExecuteBatchAsyncWithPreferAsync Tests

	/// <summary>
	/// Tests ExecuteBatchAsyncWithPreferAsync sends Prefer header.
	/// </summary>
	[Fact]
	public async Task ExecuteBatchAsyncWithPreferAsync_SendsPreferHeader()
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

		var batch = _client.CreateBatch()
			.Get<TestProduct>("Products", 1);

		// Act
		await _client.ExecuteBatchAsyncWithPreferAsync(batch, cancellationToken: CancellationToken);

		// Assert
		capturedRequest.Should().NotBeNull();
		capturedRequest!.Headers.TryGetValues("Prefer", out var preferValues).Should().BeTrue();
		preferValues.Should().Contain("respond-async");
	}

	/// <summary>
	/// Tests ExecuteBatchAsyncWithPreferAsync when server completes synchronously.
	/// </summary>
	[Fact]
	public async Task ExecuteBatchAsyncWithPreferAsync_SyncCompletion_ReturnsBatchResponse()
	{
		// Arrange
		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(CreateBatchResponse());

		var batch = _client.CreateBatch()
			.Get<TestProduct>("Products", 1);

		// Act
		var result = await _client.ExecuteBatchAsyncWithPreferAsync(batch, cancellationToken: CancellationToken);

		// Assert
		result.IsAsync.Should().BeFalse();
		result.SynchronousResult.Should().NotBeNull();
	}

	/// <summary>
	/// Tests ExecuteBatchAsyncWithPreferAsync when server returns 202.
	/// </summary>
	[Fact]
	public async Task ExecuteBatchAsyncWithPreferAsync_AsyncAccepted_ReturnsAsyncOperation()
	{
		// Arrange
		var response = new HttpResponseMessage(HttpStatusCode.Accepted);
		response.Headers.Location = new Uri("https://test.odata.org/async-batch/abc");

		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(response);

		var batch = _client.CreateBatch()
			.Get<TestProduct>("Products", 1);

		// Act
		var result = await _client.ExecuteBatchAsyncWithPreferAsync(batch, cancellationToken: CancellationToken);

		// Assert
		result.IsAsync.Should().BeTrue();
		result.AsyncOperation.Should().NotBeNull();
	}

	/// <summary>
	/// Tests ExecuteBatchAsyncWithPreferAsync throws when 202 without Location.
	/// </summary>
	[Fact]
	public async Task ExecuteBatchAsyncWithPreferAsync_AsyncWithoutLocation_ThrowsInvalidOperationException()
	{
		// Arrange
		var response = new HttpResponseMessage(HttpStatusCode.Accepted);
		// No Location header

		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(response);

		var batch = _client.CreateBatch()
			.Get<TestProduct>("Products", 1);

		// Act
		var act = async () => await _client.ExecuteBatchAsyncWithPreferAsync(batch, cancellationToken: CancellationToken);

		// Assert
		await act.Should().ThrowAsync<InvalidOperationException>()
			.WithMessage("*monitor URL*");
	}

	#endregion

	#region ODataAsyncOperationResult Tests

	/// <summary>
	/// Tests ODataAsyncOperationResult IsAsync property.
	/// </summary>
	[Fact]
	public void ODataAsyncOperationResult_IsAsync_ReflectsState()
	{
		// Arrange
		var asyncResult = new ODataAsyncOperationResult<ActionResult> { IsAsync = true };
		var syncResult = new ODataAsyncOperationResult<ActionResult> { IsAsync = false };

		// Assert
		asyncResult.IsAsync.Should().BeTrue();
		syncResult.IsAsync.Should().BeFalse();
	}

	/// <summary>
	/// Tests ODataAsyncOperationResult SynchronousResult property.
	/// </summary>
	[Fact]
	public void ODataAsyncOperationResult_SynchronousResult_HoldsValue()
	{
		// Arrange
		var expected = new ActionResult { Success = true, Value = 42 };
		var result = new ODataAsyncOperationResult<ActionResult>
		{
			IsAsync = false,
			SynchronousResult = expected
		};

		// Assert
		result.SynchronousResult.Should().BeSameAs(expected);
	}

	#endregion

	#region Helper Methods

	private void SetupMockResponse(HttpStatusCode statusCode, string content) => _mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(new HttpResponseMessage(statusCode)
			{
				Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json")
			});

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
			Content = new StringContent(content, System.Text.Encoding.UTF8, "multipart/mixed")
		};
		response.Content.Headers.ContentType!.Parameters.Add(
			new System.Net.Http.Headers.NameValueHeaderValue("boundary", batchBoundary));

		return response;
	}

	#endregion

	#region Test Classes

	private sealed class ActionResult
	{
		public bool Success { get; set; }

		public int Value { get; set; }
	}

	private sealed class TestProduct
	{
		public int Id { get; set; }

		public string Name { get; set; } = string.Empty;
	}

	#endregion
}
