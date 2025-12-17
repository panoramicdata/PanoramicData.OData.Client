using PanoramicData.OData.Client.Exceptions;

namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Unit tests for OData exception types.
/// </summary>
public class ODataExceptionTests : TestBase
{
	#region ODataClientException Tests

	/// <summary>
	/// Tests ODataClientException constructor with message only.
	/// </summary>
	[Fact]
	public void ODataClientException_MessageOnly_SetsMessage()
	{
		// Act
		var ex = new ODataClientException("Test error");

		// Assert
		ex.Message.Should().Be("Test error");
		ex.StatusCode.Should().BeNull();
		ex.ResponseBody.Should().BeNull();
		ex.RequestUrl.Should().BeNull();
	}

	/// <summary>
	/// Tests ODataClientException constructor with inner exception.
	/// </summary>
	[Fact]
	public void ODataClientException_WithInnerException_SetsInnerException()
	{
		// Arrange
		var inner = new InvalidOperationException("Inner error");

		// Act
		var ex = new ODataClientException("Outer error", inner);

		// Assert
		ex.Message.Should().Be("Outer error");
		ex.InnerException.Should().BeSameAs(inner);
	}

	/// <summary>
	/// Tests ODataClientException constructor with full details.
	/// </summary>
	[Fact]
	public void ODataClientException_WithFullDetails_SetsAllProperties()
	{
		// Act
		var ex = new ODataClientException("Error message", 500, "Response body", "https://test.com/entity");

		// Assert
		ex.Message.Should().Be("Error message");
		ex.StatusCode.Should().Be(500);
		ex.ResponseBody.Should().Be("Response body");
		ex.RequestUrl.Should().Be("https://test.com/entity");
	}

	/// <summary>
	/// Tests ODataClientException constructor with full details and inner exception.
	/// </summary>
	[Fact]
	public void ODataClientException_WithFullDetailsAndInner_SetsAllProperties()
	{
		// Arrange
		var inner = new FormatException("Inner");

		// Act
		var ex = new ODataClientException("Error", 400, "Body", "URL", inner);

		// Assert
		ex.Message.Should().Be("Error");
		ex.StatusCode.Should().Be(400);
		ex.ResponseBody.Should().Be("Body");
		ex.RequestUrl.Should().Be("URL");
		ex.InnerException.Should().BeSameAs(inner);
	}

	#endregion

	#region ODataNotFoundException Tests

	/// <summary>
	/// Tests ODataNotFoundException constructor with message only.
	/// </summary>
	[Fact]
	public void ODataNotFoundException_MessageOnly_SetsMessage()
	{
		// Act
		var ex = new ODataNotFoundException("Entity not found");

		// Assert
		ex.Message.Should().Be("Entity not found");
		ex.StatusCode.Should().BeNull();
	}

	/// <summary>
	/// Tests ODataNotFoundException constructor with request URL.
	/// </summary>
	[Fact]
	public void ODataNotFoundException_WithRequestUrl_SetsProperties()
	{
		// Act
		var ex = new ODataNotFoundException("Not found", "Products(999)");

		// Assert
		ex.Message.Should().Be("Not found");
		ex.StatusCode.Should().Be(404);
		ex.RequestUrl.Should().Be("Products(999)");
	}

	/// <summary>
	/// Tests ODataNotFoundException is ODataClientException.
	/// </summary>
	[Fact]
	public void ODataNotFoundException_IsODataClientException()
	{
		// Act
		var ex = new ODataNotFoundException("Not found");

		// Assert
		ex.Should().BeAssignableTo<ODataClientException>();
	}

	#endregion

	#region ODataUnauthorizedException Tests

	/// <summary>
	/// Tests ODataUnauthorizedException constructor with message only.
	/// </summary>
	[Fact]
	public void ODataUnauthorizedException_MessageOnly_SetsMessage()
	{
		// Act
		var ex = new ODataUnauthorizedException("Unauthorized");

		// Assert
		ex.Message.Should().Be("Unauthorized");
		ex.StatusCode.Should().BeNull();
	}

	/// <summary>
	/// Tests ODataUnauthorizedException constructor with full details.
	/// </summary>
	[Fact]
	public void ODataUnauthorizedException_WithDetails_SetsProperties()
	{
		// Act
		var ex = new ODataUnauthorizedException("Auth required", "Token expired", "Products");

		// Assert
		ex.Message.Should().Be("Auth required");
		ex.StatusCode.Should().Be(401);
		ex.ResponseBody.Should().Be("Token expired");
		ex.RequestUrl.Should().Be("Products");
	}

	/// <summary>
	/// Tests ODataUnauthorizedException is ODataClientException.
	/// </summary>
	[Fact]
	public void ODataUnauthorizedException_IsODataClientException()
	{
		// Act
		var ex = new ODataUnauthorizedException("Unauthorized");

		// Assert
		ex.Should().BeAssignableTo<ODataClientException>();
	}

