using AwesomeAssertions;
using PanoramicData.OData.Client.Test.Fixtures;

namespace PanoramicData.OData.Client.Test.IntegrationTests;

/// <summary>
/// Integration tests for OData metadata and service document operations.
/// Tests $metadata endpoint and service document retrieval.
/// </summary>
public class MetadataIntegrationTests : TestBase, IClassFixture<ODataClientFixture>
{
	private readonly ODataClientFixture _fixture;

	/// <summary>
	/// Initializes a new instance of the test class.
	/// </summary>
	public MetadataIntegrationTests(ODataClientFixture fixture)
	{
		_fixture = fixture;
	}

	#region Metadata Tests

	/// <summary>
	/// Tests that GetMetadataXmlAsync returns valid metadata XML.
	/// </summary>
	[Fact]
	public async Task GetMetadataXmlAsync_ReturnsValidMetadata()
	{
		// Act
		var metadata = await _fixture.Client.GetMetadataXmlAsync(CancellationToken);

		// Assert
		metadata.Should().NotBeNullOrEmpty();
		metadata.Should().Contain("<?xml");
		metadata.Should().Contain("edmx:Edmx");
		metadata.Should().Contain("Schema");
	}

	/// <summary>
	/// Tests that metadata contains entity type definitions.
	/// </summary>
	[Fact]
	public async Task GetMetadataXmlAsync_ContainsEntityTypes()
	{
		// Act
		var metadata = await _fixture.Client.GetMetadataXmlAsync(CancellationToken);

		// Assert
		metadata.Should().Contain("EntityType");
		metadata.Should().Contain("Product");
		metadata.Should().Contain("Category");
	}

	/// <summary>
	/// Tests that metadata contains navigation properties.
	/// </summary>
	[Fact]
	public async Task GetMetadataXmlAsync_ContainsNavigationProperties()
	{
		// Act
		var metadata = await _fixture.Client.GetMetadataXmlAsync(CancellationToken);

		// Assert
		metadata.Should().Contain("NavigationProperty");
	}

	/// <summary>
	/// Tests that metadata contains property definitions.
	/// </summary>
	[Fact]
	public async Task GetMetadataXmlAsync_ContainsProperties()
	{
		// Act
		var metadata = await _fixture.Client.GetMetadataXmlAsync(CancellationToken);

		// Assert
		metadata.Should().Contain("Property Name=\"ID\"");
		metadata.Should().Contain("Property Name=\"Name\"");
	}

	/// <summary>
	/// Tests that GetMetadataAsync returns parsed metadata.
	/// </summary>
	[Fact]
	public async Task GetMetadataAsync_ReturnsParsedMetadata()
	{
		// Act
		var metadata = await _fixture.Client.GetMetadataAsync(CancellationToken);

		// Assert
		metadata.Should().NotBeNull();
		metadata.EntityTypes.Should().NotBeEmpty();
	}

	/// <summary>
	/// Tests that parsed metadata contains entity sets.
	/// </summary>
	[Fact]
	public async Task GetMetadataAsync_ContainsEntitySets()
	{
		// Act
		var metadata = await _fixture.Client.GetMetadataAsync(CancellationToken);

		// Assert
		metadata.EntitySets.Should().NotBeEmpty();
		metadata.EntitySets.Should().Contain(es => es.Name == "Products");
	}

	#endregion

	#region Service Document Tests

	/// <summary>
	/// Tests that GetServiceDocumentAsync returns a valid service document.
	/// </summary>
	[Fact]
	public async Task GetServiceDocumentAsync_ReturnsValidDocument()
	{
		// Act
		var serviceDoc = await _fixture.Client.GetServiceDocumentAsync(headers: null, cancellationToken: CancellationToken);

		// Assert
		serviceDoc.Should().NotBeNull();
		serviceDoc.Context.Should().NotBeNullOrEmpty();
		serviceDoc.EntitySets.Should().NotBeEmpty();
	}

	/// <summary>
	/// Tests that service document contains expected entity sets.
	/// </summary>
	[Fact]
	public async Task GetServiceDocumentAsync_ContainsEntitySets()
	{
		// Act
		var serviceDoc = await _fixture.Client.GetServiceDocumentAsync(headers: null, cancellationToken: CancellationToken);

		// Assert
		serviceDoc.EntitySets.Should().Contain(es =>
			es.Name == "Products" || es.Name == "Categories");
	}

	/// <summary>
	/// Tests that entity sets have correct URLs.
	/// </summary>
	[Fact]
	public async Task GetServiceDocumentAsync_EntitySetsHaveUrls()
	{
		// Act
		var serviceDoc = await _fixture.Client.GetServiceDocumentAsync(headers: null, cancellationToken: CancellationToken);

		// Assert
		serviceDoc.EntitySets.Should().AllSatisfy(es =>
		{
			es.Name.Should().NotBeNullOrEmpty();
			es.Url.Should().NotBeNullOrEmpty();
		});
	}

	#endregion

	#region Metadata Caching Tests

	/// <summary>
	/// Tests that metadata can be invalidated and refetched.
	/// </summary>
	[Fact]
	public async Task InvalidateMetadataCache_ThenGetMetadata_Succeeds()
	{
		// Arrange - First fetch to populate any cache
		var metadata1 = await _fixture.Client.GetMetadataXmlAsync(CancellationToken);

		// Act - Invalidate cache and refetch
		_fixture.Client.InvalidateMetadataCache();
		var metadata2 = await _fixture.Client.GetMetadataXmlAsync(CancellationToken);

		// Assert
		metadata1.Should().Be(metadata2); // Content should be the same
	}

	#endregion
}
