using System.Xml.Linq;

namespace PanoramicData.OData.Client;

/// <summary>
/// ODataMetadataParser - import and attribute parsing.
/// </summary>
internal static partial class ODataMetadataParser
{
	private static ODataFunctionImport ParseFunctionImport(XElement element) => new()
	{
		Name = element.Attribute("Name")?.Value ?? string.Empty,
		Function = element.Attribute("Function")?.Value ?? string.Empty,
		EntitySet = element.Attribute("EntitySet")?.Value,
		IncludeInServiceDocument = GetBoolAttribute(element, "IncludeInServiceDocument")
	};

	private static ODataActionImport ParseActionImport(XElement element) => new()
	{
		Name = element.Attribute("Name")?.Value ?? string.Empty,
		Action = element.Attribute("Action")?.Value ?? string.Empty,
		EntitySet = element.Attribute("EntitySet")?.Value
	};

	private static bool GetBoolAttribute(XElement element, string attributeName, bool defaultIfMissing = false) =>
		element.Attribute(attributeName)?.Value?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? defaultIfMissing;

	/// <summary>
	/// Parses the Nullable attribute which defaults to true if not specified.
	/// </summary>
	private static bool IsNullableAttribute(XElement element) =>
		!element.Attribute("Nullable")?.Value?.Equals("false", StringComparison.OrdinalIgnoreCase) ?? true;
}
