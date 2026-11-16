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
public partial class FluentODataQueryBuilder
{
	private readonly ODataClient _client;
	private readonly string _entitySet;
	private readonly ILogger _logger;
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
	private string? _function;
	private object? _functionParameters;
	private string? _apply;
	private readonly List<string> _rawQueryOptions = [];

	/// <summary>
	/// Initializes a new instance of the <see cref="FluentODataQueryBuilder"/> class.
	/// </summary>
	internal FluentODataQueryBuilder(ODataClient client, string entitySet, ILogger logger)
	{
		_client = client;
		_entitySet = entitySet;
		_logger = logger;
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
	/// <param name="keyValue">The entity key.</param>
	/// <returns>This builder for method chaining.</returns>
	public FluentODataQueryBuilder Key<TKey>(TKey keyValue)
	{
		_key = keyValue;
		return this;
	}

	/// <summary>
	/// Navigates to a related collection or singleton via a navigation property name.
	/// Produces a URL segment of the form <c>EntitySet(key)/NavigationProperty</c>.
	/// </summary>
	/// <param name="navigationProperty">The name of the navigation property.</param>
	/// <returns>A new <see cref="FluentODataQueryBuilder"/> rooted at the navigation path.</returns>
	/// <exception cref="InvalidOperationException">Thrown if no key has been set on this builder.</exception>
	public FluentODataQueryBuilder NavigateTo(string navigationProperty)
	{
		if (_key is null)
		{
			throw new InvalidOperationException(
				"NavigateTo requires a key to be set. Call Key() before NavigateTo().");
		}

		var path = $"{_entitySet}({ODataLiteral.FormatKey(_key)})/{navigationProperty}";
		return new FluentODataQueryBuilder(_client, path, _logger);
	}


	/// <summary>
	/// Re-types this builder to deserialize results as <typeparamref name="TResult"/>.
	/// Use after <c>NavigateTo(x =&gt; x.NavProp)</c> when the navigation property type differs
	/// from the desired result type (e.g., a collection property typed as <c>List&lt;T&gt;</c>).
	/// </summary>
	/// <typeparam name="TResult">The type to deserialize results as.</typeparam>
	/// <returns>A typed <see cref="ODataQueryBuilder{TResult}"/> with the same path.</returns>
	/// <example>
	/// <code>
	/// var permissions = await client.For&lt;Mailbox&gt;()
	///     .Key(identity)
	///     .NavigateTo(x =&gt; x.MailboxPermissions)
	///     .As&lt;MailboxPermission&gt;()
	///     .GetAsync(ct);
	/// </code>
	/// </example>
	public ODataQueryBuilder<TResult> As<TResult>() where TResult : class
		=> new(_client, _entitySet, _logger);

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
	/// <param name="orderByExpression">The order by expression.</param>
	/// <returns>This builder for method chaining.</returns>
	public FluentODataQueryBuilder OrderBy(string orderByExpression)
	{
		if (!string.IsNullOrWhiteSpace(orderByExpression))
		{
			_orderByClauses.Add(orderByExpression);
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
	public FluentODataQueryBuilder Count([Optional, DefaultParameterValue(true)] bool includeCount)
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

	/// <summary>
	/// Sets a function to call on the entity set.
	/// </summary>
	/// <param name="functionName">The function name.</param>
	/// <param name="parameters">Optional function parameters.</param>
	/// <returns>This builder for method chaining.</returns>
	public FluentODataQueryBuilder Function(string functionName, [Optional, DefaultParameterValue(null)] object? parameters)
	{
		_function = functionName;
		_functionParameters = parameters;
		return this;
	}

	/// <summary>
	/// Sets a raw $apply clause for aggregations.
	/// </summary>
	/// <param name="applyClause">The apply clause.</param>
	/// <returns>This builder for method chaining.</returns>
	public FluentODataQueryBuilder Apply(string applyClause)
	{
		_apply = applyClause;
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
	public FluentODataQueryBuilder QueryOptions(string rawQueryOptions)
	{
		if (!string.IsNullOrWhiteSpace(rawQueryOptions))
		{
			_rawQueryOptions.Add(rawQueryOptions);
		}

		return this;
	}

	#endregion
}
