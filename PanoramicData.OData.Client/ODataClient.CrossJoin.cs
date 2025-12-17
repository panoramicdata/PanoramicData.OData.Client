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
		LoggerMessages.CrossJoinCreating(_logger, string.Join(", ", entitySets));
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
		LoggerMessages.GetCrossJoinAsync(_logger, url);

		var request = CreateRequest(HttpMethod.Get, url, crossJoin.CustomHeaders);

		var response = await SendWithRetryAsync(request, cancellationToken).ConfigureAwait(false);
		await EnsureSuccessAsync(response, url, cancellationToken).ConfigureAwait(false);

		var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

		LoggerMessages.GetCrossJoinAsyncResponse(_logger, content.Length);

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

		LoggerMessages.GetAllCrossJoinAsyncInitial(_logger, url);

		var pageCount = 0;
		do
		{
			cancellationToken.ThrowIfCancellationRequested();

			pageCount++;
			LoggerMessages.GetAllCrossJoinAsyncFetchingPage(_logger, pageCount, url);

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

			LoggerMessages.GetAllCrossJoinAsyncPageReturned(_logger, pageCount, pageResult.Value.Count);

			url = pageResult.NextLink;
		}
		while (!string.IsNullOrEmpty(url));

		LoggerMessages.GetAllCrossJoinAsyncComplete(_logger, allResults.Value.Count);

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

			LoggerMessages.ParseCrossJoinResponseComplete(_logger, result.Value.Count);
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
