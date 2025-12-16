namespace PanoramicData.OData.Client.Test.Models;

/// <summary>
/// Represents a City complex type.
/// </summary>
public class City
{
	/// <summary>
	/// Gets or sets the name.
	/// </summary>
	public string? Name { get; set; }

	/// <summary>
	/// Gets or sets the country/region.
	/// </summary>
	public string? CountryRegion { get; set; }

	/// <summary>
	/// Gets or sets the region.
	/// </summary>
	public string? Region { get; set; }
}
