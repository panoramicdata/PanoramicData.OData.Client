using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using PanoramicData.OData.Client.Test.Models;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Unit tests for OData batch request support.
/// </summary>
public class ODataClientBatchTests : TestBase, IDisposable
{
	private readonly Mock<HttpMessageHandler> _mockHandler;
	private readonly HttpClient _httpClient;
	private readonly ODataClient _client;

	/// <summary>
	/// Initializes a new instance of the test class.
	/// </summary>
	public ODataClientBatchTests()
	{
		_mockHandler = new Mock<HttpMessageHandler>();
		_httpClient = new HttpClient(_mockHandler.Object)
		{
			BaseAddress = new Uri("https://test.odata.org/")
		};
		_client = new ODataClient(new ODataClientOptions
		{
			BaseUrl = "https://test.odata.org/",
			HttpClient = _httpClient,
			Logger = NullLogger.Instance,
			RetryCount = 0
		});
	}

	/// <inheritdoc/>
	public void Dispose()
	{
		_client.Dispose();
		_httpClient.Dispose();
		GC.SuppressFinalize(this);
	}

	#region CreateBatch Tests

	/// <summary>
	/// Tests that CreateBatch returns a batch builder.
	/// </summary>
	[Fact]
	public void CreateBatch_ShouldReturnBatchBuilder()
	{
		// Act
		var batch = _client.CreateBatch();

		// Assert
		batch.Should().NotBeNull();
		batch.Should().BeOfType<ODataBatchBuilder>();
	}

	#endregion

	#region Batch Operation Builder Tests

	/// <summary>
	/// Tests that Get adds a GET operation and returns builder for chaining.
	/// </summary>
	[Fact]
	public void Batch_Get_ShouldReturnBuilderAndAddOperation()
	{
		// Arrange
		var batch = _client.CreateBatch();

		// Act
		var result = batch.Get<Product>(entitySet: "Products", key: 1);

		// Assert
		result.Should().BeSameAs(batch);
		batch.Items.Should().ContainSingle();
	}

	/// <summary>
	/// Tests that Create adds a POST operation and returns builder for chaining.
	/// </summary>
	[Fact]
	public void Batch_Create_ShouldReturnBuilderAndAddOperation()
	{
		// Arrange
		var batch = _client.CreateBatch();
		var product = new Product { Name = "Test Product" };

		// Act
		var result = batch.Create("Products", product);

		// Assert
		result.Should().BeSameAs(batch);
		batch.Items.Should().ContainSingle();
	}

	/// <summary>
	/// Tests that Update adds a PATCH operation and returns builder for chaining.
	/// </summary>
	[Fact]
	public void Batch_Update_ShouldReturnBuilderAndAddOperation()
	{
		// Arrange
		var batch = _client.CreateBatch();

		// Act
		var result = batch.Update<Product>("Products", 1, new { Name = "Updated" });

		// Assert
		result.Should().BeSameAs(batch);
		batch.Items.Should().ContainSingle();
	}

	/// <summary>
	/// Tests that Delete adds a DELETE operation and returns builder for chaining.
	/// </summary>
	[Fact]
	public void Batch_Delete_ShouldReturnBuilderAndAddOperation()
	{
		// Arrange
		var batch = _client.CreateBatch();

		// Act
		var result = batch.Delete("Products", 1);

		// Assert
		result.Should().BeSameAs(batch);
		batch.Items.Should().ContainSingle();
	}

	/// <summary>
	/// Tests that multiple operations can be chained fluently.
	/// </summary>
	[Fact]
	public void Batch_FluentChaining_ShouldAddAllOperations()
	{
		// Arrange & Act
		var batch = _client.CreateBatch()
			.Get<Product>("Products", 1)
			.Create("Products", new Product { Name = "New" })
			.Update<Product>("Products", 2, new { Name = "Updated" })
			.Delete("Products", 3);

		// Assert
		batch.Items.Should().HaveCount(4);
		batch.GetAllOperations().Should().HaveCount(4);
	}

	#endregion

	#region Changeset Tests

	/// <summary>
	/// Tests that Changeset adds a changeset using the action pattern.
	/// </summary>
	[Fact]
	public void Batch_Changeset_ShouldAddChangesetWithOperations()
	{
		// Arrange & Act
		var batch = _client.CreateBatch()
			.Changeset(cs => cs
				.Create("Products", new Product { Name = "Test" })
				.Update<Product>("Products", 1, new { Name = "Updated" })
				.Delete("Products", 2));

		// Assert
		batch.Items.Should().ContainSingle(); // One changeset
		batch.GetAllOperations().Should().HaveCount(3); // Three operations
	}

