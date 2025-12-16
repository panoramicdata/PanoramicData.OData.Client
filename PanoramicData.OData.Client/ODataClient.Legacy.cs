using System.Text.Json;

namespace PanoramicData.OData.Client;

/// <summary>
/// ODataClient - Legacy compatibility methods.
/// These methods are provided for backward compatibility with SimpleODataClient.
/// </summary>
public partial class ODataClient
{
	/// <summary>
	/// Clears the OData client metadata cache.
	/// </summary>
	/// <remarks>
	/// This is a static method for backward compatibility.
	/// For new code, use the instance method <see cref="InvalidateMetadataCache"/> instead.
	/// </remarks>
	[Obsolete("Use the instance method InvalidateMetadataCache() instead. This static method only clears the cache for this specific instance.")]
	public static void ClearODataClientMetaDataCache()
	{
		// Note: This static method cannot clear instance caches.
		// It's provided for API compatibility only.
		// Users should migrate to using InvalidateMetadataCache() on their client instance.
	}

	/// <summary>
	/// Creates a fluent query builder for the specified entity set that supports legacy-style execution.
	/// </summary>
	/// <param name="entitySetName">The name of the entity set.</param>
	/// <returns>A fluent query builder with execution methods.</returns>
	/// <remarks>
	/// <para>This method provides backward compatibility with Simple.OData.Client's fluent API pattern.</para>
	/// <para><b>New API pattern (recommended):</b></para>
	/// <code>
	/// // Typed approach (recommended):
	/// var query = client.For&lt;Incident&gt;("incidents").Top(10);
	/// var response = await client.GetAsync(query, cancellationToken);
	/// var incidents = response.Value;
	/// </code>
	/// </remarks>
	[Obsolete("Use For<T>(entitySetName) with a typed entity for better type safety. " +
		"Example: var query = client.For<Incident>(\"incidents\").Top(10); var response = await client.GetAsync(query, ct);")]
	public FluentODataQueryBuilder For(string entitySetName)
		=> new(this, entitySetName, _logger);

	/// <summary>
	/// Finds entries matching the specified query and returns them as dictionaries.
	/// </summary>
	/// <param name="query">The OData query URL (relative to base URL).</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A collection of dictionaries representing the entities.</returns>
	/// <remarks>
	/// This method is provided for backward compatibility.
	/// For new code, use <see cref="GetAsync{T}(ODataQueryBuilder{T}, CancellationToken)"/> with a typed entity,
	/// or use <see cref="GetRawAsync(string, IReadOnlyDictionary{string, string}?, CancellationToken)"/>
	/// and parse the JSON yourself for full control.
	/// </remarks>
	[Obsolete("Use For<T>().GetAsync() with a typed entity. Example: var response = await client.For<Product>().Top(10).GetAsync(ct);")]
	public async Task<IEnumerable<IDictionary<string, object?>>> FindEntriesAsync(
		string query,
		CancellationToken cancellationToken)
	{
		var rawResponse = await ExecuteRawQueryAsync(query, cancellationToken).ConfigureAwait(false);
		return rawResponse.GetEntries();
	}

	/// <summary>
	/// Finds a single entry matching the specified query and returns it as a dictionary.
	/// </summary>
	/// <param name="query">The OData query URL (relative to base URL).</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A dictionary representing the entity, or null if not found.</returns>
	/// <remarks>
	/// This method is provided for backward compatibility.
	/// For new code, use <see cref="GetByKeyAsync{T, TKey}(TKey, ODataQueryBuilder{T}?, CancellationToken)"/>
	/// or <see cref="GetFirstOrDefaultAsync{T}(ODataQueryBuilder{T}, CancellationToken)"/> with a typed entity.
	/// </remarks>
	[Obsolete("Use For<T>().Key(id).GetEntryAsync() or client.GetByKeyAsync<T, TKey>(key, ct) with a typed entity.")]
	public async Task<IDictionary<string, object?>?> FindEntryAsync(
		string query,
		CancellationToken cancellationToken)
	{
		var rawResponse = await ExecuteRawQueryAsync(query, cancellationToken).ConfigureAwait(false);
		return rawResponse.GetEntry();
	}

