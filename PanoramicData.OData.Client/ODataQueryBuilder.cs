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
	private readonly List<string> _rawQueryOptions = [];

	/// <summary>Gets a value indicating whether a key has been set on this query.</summary>
	internal bool HasKey => _key is not null;

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

	/// <summary>
	/// Sets the key for a single entity query.
	/// </summary>
	public ODataQueryBuilder<T> Key<TKey>(TKey keyValue)
	{
		_key = keyValue;
		return this;
	}

	/// <summary>
	/// Navigates to a related collection or singleton via a key and a navigation property name.
	/// Produces a URL segment of the form <c>EntitySet(key)/NavigationProperty</c>.
	/// </summary>
	/// <typeparam name="TNav">The type of the navigation target entity.</typeparam>
	/// <param name="navigationProperty">The name of the navigation property.</param>
	/// <returns>A new <see cref="ODataQueryBuilder{TNav}"/> rooted at the navigation path.</returns>
	/// <exception cref="InvalidOperationException">Thrown if no key has been set on this builder.</exception>
	public ODataQueryBuilder<TNav> NavigateTo<TNav>(string navigationProperty) where TNav : class
	{
		if (_key is null)
		{
			throw new InvalidOperationException(
				"NavigateTo requires a key to be set. Call Key() before NavigateTo().");
		}

		var path = $"{_entitySet}({ODataLiteral.FormatKey(_key)})/{navigationProperty}";
		LoggerMessages.QueryBuilderNavigateTo(_logger, typeof(T).Name, path);

		return _client is null
			? new ODataQueryBuilder<TNav>(path, _logger)
			: new ODataQueryBuilder<TNav>(_client, path, _logger);
	}

	/// <summary>
	/// Navigates to a related entity using an expression that selects any navigation property,
	/// returning an untyped builder. Use <see cref="FluentODataQueryBuilder.As{TResult}"/> to re-type
	/// the result for deserialization.
	/// Produces a URL segment of the form <c>EntitySet(key)/NavigationProperty</c>.
	/// </summary>
	/// <param name="navigationProperty">Expression selecting the navigation property (typed or collection).</param>
	/// <returns>A <see cref="FluentODataQueryBuilder"/> rooted at the navigation path.</returns>
	/// <exception cref="InvalidOperationException">Thrown if no key has been set on this builder.</exception>
	/// <example>
	/// <code>
	/// var permissions = await client.For&lt;Mailbox&gt;()
	///     .Key(identity)
	///     .NavigateTo(x =&gt; x.MailboxPermissions)
	///     .As&lt;MailboxPermission&gt;()
	///     .GetAsync(ct);
	/// </code>
	/// </example>
	public FluentODataQueryBuilder NavigateTo(Expression<Func<T, object?>> navigationProperty)
	{
		if (_key is null)
		{
			throw new InvalidOperationException(
				"NavigateTo requires a key to be set. Call Key() before NavigateTo().");
		}

		var propName = GetMemberName(navigationProperty);
		var path = $"{_entitySet}({ODataLiteral.FormatKey(_key)})/{propName}";
		LoggerMessages.QueryBuilderNavigateTo(_logger, typeof(T).Name, path);

		return _client is null
			? throw new InvalidOperationException(
				"NavigateTo() without a type parameter requires a client. Use client.For<T>() to create a query builder.")
			: new FluentODataQueryBuilder(_client, path, _logger);
	}

	/// <summary>
	/// Filters to only entities of a derived type.
	/// OData V4 supports querying derived types via type casting.
	/// Example: GET People/Microsoft.OData.SampleService.Models.TripPin.Employee
	/// </summary>
	/// <typeparam name="TDerived">The derived type to filter to.</typeparam>
	/// <param name="fullTypeName">The full namespace-qualified type name. If null, uses the type name without namespace.</param>
	/// <returns>A new query builder for the derived type.</returns>
	public ODataQueryBuilder<TDerived> OfType<TDerived>([Optional] string? fullTypeName) where TDerived : class, T
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
		var filterString = ODataExpressionTranslator.ExpressionToODataFilter(predicate.Body);
		ODataListText.AddIfNotBlank(_filterClauses, filterString);

		return this;
	}

	/// <summary>
	/// Adds a raw filter string.
	/// </summary>
	public ODataQueryBuilder<T> Filter(string rawFilter)
	{
		ODataListText.AddIfNotBlank(_filterClauses, rawFilter);

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
	/// Adds an order by clause.
	/// </summary>
	public ODataQueryBuilder<T> OrderBy(Expression<Func<T, object?>> selector, [Optional] bool descending)
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
	public ODataQueryBuilder<T> OrderBy(string orderByExpression)
	{
		ODataListText.AddIfNotBlank(_orderByClauses, orderByExpression);

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
	public ODataQueryBuilder<T> Count([Optional, DefaultParameterValue(true)] bool includeCount)
	{
		_count = includeCount;
		return this;
	}

	/// <summary>
	/// Sets a function to call on the entity set.
	/// </summary>
	public ODataQueryBuilder<T> Function(string functionName, [Optional, DefaultParameterValue(null)] object? parameters)
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
		ODataListText.AddIfNotBlank(_computeExpressions, computeExpression);

		return this;
	}

	/// <summary>
	/// Appends a raw, vendor-specific query option to the request URL.
	/// </summary>
	/// <param name="rawQueryOptions">
	/// A raw query string segment to append, e.g. <c>"PropertySet=Minimum,AddressList"</c>.
	/// The value is appended verbatim - no quoting or URL encoding is applied.
	/// Multiple calls are combined with <c>&amp;</c>.
	/// </param>
	/// <remarks>
	/// Use this for non-standard query parameters that are not part of the OData specification,
	/// such as Exchange Online's <c>PropertySet</c> parameter.
	/// Unlike Simple.OData.Client's <c>QueryOptions(IDictionary)</c> overload,
	/// this method does not wrap values in single quotes.
	/// </remarks>
	public ODataQueryBuilder<T> QueryOptions(string rawQueryOptions)
	{
		ODataListText.AddIfNotBlank(_rawQueryOptions, rawQueryOptions);

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
}