	/// <summary>
	/// Tests that changeset operations return builder for chaining.
	/// </summary>
	[Fact]
	public void Changeset_Operations_ShouldReturnBuilderForChaining()
	{
		// Arrange
		ODataChangesetBuilder? capturedCs = null;

		// Act
		_client.CreateBatch()
			.Changeset(cs =>
			{
				capturedCs = cs;
				var result1 = cs.Create("Products", new Product { Name = "Test" });
				var result2 = cs.Update<Product>("Products", 1, new { Name = "Updated" });
				var result3 = cs.Delete("Products", 2);

				// All should return same builder
				result1.Should().BeSameAs(cs);
				result2.Should().BeSameAs(cs);
				result3.Should().BeSameAs(cs);
			});

		// Assert
		capturedCs.Should().NotBeNull();
	}

	/// <summary>
	/// Tests that mixed operations and changesets maintain order with fluent API.
	/// </summary>
	[Fact]
	public void Batch_FluentMixedOperationsAndChangesets_ShouldMaintainOrder()
	{
		// Act - Fully fluent chain
		var batch = _client.CreateBatch()
			.Get<Product>("Products", 1)
			.Changeset(cs => cs
				.Create("Products", new Product { Name = "Test" })
				.Update<Product>("Products", 2, new { Name = "Updated" }))
			.Get<Product>("Products", 3);

		// Assert
		batch.Items.Should().HaveCount(3); // GET, Changeset, GET
		batch.GetAllOperations().Should().HaveCount(4); // 2 GETs + 2 changeset operations
	}

	#endregion

	#region Batch Execution Tests

	/// <summary>
	/// Tests that ExecuteAsync sends a batch request.
	/// </summary>
	[Fact]
	public async Task Batch_ExecuteAsync_ShouldSendBatchRequest()
	{
		// Arrange
		HttpRequestMessage? capturedRequest = null;
		var responseContent = BuildMultipartResponse([
			(200, """{"Id":1,"Name":"Product 1"}"""),
			(200, """{"Id":2,"Name":"Product 2"}""")
		]);

		var response = new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent(responseContent)
		};
		response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/mixed; boundary=batch_response");

		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
			.ReturnsAsync(response);

		// Act
		var result = await _client.CreateBatch()
			.Get<Product>("Products", 1)
			.Get<Product>("Products", 2)
			.ExecuteAsync(CancellationToken);

