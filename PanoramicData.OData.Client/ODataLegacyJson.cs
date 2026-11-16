namespace PanoramicData.OData.Client;

/// <summary>
/// Legacy helper methods for JSON conversion.
/// </summary>
internal static class ODataLegacyJson
{
	/// <summary>
	/// Converts a JsonElement to a dictionary (for legacy fluent API support).
	/// </summary>
	internal static Dictionary<string, object?> LegacyJsonElementToDictionary(JsonElement element)
	{
		var dict = new Dictionary<string, object?>();

		foreach (var property in element.EnumerateObject())
		{
			dict[property.Name] = LegacyJsonElementToObject(property.Value);
		}

		return dict;
	}

	private static object? LegacyJsonElementToObject(JsonElement element) => element.ValueKind switch
	{
		JsonValueKind.Null => null,
		JsonValueKind.True or JsonValueKind.False => element.GetBoolean(),
		JsonValueKind.Number when element.TryGetInt64(out var l) => l,
		JsonValueKind.Number when element.TryGetDouble(out var d) => d,
		JsonValueKind.Number => element.GetDecimal(),
		JsonValueKind.String when element.TryGetDateTime(out var dt) => dt,
		JsonValueKind.String when element.TryGetGuid(out var g) => g,
		JsonValueKind.String => element.GetString(),
		JsonValueKind.Array => element.EnumerateArray().Select(LegacyJsonElementToObject).ToList(),
		JsonValueKind.Object => LegacyJsonElementToDictionary(element),
		_ => element.GetRawText()
	};
}
