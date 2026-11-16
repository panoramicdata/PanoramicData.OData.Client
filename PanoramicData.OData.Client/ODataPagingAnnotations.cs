namespace PanoramicData.OData.Client;

/// <summary>
/// The count and link annotations of an OData collection response.
/// </summary>
internal readonly record struct ODataPagingAnnotations(long? Count, string? NextLink, string? DeltaLink)
{
	internal static ODataPagingAnnotations Read(JsonElement root) => new(
		root.TryGetProperty("@odata.count", out var count) ? count.GetInt64() : null,
		root.TryGetProperty("@odata.nextLink", out var next) ? next.GetString() : null,
		root.TryGetProperty("@odata.deltaLink", out var delta) ? delta.GetString() : null);
}
