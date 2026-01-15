#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
using PanoramicData.OData.Client.Converters;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Tests for ODataTypeAnnotationConverter to verify @odata.type annotation is added for derived types.
/// </summary>
public class ODataTypeAnnotationConverterTests
{
	private readonly JsonSerializerOptions _options;

	public ODataTypeAnnotationConverterTests()
	{
		_options = new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true,
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			Converters =
			{
				new ODataTypeAnnotationConverter()
			}
		};
	}

	[Fact]
	public void Serialize_DerivedType_IncludesODataTypeAnnotation()
	{
		// Arrange
		var employee = new Employee
		{
			Id = 1,
			Name = "John Doe",
			Department = "Engineering"
		};

		// Act - serialize as base type Person
		var json = JsonSerializer.Serialize<Person>(employee, _options);

		// Assert
		json.Should().Contain("\"@odata.type\":\"#PanoramicData.OData.Client.Test.UnitTests.ODataTypeAnnotationConverterTests\\u002BEmployee\"");
		json.Should().Contain("\"id\":1");
		json.Should().Contain("\"name\":\"John Doe\"");
		json.Should().Contain("\"department\":\"Engineering\"");
	}

	[Fact]
	public void Serialize_BaseType_DoesNotIncludeODataTypeAnnotation()
	{
		// Arrange
		var person = new Person
		{
			Id = 1,
			Name = "Jane Doe"
		};

		// Act
		var json = JsonSerializer.Serialize<Person>(person, _options);

		// Assert
		json.Should().NotContain("@odata.type");
		json.Should().Contain("\"id\":1");
		json.Should().Contain("\"name\":\"Jane Doe\"");
	}

	[Fact]
	public void Serialize_DerivedTypeAsItself_DoesNotIncludeODataTypeAnnotation()
	{
		// Arrange
		var employee = new Employee
		{
			Id = 1,
			Name = "John Doe",
			Department = "Engineering"
		};

		// Act - serialize as Employee (not as base Person)
		var json = JsonSerializer.Serialize<Employee>(employee, _options);

		// Assert
		json.Should().NotContain("@odata.type");
		json.Should().Contain("\"id\":1");
		json.Should().Contain("\"name\":\"John Doe\"");
		json.Should().Contain("\"department\":\"Engineering\"");
	}

	[Fact]
	public void Serialize_TypeWithAlwaysIncludeAttribute_IncludesODataTypeAnnotation()
	{
		// Arrange
		var manager = new Manager
		{
			Id = 1,
			Name = "Alice Smith",
			Department = "Management",
			TeamSize = 10
		};

		// Act
		var json = JsonSerializer.Serialize<Manager>(manager, _options);

		// Assert
		json.Should().Contain("\"@odata.type\":\"#PanoramicData.OData.Client.Test.UnitTests.ODataTypeAnnotationConverterTests\\u002BManager\"");
		json.Should().Contain("\"teamSize\":10");
	}

	[Fact]
	public void Serialize_TypeWithCustomODataTypeName_UsesCustomName()
	{
		// Arrange
		var contractor = new Contractor
		{
			Id = 1,
			Name = "Bob Johnson",
			Company = "Acme Corp"
		};

		// Act
		var json = JsonSerializer.Serialize<Person>(contractor, _options);

		// Assert
		json.Should().Contain("\"@odata.type\":\"#CustomNamespace.ContractorType\"");
		json.Should().Contain("\"company\":\"Acme Corp\"");
	}

	[Fact]
	public void Deserialize_JsonWithODataTypeAnnotation_IgnoresAnnotation()
	{
		// Arrange
		var json = "{\"@odata.type\":\"#PanoramicData.OData.Client.Test.UnitTests.Employee\",\"id\":1,\"name\":\"John Doe\",\"department\":\"Engineering\"}";

		// Act
		var result = JsonSerializer.Deserialize<Employee>(json, _options);

		// Assert
		result.Should().NotBeNull();
		result!.Id.Should().Be(1);
		result.Name.Should().Be("John Doe");
		result.Department.Should().Be("Engineering");
	}

	[Fact]
	public void RoundTrip_SerializeAndDeserialize_PreservesData()
	{
		// Arrange
		var original = new Employee
		{
			Id = 1,
			Name = "John Doe",
			Department = "Engineering"
		};

		// Act
		var json = JsonSerializer.Serialize<Person>(original, _options);
		var deserialized = JsonSerializer.Deserialize<Employee>(json, _options);

		// Assert
		deserialized.Should().NotBeNull();
		deserialized!.Id.Should().Be(original.Id);
		deserialized.Name.Should().Be(original.Name);
		deserialized.Department.Should().Be(original.Department);
	}

	// Test entity classes
	public class Person
	{
		public int Id { get; set; }
		public string Name { get; set; } = string.Empty;
	}

	public class Employee : Person
	{
		public string Department { get; set; } = string.Empty;
	}

	[ODataTypeAnnotation(AlwaysInclude = true)]
	public class Manager : Employee
	{
		public int TeamSize { get; set; }
	}

	[ODataTypeAnnotation(TypeName = "#CustomNamespace.ContractorType")]
	public class Contractor : Person
	{
		public string Company { get; set; } = string.Empty;
	}
}
#pragma warning restore CS1591
