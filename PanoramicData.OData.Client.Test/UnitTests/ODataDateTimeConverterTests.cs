using PanoramicData.OData.Client.Converters;
using System.Text.Json;

namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Unit tests for the OData date and nullable date JSON converters.
/// </summary>
public class ODataDateTimeConverterTests
{
	private const string Expected = "\"2024-01-15T10:30:00Z\"";

	private readonly JsonSerializerOptions _options = Options(new ODataDateTimeConverter());
	private readonly JsonSerializerOptions _nullableOptions = Options(new ODataNullableDateTimeConverter());

	/// <summary>
	/// Valid inputs read to the expected UTC time, for both converters.
	/// </summary>
	[Theory]
	[InlineData("\"2024-01-15T10:30:00Z\"", 10)]
	[InlineData("\"2024-01-15T10:30:00+02:00\"", 8)]
	public void Read_ValidDate_ReturnsUtcDateTime(string json, int expectedHour)
	{
		var result = JsonSerializer.Deserialize<DateTime>(json, _options);
		var nullable = JsonSerializer.Deserialize<DateTime?>(json, _nullableOptions);

		result.Should().Be(new DateTime(2024, 1, 15, expectedHour, 30, 0, DateTimeKind.Utc));
		nullable.Should().Be(result);
	}

	/// <summary>
	/// A date-only string reads as midnight UTC.
	/// </summary>
	[Fact]
	public void Read_SimpleDateFormat_ParsesCorrectly()
	{
		var result = JsonSerializer.Deserialize<DateTime>("\"2024-01-15\"", _options);

		result.Should().Be(new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc));
		result.Kind.Should().Be(DateTimeKind.Utc);
	}

	/// <summary>
	/// Empty, invalid and null inputs read as default, or null for the nullable converter.
	/// </summary>
	[Theory]
	[InlineData("\"\"")]
	[InlineData("\"not-a-date\"")]
	public void Read_EmptyOrInvalid_ReturnsDefaultOrNull(string json)
	{
		JsonSerializer.Deserialize<DateTime>(json, _options).Should().Be(default);
		JsonSerializer.Deserialize<DateTime?>(json, _nullableOptions).Should().BeNull();
	}

	/// <summary>
	/// A JSON null reads as null for the nullable converter.
	/// </summary>
	[Fact]
	public void NullableRead_Null_ReturnsNull()
		=> JsonSerializer.Deserialize<DateTime?>("null", _nullableOptions).Should().BeNull();

	/// <summary>
	/// UTC and unspecified kinds write with a Z suffix, for both converters.
	/// </summary>
	[Theory]
	[InlineData(DateTimeKind.Utc)]
	[InlineData(DateTimeKind.Unspecified)]
	public void Write_UtcOrUnspecified_WritesWithZSuffix(DateTimeKind kind)
	{
		var dateTime = new DateTime(2024, 1, 15, 10, 30, 0, kind);

		JsonSerializer.Serialize(dateTime, _options).Should().Be(Expected);
		JsonSerializer.Serialize<DateTime?>(dateTime, _nullableOptions).Should().Be(Expected);
	}

	/// <summary>
	/// A local time is converted to UTC and written with a Z suffix, for both converters.
	/// </summary>
	[Fact]
	public void Write_LocalDateTime_ConvertsToUtc()
	{
		var dateTime = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Local);

		JsonSerializer.Serialize(dateTime, _options).Should().Contain("Z");
		JsonSerializer.Serialize<DateTime?>(dateTime, _nullableOptions).Should().Contain("Z");
	}

	/// <summary>
	/// A null value writes JSON null for the nullable converter.
	/// </summary>
	[Fact]
	public void NullableWrite_Null_WritesNull()
		=> JsonSerializer.Serialize<DateTime?>(null, _nullableOptions).Should().Be("null");

	private static JsonSerializerOptions Options(System.Text.Json.Serialization.JsonConverter converter)
	{
		var options = new JsonSerializerOptions();
		options.Converters.Add(converter);
		return options;
	}
}
