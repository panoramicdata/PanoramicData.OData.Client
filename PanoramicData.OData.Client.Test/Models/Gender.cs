using System.Text.Json.Serialization;

namespace PanoramicData.OData.Client.Test.Models;

/// <summary>
/// Gender enumeration for Person entity.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Gender
{
	/// <summary>Male gender.</summary>
	Male,
	/// <summary>Female gender.</summary>
	Female,
	/// <summary>Unknown gender.</summary>
	Unknown
}
