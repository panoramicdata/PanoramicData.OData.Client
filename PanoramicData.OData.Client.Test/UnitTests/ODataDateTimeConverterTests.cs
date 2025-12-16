using AwesomeAssertions;
using PanoramicData.OData.Client.Converters;
using System.Text.Json;

namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Tests for ODataDateTimeConverter and ODataNullableDateTimeConverter.
/// </summary>
public class ODataDateTimeConverterTests
{
	private readonly JsonSerializerOptions _options;
	private readonly JsonSerializerOptions _nullableOptions;

	/// <summary>
	/// Initializes a new instance of the test class.
	/// </summary>
	public ODataDateTimeConverterTests()
	{
		_options = new JsonSerializerOptions();
		_options.Converters.Add(new ODataDateTimeConverter());

		_nullableOptions = new JsonSerializerOptions();
		_nullableOptions.Converters.Add(new ODataNullableDateTimeConverter());
	}

	#region ODataDateTimeConverter Tests

	/// <summary>
	/// Tests reading a valid ISO 8601 date with Z suffix.
	/// </summary>
	[Fact]
	public void Read_ValidIso8601WithZ_ReturnsUtcDateTime()
	{
		// Arrange
		var json = "\"2024-01-15T10:30:00Z\"";

		// Act
		var result = JsonSerializer.Deserialize<DateTime>(json, _options);

		// Assert
		result.Year.Should().Be(2024);
		result.Month.Should().Be(1);
		result.Day.Should().Be(15);
		result.Hour.Should().Be(10);
		result.Minute.Should().Be(30);
		result.Second.Should().Be(0);
	}

	/// <summary>
	/// Tests reading a valid ISO 8601 date with timezone offset.
	/// </summary>
	[Fact]
	public void Read_ValidIso8601WithOffset_ReturnsUtcDateTime()
	{
		// Arrange - 10:30 with +02:00 offset = 08:30 UTC
		var json = "\"2024-01-15T10:30:00+02:00\"";

		// Act
		var result = JsonSerializer.Deserialize<DateTime>(json, _options);

		// Assert
		result.Hour.Should().Be(8); // Converted to UTC
	}

	/// <summary>
	/// Tests reading an empty string returns default DateTime.
	/// </summary>
	[Fact]
	public void Read_EmptyString_ReturnsDefault()
	{
		// Arrange
		var json = "\"\"";

		// Act
		var result = JsonSerializer.Deserialize<DateTime>(json, _options);

		// Assert
		result.Should().Be(default(DateTime));
	}

	/// <summary>
	/// Tests reading an invalid date string returns default DateTime.
	/// </summary>
	[Fact]
	public void Read_InvalidDate_ReturnsDefault()
	{
		// Arrange
		var json = "\"not-a-date\"";

		// Act
		var result = JsonSerializer.Deserialize<DateTime>(json, _options);

		// Assert
		result.Should().Be(default(DateTime));
	}

	/// <summary>
	/// Tests reading a simple date format.
	/// </summary>
	[Fact]
	public void Read_SimpleDateFormat_ParsesCorrectly()
	{
		// Arrange
		var json = "\"2024-01-15\"";

		// Act
		var result = JsonSerializer.Deserialize<DateTime>(json, _options);

		// Assert
		result.Year.Should().Be(2024);
		result.Month.Should().Be(1);
		result.Day.Should().Be(15);
	}

	/// <summary>
	/// Tests writing a UTC DateTime includes Z suffix.
	/// </summary>
	[Fact]
	public void Write_UtcDateTime_WritesWithZSuffix()
	{
		// Arrange
		var dateTime = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);

		// Act
		var json = JsonSerializer.Serialize(dateTime, _options);

