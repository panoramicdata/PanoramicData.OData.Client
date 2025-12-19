namespace PanoramicData.OData.Client.Test.Models;

/// <summary>
/// Represents a Tenant entity for testing navigation properties with scalar properties.
/// </summary>
public class Tenant
{
	/// <summary>
	/// Gets or sets the tenant ID.
	/// </summary>
	[JsonPropertyName("ID")]
	public int Id { get; set; }

	/// <summary>
	/// Gets or sets the tenant name.
	/// </summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the tenant description.
	/// </summary>
	public string? Description { get; set; }

	/// <summary>
	/// Gets or sets whether the tenant is active.
	/// </summary>
	public bool IsActive { get; set; }

	/// <summary>
	/// Gets or sets the tenant's creation date.
	/// </summary>
	public DateTimeOffset? CreatedAt { get; set; }
}
