using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PanoramicData.OData.Client;

/// <summary>
/// ODataClient - Batch operations.
/// </summary>
public partial class ODataClient
{
	/// <summary>
	/// Creates a new batch request builder.
	/// </summary>
	/// <returns>A batch builder for adding operations.</returns>
	public ODataBatchBuilder CreateBatch()
	{
		_logger.LogDebug("CreateBatch - Creating new batch request builder");
		return new ODataBatchBuilder(this, _jsonOptions);
	}

	/// <summary>
	/// Executes a batch request and returns the results.
	/// </summary>
	/// <param name="batch">The batch builder containing operations to execute.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The batch response containing all operation results.</returns>
	internal async Task<ODataBatchResponse> ExecuteBatchAsync(
		ODataBatchBuilder batch,
		CancellationToken cancellationToken = default)
	{
		var batchBoundary = $"batch_{Guid.NewGuid():N}";

		_logger.LogDebug("ExecuteBatchAsync - Executing batch with {Count} items, boundary: {Boundary}",
			batch.Items.Count, batchBoundary);

		// Build the multipart content
		var content = BuildBatchContent(batch, batchBoundary);

		var request = new HttpRequestMessage(HttpMethod.Post, "$batch")
		{
			Content = content
		};

		_options.ConfigureRequest?.Invoke(request);

		var response = await SendWithRetryAsync(request, cancellationToken).ConfigureAwait(false);
		await EnsureSuccessAsync(response, "$batch", cancellationToken).ConfigureAwait(false);

		var responseContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
		var contentType = response.Content.Headers.ContentType?.ToString() ?? "";

		_logger.LogDebug("ExecuteBatchAsync - Response received, content length: {Length}, content-type: {ContentType}",
			responseContent.Length, contentType);

		return ParseBatchResponse(responseContent, contentType, batch);
	}

	private MultipartContent BuildBatchContent(ODataBatchBuilder batch, string batchBoundary)
	{
		var batchContent = new MultipartContent("mixed", batchBoundary);

		foreach (var item in batch.Items)
		{
			if (item is ODataBatchOperation operation)
			{
				var operationContent = BuildOperationContent(operation);
				batchContent.Add(operationContent);
			}
			else if (item is ODataChangeset changeset)
			{
				var changesetContent = BuildChangesetContent(changeset);
				batchContent.Add(changesetContent);
			}
		}

		return batchContent;
	}

	private HttpMessageContent BuildOperationContent(ODataBatchOperation operation)
	{
		var method = operation.OperationType switch
		{
			ODataBatchOperationType.Get => HttpMethod.Get,
			ODataBatchOperationType.Create => HttpMethod.Post,
			ODataBatchOperationType.Update => new HttpMethod("PATCH"),
			ODataBatchOperationType.Delete => HttpMethod.Delete,
			_ => throw new ArgumentOutOfRangeException(nameof(operation), operation.OperationType, "Unknown operation type")
		};

		var innerRequest = new HttpRequestMessage(method, operation.Url);

		// Add Content-ID header
		innerRequest.Headers.TryAddWithoutValidation("Content-ID", operation.Id);

		// Add custom headers
		foreach (var header in operation.Headers)
		{
			innerRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
		}

		// Add ETag header for update/delete
		if (!string.IsNullOrEmpty(operation.ETag) &&
			(operation.OperationType == ODataBatchOperationType.Update ||
			 operation.OperationType == ODataBatchOperationType.Delete))
		{
			innerRequest.Headers.TryAddWithoutValidation("If-Match", operation.ETag);
		}

		// Add body for create/update
		if (operation.Body is not null)
		{
			var json = JsonSerializer.Serialize(operation.Body, _jsonOptions);
			innerRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");
		}

		var content = new HttpMessageContent(innerRequest);
		content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/http");
		content.Headers.TryAddWithoutValidation("Content-Transfer-Encoding", "binary");

		return content;
	}

	private MultipartContent BuildChangesetContent(ODataChangeset changeset)
	{
		var changesetContent = new MultipartContent("mixed", changeset.Id);

		foreach (var operation in changeset.Operations)
		{
			var operationContent = BuildOperationContent(operation);
			changesetContent.Add(operationContent);
		}

		return changesetContent;
	}

