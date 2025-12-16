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
