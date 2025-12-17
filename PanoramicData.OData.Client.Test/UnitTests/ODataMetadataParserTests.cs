namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Unit tests for OData metadata parsing through the public API.
/// </summary>
public class ODataMetadataParserTests : TestBase, IDisposable
{
	private readonly Mock<HttpMessageHandler> _mockHandler;
	private readonly HttpClient _httpClient;
	private readonly ODataClient _client;

	/// <summary>
	/// Initializes a new instance of the test class with mocked dependencies.
	/// </summary>
	public ODataMetadataParserTests()
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
			RetryCount = 0,
			MetadataCacheDuration = TimeSpan.Zero // Disable caching for tests
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
	/// Tests GetMetadataAsync extracts namespace.
	/// </summary>
	[Fact]
	public async Task GetMetadataAsync_ExtractsNamespace()
	{
		// Arrange
		SetupMetadataResponse("""
			<?xml version="1.0" encoding="utf-8"?>
			<edmx:Edmx Version="4.0" xmlns:edmx="http://docs.oasis-open.org/odata/ns/edmx">
				<edmx:DataServices>
					<Schema Namespace="TestNamespace" xmlns="http://docs.oasis-open.org/odata/ns/edm">
					</Schema>
				</edmx:DataServices>
			</edmx:Edmx>
			""");

		// Act
		var metadata = await _client.GetMetadataAsync(CancellationToken);

		// Assert
		metadata.Namespace.Should().Be("TestNamespace");
	}

	/// <summary>
	/// Tests GetMetadataAsync handles empty schema.
	/// </summary>
	[Fact]
	public async Task GetMetadataAsync_EmptySchema_ReturnsEmptyMetadata()
	{
		// Arrange
		SetupMetadataResponse("""
			<?xml version="1.0" encoding="utf-8"?>
			<edmx:Edmx Version="4.0" xmlns:edmx="http://docs.oasis-open.org/odata/ns/edmx">
				<edmx:DataServices>
					<Schema Namespace="Test" xmlns="http://docs.oasis-open.org/odata/ns/edm">
					</Schema>
				</edmx:DataServices>
			</edmx:Edmx>
			""");

		// Act
		var metadata = await _client.GetMetadataAsync(CancellationToken);

		// Assert
		metadata.EntityTypes.Should().BeEmpty();
		metadata.ComplexTypes.Should().BeEmpty();
		metadata.EnumTypes.Should().BeEmpty();
		metadata.EntitySets.Should().BeEmpty();
	}

	/// <summary>
	/// Tests GetMetadataAsync extracts entity type with key.
	/// </summary>
	[Fact]
	public async Task GetMetadataAsync_EntityType_ExtractsNameAndKey()
	{
		// Arrange
		SetupMetadataResponse(CreateMetadataXml("""
			<EntityType Name="Product">
				<Key>
					<PropertyRef Name="ID"/>
				</Key>
				<Property Name="ID" Type="Edm.Int32" Nullable="false"/>
				<Property Name="Name" Type="Edm.String"/>
			</EntityType>
			"""));

		// Act
		var metadata = await _client.GetMetadataAsync(CancellationToken);

		// Assert
		metadata.EntityTypes.Should().ContainSingle();
		var entityType = metadata.EntityTypes[0];
		entityType.Name.Should().Be("Product");
		entityType.Key.Should().ContainSingle().Which.Should().Be("ID");
	}

	/// <summary>
	/// Tests GetMetadataAsync extracts entity type properties.
	/// </summary>
	[Fact]
	public async Task GetMetadataAsync_EntityType_ExtractsProperties()
	{
		// Arrange
		SetupMetadataResponse(CreateMetadataXml("""
			<EntityType Name="Product">
				<Key>
					<PropertyRef Name="ID"/>
				</Key>
				<Property Name="ID" Type="Edm.Int32" Nullable="false"/>
				<Property Name="Name" Type="Edm.String" Nullable="true"/>
				<Property Name="Price" Type="Edm.Decimal" Precision="10" Scale="2"/>
			</EntityType>
			"""));

		// Act
		var metadata = await _client.GetMetadataAsync(CancellationToken);

		// Assert
		var entityType = metadata.EntityTypes[0];
		entityType.Properties.Should().HaveCount(3);

		var idProp = entityType.GetProperty("ID");
		idProp.Should().NotBeNull();
		idProp!.Type.Should().Be("Edm.Int32");
		idProp.IsNullable.Should().BeFalse();

		var priceProp = entityType.GetProperty("Price");
		priceProp.Should().NotBeNull();
		priceProp!.Precision.Should().Be(10);
		priceProp.Scale.Should().Be(2);
	}