	private ODataBatchResponse ParseBatchResponse(string content, string contentType, ODataBatchBuilder batch)
	{
		var boundaryMatch = Regex.Match(contentType, @"boundary=([^;\s]+)");
		if (!boundaryMatch.Success)
		{
			_logger.LogWarning("ParseBatchResponse - Could not find boundary in content-type: {ContentType}", contentType);
			return ParseJsonBatchResponse(content, batch);
		}

		var boundary = boundaryMatch.Groups[1].Value.Trim('"');
		var parts = content.Split([$"--{boundary}"], StringSplitOptions.RemoveEmptyEntries);
		var allOperations = batch.GetAllOperations().ToList();

		var result = ParseBatchParts(parts, allOperations);

		_logger.LogDebug("ParseBatchResponse - Parsed {Count} operation results", result.Results.Count);
		return result;
	}

	private ODataBatchResponse ParseBatchParts(string[] parts, List<ODataBatchOperation> allOperations)
	{
		var result = new ODataBatchResponse();
		var operationIndex = 0;

		foreach (var part in parts)
		{
			if (IsEmptyPart(part))
			{
				continue;
			}

			if (part.Contains("multipart/mixed"))
			{
				ParseChangesetPart(part, allOperations, result, ref operationIndex);
			}
			else if (part.Contains("HTTP/"))
			{
				AddOperationResult(part, allOperations, result, ref operationIndex);
			}
		}

		return result;
	}

	private static bool IsEmptyPart(string part) =>
		part.Trim() == "--" || string.IsNullOrWhiteSpace(part);

	private void ParseChangesetPart(string part, List<ODataBatchOperation> allOperations, ODataBatchResponse result, ref int operationIndex)
	{
		var changesetBoundaryMatch = Regex.Match(part, @"boundary=([^;\s]+)");
		if (!changesetBoundaryMatch.Success)
		{
			return;
		}

		var changesetBoundary = changesetBoundaryMatch.Groups[1].Value.Trim('"');
		var changesetParts = part.Split([$"--{changesetBoundary}"], StringSplitOptions.RemoveEmptyEntries);

		foreach (var cspart in changesetParts)
		{
			if (IsEmptyPart(cspart) || cspart.Contains("Content-Type: multipart/mixed"))
			{
				continue;
			}

			AddOperationResult(cspart, allOperations, result, ref operationIndex);
		}
	}

	private void AddOperationResult(string part, List<ODataBatchOperation> allOperations, ODataBatchResponse result, ref int operationIndex)
	{
		var opResult = ParseOperationResponse(part, allOperations, ref operationIndex);
		if (opResult is not null)
		{
			result.Results.Add(opResult);
		}
	}

	private ODataBatchOperationResult? ParseOperationResponse(string part, List<ODataBatchOperation> operations, ref int operationIndex)
	{
		var statusMatch = Regex.Match(part, @"HTTP/\d\.\d\s+(\d{3})\s+(.*)");
		if (!statusMatch.Success)
		{
			return null;
		}

		var result = new ODataBatchOperationResult
		{
			StatusCode = int.Parse(statusMatch.Groups[1].Value, CultureInfo.InvariantCulture),
			OperationId = ExtractOperationId(part, operations, operationIndex)
		};

		ExtractResponseBody(part, result, operations, operationIndex);
		SetErrorMessage(result);

		operationIndex++;
		return result;
	}

	private static string ExtractOperationId(string part, List<ODataBatchOperation> operations, int operationIndex)
	{
		var contentIdMatch = Regex.Match(part, @"Content-ID:\s*(\S+)", RegexOptions.IgnoreCase);
		if (contentIdMatch.Success)
		{
			return contentIdMatch.Groups[1].Value;
		}

		return operationIndex < operations.Count ? operations[operationIndex].Id : string.Empty;
	}

	private void ExtractResponseBody(string part, ODataBatchOperationResult result, List<ODataBatchOperation> operations, int operationIndex)
	{
		var bodyStart = part.IndexOf("\r\n\r\n", StringComparison.Ordinal);
		if (bodyStart == -1)
		{
			bodyStart = part.IndexOf("\n\n", StringComparison.Ordinal);
		}

		if (bodyStart < 0)
		{
			return;
		}

		result.ResponseBody = part[(bodyStart + 4)..].Trim();
		TryDeserializeResult(result, operations, operationIndex);
	}

