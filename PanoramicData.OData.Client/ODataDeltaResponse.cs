namespace PanoramicData.OData.Client;

/// <summary>
/// Represents a delta response from an OData query with change tracking.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public class ODataDeltaResponse<T>
{
	/// <summary>
	/// Entities that were added or modified since the last query.
	/// </summary>
	public List<T> Value { get; set; } = [];

	/// <summary>
	/// Entity keys that were deleted since the last query.
	/// </summary>
	public List<ODataDeletedEntity> Deleted { get; set; } = [];

	/// <summary>
	/// The delta link for the next delta query.
	/// </summary>
	public string? DeltaLink { get; set; }

	/// <summary>
	/// The next link for paging within the delta response.
	/// </summary>
	public string? NextLink { get; set; }

	/// <summary>
	/// The total count of changes (if $count=true was requested).
	/// </summary>
	public long? Count { get; set; }
}

/// <summary>
/// Represents a deleted entity in a delta response.
/// </summary>
public class ODataDeletedEntity
{
	/// <summary>
	/// The ID of the deleted entity (the @odata.id value).
	/// </summary>
	public string? Id { get; set; }

	/// <summary>
	/// The reason for removal. Can be "deleted" or "changed" (for filtered out entities).
	/// </summary>
	public string? Reason { get; set; }
}
