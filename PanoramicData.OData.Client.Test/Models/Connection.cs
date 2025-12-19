namespace PanoramicData.OData.Client.Test.Models;

/// <summary>
/// Represents a Connection entity for testing navigation properties with scalar property access.
/// </summary>
public class Connection
{
	/// <summary>
	/// Gets or sets the connection ID.
	/// </summary>
	[JsonPropertyName("ID")]
	public int Id { get; set; }

	/// <summary>
	/// Gets or sets the connection name.
	/// </summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the connection string.
	/// </summary>
	public string? ConnectionString { get; set; }

	/// <summary>
	/// Gets or sets the tenant (navigation property).
	/// </summary>
	public Tenant? Tenant { get; set; }

	/// <summary>
	/// Gets or sets the role (navigation property).
	/// </summary>
	public Role? Role { get; set; }

	/// <summary>
	/// Gets or sets the category (navigation property).
	/// </summary>
	public Category? Category { get; set; }
}
