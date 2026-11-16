using System.Xml.Linq;

namespace PanoramicData.OData.Client;

/// <summary>
/// ODataMetadataParser - enum parsing.
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
}