	private void TryDeserializeResult(ODataBatchOperationResult result, List<ODataBatchOperation> operations, int operationIndex)
	{
		if (string.IsNullOrEmpty(result.ResponseBody) || operationIndex >= operations.Count)
		{
			return;
		}

		var operation = operations[operationIndex];
		if (operation.ResultType is null || !result.IsSuccess)
		{
			return;
		}

		try
		{
			result.Result = JsonSerializer.Deserialize(result.ResponseBody, operation.ResultType, _jsonOptions);
		}
		catch (JsonException ex)
		{
			_logger.LogDebug(ex, "ParseOperationResponse - Failed to deserialize response body for operation {Id}", result.OperationId);
		}
	}

	private static void SetErrorMessage(ODataBatchOperationResult result)
	{
		if (!result.IsSuccess && !string.IsNullOrEmpty(result.ResponseBody))
		{
			result.ErrorMessage = result.ResponseBody;
		}
	}

	private ODataBatchResponse ParseJsonBatchResponse(string content, ODataBatchBuilder batch)
	{
		var result = new ODataBatchResponse();

		try
		{
			using var doc = JsonDocument.Parse(content);
			if (doc.RootElement.TryGetProperty("responses", out var responsesElement))
			{
				ParseJsonResponses(responsesElement, batch.GetAllOperations().ToList(), result);
			}
		}
		catch (JsonException ex)
		{
			_logger.LogWarning(ex, "ParseJsonBatchResponse - Failed to parse JSON batch response");
		}

		return result;
	}

	private void ParseJsonResponses(JsonElement responsesElement, List<ODataBatchOperation> allOperations, ODataBatchResponse result)
	{
		var operationIndex = 0;

		foreach (var responseElement in responsesElement.EnumerateArray())
		{
			var opResult = ParseJsonOperationResult(responseElement, allOperations, operationIndex);
			result.Results.Add(opResult);
			operationIndex++;
		}
	}

	private ODataBatchOperationResult ParseJsonOperationResult(JsonElement responseElement, List<ODataBatchOperation> allOperations, int operationIndex)
	{
		var opResult = new ODataBatchOperationResult
		{
			OperationId = GetJsonOperationId(responseElement, allOperations, operationIndex),
			StatusCode = GetJsonStatusCode(responseElement)
		};

		ParseJsonBody(responseElement, opResult, allOperations, operationIndex);
		return opResult;
	}

	private static string GetJsonOperationId(JsonElement responseElement, List<ODataBatchOperation> allOperations, int operationIndex)
	{
		if (responseElement.TryGetProperty("id", out var idElement))
		{
			return idElement.GetString() ?? string.Empty;
		}

		return operationIndex < allOperations.Count ? allOperations[operationIndex].Id : string.Empty;
	}

	private static int GetJsonStatusCode(JsonElement responseElement) =>
		responseElement.TryGetProperty("status", out var statusElement) ? statusElement.GetInt32() : 0;

	private void ParseJsonBody(JsonElement responseElement, ODataBatchOperationResult opResult, List<ODataBatchOperation> allOperations, int operationIndex)
	{
		if (!responseElement.TryGetProperty("body", out var bodyElement))
		{
			return;
		}

		opResult.ResponseBody = bodyElement.GetRawText();
		TryDeserializeJsonResult(opResult, allOperations, operationIndex);
	}

	private void TryDeserializeJsonResult(ODataBatchOperationResult opResult, List<ODataBatchOperation> allOperations, int operationIndex)
	{
		if (operationIndex >= allOperations.Count)
		{
			return;
		}

		var operation = allOperations[operationIndex];
		if (operation.ResultType is null || !opResult.IsSuccess)
		{
			return;
		}

		try
		{
			opResult.Result = JsonSerializer.Deserialize(opResult.ResponseBody!, operation.ResultType, _jsonOptions);
		}
		catch (JsonException ex)
		{
			_logger.LogDebug(ex, "ParseJsonBatchResponse - Failed to deserialize response for operation {Id}", opResult.OperationId);
		}
	}
}
