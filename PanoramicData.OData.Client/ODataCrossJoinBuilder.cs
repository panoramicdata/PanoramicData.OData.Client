namespace PanoramicData.OData.Client;

/// <summary>
/// Builds OData cross-join queries.
/// Cross-join allows combining multiple entity sets in a single query.
/// Example: /$crossjoin(Products,Categories)?$filter=Products/CategoryId eq Categories/Id
/// </summary>
public class ODataCrossJoinBuilder
{
	private readonly List<string> _entitySets;
	private readonly ILogger _logger;
	private readonly List<string> _filterClauses = [];
	private readonly List<string> _selectFields = [];
	private readonly List<string> _expandFields = [];
	private readonly List<string> _orderByClauses = [];
	private readonly Dictionary<string, string> _customHeaders = [];
	private long? _skip;
	private long? _top;
	private bool _count;

	/// <summary>
	/// Creates a new cross-join query builder.
	/// </summary>
	/// <param name="entitySets">The entity sets to join (minimum 2).</param>
	/// <param name="logger">The logger instance.</param>
	/// <exception cref="ArgumentException">Thrown when less than 2 entity sets are provided.</exception>
	public ODataCrossJoinBuilder(IEnumerable<string> entitySets, ILogger logger)
	{
		_entitySets = [.. entitySets];
		_logger = logger;

		if (_entitySets.Count < 2)
		{
			throw new ArgumentException("Cross-join requires at least two entity sets", nameof(entitySets));
		}
	}

	/// <summary>
	/// Adds a filter clause.
	/// Use qualified property names like "Products/Name" or "Categories/Id".
	/// </summary>
	/// <param name="filter">The OData filter expression.</param>
	/// <returns>This builder.</returns>
	public ODataCrossJoinBuilder Filter(string filter)
	{
		if (!string.IsNullOrWhiteSpace(filter))
		{
			_filterClauses.Add(filter);
		}

		return this;
	}

	/// <summary>
	/// Adds select fields.
	/// Use qualified property names like "Products/Name,Categories/Name".
	/// </summary>
	/// <param name="fields">Comma-separated field names.</param>
	/// <returns>This builder.</returns>
	public ODataCrossJoinBuilder Select(string fields)
	{
		if (!string.IsNullOrWhiteSpace(fields))
		{
			_selectFields.AddRange(fields.Split(',').Select(f => f.Trim()));
		}

		return this;
	}

	/// <summary>
	/// Adds expand fields.
	/// </summary>
	/// <param name="fields">Comma-separated expand expressions.</param>
	/// <returns>This builder.</returns>
	public ODataCrossJoinBuilder Expand(string fields)
	{
		if (!string.IsNullOrWhiteSpace(fields))
		{
			_expandFields.AddRange(fields.Split(',').Select(f => f.Trim()));
		}

		return this;
	}

	/// <summary>
	/// Adds an order by clause.
	/// Use qualified property names like "Products/Name desc".
	/// </summary>
	/// <param name="orderBy">The order by expression.</param>
	/// <returns>This builder.</returns>
	public ODataCrossJoinBuilder OrderBy(string orderBy)
	{
		if (!string.IsNullOrWhiteSpace(orderBy))
		{
			_orderByClauses.Add(orderBy);
		}

		return this;
	}

	/// <summary>
	/// Sets the number of results to skip.
	/// </summary>
	/// <param name="count">The number to skip.</param>
	/// <returns>This builder.</returns>
	public ODataCrossJoinBuilder Skip(long count)
	{
		_skip = count;
		return this;
	}

	/// <summary>
	/// Sets the maximum number of results to return.
	/// </summary>
	/// <param name="count">The maximum count.</param>
	/// <returns>This builder.</returns>
	public ODataCrossJoinBuilder Top(long count)
	{
		_top = count;
		return this;
	}

	/// <summary>
	/// Requests the count of matching results.
	/// </summary>
	/// <param name="includeCount">Whether to include count.</param>
	/// <returns>This builder.</returns>
	public ODataCrossJoinBuilder Count(bool includeCount = true)
	{
		_count = includeCount;
		return this;
	}

	/// <summary>
	/// Adds a custom header to be sent with the request.
	/// </summary>
	/// <param name="name">Header name.</param>
	/// <param name="value">Header value.</param>
	/// <returns>This builder.</returns>
	public ODataCrossJoinBuilder WithHeader(string name, string value)
	{
		_customHeaders[name] = value;
		return this;
	}

	/// <summary>
	/// Gets the custom headers configured for this query.
	/// </summary>
	public IReadOnlyDictionary<string, string> CustomHeaders => _customHeaders;

	/// <summary>
	/// Builds the relative URL for this cross-join query.
	/// </summary>
	/// <returns>The URL string.</returns>
	public string BuildUrl()
	{
		var sb = new StringBuilder();

		sb.Append("$crossjoin(");
		sb.Append(string.Join(",", _entitySets));
		sb.Append(')');

		LoggerMessages.CrossJoinBuilderBuildUrl(_logger, string.Join(",", _entitySets));

		var queryParams = BuildQueryParameters();

		if (queryParams.Count > 0)
		{
			sb.Append('?');
			sb.Append(string.Join("&", queryParams));
		}

		var url = sb.ToString();
		LoggerMessages.CrossJoinBuilderFinalUrl(_logger, url);

		return url;
	}

	private List<string> BuildQueryParameters()
	{
		var queryParams = new List<string>();

		AppendFilterParameter(queryParams);
		AppendSelectParameter(queryParams);
		AppendExpandParameter(queryParams);
		AppendOrderByParameter(queryParams);
		AppendPaginationParameters(queryParams);

		return queryParams;
	}

	private void AppendFilterParameter(List<string> queryParams)
	{
		if (_filterClauses.Count > 0)
		{
			var combinedFilter = string.Join(" and ", _filterClauses.Select(f => $"({f})"));
			queryParams.Add($"$filter={Uri.EscapeDataString(combinedFilter)}");
		}
	}

	private void AppendSelectParameter(List<string> queryParams)
	{
		if (_selectFields.Count > 0)
		{
			queryParams.Add($"$select={string.Join(",", _selectFields)}");
		}
	}

	private void AppendExpandParameter(List<string> queryParams)
	{
		if (_expandFields.Count > 0)
		{
			queryParams.Add($"$expand={string.Join(",", _expandFields)}");
		}
	}

	private void AppendOrderByParameter(List<string> queryParams)
	{
		if (_orderByClauses.Count > 0)
		{
			queryParams.Add($"$orderby={string.Join(",", _orderByClauses)}");
		}
	}

	private void AppendPaginationParameters(List<string> queryParams)
	{
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
	}
}
