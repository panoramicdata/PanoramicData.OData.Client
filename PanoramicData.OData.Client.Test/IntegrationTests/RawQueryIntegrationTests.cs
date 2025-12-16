using AwesomeAssertions;
using PanoramicData.OData.Client.Test.Fixtures;
using System.Text.Json;

namespace PanoramicData.OData.Client.Test.IntegrationTests;

/// <summary>
/// Integration tests for raw query operations using GetRawAsync.
/// Tests direct JSON document access and custom query scenarios.
/// </summary>
public class RawQueryIntegrationTests : TestBase, IClassFixture<ODataClientFixture>
{
	private readonly ODataClientFixture _fixture;

	/// <summary>
	/// Initializes a new instance of the test class.
	/// </summary>
	public RawQueryIntegrationTests(ODataClientFixture fixture)
	{
		_fixture = fixture;
	}

	#region GetRawAsync Tests

	/// <summary>
	/// Tests that GetRawAsync returns a valid JsonDocument.
	/// </summary>
	[Fact]
	public async Task GetRawAsync_ValidQuery_ReturnsJsonDocument()
	{
		// Arrange & Act
		using var doc = await _fixture.Client.GetRawAsync(
			"Products?$top=5",
			cancellationToken: CancellationToken);

		// Assert
		doc.Should().NotBeNull();
		doc.RootElement.TryGetProperty("value", out var valueElement).Should().BeTrue();
		valueElement.ValueKind.Should().Be(JsonValueKind.Array);
		valueElement.GetArrayLength().Should().BeGreaterThan(0);
	}

	/// <summary>
	/// Tests that GetRawAsync with filter returns filtered results.
	/// </summary>
	[Fact]
	public async Task GetRawAsync_WithFilter_ReturnsFilteredResults()
	{
		// Arrange & Act
		using var doc = await _fixture.Client.GetRawAsync(
			"Products?$filter=Rating gt 3&$top=5",
			cancellationToken: CancellationToken);

		// Assert
		doc.Should().NotBeNull();
		var valueElement = doc.RootElement.GetProperty("value");
		foreach (var item in valueElement.EnumerateArray())
		{
			var rating = item.GetProperty("Rating").GetInt32();
			rating.Should().BeGreaterThan(3);
		}
	}

	/// <summary>
	/// Tests that GetRawAsync with select returns only selected fields.
	/// </summary>
	[Fact]
	public async Task GetRawAsync_WithSelect_ReturnsSelectedFields()
	{
		// Arrange & Act
		using var doc = await _fixture.Client.GetRawAsync(
			"Products?$select=ID,Name&$top=3",
			cancellationToken: CancellationToken);

		// Assert
		doc.Should().NotBeNull();
		var valueElement = doc.RootElement.GetProperty("value");
		foreach (var item in valueElement.EnumerateArray())
		{
			item.TryGetProperty("ID", out _).Should().BeTrue();
			item.TryGetProperty("Name", out _).Should().BeTrue();
		}
	}

	/// <summary>
	/// Tests that GetRawAsync returns count when requested.
	/// </summary>
	[Fact]
	public async Task GetRawAsync_WithCount_ReturnsCount()
	{
		// Arrange & Act
		using var doc = await _fixture.Client.GetRawAsync(
			"Products?$count=true&$top=1",
			cancellationToken: CancellationToken);

		// Assert
		doc.Should().NotBeNull();
		doc.RootElement.TryGetProperty("@odata.count", out var countElement).Should().BeTrue();
		countElement.GetInt64().Should().BePositive();
	}

	/// <summary>
	/// Tests that GetRawAsync for single entity returns the entity directly.
	/// </summary>
	[Fact]
	public async Task GetRawAsync_SingleEntity_ReturnsEntity()
	{
		// Arrange & Act
		using var doc = await _fixture.Client.GetRawAsync(
			"Products(0)",
			cancellationToken: CancellationToken);

		// Assert
		doc.Should().NotBeNull();
		doc.RootElement.ValueKind.Should().Be(JsonValueKind.Object);
		doc.RootElement.TryGetProperty("ID", out var idElement).Should().BeTrue();
		idElement.GetInt32().Should().Be(0);
	}

	/// <summary>
	/// Tests that GetRawAsync with expand returns related entities.
	/// </summary>
	[Fact]
	public async Task GetRawAsync_WithExpand_ReturnsRelatedEntities()
	{
		// Arrange & Act
		using var doc = await _fixture.Client.GetRawAsync(
			"Categories?$expand=Products&$top=1",
			cancellationToken: CancellationToken);

		// Assert
		doc.Should().NotBeNull();
		var valueElement = doc.RootElement.GetProperty("value");
		var firstCategory = valueElement.EnumerateArray().First();
		firstCategory.TryGetProperty("Products", out var productsElement).Should().BeTrue();
		productsElement.ValueKind.Should().Be(JsonValueKind.Array);
	}

	#endregion

	#region Custom Headers Tests

	/// <summary>
	/// Tests that GetRawAsync accepts custom headers.
	/// </summary>
	[Fact]
	public async Task GetRawAsync_WithCustomHeaders_Succeeds()
	{
		// Arrange
		var headers = new Dictionary<string, string>
		{
			["Accept"] = "application/json",
			["X-Custom-Header"] = "test-value"
		};

		// Act
		using var doc = await _fixture.Client.GetRawAsync(
			"Products?$top=1",
			headers,
			CancellationToken);

		// Assert
		doc.Should().NotBeNull();
		doc.RootElement.TryGetProperty("value", out _).Should().BeTrue();
	}

	#endregion

	#region OData Annotations Tests

	/// <summary>
	/// Tests that GetRawAsync preserves OData annotations in the response.
	/// </summary>
	[Fact]
	public async Task GetRawAsync_PreservesODataAnnotations()
	{
		// Arrange & Act
		using var doc = await _fixture.Client.GetRawAsync(
			"Products?$count=true&$top=1",
			cancellationToken: CancellationToken);

		// Assert
		doc.Should().NotBeNull();
		// Check for @odata.context
		doc.RootElement.TryGetProperty("@odata.context", out var contextElement).Should().BeTrue();
		contextElement.GetString().Should().NotBeNullOrEmpty();
	}

	#endregion

	#region Edge Cases

	/// <summary>
	/// Tests that GetRawAsync handles empty result sets.
	/// </summary>
	[Fact]
	public async Task GetRawAsync_EmptyResults_ReturnsEmptyArray()
	{
		// Arrange & Act
		using var doc = await _fixture.Client.GetRawAsync(
			"Products?$filter=ID eq -99999",
			cancellationToken: CancellationToken);

		// Assert
		doc.Should().NotBeNull();
		var valueElement = doc.RootElement.GetProperty("value");
		valueElement.GetArrayLength().Should().Be(0);
	}

	#endregion
}
