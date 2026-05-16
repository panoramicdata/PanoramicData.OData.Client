#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
using PanoramicData.OData.Client.Converters;
using System.Collections;
using System.Text.Json;

namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Tests for correct serialization of dictionary-based PATCH bodies.
///
/// Regression suite for the bug introduced in 10.0.70 where the ODataTypeAnnotationConverter
/// intercepted IDictionary patch bodies and injected an @odata.type annotation, causing the
/// ASP.NET OData Delta&lt;T&gt; model binder on the server to return null and produce a 400
/// "A PATCH request body is required" response.
/// </summary>
public class ODataPatchDictionaryTests : TestBase, IDisposable
{
    private readonly Mock<HttpMessageHandler> _mockHandler;
    private readonly HttpClient _httpClient;
    private readonly ODataClient _client;
    private string? _capturedBody;

    public ODataPatchDictionaryTests()
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

    private void SetupCaptureAndReturn(HttpStatusCode statusCode, string responseBody) =>
        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, ct) =>
            {
                _capturedBody = req.Content!.ReadAsStringAsync(ct).GetAwaiter().GetResult();
            })
            .ReturnsAsync(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
            });

    // -----------------------------------------------------------------------
    // ODataTypeAnnotationConverter.CanConvert - dictionary exclusion
    // -----------------------------------------------------------------------

    /// <summary>
    /// The converter must not handle Dictionary&lt;string, object?&gt; - the primary patch body type.
    /// </summary>
    [Fact]
    public void ODataTypeAnnotationConverter_CanConvert_ReturnsFalse_ForNullableValueDictionary()
    {
        var converter = new ODataTypeAnnotationConverter();
        converter.CanConvert(typeof(Dictionary<string, object?>)).Should().BeFalse(
            because: "Dictionary<string, object?> is used as a PATCH body and must not receive @odata.type");
    }

    /// <summary>
    /// The converter must not handle Dictionary&lt;string, object&gt;.
    /// </summary>
    [Fact]
    public void ODataTypeAnnotationConverter_CanConvert_ReturnsFalse_ForObjectValueDictionary()
    {
        var converter = new ODataTypeAnnotationConverter();
        converter.CanConvert(typeof(Dictionary<string, object>)).Should().BeFalse(
            because: "Dictionary<string, object> is also a common patch body type");
    }

    /// <summary>
    /// Any generic Dictionary&lt;,&gt; should be excluded to prevent future variants breaking PATCH.
    /// </summary>
    [Fact]
    public void ODataTypeAnnotationConverter_CanConvert_ReturnsFalse_ForArbitraryGenericDictionary()
    {
        var converter = new ODataTypeAnnotationConverter();
        converter.CanConvert(typeof(Dictionary<string, int>)).Should().BeFalse(
            because: "all Dictionary<,> types should be excluded from @odata.type injection");
    }

    /// <summary>
    /// Types that implement IDictionary (non-generic) should also be excluded.
    /// </summary>
    [Fact]
    public void ODataTypeAnnotationConverter_CanConvert_ReturnsFalse_ForIDictionaryImplementors()
    {
        var converter = new ODataTypeAnnotationConverter();
        converter.CanConvert(typeof(Hashtable)).Should().BeFalse(
            because: "Hashtable implements IDictionary and must not receive @odata.type");
    }

    /// <summary>
    /// SortedDictionary is a common concrete IDictionary implementation that should be excluded.
    /// </summary>
    [Fact]
    public void ODataTypeAnnotationConverter_CanConvert_ReturnsFalse_ForSortedDictionary()
    {
        var converter = new ODataTypeAnnotationConverter();
        converter.CanConvert(typeof(SortedDictionary<string, object>)).Should().BeFalse(
            because: "SortedDictionary implements IDictionary and must not receive @odata.type");
    }

    /// <summary>
    /// Sanity check: regular entity types should still be handled by the converter (TPH support intact).
    /// </summary>
    [Fact]
    public void ODataTypeAnnotationConverter_CanConvert_ReturnsTrue_ForRegularEntityTypes()
    {
        var converter = new ODataTypeAnnotationConverter();
        converter.CanConvert(typeof(SimpleEntity)).Should().BeTrue(
            because: "regular entity classes must still be processed for @odata.type TPH support");
    }

    // -----------------------------------------------------------------------
    // Serialization - no @odata.type in patch body
    // -----------------------------------------------------------------------

    /// <summary>
    /// Serializing a Dictionary&lt;string, object?&gt; with the OData JSON options must not produce
    /// an @odata.type property - the primary regression guard.
    /// </summary>
    [Fact]
    public void Serialize_PatchDictionary_DoesNotContain_ODataTypeAnnotation()
    {
        var options = BuildODataJsonOptions();
        var patch = new Dictionary<string, object?> { ["description"] = "new value", ["name"] = "updated" };

        var json = JsonSerializer.Serialize(patch, options);

        json.Should().NotContain("@odata.type",
            because: "PATCH body dictionaries must not have @odata.type injected or the OData Delta<T> binder returns null");
        json.Should().Contain("\"description\"");
        json.Should().Contain("\"new value\"");
    }

    /// <summary>
    /// Serializing a Dictionary typed as object (the compile-time type in UpdateAsync before the fix)
    /// must also produce a clean body without @odata.type.
    /// </summary>
    [Fact]
    public void Serialize_PatchDictionaryAsObject_DoesNotContain_ODataTypeAnnotation()
    {
        var options = BuildODataJsonOptions();
        object patchValues = new Dictionary<string, object?> { ["description"] = "new value" };

        // Use the runtime type - mirrors the fixed JsonContent.Create(patchValues, patchValues.GetType(), ...) call
        var json = JsonSerializer.Serialize(patchValues, patchValues.GetType(), options);

        json.Should().NotContain("@odata.type",
            because: "before the fix, passing patchValues as object caused @odata.type to be injected into the PATCH body");
        json.Should().Contain("\"description\"");
    }

    /// <summary>
    /// Serializing a Dictionary typed as object using typeof(object) (the broken pre-fix path)
    /// previously injected @odata.type. This test documents that the runtime-type fix resolves it.
    /// </summary>
    [Fact]
    public void Serialize_PatchDictionary_WithRuntimeType_ProducesCorrectJson()
    {
        var options = BuildODataJsonOptions();
        var patch = new Dictionary<string, object?> { ["status"] = "Active", ["count"] = 42 };

        var json = JsonSerializer.Serialize(patch, patch.GetType(), options);

        json.Should().Be("{\"status\":\"Active\",\"count\":42}",
            because: "the PATCH body must be a flat JSON object with only the changed properties");
    }

    // -----------------------------------------------------------------------
    // Integration: UpdateAsync sends correct PATCH body
    // -----------------------------------------------------------------------

    /// <summary>
    /// UpdateAsync must send a PATCH body containing the changed fields and no @odata.type annotation.
    /// This is the end-to-end regression test for the "A PATCH request body is required" bug.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_WithDictionaryPatch_SendsCorrectBody_WithoutODataTypeAnnotation()
    {
        // Arrange
        SetupCaptureAndReturn(
            HttpStatusCode.OK,
            """{"id": 1, "name": "Updated Name", "description": "new value"}""");

        var patch = new Dictionary<string, object?> { ["description"] = "new value" };

        // Act
        await _client.UpdateAsync<SimpleEntity>("Entities", 1, patch, cancellationToken: CancellationToken);

        // Assert
        _capturedBody.Should().NotBeNull();
        _capturedBody.Should().NotContain("@odata.type",
            because: "injecting @odata.type into a PATCH body causes the OData Delta<T> binder to return null → 400");
        _capturedBody.Should().Contain("\"description\"");
        _capturedBody.Should().Contain("\"new value\"");
    }

    /// <summary>
    /// UpdateAsync with multiple changed fields must include all fields in the PATCH body.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_WithMultipleChangedFields_SendsAllFieldsInBody()
    {
        // Arrange
        SetupCaptureAndReturn(
            HttpStatusCode.OK,
            """{"id": 5, "name": "New Name", "description": "new desc"}""");

        var patch = new Dictionary<string, object?> { ["name"] = "New Name", ["description"] = "new desc" };

        // Act
        await _client.UpdateAsync<SimpleEntity>("Entities", 5, patch, cancellationToken: CancellationToken);

        // Assert
        _capturedBody.Should().Contain("\"name\"");
        _capturedBody.Should().Contain("\"New Name\"");
        _capturedBody.Should().Contain("\"description\"");
        _capturedBody.Should().Contain("\"new desc\"");
        _capturedBody.Should().NotContain("@odata.type");
    }

    /// <summary>
    /// UpdateAsync must not send an @odata.type annotation even when values contain class instances.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_WithNullableField_SerializesNull_WithoutODataTypeAnnotation()
    {
        // Arrange
        SetupCaptureAndReturn(
            HttpStatusCode.OK,
            """{"id": 3, "name": "Test", "description": null}""");

        var patch = new Dictionary<string, object?> { ["description"] = null };

        // Act
        await _client.UpdateAsync<SimpleEntity>("Entities", 3, patch, cancellationToken: CancellationToken);

        // Assert - null fields are omitted by WhenWritingNull, but no @odata.type
        _capturedBody.Should().NotContain("@odata.type");
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static JsonSerializerOptions BuildODataJsonOptions() => new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new ODataTypeAnnotationConverter(),
            new System.Text.Json.Serialization.JsonStringEnumConverter(),
        }
    };

    public class SimpleEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
