using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using System.Net;

namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Unit tests for legacy compatibility methods.
/// </summary>
public class ODataClientLegacyTests : IDisposable
{
	private readonly Mock<HttpMessageHandler> _mockHandler;
	private readonly HttpClient _httpClient;
	private readonly ODataClient _client;

	/// <summary>
	/// Initializes a new instance of the test class.
	/// </summary>
	public ODataClientLegacyTests()
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
	/// Tests that For(string) creates a dynamic query builder.
	/// </summary>
	[Fact]
	public void For_WithEntitySetName_CreatesDynamicQueryBuilder()
	{
#pragma warning disable CS0618 // Type or member is obsolete
		var builder = _client.For("Products");
#pragma warning restore CS0618

		var url = builder.BuildUrl();
		url.Should().Be("Products");
	}

	/// <summary>
	/// Tests that For(string) with filter works correctly.
	/// </summary>
	[Fact]
	public void For_WithFilter_BuildsCorrectUrl()
	{
#pragma warning disable CS0618 // Type or member is obsolete
		var builder = _client.For("Products")
			.Filter("Price gt 100")
			.Top(10);
#pragma warning restore CS0618

		var url = builder.BuildUrl();
		url.Should().Contain("Products");
		url.Should().Contain("$filter=");
		url.Should().Contain("$top=10");
	}

	/// <summary>
	/// Tests that FindEntriesAsync returns entries as dictionaries.
	/// </summary>
	[Fact]
	public async Task FindEntriesAsync_ReturnsEntriesAsDictionaries()
	{
		// Arrange
		var responseJson = """
		{
			"value": [
				{ "ID": 1, "Name": "Product 1", "Price": 10.00 },
				{ "ID": 2, "Name": "Product 2", "Price": 20.00 }
			]
		}
		""";

		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(responseJson)
			});

		// Act
#pragma warning disable CS0618 // Type or member is obsolete
		var entries = await _client.FindEntriesAsync("Products", CancellationToken.None);
#pragma warning restore CS0618

		// Assert
		var entriesList = entries.ToList();
		entriesList.Should().HaveCount(2);
		entriesList[0]["ID"].Should().Be(1L);
		entriesList[0]["Name"].Should().Be("Product 1");
		entriesList[1]["ID"].Should().Be(2L);
		entriesList[1]["Name"].Should().Be("Product 2");
	}

	/// <summary>
	/// Tests that FindEntryAsync returns a single entry as a dictionary.
	/// </summary>
	[Fact]
	public async Task FindEntryAsync_ReturnsSingleEntryAsDictionary()
	{
		// Arrange
		var responseJson = """
		{
			"ID": 1,
			"Name": "Product 1",
			"Price": 10.00
		}
		""";

		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(responseJson)
			});

		// Act
#pragma warning disable CS0618 // Type or member is obsolete
		var entry = await _client.FindEntryAsync("Products(1)", CancellationToken.None);
#pragma warning restore CS0618

		// Assert
		entry.Should().NotBeNull();
		entry!["ID"].Should().Be(1L);
		entry["Name"].Should().Be("Product 1");
	}

	/// <summary>
	/// Tests that FindEntryAsync returns first entry from collection response.
	/// </summary>
	[Fact]
	public async Task FindEntryAsync_FromCollection_ReturnsFirstEntry()
	{
		// Arrange
		var responseJson = """
		{
			"value": [
				{ "ID": 1, "Name": "Product 1" }
			]
		}
		""";

		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(responseJson)
			});

		// Act
#pragma warning disable CS0618 // Type or member is obsolete
		var entry = await _client.FindEntryAsync("Products?$top=1", CancellationToken.None);
#pragma warning restore CS0618

		// Assert
		entry.Should().NotBeNull();
		entry!["ID"].Should().Be(1L);
	}

	/// <summary>
	/// Tests that FindEntryAsync returns null for empty collection.
	/// </summary>
	[Fact]
	public async Task FindEntryAsync_EmptyCollection_ReturnsNull()
	{
		// Arrange
		var responseJson = """{ "value": [] }""";

		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(responseJson)
			});

		// Act
#pragma warning disable CS0618 // Type or member is obsolete
		var entry = await _client.FindEntryAsync("Products?$filter=ID eq 999", CancellationToken.None);
#pragma warning restore CS0618

		// Assert
		entry.Should().BeNull();
	}

	/// <summary>
	/// Tests that ExecuteRawQueryAsync returns ODataRawResponse.
	/// </summary>
	[Fact]
	public async Task ExecuteRawQueryAsync_ReturnsRawResponse()
	{
		// Arrange
		var responseJson = """
		{
			"value": [
				{ "ID": 1, "Name": "Product 1" }
			]
		}
		""";

		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(responseJson)
			});

		// Act
#pragma warning disable CS0618 // Type or member is obsolete
		using var response = await _client.ExecuteRawQueryAsync("Products", CancellationToken.None);
#pragma warning restore CS0618

		// Assert
		response.Should().NotBeNull();
		response.Document.Should().NotBeNull();
		
		var entries = response.GetEntries().ToList();
		entries.Should().HaveCount(1);
	}

	/// <summary>
	/// Tests that ClearODataClientMetaDataCache can be called (static method).
	/// </summary>
	[Fact]
	public void ClearODataClientMetaDataCache_CanBeCalled()
	{
		// This is a static method for backward compatibility
		// It doesn't actually do anything useful since it can't access instance state
#pragma warning disable CS0618 // Type or member is obsolete
		ODataClient.ClearODataClientMetaDataCache();
#pragma warning restore CS0618

		// No exception means success
	}
}
