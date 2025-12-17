namespace PanoramicData.OData.Client.Test.Models;

/// <summary>
/// Represents a Category entity for testing navigation properties and relationships.
/// </summary>
public class Category
{
	/// <summary>
	/// Gets or sets the category ID.
	/// </summary>
	[JsonPropertyName("ID")]
	public int Id { get; set; }

	/// <summary>
	/// Gets or sets the category name.
	/// </summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the products in this category.
	/// </summary>
	public List<Product>? Products { get; set; }
}
