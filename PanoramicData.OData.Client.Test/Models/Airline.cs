namespace PanoramicData.OData.Client.Test.Models;

/// <summary>
/// Represents an Airline entity from TripPin service.
/// </summary>
public class Airline
{
	/// <summary>
	/// Gets or sets the airline code (key).
	/// </summary>
	public string AirlineCode { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the airline name.
	/// </summary>
	public string? Name { get; set; }
}
