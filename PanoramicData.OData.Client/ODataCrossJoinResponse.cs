namespace PanoramicData.OData.Client;

/// <summary>
/// Response from a cross-join query.
/// </summary>
public class ODataCrossJoinResponse
{
	/// <summary>
	/// Gets the result rows.
	/// </summary>
	public List<ODataCrossJoinResult> Value { get; } = [];

	/// <summary>
	/// Gets the total count of matching results (if requested).
	/// </summary>
	public long? Count { get; set; }

	/// <summary>
	/// Gets the next page link (if more results are available).
	/// </summary>
	public string? NextLink { get; set; }
}
