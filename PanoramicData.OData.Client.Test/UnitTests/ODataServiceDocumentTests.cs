namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Tests for ODataServiceDocument and related classes.
/// </summary>
public class ODataServiceDocumentTests
{
	#region ODataServiceDocument Tests

	/// <summary>
	/// Tests that EntitySets property filters correctly.
	/// </summary>
	[Fact]
	public void EntitySets_FiltersCorrectly()
	{
		// Arrange
		var doc = new ODataServiceDocument();
		doc.Resources.Add(new ODataServiceResource { Name = "Products", Kind = ODataServiceResourceKind.EntitySet });
		doc.Resources.Add(new ODataServiceResource { Name = "Me", Kind = ODataServiceResourceKind.Singleton });
		doc.Resources.Add(new ODataServiceResource { Name = "Categories", Kind = ODataServiceResourceKind.EntitySet });

		// Act
		var entitySets = doc.EntitySets.ToList();

		// Assert
		entitySets.Should().HaveCount(2);
		entitySets.Should().Contain(r => r.Name == "Products");
		entitySets.Should().Contain(r => r.Name == "Categories");
	}

	/// <summary>
	/// Tests that Singletons property filters correctly.
	/// </summary>
	[Fact]
	public void Singletons_FiltersCorrectly()
	{
		// Arrange
		var doc = new ODataServiceDocument();
		doc.Resources.Add(new ODataServiceResource { Name = "Products", Kind = ODataServiceResourceKind.EntitySet });
		doc.Resources.Add(new ODataServiceResource { Name = "Me", Kind = ODataServiceResourceKind.Singleton });
		doc.Resources.Add(new ODataServiceResource { Name = "Settings", Kind = ODataServiceResourceKind.Singleton });

		// Act
		var singletons = doc.Singletons.ToList();

		// Assert
		singletons.Should().HaveCount(2);
		singletons.Should().Contain(r => r.Name == "Me");
		singletons.Should().Contain(r => r.Name == "Settings");
	}

	/// <summary>
	/// Tests that FunctionImports property filters correctly.
	/// </summary>
	[Fact]
	public void FunctionImports_FiltersCorrectly()
	{
		// Arrange
		var doc = new ODataServiceDocument();
		doc.Resources.Add(new ODataServiceResource { Name = "Products", Kind = ODataServiceResourceKind.EntitySet });
		doc.Resources.Add(new ODataServiceResource { Name = "GetTopProducts", Kind = ODataServiceResourceKind.FunctionImport });
		doc.Resources.Add(new ODataServiceResource { Name = "Search", Kind = ODataServiceResourceKind.FunctionImport });

		// Act
		var functions = doc.FunctionImports.ToList();

		// Assert
		functions.Should().HaveCount(2);
		functions.Should().Contain(r => r.Name == "GetTopProducts");
		functions.Should().Contain(r => r.Name == "Search");
	}

	/// <summary>
	/// Tests that GetResource finds a resource by name.
	/// </summary>
	[Fact]
	public void GetResource_FindsByName()
	{
		// Arrange
		var doc = new ODataServiceDocument();
		doc.Resources.Add(new ODataServiceResource { Name = "Products", Url = "Products" });
		doc.Resources.Add(new ODataServiceResource { Name = "Categories", Url = "Categories" });

		// Act
		var result = doc.GetResource("Products");

		// Assert
		result.Should().NotBeNull();
		result!.Name.Should().Be("Products");
	}

	/// <summary>
	/// Tests that GetResource is case insensitive.
	/// </summary>
	[Fact]
	public void GetResource_CaseInsensitive()
	{
		// Arrange
		var doc = new ODataServiceDocument();
		doc.Resources.Add(new ODataServiceResource { Name = "Products", Url = "Products" });

		// Act
		var result = doc.GetResource("products");

		// Assert
		result.Should().NotBeNull();
		result!.Name.Should().Be("Products");
	}

	/// <summary>
	/// Tests that GetResource returns null when resource not found.
	/// </summary>
	[Fact]
	public void GetResource_NotFound_ReturnsNull()
	{
		// Arrange
		var doc = new ODataServiceDocument();
		doc.Resources.Add(new ODataServiceResource { Name = "Products", Url = "Products" });

		// Act
		var result = doc.GetResource("NonExistent");

		// Assert
		result.Should().BeNull();
	}

	/// <summary>
	/// Tests that Context property can be set and retrieved.
	/// </summary>
	[Fact]
	public void Context_CanBeSetAndRetrieved()
	{
		// Arrange & Act
		var doc = new ODataServiceDocument
		{
			Context = "https://api.example.com/odata/$metadata"
		};

		// Assert
		doc.Context.Should().Be("https://api.example.com/odata/$metadata");
	}

	#endregion

	#region ODataServiceResource Tests

	/// <summary>
	/// Tests the default values of ODataServiceResource.
	/// </summary>
	[Fact]
	public void ODataServiceResource_DefaultValues()
	{
		// Arrange & Act
		var resource = new ODataServiceResource();

		// Assert
		resource.Name.Should().Be(string.Empty);
		resource.Kind.Should().Be(ODataServiceResourceKind.EntitySet);
		resource.Url.Should().Be(string.Empty);
		resource.Title.Should().BeNull();
	}

	/// <summary>
	/// Tests that all properties of ODataServiceResource can be set.
	/// </summary>
	[Fact]
	public void ODataServiceResource_AllPropertiesCanBeSet()
	{
		// Arrange & Act
		var resource = new ODataServiceResource
		{
			Name = "Products",
			Kind = ODataServiceResourceKind.EntitySet,
			Url = "Products",
			Title = "Product Catalog"
		};

		// Assert
		resource.Name.Should().Be("Products");
		resource.Kind.Should().Be(ODataServiceResourceKind.EntitySet);
		resource.Url.Should().Be("Products");
		resource.Title.Should().Be("Product Catalog");
	}

	#endregion

	#region ODataServiceResourceKind Tests

	/// <summary>
	/// Tests that ODataServiceResourceKind has expected enum values.
	/// </summary>
	[Fact]
	public void ODataServiceResourceKind_HasExpectedValues()
	{
		// Assert
		Enum.GetNames<ODataServiceResourceKind>().Should().Contain("EntitySet");
		Enum.GetNames<ODataServiceResourceKind>().Should().Contain("Singleton");
		Enum.GetNames<ODataServiceResourceKind>().Should().Contain("FunctionImport");
		Enum.GetNames<ODataServiceResourceKind>().Should().Contain("ServiceDocument");
	}

	#endregion
}
