namespace PanoramicData.OData.Client.Test.Models;

/// <summary>
/// Coordinate reference system.
/// </summary>
public class CoordinateSystem
{
	/// <summary>
	/// Gets or sets the type.
	/// </summary>
	public string? Type { get; set; }

	/// <summary>
	/// Gets or sets the properties.
	/// </summary>
	public CrsProperties? Properties { get; set; }
}
