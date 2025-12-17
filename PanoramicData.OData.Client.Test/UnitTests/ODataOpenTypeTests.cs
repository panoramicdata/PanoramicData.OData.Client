#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
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

	[Fact]
	public void OpenType_GetDynamicLong_ShouldParseLong()
	{
		// Arrange
		var json = """{ "Id": 1, "Name": "Test", "BigNumber": 9999999999999 }""";

		// Act
		var entity = JsonSerializer.Deserialize<TestOpenEntity>(json)!;
		var value = entity.GetDynamicLong("BigNumber");

		// Assert
		value.Should().HaveValue();
		value!.Value.Should().Be(9999999999999L);
	}

	[Fact]
	public void OpenType_GetDynamicLong_NonNumber_ReturnsNull()
	{
		// Arrange
		var json = """{ "Id": 1, "Name": "Test", "Text": "not a number" }""";

		// Act
		var entity = JsonSerializer.Deserialize<TestOpenEntity>(json)!;
		var value = entity.GetDynamicLong("Text");

		// Assert
		value.Should().BeNull();
	}

	[Fact]
	public void OpenType_GetDynamicDouble_ShouldParseDouble()
	{
		// Arrange
		var json = """{ "Id": 1, "Name": "Test", "Rate": 3.14159 }""";

		// Act
		var entity = JsonSerializer.Deserialize<TestOpenEntity>(json)!;
		var value = entity.GetDynamicDouble("Rate");

		// Assert
		value.Should().HaveValue();
		value!.Value.Should().BeApproximately(3.14159, 0.00001);
	}

	[Fact]
	public void OpenType_GetDynamicDouble_NonNumber_ReturnsNull()
	{
		// Arrange
		var json = """{ "Id": 1, "Name": "Test", "Text": "not a number" }""";

		// Act
		var entity = JsonSerializer.Deserialize<TestOpenEntity>(json)!;
		var value = entity.GetDynamicDouble("Text");

		// Assert
		value.Should().BeNull();
	}

	[Fact]
	public void OpenType_GetDynamicDateTimeOffset_ShouldParse()
	{
		// Arrange
		var json = """{ "Id": 1, "Name": "Test", "ModifiedAt": "2024-01-15T10:30:00+02:00" }""";

		// Act
		var entity = JsonSerializer.Deserialize<TestOpenEntity>(json)!;
		var value = entity.GetDynamicDateTimeOffset("ModifiedAt");

		// Assert
		value.Should().HaveValue();
		value!.Value.Year.Should().Be(2024);
		value.Value.Month.Should().Be(1);
		value.Value.Day.Should().Be(15);
	}

	[Fact]
	public void OpenType_GetDynamicDateTimeOffset_Invalid_ReturnsNull()
	{
		// Arrange
		var json = """{ "Id": 1, "Name": "Test", "Number": 123 }""";

		// Act
		var entity = JsonSerializer.Deserialize<TestOpenEntity>(json)!;
		var value = entity.GetDynamicDateTimeOffset("Number");

		// Assert
		value.Should().BeNull();
	}

	[Fact]
	public void OpenType_GetDynamicBool_False_ReturnsFalse()
	{
		// Arrange
		var json = """{ "Id": 1, "Name": "Test", "IsActive": false }""";

		// Act
		var entity = JsonSerializer.Deserialize<TestOpenEntity>(json)!;
		var value = entity.GetDynamicBool("IsActive");

		// Assert
		value.Should().HaveValue();
		value!.Value.Should().BeFalse();
	}

	[Fact]
	public void OpenType_GetDynamicBool_NonBool_ReturnsNull()
	{
		// Arrange
		var json = """{ "Id": 1, "Name": "Test", "Number": 1 }""";

		// Act
		var entity = JsonSerializer.Deserialize<TestOpenEntity>(json)!;
		var value = entity.GetDynamicBool("Number");

		// Assert
		value.Should().BeNull();
	}

	[Fact]
	public void OpenType_GetDynamicInt_NonNumber_ReturnsNull()
	{
		// Arrange
		var json = """{ "Id": 1, "Name": "Test", "Text": "not a number" }""";

		// Act
		var entity = JsonSerializer.Deserialize<TestOpenEntity>(json)!;
		var value = entity.GetDynamicInt("Text");

		// Assert
		value.Should().BeNull();
	}

	[Fact]
	public void OpenType_GetDynamicDateTime_Invalid_ReturnsNull()
	{
		// Arrange
		var json = """{ "Id": 1, "Name": "Test", "Number": 123 }""";

		// Act
		var entity = JsonSerializer.Deserialize<TestOpenEntity>(json)!;
		var value = entity.GetDynamicDateTime("Number");

		// Assert
		value.Should().BeNull();
	}

	[Fact]
	public void OpenType_GetDynamicGuid_Invalid_ReturnsNull()
	{
		// Arrange
		var json = """{ "Id": 1, "Name": "Test", "Number": 123 }""";

		// Act
		var entity = JsonSerializer.Deserialize<TestOpenEntity>(json)!;
		var value = entity.GetDynamicGuid("Number");

		// Assert
		value.Should().BeNull();
	}

	[Fact]
	public void OpenType_GetDynamicProperty_NotFound_ReturnsDefault()
	{
		// Arrange
		var entity = new TestOpenEntity { Id = 1, Name = "Test" };

		// Act
		var value = entity.GetDynamicProperty<TestAddress>("NonExistent");

		// Assert
		value.Should().BeNull();
	}

	[Fact]
	public void OpenType_RemoveDynamicProperty_NotExists_ReturnsFalse()
	{
		// Arrange
		var entity = new TestOpenEntity { Id = 1, Name = "Test" };

		// Act
		var result = entity.RemoveDynamicProperty("NonExistent");

		// Assert
		result.Should().BeFalse();
	}

	[Fact]
	public void OpenType_GetDynamicPropertyNames_Empty_ReturnsEmpty()
	{
		// Arrange
		var entity = new TestOpenEntity { Id = 1, Name = "Test" };

		// Act
		var names = entity.GetDynamicPropertyNames().ToList();

		// Assert
		names.Should().BeEmpty();
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
