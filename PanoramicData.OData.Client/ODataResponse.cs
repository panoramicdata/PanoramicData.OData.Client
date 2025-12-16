namespace PanoramicData.OData.Client;

/// <summary>
/// Represents the response from an OData query.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public class ODataResponse<T>
{
	/// <summary>
	/// The collection of entities returned.
	/// </summary>
	public List<T> Value { get; set; } = [];

	/// <summary>
	/// The total count of entities (if $count=true was requested).
	/// </summary>
	public long? Count { get; set; }

	/// <summary>
	/// The URL for the next page of results (for server-side paging).
	/// </summary>
	public string? NextLink { get; set; }

	/// <summary>
	/// The URL for the delta link (for change tracking).
	/// </summary>
	public string? DeltaLink { get; set; }

	/// <summary>
	/// The ETag value from the response (for concurrency control).
	/// </summary>
	public string? ETag { get; set; }
}

/// <summary>
/// Represents a single entity response from OData.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public class ODataSingleResponse<T>
{
	/// <summary>
	/// The entity returned.
	/// </summary>
	public T? Value { get; set; }

	/// <summary>
	/// The OData context URL.
	/// </summary>
	public string? Context { get; set; }

	/// <summary>
	/// The ETag value from the response (for concurrency control).
	/// </summary>
	public string? ETag { get; set; }
}
