namespace PanoramicData.OData.Client;

/// <summary>
/// Builds OData query URLs from LINQ-like expressions.
/// </summary>
/// <typeparam name="T">The entity type being queried.</typeparam>
public partial class ODataQueryBuilder<T> where T : class
{
	private readonly string _entitySet;
	private readonly ILogger _logger;
	private readonly ODataClient? _client;
	private readonly List<string> _filterClauses = [];
	private readonly List<string> _orderByClauses = [];
	private readonly List<string> _selectFields = [];
	private readonly List<string> _expandFields = [];
	private readonly List<string> _computeExpressions = [];
	private readonly Dictionary<string, string> _customHeaders = [];
	private long? _skip;
	private long? _top;
	private bool _count;
	private object? _key;
	private string? _function;
	private object? _functionParameters;
	private string? _apply;
	private string? _search;
	private string? _derivedType;

	/// <summary>
	/// Initializes a new instance of the <see cref="ODataQueryBuilder{T}"/> class.
	/// </summary>
	/// <param name="entitySet">The entity set name.</param>
	/// <param name="logger">The logger instance.</param>
	public ODataQueryBuilder(string entitySet, ILogger logger)
	{
		_entitySet = entitySet;
		_logger = logger;
		_client = null;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ODataQueryBuilder{T}"/> class with a client reference for fluent execution.
	/// </summary>
	/// <param name="client">The OData client instance.</param>
	/// <param name="entitySet">The entity set name.</param>
	/// <param name="logger">The logger instance.</param>
	internal ODataQueryBuilder(ODataClient client, string entitySet, ILogger logger)
	{
		_client = client;
		_entitySet = entitySet;
		_logger = logger;
	}

	#region Fluent Execution Methods

	/// <summary>
	/// Executes the query and returns a single page of results.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>An ODataResponse containing the results.</returns>
	/// <exception cref="InvalidOperationException">Thrown if this query builder was not created from an ODataClient.</exception>
	public Task<ODataResponse<T>> GetAsync(CancellationToken cancellationToken = default)
	{
		EnsureClientAvailable();
		return _client!.GetAsync(this, cancellationToken);
	}

	/// <summary>
	/// Executes the query and returns all pages of results, following pagination.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>An ODataResponse containing all results.</returns>
	/// <exception cref="InvalidOperationException">Thrown if this query builder was not created from an ODataClient.</exception>
	public Task<ODataResponse<T>> GetAllAsync(CancellationToken cancellationToken = default)
	{
		EnsureClientAvailable();
		return _client!.GetAllAsync(this, cancellationToken);
	}

	/// <summary>
	/// Gets the first entity matching the query, or null if no match.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The first matching entity, or null.</returns>
	/// <exception cref="InvalidOperationException">Thrown if this query builder was not created from an ODataClient.</exception>
	public Task<T?> GetFirstOrDefaultAsync(CancellationToken cancellationToken = default)
	{
		EnsureClientAvailable();
		return _client!.GetFirstOrDefaultAsync(this, cancellationToken);
	}

	/// <summary>
	/// Gets a single entity matching the query. Throws if zero or more than one match.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The single matching entity.</returns>
	/// <exception cref="InvalidOperationException">Thrown if this query builder was not created from an ODataClient, or if zero or more than one entity matches.</exception>
	public Task<T> GetSingleAsync(CancellationToken cancellationToken = default)
	{
		EnsureClientAvailable();
		return _client!.GetSingleAsync(this, cancellationToken);
	}

	/// <summary>
	/// Gets a single entity matching the query, or null if no match. Throws if more than one match.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The single matching entity, or null.</returns>
	/// <exception cref="InvalidOperationException">Thrown if this query builder was not created from an ODataClient, or if more than one entity matches.</exception>
	public Task<T?> GetSingleOrDefaultAsync(CancellationToken cancellationToken = default)
	{
		EnsureClientAvailable();
		return _client!.GetSingleOrDefaultAsync(this, cancellationToken);
	}

	/// <summary>
	/// Gets only the count of entities matching the query, without retrieving the entities.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The count of matching entities.</returns>
	/// <exception cref="InvalidOperationException">Thrown if this query builder was not created from an ODataClient.</exception>
	public Task<long> GetCountAsync(CancellationToken cancellationToken = default)
	{
		EnsureClientAvailable();
		return _client!.GetCountAsync(this, cancellationToken);
	}

	private void EnsureClientAvailable()
	{
		if (_client is null)
		{
			throw new InvalidOperationException(
				"This query builder was not created from an ODataClient instance. " +
				"Use client.For<T>() to create a query builder that supports direct execution, " +
				"or pass this query to the client's GetAsync/GetAllAsync methods.");
		}
	}

	#endregion

	/// <summary>
	/// Sets the key for a single entity query.
	/// </summary>
	public ODataQueryBuilder<T> Key<TKey>(TKey key)
	{
		_key = key;
		return this;
	}

	/// <summary>
	/// Filters to only entities of a derived type.
	/// OData V4 supports querying derived types via type casting.
	/// Example: GET People/Microsoft.OData.SampleService.Models.TripPin.Employee
	/// </summary>
	/// <typeparam name="TDerived">The derived type to filter to.</typeparam>
	/// <param name="fullTypeName">The full namespace-qualified type name. If null, uses the type name without namespace.</param>
	/// <returns>A new query builder for the derived type.</returns>
	public ODataQueryBuilder<TDerived> OfType<TDerived>(string? fullTypeName = null) where TDerived : class, T
	{
		var typeName = fullTypeName ?? typeof(TDerived).Name;

		// Create a new query builder with the derived type
		var derivedBuilder = _client is not null
			? new ODataQueryBuilder<TDerived>(_client, _entitySet, _logger) { _derivedType = typeName }
			: new ODataQueryBuilder<TDerived>(_entitySet, _logger) { _derivedType = typeName };

		// Copy over existing query options
		derivedBuilder._filterClauses.AddRange(_filterClauses);
		derivedBuilder._orderByClauses.AddRange(_orderByClauses);
		derivedBuilder._selectFields.AddRange(_selectFields);
		derivedBuilder._expandFields.AddRange(_expandFields);
		derivedBuilder._computeExpressions.AddRange(_computeExpressions);
		foreach (var header in _customHeaders)
		{
			derivedBuilder._customHeaders[header.Key] = header.Value;
		}

		derivedBuilder._skip = _skip;
		derivedBuilder._top = _top;
		derivedBuilder._count = _count;
		derivedBuilder._apply = _apply;
		derivedBuilder._search = _search;

		return derivedBuilder;
	}

	/// <summary>
	/// Sets the derived type for this query without changing the result type.
	/// Use this when you want to filter to a derived type but keep the same result type.
	/// </summary>
	/// <param name="fullTypeName">The full namespace-qualified type name.</param>
	/// <returns>This query builder.</returns>
	public ODataQueryBuilder<T> Cast(string fullTypeName)
	{
		_derivedType = fullTypeName;
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
	/// Supports nested expands via expressions like p => new { p.Parent, p.Parent!.Children }
	/// which produces $expand=Parent($expand=Children).
	/// </summary>
	public ODataQueryBuilder<T> Expand(Expression<Func<T, object?>> selector)
	{
		var memberPaths = GetExpandMemberPaths(selector);
		var nestedExpands = BuildNestedExpandFields(memberPaths);
		_expandFields.AddRange(nestedExpands);
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
	/// Expands a navigation property and selects specific scalar properties within it.
	/// Produces $expand=NavProperty($select=Prop1,Prop2,Prop3).
	/// </summary>
	/// <typeparam name="TNav">The type of the navigation property.</typeparam>
	/// <param name="navigationProperty">Expression selecting the navigation property to expand.</param>
	/// <param name="selectProperties">Expression selecting the scalar properties to include from the expanded entity.</param>
	/// <returns>This query builder for method chaining.</returns>
	/// <example>
	/// <code>
	/// // Produces: $expand=ReportSchedule($select=Id,Name,Description)
	/// query.ExpandWithSelect(
	///     x => x.ReportSchedule,
	///     rs => new { rs.Id, rs.Name, rs.Description });
	/// </code>
	/// </example>
	public ODataQueryBuilder<T> ExpandWithSelect<TNav>(
		Expression<Func<T, TNav?>> navigationProperty,
		Expression<Func<TNav, object?>> selectProperties) where TNav : class
	{
		var navPropertyName = GetNavigationPropertyName(navigationProperty);
		var selectedFields = GetSelectFieldNames(selectProperties);

		if (string.IsNullOrEmpty(navPropertyName) || selectedFields.Count == 0)
		{
			return this;
		}

		var expandClause = $"{navPropertyName}($select={string.Join(",", selectedFields)})";
		_expandFields.Add(expandClause);

		return this;
	}

	/// <summary>
	/// Expands a navigation property with nested expand and/or select options.
	/// </summary>
	/// <typeparam name="TNav">The type of the navigation property.</typeparam>
	/// <param name="navigationProperty">Expression selecting the navigation property to expand.</param>
	/// <param name="configure">Action to configure nested expand options.</param>
	/// <returns>This query builder for method chaining.</returns>
	/// <example>
	/// <code>
	/// // Produces: $expand=ReportSchedule($select=Id,Name;$expand=Owner)
	/// query.Expand(
	///     x => x.ReportSchedule,
	///     nested => nested
	///         .Select(rs => new { rs.Id, rs.Name })
	///         .Expand(rs => rs.Owner));
	/// </code>
	/// </example>
	public ODataQueryBuilder<T> Expand<TNav>(
		Expression<Func<T, TNav?>> navigationProperty,
		Action<NestedExpandBuilder<TNav>> configure) where TNav : class
	{
		var navPropertyName = GetNavigationPropertyName(navigationProperty);
		if (string.IsNullOrEmpty(navPropertyName))
		{
			return this;
		}

		var nestedBuilder = new NestedExpandBuilder<TNav>();
		configure(nestedBuilder);

		var nestedOptions = nestedBuilder.Build();
		var expandClause = string.IsNullOrEmpty(nestedOptions)
			? navPropertyName
			: $"{navPropertyName}({nestedOptions})";

		_expandFields.Add(expandClause);

		return this;
	}

	/// <summary>
	/// Expands a collection navigation property with nested expand and/or select options.
	/// </summary>
	/// <typeparam name="TNav">The element type of the collection navigation property.</typeparam>
	/// <param name="navigationProperty">Expression selecting the collection navigation property to expand.</param>
	/// <param name="configure">Action to configure nested expand options.</param>
	/// <returns>This query builder for method chaining.</returns>
	public ODataQueryBuilder<T> Expand<TNav>(
		Expression<Func<T, IEnumerable<TNav>?>> navigationProperty,
		Action<NestedExpandBuilder<TNav>> configure) where TNav : class
	{
		var navPropertyName = GetCollectionNavigationPropertyName(navigationProperty);
		if (string.IsNullOrEmpty(navPropertyName))
		{
			return this;
		}

		var nestedBuilder = new NestedExpandBuilder<TNav>();
		configure(nestedBuilder);

		var nestedOptions = nestedBuilder.Build();
		var expandClause = string.IsNullOrEmpty(nestedOptions)
			? navPropertyName
			: $"{navPropertyName}({nestedOptions})";

		_expandFields.Add(expandClause);

		return this;
	}

	private static string GetNavigationPropertyName<TNav>(Expression<Func<T, TNav?>> selector)
	{
		var body = selector.Body;

		// Handle null-forgiving operator (!.)
		if (body is UnaryExpression unary && unary.NodeType == ExpressionType.Convert)
		{
			body = unary.Operand;
		}

		if (body is MemberExpression member)
		{
			return member.Member.Name;
		}

		return string.Empty;
	}

	private static string GetCollectionNavigationPropertyName<TNav>(Expression<Func<T, IEnumerable<TNav>?>> selector)
	{
		var body = selector.Body;

		// Handle null-forgiving operator (!.)
		if (body is UnaryExpression unary && unary.NodeType == ExpressionType.Convert)
		{
			body = unary.Operand;
		}

		if (body is MemberExpression member)
		{
			return member.Member.Name;
		}

		return string.Empty;
	}

	private static List<string> GetSelectFieldNames<TEntity>(Expression<Func<TEntity, object?>> selector)
	{
		var results = new List<string>();

		var body = selector.Body;

		// Handle Convert expressions
		if (body is UnaryExpression unary && unary.NodeType == ExpressionType.Convert)
		{
			body = unary.Operand;
		}

		if (body is NewExpression newExpr)
		{
			foreach (var arg in newExpr.Arguments)
			{
				var memberName = GetDirectMemberName(arg);
				if (!string.IsNullOrEmpty(memberName) && !results.Contains(memberName))
				{
					results.Add(memberName);
				}
			}
		}
		else if (body is MemberExpression member)
		{
			results.Add(member.Member.Name);
		}

		return results;
	}

	private static string GetDirectMemberName(Expression expression)
	{
		if (expression is UnaryExpression unary && unary.NodeType == ExpressionType.Convert)
		{
			expression = unary.Operand;
		}

		if (expression is MemberExpression member)
		{
			return member.Member.Name;
		}

		return string.Empty;
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
	/// Adds a $compute expression for computed properties.
	/// OData V4.01 feature that allows defining computed properties in the query.
	/// </summary>
	/// <param name="computeExpression">The compute expression (e.g., "Price mul Quantity as Total").</param>
	public ODataQueryBuilder<T> Compute(string computeExpression)
	{
		if (!string.IsNullOrWhiteSpace(computeExpression))
		{
			_computeExpressions.Add(computeExpression);
		}

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
		LoggerMessages.QueryBuilderBuildUrl(_logger, typeof(T).Name, _entitySet);

		var sb = new StringBuilder();
		sb.Append(_entitySet);

		AppendDerivedTypeToUrl(sb);
		AppendKeyToUrl(sb);
		AppendFunctionToUrl(sb);
		AppendQueryString(sb);

		var url = sb.ToString();
		LoggerMessages.QueryBuilderFinalUrl(_logger, typeof(T).Name, url);

		return url;
	}

	private void AppendDerivedTypeToUrl(StringBuilder sb)
	{
		if (string.IsNullOrEmpty(_derivedType))
		{
			return;
		}

		sb.Append('/');
		sb.Append(_derivedType);
		LoggerMessages.QueryBuilderDerivedType(_logger, _derivedType);
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
		LoggerMessages.QueryBuilderKey(_logger, _key);
	}

	private void AppendFunctionToUrl(StringBuilder sb)
	{
		if (string.IsNullOrEmpty(_function))
		{
			return;
		}

		sb.Append('/');
		sb.Append(_function);
		LoggerMessages.QueryBuilderFunction(_logger, _function);

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
		AppendComputeParameter(queryParams);

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
		LoggerMessages.QueryBuilderFilter(_logger, combinedFilter);
	}

	private void AppendSearchParameter(List<string> queryParams)
	{
		if (string.IsNullOrWhiteSpace(_search))
		{
			return;
		}

		queryParams.Add($"$search={Uri.EscapeDataString(_search)}");
		LoggerMessages.QueryBuilderSearch(_logger, _search);
	}

	private void AppendSelectParameter(List<string> queryParams)
	{
		if (_selectFields.Count <= 0)
		{
			return;
		}

		var selectClause = string.Join(",", _selectFields);
		queryParams.Add($"$select={selectClause}");
		LoggerMessages.QueryBuilderSelect(_logger, selectClause);
	}

	private void AppendExpandParameter(List<string> queryParams)
	{
		if (_expandFields.Count <= 0)
		{
			return;
		}

		var expandClause = string.Join(",", _expandFields);
		queryParams.Add($"$expand={expandClause}");
		LoggerMessages.QueryBuilderExpand(_logger, expandClause);
	}

	private void AppendOrderByParameter(List<string> queryParams)
	{
		if (_orderByClauses.Count <= 0)
		{
			return;
		}

		var orderByClause = string.Join(",", _orderByClauses);
		queryParams.Add($"$orderby={orderByClause}");
		LoggerMessages.QueryBuilderOrderBy(_logger, orderByClause);
	}

	private void AppendSkipParameter(List<string> queryParams)
	{
		if (!_skip.HasValue)
		{
			return;
		}

		queryParams.Add($"$skip={_skip.Value}");
		LoggerMessages.QueryBuilderSkip(_logger, _skip.Value);
	}

	private void AppendTopParameter(List<string> queryParams)
	{
		if (!_top.HasValue)
		{
			return;
		}

		queryParams.Add($"$top={_top.Value}");
		LoggerMessages.QueryBuilderTop(_logger, _top.Value);
	}

	private void AppendCountParameter(List<string> queryParams)
	{
		if (!_count)
		{
			return;
		}

		queryParams.Add("$count=true");
		LoggerMessages.QueryBuilderCount(_logger);
	}

	private void AppendApplyParameter(List<string> queryParams)
	{
		if (string.IsNullOrWhiteSpace(_apply))
		{
			return;
		}

		queryParams.Add($"$apply={Uri.EscapeDataString(_apply)}");
		LoggerMessages.QueryBuilderApply(_logger, _apply);
	}

	private void AppendComputeParameter(List<string> queryParams)
	{
		if (_computeExpressions.Count <= 0)
		{
			return;
		}

		var computeClause = string.Join(",", _computeExpressions);
		queryParams.Add($"$compute={Uri.EscapeDataString(computeClause)}");
		LoggerMessages.QueryBuilderCompute(_logger, computeClause);
	}
}
