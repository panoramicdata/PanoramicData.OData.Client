#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
using AwesomeAssertions;
using System.Text.Json;

namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Tests for open type / dynamic property support.
/// </summary>
public class ODataOpenTypeTests
{
	[Fact]
	public void OpenType_ShouldDeserializeDynamicProperties()
	{
		// Arrange
		var json = """
		{
			"Id": 1,
			"Name": "Test Entity",
			"CustomField1": "Custom Value",
			"CustomField2": 42,
			"CustomField3": true
		}
		""";

		// Act
		var entity = JsonSerializer.Deserialize<TestOpenEntity>(json);

		// Assert
		entity.Should().NotBeNull();
		entity.Id.Should().Be(1);
		entity.Name.Should().Be("Test Entity");
		entity.HasDynamicProperty("CustomField1").Should().BeTrue();
		entity.HasDynamicProperty("CustomField2").Should().BeTrue();
		entity.HasDynamicProperty("CustomField3").Should().BeTrue();
		entity.GetDynamicString("CustomField1").Should().Be("Custom Value");
		entity.GetDynamicInt("CustomField2").Should().Be(42);
		entity.GetDynamicBool("CustomField3").Should().BeTrue();
	}

	[Fact]
	public void OpenType_GetDynamicProperty_ShouldDeserializeComplexType()
	{
		// Arrange
		var json = """
		{
			"Id": 1,
			"Name": "Test",
			"Address": {
				"Street": "123 Main St",
				"City": "Seattle"
			}
		}
		""";

		// Act
		var entity = JsonSerializer.Deserialize<TestOpenEntity>(json);

		// Assert
		entity.Should().NotBeNull();
		var address = entity.GetDynamicProperty<TestAddress>("Address");
		address.Should().NotBeNull();
		address.Street.Should().Be("123 Main St");
		address.City.Should().Be("Seattle");
	}

	[Fact]
	public void OpenType_SetDynamicProperty_ShouldAddProperty()
	{
		// Arrange
		var entity = new TestOpenEntity { Id = 1, Name = "Test" };

		// Act
		entity.SetDynamicProperty("CustomField", "Custom Value");

		// Assert
		entity.HasDynamicProperty("CustomField").Should().BeTrue();
		entity.GetDynamicString("CustomField").Should().Be("Custom Value");
	}

	[Fact]
	public void OpenType_RemoveDynamicProperty_ShouldRemoveProperty()
	{
		// Arrange
		var json = """{ "Id": 1, "Name": "Test", "ToRemove": "value" }""";
		var entity = JsonSerializer.Deserialize<TestOpenEntity>(json)!;

		// Act
		var removed = entity.RemoveDynamicProperty("ToRemove");

		// Assert
		removed.Should().BeTrue();
		entity.HasDynamicProperty("ToRemove").Should().BeFalse();
	}

	[Fact]
	public void OpenType_GetDynamicPropertyNames_ShouldReturnAllNames()
	{
		// Arrange
		var json = """
		{
			"Id": 1,
			"Name": "Test",
			"Field1": "a",
			"Field2": "b",
			"Field3": "c"
		}
		""";

		// Act
		var entity = JsonSerializer.Deserialize<TestOpenEntity>(json)!;
		var names = entity.GetDynamicPropertyNames().ToList();

		// Assert
		names.Should().Contain("Field1");
		names.Should().Contain("Field2");
		names.Should().Contain("Field3");
	}

	[Fact]
	public void OpenType_GetDynamicDateTime_ShouldParseDate()
	{
		// Arrange
		var json = """{ "Id": 1, "Name": "Test", "CreatedAt": "2024-01-15T10:30:00Z" }""";

		// Act
		var entity = JsonSerializer.Deserialize<TestOpenEntity>(json)!;
		var date = entity.GetDynamicDateTime("CreatedAt");

		// Assert
		date.Should().HaveValue();
		date!.Value.Year.Should().Be(2024);
		date.Value.Month.Should().Be(1);
		date.Value.Day.Should().Be(15);
	}

	[Fact]
	public void OpenType_GetDynamicGuid_ShouldParseGuid()
	{
		// Arrange
		var expectedGuid = Guid.NewGuid();
		var json = $$$"""{ "Id": 1, "Name": "Test", "UniqueId": "{{{expectedGuid}}}" }""";

		// Act
		var entity = JsonSerializer.Deserialize<TestOpenEntity>(json)!;
		var guid = entity.GetDynamicGuid("UniqueId");

		// Assert
		guid.Should().HaveValue();
		guid!.Value.Should().Be(expectedGuid);
	}

	[Fact]
	public void OpenType_NonExistentProperty_ShouldReturnDefault()
	{
		// Arrange
		var entity = new TestOpenEntity { Id = 1, Name = "Test" };

		// Act & Assert
		entity.GetDynamicString("NonExistent").Should().BeNull();
		entity.GetDynamicInt("NonExistent").Should().BeNull();
		entity.GetDynamicBool("NonExistent").Should().BeNull();
		entity.HasDynamicProperty("NonExistent").Should().BeFalse();
	}

	// Test entity that inherits from ODataOpenType
	public class TestOpenEntity : ODataOpenType
	{
		public int Id { get; set; }
		public string Name { get; set; } = string.Empty;
	}

	public class TestAddress
	{
		public string Street { get; set; } = string.Empty;
		public string City { get; set; } = string.Empty;
	}
}
#pragma warning restore CS1591