	/// <summary>
	/// Tests GetMetadataAsync extracts composite key.
	/// </summary>
	[Fact]
	public async Task GetMetadataAsync_EntityType_CompositeKey()
	{
		// Arrange
		SetupMetadataResponse(CreateMetadataXml("""
			<EntityType Name="OrderDetail">
				<Key>
					<PropertyRef Name="OrderID"/>
					<PropertyRef Name="ProductID"/>
				</Key>
				<Property Name="OrderID" Type="Edm.Int32" Nullable="false"/>
				<Property Name="ProductID" Type="Edm.Int32" Nullable="false"/>
			</EntityType>
			"""));

		// Act
		var metadata = await _client.GetMetadataAsync(CancellationToken);

		// Assert
		var entityType = metadata.EntityTypes[0];
		entityType.Key.Should().HaveCount(2);
		entityType.Key.Should().Contain("OrderID");
		entityType.Key.Should().Contain("ProductID");
	}

	/// <summary>
	/// Tests GetMetadataAsync extracts abstract entity type.
	/// </summary>
	[Fact]
	public async Task GetMetadataAsync_EntityType_Abstract()
	{
		// Arrange
		SetupMetadataResponse(CreateMetadataXml("""
			<EntityType Name="BaseEntity" Abstract="true">
				<Property Name="ID" Type="Edm.Int32" Nullable="false"/>
			</EntityType>
			"""));

		// Act
		var metadata = await _client.GetMetadataAsync(CancellationToken);

		// Assert
		metadata.EntityTypes[0].IsAbstract.Should().BeTrue();
	}

	/// <summary>
	/// Tests GetMetadataAsync extracts open entity type.
	/// </summary>
	[Fact]
	public async Task GetMetadataAsync_EntityType_OpenType()
	{
		// Arrange
		SetupMetadataResponse(CreateMetadataXml("""
			<EntityType Name="DynamicEntity" OpenType="true">
				<Key>
					<PropertyRef Name="ID"/>
				</Key>
				<Property Name="ID" Type="Edm.Int32" Nullable="false"/>
			</EntityType>
			"""));

		// Act
		var metadata = await _client.GetMetadataAsync(CancellationToken);

		// Assert
		metadata.EntityTypes[0].IsOpenType.Should().BeTrue();
	}

	/// <summary>
	/// Tests GetMetadataAsync extracts media entity.
	/// </summary>
	[Fact]
	public async Task GetMetadataAsync_EntityType_HasStream()
	{
		// Arrange
		SetupMetadataResponse(CreateMetadataXml("""
			<EntityType Name="Photo" HasStream="true">
				<Key>
					<PropertyRef Name="ID"/>
				</Key>
				<Property Name="ID" Type="Edm.Int32" Nullable="false"/>
			</EntityType>
			"""));

		// Act
		var metadata = await _client.GetMetadataAsync(CancellationToken);

		// Assert
		metadata.EntityTypes[0].HasStream.Should().BeTrue();
	}

	/// <summary>
	/// Tests GetMetadataAsync extracts derived entity type.
	/// </summary>
	[Fact]
	public async Task GetMetadataAsync_EntityType_BaseType()
	{
		// Arrange
		SetupMetadataResponse(CreateMetadataXml("""
			<EntityType Name="Employee" BaseType="Test.Person">
				<Property Name="Department" Type="Edm.String"/>
			</EntityType>
			"""));

		// Act
		var metadata = await _client.GetMetadataAsync(CancellationToken);

		// Assert
		metadata.EntityTypes[0].BaseType.Should().Be("Test.Person");
	}

	#endregion

	#region Navigation Property Tests

