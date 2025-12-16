namespace PanoramicData.OData.Client;

/// <summary>
/// Represents a changeset within a batch request.
/// All operations in a changeset are atomic - they all succeed or all fail.
/// </summary>
public class ODataChangeset
{
	/// <summary>
	/// Gets or sets the unique identifier for this changeset.
	/// </summary>
	public string Id { get; set; } = $"changeset_{Guid.NewGuid():N}"[..20];

	/// <summary>
	/// Gets the operations in this changeset.
	/// </summary>
	public List<ODataBatchOperation> Operations { get; } = [];
}
