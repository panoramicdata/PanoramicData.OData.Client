namespace PanoramicData.OData.Client.Test.Models;

/// <summary>
/// Represents a RoleRight entity for testing navigation properties.
/// </summary>
public class RoleRight
{
	/// <summary>
	/// Gets or sets the ID.
	/// </summary>
	[JsonPropertyName("ID")]
	public int Id { get; set; }

	/// <summary>
	/// Gets or sets the name.
	/// </summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the code.
	/// </summary>
	public string? Code { get; set; }
}