		// Assert
		json.Should().Be("\"2024-01-15T10:30:00Z\"");
	}

	/// <summary>
	/// Tests writing a local DateTime converts to UTC.
	/// </summary>
	[Fact]
	public void Write_LocalDateTime_ConvertsToUtc()
	{
		// Arrange
		var dateTime = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Local);

		// Act
		var json = JsonSerializer.Serialize(dateTime, _options);

		// Assert
		json.Should().Contain("Z"); // Should end with Z indicating UTC
	}

	/// <summary>
	/// Tests writing an unspecified kind DateTime treats as UTC.
	/// </summary>
	[Fact]
	public void Write_UnspecifiedKind_TreatsAsUtc()
	{
		// Arrange
		var dateTime = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Unspecified);

		// Act
		var json = JsonSerializer.Serialize(dateTime, _options);

		// Assert
		json.Should().Be("\"2024-01-15T10:30:00Z\"");
	}

	#endregion

	#region ODataNullableDateTimeConverter Tests

	/// <summary>
	/// Tests reading null returns null.
	/// </summary>
	[Fact]
	public void NullableRead_Null_ReturnsNull()
	{
		// Arrange
		var json = "null";

		// Act
		var result = JsonSerializer.Deserialize<DateTime?>(json, _nullableOptions);

		// Assert
		result.Should().BeNull();
	}

	/// <summary>
	/// Tests reading empty string returns null.
	/// </summary>
	[Fact]
	public void NullableRead_EmptyString_ReturnsNull()
	{
		// Arrange
		var json = "\"\"";

		// Act
		var result = JsonSerializer.Deserialize<DateTime?>(json, _nullableOptions);

		// Assert
		result.Should().BeNull();
	}

	/// <summary>
	/// Tests reading a valid date returns DateTime.
	/// </summary>
	[Fact]
	public void NullableRead_ValidDate_ReturnsDateTime()
	{
		// Arrange
		var json = "\"2024-01-15T10:30:00Z\"";

		// Act
		var result = JsonSerializer.Deserialize<DateTime?>(json, _nullableOptions);

		// Assert
		result.Should().NotBeNull();
		result!.Value.Year.Should().Be(2024);
		result.Value.Month.Should().Be(1);
		result.Value.Day.Should().Be(15);
	}

	/// <summary>
	/// Tests reading a date with offset converts to UTC.
	/// </summary>
	[Fact]
	public void NullableRead_ValidDateWithOffset_ReturnsUtc()
	{
		// Arrange
		var json = "\"2024-01-15T10:30:00+02:00\"";

		// Act
		var result = JsonSerializer.Deserialize<DateTime?>(json, _nullableOptions);

		// Assert
		result.Should().NotBeNull();
		result!.Value.Hour.Should().Be(8); // Converted to UTC
	}

	/// <summary>
	/// Tests reading an invalid date returns null.
	/// </summary>
	[Fact]
	public void NullableRead_InvalidDate_ReturnsNull()
	{
		// Arrange
		var json = "\"not-a-date\"";

		// Act
		var result = JsonSerializer.Deserialize<DateTime?>(json, _nullableOptions);

		// Assert
		result.Should().BeNull();
	}

	/// <summary>
	/// Tests writing null writes null.
	/// </summary>
	[Fact]
	public void NullableWrite_Null_WritesNull()
	{
		// Arrange
		DateTime? dateTime = null;

		// Act
		var json = JsonSerializer.Serialize(dateTime, _nullableOptions);

		// Assert
		json.Should().Be("null");
	}

	/// <summary>
	/// Tests writing a valid date includes Z suffix.
	/// </summary>
	[Fact]
	public void NullableWrite_ValidDate_WritesWithZSuffix()
	{
		// Arrange
		DateTime? dateTime = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);

		// Act
		var json = JsonSerializer.Serialize(dateTime, _nullableOptions);

		// Assert
		json.Should().Be("\"2024-01-15T10:30:00Z\"");
	}

	/// <summary>
	/// Tests writing unspecified kind treats as UTC.
	/// </summary>
	[Fact]
	public void NullableWrite_UnspecifiedKind_TreatsAsUtc()
	{
		// Arrange
		DateTime? dateTime = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Unspecified);

		// Act
		var json = JsonSerializer.Serialize(dateTime, _nullableOptions);

		// Assert
		json.Should().Be("\"2024-01-15T10:30:00Z\"");
	}

	/// <summary>
	/// Tests writing local DateTime converts to UTC.
	/// </summary>
	[Fact]
	public void NullableWrite_LocalDateTime_ConvertsToUtc()
	{
		// Arrange
		DateTime? dateTime = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Local);

		// Act
		var json = JsonSerializer.Serialize(dateTime, _nullableOptions);

		// Assert
		json.Should().Contain("Z");
	}

	#endregion
}
