namespace PanoramicData.OData.Client;

/// <summary>
/// Date, time and GUID accessors for dynamic properties.
/// </summary>
public abstract partial class ODataOpenType
{
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
}
