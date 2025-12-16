using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace PanoramicData.OData.Client;

/// <summary>
/// ODataClient - Cross-join query operations.
/// </summary>
public partial class ODataClient
{
	/// <summary>
	/// Creates a cross-join query builder for combining multiple entity sets.
	/// OData V4 cross-join allows querying across multiple entity sets.
	/// </summary>
	/// <param name="entitySets">The entity set names to join.</param>
	/// <returns>A cross-join query builder.</returns>
	public ODataCrossJoinBuilder CrossJoin(params string[] entitySets)
	{
		_logger.LogDebug("CrossJoin - Creating cross-join for entity sets: {EntitySets}", string.Join(", ", entitySets));
		return new ODataCrossJoinBuilder(entitySets, _logger);
	}

	/// <summary>
	/// Executes a cross-join query and returns the results.
	/// </summary>
	/// <param name="crossJoin">The cross-join query builder.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The cross-join response containing result rows.</returns>
	public async Task<ODataCrossJoinResponse> GetCrossJoinAsync(
		ODataCrossJoinBuilder crossJoin,
		CancellationToken cancellationToken = default)
	{
		var url = crossJoin.BuildUrl();
		_logger.LogDebug("GetCrossJoinAsync - URL: {Url}", url);

		var request = CreateRequest(HttpMethod.Get, url, crossJoin.CustomHeaders);

		var response = await SendWithRetryAsync(request, cancellationToken).ConfigureAwait(false);
		await EnsureSuccessAsync(response, url, cancellationToken).ConfigureAwait(false);

		var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

		_logger.LogDebug("GetCrossJoinAsync - Response received, content length: {Length}", content.Length);

		return ParseCrossJoinResponse(content);
	}

	/// <summary>
	/// Executes a cross-join query and returns all results, following pagination.
	/// </summary>
	/// <param name="crossJoin">The cross-join query builder.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The cross-join response containing all result rows.</returns>
	public async Task<ODataCrossJoinResponse> GetAllCrossJoinAsync(
		ODataCrossJoinBuilder crossJoin,
		CancellationToken cancellationToken = default)
	{
		var allResults = new ODataCrossJoinResponse();
		var url = crossJoin.BuildUrl();

		_logger.LogDebug("GetAllCrossJoinAsync - Initial URL: {Url}", url);

		var pageCount = 0;
		do
		{
			cancellationToken.ThrowIfCancellationRequested();

			pageCount++;
			_logger.LogDebug("GetAllCrossJoinAsync - Fetching page {Page}, URL: {Url}", pageCount, url);

			var request = CreateRequest(HttpMethod.Get, url, crossJoin.CustomHeaders);
			var response = await SendWithRetryAsync(request, cancellationToken).ConfigureAwait(false);
			await EnsureSuccessAsync(response, url, cancellationToken).ConfigureAwait(false);

			var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
			var pageResult = ParseCrossJoinResponse(content);

			allResults.Value.AddRange(pageResult.Value);

			// Only set count from first page
			if (pageResult.Count.HasValue && !allResults.Count.HasValue)
			{
				allResults.Count = pageResult.Count;
			}

			_logger.LogDebug("GetAllCrossJoinAsync - Page {Page} returned {Count} rows", pageCount, pageResult.Value.Count);

			url = pageResult.NextLink;
		}
		while (!string.IsNullOrEmpty(url));

		_logger.LogDebug("GetAllCrossJoinAsync - Complete. Total rows: {Total}", allResults.Value.Count);

		return allResults;
	}

	private ODataCrossJoinResponse ParseCrossJoinResponse(string content)
	{
		using var doc = JsonDocument.Parse(content);
		var result = new ODataCrossJoinResponse();

		// Parse value array
		if (doc.RootElement.TryGetProperty("value", out var valueElement))
		{
			foreach (var item in valueElement.EnumerateArray())
			{
				var row = new ODataCrossJoinResult();

				// Each property in the row is an entity set name
				foreach (var property in item.EnumerateObject())
				{
					row.Entities[property.Name] = property.Value.Clone();
				}

				result.Value.Add(row);
			}

			_logger.LogDebug("ParseCrossJoinResponse - Parsed {Count} rows", result.Value.Count);
		}

		// Parse count
		if (doc.RootElement.TryGetProperty("@odata.count", out var countElement))
		{
			result.Count = countElement.GetInt64();
		}

		// Parse nextLink
		if (doc.RootElement.TryGetProperty("@odata.nextLink", out var nextLinkElement))
		{
			result.NextLink = nextLinkElement.GetString();
		}

		return result;
	}
}
