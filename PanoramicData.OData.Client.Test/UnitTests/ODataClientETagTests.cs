using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using PanoramicData.OData.Client.Exceptions;
using PanoramicData.OData.Client.Test.Models;
using System.Net;
using System.Net.Http.Headers;

namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Unit tests for ETag and concurrency control support.
/// </summary>
public class ODataClientETagTests : IDisposable
{
	private readonly Mock<HttpMessageHandler> _mockHandler;
	private readonly HttpClient _httpClient;
	private readonly ODataClient _client;

	/// <summary>
	/// Initializes a new instance of the test class.
	/// </summary>
	public ODataClientETagTests()
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

	#region GetByKeyWithETagAsync Tests

	/// <summary>
	/// Tests that GetByKeyWithETagAsync returns the ETag from response headers.
	/// </summary>
	[Fact]
	public async Task GetByKeyWithETagAsync_ReturnsETag()
	{
		// Arrange
		var response = new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent("""{"Id": 1, "Name": "Widget"}""")
		};
		response.Headers.ETag = new EntityTagHeaderValue("\"abc123\"", isWeak: true);

		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(response);

		// Act
		var result = await _client.GetByKeyWithETagAsync<Product, int>(1, cancellationToken: CancellationToken.None);

		// Assert
		result.Value.Should().NotBeNull();
		result.Value!.Id.Should().Be(1);
		result.ETag.Should().Be("W/\"abc123\"");
	}

	/// <summary>
	/// Tests that GetByKeyWithETagAsync works without ETag.
	/// </summary>
	[Fact]
	public async Task GetByKeyWithETagAsync_NoETag_ReturnsNullETag()
	{
		// Arrange
		var response = new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent("""{"Id": 1, "Name": "Widget"}""")
		};

		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(response);

		// Act
		var result = await _client.GetByKeyWithETagAsync<Product, int>(1, cancellationToken: CancellationToken.None);

		// Assert
		result.Value.Should().NotBeNull();
		result.ETag.Should().BeNull();
	}

	#endregion

	#region UpdateAsync with ETag Tests

	/// <summary>
	/// Tests that UpdateAsync sends If-Match header when ETag is provided.
	/// </summary>
	[Fact]
	public async Task UpdateAsync_WithETag_SendsIfMatchHeader()
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
				Content = new StringContent("""{"Id": 1, "Name": "Updated Widget"}""")
			});

		// Act
		await _client.UpdateAsync<Product>("Products", 1, new { Name = "Updated Widget" }, "W/\"abc123\"", cancellationToken: CancellationToken.None);

		// Assert
		capturedRequest.Should().NotBeNull();
		capturedRequest!.Headers.IfMatch.Should().ContainSingle();
		capturedRequest.Headers.IfMatch.First().ToString().Should().Be("W/\"abc123\"");
	}

	/// <summary>
	/// Tests that UpdateAsync without ETag does not send If-Match header.
	/// </summary>
	[Fact]
	public async Task UpdateAsync_WithoutETag_NoIfMatchHeader()
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
				Content = new StringContent("""{"Id": 1, "Name": "Updated Widget"}""")
			});

		// Act
		await _client.UpdateAsync<Product>("Products", 1, new { Name = "Updated Widget" }, cancellationToken: CancellationToken.None);

		// Assert
		capturedRequest.Should().NotBeNull();
		capturedRequest!.Headers.IfMatch.Should().BeEmpty();
	}

	/// <summary>
	/// Tests that UpdateAsync throws ODataConcurrencyException on 412 response.
	/// </summary>
	[Fact]
	public async Task UpdateAsync_ConcurrencyConflict_ThrowsODataConcurrencyException()
	{
		// Arrange
		var response = new HttpResponseMessage(HttpStatusCode.PreconditionFailed)
		{
			Content = new StringContent("""{"error": {"message": "Precondition Failed"}}""")
		};
		response.Headers.ETag = new EntityTagHeaderValue("\"def456\"", isWeak: true);

		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(response);

		// Act
		var act = async () => await _client.UpdateAsync<Product>(
			"Products", 1, new { Name = "Updated" }, "W/\"abc123\"", cancellationToken: CancellationToken.None);

		// Assert
		var exception = await act.Should().ThrowAsync<ODataConcurrencyException>();
		exception.Which.RequestETag.Should().Be("W/\"abc123\"");
		exception.Which.CurrentETag.Should().Be("W/\"def456\"");
		exception.Which.StatusCode.Should().Be(412);
	}

	#endregion

	#region DeleteAsync with ETag Tests

	/// <summary>
	/// Tests that DeleteAsync sends If-Match header when ETag is provided.
	/// </summary>
	[Fact]
	public async Task DeleteAsync_WithETag_SendsIfMatchHeader()
	{
		// Arrange
		HttpRequestMessage? capturedRequest = null;

		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NoContent));

		// Act
		await _client.DeleteAsync("Products", 1, "W/\"abc123\"", cancellationToken: CancellationToken.None);

		// Assert
		capturedRequest.Should().NotBeNull();
		capturedRequest!.Headers.IfMatch.Should().ContainSingle();
		capturedRequest.Headers.IfMatch.First().ToString().Should().Be("W/\"abc123\"");
	}

	/// <summary>
	/// Tests that DeleteAsync throws ODataConcurrencyException on 412 response.
	/// </summary>
	[Fact]
	public async Task DeleteAsync_ConcurrencyConflict_ThrowsODataConcurrencyException()
	{
		// Arrange
		var response = new HttpResponseMessage(HttpStatusCode.PreconditionFailed)
		{
			Content = new StringContent("""{"error": {"message": "Precondition Failed"}}""")
		};

		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(response);

		// Act
		var act = async () => await _client.DeleteAsync("Products", 1, "W/\"abc123\"", cancellationToken: CancellationToken.None);

		// Assert
		await act.Should().ThrowAsync<ODataConcurrencyException>();
	}

	#endregion

	#region GetAsync ETag Tests

	/// <summary>
	/// Tests that GetAsync collection response includes ETag when present.
	/// </summary>
	[Fact]
	public async Task GetAsync_WithETag_ReturnsETagInResponse()
	{
		// Arrange
		var response = new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent("""{"value": [{"Id": 1, "Name": "Widget"}]}""")
		};
		response.Headers.ETag = new EntityTagHeaderValue("\"collection123\"", isWeak: true);

		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(response);

		// Act
		var query = _client.For<Product>("Products").Top(10);
		var result = await _client.GetAsync(query, CancellationToken.None);

		// Assert
		result.ETag.Should().Be("W/\"collection123\"");
		result.Value.Should().ContainSingle();
	}

	#endregion

	#region Optimistic Concurrency Workflow Test

	/// <summary>
	/// Tests a complete optimistic concurrency workflow: Get with ETag -> Update with ETag.
	/// </summary>
	[Fact]
	public async Task OptimisticConcurrency_GetThenUpdate_WorksCorrectly()
	{
		// Arrange
		var callCount = 0;
		HttpRequestMessage? updateRequest = null;

		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync((HttpRequestMessage req, CancellationToken _) =>
			{
				callCount++;
				if (callCount == 1)
				{
					// GET response
					var getResponse = new HttpResponseMessage(HttpStatusCode.OK)
					{
						Content = new StringContent("""{"Id": 1, "Name": "Widget", "Price": 100}""")
					};
					getResponse.Headers.ETag = new EntityTagHeaderValue("\"v1\"", isWeak: true);
					return getResponse;
				}
				else
				{
					// PATCH response
					updateRequest = req;
					return new HttpResponseMessage(HttpStatusCode.OK)
					{
						Content = new StringContent("""{"Id": 1, "Name": "Updated Widget", "Price": 100}""")
					};
				}
			});

		// Act - Get the entity with ETag
		var entityWithETag = await _client.GetByKeyWithETagAsync<Product, int>(1, cancellationToken: CancellationToken.None);
		var etag = entityWithETag.ETag;

		// Update using the ETag
		var updated = await _client.UpdateAsync<Product>("Products", 1, new { Name = "Updated Widget" }, etag, cancellationToken: CancellationToken.None);

		// Assert
		etag.Should().Be("W/\"v1\"");
		updateRequest.Should().NotBeNull();
		updateRequest!.Headers.IfMatch.Should().ContainSingle();
		updateRequest.Headers.IfMatch.First().ToString().Should().Be("W/\"v1\"");
		updated.Name.Should().Be("Updated Widget");
	}

	#endregion
}
