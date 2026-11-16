using System.Xml.Linq;

namespace PanoramicData.OData.Client;

/// <summary>
/// ODataMetadataParser - Type parsing operations.
/// </summary>
internal static partial class ODataMetadataParser
{
	private static ODataEntityType ParseEntityType(XElement element)
	{
		var entityType = CreateEntityTypeFromAttributes(element);
		ParseEntityTypeKey(element, entityType);
		ParseProperties(element, entityType.Properties);
		ParseNavigationProperties(element, entityType.NavigationProperties);
		return entityType;
	}

	private static ODataEntityType CreateEntityTypeFromAttributes(XElement element) => new()
	{
		Name = element.Attribute("Name")?.Value ?? string.Empty,
		BaseType = element.Attribute("BaseType")?.Value,
		IsAbstract = GetBoolAttribute(element, "Abstract"),
		IsOpenType = GetBoolAttribute(element, "OpenType"),
		HasStream = GetBoolAttribute(element, "HasStream")
	};

	private static void ParseEntityTypeKey(XElement element, ODataEntityType entityType)
	{
		var keyElement = element.Elements().FirstOrDefault(e => e.Name.LocalName == "Key");
		if (keyElement is null)
		{
			return;
		}

		foreach (var propertyRefElement in keyElement.Elements().Where(e => e.Name.LocalName == "PropertyRef"))
		{
			var keyName = propertyRefElement.Attribute("Name")?.Value;
			if (!string.IsNullOrEmpty(keyName))
			{
				entityType.Key.Add(keyName);
			}
		}
	}

	private static void ParseProperties(XElement element, List<ODataProperty> properties)
	{
		foreach (var propertyElement in element.Elements().Where(e => e.Name.LocalName == "Property"))
		{
			properties.Add(ParseProperty(propertyElement));
		}
	}

	private static void ParseNavigationProperties(XElement element, List<ODataNavigationProperty> navProperties)
	{
		foreach (var navPropertyElement in element.Elements().Where(e => e.Name.LocalName == "NavigationProperty"))
		{
			navProperties.Add(ParseNavigationProperty(navPropertyElement));
		}
	}
}
