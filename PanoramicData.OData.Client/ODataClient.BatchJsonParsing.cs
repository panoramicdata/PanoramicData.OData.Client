namespace PanoramicData.OData.Client;

/// <summary>
/// ODataClient - Batch JSON response parsing.
/// </summary>
public partial class ODataClient
{
	private ODataBatchResponse ParseJsonBatchResponse(string content, ODataBatchBuilder batch)
	{
		var result = new ODataBatchResponse();

		try
		{
			using var doc = JsonDocument.Parse(content);
			if (doc.RootElement.TryGetProperty("responses", out var responsesElement))
			{
				ParseJsonResponses(responsesElement, [.. batch.GetAllOperations()], result);
			}
		}
		catch (JsonException ex)
		{
			LoggerMessages.ParseJsonBatchResponseFailed(_logger, ex);
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
			LoggerMessages.ParseJsonBatchResponseDeserializeFailed(_logger, ex, opResult.OperationId);
		}
	}
}