	#endregion

	#region ODataForbiddenException Tests

	/// <summary>
	/// Tests ODataForbiddenException constructor with message only.
	/// </summary>
	[Fact]
	public void ODataForbiddenException_MessageOnly_SetsMessage()
	{
		// Act
		var ex = new ODataForbiddenException("Forbidden");

		// Assert
		ex.Message.Should().Be("Forbidden");
		ex.StatusCode.Should().BeNull();
	}

	/// <summary>
	/// Tests ODataForbiddenException constructor with full details.
	/// </summary>
	[Fact]
	public void ODataForbiddenException_WithDetails_SetsProperties()
	{
		// Act
		var ex = new ODataForbiddenException("Access denied", "Insufficient permissions", "Admin/Users");

		// Assert
		ex.Message.Should().Be("Access denied");
		ex.StatusCode.Should().Be(403);
		ex.ResponseBody.Should().Be("Insufficient permissions");
		ex.RequestUrl.Should().Be("Admin/Users");
	}

	/// <summary>
	/// Tests ODataForbiddenException is ODataClientException.
	/// </summary>
	[Fact]
	public void ODataForbiddenException_IsODataClientException()
	{
		// Act
		var ex = new ODataForbiddenException("Forbidden");

		// Assert
		ex.Should().BeAssignableTo<ODataClientException>();
	}

	#endregion

	#region ODataConcurrencyException Tests

	/// <summary>
	/// Tests ODataConcurrencyException constructor with basic info.
	/// </summary>
	[Fact]
	public void ODataConcurrencyException_BasicConstructor_SetsProperties()
	{
		// Act
		var ex = new ODataConcurrencyException("Concurrency conflict", "Products(1)");

		// Assert
		ex.Message.Should().Be("Concurrency conflict");
		ex.StatusCode.Should().Be(412);
		ex.RequestUrl.Should().Be("Products(1)");
		ex.RequestETag.Should().BeNull();
		ex.CurrentETag.Should().BeNull();
	}

	/// <summary>
	/// Tests ODataConcurrencyException constructor with ETag info.
	/// </summary>
	[Fact]
	public void ODataConcurrencyException_WithETags_SetsAllProperties()
	{
		// Act
		var ex = new ODataConcurrencyException(
			"Entity modified",
			"Products(1)",
			"\"old-etag\"",
			"\"new-etag\"",
			"Error body");

		// Assert
		ex.Message.Should().Be("Entity modified");
		ex.StatusCode.Should().Be(412);
		ex.RequestUrl.Should().Be("Products(1)");
		ex.RequestETag.Should().Be("\"old-etag\"");
		ex.CurrentETag.Should().Be("\"new-etag\"");
		ex.ResponseBody.Should().Be("Error body");
	}

	/// <summary>
	/// Tests ODataConcurrencyException is ODataClientException.
	/// </summary>
	[Fact]
	public void ODataConcurrencyException_IsODataClientException()
	{
		// Act
		var ex = new ODataConcurrencyException("Conflict", "url");

		// Assert
		ex.Should().BeAssignableTo<ODataClientException>();
	}

	/// <summary>
	/// Tests ODataConcurrencyException with null ETags.
	/// </summary>
	[Fact]
	public void ODataConcurrencyException_NullETags_Allowed()
	{
		// Act
		var ex = new ODataConcurrencyException("Conflict", "url", null, null, null);

		// Assert
		ex.RequestETag.Should().BeNull();
		ex.CurrentETag.Should().BeNull();
		ex.ResponseBody.Should().BeNull();
	}

	#endregion

	#region Exception Hierarchy Tests

	/// <summary>
	/// Tests all OData exceptions derive from Exception.
	/// </summary>
	[Fact]
	public void AllODataExceptions_DeriveFromException()
	{
		// Assert
		typeof(ODataClientException).Should().BeDerivedFrom<Exception>();
		typeof(ODataNotFoundException).Should().BeDerivedFrom<Exception>();
		typeof(ODataUnauthorizedException).Should().BeDerivedFrom<Exception>();
		typeof(ODataForbiddenException).Should().BeDerivedFrom<Exception>();
		typeof(ODataConcurrencyException).Should().BeDerivedFrom<Exception>();
	}

	/// <summary>
	/// Tests all specialized exceptions derive from ODataClientException.
	/// </summary>
	[Fact]
	public void SpecializedExceptions_DeriveFromODataClientException()
	{
		// Assert
		typeof(ODataNotFoundException).Should().BeDerivedFrom<ODataClientException>();
		typeof(ODataUnauthorizedException).Should().BeDerivedFrom<ODataClientException>();
		typeof(ODataForbiddenException).Should().BeDerivedFrom<ODataClientException>();
		typeof(ODataConcurrencyException).Should().BeDerivedFrom<ODataClientException>();
	}

	#endregion
}
