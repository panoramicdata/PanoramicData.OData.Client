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
		var result = new ODataBatchResponse();

		// Extract boundary from content-type
		var boundaryMatch = Regex.Match(contentType, @"boundary=([^;\s]+)");
		if (!boundaryMatch.Success)
		{
			_logger.LogWarning("ParseBatchResponse - Could not find boundary in content-type: {ContentType}", contentType);
			// Try to parse as JSON (some OData services use JSON for batch responses)
			return ParseJsonBatchResponse(content, batch);
		}

		var boundary = boundaryMatch.Groups[1].Value.Trim('"');
		var parts = content.Split([$"--{boundary}"], StringSplitOptions.RemoveEmptyEntries);

		var allOperations = batch.GetAllOperations().ToList();
		var operationIndex = 0;

		foreach (var part in parts)
		{
			if (part.Trim() == "--" || string.IsNullOrWhiteSpace(part))
			{
				continue;
			}

			// Check if this is a changeset
			if (part.Contains("multipart/mixed"))
			{
				// Parse nested changeset
				var changesetBoundaryMatch = Regex.Match(part, @"boundary=([^;\s]+)");
				if (changesetBoundaryMatch.Success)
				{
					var changesetBoundary = changesetBoundaryMatch.Groups[1].Value.Trim('"');
					var changesetParts = part.Split(new[] { $"--{changesetBoundary}" }, StringSplitOptions.RemoveEmptyEntries);

					foreach (var cspart in changesetParts)
					{
						if (cspart.Trim() == "--" || string.IsNullOrWhiteSpace(cspart) || cspart.Contains("Content-Type: multipart/mixed"))
						{
							continue;
						}

						var opResult = ParseOperationResponse(cspart, allOperations, ref operationIndex);
						if (opResult is not null)
						{
							result.Results.Add(opResult);
						}
					}
				}
			}
			else if (part.Contains("HTTP/"))
			{
				// Regular operation response
				var opResult = ParseOperationResponse(part, allOperations, ref operationIndex);
				if (opResult is not null)
				{
					result.Results.Add(opResult);
				}
			}
		}

		_logger.LogDebug("ParseBatchResponse - Parsed {Count} operation results", result.Results.Count);
		return result;
	}

	private ODataBatchOperationResult? ParseOperationResponse(string part, List<ODataBatchOperation> operations, ref int operationIndex)
	{
		var result = new ODataBatchOperationResult();

		// Parse HTTP status line
		var statusMatch = Regex.Match(part, @"HTTP/\d\.\d\s+(\d{3})\s+(.*)");
		if (!statusMatch.Success)
		{
			return null;
		}

		result.StatusCode = int.Parse(statusMatch.Groups[1].Value, CultureInfo.InvariantCulture);

		// Try to extract Content-ID
		var contentIdMatch = Regex.Match(part, @"Content-ID:\s*(\S+)", RegexOptions.IgnoreCase);
		if (contentIdMatch.Success)
		{
			result.OperationId = contentIdMatch.Groups[1].Value;
		}
		else if (operationIndex < operations.Count)
		{
			// Fall back to operation order
			result.OperationId = operations[operationIndex].Id;
		}

		// Parse response body (after empty line)
		var bodyStart = part.IndexOf("\r\n\r\n", StringComparison.Ordinal);
		if (bodyStart == -1)
		{
			bodyStart = part.IndexOf("\n\n", StringComparison.Ordinal);
		}

		if (bodyStart >= 0)
		{
			result.ResponseBody = part[(bodyStart + 4)..].Trim();

			// Try to deserialize if we have a result type
			if (!string.IsNullOrEmpty(result.ResponseBody) && operationIndex < operations.Count)
			{
				var operation = operations[operationIndex];
				if (operation.ResultType is not null && result.IsSuccess)
				{
					try
					{
						result.Result = JsonSerializer.Deserialize(result.ResponseBody, operation.ResultType, _jsonOptions);
					}
					catch (JsonException ex)
					{
						_logger.LogDebug(ex, "ParseOperationResponse - Failed to deserialize response body for operation {Id}", result.OperationId);
					}
				}
			}
		}

		if (!result.IsSuccess && !string.IsNullOrEmpty(result.ResponseBody))
		{
			result.ErrorMessage = result.ResponseBody;
		}

		operationIndex++;
		return result;
	}

	private ODataBatchResponse ParseJsonBatchResponse(string content, ODataBatchBuilder batch)
	{
		var result = new ODataBatchResponse();

		try
		{
			using var doc = JsonDocument.Parse(content);

			if (doc.RootElement.TryGetProperty("responses", out var responsesElement))
			{
				var allOperations = batch.GetAllOperations().ToList();
				var operationIndex = 0;

				foreach (var responseElement in responsesElement.EnumerateArray())
				{
					var opResult = new ODataBatchOperationResult();

					if (responseElement.TryGetProperty("id", out var idElement))
					{
						opResult.OperationId = idElement.GetString() ?? string.Empty;
					}
					else if (operationIndex < allOperations.Count)
					{
						opResult.OperationId = allOperations[operationIndex].Id;
					}

					if (responseElement.TryGetProperty("status", out var statusElement))
					{
						opResult.StatusCode = statusElement.GetInt32();
					}

					if (responseElement.TryGetProperty("body", out var bodyElement))
					{
						opResult.ResponseBody = bodyElement.GetRawText();

						if (operationIndex < allOperations.Count)
						{
							var operation = allOperations[operationIndex];
							if (operation.ResultType is not null && opResult.IsSuccess)
							{
								try
								{
									opResult.Result = JsonSerializer.Deserialize(opResult.ResponseBody, operation.ResultType, _jsonOptions);
								}
								catch (JsonException ex)
								{
									_logger.LogDebug(ex, "ParseJsonBatchResponse - Failed to deserialize response for operation {Id}", opResult.OperationId);
								}
							}
						}
					}

					result.Results.Add(opResult);
					operationIndex++;
				}
			}
		}
		catch (JsonException ex)
		{
			_logger.LogWarning(ex, "ParseJsonBatchResponse - Failed to parse JSON batch response");
		}

		return result;
	}
}
