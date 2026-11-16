using System.Xml.Linq;

namespace PanoramicData.OData.Client;

/// <summary>
/// ODataMetadataParser - complex type and property parsing.
/// </summary>
internal static partial class ODataMetadataParser
{
	private static ODataComplexType ParseComplexType(XElement element)
	{
		var complexType = new ODataComplexType
		{
			Name = element.Attribute("Name")?.Value ?? string.Empty,
			BaseType = element.Attribute("BaseType")?.Value,
			IsAbstract = GetBoolAttribute(element, "Abstract"),
			IsOpenType = GetBoolAttribute(element, "OpenType")
		};

		ParseProperties(element, complexType.Properties);
		return complexType;
	}

	private static ODataProperty ParseProperty(XElement element)
	{
		var property = new ODataProperty
		{
			Name = element.Attribute("Name")?.Value ?? string.Empty,
			Type = element.Attribute("Type")?.Value ?? "Edm.String",
			IsNullable = IsNullableAttribute(element),
			DefaultValue = element.Attribute("DefaultValue")?.Value
		};

		ParsePropertyConstraints(element, property);
		return property;
	}

	private static void ParsePropertyConstraints(XElement element, ODataProperty property)
	{
		if (int.TryParse(element.Attribute("MaxLength")?.Value, out var maxLength))
		{
			property.MaxLength = maxLength;
		}

		if (int.TryParse(element.Attribute("Precision")?.Value, out var precision))
		{
			property.Precision = precision;
		}

		if (int.TryParse(element.Attribute("Scale")?.Value, out var scale))
		{
			property.Scale = scale;
		}
	}

	private static ODataNavigationProperty ParseNavigationProperty(XElement element) => new()
	{
		Name = element.Attribute("Name")?.Value ?? string.Empty,
		Type = element.Attribute("Type")?.Value ?? string.Empty,
		IsNullable = IsNullableAttribute(element),
		Partner = element.Attribute("Partner")?.Value,
		ContainsTarget = GetBoolAttribute(element, "ContainsTarget")
	};
}
