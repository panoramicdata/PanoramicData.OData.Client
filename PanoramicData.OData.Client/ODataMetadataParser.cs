using System.Xml.Linq;

namespace PanoramicData.OData.Client;

/// <summary>
/// Parses OData CSDL (Common Schema Definition Language) metadata.
/// </summary>
internal static class ODataMetadataParser
{
	// OData CSDL namespace
	private static readonly XNamespace EdmxNs = "http://docs.oasis-open.org/odata/ns/edmx";
	private static readonly XNamespace EdmNs = "http://docs.oasis-open.org/odata/ns/edm";

	/// <summary>
	/// Parses CSDL XML content into ODataMetadata.
	/// </summary>
	/// <param name="xml">The CSDL XML content.</param>
	/// <returns>The parsed metadata.</returns>
	public static ODataMetadata Parse(string xml)
	{
		var doc = XDocument.Parse(xml);
		var metadata = new ODataMetadata();

		// Find the Schema element
		var schemaElement = doc.Descendants(EdmNs + "Schema").FirstOrDefault()
			?? doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "Schema");

		if (schemaElement is null)
		{
			return metadata;
		}

		// Get namespace from Schema
		metadata.Namespace = schemaElement.Attribute("Namespace")?.Value ?? string.Empty;

		// Parse entity types
		foreach (var entityTypeElement in schemaElement.Elements().Where(e => e.Name.LocalName == "EntityType"))
		{
			var entityType = ParseEntityType(entityTypeElement);
			metadata.EntityTypes.Add(entityType);
		}

		// Parse complex types
		foreach (var complexTypeElement in schemaElement.Elements().Where(e => e.Name.LocalName == "ComplexType"))
		{
			var complexType = ParseComplexType(complexTypeElement);
			metadata.ComplexTypes.Add(complexType);
		}

		// Parse enum types
		foreach (var enumTypeElement in schemaElement.Elements().Where(e => e.Name.LocalName == "EnumType"))
		{
			var enumType = ParseEnumType(enumTypeElement);
			metadata.EnumTypes.Add(enumType);
		}

		// Find the EntityContainer
		var containerElement = schemaElement.Elements()
			.FirstOrDefault(e => e.Name.LocalName == "EntityContainer");

		if (containerElement is not null)
		{
			// Parse entity sets
			foreach (var entitySetElement in containerElement.Elements().Where(e => e.Name.LocalName == "EntitySet"))
			{
				var entitySet = ParseEntitySet(entitySetElement);
				metadata.EntitySets.Add(entitySet);
			}

			// Parse singletons
			foreach (var singletonElement in containerElement.Elements().Where(e => e.Name.LocalName == "Singleton"))
			{
				var singleton = ParseSingleton(singletonElement);
				metadata.Singletons.Add(singleton);
			}

			// Parse function imports
			foreach (var functionImportElement in containerElement.Elements().Where(e => e.Name.LocalName == "FunctionImport"))
			{
				var functionImport = ParseFunctionImport(functionImportElement);
				metadata.FunctionImports.Add(functionImport);
			}

			// Parse action imports
			foreach (var actionImportElement in containerElement.Elements().Where(e => e.Name.LocalName == "ActionImport"))
			{
				var actionImport = ParseActionImport(actionImportElement);
				metadata.ActionImports.Add(actionImport);
			}
		}

