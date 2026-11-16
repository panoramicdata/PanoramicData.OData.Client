namespace PanoramicData.OData.Client;

/// <summary>
/// Shared list handling for the query builders' string options.
/// </summary>
internal static class ODataListText
{
	internal static void AddCommaSeparated(List<string> target, string value)
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			target.AddRange(value.Split(',').Select(f => f.Trim()));
		}
	}

	internal static void AddIfNotBlank(List<string> target, string value)
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			target.Add(value);
		}
	}
}
