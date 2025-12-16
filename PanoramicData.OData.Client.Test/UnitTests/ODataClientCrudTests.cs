using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using PanoramicData.OData.Client.Exceptions;
using PanoramicData.OData.Client.Test.Models;

namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Unit tests for ODataClient CRUD operations with mocked HttpClient.
/// </summary>
public class ODataClientCrudTests : TestBase, IDisposable
{
	private readonly Mock<HttpMessageHandler> _mockHandler;
	private readonly HttpClient _httpClient;
	private readonly ODataClient _client;

	/// <summary>
	/// Initializes a new instance of the test class with mocked dependencies.
	/// </summary>
	public ODataClientCrudTests()
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

	#region GetAsync Tests

	/// <summary>
	/// Tests GetAsync returns entities from response.
	/// </summary>
	[Fact]
	public async Task GetAsync_ReturnsEntities()
	{
		// Arrange
		var responseJson = """
		{
			"@odata.context": "https://test.odata.org/$metadata#Products",
			"value": [
				{ "ID": 1, "Name": "Widget", "Price": 99.99 },
				{ "ID": 2, "Name": "Gadget", "Price": 149.99 }
			]
		}
		""";
		SetupMockResponse(HttpStatusCode.OK, responseJson);

		// Act
		var query = _client.For<Product>("Products");
		var response = await _client.GetAsync(query, CancellationToken);

		// Assert
		response.Value.Should().HaveCount(2);
		response.Value[0].Name.Should().Be("Widget");
		response.Value[1].Name.Should().Be("Gadget");
	}

	/// <summary>
	/// Tests GetAsync parses @odata.count.
	/// </summary>
	[Fact]
	public async Task GetAsync_ParsesODataCount()
	{
		// Arrange
		var responseJson = """
		{
			"@odata.context": "https://test.odata.org/$metadata#Products",
			"@odata.count": 100,
			"value": [
				{ "ID": 1, "Name": "Widget", "Price": 99.99 }
			]
		}
		""";
		SetupMockResponse(HttpStatusCode.OK, responseJson);

		// Act
		var query = _client.For<Product>("Products").Count();
		var response = await _client.GetAsync(query, CancellationToken);

		// Assert
		response.Count.Should().Be(100);
	}

	/// <summary>
	/// Tests GetAsync parses @odata.nextLink.
	/// </summary>
	[Fact]
	public async Task GetAsync_ParsesNextLink()
	{
		// Arrange
		var responseJson = """
		{
			"@odata.context": "https://test.odata.org/$metadata#Products",
			"@odata.nextLink": "https://test.odata.org/Products?$skip=10",
			"value": [
				{ "ID": 1, "Name": "Widget", "Price": 99.99 }
			]
		}
		""";
		SetupMockResponse(HttpStatusCode.OK, responseJson);

		// Act
		var query = _client.For<Product>("Products");
		var response = await _client.GetAsync(query, CancellationToken);

		// Assert
		response.NextLink.Should().Be("https://test.odata.org/Products?$skip=10");
	}

	/// <summary>
	/// Tests GetAsync parses @odata.deltaLink.
	/// </summary>
	[Fact]
	public async Task GetAsync_ParsesDeltaLink()
	{
		// Arrange
		var responseJson = """
		{
			"@odata.context": "https://test.odata.org/$metadata#Products",
			"@odata.deltaLink": "https://test.odata.org/Products?$deltatoken=abc123",
			"value": []
		}
		""";
		SetupMockResponse(HttpStatusCode.OK, responseJson);

		// Act
		var query = _client.For<Product>("Products");
		var response = await _client.GetAsync(query, CancellationToken);

		// Assert
		response.DeltaLink.Should().Be("https://test.odata.org/Products?$deltatoken=abc123");
	}

	#endregion

	#region GetByKeyAsync Tests

	/// <summary>
	/// Tests GetByKeyAsync returns single entity.
	/// </summary>
	[Fact]
	public async Task GetByKeyAsync_ReturnsSingleEntity()
	{
		// Arrange
		var responseJson = """{ "ID": 1, "Name": "Widget", "Price": 99.99 }""";
		SetupMockResponse(HttpStatusCode.OK, responseJson);

		// Act
		var product = await _client.GetByKeyAsync<Product, int>(1, cancellationToken: CancellationToken);

		// Assert
		product.Should().NotBeNull();
		product!.Id.Should().Be(1);
		product.Name.Should().Be("Widget");
	}