		return metadata;
	}

	private static ODataEntityType ParseEntityType(XElement element)
	{
		var entityType = new ODataEntityType
		{
			Name = element.Attribute("Name")?.Value ?? string.Empty,
			BaseType = element.Attribute("BaseType")?.Value,
			IsAbstract = element.Attribute("Abstract")?.Value?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false,
			IsOpenType = element.Attribute("OpenType")?.Value?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false,
			HasStream = element.Attribute("HasStream")?.Value?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false
		};

		// Parse Key
		var keyElement = element.Elements().FirstOrDefault(e => e.Name.LocalName == "Key");
		if (keyElement is not null)
		{
			foreach (var propertyRefElement in keyElement.Elements().Where(e => e.Name.LocalName == "PropertyRef"))
			{
				var keyName = propertyRefElement.Attribute("Name")?.Value;
				if (!string.IsNullOrEmpty(keyName))
				{
					entityType.Key.Add(keyName);
				}
			}
		}

		// Parse Properties
		foreach (var propertyElement in element.Elements().Where(e => e.Name.LocalName == "Property"))
		{
			var property = ParseProperty(propertyElement);
			entityType.Properties.Add(property);
		}

		// Parse Navigation Properties
		foreach (var navPropertyElement in element.Elements().Where(e => e.Name.LocalName == "NavigationProperty"))
		{
			var navProperty = ParseNavigationProperty(navPropertyElement);
			entityType.NavigationProperties.Add(navProperty);
		}

		return entityType;
	}

	private static ODataComplexType ParseComplexType(XElement element)
	{
		var complexType = new ODataComplexType
		{
			Name = element.Attribute("Name")?.Value ?? string.Empty,
			BaseType = element.Attribute("BaseType")?.Value,
			IsAbstract = element.Attribute("Abstract")?.Value?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false,
			IsOpenType = element.Attribute("OpenType")?.Value?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false
		};

		// Parse Properties
		foreach (var propertyElement in element.Elements().Where(e => e.Name.LocalName == "Property"))
		{
			var property = ParseProperty(propertyElement);
			complexType.Properties.Add(property);
		}

		return complexType;
	}

	private static ODataProperty ParseProperty(XElement element)
	{
		var property = new ODataProperty
		{
			Name = element.Attribute("Name")?.Value ?? string.Empty,
			Type = element.Attribute("Type")?.Value ?? "Edm.String",
			IsNullable = !element.Attribute("Nullable")?.Value?.Equals("false", StringComparison.OrdinalIgnoreCase) ?? true,
			DefaultValue = element.Attribute("DefaultValue")?.Value
		};

		// Parse optional attributes
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

		return property;
	}

	private static ODataNavigationProperty ParseNavigationProperty(XElement element)
	{
		var navProperty = new ODataNavigationProperty
		{
			Name = element.Attribute("Name")?.Value ?? string.Empty,
			Type = element.Attribute("Type")?.Value ?? string.Empty,
			IsNullable = !element.Attribute("Nullable")?.Value?.Equals("false", StringComparison.OrdinalIgnoreCase) ?? true,
			Partner = element.Attribute("Partner")?.Value,
			ContainsTarget = element.Attribute("ContainsTarget")?.Value?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false
		};

		return navProperty;
	}

	private static ODataEnumType ParseEnumType(XElement element)
	{
		var enumType = new ODataEnumType
		{
			Name = element.Attribute("Name")?.Value ?? string.Empty,
			UnderlyingType = element.Attribute("UnderlyingType")?.Value ?? "Edm.Int32",
			IsFlags = element.Attribute("IsFlags")?.Value?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false
		};

		// Parse Members
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

		return enumType;
	}

	private static ODataEntitySet ParseEntitySet(XElement element)
	{
		var entitySet = new ODataEntitySet
		{
			Name = element.Attribute("Name")?.Value ?? string.Empty,
			EntityType = element.Attribute("EntityType")?.Value ?? string.Empty
		};

		// Parse Navigation Property Bindings
		foreach (var bindingElement in element.Elements().Where(e => e.Name.LocalName == "NavigationPropertyBinding"))
		{
			var binding = new ODataNavigationPropertyBinding
			{
				Path = bindingElement.Attribute("Path")?.Value ?? string.Empty,
				Target = bindingElement.Attribute("Target")?.Value ?? string.Empty
			};
			entitySet.NavigationPropertyBindings.Add(binding);
		}

		return entitySet;
	}

	private static ODataSingleton ParseSingleton(XElement element)
	{
		var singleton = new ODataSingleton
		{
			Name = element.Attribute("Name")?.Value ?? string.Empty,
			Type = element.Attribute("Type")?.Value ?? string.Empty
		};

		// Parse Navigation Property Bindings
		foreach (var bindingElement in element.Elements().Where(e => e.Name.LocalName == "NavigationPropertyBinding"))
		{
			var binding = new ODataNavigationPropertyBinding
			{
				Path = bindingElement.Attribute("Path")?.Value ?? string.Empty,
				Target = bindingElement.Attribute("Target")?.Value ?? string.Empty
			};
			singleton.NavigationPropertyBindings.Add(binding);
		}

		return singleton;
	}

	private static ODataFunctionImport ParseFunctionImport(XElement element)
	{
		var functionImport = new ODataFunctionImport
		{
			Name = element.Attribute("Name")?.Value ?? string.Empty,
			Function = element.Attribute("Function")?.Value ?? string.Empty,
			EntitySet = element.Attribute("EntitySet")?.Value,
			IncludeInServiceDocument = element.Attribute("IncludeInServiceDocument")?.Value?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false
		};

		return functionImport;
	}

	private static ODataActionImport ParseActionImport(XElement element)
	{
		var actionImport = new ODataActionImport
		{
			Name = element.Attribute("Name")?.Value ?? string.Empty,
			Action = element.Attribute("Action")?.Value ?? string.Empty,
			EntitySet = element.Attribute("EntitySet")?.Value
		};

		return actionImport;
	}
}
