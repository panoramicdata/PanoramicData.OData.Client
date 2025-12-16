namespace PanoramicData.OData.Client.Test.Models;

/// <summary>
/// Represents an AirportLocation complex type with geo coordinates.
/// </summary>
public class AirportLocation : Location
{
	/// <summary>
	/// Gets or sets the geo location (for testing geo functions).
	/// </summary>
	public GeoPoint? Loc { get; set; }
}