	/// <summary>
	/// Tests GetByKeyAsync throws ODataNotFoundException for 404.
	/// </summary>
	[Fact]
	public async Task GetByKeyAsync_NotFound_ThrowsODataNotFoundException()
	{
		// Arrange
		SetupMockResponse(HttpStatusCode.NotFound, "{}");

		// Act
		var act = async () => await _client.GetByKeyAsync<Product, int>(999, cancellationToken: CancellationToken);

		// Assert
		await act.Should().ThrowAsync<ODataNotFoundException>();
	}

	#endregion

	#region CreateAsync Tests

	/// <summary>
	/// Tests CreateAsync sends POST and returns created entity.
	/// </summary>
	[Fact]
	public async Task CreateAsync_SendsPost_ReturnsCreatedEntity()
	{
		// Arrange
		var responseJson = """{ "ID": 10, "Name": "NewWidget", "Price": 50.00 }""";
		SetupMockResponse(HttpStatusCode.Created, responseJson);

		var newProduct = new Product { Name = "NewWidget", Price = 50.00m };

		// Act
		var created = await _client.CreateAsync("Products", newProduct, cancellationToken: CancellationToken);

		// Assert
		created.Id.Should().Be(10);
		created.Name.Should().Be("NewWidget");
		VerifyRequest(HttpMethod.Post, "Products");
	}

	#endregion

	#region UpdateAsync Tests

	/// <summary>
	/// Tests UpdateAsync sends PATCH and returns updated entity.
	/// </summary>
	[Fact]
	public async Task UpdateAsync_SendsPatch_ReturnsUpdatedEntity()
	{
		// Arrange
		var responseJson = """{ "ID": 1, "Name": "Widget", "Price": 150.00 }""";
		SetupMockResponse(HttpStatusCode.OK, responseJson);

		// Act
		var updated = await _client.UpdateAsync<Product>("Products", 1, new { Price = 150.00 }, cancellationToken: CancellationToken);

		// Assert
		updated.Price.Should().Be(150.00m);
		VerifyRequest(new HttpMethod("PATCH"), "Products(1)");
	}

	/// <summary>
	/// Tests UpdateAsync handles 204 No Content by fetching entity.
	/// </summary>
	[Fact]
	public async Task UpdateAsync_NoContent_FetchesUpdatedEntity()
	{
		// Arrange - First call returns 204, second call returns entity
		var sequence = new Queue<(HttpStatusCode, string)>();
		sequence.Enqueue((HttpStatusCode.NoContent, ""));
		sequence.Enqueue((HttpStatusCode.OK, """{ "ID": 1, "Name": "Widget", "Price": 150.00 }"""));

		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(() =>
			{
				var (status, content) = sequence.Dequeue();
				return new HttpResponseMessage(status)
				{
					Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json")
				};
			});

		// Act
		var updated = await _client.UpdateAsync<Product>("Products", 1, new { Price = 150.00 }, cancellationToken: CancellationToken);

		// Assert
		updated.Price.Should().Be(150.00m);
	}

	#endregion

	#region DeleteAsync Tests

	/// <summary>
	/// Tests DeleteAsync sends DELETE request.
	/// </summary>
	[Fact]
	public async Task DeleteAsync_SendsDelete()
	{
		// Arrange
		SetupMockResponse(HttpStatusCode.NoContent, "");

		// Act
		await _client.DeleteAsync("Products", 1, cancellationToken: CancellationToken);

		// Assert
		VerifyRequest(HttpMethod.Delete, "Products(1)");
	}

	#endregion

	#region Error Handling Tests

	/// <summary>
	/// Tests 401 throws ODataUnauthorizedException.
	/// </summary>
	[Fact]
	public async Task Request_Unauthorized_ThrowsODataUnauthorizedException()
	{
		// Arrange
		SetupMockResponse(HttpStatusCode.Unauthorized, """{"error": "Unauthorized"}""");

		// Act
		var act = async () => await _client.GetAsync(_client.For<Product>("Products"), CancellationToken);

		// Assert
		await act.Should().ThrowAsync<ODataUnauthorizedException>();
	}

