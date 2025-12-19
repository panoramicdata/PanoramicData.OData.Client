namespace PanoramicData.OData.Client.Test.Models;

/// <summary>
/// Represents a Role entity for testing navigation properties.
/// </summary>
public class Role
{
	/// <summary>
	/// Gets or sets the role ID.
	/// </summary>
	[JsonPropertyName("ID")]
	public int Id { get; set; }

	/// <summary>
	/// Gets or sets the role name.
	/// </summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the role rights (navigation property to collection).
	/// </summary>
	public List<RoleRight>? RoleRights { get; set; }
}
