namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Unit tests for ODataBatchResponse and ODataBatchOperationResult.
/// </summary>
public class ODataBatchResponseTests : TestBase
{
	#region ODataBatchOperationResult Tests

	/// <summary>
	/// Tests IsSuccess returns true for 2xx status codes.
	/// </summary>
	[Theory]
	[InlineData(200)]
	[InlineData(201)]
	[InlineData(204)]
	[InlineData(299)]
	public void ODataBatchOperationResult_IsSuccess_TrueFor2xxStatusCodes(int statusCode)
	{
		// Arrange
		var result = new ODataBatchOperationResult { StatusCode = statusCode };

		// Assert
		result.IsSuccess.Should().BeTrue();
	}

	/// <summary>
	/// Tests IsSuccess returns false for non-2xx status codes.
	/// </summary>
	[Theory]
	[InlineData(400)]
	[InlineData(401)]
	[InlineData(404)]
	[InlineData(500)]
	[InlineData(199)]
	public void ODataBatchOperationResult_IsSuccess_FalseForNon2xxStatusCodes(int statusCode)
	{
		// Arrange
		var result = new ODataBatchOperationResult { StatusCode = statusCode };

		// Assert
		result.IsSuccess.Should().BeFalse();
	}

	/// <summary>
	/// Tests default values are set correctly.
	/// </summary>
	[Fact]
	public void ODataBatchOperationResult_DefaultValues_AreCorrect()
	{
		// Act
		var result = new ODataBatchOperationResult();

		// Assert
		result.OperationId.Should().BeEmpty();
		result.StatusCode.Should().Be(0);
		result.ResponseBody.Should().BeNull();
		result.Result.Should().BeNull();
		result.ErrorMessage.Should().BeNull();
		result.Headers.Should().BeEmpty();
	}

	#endregion

	#region AllSucceeded Tests

	/// <summary>
	/// Tests AllSucceeded returns true when all operations succeeded.
	/// </summary>
	[Fact]
	public void AllSucceeded_AllOperationsSuccessful_ReturnsTrue()
	{
		// Arrange
		var response = new ODataBatchResponse
		{
			Results =
			[
				new ODataBatchOperationResult { StatusCode = 200 },
				new ODataBatchOperationResult { StatusCode = 201 },
				new ODataBatchOperationResult { StatusCode = 204 }
			]
		};

		// Assert
		response.AllSucceeded.Should().BeTrue();
	}

	/// <summary>
	/// Tests AllSucceeded returns false when any operation failed.
	/// </summary>
	[Fact]
	public void AllSucceeded_SomeOperationsFailed_ReturnsFalse()
	{
		// Arrange
		var response = new ODataBatchResponse
		{
			Results =
			[
				new ODataBatchOperationResult { StatusCode = 200 },
				new ODataBatchOperationResult { StatusCode = 400 },
				new ODataBatchOperationResult { StatusCode = 201 }
			]
		};

		// Assert
		response.AllSucceeded.Should().BeFalse();
	}

	/// <summary>
	/// Tests AllSucceeded returns false for empty results.
	/// </summary>
	[Fact]
	public void AllSucceeded_EmptyResults_ReturnsFalse()
	{
		// Arrange
		var response = new ODataBatchResponse();

		// Assert
		response.AllSucceeded.Should().BeFalse();
	}

	#endregion

	#region HasErrors Tests

	/// <summary>
	/// Tests HasErrors returns true when any operation failed.
	/// </summary>
	[Fact]
	public void HasErrors_SomeOperationsFailed_ReturnsTrue()
	{
		// Arrange
		var response = new ODataBatchResponse
		{
			Results =
			[
				new ODataBatchOperationResult { StatusCode = 200 },
				new ODataBatchOperationResult { StatusCode = 500 }
			]
		};

		// Assert
		response.HasErrors.Should().BeTrue();
	}

	/// <summary>
	/// Tests HasErrors returns false when all operations succeeded.
	/// </summary>
	[Fact]
	public void HasErrors_AllOperationsSuccessful_ReturnsFalse()
	{
		// Arrange
		var response = new ODataBatchResponse
		{
			Results =
			[
				new ODataBatchOperationResult { StatusCode = 200 },
				new ODataBatchOperationResult { StatusCode = 201 }
			]
		};

		// Assert
		response.HasErrors.Should().BeFalse();
	}

	/// <summary>
	/// Tests HasErrors returns false for empty results.
	/// </summary>
	[Fact]
	public void HasErrors_EmptyResults_ReturnsFalse()
	{
		// Arrange
		var response = new ODataBatchResponse();

		// Assert
		response.HasErrors.Should().BeFalse();
	}

	#endregion

	#region FailedResults Tests

	/// <summary>
	/// Tests FailedResults returns only failed operations.
	/// </summary>
	[Fact]
	public void FailedResults_ReturnsOnlyFailedOperations()
	{
		// Arrange
		var response = new ODataBatchResponse
		{
			Results =
			[
				new ODataBatchOperationResult { OperationId = "1", StatusCode = 200 },
				new ODataBatchOperationResult { OperationId = "2", StatusCode = 400 },
				new ODataBatchOperationResult { OperationId = "3", StatusCode = 201 },
				new ODataBatchOperationResult { OperationId = "4", StatusCode = 500 }
			]
		};

		// Act
		var failed = response.FailedResults.ToList();

		// Assert
		failed.Should().HaveCount(2);
		failed.Select(f => f.OperationId).Should().BeEquivalentTo(["2", "4"]);
	}

