namespace PanoramicData.OData.Client.Test.Models;

/// <summary>
/// Represents a Supplier entity for testing complex relationships.
/// </summary>
public class Supplier
{
	/// <summary>
	/// Gets or sets the supplier ID.
	/// </summary>
	[JsonPropertyName("ID")]
	public int Id { get; set; }

	/// <summary>
	/// Gets or sets the supplier name.
	/// </summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the supplier's address.
	/// </summary>
	public Address? Address { get; set; }

	/// <summary>
	/// Gets or sets the concurrency token for optimistic concurrency.
	/// </summary>
	public int? Concurrency { get; set; }

	/// <summary>
	/// Gets or sets the products supplied by this supplier.
	/// </summary>
	public List<Product>? Products { get; set; }
}
