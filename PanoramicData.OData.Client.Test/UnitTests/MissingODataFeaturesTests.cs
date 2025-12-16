namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Tests for OData V4 features that are NOT YET IMPLEMENTED.
/// These tests document missing functionality and are expected to fail.
/// When implementing a feature, the corresponding test should be updated to pass.
/// </summary>
public class MissingODataFeaturesTests
{
	#region Derived Types

	/// <summary>
	/// Tests derived type querying - NOT IMPLEMENTED via expressions.
	/// OData V4 supports querying derived types.
	/// </summary>
	[Fact(Skip = "Derived type querying not yet implemented")]
	public async Task DerivedType_Filter_ShouldCastCorrectly()
	{
		// OData V4 derived types:
		// GET People/Microsoft.OData.SampleService.Models.TripPin.Employee
		// GET People?$filter=Microsoft.OData.SampleService.Models.TripPin.Employee/EmployeeId ne null

		// Expected API:
		// var employees = await client.For<Employee>("People").OfType<Employee>().GetAsync();

		await Task.CompletedTask;
		Assert.Fail("Derived type querying not yet implemented");
	}

	#endregion

	#region Open Types / Dynamic Properties

	/// <summary>
	/// Tests open type support - NOT IMPLEMENTED.
	/// OData V4 supports open types with dynamic properties.
	/// </summary>
	[Fact(Skip = "Open types not yet implemented")]
	public async Task OpenType_DynamicProperties_ShouldBeAccessible()
	{
		// OData V4 open types can have dynamic properties not in the schema
		// Expected API:
		// var entity = await client.GetByKeyAsync<OpenEntity>(1);
		// var dynamicValue = entity.DynamicProperties["CustomField"];

		await Task.CompletedTask;
		Assert.Fail("Open types not yet implemented");
	}

	#endregion

	#region Async Operations ($async)

	/// <summary>
	/// Tests async operation support - NOT IMPLEMENTED.
	/// OData V4 supports long-running async operations.
	/// </summary>
	[Fact(Skip = "Async operations not yet implemented")]
	public async Task AsyncOperation_LongRunning_ShouldReturnMonitorUrl()
	{
		// OData V4 async operations:
		// Client sends: Respond-Async: true
		// Server returns: 202 Accepted, Location: /monitor/123
		// Client polls monitor URL until complete

		// Expected API:
		// var operation = await client.StartAsyncOperationAsync(action);
		// await operation.WaitForCompletionAsync();
		// var result = operation.Result;

		await Task.CompletedTask;
		Assert.Fail("Async operations not yet implemented");
	}

	#endregion

	#region Cross-Join Queries

	/// <summary>
	/// Tests cross-join query support - NOT IMPLEMENTED.
	/// OData V4 supports cross-join queries.
	/// </summary>
	[Fact(Skip = "Cross-join queries not yet implemented")]
	public async Task CrossJoin_MultipleEntitySets_ShouldCombine()
	{
		// OData V4 cross-join:
		// GET /$crossjoin(Products,Categories)?$filter=Products/CategoryId eq Categories/Id

		// Expected API:
		// var result = await client.CrossJoin("Products", "Categories")
		//     .Filter("Products/CategoryId eq Categories/Id")
		//     .GetAsync();

		await Task.CompletedTask;
		Assert.Fail("Cross-join queries not yet implemented");
	}

	#endregion
}
