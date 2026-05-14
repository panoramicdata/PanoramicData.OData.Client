using PanoramicData.OData.Client.Exceptions;

namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Unit tests for 404 Not Found handling: IgnoreResourceNotFoundException option and GetByKeyOrDefaultAsync.
/// </summary>
public sealed class ODataClientNotFoundTests : TestBase, IDisposable
{
    private readonly Mock<HttpMessageHandler> _mockHandler;
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the test class with mocked dependencies.
    /// </summary>
    public ODataClientNotFoundTests()
    {
        _mockHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHandler.Object)
        {
            BaseAddress = new Uri("https://test.odata.org/")
        };
    }

	/// <inheritdoc/>
	public void Dispose() => _httpClient.Dispose();

	private ODataClient CreateClient(bool ignoreResourceNotFoundException = false)
        => new(new ODataClientOptions
        {
            BaseUrl = "https://test.odata.org/",
            HttpClient = _httpClient,
            Logger = NullLogger.Instance,
            RetryCount = 0,
            IgnoreResourceNotFoundException = ignoreResourceNotFoundException
        });

    private void SetupMockResponse(HttpStatusCode statusCode, string content) =>
        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json")
            });

    #region GetByKeyAsync with IgnoreResourceNotFoundException

    /// <summary>
    /// GetByKeyAsync throws ODataNotFoundException by default when resource is not found.
    /// </summary>
    [Fact]
    public async Task GetByKeyAsync_WhenNotFound_AndOptionIsFalse_ThrowsODataNotFoundException()
    {
        // Arrange
        SetupMockResponse(HttpStatusCode.NotFound, "{}");
        using var client = CreateClient(ignoreResourceNotFoundException: false);

        // Act
        var act = async () => await client.GetByKeyAsync<Product, int>(999, cancellationToken: CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ODataNotFoundException>();
    }

    /// <summary>
    /// GetByKeyAsync returns null when IgnoreResourceNotFoundException is true and resource is not found.
    /// </summary>
    [Fact]
    public async Task GetByKeyAsync_WhenNotFound_AndOptionIsTrue_ReturnsNull()
    {
        // Arrange
        SetupMockResponse(HttpStatusCode.NotFound, "{}");
        using var client = CreateClient(ignoreResourceNotFoundException: true);

        // Act
        var result = await client.GetByKeyAsync<Product, int>(999, cancellationToken: CancellationToken);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// GetByKeyAsync still returns the entity when IgnoreResourceNotFoundException is true and resource exists.
    /// </summary>
    [Fact]
    public async Task GetByKeyAsync_WhenFound_AndOptionIsTrue_ReturnsEntity()
    {
        // Arrange
        SetupMockResponse(HttpStatusCode.OK, """{ "ID": 1, "Name": "Widget", "Price": 9.99 }""");
        using var client = CreateClient(ignoreResourceNotFoundException: true);

        // Act
        var result = await client.GetByKeyAsync<Product, int>(1, cancellationToken: CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
    }

    #endregion

    #region GetByKeyOrDefaultAsync

    /// <summary>
    /// GetByKeyOrDefaultAsync returns null on 404 regardless of IgnoreResourceNotFoundException option.
    /// </summary>
    [Fact]
    public async Task GetByKeyOrDefaultAsync_WhenNotFound_ReturnsNull()
    {
        // Arrange
        SetupMockResponse(HttpStatusCode.NotFound, "{}");
        using var client = CreateClient(ignoreResourceNotFoundException: false);

        // Act
        var result = await client.GetByKeyOrDefaultAsync<Product, int>(999, cancellationToken: CancellationToken);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// GetByKeyOrDefaultAsync returns the entity when it exists.
    /// </summary>
    [Fact]
    public async Task GetByKeyOrDefaultAsync_WhenFound_ReturnsEntity()
    {
        // Arrange
        SetupMockResponse(HttpStatusCode.OK, """{ "ID": 42, "Name": "Gadget", "Price": 19.99 }""");
        using var client = CreateClient();

        // Act
        var result = await client.GetByKeyOrDefaultAsync<Product, int>(42, cancellationToken: CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(42);
        result.Name.Should().Be("Gadget");
    }

    /// <summary>
    /// GetByKeyOrDefaultAsync still throws for non-404 error responses.
    /// </summary>
    [Fact]
    public async Task GetByKeyOrDefaultAsync_WhenServerError_ThrowsODataClientException()
    {
        // Arrange
        SetupMockResponse(HttpStatusCode.InternalServerError, "{}");
        using var client = CreateClient();

        // Act
        var act = async () => await client.GetByKeyOrDefaultAsync<Product, int>(1, cancellationToken: CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ODataClientException>();
    }

    #endregion

    #region GetByKeyWithETagAsync with IgnoreResourceNotFoundException

    /// <summary>
    /// GetByKeyWithETagAsync returns response with null Value when IgnoreResourceNotFoundException is true and resource is not found.
    /// </summary>
    [Fact]
    public async Task GetByKeyWithETagAsync_WhenNotFound_AndOptionIsTrue_ReturnsNullValue()
    {
        // Arrange
        SetupMockResponse(HttpStatusCode.NotFound, "{}");
        using var client = CreateClient(ignoreResourceNotFoundException: true);

        // Act
        var result = await client.GetByKeyWithETagAsync<Product, int>(999, cancellationToken: CancellationToken);

        // Assert
        result.Value.Should().BeNull();
        result.ETag.Should().BeNull();
    }

    /// <summary>
    /// GetByKeyWithETagAsync throws ODataNotFoundException by default when resource is not found.
    /// </summary>
    [Fact]
    public async Task GetByKeyWithETagAsync_WhenNotFound_AndOptionIsFalse_ThrowsODataNotFoundException()
    {
        // Arrange
        SetupMockResponse(HttpStatusCode.NotFound, "{}");
        using var client = CreateClient(ignoreResourceNotFoundException: false);

        // Act
        var act = async () => await client.GetByKeyWithETagAsync<Product, int>(999, cancellationToken: CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ODataNotFoundException>();
    }

    #endregion
}
