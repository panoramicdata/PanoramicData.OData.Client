using System.Text.Json;
using System.Text.Json.Serialization;

namespace PanoramicData.OData.Client;

/// <summary>
/// Base class for OData open types that support dynamic properties.
/// Derive from this class when working with OData open types that can have
/// properties not defined in the schema.
/// </summary>
public abstract class ODataOpenType
{
	/// <summary>
	/// Gets the dictionary of dynamic properties that are not defined in the schema.
	/// </summary>
	[JsonExtensionData]
	public Dictionary<string, JsonElement> DynamicProperties { get; set; } = [];

	/// <summary>
	/// Gets a dynamic property value as the specified type.
	/// </summary>
	/// <typeparam name="TValue">The type to deserialize the value as.</typeparam>
	/// <param name="propertyName">The property name.</param>
	/// <param name="options">Optional JSON serializer options.</param>
	/// <returns>The property value, or default if not found.</returns>
	public TValue? GetDynamicProperty<TValue>(string propertyName, JsonSerializerOptions? options = null)
	{
		if (!DynamicProperties.TryGetValue(propertyName, out var element))
		{
			return default;
		}

		return element.Deserialize<TValue>(options);
	}

	/// <summary>
	/// Gets a dynamic property value as a string.
	/// </summary>
	/// <param name="propertyName">The property name.</param>
	/// <returns>The property value as a string, or null if not found.</returns>
	public string? GetDynamicString(string propertyName)
	{
		if (!DynamicProperties.TryGetValue(propertyName, out var element))
		{
			return null;
		}

		return element.ValueKind == JsonValueKind.String
			? element.GetString()
			: element.GetRawText();
	}

	/// <summary>
	/// Gets a dynamic property value as an integer.
	/// </summary>
	/// <param name="propertyName">The property name.</param>
	/// <returns>The property value as an integer, or null if not found or not a number.</returns>
	public int? GetDynamicInt(string propertyName)
	{
		if (!DynamicProperties.TryGetValue(propertyName, out var element))
		{
			return null;
		}

		if (element.ValueKind != JsonValueKind.Number)
		{
			return null;
		}

		if (element.TryGetInt32(out var value))
		{
			return value;
		}

		return null;
	}

	/// <summary>
	/// Gets a dynamic property value as a long.
	/// </summary>
	/// <param name="propertyName">The property name.</param>
	/// <returns>The property value as a long, or null if not found or not a number.</returns>
	public long? GetDynamicLong(string propertyName)
	{
		if (!DynamicProperties.TryGetValue(propertyName, out var element))
		{
			return null;
		}

		if (element.ValueKind != JsonValueKind.Number)
		{
			return null;
		}

		if (element.TryGetInt64(out var value))
		{
			return value;
		}

		return null;
	}

	/// <summary>
	/// Gets a dynamic property value as a double.
	/// </summary>
	/// <param name="propertyName">The property name.</param>
	/// <returns>The property value as a double, or null if not found or not a number.</returns>
	public double? GetDynamicDouble(string propertyName)
	{
		if (!DynamicProperties.TryGetValue(propertyName, out var element))
		{
			return null;
		}

		if (element.ValueKind != JsonValueKind.Number)
		{
			return null;
		}

		if (element.TryGetDouble(out var value))
		{
			return value;
		}

		return null;
	}

	/// <summary>
	/// Gets a dynamic property value as a boolean.
	/// </summary>
	/// <param name="propertyName">The property name.</param>
	/// <returns>The property value as a boolean, or null if not found or not a boolean.</returns>
	public bool? GetDynamicBool(string propertyName)
	{
		if (!DynamicProperties.TryGetValue(propertyName, out var element))
		{
			return null;
		}

		return element.ValueKind switch
		{
			JsonValueKind.True => true,
			JsonValueKind.False => false,
			_ => null
		};
	}

	/// <summary>
	/// Gets a dynamic property value as a DateTime.
	/// </summary>
	/// <param name="propertyName">The property name.</param>
	/// <returns>The property value as a DateTime, or null if not found or not a valid date.</returns>
	public DateTime? GetDynamicDateTime(string propertyName)
	{
		if (!DynamicProperties.TryGetValue(propertyName, out var element))
		{
			return null;
		}

		if (element.ValueKind != JsonValueKind.String)
		{
			return null;
		}

		if (element.TryGetDateTime(out var value))
		{
			return value;
		}

		return null;
	}

	/// <summary>
	/// Gets a dynamic property value as a DateTimeOffset.
	/// </summary>
	/// <param name="propertyName">The property name.</param>
	/// <returns>The property value as a DateTimeOffset, or null if not found or not a valid date.</returns>
	public DateTimeOffset? GetDynamicDateTimeOffset(string propertyName)
	{
		if (!DynamicProperties.TryGetValue(propertyName, out var element))
		{
			return null;
		}

		if (element.ValueKind != JsonValueKind.String)
		{
			return null;
		}

		if (element.TryGetDateTimeOffset(out var value))
		{
			return value;
		}

		return null;
	}

	/// <summary>
	/// Gets a dynamic property value as a Guid.
	/// </summary>
	/// <param name="propertyName">The property name.</param>
	/// <returns>The property value as a Guid, or null if not found or not a valid GUID.</returns>
	public Guid? GetDynamicGuid(string propertyName)
	{
		if (!DynamicProperties.TryGetValue(propertyName, out var element))
		{
			return null;
		}

		if (element.ValueKind != JsonValueKind.String)
		{
			return null;
		}

		if (element.TryGetGuid(out var value))
		{
			return value;
		}

		return null;
	}

	/// <summary>
	/// Checks if a dynamic property exists.
	/// </summary>
	/// <param name="propertyName">The property name.</param>
	/// <returns>True if the property exists, false otherwise.</returns>
	public bool HasDynamicProperty(string propertyName)
		=> DynamicProperties.ContainsKey(propertyName);

	/// <summary>
	/// Sets a dynamic property value.
	/// Note: This method serializes the value to JsonElement for storage.
	/// </summary>
	/// <typeparam name="TValue">The type of the value.</typeparam>
	/// <param name="propertyName">The property name.</param>
	/// <param name="value">The value to set.</param>
	/// <param name="options">Optional JSON serializer options.</param>
	public void SetDynamicProperty<TValue>(string propertyName, TValue value, JsonSerializerOptions? options = null)
	{
		var json = JsonSerializer.Serialize(value, options);
		DynamicProperties[propertyName] = JsonDocument.Parse(json).RootElement.Clone();
	}

	/// <summary>
	/// Removes a dynamic property.
	/// </summary>
	/// <param name="propertyName">The property name.</param>
	/// <returns>True if the property was removed, false if it didn't exist.</returns>
	public bool RemoveDynamicProperty(string propertyName)
		=> DynamicProperties.Remove(propertyName);

	/// <summary>
	/// Gets all dynamic property names.
	/// </summary>
	/// <returns>An enumerable of property names.</returns>
	public IEnumerable<string> GetDynamicPropertyNames()
		=> DynamicProperties.Keys;
}
