namespace PanoramicData.OData.Client.Test.Models;

/// <summary>
/// Represents a Trip entity from TripPin service.
/// </summary>
public class Trip
{
	/// <summary>
	/// Gets or sets the trip ID.
	/// </summary>
	public int TripId { get; set; }

	/// <summary>
	/// Gets or sets the share ID.
	/// </summary>
	public Guid? ShareId { get; set; }

	/// <summary>
	/// Gets or sets the name.
	/// </summary>
	public string? Name { get; set; }

	/// <summary>
	/// Gets or sets the budget.
	/// </summary>
	public float? Budget { get; set; }

	/// <summary>
	/// Gets or sets the description.
	/// </summary>
	public string? Description { get; set; }

	/// <summary>
	/// Gets or sets the tags.
	/// </summary>
	public List<string>? Tags { get; set; }

	/// <summary>
	/// Gets or sets the start time.
	/// </summary>
	public DateTimeOffset? StartsAt { get; set; }

	/// <summary>
	/// Gets or sets the end time.
	/// </summary>
	public DateTimeOffset? EndsAt { get; set; }
}
