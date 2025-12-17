namespace PanoramicData.OData.Client;

/// <summary>
/// Represents a resource in an OData service document.
/// </summary>
public class ODataServiceResource
{
	/// <summary>
	/// Gets or sets the resource name.
	/// </summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the resource kind.
	/// </summary>
	public ODataServiceResourceKind Kind { get; set; } = ODataServiceResourceKind.EntitySet;

	/// <summary>
	/// Gets or sets the URL to access this resource.
	/// </summary>
	public string Url { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the human-readable title (optional).
	/// </summary>
	public string? Title { get; set; }
}
