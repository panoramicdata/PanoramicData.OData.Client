namespace PanoramicData.OData.Client;

/// <summary>
/// ODataClient - Delta query operations.
/// </summary>
public partial class ODataClient
{
	/// <summary>
	/// Gets changes since the last query using a delta link.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="deltaLink">The delta link from a previous query response.</param>
	/// <param name="headers">Optional additional headers.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A delta response containing added/modified entities and deleted entity IDs.</returns>
	public async Task<ODataDeltaResponse<T>> GetDeltaAsync<T>(
		string deltaLink,
		[Optional] IReadOnlyDictionary<string, string>? headers,
		[Optional] CancellationToken cancellationToken) where T : class
	{
		var typeName = typeof(T).Name;
		LoggerMessages.GetDeltaAsync(_logger, typeName, deltaLink);

		var content = await SendForContentAsync(HttpMethod.Get, deltaLink, headers, cancellationToken).ConfigureAwait(false);

		LoggerMessages.GetDeltaAsyncResponseReceived(_logger, typeName, content.Length);

		return ParseDeltaResponse<T>(content);
	}

	/// <summary>
	/// Gets all changes since the last query, following pagination.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="deltaLink">The delta link from a previous query response.</param>
	/// <param name="headers">Optional additional headers.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A delta response containing all added/modified entities and deleted entity IDs.</returns>
	public async Task<ODataDeltaResponse<T>> GetAllDeltaAsync<T>(
		string deltaLink,
		[Optional] IReadOnlyDictionary<string, string>? headers,
		[Optional] CancellationToken cancellationToken) where T : class
	{
		var allResults = new ODataDeltaResponse<T>();
		var typeName = typeof(T).Name;

		LoggerMessages.GetAllDeltaAsyncInitial(_logger, typeName, deltaLink);

		await FetchPagesAsync(
			deltaLink,
			(pageUrl, pageNumber) =>
			{
				LoggerMessages.GetAllDeltaAsyncFetchingPage(_logger, typeName, pageNumber, pageUrl);
				return GetDeltaAsync<T>(pageUrl, headers, cancellationToken);
			},
			page => page.NextLink,
			(page, pageNumber) =>
			{
				allResults.Value.AddRange(page.Value);
				allResults.Deleted.AddRange(page.Deleted);
				allResults.Count ??= page.Count;

				if (!string.IsNullOrEmpty(page.DeltaLink))
				{
					allResults.DeltaLink = page.DeltaLink;
				}

				LoggerMessages.GetAllDeltaAsyncPageReturned(_logger, typeName, pageNumber, page.Value.Count, page.Deleted.Count);
			},
			cancellationToken).ConfigureAwait(false);

		LoggerMessages.GetAllDeltaAsyncComplete(_logger, typeName, allResults.Value.Count, allResults.Deleted.Count);

		return allResults;
	}

	private ODataDeltaResponse<T> ParseDeltaResponse<T>(string content) where T : class
	{
		using var doc = JsonDocument.Parse(content);
		var result = new ODataDeltaResponse<T>();
		var typeName = typeof(T).Name;

		// Parse value array, separating normal entities from deleted ones
		if (doc.RootElement.TryGetProperty("value", out var valueElement))
		{
			foreach (var item in valueElement.EnumerateArray())
			{
				// Check if this is a deleted entity annotation
				if (item.TryGetProperty("@removed", out var removedElement) ||
					item.TryGetProperty("@odata.removed", out removedElement))
				{
					var deletedEntity = ParseDeletedEntity(item, removedElement);
					result.Deleted.Add(deletedEntity);
					LoggerMessages.ParseDeltaResponseDeletedEntity(_logger, deletedEntity.Id, deletedEntity.Reason);
				}
				else
				{
					// Normal entity - deserialize it
					var entity = JsonSerializer.Deserialize<T>(item.GetRawText(), _jsonOptions);
					if (entity is not null)
					{
						result.Value.Add(entity);
					}
				}
			}

			LoggerMessages.ParseDeltaResponseComplete(_logger, typeName, result.Value.Count, result.Deleted.Count);
		}

		var paging = ODataPagingAnnotations.Read(doc.RootElement);
		result.Count = paging.Count;
		result.NextLink = paging.NextLink;
		result.DeltaLink = paging.DeltaLink;

		return result;
	}

	private static ODataDeletedEntity ParseDeletedEntity(JsonElement item, JsonElement removedElement)
	{
		var deletedEntity = new ODataDeletedEntity();

		if (removedElement.TryGetProperty("reason", out var reasonElement))
		{
			deletedEntity.Reason = reasonElement.GetString();
		}

		if (item.TryGetProperty("@odata.id", out var idElement) || item.TryGetProperty("id", out idElement))
		{
			deletedEntity.Id = idElement.GetString();
		}

		return deletedEntity;
	}
}