	/// <summary>
	/// Tests 403 throws ODataForbiddenException.
	/// </summary>
	[Fact]
	public async Task Request_Forbidden_ThrowsODataForbiddenException()
	{
		// Arrange
		SetupMockResponse(HttpStatusCode.Forbidden, """{"error": "Forbidden"}""");

		// Act
		var act = async () => await _client.GetAsync(_client.For<Product>("Products"), CancellationToken);

		// Assert
		await act.Should().ThrowAsync<ODataForbiddenException>();
	}

	/// <summary>
	/// Tests 500 throws ODataClientException.
	/// </summary>
	[Fact]
	public async Task Request_ServerError_ThrowsODataClientException()
	{
		// Arrange
		SetupMockResponse(HttpStatusCode.InternalServerError, """{"error": "Server Error"}""");

		// Act
		var act = async () => await _client.GetAsync(_client.For<Product>("Products"), CancellationToken);

		// Assert
		var ex = await act.Should().ThrowAsync<ODataClientException>();
		ex.Which.StatusCode.Should().Be(500);
	}

	/// <summary>
	/// Tests HTML response throws ODataClientException.
	/// </summary>
	[Fact]
	public async Task Request_HtmlResponse_ThrowsODataClientException()
	{
		// Arrange
		SetupMockResponse(HttpStatusCode.OK, "<html><body>Login Page</body></html>");

		// Act
		var act = async () => await _client.GetAsync(_client.For<Product>("Products"), CancellationToken);

		// Assert
		await act.Should().ThrowAsync<ODataClientException>()
			.WithMessage("*HTML*");
	}

	#endregion

	#region Function/Action Tests

	/// <summary>
	/// Tests CallFunctionAsync parses value property.
	/// </summary>
	[Fact]
	public async Task CallFunctionAsync_ParsesValueProperty()
	{
		// Arrange
		var responseJson = """
		{
			"@odata.context": "https://test.odata.org/$metadata#Products",
			"value": [
				{ "ID": 1, "Name": "Result1" }
			]
		}
		""";
		SetupMockResponse(HttpStatusCode.OK, responseJson);

		// Act
		var query = _client.For<Product>("Products")
			.Function("SearchProducts", new { SearchTerm = "test" });
		var result = await _client.CallFunctionAsync<Product, List<Product>>(query, CancellationToken);

		// Assert
		result.Should().ContainSingle();
		result![0].Name.Should().Be("Result1");
	}

	/// <summary>
	/// Tests CallActionAsync sends POST request.
	/// </summary>
	[Fact]
	public async Task CallActionAsync_SendsPost()
	{
		// Arrange
		var responseJson = """{ "Success": true }""";
		SetupMockResponse(HttpStatusCode.OK, responseJson);

		// Act
		var result = await _client.CallActionAsync<JsonDocument>(
			"Products(1)/DoSomething",
			new { Param1 = "value" },
			cancellationToken: CancellationToken);

		// Assert
		result.Should().NotBeNull();
		VerifyRequest(HttpMethod.Post, "Products(1)/DoSomething");
	}

	/// <summary>
	/// Tests CallActionAsync handles 204 No Content.
	/// </summary>
	[Fact]
	public async Task CallActionAsync_NoContent_ReturnsDefault()
	{
		// Arrange
		SetupMockResponse(HttpStatusCode.NoContent, "");

		// Act
		var result = await _client.CallActionAsync<object>("Products(1)/DoSomething", cancellationToken: CancellationToken);

		// Assert
		result.Should().BeNull();
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

	private void VerifyRequest(HttpMethod method, string url) => _mockHandler.Protected().Verify(
			"SendAsync",
			Times.AtLeastOnce(),
			ItExpr.Is<HttpRequestMessage>(r =>
				r.Method == method &&
				r.RequestUri!.ToString().Contains(url)),
			ItExpr.IsAny<CancellationToken>());

	#endregion
}
