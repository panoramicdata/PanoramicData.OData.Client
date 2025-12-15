using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace PanoramicData.OData.Client;

/// <summary>
/// Builds OData query URLs from LINQ-like expressions.
/// </summary>
/// <typeparam name="T">The entity type being queried.</typeparam>
public class ODataQueryBuilder<T>(string entitySet, ILogger logger) where T : class
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
	public ODataQueryBuilder<T> Select(Expression<Func<T, object>> selector)
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
	public ODataQueryBuilder<T> Expand(Expression<Func<T, object>> selector)
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
	public ODataQueryBuilder<T> OrderBy(Expression<Func<T, object>> selector, bool descending = false)
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
		foreach (var ob in orderBys)
		{
			_orderByClauses.Add(ob.Value ? $"{ob.Key} desc" : ob.Key);
		}

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

		// Handle key for single entity
		if (_key is not null)
		{
			sb.Append('(');
			sb.Append(FormatKey(_key));
			sb.Append(')');
			logger.LogDebug("ODataQueryBuilder - Key: {Key}", _key);
		}

		// Handle function call
		if (!string.IsNullOrEmpty(_function))
		{
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

		// Build query string
		var queryParams = new List<string>();

		if (_filterClauses.Count > 0)
		{
			var combinedFilter = string.Join(" and ", _filterClauses.Select(f => $"({f})"));
			queryParams.Add($"$filter={Uri.EscapeDataString(combinedFilter)}");
			logger.LogDebug("ODataQueryBuilder - Filter: {Filter}", combinedFilter);
		}

		if (!string.IsNullOrWhiteSpace(_search))
		{
			queryParams.Add($"$search={Uri.EscapeDataString(_search)}");
			logger.LogDebug("ODataQueryBuilder - Search: {Search}", _search);
		}

		if (_selectFields.Count > 0)
		{
			var selectClause = string.Join(",", _selectFields);
			queryParams.Add($"$select={selectClause}");
			logger.LogDebug("ODataQueryBuilder - Select: {Select}", selectClause);
		}

		if (_expandFields.Count > 0)
		{
			var expandClause = string.Join(",", _expandFields);
			queryParams.Add($"$expand={expandClause}");
			logger.LogDebug("ODataQueryBuilder - Expand: {Expand}", expandClause);
		}

		if (_orderByClauses.Count > 0)
		{
			var orderByClause = string.Join(",", _orderByClauses);
			queryParams.Add($"$orderby={orderByClause}");
			logger.LogDebug("ODataQueryBuilder - OrderBy: {OrderBy}", orderByClause);
		}

		if (_skip.HasValue)
		{
			queryParams.Add($"$skip={_skip.Value}");
			logger.LogDebug("ODataQueryBuilder - Skip: {Skip}", _skip.Value);
		}

		if (_top.HasValue)
		{
			queryParams.Add($"$top={_top.Value}");
			logger.LogDebug("ODataQueryBuilder - Top: {Top}", _top.Value);
		}

		if (_count)
		{
			queryParams.Add("$count=true");
			logger.LogDebug("ODataQueryBuilder - Count: true");
		}

		if (!string.IsNullOrWhiteSpace(_apply))
		{
			queryParams.Add($"$apply={Uri.EscapeDataString(_apply)}");
			logger.LogDebug("ODataQueryBuilder - Apply: {Apply}", _apply);
		}

		if (queryParams.Count > 0)
		{
			sb.Append('?');
			sb.Append(string.Join("&", queryParams));
		}

		var url = sb.ToString();
		logger.LogDebug("ODataQueryBuilder<{Type}>.BuildUrl() - Final URL: {Url}", typeof(T).Name, url);

		return url;
	}

	#region Expression Parsing

	private static string ExpressionToODataFilter(Expression expression) => expression switch
	{
		BinaryExpression binary => ParseBinaryExpression(binary),
		MethodCallExpression methodCall => ParseMethodCallExpression(methodCall),
		UnaryExpression unary when unary.NodeType == ExpressionType.Not => $"not ({ExpressionToODataFilter(unary.Operand)})",
		UnaryExpression unary when unary.NodeType == ExpressionType.Convert => ExpressionToODataFilter(unary.Operand),
		MemberExpression member when member.Type == typeof(bool) && !ShouldEvaluate(member) => GetMemberPath(member),
		MemberExpression member when ShouldEvaluate(member) => FormatValue(EvaluateExpression(member)),
		MemberExpression member => GetMemberPath(member),
		ConstantExpression constant => FormatValue(constant.Value),
		_ => throw new NotSupportedException($"Expression type {expression.NodeType} is not supported")
	};

	/// <summary>
	/// Determines if an expression should be evaluated (compiled and executed)
	/// rather than converted to an OData property path.
	/// This includes:
	/// - Static property access (e.g., DateTime.UtcNow, DateTime.Now)
	/// - Captured variables (closures)
	/// </summary>
	private static bool ShouldEvaluate(MemberExpression member)
	{
		// Walk up the expression tree to find the root
		Expression? current = member;
		while (current is MemberExpression memberExpr)
		{
			current = memberExpr.Expression;
		}

		// If the root is null, this is a static property access (e.g., DateTime.UtcNow)
		if (current is null)
		{
			return true;
		}

		// If the root is a ConstantExpression, this is a closure (captured variable)
		// If the root is a ParameterExpression, this is a property access on the entity
		return current is ConstantExpression;
	}

	/// <summary>
	/// Evaluates an expression and returns its value.
	/// </summary>
	private static object? EvaluateExpression(Expression expression)
	{
		var objectMember = Expression.Convert(expression, typeof(object));
		var getterLambda = Expression.Lambda<Func<object>>(objectMember);
		var getter = getterLambda.Compile();
		return getter();
	}

	private static string ParseBinaryExpression(BinaryExpression binary)
	{
		var left = ExpressionToODataFilter(binary.Left);
		var right = ExpressionToODataFilter(binary.Right);

		var op = binary.NodeType switch
		{
			ExpressionType.Equal => "eq",
			ExpressionType.NotEqual => "ne",
			ExpressionType.GreaterThan => "gt",
			ExpressionType.GreaterThanOrEqual => "ge",
			ExpressionType.LessThan => "lt",
			ExpressionType.LessThanOrEqual => "le",
			ExpressionType.AndAlso => "and",
			ExpressionType.OrElse => "or",
			_ => throw new NotSupportedException($"Binary operator {binary.NodeType} is not supported")
		};

		return $"{left} {op} {right}";
	}

	private static string ParseMethodCallExpression(MethodCallExpression methodCall)
	{
		var methodName = methodCall.Method.Name;

		// Handle string instance methods (e.g., property.Contains("value"))
		if (methodCall.Object?.Type == typeof(string))
		{
			// Get the string expression path - handles both MemberExpression and nested method calls
			var stringPath = GetStringExpressionPath(methodCall.Object);

			return methodName switch
			{
				"Contains" => $"contains({stringPath},{FormatValue(GetValue(methodCall.Arguments[0]))})",
				"StartsWith" => $"startswith({stringPath},{FormatValue(GetValue(methodCall.Arguments[0]))})",
				"EndsWith" => $"endswith({stringPath},{FormatValue(GetValue(methodCall.Arguments[0]))})",
				"ToLower" => $"tolower({stringPath})",
				"ToUpper" => $"toupper({stringPath})",
				"Trim" => $"trim({stringPath})",
				_ => throw new NotSupportedException($"Method {methodName} is not supported")
			};
		}

		// Handle collection Contains (e.g., new[] { 1, 2, 3 }.Contains(x.Id) or list.Contains(x.Property))
		// This translates to OData "in" operator: Id in (1, 2, 3)
		if (methodName == "Contains" && methodCall.Arguments.Count >= 1)
		{
			// Static Enumerable.Contains<T>(source, value) - 2 arguments
			if (methodCall.Arguments.Count == 2 && methodCall.Object is null)
			{
				var collection = GetValue(methodCall.Arguments[0]);
				var propertyArg = methodCall.Arguments[1];

				if (propertyArg is MemberExpression memberExpr && collection is System.Collections.IEnumerable enumerable)
				{
					return FormatInClause(GetMemberPath(memberExpr), enumerable);
				}
			}
			// Instance List<T>.Contains(value) or ICollection<T>.Contains(value) - 1 argument, Object is the collection
			else if (methodCall.Arguments.Count == 1 && methodCall.Object is not null)
			{
				var collection = GetValue(methodCall.Object);
				var propertyArg = methodCall.Arguments[0];

				if (propertyArg is MemberExpression memberExpr && collection is System.Collections.IEnumerable enumerable)
				{
					return FormatInClause(GetMemberPath(memberExpr), enumerable);
				}
			}
		}

		throw new NotSupportedException($"Method {methodName} is not supported");
	}

	/// <summary>
	/// Gets the OData path for a string expression, handling both simple property access
	/// and nested method calls like ToLower().
	/// </summary>
	private static string GetStringExpressionPath(Expression expression) => expression switch
	{
		MemberExpression member => GetMemberPath(member),
		MethodCallExpression nestedMethodCall => ParseMethodCallExpression(nestedMethodCall),
		UnaryExpression unary when unary.Operand is MemberExpression unaryMember => GetMemberPath(unaryMember),
		_ => throw new NotSupportedException($"Expression type {expression.GetType().Name} is not supported for string operations")
	};

	private static string FormatInClause(string propertyPath, System.Collections.IEnumerable values)
	{
		var formattedValues = new List<string>();
		foreach (var value in values)
		{
			formattedValues.Add(FormatValue(value));
		}

		if (formattedValues.Count == 0)
		{
			// Empty collection - return a filter that's always false
			return "false";
		}

		return $"{propertyPath} in ({string.Join(",", formattedValues)})";
	}

	private static string GetMemberPath(MemberExpression member)
	{
		var path = new List<string>();
		Expression? current = member;

		while (current is MemberExpression memberExpr)
		{
			path.Insert(0, memberExpr.Member.Name);
			current = memberExpr.Expression;
		}

		return string.Join("/", path);
	}

	private static object? GetValue(Expression expression) => expression switch
	{
		ConstantExpression constant => constant.Value,
		MemberExpression member => GetMemberValue(member),
		_ => Expression.Lambda(expression).Compile().DynamicInvoke()
	};

	private static object? GetMemberValue(MemberExpression member)
	{
		var objectMember = Expression.Convert(member, typeof(object));
		var getterLambda = Expression.Lambda<Func<object>>(objectMember);
		var getter = getterLambda.Compile();
		return getter();
	}

	private static string FormatValue(object? value) => value switch
	{
		null => "null",
		string s => $"'{s.Replace("'", "''")}'",
		bool b => b.ToString().ToLowerInvariant(),
		DateTime dt => $"{dt:yyyy-MM-ddTHH:mm:ssZ}",
		DateTimeOffset dto => $"{dto.UtcDateTime:yyyy-MM-ddTHH:mm:ssZ}",
		Guid g => g.ToString(),
		Enum e => $"'{e}'",
		_ => value.ToString() ?? "null"
	};

	private static string FormatKey(object key) => key switch
	{
		int i => i.ToString(CultureInfo.InvariantCulture),
		long l => l.ToString(CultureInfo.InvariantCulture),
		Guid g => g.ToString(),
		string s => $"'{s.Replace("'", "''")}'",
		_ => key.ToString() ?? throw new ArgumentException("Invalid key value")
	};

	private static string FormatFunctionParameters(object parameters)
	{
		var props = parameters.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
		var paramStrings = props
			.Where(p => p.GetValue(parameters) is not null)
			.Select(p =>
			{
				var value = p.GetValue(parameters);
				var formattedValue = FormatFunctionParameterValue(value);
				return $"{char.ToLowerInvariant(p.Name[0])}{p.Name[1..]}={formattedValue}";
			});

		return string.Join(",", paramStrings);
	}

	private static string FormatFunctionParameterValue(object? value) => value switch
	{
		null => "null",
		string s => $"'{s.Replace("'", "''")}'",
		bool b => b.ToString().ToLowerInvariant(),
		int i => i.ToString(CultureInfo.InvariantCulture),
		long l => l.ToString(CultureInfo.InvariantCulture),
		double d => d.ToString(CultureInfo.InvariantCulture),
		decimal dec => dec.ToString(CultureInfo.InvariantCulture),
		DateTime dt => $"{dt:yyyy-MM-ddTHH:mm:ssZ}",
		DateTimeOffset dto => $"{dto.UtcDateTime:yyyy-MM-ddTHH:mm:ssZ}",
		Guid g => g.ToString(),
		Enum e => $"'{e}'",
		// Handle arrays/collections - OData uses [...] syntax for collections
		Array arr => FormatArrayParameter(arr),
		System.Collections.IEnumerable enumerable => FormatEnumerableParameter(enumerable),
		_ => value?.ToString() ?? "null"
	};

	/// <summary>
	/// Formats a value for use inside an OData array parameter.
	/// String values in arrays should NOT be quoted for enum-like values.
	/// </summary>
	private static string FormatArrayElementValue(object? value) => value switch
	{
		null => "null",
		// Strings in arrays are typically enum values in OData functions - don't quote them
		string s => s,
		bool b => b.ToString().ToLowerInvariant(),
		int i => i.ToString(CultureInfo.InvariantCulture),
		long l => l.ToString(CultureInfo.InvariantCulture),
		double d => d.ToString(CultureInfo.InvariantCulture),
		decimal dec => dec.ToString(CultureInfo.InvariantCulture),
		DateTime dt => $"{dt:yyyy-MM-ddTHH:mm:ssZ}",
		DateTimeOffset dto => $"{dto.UtcDateTime:yyyy-MM-ddTHH:mm:ssZ}",
		Guid g => g.ToString(),
		Enum e => e.ToString(),
		_ => value?.ToString() ?? "null"
	};

	private static string FormatArrayParameter(Array arr)
	{
		var elements = new List<string>();
		foreach (var item in arr)
		{
			elements.Add(FormatArrayElementValue(item));
		}

		return $"[{string.Join(",", elements)}]";
	}

	private static string FormatEnumerableParameter(System.Collections.IEnumerable enumerable)
	{
		var elements = new List<string>();
		foreach (var item in enumerable)
		{
			elements.Add(FormatArrayElementValue(item));
		}

		return $"[{string.Join(",", elements)}]";
	}

	private static List<string> GetMemberNames(Expression<Func<T, object>> selector)
	{
		if (selector.Body is NewExpression newExpr)
		{
			// For anonymous types, extract the member paths from the arguments
			// This handles cases like: x => new { x.Tenant!.Name } where we want "Tenant" not "Name"
			var results = new List<string>();
			foreach (var arg in newExpr.Arguments)
			{
				var memberPath = GetMemberPathFromExpression(arg);
				if (!string.IsNullOrEmpty(memberPath))
				{
					// For expand, we only want the first navigation property in the path
					// e.g., "Tenant/Name" should become "Tenant" for $expand
					var firstSegment = memberPath.Split('/')[0];
					if (!results.Contains(firstSegment))
					{
						results.Add(firstSegment);
					}
				}
			}

			return results;
		}

		if (selector.Body is MemberExpression member)
		{
			var memberPath = GetMemberPathFromExpression(member);
			// For expand, we only want the first navigation property in the path
			// e.g., "Tenant/Name" should become "Tenant" for $expand
			var firstSegment = memberPath.Split('/')[0];
			return [firstSegment];
		}

		if (selector.Body is UnaryExpression unary && unary.Operand is MemberExpression unaryMember)
		{
			var memberPath = GetMemberPathFromExpression(unaryMember);
			// For expand, we only want the first navigation property in the path
			// e.g., "Tenant/Name" should become "Tenant" for $expand
			var firstSegment = memberPath.Split('/')[0];
			return [firstSegment];
		}

		return [];
	}

	private static string GetMemberPathFromExpression(Expression expression)
	{
		// Handle null-forgiving operator (!) which appears as a Convert expression
		if (expression is UnaryExpression unary && unary.NodeType == ExpressionType.Convert)
		{
			expression = unary.Operand;
		}

		if (expression is MemberExpression member)
		{
			return GetMemberPath(member);
		}

		return string.Empty;
	}

	private static string GetMemberName(Expression<Func<T, object>> selector)
	{
		if (selector.Body is MemberExpression member)
		{
			return member.Member.Name;
		}

		if (selector.Body is UnaryExpression unary && unary.Operand is MemberExpression unaryMember)
		{
			return unaryMember.Member.Name;
		}

		throw new ArgumentException("Invalid selector expression");
	}

	#endregion
}