	/// <summary>
	/// Tests GetMetadataAsync extracts navigation properties.
	/// </summary>
	[Fact]
	public async Task GetMetadataAsync_EntityType_NavigationProperties()
	{
		// Arrange
		SetupMetadataResponse(CreateMetadataXml("""
			<EntityType Name="Order">
				<Key>
					<PropertyRef Name="ID"/>
				</Key>
				<Property Name="ID" Type="Edm.Int32" Nullable="false"/>
				<NavigationProperty Name="Customer" Type="Test.Customer"/>
				<NavigationProperty Name="Items" Type="Collection(Test.OrderItem)"/>
			</EntityType>
			"""));

		// Act
		var metadata = await _client.GetMetadataAsync(CancellationToken);

		// Assert
		var entityType = metadata.EntityTypes[0];
		entityType.NavigationProperties.Should().HaveCount(2);

		var customerNav = entityType.GetNavigationProperty("Customer");
		customerNav.Should().NotBeNull();
		customerNav!.Type.Should().Be("Test.Customer");
		customerNav.IsCollection.Should().BeFalse();

		var itemsNav = entityType.GetNavigationProperty("Items");
		itemsNav.Should().NotBeNull();
		itemsNav!.IsCollection.Should().BeTrue();
		itemsNav.TargetType.Should().Be("Test.OrderItem");
	}

	/// <summary>
	/// Tests GetMetadataAsync extracts navigation property partner.
	/// </summary>
	[Fact]
	public async Task GetMetadataAsync_NavigationProperty_Partner()
	{
		// Arrange
		SetupMetadataResponse(CreateMetadataXml("""
			<EntityType Name="Order">
				<Key>
					<PropertyRef Name="ID"/>
				</Key>
				<Property Name="ID" Type="Edm.Int32" Nullable="false"/>
				<NavigationProperty Name="Customer" Type="Test.Customer" Partner="Orders"/>
			</EntityType>
			"""));

		// Act
		var metadata = await _client.GetMetadataAsync(CancellationToken);

		// Assert
		var navProp = metadata.EntityTypes[0].GetNavigationProperty("Customer");
		navProp!.Partner.Should().Be("Orders");
	}

	#endregion

	#region ComplexType Parsing Tests

	/// <summary>
	/// Tests GetMetadataAsync extracts complex types.
	/// </summary>
	[Fact]
	public async Task GetMetadataAsync_ComplexType_Extracted()
	{
		// Arrange
		SetupMetadataResponse(CreateMetadataXml("""
			<ComplexType Name="Address">
				<Property Name="Street" Type="Edm.String"/>
				<Property Name="City" Type="Edm.String"/>
				<Property Name="PostalCode" Type="Edm.String"/>
			</ComplexType>
			"""));

		// Act
		var metadata = await _client.GetMetadataAsync(CancellationToken);

		// Assert
		metadata.ComplexTypes.Should().ContainSingle();
		var complexType = metadata.ComplexTypes[0];
		complexType.Name.Should().Be("Address");
		complexType.Properties.Should().HaveCount(3);
	}

	/// <summary>
	/// Tests GetMetadataAsync extracts abstract complex type.
	/// </summary>
	[Fact]
	public async Task GetMetadataAsync_ComplexType_Abstract()
	{
		// Arrange
		SetupMetadataResponse(CreateMetadataXml("""
			<ComplexType Name="BaseAddress" Abstract="true">
				<Property Name="Country" Type="Edm.String"/>
			</ComplexType>
			"""));

		// Act
		var metadata = await _client.GetMetadataAsync(CancellationToken);

		// Assert
		metadata.ComplexTypes[0].IsAbstract.Should().BeTrue();
	}

	#endregion

	#region EnumType Parsing Tests

	/// <summary>
	/// Tests GetMetadataAsync extracts enum types.
	/// </summary>
	[Fact]
	public async Task GetMetadataAsync_EnumType_Extracted()
	{
		// Arrange
		SetupMetadataResponse(CreateMetadataXml("""
			<EnumType Name="Color">
				<Member Name="Red" Value="0"/>
				<Member Name="Green" Value="1"/>
				<Member Name="Blue" Value="2"/>
			</EnumType>
			"""));

		// Act
		var metadata = await _client.GetMetadataAsync(CancellationToken);

		// Assert
		metadata.EnumTypes.Should().ContainSingle();
		var enumType = metadata.EnumTypes[0];
		enumType.Name.Should().Be("Color");
		enumType.Members.Should().HaveCount(3);
		enumType.Members[0].Name.Should().Be("Red");
		enumType.Members[0].Value.Should().Be(0);
	}

