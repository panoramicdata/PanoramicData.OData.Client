using System.Net;
using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using PanoramicData.OData.Client.Test.Models;

namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Unit tests for ODataClient pagination handling.
/// </summary>
public class ODataClientPaginationTests : IDisposable
{
	private readonly Mock<HttpMessageHandler> _mockHandler;
	private readonly HttpClient _httpClient;
	private readonly ODataClient _client;

	/// <summary>
	/// Initializes a new instance of the test class.
	/// </summary>
	public ODataClientPaginationTests()
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

	/// <summary>
	/// Tests GetAllAsync follows nextLink to get all pages.
	/// </summary>
	[Fact]
	public async Task GetAllAsync_FollowsNextLink()
	{
		// Arrange
		var callCount = 0;
		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(() =>
			{
				callCount++;
				return callCount switch
				{
					1 => new HttpResponseMessage(HttpStatusCode.OK)
					{
						Content = new StringContent("""
						{
							"value": [{ "ID": 1, "Name": "Item1" }],
							"@odata.nextLink": "https://test.odata.org/Products?$skip=1"
						}
						""")
					},
					2 => new HttpResponseMessage(HttpStatusCode.OK)
					{
						Content = new StringContent("""
						{
							"value": [{ "ID": 2, "Name": "Item2" }],
							"@odata.nextLink": "https://test.odata.org/Products?$skip=2"
						}
						""")
					},
					_ => new HttpResponseMessage(HttpStatusCode.OK)
					{
						Content = new StringContent("""
						{
							"value": [{ "ID": 3, "Name": "Item3" }]
						}
						""")
					}
				};
			});

		// Act
		var query = _client.For<Product>("Products");
		var response = await _client.GetAllAsync(query, CancellationToken.None);

		// Assert
		callCount.Should().Be(3);
		response.Value.Should().HaveCount(3);
		response.Value[0].Name.Should().Be("Item1");
		response.Value[1].Name.Should().Be("Item2");
		response.Value[2].Name.Should().Be("Item3");
	}

	/// <summary>
	/// Tests GetAllAsync preserves count from first response.
	/// </summary>
	[Fact]
	public async Task GetAllAsync_PreservesTotalCount()
	{
		// Arrange
		var callCount = 0;
		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(() =>
			{
				callCount++;
				return callCount switch
				{
					1 => new HttpResponseMessage(HttpStatusCode.OK)
					{
						Content = new StringContent("""
						{
							"@odata.count": 50,
							"value": [{ "ID": 1, "Name": "Item1" }],
							"@odata.nextLink": "https://test.odata.org/Products?$skip=1"
						}
						""")
					},
					_ => new HttpResponseMessage(HttpStatusCode.OK)
					{
						Content = new StringContent("""
						{
							"value": [{ "ID": 2, "Name": "Item2" }]
						}
						""")
					}
				};
			});

		// Act
		var query = _client.For<Product>("Products").Count();
		var response = await _client.GetAllAsync(query, CancellationToken.None);

		// Assert
		response.Count.Should().Be(50);
		response.Value.Should().HaveCount(2);
	}

	/// <summary>
	/// Tests GetAllAsync handles empty result set.
	/// </summary>
	[Fact]
	public async Task GetAllAsync_EmptyResult_ReturnsEmpty()
	{
		// Arrange
		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("""{"value": []}""")
			});

		// Act
		var query = _client.For<Product>("Products");
		var response = await _client.GetAllAsync(query, CancellationToken.None);

		// Assert
		response.Value.Should().BeEmpty();
	}

	/// <summary>
	/// Tests GetAllAsync respects cancellation token.
	/// </summary>
	[Fact]
	public async Task GetAllAsync_Cancellation_ThrowsOperationCanceled()
	{
		// Arrange
		using var cts = new CancellationTokenSource();
		var callCount = 0;

		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(() =>
			{
				callCount++;
				if (callCount == 2)
				{
					cts.Cancel();
				}

				return new HttpResponseMessage(HttpStatusCode.OK)
				{
					Content = new StringContent($$"""
					{
						"value": [{ "ID": {{callCount}}, "Name": "Item{{callCount}}" }],
						"@odata.nextLink": "https://test.odata.org/Products?$skip={{callCount}}"
					}
					""")
				};
			});

		// Act
		var query = _client.For<Product>("Products");
		var act = async () => await _client.GetAllAsync(query, cts.Token);

		// Assert
		await act.Should().ThrowAsync<OperationCanceledException>();
	}
}
