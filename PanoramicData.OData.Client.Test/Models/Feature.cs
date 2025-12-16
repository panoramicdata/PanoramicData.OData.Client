using System.Text.Json.Serialization;

namespace PanoramicData.OData.Client.Test.Models;

/// <summary>
/// Feature enumeration for Person entity.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Feature
{
	/// <summary>Feature 1.</summary>
	Feature1,
	/// <summary>Feature 2.</summary>
	Feature2,
	/// <summary>Feature 3.</summary>
	Feature3,
	/// <summary>Feature 4.</summary>
	Feature4
}