		// Assert
		result.Should().NotBeNull();
		capturedRequest.Should().NotBeNull();
		capturedRequest!.RequestUri!.ToString().Should().Contain("$batch");
	}

	/// <summary>
	/// Tests that ExecuteAsync parses multiple results accessible by index.
	/// </summary>
	[Fact]
	public async Task Batch_ExecuteAsync_ShouldParseResultsAccessibleByIndex()
	{
		// Arrange
		var responseContent = BuildMultipartResponse([
			(200, """{"Id":1,"Name":"Product 1"}"""),
			(201, """{"Id":3,"Name":"New Product"}"""),
			(204, ""),
			(204, "")
		]);

		var response = new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent(responseContent)
		};
		response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/mixed; boundary=batch_response");

		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(response);

		// Act
		var result = await _client.CreateBatch()
			.Get<Product>("Products", 1)
			.Create("Products", new Product { Name = "New Product" })
			.Update<Product>("Products", 2, new { Name = "Updated" })
			.Delete("Products", 3)
			.ExecuteAsync(CancellationToken);

		// Assert
		result.Results.Should().HaveCount(4);
		result.AllSucceeded.Should().BeTrue();
		result[0].StatusCode.Should().Be(200);
		result[1].StatusCode.Should().Be(201);
		result[2].StatusCode.Should().Be(204);
		result[3].StatusCode.Should().Be(204);
	}

	/// <summary>
	/// Tests that partial failure reports failed results.
	/// </summary>
	[Fact]
	public async Task Batch_ExecuteAsync_PartialFailure_ShouldReportFailedResults()
	{
		// Arrange
		var responseContent = BuildMultipartResponse([
			(200, """{"Id":1,"Name":"Product 1"}"""),
			(404, """{"error":{"message":"Not found"}}""")
		]);

		var response = new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent(responseContent)
		};
		response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/mixed; boundary=batch_response");

		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(response);

		// Act
		var result = await _client.CreateBatch()
			.Get<Product>("Products", 1)
			.Get<Product>("Products", 999)
			.ExecuteAsync(CancellationToken);

		// Assert
		result.AllSucceeded.Should().BeFalse();
		result.HasErrors.Should().BeTrue();
		result.FailedResults.Should().ContainSingle();
		result.FailedResults.First().StatusCode.Should().Be(404);
	}

	#endregion

	#region Batch with ETag Tests

	/// <summary>
	/// Tests that Update with ETag includes ETag in operation.
	/// </summary>
	[Fact]
	public void Batch_UpdateWithETag_ShouldIncludeETagInOperation()
	{
		// Arrange
		const string etag = "\"abc123\"";

		// Act
		var batch = _client.CreateBatch()
			.Update<Product>("Products", 1, new { Name = "Updated" }, etag);

		// Assert
		var operation = batch.GetAllOperations().First();
		operation.ETag.Should().Be(etag);
	}

	/// <summary>
	/// Tests that Delete with ETag includes ETag in operation.
	/// </summary>
	[Fact]
	public void Batch_DeleteWithETag_ShouldIncludeETagInOperation()
	{
		// Arrange
		const string etag = "\"xyz789\"";

		// Act
		var batch = _client.CreateBatch()
			.Delete("Products", 1, etag);

		// Assert
		var operation = batch.GetAllOperations().First();
		operation.ETag.Should().Be(etag);
	}

	#endregion

	#region Batch Response Tests

	/// <summary>
	/// Tests AllSucceeded when all operations succeed.
	/// </summary>
	[Fact]
	public void ODataBatchResponse_AllSucceeded_AllSuccess_ShouldReturnTrue()
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
	/// Tests AllSucceeded when some operations fail.
	/// </summary>
	[Fact]
	public void ODataBatchResponse_AllSucceeded_WithFailure_ShouldReturnFalse()
	{
		// Arrange
		var response = new ODataBatchResponse
		{
			Results =
			[
				new ODataBatchOperationResult { StatusCode = 200 },
				new ODataBatchOperationResult { StatusCode = 404 }
			]
		};

		// Assert
		response.AllSucceeded.Should().BeFalse();
	}

	/// <summary>
	/// Tests HasErrors when some operations fail.
	/// </summary>
	[Fact]
	public void ODataBatchResponse_HasErrors_WithFailure_ShouldReturnTrue()
	{
		// Arrange
		var response = new ODataBatchResponse
		{
			Results =
			[
				new ODataBatchOperationResult { StatusCode = 200 },
				new ODataBatchOperationResult { StatusCode = 404 }
			]
		};

		// Assert
		response.HasErrors.Should().BeTrue();
	}

	/// <summary>
	/// Tests FailedResults returns only failures.
	/// </summary>
	[Fact]
	public void ODataBatchResponse_FailedResults_ShouldReturnOnlyFailures()
	{
		// Arrange
		var response = new ODataBatchResponse
		{
			Results =
			[
				new ODataBatchOperationResult { OperationId = "1", StatusCode = 200 },
				new ODataBatchOperationResult { OperationId = "2", StatusCode = 404 },
				new ODataBatchOperationResult { OperationId = "3", StatusCode = 500 }
			]
		};

		// Act
		var failed = response.FailedResults.ToList();

		// Assert
		failed.Should().HaveCount(2);
		failed.Should().Contain(r => r.OperationId == "2");
		failed.Should().Contain(r => r.OperationId == "3");
	}

	/// <summary>
	/// Tests GetResult by index.
	/// </summary>
	[Fact]
	public void ODataBatchResponse_Indexer_ShouldReturnCorrectResult()
	{
		// Arrange
		var response = new ODataBatchResponse
		{
			Results =
			[
				new ODataBatchOperationResult { OperationId = "op1", StatusCode = 200 },
				new ODataBatchOperationResult { OperationId = "op2", StatusCode = 201 }
			]
		};

		// Act & Assert
		response[0].OperationId.Should().Be("op1");
		response[1].OperationId.Should().Be("op2");
	}

	/// <summary>
	/// Tests typed GetResult by index.
	/// </summary>
	[Fact]
	public void ODataBatchResponse_GetResultByIndex_ShouldReturnTypedResult()
	{
		// Arrange
		var product = new Product { Id = 1, Name = "Test" };
		var response = new ODataBatchResponse
		{
			Results =
			[
				new ODataBatchOperationResult { StatusCode = 200, Result = product }
			]
		};

		// Act
		var result = response.GetResult<Product>(0);

		// Assert
		result.Should().BeSameAs(product);
	}

	/// <summary>
	/// Tests TryGetResult by index.
	/// </summary>
	[Fact]
	public void ODataBatchResponse_TryGetResult_ShouldReturnTrueWhenFound()
	{
		// Arrange
		var product = new Product { Id = 1, Name = "Test" };
		var response = new ODataBatchResponse
		{
			Results =
			[
				new ODataBatchOperationResult { StatusCode = 200, Result = product }
			]
		};

		// Act
		var success = response.TryGetResult<Product>(0, out var result);

		// Assert
		success.Should().BeTrue();
		result.Should().BeSameAs(product);
	}

	/// <summary>
	/// Tests TryGetResult returns false for out of range index.
	/// </summary>
	[Fact]
	public void ODataBatchResponse_TryGetResult_OutOfRange_ShouldReturnFalse()
	{
		// Arrange
		var response = new ODataBatchResponse
		{
			Results = [new ODataBatchOperationResult { StatusCode = 200 }]
		};

		// Act
		var success = response.TryGetResult<Product>(5, out var result);

		// Assert
		success.Should().BeFalse();
		result.Should().BeNull();
	}

	/// <summary>
	/// Tests GetResult by operation ID.
	/// </summary>
	[Fact]
	public void ODataBatchResponse_GetResult_ById_ShouldReturnCorrectResult()
	{
		// Arrange
		var response = new ODataBatchResponse
		{
			Results =
			[
				new ODataBatchOperationResult { OperationId = "op1", StatusCode = 200 },
				new ODataBatchOperationResult { OperationId = "op2", StatusCode = 201 }
			]
		};

		// Act
		var result = response.GetResult("op2");

		// Assert
		result.Should().NotBeNull();
		result!.OperationId.Should().Be("op2");
		result.StatusCode.Should().Be(201);
	}

	/// <summary>
	/// Tests GetResult returns null when not found.
	/// </summary>
	[Fact]
	public void ODataBatchResponse_GetResult_NotFound_ShouldReturnNull()
	{
		// Arrange
		var response = new ODataBatchResponse
		{
			Results = [new ODataBatchOperationResult { OperationId = "op1", StatusCode = 200 }]
		};

		// Act
		var result = response.GetResult("nonexistent");

		// Assert
		result.Should().BeNull();
	}

	#endregion

	#region JSON Batch Response Tests

	/// <summary>
	/// Tests parsing JSON batch response format.
	/// </summary>
	[Fact]
	public async Task Batch_ExecuteAsync_ShouldParseJsonBatchResponse()
	{
		// Arrange
		var jsonResponse = """
		{
			"responses": [
				{
					"id": "op1",
					"status": 200,
					"body": {"Id": 1, "Name": "Product 1"}
				},
				{
					"id": "op2",
					"status": 201,
					"body": {"Id": 2, "Name": "New Product"}
				}
			]
		}
		""";

		var response = new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent(jsonResponse)
		};
		response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(response);

		// Act
		var result = await _client.CreateBatch()
			.Get<Product>("Products", 1)
			.Create("Products", new Product { Name = "New Product" })
			.ExecuteAsync(CancellationToken);

		// Assert
		result.Results.Should().HaveCount(2);
		result.AllSucceeded.Should().BeTrue();
	}

	#endregion

	#region Helper Methods

	private static string BuildMultipartResponse(List<(int StatusCode, string Body)> responses)
	{
		var sb = new StringBuilder();
		const string boundary = "batch_response";

		foreach (var (statusCode, body) in responses)
		{
			sb.AppendLine();
			sb.Append("--");
			sb.AppendLine(boundary);
			sb.AppendLine("Content-Type: application/http");
			sb.AppendLine("Content-Transfer-Encoding: binary");
			sb.AppendLine();
			sb.Append("HTTP/1.1 ");
			sb.Append(statusCode);
			sb.Append(' ');
			sb.AppendLine(GetStatusDescription(statusCode));

			if (!string.IsNullOrEmpty(body))
			{
				sb.AppendLine("Content-Type: application/json");
			}

			sb.AppendLine();

			if (!string.IsNullOrEmpty(body))
			{
				sb.AppendLine(body);
			}
		}

		sb.Append("--");
		sb.Append(boundary);
		sb.AppendLine("--");

		return sb.ToString();
	}

	private static string GetStatusDescription(int statusCode) => statusCode switch
	{
		200 => "OK",
		201 => "Created",
		204 => "No Content",
		404 => "Not Found",
		500 => "Internal Server Error",
		_ => "Unknown"
	};

	#endregion
}
