using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace PanoramicData.OData.Client;

/// <summary>
/// A fluent query builder that holds a reference to the ODataClient for executing queries.
/// This provides backward compatibility with Simple.OData.Client's fluent API pattern.
/// </summary>
/// <remarks>
/// <para>This class is provided for backward compatibility. For new code, use the typed API:</para>
/// <code>
/// var query = client.For&lt;Product&gt;("Products").Top(10);
/// var response = await client.GetAsync(query, cancellationToken);
/// </code>
/// </remarks>
public class FluentODataQueryBuilder
{
	private readonly ODataClient _client;
	private readonly string _entitySet;
	private readonly List<string> _filterClauses = [];
	private readonly List<string> _orderByClauses = [];
	private readonly List<string> _selectFields = [];
	private readonly List<string> _expandFields = [];
	private readonly Dictionary<string, string> _customHeaders = [];
	private long? _skip;
	private long? _top;
	private bool _count;
	private object? _key;
	private string? _search;

	/// <summary>
	/// Initializes a new instance of the <see cref="FluentODataQueryBuilder"/> class.
	/// </summary>
	internal FluentODataQueryBuilder(ODataClient client, string entitySet, ILogger logger)
	{
		_client = client;
		_entitySet = entitySet;
		_ = logger; // Reserved for future use
	}

	/// <summary>
	/// Gets the custom headers configured for this query.
	/// </summary>
	public IReadOnlyDictionary<string, string> CustomHeaders => _customHeaders;

	#region Query Building Methods

	/// <summary>
	/// Sets the key for a single entity query.
	/// </summary>
	/// <typeparam name="TKey">The type of the key.</typeparam>
	/// <param name="key">The entity key.</param>
	/// <returns>This builder for method chaining.</returns>
	public FluentODataQueryBuilder Key<TKey>(TKey key)
	{
		_key = key;
		return this;
	}

	/// <summary>
	/// Adds a raw filter string.
	/// </summary>
	/// <param name="rawFilter">The OData filter expression.</param>
	/// <returns>This builder for method chaining.</returns>
	public FluentODataQueryBuilder Filter(string rawFilter)
	{
		if (!string.IsNullOrWhiteSpace(rawFilter))
		{
			_filterClauses.Add(rawFilter);
		}

		return this;
	}

	/// <summary>
	/// Adds a search term.
	/// </summary>
	/// <param name="searchTerm">The search term.</param>
	/// <returns>This builder for method chaining.</returns>
	public FluentODataQueryBuilder Search(string searchTerm)
	{
		_search = searchTerm;
		return this;
	}

	/// <summary>
	/// Adds select fields from a comma-separated string.
	/// </summary>
	/// <param name="fields">Comma-separated field names.</param>
	/// <returns>This builder for method chaining.</returns>
	public FluentODataQueryBuilder Select(string fields)
	{
		if (!string.IsNullOrWhiteSpace(fields))
		{
			_selectFields.AddRange(fields.Split(',').Select(f => f.Trim()));
		}

		return this;
	}

	/// <summary>
	/// Adds expand fields from a comma-separated string.
	/// </summary>
	/// <param name="fields">Comma-separated navigation property names.</param>
	/// <returns>This builder for method chaining.</returns>
	public FluentODataQueryBuilder Expand(string fields)
	{
		if (!string.IsNullOrWhiteSpace(fields))
		{
			_expandFields.AddRange(fields.Split(',').Select(f => f.Trim()));
		}

		return this;
	}

	/// <summary>
	/// Adds a raw order by string.
	/// </summary>
	/// <param name="orderBy">The order by expression.</param>
	/// <returns>This builder for method chaining.</returns>
	public FluentODataQueryBuilder OrderBy(string orderBy)
	{
		if (!string.IsNullOrWhiteSpace(orderBy))
		{
			_orderByClauses.Add(orderBy);
		}

		return this;
	}