	/// <summary>
	/// Tests FailedResults returns empty for all successful.
	/// </summary>
	[Fact]
	public void FailedResults_AllSuccessful_ReturnsEmpty()
	{
		// Arrange
		var response = new ODataBatchResponse
		{
			Results =
			[
				new ODataBatchOperationResult { StatusCode = 200 },
				new ODataBatchOperationResult { StatusCode = 201 }
			]
		};

		// Assert
		response.FailedResults.Should().BeEmpty();
	}

	#endregion

	#region Indexer Tests

	/// <summary>
	/// Tests indexer returns correct result.
	/// </summary>
	[Fact]
	public void Indexer_ValidIndex_ReturnsResult()
	{
		// Arrange
		var result = new ODataBatchOperationResult { OperationId = "test", StatusCode = 200 };
		var response = new ODataBatchResponse
		{
			Results = [result]
		};

		// Act
		var retrieved = response[0];

		// Assert
		retrieved.Should().BeSameAs(result);
	}

	/// <summary>
	/// Tests indexer throws for invalid index.
	/// </summary>
	[Fact]
	public void Indexer_InvalidIndex_ThrowsArgumentOutOfRangeException()
	{
		// Arrange
		var response = new ODataBatchResponse
		{
			Results = [new ODataBatchOperationResult()]
		};

		// Act
		var act = () => response[5];

		// Assert
		act.Should().Throw<ArgumentOutOfRangeException>();
	}

	/// <summary>
	/// Tests indexer throws for negative index.
	/// </summary>
	[Fact]
	public void Indexer_NegativeIndex_ThrowsArgumentOutOfRangeException()
	{
		// Arrange
		var response = new ODataBatchResponse
		{
			Results = [new ODataBatchOperationResult()]
		};

		// Act
		var act = () => response[-1];

		// Assert
		act.Should().Throw<ArgumentOutOfRangeException>();
	}

	#endregion

	#region GetResult<T>(int) Tests

	/// <summary>
	/// Tests GetResult&lt;T&gt;(int) returns typed result.
	/// </summary>
	[Fact]
	public void GetResult_ValidIndex_ReturnsTypedResult()
	{
		// Arrange
		var product = new TestProduct { Id = 1, Name = "Widget" };
		var response = new ODataBatchResponse
		{
			Results =
			[
				new ODataBatchOperationResult { StatusCode = 200, Result = product }
			]
		};

		// Act
		var result = response.GetResult<TestProduct>(0);

		// Assert
		result.Id.Should().Be(1);
		result.Name.Should().Be("Widget");
	}

	/// <summary>
	/// Tests GetResult&lt;T&gt;(int) throws for null result.
	/// </summary>
	[Fact]
	public void GetResult_NullResult_ThrowsInvalidOperationException()
	{
		// Arrange
		var response = new ODataBatchResponse
		{
			Results =
			[
				new ODataBatchOperationResult { StatusCode = 404, Result = null }
			]
		};

		// Act
		var act = () => response.GetResult<TestProduct>(0);

		// Assert
		act.Should().Throw<InvalidOperationException>()
			.WithMessage("*no result*");
	}

	/// <summary>
	/// Tests GetResult&lt;T&gt;(int) throws for wrong type.
	/// </summary>
	[Fact]
	public void GetResult_WrongType_ThrowsInvalidOperationException()
	{
		// Arrange
		var response = new ODataBatchResponse
		{
			Results =
			[
				new ODataBatchOperationResult { StatusCode = 200, Result = "not a product" }
			]
		};

		// Act
		var act = () => response.GetResult<TestProduct>(0);

		// Assert
		act.Should().Throw<InvalidOperationException>()
			.WithMessage("*String*TestProduct*");
	}

	/// <summary>
	/// Tests GetResult&lt;T&gt;(int) throws for invalid index.
	/// </summary>
	[Fact]
	public void GetResult_InvalidIndex_ThrowsArgumentOutOfRangeException()
	{
		// Arrange
		var response = new ODataBatchResponse();

		// Act
		var act = () => response.GetResult<TestProduct>(0);

		// Assert
		act.Should().Throw<ArgumentOutOfRangeException>();
	}

	#endregion

	#region TryGetResult<T> Tests

	/// <summary>
	/// Tests TryGetResult returns true for valid typed result.
	/// </summary>
	[Fact]
	public void TryGetResult_ValidTypedResult_ReturnsTrue()
	{
		// Arrange
		var product = new TestProduct { Id = 1, Name = "Widget" };
		var response = new ODataBatchResponse
		{
			Results =
			[
				new ODataBatchOperationResult { StatusCode = 200, Result = product }
			]
		};

		// Act
		var success = response.TryGetResult<TestProduct>(0, out var result);

		// Assert
		success.Should().BeTrue();
		result.Should().NotBeNull();
		result!.Id.Should().Be(1);
	}

