using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using System.Text;

namespace PanoramicData.OData.Client;

/// <summary>
/// Builds OData query URLs from LINQ-like expressions.
/// </summary>
/// <typeparam name="T">The entity type being queried.</typeparam>
public partial class ODataQueryBuilder<T>(string entitySet, ILogger logger) where T : class
{
	private readonly List<string> _filterClauses = [];
	private readonly List<string> _orderByClauses = [];
	private readonly List<string> _selectFields = [];
	private readonly List<string> _expandFields = [];
	private readonly Dictionary<string, string> _customHeaders = [];
	private long? _skip;
	private long? _top;
	private bool _count;
	private object? _key;
	private string? _function;
	private object? _functionParameters;
	private string? _apply;
	private string? _search;

	/// <summary>
	/// Sets the key for a single entity query.
	/// </summary>
	public ODataQueryBuilder<T> Key<TKey>(TKey key)
	{
		_key = key;
		return this;
	}

	/// <summary>
	/// Adds a filter expression.
	/// </summary>
	public ODataQueryBuilder<T> Filter(Expression<Func<T, bool>> predicate)
	{
		var filterString = ExpressionToODataFilter(predicate.Body);
		if (!string.IsNullOrWhiteSpace(filterString))
		{
			_filterClauses.Add(filterString);
		}

		return this;
	}

	/// <summary>
	/// Adds a raw filter string.
	/// </summary>
	public ODataQueryBuilder<T> Filter(string rawFilter)
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
	public ODataQueryBuilder<T> Search(string searchTerm)
	{
		_search = searchTerm;
		return this;
	}

	/// <summary>
	/// Adds select fields from an expression.
	/// </summary>
	public ODataQueryBuilder<T> Select(Expression<Func<T, object?>> selector)
	{
		var memberNames = GetMemberNames(selector);
		_selectFields.AddRange(memberNames);
		return this;
	}

	/// <summary>
	/// Adds select fields from a comma-separated string.
	/// </summary>
	public ODataQueryBuilder<T> Select(string fields)
	{
		if (!string.IsNullOrWhiteSpace(fields))
		{
			_selectFields.AddRange(fields.Split(',').Select(f => f.Trim()));
		}

		return this;
	}

	/// <summary>
	/// Adds expand fields from an expression.
	/// </summary>
	public ODataQueryBuilder<T> Expand(Expression<Func<T, object?>> selector)
	{
		var memberNames = GetMemberNames(selector);
		_expandFields.AddRange(memberNames);
		return this;
	}

	/// <summary>
	/// Adds expand fields from a comma-separated string.
	/// </summary>
	public ODataQueryBuilder<T> Expand(string fields)
	{
		if (!string.IsNullOrWhiteSpace(fields))
		{
			_expandFields.AddRange(fields.Split(',').Select(f => f.Trim()));
		}

		return this;
	}

	/// <summary>
	/// Adds an order by clause.
	/// </summary>
	public ODataQueryBuilder<T> OrderBy(Expression<Func<T, object?>> selector, bool descending = false)
	{
		var memberName = GetMemberName(selector);
		_orderByClauses.Add(descending ? $"{memberName} desc" : memberName);
		return this;
	}

	/// <summary>
	/// Adds order by clauses from key-value pairs (key = property name, value = true for descending).
	/// </summary>
	public ODataQueryBuilder<T> OrderBy(IEnumerable<KeyValuePair<string, bool>> orderBys)
	{
		_orderByClauses.AddRange(orderBys.Select(ob => ob.Value ? $"{ob.Key} desc" : ob.Key));
		return this;
	}

	/// <summary>
	/// Adds a raw order by string.
	/// </summary>
	public ODataQueryBuilder<T> OrderBy(string orderBy)
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
	public ODataQueryBuilder<T> Skip(long count)
	{
		_skip = count;
		return this;
	}

	/// <summary>
	/// Sets the maximum number of results to return.
	/// </summary>
	public ODataQueryBuilder<T> Top(long count)
	{
		_top = count;
		return this;
	}

	/// <summary>
	/// Requests the count of matching entities.
	/// </summary>
	public ODataQueryBuilder<T> Count(bool includeCount = true)
	{
		_count = includeCount;
		return this;
	}

	/// <summary>
	/// Sets a function to call on the entity set.
	/// </summary>
	public ODataQueryBuilder<T> Function(string functionName, object? parameters = null)
	{
		_function = functionName;
		_functionParameters = parameters;
		return this;
	}

	/// <summary>
	/// Sets a raw $apply clause for aggregations.
	/// </summary>
	public ODataQueryBuilder<T> Apply(string applyClause)
	{
		_apply = applyClause;
		return this;
	}

	/// <summary>
	/// Adds a custom header to be sent with the request.
	/// </summary>
	public ODataQueryBuilder<T> WithHeader(string name, string value)
	{
		_customHeaders[name] = value;
		return this;
	}

	/// <summary>
	/// Gets the custom headers configured for this query.
	/// </summary>
	public IReadOnlyDictionary<string, string> CustomHeaders => _customHeaders;