	/// <summary>
	/// Adds a descending order by clause.
	/// </summary>
	/// <param name="field">The field to order by descending.</param>
	/// <returns>This builder for method chaining.</returns>
	public FluentODataQueryBuilder OrderByDescending(string field)
	{
		if (!string.IsNullOrWhiteSpace(field))
		{
			_orderByClauses.Add($"{field} desc");
		}

		return this;
	}

	/// <summary>
	/// Sets the number of results to skip.
	/// </summary>
	/// <param name="count">Number of results to skip.</param>
	/// <returns>This builder for method chaining.</returns>
	public FluentODataQueryBuilder Skip(long count)
	{
		_skip = count;
		return this;
	}

	/// <summary>
	/// Sets the maximum number of results to return.
	/// </summary>
	/// <param name="count">Maximum number of results.</param>
	/// <returns>This builder for method chaining.</returns>
	public FluentODataQueryBuilder Top(long count)
	{
		_top = count;
		return this;
	}

	/// <summary>
	/// Requests the count of matching entities.
	/// </summary>
	/// <param name="includeCount">Whether to include the count.</param>
	/// <returns>This builder for method chaining.</returns>
	public FluentODataQueryBuilder Count(bool includeCount = true)
	{
		_count = includeCount;
		return this;
	}

	/// <summary>
	/// Adds a custom header to be sent with the request.
	/// </summary>
	/// <param name="name">The header name.</param>
	/// <param name="value">The header value.</param>
	/// <returns>This builder for method chaining.</returns>
	public FluentODataQueryBuilder WithHeader(string name, string value)
	{
		_customHeaders[name] = value;
		return this;
	}

	#endregion

	#region Execution Methods

	/// <summary>
	/// Executes the query and returns a single page of results as dictionaries.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>An ODataResponse containing the results as dictionaries.</returns>
	/// <remarks>
	/// <para><b>New API pattern (recommended):</b></para>
	/// <code>
	/// var query = client.For&lt;Product&gt;("Products").Top(10);
	/// var response = await client.GetAsync(query, cancellationToken);
	/// </code>
	/// </remarks>
	public Task<ODataResponse<Dictionary<string, object?>>> GetAsync(CancellationToken cancellationToken = default)
		=> _client.GetFluentAsync(this, cancellationToken);

	/// <summary>
	/// Executes the query and returns the raw JSON response as a JsonDocument.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A JsonDocument containing the raw OData response. The caller is responsible for disposing the document.</returns>
	/// <remarks>
	/// <para>This method returns the raw JSON response without any parsing into typed objects.</para>
	/// <para>The returned JsonDocument must be disposed by the caller.</para>
	/// <example>
	/// <code>
	/// using var json = await client.For("incidents").Top(10).GetJsonAsync(ct);
	/// foreach (var item in json.RootElement.GetProperty("value").EnumerateArray())
	/// {
	///     Console.WriteLine(item.GetProperty("title").GetString());
	/// }
	/// </code>
	/// </example>
	/// </remarks>
	public Task<JsonDocument> GetJsonAsync(CancellationToken cancellationToken = default)
		=> _client.GetFluentJsonAsync(this, cancellationToken);

	/// <summary>
	/// Executes the query and returns all pages of results as dictionaries.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>An ODataResponse containing all results as dictionaries.</returns>
	/// <remarks>
	/// <para><b>New API pattern (recommended):</b></para>
	/// <code>
	/// var query = client.For&lt;Product&gt;("Products").Filter("Price gt 100");
	/// var response = await client.GetAllAsync(query, cancellationToken);
	/// </code>
	/// </remarks>
	public Task<ODataResponse<Dictionary<string, object?>>> GetAllAsync(CancellationToken cancellationToken = default)
		=> _client.GetAllFluentAsync(this, cancellationToken);

