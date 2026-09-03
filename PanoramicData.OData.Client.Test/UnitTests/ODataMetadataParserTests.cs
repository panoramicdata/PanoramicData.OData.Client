namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Unit tests for parsing entity types and their navigation properties out of $metadata.
/// </summary>
public class ODataMetadataParserTests : ODataMetadataTestBase
{
	#region GetMetadataAsync Tests

	/// <summary>
	/// Tests GetMetadataAsync extracts namespace.
	/// </summary>
	[Fact]
	public async Task GetMetadataAsync_ExtractsNamespace()
	{
		// Arrange
		SetupMetadataResponse(CreateMetadataXml(string.Empty, "TestNamespace"));

		// Act
		var metadata = await Client.GetMetadataAsync(CancellationToken);

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
		SetupMetadataResponse(CreateMetadataXml(string.Empty));

		// Act
		var metadata = await Client.GetMetadataAsync(CancellationToken);

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
		var metadata = await Client.GetMetadataAsync(CancellationToken);

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
		var metadata = await Client.GetMetadataAsync(CancellationToken);

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
		var metadata = await Client.GetMetadataAsync(CancellationToken);

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
		var metadata = await Client.GetMetadataAsync(CancellationToken);

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
		var metadata = await Client.GetMetadataAsync(CancellationToken);

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
		var metadata = await Client.GetMetadataAsync(CancellationToken);

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
		var metadata = await Client.GetMetadataAsync(CancellationToken);

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
		var metadata = await Client.GetMetadataAsync(CancellationToken);

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
		var metadata = await Client.GetMetadataAsync(CancellationToken);

		// Assert
		var navProp = metadata.EntityTypes[0].GetNavigationProperty("Customer");
		navProp!.Partner.Should().Be("Orders");
	}

	#endregion
}
