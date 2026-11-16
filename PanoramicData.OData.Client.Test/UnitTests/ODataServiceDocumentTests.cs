namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Tests for ODataServiceDocument and related classes.
/// </summary>
public class ODataServiceDocumentTests
{
	#region ODataServiceDocument Tests

	/// <summary>
	/// Tests that EntitySets, Singletons and FunctionImports each return only their own kind.
	/// </summary>
	[Fact]
	public void KindFilters_ReturnOnlyMatchingResources()
	{
		var doc = new ODataServiceDocument();
		doc.Resources.Add(new ODataServiceResource { Name = "Products", Kind = ODataServiceResourceKind.EntitySet });
		doc.Resources.Add(new ODataServiceResource { Name = "Categories", Kind = ODataServiceResourceKind.EntitySet });
		doc.Resources.Add(new ODataServiceResource { Name = "Me", Kind = ODataServiceResourceKind.Singleton });
		doc.Resources.Add(new ODataServiceResource { Name = "Settings", Kind = ODataServiceResourceKind.Singleton });
		doc.Resources.Add(new ODataServiceResource { Name = "Search", Kind = ODataServiceResourceKind.FunctionImport });

		doc.EntitySets.Select(r => r.Name).Should().BeEquivalentTo("Products", "Categories");
		doc.Singletons.Select(r => r.Name).Should().BeEquivalentTo("Me", "Settings");
		doc.FunctionImports.Select(r => r.Name).Should().BeEquivalentTo("Search");
	}

	/// <summary>
	/// Tests that GetResource finds by name ignoring case, and returns null when absent.
	/// </summary>
	[Fact]
	public void GetResource_FindsByNameIgnoringCase()
	{
		var doc = new ODataServiceDocument();
		doc.Resources.Add(new ODataServiceResource { Name = "Products", Url = "Products" });
		doc.Resources.Add(new ODataServiceResource { Name = "Categories", Url = "Categories" });

		doc.GetResource("Products")!.Name.Should().Be("Products");
		doc.GetResource("products")!.Name.Should().Be("Products");
		doc.GetResource("NonExistent").Should().BeNull();
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
