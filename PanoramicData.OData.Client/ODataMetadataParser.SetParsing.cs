using System.Xml.Linq;

namespace PanoramicData.OData.Client;

/// <summary>
/// ODataMetadataParser - entity set and singleton parsing.
/// </summary>
internal static partial class ODataMetadataParser
{
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
}
