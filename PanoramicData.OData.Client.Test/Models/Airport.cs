namespace PanoramicData.OData.Client.Test.Models;

/// <summary>
/// Represents an Airport entity from TripPin service.
/// </summary>
public class Airport
{
	/// <summary>
	/// Gets or sets the ICAO code (key).
	/// </summary>
	public string IcaoCode { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the name.
	/// </summary>
	public string? Name { get; set; }

	/// <summary>
	/// Gets or sets the IATA code.
	/// </summary>
	public string? IataCode { get; set; }

	/// <summary>
	/// Gets or sets the location.
	/// </summary>
	public AirportLocation? Location { get; set; }
}
