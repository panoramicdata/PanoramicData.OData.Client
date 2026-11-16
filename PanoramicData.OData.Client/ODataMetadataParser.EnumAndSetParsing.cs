using System.Xml.Linq;

namespace PanoramicData.OData.Client;

/// <summary>
/// ODataMetadataParser - enum, entity set and import parsing.
/// </summary>
internal static partial class ODataMetadataParser
{
	private static ODataEnumType ParseEnumType(XElement element)
	{
		var enumType = new ODataEnumType
		{
			Name = element.Attribute("Name")?.Value ?? string.Empty,
			UnderlyingType = element.Attribute("UnderlyingType")?.Value ?? "Edm.Int32",
			IsFlags = GetBoolAttribute(element, "IsFlags")
		};

		ParseEnumMembers(element, enumType);
		return enumType;
	}

	private static void ParseEnumMembers(XElement element, ODataEnumType enumType)
	{
		foreach (var memberElement in element.Elements().Where(e => e.Name.LocalName == "Member"))
		{
			var member = new ODataEnumMember
			{
				Name = memberElement.Attribute("Name")?.Value ?? string.Empty
			};

			if (long.TryParse(memberElement.Attribute("Value")?.Value, out var value))
			{
				member.Value = value;
			}

			enumType.Members.Add(member);
		}
	}

	private static ODataEntitySet ParseEntitySet(XElement element)
	{
		var entitySet = new ODataEntitySet
		{
			Name = element.Attribute("Name")?.Value ?? string.Empty,
			EntityType = element.Attribute("EntityType")?.Value ?? string.Empty
		};

		ParseNavigationPropertyBindings(element, entitySet.NavigationPropertyBindings);
		return entitySet;
	}

	private static ODataSingleton ParseSingleton(XElement element)
	{
		var singleton = new ODataSingleton
		{
			Name = element.Attribute("Name")?.Value ?? string.Empty,
			Type = element.Attribute("Type")?.Value ?? string.Empty
		};

		ParseNavigationPropertyBindings(element, singleton.NavigationPropertyBindings);
		return singleton;
	}

	private static void ParseNavigationPropertyBindings(XElement element, List<ODataNavigationPropertyBinding> bindings)
	{
		foreach (var bindingElement in element.Elements().Where(e => e.Name.LocalName == "NavigationPropertyBinding"))
		{
			bindings.Add(new ODataNavigationPropertyBinding
			{
				Path = bindingElement.Attribute("Path")?.Value ?? string.Empty,
				Target = bindingElement.Attribute("Target")?.Value ?? string.Empty
			});
		}
	}

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
