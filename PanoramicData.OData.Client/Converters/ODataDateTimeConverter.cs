namespace PanoramicData.OData.Client.Converters;

/// <summary>
/// Shared parsing and formatting for the OData date converters.
/// </summary>
internal static class ODataDateTimeText
{
	private const string Format = "yyyy-MM-ddTHH:mm:ssZ";

	// Handles a 'Z' suffix, offsets, and date-only strings (treated as UTC).
	internal static DateTime? ParseUtc(string? text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return null;
		}

		if (DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dto))
		{
			return dto.UtcDateTime;
		}

		return DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dt)
			? DateTime.SpecifyKind(dt, DateTimeKind.Utc)
			: null;
	}

	// DateTime.MinValue is often used as a "not set" value, so Unspecified is treated as UTC.
	internal static string ToODataString(DateTime value)
	{
		var utcValue = value.Kind == DateTimeKind.Unspecified
			? DateTime.SpecifyKind(value, DateTimeKind.Utc)
			: value.ToUniversalTime();

		return utcValue.ToString(Format, CultureInfo.InvariantCulture);
	}
}

/// <summary>
/// Custom JSON converter for DateTime that formats dates as ISO 8601 with UTC 'Z' suffix
/// as required by OData (Edm.DateTimeOffset).
/// </summary>
public class ODataDateTimeConverter : JsonConverter<DateTime>
{
	/// <inheritdoc />
	public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		=> ODataDateTimeText.ParseUtc(reader.GetString()) ?? default;

	/// <inheritdoc />
	public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
		=> writer.WriteStringValue(ODataDateTimeText.ToODataString(value));
}

/// <summary>
/// Custom JSON converter for nullable DateTime that formats dates as ISO 8601 with UTC 'Z' suffix
/// as required by OData (Edm.DateTimeOffset).
/// </summary>
public class ODataNullableDateTimeConverter : JsonConverter<DateTime?>
{
	/// <inheritdoc />
	public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		=> reader.TokenType == JsonTokenType.Null ? null : ODataDateTimeText.ParseUtc(reader.GetString());

	/// <inheritdoc />
	public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
	{
		if (value.HasValue)
		{
			writer.WriteStringValue(ODataDateTimeText.ToODataString(value.Value));
		}
		else
		{
			writer.WriteNullValue();
		}
	}
}
