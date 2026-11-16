namespace PanoramicData.OData.Client;

/// <summary>
/// Represents a raw OData response for legacy compatibility.
/// </summary>
/// <remarks>
/// This class is provided for backward compatibility with SimpleODataClient.
/// For new code, use <see cref="JsonDocument"/> directly via <see cref="ODataClient.GetRawAsync"/>.
/// </remarks>
/// <param name="document">The JSON document.</param>
[Obsolete("Use GetRawAsync() which returns a JsonDocument for more efficient JSON handling.")]
public class ODataRawResponse(JsonDocument document) : IDisposable
{
	private bool _disposed;

	/// <summary>
	/// Gets the raw JSON document.
	/// </summary>
	public JsonDocument Document => document;

	/// <summary>
	/// Gets the entries from the response as dictionaries.
	/// </summary>
	/// <returns>A collection of dictionaries representing the entities.</returns>
	public IEnumerable<IDictionary<string, object?>> GetEntries()
	{
		if (document.RootElement.TryGetProperty("value", out var valueElement) &&
			valueElement.ValueKind == JsonValueKind.Array)
		{
			foreach (var item in valueElement.EnumerateArray())
			{
				yield return ODataLegacyJson.LegacyJsonElementToDictionary(item);
			}
		}
	}

	/// <summary>
	/// Gets a single entry from the response as a dictionary.
	/// </summary>
	/// <returns>A dictionary representing the entity, or null if not found.</returns>
	public IDictionary<string, object?>? GetEntry()
	{
		// Check if it's a collection response
		if (document.RootElement.TryGetProperty("value", out var valueElement))
		{
			if (valueElement.ValueKind == JsonValueKind.Array)
			{
				var enumerator = valueElement.EnumerateArray();
				if (enumerator.MoveNext())
				{
					return ODataLegacyJson.LegacyJsonElementToDictionary(enumerator.Current);
				}

				return null;
			}
		}

		// Single entity response
		if (document.RootElement.ValueKind == JsonValueKind.Object)
		{
			return ODataLegacyJson.LegacyJsonElementToDictionary(document.RootElement);
		}

		return null;
	}

	/// <summary>
	/// Releases the resources used by this instance.
	/// </summary>
	public void Dispose()
	{
		if (!_disposed)
		{
			document.Dispose();
			_disposed = true;
		}

		GC.SuppressFinalize(this);
	}
}
