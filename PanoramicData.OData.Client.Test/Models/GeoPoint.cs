namespace PanoramicData.OData.Client.Test.Models;

/// <summary>
/// Represents a geographic point for testing geo functions.
/// </summary>
public class GeoPoint
{
	/// <summary>
	/// Gets or sets the type (Point).
	/// </summary>
	public string Type { get; set; } = "Point";

	/// <summary>
	/// Gets or sets the coordinates [longitude, latitude].
	/// </summary>
	public double[]? Coordinates { get; set; }

	/// <summary>
	/// Gets or sets the coordinate reference system.
	/// </summary>
	public CoordinateSystem? Crs { get; set; }
}
