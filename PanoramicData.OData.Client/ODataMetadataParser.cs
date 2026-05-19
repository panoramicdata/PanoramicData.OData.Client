using System.Xml.Linq;

namespace PanoramicData.OData.Client;

/// <summary>
/// Parses OData CSDL (Common Schema Definition Language) metadata.
/// </summary>
internal static partial class ODataMetadataParser
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

		var schemaElements = doc.Descendants(EdmNs + "Schema").ToList();
		if (schemaElements.Count == 0)
		{
			schemaElements = doc.Descendants().Where(e => e.Name.LocalName == "Schema").ToList();
		}

		if (schemaElements.Count == 0)
		{
			return metadata;
		}

		// Use the namespace from the first schema
		metadata.Namespace = schemaElements[0].Attribute("Namespace")?.Value ?? string.Empty;

		// Parse all schemas - some services (e.g. Northwind) split types and the entity container across multiple schemas
		foreach (var schemaElement in schemaElements)
		{
			ParseSchemaTypes(schemaElement, metadata);
			ParseEntityContainer(schemaElement, metadata);
		}

		return metadata;
	}

	private static void ParseSchemaTypes(XElement schemaElement, ODataMetadata metadata)
	{
		foreach (var entityTypeElement in schemaElement.Elements().Where(e => e.Name.LocalName == "EntityType"))
		{
			metadata.EntityTypes.Add(ParseEntityType(entityTypeElement));
		}

		foreach (var complexTypeElement in schemaElement.Elements().Where(e => e.Name.LocalName == "ComplexType"))
		{
			metadata.ComplexTypes.Add(ParseComplexType(complexTypeElement));
		}

		foreach (var enumTypeElement in schemaElement.Elements().Where(e => e.Name.LocalName == "EnumType"))
		{
			metadata.EnumTypes.Add(ParseEnumType(enumTypeElement));
		}
	}

	private static void ParseEntityContainer(XElement schemaElement, ODataMetadata metadata)
	{
		var containerElement = schemaElement.Elements()
			.FirstOrDefault(e => e.Name.LocalName == "EntityContainer");

		if (containerElement is null)
		{
			return;
		}

		ParseEntitySets(containerElement, metadata);
		ParseSingletons(containerElement, metadata);
		ParseFunctionImports(containerElement, metadata);
		ParseActionImports(containerElement, metadata);
	}

	private static void ParseEntitySets(XElement containerElement, ODataMetadata metadata)
	{
		foreach (var entitySetElement in containerElement.Elements().Where(e => e.Name.LocalName == "EntitySet"))
		{
			metadata.EntitySets.Add(ParseEntitySet(entitySetElement));
		}
	}

	private static void ParseSingletons(XElement containerElement, ODataMetadata metadata)
	{
		foreach (var singletonElement in containerElement.Elements().Where(e => e.Name.LocalName == "Singleton"))
		{
			metadata.Singletons.Add(ParseSingleton(singletonElement));
		}
	}

	private static void ParseFunctionImports(XElement containerElement, ODataMetadata metadata)
	{
		foreach (var functionImportElement in containerElement.Elements().Where(e => e.Name.LocalName == "FunctionImport"))
		{
			metadata.FunctionImports.Add(ParseFunctionImport(functionImportElement));
		}
	}

	private static void ParseActionImports(XElement containerElement, ODataMetadata metadata)
	{
		foreach (var actionImportElement in containerElement.Elements().Where(e => e.Name.LocalName == "ActionImport"))
		{
			metadata.ActionImports.Add(ParseActionImport(actionImportElement));
		}
	}
}
