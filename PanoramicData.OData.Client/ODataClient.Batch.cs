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
		LoggerMessages.CreateBatch(_logger);
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
		[Optional] CancellationToken cancellationToken)
	{
		var batchBoundary = $"batch_{Guid.NewGuid():N}";

		LoggerMessages.ExecuteBatchAsync(_logger, batch.Items.Count, batchBoundary);

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

		LoggerMessages.ExecuteBatchAsyncResponse(_logger, responseContent.Length, contentType);

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

		var baseUri = new Uri(_options.BaseUrl.TrimEnd('/') + "/");
		var content = new HttpMessageContent(innerRequest, baseUri);
		content.Headers.ContentType = new MediaTypeHeaderValue("application/http");

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
}
