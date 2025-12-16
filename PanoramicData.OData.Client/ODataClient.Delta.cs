using Microsoft.Extensions.Logging;
using System.Text.Json;

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
		IReadOnlyDictionary<string, string>? headers = null,
		CancellationToken cancellationToken = default) where T : class
	{
		_logger.LogDebug("GetDeltaAsync<{Type}> - DeltaLink: {DeltaLink}", typeof(T).Name, deltaLink);

		var request = CreateRequest(HttpMethod.Get, deltaLink, headers);
		var response = await SendWithRetryAsync(request, cancellationToken).ConfigureAwait(false);
		await EnsureSuccessAsync(response, deltaLink, cancellationToken).ConfigureAwait(false);

		var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

		_logger.LogDebug("GetDeltaAsync<{Type}> - Response received, content length: {Length}", typeof(T).Name, content.Length);

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
		IReadOnlyDictionary<string, string>? headers = null,
		CancellationToken cancellationToken = default) where T : class
	{
		var allResults = new ODataDeltaResponse<T>();
		var url = deltaLink;

		_logger.LogDebug("GetAllDeltaAsync<{Type}> - Initial DeltaLink: {DeltaLink}", typeof(T).Name, deltaLink);

		var pageCount = 0;
		do
		{
			cancellationToken.ThrowIfCancellationRequested();

			pageCount++;
			_logger.LogDebug("GetAllDeltaAsync<{Type}> - Fetching page {Page}, URL: {Url}", typeof(T).Name, pageCount, url);

			var response = await GetDeltaAsync<T>(url, headers, cancellationToken).ConfigureAwait(false);
			allResults.Value.AddRange(response.Value);
			allResults.Deleted.AddRange(response.Deleted);

			// Preserve count from first page
			if (response.Count.HasValue && !allResults.Count.HasValue)
			{
				allResults.Count = response.Count;
			}

			// Keep the final delta link
			if (!string.IsNullOrEmpty(response.DeltaLink))
			{
				allResults.DeltaLink = response.DeltaLink;
			}

			_logger.LogDebug("GetAllDeltaAsync<{Type}> - Page {Page} returned {Count} items, {Deleted} deleted",
				typeof(T).Name, pageCount, response.Value.Count, response.Deleted.Count);

			url = response.NextLink;
		}
		while (!string.IsNullOrEmpty(url));

		_logger.LogDebug("GetAllDeltaAsync<{Type}> - Complete. Total items: {Total}, Total deleted: {Deleted}",
			typeof(T).Name, allResults.Value.Count, allResults.Deleted.Count);

		return allResults;
	}

	private ODataDeltaResponse<T> ParseDeltaResponse<T>(string content) where T : class
	{
		using var doc = JsonDocument.Parse(content);
		var result = new ODataDeltaResponse<T>();

		// Parse value array, separating normal entities from deleted ones
		if (doc.RootElement.TryGetProperty("value", out var valueElement))
		{
			foreach (var item in valueElement.EnumerateArray())
			{
				// Check if this is a deleted entity annotation
				if (item.TryGetProperty("@removed", out var removedElement) ||
					item.TryGetProperty("@odata.removed", out removedElement))
				{
					var deletedEntity = new ODataDeletedEntity();

					// Get the reason if present
					if (removedElement.TryGetProperty("reason", out var reasonElement))
					{
						deletedEntity.Reason = reasonElement.GetString();
					}

					// Get the ID
					if (item.TryGetProperty("@odata.id", out var idElement))
					{
						deletedEntity.Id = idElement.GetString();
					}
					else if (item.TryGetProperty("id", out idElement))
					{
						deletedEntity.Id = idElement.GetString();
					}

					result.Deleted.Add(deletedEntity);
					_logger.LogDebug("ParseDeltaResponse - Found deleted entity: {Id}, Reason: {Reason}",
						deletedEntity.Id, deletedEntity.Reason);
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

			_logger.LogDebug("ParseDeltaResponse<{Type}> - Parsed {Count} entities, {Deleted} deleted",
				typeof(T).Name, result.Value.Count, result.Deleted.Count);
		}

		// Parse count
		if (doc.RootElement.TryGetProperty("@odata.count", out var countElement))
		{
			result.Count = countElement.GetInt64();
		}

		// Parse nextLink (for paging within delta)
		if (doc.RootElement.TryGetProperty("@odata.nextLink", out var nextLinkElement))
		{
			result.NextLink = nextLinkElement.GetString();
		}

		// Parse deltaLink (for next delta query)
		if (doc.RootElement.TryGetProperty("@odata.deltaLink", out var deltaLinkElement))
		{
			result.DeltaLink = deltaLinkElement.GetString();
		}

		return result;
	}
}
