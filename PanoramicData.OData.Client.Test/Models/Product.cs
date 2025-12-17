namespace PanoramicData.OData.Client.Test.Models;

/// <summary>
/// Represents a Product entity from the OData sample service.
/// </summary>
public class Product
{
	/// <summary>
	/// Gets or sets the product ID.
	/// </summary>
	[JsonPropertyName("ID")]
	public int Id { get; set; }

	/// <summary>
	/// Gets or sets the product name.
	/// </summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the product description.
	/// </summary>
	public string? Description { get; set; }

	/// <summary>
	/// Gets or sets the release date.
	/// </summary>
	public DateTimeOffset? ReleaseDate { get; set; }

	/// <summary>
	/// Gets or sets the discontinued date.
	/// </summary>
	public DateTimeOffset? DiscontinuedDate { get; set; }

	/// <summary>
	/// Gets or sets the product rating.
	/// </summary>
	public int? Rating { get; set; }

	/// <summary>
	/// Gets or sets the product price.
	/// </summary>
	public decimal? Price { get; set; }
}
