using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using System.Net;

namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Unit tests for OData metadata parsing support.
/// </summary>
public class ODataClientMetadataTests : IDisposable
{
	private readonly Mock<HttpMessageHandler> _mockHandler;
	private readonly HttpClient _httpClient;
	private readonly ODataClient _client;

	/// <summary>
	/// Initializes a new instance of the test class.
	/// </summary>
	public ODataClientMetadataTests()
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

	#region GetMetadataAsync Tests

	/// <summary>
	/// Tests that GetMetadataAsync requests $metadata endpoint.
	/// </summary>
	[Fact]
	public async Task GetMetadataAsync_RequestsMetadataEndpoint()
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
				Content = new StringContent(SimpleMetadataXml)
			});

		// Act
		await _client.GetMetadataAsync(cancellationToken: CancellationToken.None);

		// Assert
		capturedRequest.Should().NotBeNull();
		capturedRequest!.RequestUri!.ToString().Should().Contain("$metadata");
	}

	/// <summary>
	/// Tests that GetMetadataAsync parses namespace.
	/// </summary>
	[Fact]
	public async Task GetMetadataAsync_ParsesNamespace()
	{
		// Arrange
		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(SimpleMetadataXml)
			});

		// Act
		var metadata = await _client.GetMetadataAsync(cancellationToken: CancellationToken.None);

		// Assert
		metadata.Namespace.Should().Be("TestService");
	}

	/// <summary>
	/// Tests that GetMetadataAsync parses entity types.
	/// </summary>
	[Fact]
	public async Task GetMetadataAsync_ParsesEntityTypes()
	{
		// Arrange
		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(SimpleMetadataXml)
			});

		// Act
		var metadata = await _client.GetMetadataAsync(cancellationToken: CancellationToken.None);

		// Assert
		metadata.EntityTypes.Should().HaveCount(2);
		metadata.GetEntityType("Product").Should().NotBeNull();
		metadata.GetEntityType("Category").Should().NotBeNull();
	}

	/// <summary>
	/// Tests that GetMetadataAsync parses entity type properties.
	/// </summary>
	[Fact]
	public async Task GetMetadataAsync_ParsesEntityTypeProperties()
	{
		// Arrange
		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(SimpleMetadataXml)
			});

		// Act
		var metadata = await _client.GetMetadataAsync(cancellationToken: CancellationToken.None);
		var productType = metadata.GetEntityType("Product");

		// Assert
		productType.Should().NotBeNull();
		productType!.Properties.Should().HaveCount(3);
		productType.GetProperty("Id").Should().NotBeNull();
		productType.GetProperty("Name").Should().NotBeNull();
		productType.GetProperty("Price").Should().NotBeNull();

		var nameProperty = productType.GetProperty("Name");
		nameProperty!.Type.Should().Be("Edm.String");
		nameProperty.IsNullable.Should().BeTrue();
	}

	/// <summary>
	/// Tests that GetMetadataAsync parses entity type key.
	/// </summary>
	[Fact]
	public async Task GetMetadataAsync_ParsesEntityTypeKey()
	{
		// Arrange
		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(SimpleMetadataXml)
			});

		// Act
		var metadata = await _client.GetMetadataAsync(cancellationToken: CancellationToken.None);
		var productType = metadata.GetEntityType("Product");

		// Assert
		productType.Should().NotBeNull();
		productType!.Key.Should().ContainSingle();
		productType.Key.Should().Contain("Id");
	}

	/// <summary>
	/// Tests that GetMetadataAsync parses navigation properties.
	/// </summary>
	[Fact]
	public async Task GetMetadataAsync_ParsesNavigationProperties()
	{
		// Arrange
		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(SimpleMetadataXml)
			});

		// Act
		var metadata = await _client.GetMetadataAsync(cancellationToken: CancellationToken.None);
		var productType = metadata.GetEntityType("Product");

		// Assert
		productType.Should().NotBeNull();
		productType!.NavigationProperties.Should().ContainSingle();

		var categoryNav = productType.GetNavigationProperty("Category");
		categoryNav.Should().NotBeNull();
		categoryNav!.Type.Should().Be("TestService.Category");
		categoryNav.IsCollection.Should().BeFalse();
	}

	/// <summary>
	/// Tests that GetMetadataAsync parses entity sets.
	/// </summary>
	[Fact]
	public async Task GetMetadataAsync_ParsesEntitySets()
	{
		// Arrange
		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(SimpleMetadataXml)
			});

		// Act
		var metadata = await _client.GetMetadataAsync(cancellationToken: CancellationToken.None);

		// Assert
		metadata.EntitySets.Should().HaveCount(2);
		metadata.GetEntitySet("Products").Should().NotBeNull();
		metadata.GetEntitySet("Categories").Should().NotBeNull();

		var productsSet = metadata.GetEntitySet("Products");
		productsSet!.EntityType.Should().Be("TestService.Product");
	}

	/// <summary>
	/// Tests that GetMetadataAsync parses complex types.
	/// </summary>
	[Fact]
	public async Task GetMetadataAsync_ParsesComplexTypes()
	{
		// Arrange
		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(ComplexMetadataXml)
			});

		// Act
		var metadata = await _client.GetMetadataAsync(cancellationToken: CancellationToken.None);

		// Assert
		metadata.ComplexTypes.Should().ContainSingle();
		var addressType = metadata.ComplexTypes[0];
		addressType.Name.Should().Be("Address");
		addressType.Properties.Should().HaveCount(2);
	}

	/// <summary>
	/// Tests that GetMetadataAsync parses enum types.
	/// </summary>
	[Fact]
	public async Task GetMetadataAsync_ParsesEnumTypes()
	{
		// Arrange
		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(ComplexMetadataXml)
			});

		// Act
		var metadata = await _client.GetMetadataAsync(cancellationToken: CancellationToken.None);

		// Assert
		metadata.EnumTypes.Should().ContainSingle();
		var statusType = metadata.EnumTypes[0];
		statusType.Name.Should().Be("OrderStatus");
		statusType.Members.Should().HaveCount(3);
		statusType.Members[0].Name.Should().Be("Pending");
		statusType.Members[0].Value.Should().Be(0);
	}

	/// <summary>
	/// Tests that GetMetadataAsync parses singletons.
	/// </summary>
	[Fact]
	public async Task GetMetadataAsync_ParsesSingletons()
	{
		// Arrange
		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(ComplexMetadataXml)
			});

		// Act
		var metadata = await _client.GetMetadataAsync(cancellationToken: CancellationToken.None);

		// Assert
		metadata.Singletons.Should().ContainSingle();
		metadata.Singletons[0].Name.Should().Be("Me");
		metadata.Singletons[0].Type.Should().Be("TestService.Person");
	}

	#endregion

	#region ODataProperty Tests

	/// <summary>
	/// Tests IsCollection for collection properties.
	/// </summary>
	[Fact]
	public void ODataProperty_IsCollection_ReturnsCorrectly()
	{
		// Arrange
		var collectionProperty = new ODataProperty { Type = "Collection(Edm.String)" };
		var simpleProperty = new ODataProperty { Type = "Edm.String" };

		// Assert
		collectionProperty.IsCollection.Should().BeTrue();
		simpleProperty.IsCollection.Should().BeFalse();
	}

	/// <summary>
	/// Tests ElementType for collection properties.
	/// </summary>
	[Fact]
	public void ODataProperty_ElementType_ReturnsCorrectly()
	{
		// Arrange
		var collectionProperty = new ODataProperty { Type = "Collection(Edm.String)" };
		var simpleProperty = new ODataProperty { Type = "Edm.String" };

		// Assert
		collectionProperty.ElementType.Should().Be("Edm.String");
		simpleProperty.ElementType.Should().BeNull();
	}

	#endregion

	#region ODataNavigationProperty Tests

	/// <summary>
	/// Tests IsCollection for navigation properties.
	/// </summary>
	[Fact]
	public void ODataNavigationProperty_IsCollection_ReturnsCorrectly()
	{
		// Arrange
		var collectionNav = new ODataNavigationProperty { Type = "Collection(TestService.Order)" };
		var singleNav = new ODataNavigationProperty { Type = "TestService.Category" };

		// Assert
		collectionNav.IsCollection.Should().BeTrue();
		singleNav.IsCollection.Should().BeFalse();
	}

	/// <summary>
	/// Tests TargetType for navigation properties.
	/// </summary>
	[Fact]
	public void ODataNavigationProperty_TargetType_ReturnsCorrectly()
	{
		// Arrange
		var collectionNav = new ODataNavigationProperty { Type = "Collection(TestService.Order)" };
		var singleNav = new ODataNavigationProperty { Type = "TestService.Category" };

		// Assert
		collectionNav.TargetType.Should().Be("TestService.Order");
		singleNav.TargetType.Should().Be("TestService.Category");
	}

	#endregion

	#region Test Data

	private const string SimpleMetadataXml = """
		<?xml version="1.0" encoding="utf-8"?>
		<edmx:Edmx Version="4.0" xmlns:edmx="http://docs.oasis-open.org/odata/ns/edmx">
			<edmx:DataServices>
				<Schema Namespace="TestService" xmlns="http://docs.oasis-open.org/odata/ns/edm">
					<EntityType Name="Product">
						<Key>
							<PropertyRef Name="Id" />
						</Key>
						<Property Name="Id" Type="Edm.Int32" Nullable="false" />
						<Property Name="Name" Type="Edm.String" />
						<Property Name="Price" Type="Edm.Decimal" Nullable="false" />
						<NavigationProperty Name="Category" Type="TestService.Category" />
					</EntityType>
					<EntityType Name="Category">
						<Key>
							<PropertyRef Name="Id" />
						</Key>
						<Property Name="Id" Type="Edm.Int32" Nullable="false" />
						<Property Name="Name" Type="Edm.String" />
						<NavigationProperty Name="Products" Type="Collection(TestService.Product)" />
					</EntityType>
					<EntityContainer Name="Container">
						<EntitySet Name="Products" EntityType="TestService.Product">
							<NavigationPropertyBinding Path="Category" Target="Categories" />
						</EntitySet>
						<EntitySet Name="Categories" EntityType="TestService.Category">
							<NavigationPropertyBinding Path="Products" Target="Products" />
						</EntitySet>
					</EntityContainer>
				</Schema>
			</edmx:DataServices>
		</edmx:Edmx>
		""";

	private const string ComplexMetadataXml = """
		<?xml version="1.0" encoding="utf-8"?>
		<edmx:Edmx Version="4.0" xmlns:edmx="http://docs.oasis-open.org/odata/ns/edmx">
			<edmx:DataServices>
				<Schema Namespace="TestService" xmlns="http://docs.oasis-open.org/odata/ns/edm">
					<ComplexType Name="Address">
						<Property Name="Street" Type="Edm.String" />
						<Property Name="City" Type="Edm.String" />
					</ComplexType>
					<EnumType Name="OrderStatus">
						<Member Name="Pending" Value="0" />
						<Member Name="Processing" Value="1" />
						<Member Name="Completed" Value="2" />
					</EnumType>
					<EntityType Name="Person">
						<Key>
							<PropertyRef Name="Id" />
						</Key>
						<Property Name="Id" Type="Edm.Int32" Nullable="false" />
						<Property Name="Name" Type="Edm.String" />
						<Property Name="HomeAddress" Type="TestService.Address" />
					</EntityType>
					<EntityContainer Name="Container">
						<EntitySet Name="People" EntityType="TestService.Person" />
						<Singleton Name="Me" Type="TestService.Person" />
					</EntityContainer>
				</Schema>
			</edmx:DataServices>
		</edmx:Edmx>
		""";

	#endregion
}