	/// <summary>
	/// Tests TryGetResult returns false for invalid index.
	/// </summary>
	[Fact]
	public void TryGetResult_InvalidIndex_ReturnsFalse()
	{
		// Arrange
		var response = new ODataBatchResponse();

		// Act
		var success = response.TryGetResult<TestProduct>(0, out var result);

		// Assert
		success.Should().BeFalse();
		result.Should().BeNull();
	}

	/// <summary>
	/// Tests TryGetResult returns false for negative index.
	/// </summary>
	[Fact]
	public void TryGetResult_NegativeIndex_ReturnsFalse()
	{
		// Arrange
		var response = new ODataBatchResponse
		{
			Results = [new ODataBatchOperationResult()]
		};

		// Act
		var success = response.TryGetResult<TestProduct>(-1, out var result);

		// Assert
		success.Should().BeFalse();
		result.Should().BeNull();
	}

	/// <summary>
	/// Tests TryGetResult returns false for wrong type.
	/// </summary>
	[Fact]
	public void TryGetResult_WrongType_ReturnsFalse()
	{
		// Arrange
		var response = new ODataBatchResponse
		{
			Results =
			[
				new ODataBatchOperationResult { StatusCode = 200, Result = "not a product" }
			]
		};

		// Act
		var success = response.TryGetResult<TestProduct>(0, out var result);

		// Assert
		success.Should().BeFalse();
		result.Should().BeNull();
	}

	/// <summary>
	/// Tests TryGetResult returns false for null result.
	/// </summary>
	[Fact]
	public void TryGetResult_NullResult_ReturnsFalse()
	{
		// Arrange
		var response = new ODataBatchResponse
		{
			Results =
			[
				new ODataBatchOperationResult { StatusCode = 200, Result = null }
			]
		};

		// Act
		var success = response.TryGetResult<TestProduct>(0, out var result);

		// Assert
		success.Should().BeFalse();
		result.Should().BeNull();
	}

	#endregion

	#region GetResult(string operationId) Tests

	/// <summary>
	/// Tests GetResult by operationId returns correct result.
	/// </summary>
	[Fact]
	public void GetResult_ByOperationId_ReturnsCorrectResult()
	{
		// Arrange
		var response = new ODataBatchResponse
		{
			Results =
			[
				new ODataBatchOperationResult { OperationId = "op1", StatusCode = 200 },
				new ODataBatchOperationResult { OperationId = "op2", StatusCode = 201 },
				new ODataBatchOperationResult { OperationId = "op3", StatusCode = 204 }
			]
		};

		// Act
		var result = response.GetResult("op2");

		// Assert
		result.Should().NotBeNull();
		result!.StatusCode.Should().Be(201);
	}

	/// <summary>
	/// Tests GetResult by operationId returns null for not found.
	/// </summary>
	[Fact]
	public void GetResult_ByOperationId_NotFound_ReturnsNull()
	{
		// Arrange
		var response = new ODataBatchResponse
		{
			Results =
			[
				new ODataBatchOperationResult { OperationId = "op1", StatusCode = 200 }
			]
		};

		// Act
		var result = response.GetResult("nonexistent");

		// Assert
		result.Should().BeNull();
	}

	#endregion

	#region GetResult<T>(string operationId) Tests

	/// <summary>
	/// Tests GetResult&lt;T&gt; by operationId returns typed result.
	/// </summary>
	[Fact]
	public void GetResult_ByOperationId_Generic_ReturnsTypedResult()
	{
		// Arrange
		var product = new TestProduct { Id = 1, Name = "Widget" };
		var response = new ODataBatchResponse
		{
			Results =
			[
				new ODataBatchOperationResult { OperationId = "create-product", StatusCode = 201, Result = product }
			]
		};

		// Act
		var result = response.GetResult<TestProduct>("create-product");

		// Assert
		result.Should().NotBeNull();
		result!.Id.Should().Be(1);
	}

	/// <summary>
	/// Tests GetResult&lt;T&gt; by operationId returns default for not found.
	/// </summary>
	[Fact]
	public void GetResult_ByOperationId_Generic_NotFound_ReturnsDefault()
	{
		// Arrange
		var response = new ODataBatchResponse();

		// Act
		var result = response.GetResult<TestProduct>("nonexistent");

		// Assert
		result.Should().BeNull();
	}

	/// <summary>
	/// Tests GetResult&lt;T&gt; by operationId returns default for wrong type.
	/// </summary>
	[Fact]
	public void GetResult_ByOperationId_Generic_WrongType_ReturnsDefault()
	{
		// Arrange
		var response = new ODataBatchResponse
		{
			Results =
			[
				new ODataBatchOperationResult { OperationId = "op1", StatusCode = 200, Result = "not a product" }
			]
		};

		// Act
		var result = response.GetResult<TestProduct>("op1");

		// Assert
		result.Should().BeNull();
	}

	#endregion

	#region Test Classes

	private sealed class TestProduct
	{
		public int Id { get; set; }

		public string Name { get; set; } = string.Empty;
	}

	#endregion
}
