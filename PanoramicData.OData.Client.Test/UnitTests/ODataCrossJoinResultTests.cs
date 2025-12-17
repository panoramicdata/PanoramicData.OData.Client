using System.Text.Json;

namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Tests for ODataCrossJoinResult.
/// </summary>
public class ODataCrossJoinResultTests
{
	/// <summary>
	/// Tests that Entities dictionary is initially empty.
	/// </summary>
	[Fact]
	public void Entities_InitiallyEmpty()
	{
		// Arrange & Act
		var result = new ODataCrossJoinResult();

		// Assert
		result.Entities.Should().BeEmpty();
	}

	/// <summary>
	/// Tests that HasEntity returns false when entity not present.
	/// </summary>
	[Fact]
	public void HasEntity_ReturnsFalse_WhenNotPresent()
	{
		// Arrange
		var result = new ODataCrossJoinResult();

		// Act & Assert
		result.HasEntity("Products").Should().BeFalse();
	}

	/// <summary>
	/// Tests that HasEntity returns true when entity is present.
	/// </summary>
	[Fact]
	public void HasEntity_ReturnsTrue_WhenPresent()
	{
		// Arrange
		var result = new ODataCrossJoinResult();
		var json = JsonDocument.Parse("{\"Id\": 1, \"Name\": \"Test\"}");
		result.Entities["Products"] = json.RootElement;

		// Act & Assert
		result.HasEntity("Products").Should().BeTrue();
	}

	/// <summary>
	/// Tests that GetEntity returns default when entity not present.
	/// </summary>
	[Fact]
	public void GetEntity_ReturnsDefault_WhenNotPresent()
	{
		// Arrange
		var result = new ODataCrossJoinResult();

		// Act
		var entity = result.GetEntity<TestProduct>("Products");

		// Assert
		entity.Should().BeNull();
	}

	/// <summary>
	/// Tests that GetEntity deserializes entity correctly.
	/// </summary>
	[Fact]
	public void GetEntity_DeserializesCorrectly()
	{
		// Arrange
		var result = new ODataCrossJoinResult();
		var json = JsonDocument.Parse("{\"Id\": 123, \"Name\": \"Widget\"}");
		result.Entities["Products"] = json.RootElement;

		// Act
		var entity = result.GetEntity<TestProduct>("Products");

		// Assert
		entity.Should().NotBeNull();
		entity!.Id.Should().Be(123);
		entity.Name.Should().Be("Widget");
	}

	/// <summary>
	/// Tests that GetEntity uses custom serializer options.
	/// </summary>
	[Fact]
	public void GetEntity_WithCustomOptions_UsesOptions()
	{
		// Arrange
		var result = new ODataCrossJoinResult();
		var json = JsonDocument.Parse("{\"id\": 123, \"name\": \"Widget\"}");
		result.Entities["Products"] = json.RootElement;

		var options = new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true
		};

		// Act
		var entity = result.GetEntity<TestProduct>("Products", options);

		// Assert
		entity.Should().NotBeNull();
		entity!.Id.Should().Be(123);
		entity.Name.Should().Be("Widget");
	}

	/// <summary>
	/// Tests that multiple entities can be stored.
	/// </summary>
	[Fact]
	public void MultipleEntities_CanBeStored()
	{
		// Arrange
		var result = new ODataCrossJoinResult();
		var productJson = JsonDocument.Parse("{\"Id\": 1, \"Name\": \"Widget\"}");
		var categoryJson = JsonDocument.Parse("{\"Id\": 10, \"CategoryName\": \"Electronics\"}");
		
		result.Entities["Products"] = productJson.RootElement;
		result.Entities["Categories"] = categoryJson.RootElement;

		// Act & Assert
		result.HasEntity("Products").Should().BeTrue();
		result.HasEntity("Categories").Should().BeTrue();
		result.Entities.Should().HaveCount(2);
	}

	/// <summary>
	/// Test entity for deserialization.
	/// </summary>
	private sealed class TestProduct
	{
		/// <summary>
		/// Gets or sets the product ID.
		/// </summary>
		public int Id { get; set; }

		/// <summary>
		/// Gets or sets the product name.
		/// </summary>
		public string Name { get; set; } = string.Empty;
	}
}