	/// <summary>
	/// Tests GetMetadataAsync extracts flags enum.
	/// </summary>
	[Fact]
	public async Task GetMetadataAsync_EnumType_Flags()
	{
		// Arrange
		SetupMetadataResponse(CreateMetadataXml("""
			<EnumType Name="Permissions" IsFlags="true">
				<Member Name="Read" Value="1"/>
				<Member Name="Write" Value="2"/>
				<Member Name="Execute" Value="4"/>
			</EnumType>
			"""));

		// Act
		var metadata = await _client.GetMetadataAsync(CancellationToken);

		// Assert
		metadata.EnumTypes[0].IsFlags.Should().BeTrue();
	}

	#endregion

	#region EntitySet Parsing Tests

	/// <summary>
	/// Tests GetMetadataAsync extracts entity sets.
	/// </summary>
	[Fact]
	public async Task GetMetadataAsync_EntitySet_Extracted()
	{
		// Arrange
		SetupMetadataResponse(CreateMetadataXml("""
			<EntityType Name="Product">
				<Key>
					<PropertyRef Name="ID"/>
				</Key>
				<Property Name="ID" Type="Edm.Int32" Nullable="false"/>
			</EntityType>
			<EntityContainer Name="Container">
				<EntitySet Name="Products" EntityType="Test.Product"/>
			</EntityContainer>
			"""));

		// Act
		var metadata = await _client.GetMetadataAsync(CancellationToken);

		// Assert
		metadata.EntitySets.Should().ContainSingle();
		var entitySet = metadata.EntitySets[0];
		entitySet.Name.Should().Be("Products");
		entitySet.EntityType.Should().Be("Test.Product");
	}

	/// <summary>
	/// Tests GetEntitySet finds entity set by name.
	/// </summary>
	[Fact]
	public async Task GetEntitySet_FindsByName()
	{
		// Arrange
		SetupMetadataResponse(CreateMetadataXml("""
			<EntityContainer Name="Container">
				<EntitySet Name="Products" EntityType="Test.Product"/>
				<EntitySet Name="Categories" EntityType="Test.Category"/>
			</EntityContainer>
			"""));

		// Act
		var metadata = await _client.GetMetadataAsync(CancellationToken);
		var entitySet = metadata.GetEntitySet("Products");

		// Assert
		entitySet.Should().NotBeNull();
		entitySet!.Name.Should().Be("Products");
	}

	/// <summary>
	/// Tests GetEntityType finds entity type by name.
	/// </summary>
	[Fact]
	public async Task GetEntityType_FindsByName()
	{
		// Arrange
		SetupMetadataResponse(CreateMetadataXml("""
			<EntityType Name="Product">
				<Key><PropertyRef Name="ID"/></Key>
				<Property Name="ID" Type="Edm.Int32"/>
			</EntityType>
			<EntityType Name="Category">
				<Key><PropertyRef Name="ID"/></Key>
				<Property Name="ID" Type="Edm.Int32"/>
			</EntityType>
			"""));

		// Act
		var metadata = await _client.GetMetadataAsync(CancellationToken);
		var entityType = metadata.GetEntityType("Product");

		// Assert
		entityType.Should().NotBeNull();
		entityType!.Name.Should().Be("Product");
	}

	/// <summary>
	/// Tests GetMetadataAsync extracts singletons.
	/// </summary>
	[Fact]
	public async Task GetMetadataAsync_Singleton_Extracted()
	{
		// Arrange
		SetupMetadataResponse(CreateMetadataXml("""
			<EntityContainer Name="Container">
				<Singleton Name="Me" Type="Test.Person"/>
			</EntityContainer>
			"""));

		// Act
		var metadata = await _client.GetMetadataAsync(CancellationToken);

		// Assert
		metadata.Singletons.Should().ContainSingle();
		var singleton = metadata.Singletons[0];
		singleton.Name.Should().Be("Me");
		singleton.Type.Should().Be("Test.Person");
	}

