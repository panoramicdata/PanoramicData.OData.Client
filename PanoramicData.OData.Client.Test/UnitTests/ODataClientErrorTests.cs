using PanoramicData.OData.Client.Exceptions;

namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Unit tests for ODataClient error handling.
/// </summary>
public class ODataClientErrorTests : TestBase, IDisposable
{
	private readonly Mock<HttpMessageHandler> _mockHandler;
	private readonly HttpClient _httpClient;
	private readonly ODataClient _client;

	/// <summary>
	/// Initializes a new instance of the test class with mocked dependencies.
	/// </summary>
	public ODataClientErrorTests()
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

	#region HTTP Status Code Error Tests

	/// <summary>
	/// Tests that 404 Not Found throws ODataNotFoundException.
	/// </summary>
	[Fact]
	public async Task GetAsync_NotFound_ThrowsODataNotFoundException()
	{
		// Arrange
		SetupMockResponse(HttpStatusCode.NotFound, """{"error": {"message": "Resource not found"}}""");

		// Act
		var act = async () => await _client.GetAsync(_client.For<Product>("Products"), CancellationToken);

		// Assert
		var ex = await act.Should().ThrowAsync<ODataNotFoundException>();
		ex.Which.RequestUrl.Should().Contain("Products");
	}

	/// <summary>
	/// Tests that 401 Unauthorized throws ODataUnauthorizedException.
	/// </summary>
	[Fact]
	public async Task GetAsync_Unauthorized_ThrowsODataUnauthorizedException()
	{
		// Arrange
		SetupMockResponse(HttpStatusCode.Unauthorized, """{"error": {"message": "Authentication required"}}""");

		// Act
		var act = async () => await _client.GetAsync(_client.For<Product>("Products"), CancellationToken);

		// Assert
		var ex = await act.Should().ThrowAsync<ODataUnauthorizedException>();
		ex.Which.RequestUrl.Should().Contain("Products");
	}

	/// <summary>
	/// Tests that 403 Forbidden throws ODataForbiddenException.
	/// </summary>
	[Fact]
	public async Task GetAsync_Forbidden_ThrowsODataForbiddenException()
	{
		// Arrange
		SetupMockResponse(HttpStatusCode.Forbidden, """{"error": {"message": "Access denied"}}""");

		// Act
		var act = async () => await _client.GetAsync(_client.For<Product>("Products"), CancellationToken);

		// Assert
		var ex = await act.Should().ThrowAsync<ODataForbiddenException>();
		ex.Which.RequestUrl.Should().Contain("Products");
	}

	/// <summary>
	/// Tests that 412 Precondition Failed throws ODataConcurrencyException.
	/// </summary>
	[Fact]
	public async Task UpdateAsync_PreconditionFailed_ThrowsODataConcurrencyException()
	{
		// Arrange
		var response = new HttpResponseMessage(HttpStatusCode.PreconditionFailed)
		{
			Content = new StringContent("""{"error": {"message": "Entity was modified"}}""", System.Text.Encoding.UTF8, "application/json")
		};
		response.Headers.ETag = new System.Net.Http.Headers.EntityTagHeaderValue("\"new-etag\"");

		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(response);

		// Act
		var act = async () => await _client.UpdateAsync<Product>(
			"Products",
			1,
			new { Name = "Updated" },
			"\"old-etag\"",
			cancellationToken: CancellationToken);

		// Assert
		var ex = await act.Should().ThrowAsync<ODataConcurrencyException>();
		ex.Which.RequestUrl.Should().Contain("Products(1)");
		ex.Which.RequestETag.Should().Be("\"old-etag\"");
		ex.Which.CurrentETag.Should().Be("\"new-etag\"");
	}

	/// <summary>
	/// Tests that 500 Internal Server Error throws ODataClientException.
	/// </summary>
	[Fact]
	public async Task GetAsync_InternalServerError_ThrowsODataClientException()
	{
		// Arrange
		SetupMockResponse(HttpStatusCode.InternalServerError, """{"error": {"message": "Server error"}}""");

		// Act
		var act = async () => await _client.GetAsync(_client.For<Product>("Products"), CancellationToken);

		// Assert
		var ex = await act.Should().ThrowAsync<ODataClientException>();
		ex.Which.StatusCode.Should().Be(500);
	}

