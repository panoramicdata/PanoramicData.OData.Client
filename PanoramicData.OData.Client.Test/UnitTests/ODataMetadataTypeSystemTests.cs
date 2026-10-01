namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Unit tests for parsing the rest of the $metadata type system: complex types, enum types,
/// entity sets, singletons, operation imports, and documents split across several schemas.
/// </summary>
public class ODataMetadataTypeSystemTests : ODataMetadataTestBase
{
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
		var metadata = await Client.GetMetadataAsync(CancellationToken);

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
		var metadata = await Client.GetMetadataAsync(CancellationToken);

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
		var metadata = await Client.GetMetadataAsync(CancellationToken);

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
		var metadata = await Client.GetMetadataAsync(CancellationToken);

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
		var metadata = await Client.GetMetadataAsync(CancellationToken);

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
		var metadata = await Client.GetMetadataAsync(CancellationToken);
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
		var metadata = await Client.GetMetadataAsync(CancellationToken);
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
		var metadata = await Client.GetMetadataAsync(CancellationToken);

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
		var metadata = await Client.GetMetadataAsync(CancellationToken);

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
		var metadata = await Client.GetMetadataAsync(CancellationToken);

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

	#region Multi-Schema Tests

	/// <summary>
	/// Regression test: services like Northwind split types and EntityContainer across multiple Schema elements.
	/// The parser must read all schemas, not just the first.
	/// </summary>
	[Fact]
	public async Task Parse_MultipleSchemas_ParsesTypesAndEntitySetsFromAllSchemas()
	{
		// Arrange - two Schema elements: first has the EntityType, second has the EntityContainer
		var xml = """
			<?xml version="1.0" encoding="utf-8"?>
			<edmx:Edmx Version="4.0" xmlns:edmx="http://docs.oasis-open.org/odata/ns/edmx">
				<edmx:DataServices>
					<Schema Namespace="NorthwindModel" xmlns="http://docs.oasis-open.org/odata/ns/edm">
						<EntityType Name="Product">
							<Key><PropertyRef Name="ProductID" /></Key>
							<Property Name="ProductID" Type="Edm.Int32" Nullable="false" />
							<Property Name="ProductName" Type="Edm.String" />
						</EntityType>
					</Schema>
					<Schema Namespace="ODataWebExperimental.Northwind.Model" xmlns="http://docs.oasis-open.org/odata/ns/edm">
						<EntityContainer Name="NorthwindEntities">
							<EntitySet Name="Products" EntityType="NorthwindModel.Product" />
						</EntityContainer>
					</Schema>
				</edmx:DataServices>
			</edmx:Edmx>
			""";

		SetupMetadataResponse(xml);

		// Act
		var metadata = await Client.GetMetadataAsync(CancellationToken);

		// Assert
		metadata.EntityTypes.Should().ContainSingle(et => et.Name == "Product");
		metadata.EntitySets.Should().ContainSingle(es => es.Name == "Products");
	}


	#endregion
}