	/// <summary>
	/// Executes a raw OData query and returns the raw response.
	/// </summary>
	/// <param name="query">The OData query URL (relative to base URL).</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The raw OData response.</returns>
	/// <remarks>
	/// This method is provided for backward compatibility.
	/// For new code, use <see cref="GetRawAsync(string, IReadOnlyDictionary{string, string}?, CancellationToken)"/>
	/// which returns a <see cref="JsonDocument"/> for more efficient JSON handling.
	/// </remarks>
	[Obsolete("Use GetRawAsync(url, headers, cancellationToken) which returns a JsonDocument for more efficient JSON handling.")]
	public async Task<ODataRawResponse> ExecuteRawQueryAsync(
		string query,
		CancellationToken cancellationToken)
	{
		var request = CreateRequest(HttpMethod.Get, query);
		var response = await SendWithRetryAsync(request, cancellationToken).ConfigureAwait(false);
		await EnsureSuccessAsync(response, query, cancellationToken).ConfigureAwait(false);

		var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
		var jsonDocument = JsonDocument.Parse(content);

		return new ODataRawResponse(jsonDocument);
	}

	/// <summary>
	/// Executes a query built by a FluentODataQueryBuilder and returns results as dictionaries.
	/// </summary>
	internal async Task<ODataResponse<Dictionary<string, object?>>> GetFluentAsync(
		FluentODataQueryBuilder query,
		CancellationToken cancellationToken)
	{
		var url = query.BuildUrl();
		var request = CreateRequest(HttpMethod.Get, url, query.CustomHeaders);
		var response = await SendWithRetryAsync(request, cancellationToken).ConfigureAwait(false);
		await EnsureSuccessAsync(response, url, cancellationToken).ConfigureAwait(false);

		var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

		using var doc = JsonDocument.Parse(content);
		var result = new ODataResponse<Dictionary<string, object?>>();

		// Parse value array
		if (doc.RootElement.TryGetProperty("value", out var valueElement))
		{
			result.Value = [];
			foreach (var item in valueElement.EnumerateArray())
			{
				result.Value.Add(LegacyJsonElementToDictionary(item));
			}
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

	/// <summary>
	/// Executes a query built by a FluentODataQueryBuilder and returns all pages as dictionaries.
	/// </summary>
	internal async Task<ODataResponse<Dictionary<string, object?>>> GetAllFluentAsync(
		FluentODataQueryBuilder query,
		CancellationToken cancellationToken)
	{
		var allResults = new ODataResponse<Dictionary<string, object?>>();
		var url = query.BuildUrl();

		do
		{
			cancellationToken.ThrowIfCancellationRequested();

			var request = CreateRequest(HttpMethod.Get, url, query.CustomHeaders);
			var response = await SendWithRetryAsync(request, cancellationToken).ConfigureAwait(false);
			await EnsureSuccessAsync(response, url, cancellationToken).ConfigureAwait(false);

			var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

			using var doc = JsonDocument.Parse(content);

			// Parse value array
			if (doc.RootElement.TryGetProperty("value", out var valueElement))
			{
				foreach (var item in valueElement.EnumerateArray())
				{
					allResults.Value.Add(LegacyJsonElementToDictionary(item));
				}
			}

			// Parse count (only from first page)
			if (!allResults.Count.HasValue && doc.RootElement.TryGetProperty("@odata.count", out var countElement))
			{
				allResults.Count = countElement.GetInt64();
			}

			// Parse nextLink
			url = doc.RootElement.TryGetProperty("@odata.nextLink", out var nextLinkElement)
				? nextLinkElement.GetString()
				: null;
		}
		while (!string.IsNullOrEmpty(url));

		return allResults;
	}

	/// <summary>
	/// Gets a single entry by URL and returns it as a dictionary.
	/// </summary>
	internal async Task<Dictionary<string, object?>?> GetEntryFluentAsync(
		string url,
		IReadOnlyDictionary<string, string>? headers,
		CancellationToken cancellationToken)
	{
		var request = CreateRequest(HttpMethod.Get, url, headers);
		var response = await SendWithRetryAsync(request, cancellationToken).ConfigureAwait(false);
		await EnsureSuccessAsync(response, url, cancellationToken).ConfigureAwait(false);

		var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

		using var doc = JsonDocument.Parse(content);

		// Single entity response
		if (doc.RootElement.ValueKind == JsonValueKind.Object)
		{
			return LegacyJsonElementToDictionary(doc.RootElement);
		}

		return null;
	}
}

/// <summary>
/// Represents a raw OData response for legacy compatibility.
/// </summary>
/// <remarks>
/// This class is provided for backward compatibility with SimpleODataClient.
/// For new code, use <see cref="JsonDocument"/> directly via <see cref="ODataClient.GetRawAsync"/>.
/// </remarks>
/// <remarks>
/// Initializes a new instance of the <see cref="ODataRawResponse"/> class.
/// </remarks>
/// <param name="document">The JSON document.</param>
[Obsolete("Use JsonDocument from GetRawAsync() for new code.")]
public class ODataRawResponse(JsonDocument document) : IDisposable
{
	private bool _disposed;

	/// <summary>
	/// Gets the raw JSON document.
	/// </summary>
	public JsonDocument Document => document;

	/// <summary>
	/// Gets the entries from the response as dictionaries.
	/// </summary>
	/// <returns>A collection of dictionaries representing the entities.</returns>
	public IEnumerable<IDictionary<string, object?>> GetEntries()
	{
		if (document.RootElement.TryGetProperty("value", out var valueElement) &&
			valueElement.ValueKind == JsonValueKind.Array)
		{
			foreach (var item in valueElement.EnumerateArray())
			{
				yield return JsonElementToDictionary(item);
			}
		}
	}

	/// <summary>
	/// Gets a single entry from the response as a dictionary.
	/// </summary>
	/// <returns>A dictionary representing the entity, or null if not found.</returns>
	public IDictionary<string, object?>? GetEntry()
	{
		// Check if it's a collection response
		if (document.RootElement.TryGetProperty("value", out var valueElement))
		{
			if (valueElement.ValueKind == JsonValueKind.Array)
			{
				var enumerator = valueElement.EnumerateArray();
				if (enumerator.MoveNext())
				{
					return JsonElementToDictionary(enumerator.Current);
				}

				return null;
			}
		}

		// Single entity response
		if (document.RootElement.ValueKind == JsonValueKind.Object)
		{
			return JsonElementToDictionary(document.RootElement);
		}

		return null;
	}

	/// <summary>
	/// Converts a JsonElement to a dictionary.
	/// </summary>
	internal static Dictionary<string, object?> JsonElementToDictionary(JsonElement element)
	{
		var dict = new Dictionary<string, object?>();

		foreach (var property in element.EnumerateObject())
		{
			dict[property.Name] = JsonElementToObject(property.Value);
		}

		return dict;
	}

	private static object? JsonElementToObject(JsonElement element) => element.ValueKind switch
	{
		JsonValueKind.Null => null,
		JsonValueKind.True => true,
		JsonValueKind.False => false,
		JsonValueKind.Number when element.TryGetInt64(out var l) => l,
		JsonValueKind.Number when element.TryGetDouble(out var d) => d,
		JsonValueKind.Number => element.GetDecimal(),
		JsonValueKind.String when element.TryGetDateTime(out var dt) => dt,
		JsonValueKind.String when element.TryGetGuid(out var g) => g,
		JsonValueKind.String => element.GetString(),
		JsonValueKind.Array => element.EnumerateArray().Select(JsonElementToObject).ToList(),
		JsonValueKind.Object => JsonElementToDictionary(element),
		_ => element.GetRawText()
	};

	/// <summary>
	/// Releases the resources used by this instance.
	/// </summary>
	public void Dispose()
	{
		if (!_disposed)
		{
			document.Dispose();
			_disposed = true;
		}

		GC.SuppressFinalize(this);
	}
}
