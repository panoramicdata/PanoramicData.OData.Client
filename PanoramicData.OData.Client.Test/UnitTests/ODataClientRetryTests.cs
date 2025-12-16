using System.Net;
using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using PanoramicData.OData.Client.Exceptions;
using PanoramicData.OData.Client.Test.Models;

namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Unit tests for ODataClient retry logic.
/// </summary>
public class ODataClientRetryTests : TestBase, IDisposable
{
	private readonly Mock<HttpMessageHandler> _mockHandler;
	private readonly HttpClient _httpClient;

	/// <summary>
	/// Initializes a new instance of the test class.
	/// </summary>
	public ODataClientRetryTests()
	{
		_mockHandler = new Mock<HttpMessageHandler>();
		_httpClient = new HttpClient(_mockHandler.Object)
		{
			BaseAddress = new Uri("https://test.odata.org/")
		};
	}

	/// <inheritdoc/>
	public void Dispose()
	{
		_httpClient.Dispose();
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Tests that client retries on 500 error.
	/// </summary>
	[Fact]
	public async Task Request_ServerError_Retries()
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
				if (callCount < 3)
				{
					return new HttpResponseMessage(HttpStatusCode.InternalServerError)
					{
						Content = new StringContent("{}")
					};
				}

				return new HttpResponseMessage(HttpStatusCode.OK)
				{
					Content = new StringContent("""{"value":[]}""")
				};
			});

		using var client = new ODataClient(new ODataClientOptions
		{
			BaseUrl = "https://test.odata.org/",
			HttpClient = _httpClient,
			Logger = NullLogger.Instance,
			RetryCount = 3,
			RetryDelay = TimeSpan.FromMilliseconds(1)
		});

		// Act
		var response = await client.GetAsync(client.For<Product>("Products"), CancellationToken);

		// Assert
		callCount.Should().Be(3);
		response.Should().NotBeNull();
	}

	/// <summary>
	/// Tests that client does not retry on 4xx errors.
	/// </summary>
	[Fact]
	public async Task Request_ClientError_DoesNotRetry()
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
				return new HttpResponseMessage(HttpStatusCode.BadRequest)
				{
					Content = new StringContent("{}")
				};
			});

		using var client = new ODataClient(new ODataClientOptions
		{
			BaseUrl = "https://test.odata.org/",
			HttpClient = _httpClient,
			Logger = NullLogger.Instance,
			RetryCount = 3,
			RetryDelay = TimeSpan.FromMilliseconds(1)
		});

		// Act
		var act = async () => await client.GetAsync(client.For<Product>("Products"), CancellationToken);

		// Assert
		await act.Should().ThrowAsync<ODataClientException>();
		callCount.Should().Be(1); // No retries for 4xx
	}

	/// <summary>
	/// Tests that client retries on transient HTTP exceptions.
	/// </summary>
	[Fact]
	public async Task Request_TransientException_Retries()
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
				if (callCount < 2)
				{
					throw new HttpRequestException("Connection failed");
				}

				return new HttpResponseMessage(HttpStatusCode.OK)
				{
					Content = new StringContent("""{"value":[]}""")
				};
			});

		using var client = new ODataClient(new ODataClientOptions
		{
			BaseUrl = "https://test.odata.org/",
			HttpClient = _httpClient,
			Logger = NullLogger.Instance,
			RetryCount = 3,
			RetryDelay = TimeSpan.FromMilliseconds(1)
		});

		// Act
		var response = await client.GetAsync(client.For<Product>("Products"), CancellationToken);

		// Assert
		callCount.Should().Be(2);
		response.Should().NotBeNull();
	}

	/// <summary>
	/// Tests that client gives up after max retries.
	/// </summary>
	[Fact]
	public async Task Request_MaxRetries_ThrowsException()
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
				return new HttpResponseMessage(HttpStatusCode.InternalServerError)
				{
					Content = new StringContent("{}")
				};
			});

		using var client = new ODataClient(new ODataClientOptions
		{
			BaseUrl = "https://test.odata.org/",
			HttpClient = _httpClient,
			Logger = NullLogger.Instance,
			RetryCount = 2,
			RetryDelay = TimeSpan.FromMilliseconds(1)
		});

		// Act
		var act = async () => await client.GetAsync(client.For<Product>("Products"), CancellationToken);

		// Assert
		await act.Should().ThrowAsync<ODataClientException>();
		callCount.Should().Be(3); // Initial + 2 retries
	}

	/// <summary>
	/// Tests that client respects retry delay.
	/// </summary>
	[Fact]
	public async Task Request_RetryDelay_IsRespected()
	{
		// Arrange
		var callTimes = new List<DateTime>();
		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(() =>
			{
				callTimes.Add(DateTime.UtcNow);
				if (callTimes.Count < 2)
				{
					return new HttpResponseMessage(HttpStatusCode.InternalServerError)
					{
						Content = new StringContent("{}")
					};
				}

				return new HttpResponseMessage(HttpStatusCode.OK)
				{
					Content = new StringContent("""{"value":[]}""")
				};
			});

		using var client = new ODataClient(new ODataClientOptions
		{
			BaseUrl = "https://test.odata.org/",
			HttpClient = _httpClient,
			Logger = NullLogger.Instance,
			RetryCount = 2,
			RetryDelay = TimeSpan.FromMilliseconds(100)
		});

		// Act
		await client.GetAsync(client.For<Product>("Products"), CancellationToken);

		// Assert
		callTimes.Should().HaveCount(2);
		var delay = callTimes[1] - callTimes[0];
		delay.TotalMilliseconds.Should().BeGreaterThanOrEqualTo(90); // Allow some tolerance
	}
}