	/// <summary>
	/// Executes the query and returns a single entry as a dictionary.
	/// Requires Key() to have been called.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The entry as a dictionary, or null if not found.</returns>
	/// <remarks>
	/// <para><b>New API pattern (recommended):</b></para>
	/// <code>
	/// var product = await client.GetByKeyAsync&lt;Product, int&gt;(123, cancellationToken: ct);
	/// </code>
	/// </remarks>
	public Task<Dictionary<string, object?>?> GetEntryAsync(CancellationToken cancellationToken = default)
		=> _client.GetEntryFluentAsync(BuildUrl(), CustomHeaders, cancellationToken);

	/// <summary>
	/// Executes the query and returns the first matching entry as a dictionary.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The first matching entry as a dictionary, or null if not found.</returns>
	/// <remarks>
	/// <para><b>New API pattern (recommended):</b></para>
	/// <code>
	/// var query = client.For&lt;Product&gt;("Products").Filter("Name eq 'Widget'");
	/// var product = await client.GetFirstOrDefaultAsync(query, cancellationToken);
	/// </code>
	/// </remarks>
	public async Task<Dictionary<string, object?>?> GetFirstOrDefaultAsync(CancellationToken cancellationToken = default)
	{
		Top(1);
		var response = await GetAsync(cancellationToken).ConfigureAwait(false);
		return response.Value.FirstOrDefault();
	}

	/// <summary>
	/// Deletes the entity identified by the key set on this query builder.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A task representing the delete operation.</returns>
	/// <remarks>
	/// <para><b>New API pattern (recommended):</b></para>
	/// <code>
	/// await client.DeleteAsync("Products", 123, cancellationToken);
	/// </code>
	/// </remarks>
	public Task DeleteAsync(CancellationToken cancellationToken = default)
		=> _client.DeleteByUrlAsync(BuildUrl(), CustomHeaders, cancellationToken);

	#endregion

	#region URL Building

	/// <summary>
	/// Builds the relative URL for this query.
	/// </summary>
	/// <returns>The OData query URL.</returns>
	public string BuildUrl()
	{
		var sb = new StringBuilder();
		sb.Append(_entitySet);

		// Append key
		if (_key is not null)
		{
			sb.Append('(');
			sb.Append(FormatKey(_key));
			sb.Append(')');
		}

		// Build query string
		var queryParams = new List<string>();

		if (_filterClauses.Count > 0)
		{
			var combinedFilter = string.Join(" and ", _filterClauses.Select(f => $"({f})"));
			queryParams.Add($"$filter={Uri.EscapeDataString(combinedFilter)}");
		}

		if (!string.IsNullOrWhiteSpace(_search))
		{
			queryParams.Add($"$search={Uri.EscapeDataString(_search)}");
		}

		if (_selectFields.Count > 0)
		{
			queryParams.Add($"$select={string.Join(",", _selectFields)}");
		}

		if (_expandFields.Count > 0)
		{
			queryParams.Add($"$expand={string.Join(",", _expandFields)}");
		}

		if (_orderByClauses.Count > 0)
		{
			queryParams.Add($"$orderby={string.Join(",", _orderByClauses)}");
		}

		if (_skip.HasValue)
		{
			queryParams.Add($"$skip={_skip.Value}");
		}

		if (_top.HasValue)
		{
			queryParams.Add($"$top={_top.Value}");
		}

		if (_count)
		{
			queryParams.Add("$count=true");
		}

		if (queryParams.Count > 0)
		{
			sb.Append('?');
			sb.Append(string.Join("&", queryParams));
		}

		return sb.ToString();
	}

	private static string FormatKey(object key) => key switch
	{
		int i => i.ToString(CultureInfo.InvariantCulture),
		long l => l.ToString(CultureInfo.InvariantCulture),
		Guid g => g.ToString(),
		string s => $"'{s.Replace("'", "''")}'",
		_ => key.ToString() ?? throw new ArgumentException("Invalid key value")
	};

	#endregion
}