	/// <summary>
	/// Builds the relative URL for this query.
	/// </summary>
	public string BuildUrl()
	{
		logger.LogDebug("ODataQueryBuilder<{Type}>.BuildUrl() - EntitySet: '{EntitySet}'", typeof(T).Name, entitySet);

		var sb = new StringBuilder();
		sb.Append(entitySet);

		AppendKeyToUrl(sb);
		AppendFunctionToUrl(sb);
		AppendQueryString(sb);

		var url = sb.ToString();
		logger.LogDebug("ODataQueryBuilder<{Type}>.BuildUrl() - Final URL: {Url}", typeof(T).Name, url);

		return url;
	}

	private void AppendKeyToUrl(StringBuilder sb)
	{
		if (_key is null)
		{
			return;
		}

		sb.Append('(');
		sb.Append(FormatKey(_key));
		sb.Append(')');
		logger.LogDebug("ODataQueryBuilder - Key: {Key}", _key);
	}

	private void AppendFunctionToUrl(StringBuilder sb)
	{
		if (string.IsNullOrEmpty(_function))
		{
			return;
		}

		sb.Append('/');
		sb.Append(_function);
		logger.LogDebug("ODataQueryBuilder - Function: {Function}", _function);

		sb.Append('(');
		if (_functionParameters is not null)
		{
			sb.Append(FormatFunctionParameters(_functionParameters));
		}

		sb.Append(')');
	}

	private void AppendQueryString(StringBuilder sb)
	{
		var queryParams = BuildQueryParameters();

		if (queryParams.Count > 0)
		{
			sb.Append('?');
			sb.Append(string.Join("&", queryParams));
		}
	}

	private List<string> BuildQueryParameters()
	{
		var queryParams = new List<string>();

		AppendFilterParameter(queryParams);
		AppendSearchParameter(queryParams);
		AppendSelectParameter(queryParams);
		AppendExpandParameter(queryParams);
		AppendOrderByParameter(queryParams);
		AppendSkipParameter(queryParams);
		AppendTopParameter(queryParams);
		AppendCountParameter(queryParams);
		AppendApplyParameter(queryParams);

		return queryParams;
	}

	private void AppendFilterParameter(List<string> queryParams)
	{
		if (_filterClauses.Count <= 0)
		{
			return;
		}

		var combinedFilter = string.Join(" and ", _filterClauses.Select(f => $"({f})"));
		queryParams.Add($"$filter={Uri.EscapeDataString(combinedFilter)}");
		logger.LogDebug("ODataQueryBuilder - Filter: {Filter}", combinedFilter);
	}

	private void AppendSearchParameter(List<string> queryParams)
	{
		if (string.IsNullOrWhiteSpace(_search))
		{
			return;
		}

		queryParams.Add($"$search={Uri.EscapeDataString(_search)}");
		logger.LogDebug("ODataQueryBuilder - Search: {Search}", _search);
	}

	private void AppendSelectParameter(List<string> queryParams)
	{
		if (_selectFields.Count <= 0)
		{
			return;
		}

		var selectClause = string.Join(",", _selectFields);
		queryParams.Add($"$select={selectClause}");
		logger.LogDebug("ODataQueryBuilder - Select: {Select}", selectClause);
	}

	private void AppendExpandParameter(List<string> queryParams)
	{
		if (_expandFields.Count <= 0)
		{
			return;
		}

		var expandClause = string.Join(",", _expandFields);
		queryParams.Add($"$expand={expandClause}");
		logger.LogDebug("ODataQueryBuilder - Expand: {Expand}", expandClause);
	}

	private void AppendOrderByParameter(List<string> queryParams)
	{
		if (_orderByClauses.Count <= 0)
		{
			return;
		}

		var orderByClause = string.Join(",", _orderByClauses);
		queryParams.Add($"$orderby={orderByClause}");
		logger.LogDebug("ODataQueryBuilder - OrderBy: {OrderBy}", orderByClause);
	}

	private void AppendSkipParameter(List<string> queryParams)
	{
		if (!_skip.HasValue)
		{
			return;
		}

		queryParams.Add($"$skip={_skip.Value}");
		logger.LogDebug("ODataQueryBuilder - Skip: {Skip}", _skip.Value);
	}

	private void AppendTopParameter(List<string> queryParams)
	{
		if (!_top.HasValue)
		{
			return;
		}

		queryParams.Add($"$top={_top.Value}");
		logger.LogDebug("ODataQueryBuilder - Top: {Top}", _top.Value);
	}

	private void AppendCountParameter(List<string> queryParams)
	{
		if (!_count)
		{
			return;
		}

		queryParams.Add("$count=true");
		logger.LogDebug("ODataQueryBuilder - Count: true");
	}

	private void AppendApplyParameter(List<string> queryParams)
	{
		if (string.IsNullOrWhiteSpace(_apply))
		{
			return;
		}

		queryParams.Add($"$apply={Uri.EscapeDataString(_apply)}");
		logger.LogDebug("ODataQueryBuilder - Apply: {Apply}", _apply);
	}
}
