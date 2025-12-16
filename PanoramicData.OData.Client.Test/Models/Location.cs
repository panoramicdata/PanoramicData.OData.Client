namespace PanoramicData.OData.Client.Test.Models;

/// <summary>
/// Represents a Location complex type from TripPin service.
/// </summary>
public class Location
{
	/// <summary>
	/// Gets or sets the address.
	/// </summary>
	public string? Address { get; set; }

	/// <summary>
	/// Gets or sets the city.
	/// </summary>
	public City? City { get; set; }
}
