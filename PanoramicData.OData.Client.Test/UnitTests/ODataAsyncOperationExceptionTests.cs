using PanoramicData.OData.Client.Exceptions;

namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Tests for ODataAsyncOperationException.
/// </summary>
public class ODataAsyncOperationExceptionTests
{
	/// <summary>
	/// Tests that the constructor sets all properties correctly.
	/// </summary>
	[Fact]
	public void Constructor_SetsAllProperties()
	{
		// Arrange
		var message = "The async operation failed";
		var monitorUrl = "https://api.example.com/odata/$async/123";
		var errorDetails = "Entity not found";

		// Act
		var exception = new ODataAsyncOperationException(message, monitorUrl, errorDetails);

		// Assert
		exception.Message.Should().Be(message);
		exception.MonitorUrl.Should().Be(monitorUrl);
		exception.ErrorDetails.Should().Be(errorDetails);
	}

	/// <summary>
	/// Tests that the constructor handles null error details.
	/// </summary>
	[Fact]
	public void Constructor_WithNullErrorDetails_SetsNullErrorDetails()
	{
		// Arrange
		var message = "The async operation failed";
		var monitorUrl = "https://api.example.com/odata/$async/123";

		// Act
		var exception = new ODataAsyncOperationException(message, monitorUrl, null);

		// Assert
		exception.Message.Should().Be(message);
		exception.MonitorUrl.Should().Be(monitorUrl);
		exception.ErrorDetails.Should().BeNull();
	}

	/// <summary>
	/// Tests that the exception derives from Exception.
	/// </summary>
	[Fact]
	public void IsException_CanBeCaught()
	{
		// Arrange & Act
		var exception = new ODataAsyncOperationException("Failed", "http://test", "details");

		// Assert
		exception.Should().BeAssignableTo<Exception>();
	}
}
