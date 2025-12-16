#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Tests for derived type querying (OfType, Cast).
/// </summary>
public class ODataDerivedTypeTests
{
	[Fact]
	public void OfType_ShouldAppendDerivedTypeToUrl()
	{
		// Arrange
		var logger = NullLogger.Instance;
		var builder = new ODataQueryBuilder<Person>("People", logger);

		// Act
		var derivedBuilder = builder.OfType<Employee>("Microsoft.OData.SampleService.Models.TripPin.Employee");
		var url = derivedBuilder.BuildUrl();

		// Assert
		url.Should().Be("People/Microsoft.OData.SampleService.Models.TripPin.Employee");
	}

	[Fact]
	public void OfType_WithoutNamespace_ShouldUseTypeName()
	{
		// Arrange
		var logger = NullLogger.Instance;
		var builder = new ODataQueryBuilder<Person>("People", logger);

		// Act
		var derivedBuilder = builder.OfType<Employee>();
		var url = derivedBuilder.BuildUrl();

		// Assert
		url.Should().Be("People/Employee");
	}

	[Fact]
	public void Cast_ShouldAppendDerivedTypeToUrl()
	{
		// Arrange
		var logger = NullLogger.Instance;
		var builder = new ODataQueryBuilder<Person>("People", logger);

		// Act
		builder.Cast("Microsoft.OData.SampleService.Models.TripPin.Employee");
		var url = builder.BuildUrl();

		// Assert
		url.Should().Be("People/Microsoft.OData.SampleService.Models.TripPin.Employee");
	}

	[Fact]
	public void OfType_ShouldPreserveQueryOptions()
	{
		// Arrange
		var logger = NullLogger.Instance;
		var builder = new ODataQueryBuilder<Person>("People", logger)
			.Filter("Age gt 21")
			.Top(10)
			.Select("FirstName,LastName");

		// Act
		var derivedBuilder = builder.OfType<Employee>("Namespace.Employee");
		var url = derivedBuilder.BuildUrl();

		// Assert
		url.Should().Contain("People/Namespace.Employee");
		url.Should().Contain("$filter=");
		url.Should().Contain("$top=10");
		url.Should().Contain("$select=");
	}

	[Fact]
	public void DerivedType_WithKey_ShouldPlaceKeyAfterType()
	{
		// Arrange
		var logger = NullLogger.Instance;
		var builder = new ODataQueryBuilder<Person>("People", logger);

		// Act
		var derivedBuilder = builder.OfType<Employee>("Namespace.Employee");
		derivedBuilder.Key("john");
		var url = derivedBuilder.BuildUrl();

		// Assert
		url.Should().Be("People/Namespace.Employee('john')");
	}

	// Test entity classes
	public class Person
	{
		public string UserName { get; set; } = string.Empty;
		public string FirstName { get; set; } = string.Empty;
		public string LastName { get; set; } = string.Empty;
		public int Age { get; set; }
	}

	public class Employee : Person
	{
		public string EmployeeId { get; set; } = string.Empty;
		public string Department { get; set; } = string.Empty;
	}
}
#pragma warning restore CS1591
