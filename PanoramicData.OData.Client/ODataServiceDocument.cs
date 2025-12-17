namespace PanoramicData.OData.Client;

/// <summary>
/// Represents an OData service document.
/// </summary>
public class ODataServiceDocument
{
	/// <summary>
	/// Gets or sets the @odata.context URL.
	/// </summary>
	public string? Context { get; set; }

	/// <summary>
	/// Gets the list of available resources (entity sets, singletons, function imports).
	/// </summary>
	public List<ODataServiceResource> Resources { get; } = [];

	/// <summary>
	/// Gets all entity sets from the service document.
	/// </summary>
	public IEnumerable<ODataServiceResource> EntitySets
		=> Resources.Where(r => r.Kind == ODataServiceResourceKind.EntitySet);

	/// <summary>
	/// Gets all singletons from the service document.
	/// </summary>
	public IEnumerable<ODataServiceResource> Singletons
		=> Resources.Where(r => r.Kind == ODataServiceResourceKind.Singleton);

	/// <summary>
	/// Gets all function imports from the service document.
	/// </summary>
	public IEnumerable<ODataServiceResource> FunctionImports
		=> Resources.Where(r => r.Kind == ODataServiceResourceKind.FunctionImport);

	/// <summary>
	/// Gets a resource by name.
	/// </summary>
	/// <param name="name">The resource name.</param>
	/// <returns>The resource, or null if not found.</returns>
	public ODataServiceResource? GetResource(string name)
		=> Resources.FirstOrDefault(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
}
