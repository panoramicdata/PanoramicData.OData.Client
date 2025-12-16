namespace PanoramicData.OData.Client.Test.Models;

/// <summary>
/// Represents an Address complex type for testing nested complex types.
/// </summary>
public class Address
{
	/// <summary>
	/// Gets or sets the street address.
	/// </summary>
	public string? Street { get; set; }

	/// <summary>
	/// Gets or sets the city.
	/// </summary>
	public string? City { get; set; }

	/// <summary>
	/// Gets or sets the state or province.
	/// </summary>
	public string? State { get; set; }

	/// <summary>
	/// Gets or sets the ZIP or postal code.
	/// </summary>
	public string? ZipCode { get; set; }

	/// <summary>
	/// Gets or sets the country.
	/// </summary>
	public string? Country { get; set; }
}