	/// <summary>
	/// Tests that 502 Bad Gateway throws ODataClientException.
	/// </summary>
	[Fact]
	public async Task GetAsync_BadGateway_ThrowsODataClientException()
	{
		// Arrange
		SetupMockResponse(HttpStatusCode.BadGateway, """{"error": {"message": "Bad gateway"}}""");

		// Act
		var act = async () => await _client.GetAsync(_client.For<Product>("Products"), CancellationToken);

		// Assert
		var ex = await act.Should().ThrowAsync<ODataClientException>();
		ex.Which.StatusCode.Should().Be(502);
	}

	/// <summary>
	/// Tests that 503 Service Unavailable throws ODataClientException.
	/// </summary>
	[Fact]
	public async Task GetAsync_ServiceUnavailable_ThrowsODataClientException()
	{
		// Arrange
		SetupMockResponse(HttpStatusCode.ServiceUnavailable, """{"error": {"message": "Service unavailable"}}""");

		// Act
		var act = async () => await _client.GetAsync(_client.For<Product>("Products"), CancellationToken);

		// Assert
		var ex = await act.Should().ThrowAsync<ODataClientException>();
		ex.Which.StatusCode.Should().Be(503);
	}

	/// <summary>
	/// Tests that 400 Bad Request throws ODataClientException.
	/// </summary>
	[Fact]
	public async Task GetAsync_BadRequest_ThrowsODataClientException()
	{
		// Arrange
		SetupMockResponse(HttpStatusCode.BadRequest, """{"error": {"message": "Invalid query"}}""");

		// Act
		var act = async () => await _client.GetAsync(_client.For<Product>("Products"), CancellationToken);

		// Assert
		var ex = await act.Should().ThrowAsync<ODataClientException>();
		ex.Which.StatusCode.Should().Be(400);
	}

	#endregion

	#region HTML Response Detection Tests

	/// <summary>
	/// Tests that HTML response in body throws ODataClientException.
	/// </summary>
	[Fact]
	public async Task GetAsync_HtmlResponseBody_ThrowsODataClientException()
	{
		// Arrange
		SetupMockResponse(HttpStatusCode.OK, "<html><head><title>Login</title></head><body>Please login</body></html>");

		// Act
		var act = async () => await _client.GetAsync(_client.For<Product>("Products"), CancellationToken);

		// Assert
		await act.Should().ThrowAsync<ODataClientException>()
			.WithMessage("*HTML*");
	}

	/// <summary>
	/// Tests that HTML login page throws ODataClientException.
	/// </summary>
	[Fact]
	public async Task GetAsync_HtmlLoginPage_ThrowsODataClientException()
	{
		// Arrange
		var htmlContent = """
			<!DOCTYPE html>
			<html>
			<head><title>Sign In</title></head>
			<body>
				<form action="/login" method="post">
					<input type="text" name="username" />
					<input type="password" name="password" />
				</form>
			</body>
			</html>
			""";
		SetupMockResponse(HttpStatusCode.OK, htmlContent);

		// Act
		var act = async () => await _client.GetAsync(_client.For<Product>("Products"), CancellationToken);

		// Assert
		await act.Should().ThrowAsync<ODataClientException>()
			.WithMessage("*HTML*");
	}

	/// <summary>
	/// Tests that XML metadata response does not throw HTML error.
	/// </summary>
	[Fact]
	public async Task GetMetadataAsync_XmlResponse_DoesNotThrowHtmlError()
	{
		// Arrange
		var xmlContent = """
			<?xml version="1.0" encoding="utf-8"?>
			<edmx:Edmx Version="4.0" xmlns:edmx="http://docs.oasis-open.org/odata/ns/edmx">
				<edmx:DataServices>
					<Schema Namespace="Test" xmlns="http://docs.oasis-open.org/odata/ns/edm">
						<EntityType Name="Product">
							<Key><PropertyRef Name="ID"/></Key>
							<Property Name="ID" Type="Edm.Int32" Nullable="false"/>
						</EntityType>
					</Schema>
				</edmx:DataServices>
			</edmx:Edmx>
			""";

		var response = new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent(xmlContent, System.Text.Encoding.UTF8, "application/xml")
		};

		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(response);

		// Act
		var metadata = await _client.GetMetadataAsync(CancellationToken);

