using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PanoramicData.OData.Client.Converters;

/// <summary>
/// Custom JSON converter for DateTime that formats dates as ISO 8601 with UTC 'Z' suffix
/// as required by OData (Edm.DateTimeOffset).
/// </summary>
public class ODataDateTimeConverter : JsonConverter<DateTime>
{
	private const string ODataDateTimeFormat = "yyyy-MM-ddTHH:mm:ssZ";

	/// <inheritdoc />
	public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var dateString = reader.GetString();
		if (string.IsNullOrEmpty(dateString))
		{
			return default;
		}

		// Try parsing as DateTimeOffset first (handles 'Z' suffix and timezone offsets)
		if (DateTimeOffset.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dto))
		{
			return dto.UtcDateTime;
		}

		// Fall back to DateTime parsing
		if (DateTime.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
		{
			return dt;
		}

		return default;
	}

	/// <inheritdoc />
	public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
	{
		// OData requires dates in ISO 8601 format with 'Z' suffix for UTC
		// DateTime.MinValue is often used as a "not set" value, so we treat it as UTC
		var utcValue = value.Kind == DateTimeKind.Unspecified
			? DateTime.SpecifyKind(value, DateTimeKind.Utc)
			: value.ToUniversalTime();

		writer.WriteStringValue(utcValue.ToString(ODataDateTimeFormat, CultureInfo.InvariantCulture));
	}
}

/// <summary>
/// Custom JSON converter for nullable DateTime that formats dates as ISO 8601 with UTC 'Z' suffix
/// as required by OData (Edm.DateTimeOffset).
/// </summary>
public class ODataNullableDateTimeConverter : JsonConverter<DateTime?>
{
	private const string ODataDateTimeFormat = "yyyy-MM-ddTHH:mm:ssZ";

	/// <inheritdoc />
	public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.Null)
		{
			return null;
		}

		var dateString = reader.GetString();
		if (string.IsNullOrEmpty(dateString))
		{
			return null;
		}

		// Try parsing as DateTimeOffset first (handles 'Z' suffix and timezone offsets)
		if (DateTimeOffset.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dto))
		{
			return dto.UtcDateTime;
		}

		// Fall back to DateTime parsing
		if (DateTime.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
		{
			return dt;
		}

		return null;
	}

	/// <inheritdoc />
	public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
	{
		if (!value.HasValue)
		{
			writer.WriteNullValue();
			return;
		}

		// OData requires dates in ISO 8601 format with 'Z' suffix for UTC
		var utcValue = value.Value.Kind == DateTimeKind.Unspecified
			? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
			: value.Value.ToUniversalTime();

		writer.WriteStringValue(utcValue.ToString(ODataDateTimeFormat, CultureInfo.InvariantCulture));
	}
}
