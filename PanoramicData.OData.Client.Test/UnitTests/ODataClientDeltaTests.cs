using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using PanoramicData.OData.Client.Test.Models;
using System.Net;

namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Unit tests for OData delta query (change tracking) support.
/// </summary>
public class ODataClientDeltaTests : IDisposable
{
	private readonly Mock<HttpMessageHandler> _mockHandler;
	private readonly HttpClient _httpClient;
	private readonly ODataClient _client;

	/// <summary>
	/// Initializes a new instance of the test class.
	/// </summary>
	public ODataClientDeltaTests()
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

	#region GetDeltaAsync Tests

	/// <summary>
	/// Tests that GetDeltaAsync returns added/modified entities.
	/// </summary>
	[Fact]
	public async Task GetDeltaAsync_ReturnsModifiedEntities()
	{
		// Arrange
		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("""
				{
					"value": [
						{"Id": 1, "Name": "Widget 1"},
						{"Id": 2, "Name": "Widget 2"}
					],
					"@odata.deltaLink": "https://test.odata.org/Products?$deltatoken=abc123"
				}
				""")
			});

		// Act
		var result = await _client.GetDeltaAsync<Product>("https://test.odata.org/Products?$deltatoken=initial", cancellationToken: CancellationToken.None);

		// Assert
		result.Value.Should().HaveCount(2);
		result.Value[0].Name.Should().Be("Widget 1");
		result.DeltaLink.Should().Contain("deltatoken=abc123");
	}

	/// <summary>
	/// Tests that GetDeltaAsync parses deleted entities with @removed annotation.
	/// </summary>
	[Fact]
	public async Task GetDeltaAsync_ParsesDeletedEntities_WithRemovedAnnotation()
	{
		// Arrange
		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("""
				{
					"value": [
						{"Id": 1, "Name": "Widget 1"},
						{"@odata.id": "Products(2)", "@removed": {"reason": "deleted"}},
						{"@odata.id": "Products(3)", "@removed": {"reason": "changed"}}
					],
					"@odata.deltaLink": "https://test.odata.org/Products?$deltatoken=xyz789"
				}
				""")
			});

		// Act
		var result = await _client.GetDeltaAsync<Product>("https://test.odata.org/Products?$deltatoken=abc123", cancellationToken: CancellationToken.None);

		// Assert
		result.Value.Should().ContainSingle();
		result.Value[0].Name.Should().Be("Widget 1");
		result.Deleted.Should().HaveCount(2);
		result.Deleted[0].Id.Should().Be("Products(2)");
		result.Deleted[0].Reason.Should().Be("deleted");
		result.Deleted[1].Id.Should().Be("Products(3)");
		result.Deleted[1].Reason.Should().Be("changed");
	}

	/// <summary>
	/// Tests that GetDeltaAsync parses deleted entities with @odata.removed annotation (alternative format).
	/// </summary>
	[Fact]
	public async Task GetDeltaAsync_ParsesDeletedEntities_WithODataRemovedAnnotation()
	{
		// Arrange
		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("""
				{
					"value": [
						{"@odata.id": "Products(5)", "@odata.removed": {"reason": "deleted"}}
					],
					"@odata.deltaLink": "https://test.odata.org/Products?$deltatoken=xyz789"
				}
				""")
			});

		// Act
		var result = await _client.GetDeltaAsync<Product>("https://test.odata.org/Products?$deltatoken=abc123", cancellationToken: CancellationToken.None);

		// Assert
		result.Value.Should().BeEmpty();
		result.Deleted.Should().ContainSingle();
		result.Deleted[0].Id.Should().Be("Products(5)");
	}

	/// <summary>
	/// Tests that GetDeltaAsync handles empty delta response.
	/// </summary>
	[Fact]
	public async Task GetDeltaAsync_EmptyResponse_ReturnsEmptyCollections()
	{
		// Arrange
		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("""
				{
					"value": [],
					"@odata.deltaLink": "https://test.odata.org/Products?$deltatoken=nochanges"
				}
				""")
			});

		// Act
		var result = await _client.GetDeltaAsync<Product>("https://test.odata.org/Products?$deltatoken=abc123", cancellationToken: CancellationToken.None);

		// Assert
		result.Value.Should().BeEmpty();
		result.Deleted.Should().BeEmpty();
		result.DeltaLink.Should().Contain("nochanges");
	}

	/// <summary>
	/// Tests that GetDeltaAsync parses count.
	/// </summary>
	[Fact]
	public async Task GetDeltaAsync_WithCount_ReturnsCount()
	{
		// Arrange
		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("""
				{
					"@odata.count": 42,
					"value": [{"Id": 1, "Name": "Widget"}],
					"@odata.deltaLink": "https://test.odata.org/Products?$deltatoken=xyz"
				}
				""")
			});

		// Act
		var result = await _client.GetDeltaAsync<Product>("https://test.odata.org/Products?$deltatoken=abc", cancellationToken: CancellationToken.None);

		// Assert
		result.Count.Should().Be(42);
	}

	#endregion

	#region GetAllDeltaAsync Tests

	/// <summary>
	/// Tests that GetAllDeltaAsync follows nextLink for paged delta responses.
	/// </summary>
	[Fact]
	public async Task GetAllDeltaAsync_FollowsNextLink()
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
							"value": [{"Id": 1, "Name": "Widget 1"}],
							"@odata.nextLink": "https://test.odata.org/Products?$deltatoken=abc&$skip=1"
						}
						""")
					},
					2 => new HttpResponseMessage(HttpStatusCode.OK)
					{
						Content = new StringContent("""
						{
							"value": [{"Id": 2, "Name": "Widget 2"}],
							"@odata.deltaLink": "https://test.odata.org/Products?$deltatoken=final"
						}
						""")
					},
					_ => throw new InvalidOperationException("Too many calls")
				};
			});

		// Act
		var result = await _client.GetAllDeltaAsync<Product>("https://test.odata.org/Products?$deltatoken=initial", cancellationToken: CancellationToken.None);

		// Assert
		callCount.Should().Be(2);
		result.Value.Should().HaveCount(2);
		result.DeltaLink.Should().Contain("deltatoken=final");
	}

	/// <summary>
	/// Tests that GetAllDeltaAsync aggregates deleted entities across pages.
	/// </summary>
	[Fact]
	public async Task GetAllDeltaAsync_AggregatesDeletedEntities()
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
							"value": [
								{"Id": 1, "Name": "Widget 1"},
								{"@odata.id": "Products(10)", "@removed": {"reason": "deleted"}}
							],
							"@odata.nextLink": "https://test.odata.org/Products?$skip=2"
						}
						""")
					},
					2 => new HttpResponseMessage(HttpStatusCode.OK)
					{
						Content = new StringContent("""
						{
							"value": [
								{"@odata.id": "Products(20)", "@removed": {"reason": "deleted"}}
							],
							"@odata.deltaLink": "https://test.odata.org/Products?$deltatoken=final"
						}
						""")
					},
					_ => throw new InvalidOperationException("Too many calls")
				};
			});

		// Act
		var result = await _client.GetAllDeltaAsync<Product>("https://test.odata.org/Products?$deltatoken=init", cancellationToken: CancellationToken.None);

		// Assert
		result.Value.Should().ContainSingle();
		result.Deleted.Should().HaveCount(2);
		result.Deleted[0].Id.Should().Be("Products(10)");
		result.Deleted[1].Id.Should().Be("Products(20)");
	}

	/// <summary>
	/// Tests that GetAllDeltaAsync respects cancellation token.
	/// </summary>
	[Fact]
	public async Task GetAllDeltaAsync_Cancellation_ThrowsOperationCanceled()
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
						"value": [{"Id": {{callCount}}, "Name": "Widget{{callCount}}"}],
						"@odata.nextLink": "https://test.odata.org/Products?$skip={{callCount}}"
					}
					""")
				};
			});

		// Act
		var act = async () => await _client.GetAllDeltaAsync<Product>("https://test.odata.org/Products?$deltatoken=init", cancellationToken: cts.Token);

		// Assert
		await act.Should().ThrowAsync<OperationCanceledException>();
	}

	#endregion

	#region End-to-End Delta Workflow Test

	/// <summary>
	/// Tests a complete delta workflow: Initial query with deltaLink -> Get changes.
	/// </summary>
	[Fact]
	public async Task DeltaWorkflow_InitialQueryThenDelta_WorksCorrectly()
	{
		// Arrange
		var callCount = 0;

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
					// Initial query
					return new HttpResponseMessage(HttpStatusCode.OK)
					{
						Content = new StringContent("""
						{
							"value": [{"Id": 1, "Name": "Widget"}],
							"@odata.deltaLink": "https://test.odata.org/Products?$deltatoken=v1"
						}
						""")
					};
				}
				else
				{
					// Delta query
					return new HttpResponseMessage(HttpStatusCode.OK)
					{
						Content = new StringContent("""
						{
							"value": [
								{"Id": 1, "Name": "Updated Widget"},
								{"Id": 2, "Name": "New Widget"},
								{"@odata.id": "Products(3)", "@removed": {"reason": "deleted"}}
							],
							"@odata.deltaLink": "https://test.odata.org/Products?$deltatoken=v2"
						}
						""")
					};
				}
			});

		// Act
		// Step 1: Initial query
		var query = _client.For<Product>("Products");
		var initialResponse = await _client.GetAsync(query, CancellationToken.None);
		var deltaLink = initialResponse.DeltaLink;

		// Step 2: Get changes using delta link
		var deltaResponse = await _client.GetDeltaAsync<Product>(deltaLink!, cancellationToken: CancellationToken.None);

		// Assert
		initialResponse.Value.Should().ContainSingle();
		deltaLink.Should().Contain("deltatoken=v1");

		deltaResponse.Value.Should().HaveCount(2);
		deltaResponse.Deleted.Should().ContainSingle();
		deltaResponse.DeltaLink.Should().Contain("deltatoken=v2");
	}

	#endregion
}