		// Assert
		metadata.Should().NotBeNull();
	}

	#endregion

	#region Exception Properties Tests

	/// <summary>
	/// Tests that ODataClientException includes status code.
	/// </summary>
	[Fact]
	public async Task ODataClientException_IncludesStatusCode()
	{
		// Arrange
		SetupMockResponse(HttpStatusCode.BadRequest, """{"error": "Bad request"}""");

		// Act
		ODataClientException? caughtException = null;
		try
		{
			await _client.GetAsync(_client.For<Product>("Products"), CancellationToken);
		}
		catch (ODataClientException ex)
		{
			caughtException = ex;
		}

		// Assert
		caughtException.Should().NotBeNull();
		caughtException!.StatusCode.Should().Be(400);
	}

	/// <summary>
	/// Tests that ODataClientException includes response body.
	/// </summary>
	[Fact]
	public async Task ODataClientException_IncludesResponseBody()
	{
		// Arrange
		var errorBody = """{"error": {"code": "InvalidQuery", "message": "The query is invalid"}}""";
		SetupMockResponse(HttpStatusCode.BadRequest, errorBody);

		// Act
		ODataClientException? caughtException = null;
		try
		{
			await _client.GetAsync(_client.For<Product>("Products"), CancellationToken);
		}
		catch (ODataClientException ex)
		{
			caughtException = ex;
		}

		// Assert
		caughtException.Should().NotBeNull();
		caughtException!.ResponseBody.Should().Contain("InvalidQuery");
	}

	/// <summary>
	/// Tests that ODataClientException includes request URL.
	/// </summary>
	[Fact]
	public async Task ODataClientException_IncludesRequestUrl()
	{
		// Arrange
		SetupMockResponse(HttpStatusCode.BadRequest, """{"error": "Bad request"}""");

		// Act
		ODataClientException? caughtException = null;
		try
		{
			await _client.GetAsync(_client.For<Product>("Products").Filter("InvalidField eq 'test'"), CancellationToken);
		}
		catch (ODataClientException ex)
		{
			caughtException = ex;
		}

		// Assert
		caughtException.Should().NotBeNull();
		caughtException!.RequestUrl.Should().Contain("Products");
	}

	#endregion

	#region CRUD Operation Error Tests

	/// <summary>
	/// Tests that CreateAsync throws on error.
	/// </summary>
	[Fact]
	public async Task CreateAsync_Error_ThrowsODataClientException()
	{
		// Arrange
		SetupMockResponse(HttpStatusCode.BadRequest, """{"error": "Invalid entity"}""");

		// Act
		var act = async () => await _client.CreateAsync("Products", new Product { Name = "Test" }, cancellationToken: CancellationToken);

		// Assert
		await act.Should().ThrowAsync<ODataClientException>();
	}

	/// <summary>
	/// Tests that UpdateAsync throws on error.
	/// </summary>
	[Fact]
	public async Task UpdateAsync_Error_ThrowsODataClientException()
	{
		// Arrange
		SetupMockResponse(HttpStatusCode.BadRequest, """{"error": "Invalid update"}""");

		// Act
		var act = async () => await _client.UpdateAsync<Product>("Products", 1, new { Name = "Updated" }, cancellationToken: CancellationToken);

		// Assert
		await act.Should().ThrowAsync<ODataClientException>();
	}

	/// <summary>
	/// Tests that DeleteAsync throws on not found.
	/// </summary>
	[Fact]
	public async Task DeleteAsync_NotFound_ThrowsODataNotFoundException()
	{
		// Arrange
		SetupMockResponse(HttpStatusCode.NotFound, """{"error": "Entity not found"}""");

		// Act
		var act = async () => await _client.DeleteAsync("Products", 999, cancellationToken: CancellationToken);

		// Assert
		await act.Should().ThrowAsync<ODataNotFoundException>();
	}

	/// <summary>
	/// Tests that ReplaceAsync throws on error.
	/// </summary>
	[Fact]
	public async Task ReplaceAsync_Error_ThrowsODataClientException()
	{
		// Arrange
		SetupMockResponse(HttpStatusCode.BadRequest, """{"error": "Invalid entity"}""");

		// Act
		var act = async () => await _client.ReplaceAsync("Products", 1, new Product { Name = "Test" }, cancellationToken: CancellationToken);

		// Assert
		await act.Should().ThrowAsync<ODataClientException>();
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

	#endregion
}
