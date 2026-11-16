namespace PanoramicData.OData.Client;

/// <summary>
/// ODataClient - Batch multipart response parsing.
/// </summary>
public partial class ODataClient
{
	private ODataBatchResponse ParseBatchResponse(string content, string contentType, ODataBatchBuilder batch)
	{
		var boundaryMatch = BoundaryMatchRegex().Match(contentType);
		if (!boundaryMatch.Success)
		{
			LoggerMessages.ParseBatchResponseNoBoundary(_logger, contentType);
			return ParseJsonBatchResponse(content, batch);
		}

		var boundary = boundaryMatch.Groups[1].Value.Trim('"');
		var parts = content.Split([$"--{boundary}"], StringSplitOptions.RemoveEmptyEntries);
		var allOperations = batch.GetAllOperations().ToList();

		var result = ParseBatchParts(parts, allOperations);

		LoggerMessages.ParseBatchResponseComplete(_logger, result.Results.Count);
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
		var changesetBoundaryMatch = BoundaryMatchRegex().Match(part);
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
		var statusMatch = StatusMatchRegex().Match(part);
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
		var contentIdMatch = ContentIdMatchRegex().Match(part);
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
			LoggerMessages.ParseOperationResponseDeserializeFailed(_logger, ex, result.OperationId);
		}
	}

	private static void SetErrorMessage(ODataBatchOperationResult result)
	{
		if (!result.IsSuccess && !string.IsNullOrEmpty(result.ResponseBody))
		{
			result.ErrorMessage = result.ResponseBody;
		}
	}

	/// <summary>
	/// Returns a regular expression that matches the value of the 'boundary' parameter in a string, such as a MIME content
	/// type header.
	/// </summary>
	/// <remarks>The returned regular expression captures the boundary value in the first capturing group. This is
	/// commonly used to extract multipart boundary values from content type headers in HTTP requests or
	/// responses.</remarks>
	/// <returns>A <see cref="Regex"/> instance configured to match the 'boundary' parameter and capture its value.</returns>
	[GeneratedRegex(@"boundary=([^;\s]+)")]
	private static partial Regex BoundaryMatchRegex();

	/// <summary>
	/// Gets a regular expression that matches HTTP status lines and captures the status code and reason phrase.
	/// </summary>
	/// <remarks>The returned regular expression expects the input to begin with "HTTP/", followed by a version
	/// number, a three-digit status code, and a reason phrase separated by whitespace. This can be used to parse HTTP
	/// response status lines such as "HTTP/1.1 404 Not Found".</remarks>
	/// <returns>A <see cref="Regex"/> instance that matches HTTP status lines in the format "HTTP/x.x status-code
	/// reason-phrase". The first capture group contains the three-digit status code, and the second contains the reason
	/// phrase.
	/// </returns>
	[GeneratedRegex(@"HTTP/\d\.\d\s+(\d{3})\s+(.*)")]
	private static partial Regex StatusMatchRegex();


	/// <summary>
	/// Creates a regular expression that matches the 'Content-ID' header and captures its value in a case-insensitive
	/// manner.
	/// </summary>
	/// <remarks>The returned regular expression uses the pattern <c>Content-ID:\s*(\S+)</c> with case-insensitive
	/// matching and is culture-invariant to 'en-GB'. The first capturing group contains the value of the 'Content-ID'
	/// header, excluding leading whitespace.</remarks>
	/// <returns>A <see cref="Regex"/> instance configured to match and capture the value of the 'Content-ID' header.</returns>
	[GeneratedRegex(@"Content-ID:\s*(\S+)", RegexOptions.IgnoreCase, "en-GB")]
	private static partial Regex ContentIdMatchRegex();
}
