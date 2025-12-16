namespace PanoramicData.OData.Client.Test.Models;

/// <summary>
/// Represents a Person entity from the TripPin sample service.
/// Used for testing complex OData scenarios.
/// </summary>
public class Person
{
	/// <summary>
	/// Gets or sets the username (key).
	/// </summary>
	public string UserName { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the first name.
	/// </summary>
	public string FirstName { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the last name.
	/// </summary>
	public string LastName { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the middle name.
	/// </summary>
	public string? MiddleName { get; set; }

	/// <summary>
	/// Gets or sets the gender.
	/// </summary>
	public Gender? Gender { get; set; }

	/// <summary>
	/// Gets or sets the age.
	/// </summary>
	public long? Age { get; set; }

	/// <summary>
	/// Gets or sets the email addresses.
	/// </summary>
	public List<string>? Emails { get; set; }

	/// <summary>
	/// Gets or sets the address locations.
	/// </summary>
	public List<Location>? AddressInfo { get; set; }

	/// <summary>
	/// Gets or sets the home address.
	/// </summary>
	public Location? HomeAddress { get; set; }

	/// <summary>
	/// Gets or sets the favorite feature.
	/// </summary>
	public Feature? FavoriteFeature { get; set; }

	/// <summary>
	/// Gets or sets the features.
	/// </summary>
	public List<Feature>? Features { get; set; }

	/// <summary>
	/// Gets or sets the friends.
	/// </summary>
	public List<Person>? Friends { get; set; }

	/// <summary>
	/// Gets or sets the best friend.
	/// </summary>
	public Person? BestFriend { get; set; }

	/// <summary>
	/// Gets or sets the trips.
	/// </summary>
	public List<Trip>? Trips { get; set; }
}