	/// <summary>
	/// Tests GetMetadataAsync extracts function imports.
	/// </summary>
	[Fact]
	public async Task GetMetadataAsync_FunctionImport_Extracted()
	{
		// Arrange
		SetupMetadataResponse(CreateMetadataXml("""
			<EntityContainer Name="Container">
				<FunctionImport Name="GetTopProducts" Function="Test.GetTopProducts" EntitySet="Products"/>
			</EntityContainer>
			"""));

		// Act
		var metadata = await _client.GetMetadataAsync(CancellationToken);

		// Assert
		metadata.FunctionImports.Should().ContainSingle();
		var functionImport = metadata.FunctionImports[0];
		functionImport.Name.Should().Be("GetTopProducts");
		functionImport.Function.Should().Be("Test.GetTopProducts");
		functionImport.EntitySet.Should().Be("Products");
	}

	/// <summary>
	/// Tests GetMetadataAsync extracts action imports.
	/// </summary>
	[Fact]
	public async Task GetMetadataAsync_ActionImport_Extracted()
	{
		// Arrange
		SetupMetadataResponse(CreateMetadataXml("""
			<EntityContainer Name="Container">
				<ActionImport Name="ResetDatabase" Action="Test.ResetDatabase"/>
			</EntityContainer>
			"""));

		// Act
		var metadata = await _client.GetMetadataAsync(CancellationToken);

		// Assert
		metadata.ActionImports.Should().ContainSingle();
		var actionImport = metadata.ActionImports[0];
		actionImport.Name.Should().Be("ResetDatabase");
		actionImport.Action.Should().Be("Test.ResetDatabase");
	}

	#endregion

	#region ODataProperty Tests

	/// <summary>
	/// Tests ODataProperty IsCollection for collection type.
	/// </summary>
	[Fact]
	public void ODataProperty_IsCollection_TrueForCollectionType()
	{
		// Arrange
		var prop = new ODataProperty { Type = "Collection(Edm.String)" };

		// Assert
		prop.IsCollection.Should().BeTrue();
		prop.ElementType.Should().Be("Edm.String");
	}

	/// <summary>
	/// Tests ODataProperty IsCollection for non-collection type.
	/// </summary>
	[Fact]
	public void ODataProperty_IsCollection_FalseForScalarType()
	{
		// Arrange
		var prop = new ODataProperty { Type = "Edm.String" };

		// Assert
		prop.IsCollection.Should().BeFalse();
		prop.ElementType.Should().BeNull();
	}

	#endregion

	#region ODataNavigationProperty Tests

	/// <summary>
	/// Tests ODataNavigationProperty IsCollection for collection type.
	/// </summary>
	[Fact]
	public void ODataNavigationProperty_IsCollection_TrueForCollectionType()
	{
		// Arrange
		var navProp = new ODataNavigationProperty { Type = "Collection(Test.Order)" };

		// Assert
		navProp.IsCollection.Should().BeTrue();
		navProp.TargetType.Should().Be("Test.Order");
	}

	/// <summary>
	/// Tests ODataNavigationProperty IsCollection for single type.
	/// </summary>
	[Fact]
	public void ODataNavigationProperty_IsCollection_FalseForSingleType()
	{
		// Arrange
		var navProp = new ODataNavigationProperty { Type = "Test.Customer" };

		// Assert
		navProp.IsCollection.Should().BeFalse();
		navProp.TargetType.Should().Be("Test.Customer");
	}

	#endregion

	#region Helper Methods

	private void SetupMetadataResponse(string xml)
	{
		var response = new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent(xml, System.Text.Encoding.UTF8, "application/xml")
		};

		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(response);
	}

	private static string CreateMetadataXml(string schemaContent) => $"""
		<?xml version="1.0" encoding="utf-8"?>
		<edmx:Edmx Version="4.0" xmlns:edmx="http://docs.oasis-open.org/odata/ns/edmx">
			<edmx:DataServices>
				<Schema Namespace="Test" xmlns="http://docs.oasis-open.org/odata/ns/edm">
					{schemaContent}
				</Schema>
			</edmx:DataServices>
		</edmx:Edmx>
		""";

	#endregion
}
