using System.Collections.Frozen;
using System.Reflection;

namespace PanoramicData.OData.Client;

/// <summary>
/// Expression parsing functionality for ODataQueryBuilder.
/// </summary>
public partial class ODataQueryBuilder<T> where T : class
{
	/// <summary>
	/// Frozen dictionary for O(1) operator lookups - initialized once, thread-safe.
	/// </summary>
	private static readonly FrozenDictionary<ExpressionType, string> OperatorMap = new Dictionary<ExpressionType, string>
	{
		[ExpressionType.Equal] = "eq",
		[ExpressionType.NotEqual] = "ne",
		[ExpressionType.GreaterThan] = "gt",
		[ExpressionType.GreaterThanOrEqual] = "ge",
		[ExpressionType.LessThan] = "lt",
		[ExpressionType.LessThanOrEqual] = "le",
		[ExpressionType.AndAlso] = "and",
		[ExpressionType.OrElse] = "or"
	}.ToFrozenDictionary();

	private static string ExpressionToODataFilter(Expression expression) =>
		ExpressionToODataFilter(expression, parentOperator: null);

	private static string ExpressionToODataFilter(Expression expression, ExpressionType? parentOperator) => expression switch
	{
		BinaryExpression binary => ParseBinaryExpression(binary, parentOperator),
		MethodCallExpression methodCall => ParseMethodCallExpression(methodCall),
		UnaryExpression unary when unary.NodeType == ExpressionType.Not => $"not ({ExpressionToODataFilter(unary.Operand, parentOperator)})",
		UnaryExpression unary when unary.NodeType == ExpressionType.Convert => ExpressionToODataFilter(unary.Operand, parentOperator),
		MemberExpression member when member.Type == typeof(bool) && !ShouldEvaluate(member) => GetMemberPath(member),
		MemberExpression member when ShouldEvaluate(member) => FormatValue(EvaluateExpression(member)),
		MemberExpression member => GetMemberPath(member),
		ConstantExpression constant => FormatValue(constant.Value),
		_ => throw new NotSupportedException($"Expression type {expression.NodeType} is not supported")
	};

	/// <summary>
	/// Determines if an expression should be evaluated (compiled and executed)
	/// rather than converted to an OData property path.
	/// </summary>
	private static bool ShouldEvaluate(MemberExpression member)
	{
		Expression? current = member;
		while (current is MemberExpression memberExpr)
		{
			current = memberExpr.Expression;
		}

		if (current is null)
		{
			return true;
		}

		return current is ConstantExpression;
	}

	/// <summary>
	/// Evaluates an expression to get its value.
	/// Uses reflection for simple member access chains to avoid Expression.Compile() overhead.
	/// </summary>
	private static object? EvaluateExpression(Expression expression)
	{
		// Try fast path: use reflection for simple member access chains
		if (TryEvaluateWithReflection(expression, out var result))
		{
			return result;
		}

		// Fallback: compile the expression (expensive but handles complex cases)
		var objectMember = Expression.Convert(expression, typeof(object));
		var getterLambda = Expression.Lambda<Func<object>>(objectMember);
		var getter = getterLambda.Compile();
		return getter();
	}

	/// <summary>
	/// Attempts to evaluate a member expression using reflection instead of compilation.
	/// This is faster for simple member access chains like: closure.field or closure.field.property
	/// </summary>
	private static bool TryEvaluateWithReflection(Expression expression, out object? result)
	{
		result = null;

		// Build the member access chain
		var memberChain = new Stack<MemberInfo>();
		var current = expression;

		while (current is MemberExpression memberExpr)
		{
			memberChain.Push(memberExpr.Member);
			current = memberExpr.Expression;
		}

		// We need a constant at the root (the closure object)
		if (current is not ConstantExpression constant)
		{
			return false;
		}

		// Walk the chain and get values using reflection
		object? value = constant.Value;

		while (memberChain.Count > 0 && value is not null)
		{
			var member = memberChain.Pop();

			value = member switch
			{
				FieldInfo field => field.GetValue(value),
				PropertyInfo prop => prop.GetValue(value),
				_ => null
			};
		}

		result = value;
		return true;
	}

	private static string ParseBinaryExpression(BinaryExpression binary, ExpressionType? parentOperator)
	{
		// Pass the current operator as parent to child expressions
		var left = ExpressionToODataFilter(binary.Left, binary.NodeType);
		var right = ExpressionToODataFilter(binary.Right, binary.NodeType);

		if (!OperatorMap.TryGetValue(binary.NodeType, out var op))
		{
			throw new NotSupportedException($"Binary operator {binary.NodeType} is not supported");
		}

		var result = $"{left} {op} {right}";

		// Wrap OR expressions in parentheses when they are operands of AND
		// This preserves correct operator precedence since AND has higher precedence than OR in OData
		if (binary.NodeType == ExpressionType.OrElse && parentOperator == ExpressionType.AndAlso)
		{
			return $"({result})";
		}

		return result;
	}

	private static string ParseMethodCallExpression(MethodCallExpression methodCall)
	{
		var methodName = methodCall.Method.Name;

		if (methodCall.Object?.Type == typeof(string))
		{
			return ParseStringMethodCall(methodCall, methodName);
		}

		if (methodName == "Contains" && methodCall.Arguments.Count >= 1)
		{
			var result = TryParseCollectionContains(methodCall);
			if (result is not null)
			{
				return result;
			}
		}

		// Handle Any/All on collections
		if (methodName is "Any" or "All")
		{
			var result = TryParseAnyAll(methodCall, methodName);
			if (result is not null)
			{
				return result;
			}
		}

		throw new NotSupportedException($"Method {methodName} is not supported");
	}

	private static string ParseStringMethodCall(MethodCallExpression methodCall, string methodName)
	{
		var stringPath = GetStringExpressionPath(methodCall.Object!);

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

	private static string? TryParseCollectionContains(MethodCallExpression methodCall)
	{
		if (methodCall.Arguments.Count == 2 && methodCall.Object is null)
		{
			return TryParseStaticContains(methodCall);
		}

		if (methodCall.Arguments.Count == 1 && methodCall.Object is not null)
		{
			return TryParseInstanceContains(methodCall);
		}

		return null;
	}

	private static string? TryParseStaticContains(MethodCallExpression methodCall)
	{
		var collection = GetValue(methodCall.Arguments[0]);
		var propertyArg = methodCall.Arguments[1];

		if (propertyArg is MemberExpression memberExpr && collection is System.Collections.IEnumerable enumerable)
		{
			return FormatInClause(GetMemberPath(memberExpr), enumerable);
		}

		return null;
	}

	private static string? TryParseInstanceContains(MethodCallExpression methodCall)
	{
		var collection = GetValue(methodCall.Object!);
		var propertyArg = methodCall.Arguments[0];

		if (propertyArg is MemberExpression memberExpr && collection is System.Collections.IEnumerable enumerable)
		{
			return FormatInClause(GetMemberPath(memberExpr), enumerable);
		}

		return null;
	}

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
			return "false";
		}

		return $"{propertyPath} in ({string.Join(",", formattedValues)})";
	}

	private static string GetMemberPath(MemberExpression member)
	{
		// Use a stack to avoid List.Insert(0) which is O(n)
		var pathStack = new Stack<string>();
		Expression? current = member;

		while (current is MemberExpression memberExpr)
		{
			pathStack.Push(memberExpr.Member.Name);
			current = memberExpr.Expression;
		}

		// For small paths (common case), avoid string.Join allocation
		return pathStack.Count switch
		{
			0 => string.Empty,
			1 => pathStack.Pop(),
			_ => string.Join("/", pathStack)
		};
	}

	private static object? GetValue(Expression expression) => expression switch
	{
		ConstantExpression constant => constant.Value,
		MemberExpression member => GetMemberValue(member),
		_ => EvaluateExpression(expression) // Use optimized evaluation
	};

	private static object? GetMemberValue(MemberExpression member)
	{
		// Use the same optimized reflection-based evaluation
		if (TryEvaluateWithReflection(member, out var result))
		{
			return result;
		}

		// Fallback to compilation for complex cases
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
		DateTime dt => FormatDateTime(dt),
		DateTimeOffset dto => $"{dto.UtcDateTime:yyyy-MM-ddTHH:mm:ssZ}",
		Guid g => g.ToString(),
		Enum e => $"'{e}'",
		_ => value.ToString() ?? "null"
	};

	private static string FormatDateTime(DateTime dt)
	{
		// OData requires timezone info. Always format as UTC with Z suffix.
		var utc = dt.Kind switch
		{
			DateTimeKind.Utc => dt,
			DateTimeKind.Local => dt.ToUniversalTime(),
			DateTimeKind.Unspecified => dt, // Assume already UTC for unspecified
			_ => dt
		};
		return $"{utc:yyyy-MM-ddTHH:mm:ss.fffZ}";
	}

	private static string FormatKey(object key) => key switch
	{
		int i => i.ToString(CultureInfo.InvariantCulture),
		long l => l.ToString(CultureInfo.InvariantCulture),
		Guid g => g.ToString(),
		string s => $"'{s.Replace("'", "''")}'",
		_ => key.ToString() ?? throw new ArgumentException("Invalid key value")
	};

	/// <summary>
	/// Cache for PropertyInfo arrays by type - anonymous types used in Function() calls.
	/// Uses ConditionalWeakTable to allow garbage collection of types.
	/// </summary>
	private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Type, PropertyInfo[]> PropertyCache = [];

	private static string FormatFunctionParameters(object parameters)
	{
		var type = parameters.GetType();

		// Get or create cached PropertyInfo array
		var props = PropertyCache.GetValue(type, t =>
			t.GetProperties(BindingFlags.Public | BindingFlags.Instance));

		var paramStrings = props
			.Select(p => (Name: p.Name, Value: p.GetValue(parameters)))
			.Where(p => p.Value is not null)
			.Select(p =>
			{
				var formattedValue = FormatFunctionParameterValue(p.Value);
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
		Array arr => FormatArrayParameter(arr),
		System.Collections.IEnumerable enumerable => FormatEnumerableParameter(enumerable),
		_ => value?.ToString() ?? "null"
	};

	private static string FormatArrayElementValue(object? value) => value switch
	{
		null => "null",
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

	private static List<string> GetMemberNames(Expression<Func<T, object?>> selector)
	{
		if (selector.Body is NewExpression newExpr)
		{
			var results = new List<string>();
			foreach (var arg in newExpr.Arguments)
			{
				var memberPath = GetMemberPathFromExpression(arg);
				if (!string.IsNullOrEmpty(memberPath))
				{
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
			var firstSegment = memberPath.Split('/')[0];
			return [firstSegment];
		}

		if (selector.Body is UnaryExpression unary && unary.Operand is MemberExpression unaryMember)
		{
			var memberPath = GetMemberPathFromExpression(unaryMember);
			var firstSegment = memberPath.Split('/')[0];
			return [firstSegment];
		}

		return [];
	}

	private static string GetMemberPathFromExpression(Expression expression)
	{
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

	private static string GetMemberName(Expression<Func<T, object?>> selector)
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

	/// <summary>
	/// Gets full member paths from an expand expression.
	/// For example, p => new { p.BestFriend, p.BestFriend!.Trips } returns ["BestFriend", "BestFriend/Trips"].
	/// </summary>
	private static List<string> GetExpandMemberPaths(Expression<Func<T, object?>> selector)
	{
		var pathInfos = GetExpandMemberPathsWithInfo(selector);
		return pathInfos.Select(p => p.Path).ToList();
	}

	/// <summary>
	/// Gets full member paths from an expand expression along with property type information.
	/// For example, p => new { p.BestFriend, p.BestFriend!.Trips } returns paths with navigation/scalar info.
	/// </summary>
	private static List<ExpandPathInfo> GetExpandMemberPathsWithInfo(Expression<Func<T, object?>> selector)
	{
		var results = new List<ExpandPathInfo>();

		if (selector.Body is NewExpression newExpr)
		{
			foreach (var arg in newExpr.Arguments)
			{
				var pathInfo = GetExpandPathInfoFromExpression(arg);
				if (pathInfo is not null && !results.Any(r => r.Path == pathInfo.Path))
				{
					results.Add(pathInfo);
				}
			}

			return results;
		}

		var singlePathInfo = GetExpandPathInfoFromExpression(selector.Body);
		if (singlePathInfo is not null)
		{
			results.Add(singlePathInfo);
		}

		return results;
	}

	/// <summary>
	/// Extracts expand path information from an expression, including property type info.
	/// </summary>
	private static ExpandPathInfo? GetExpandPathInfoFromExpression(Expression expression)
	{
		if (expression is UnaryExpression unary && unary.NodeType == ExpressionType.Convert)
		{
			expression = unary.Operand;
		}

		if (expression is not MemberExpression member)
		{
			return null;
		}

		var segments = new List<ExpandSegment>();
		Expression? current = member;

		while (current is MemberExpression memberExpr)
		{
			if (memberExpr.Member is PropertyInfo propInfo)
			{
				segments.Insert(0, new ExpandSegment(propInfo.Name, IsNavigationProperty(propInfo)));
			}
			else
			{
				segments.Insert(0, new ExpandSegment(memberExpr.Member.Name, false));
			}

			current = memberExpr.Expression;
		}

		if (segments.Count == 0)
		{
			return null;
		}

		return new ExpandPathInfo(segments);
	}

	/// <summary>
	/// Determines if a property is a navigation property (vs a scalar property).
	/// Navigation properties are entity references or collections of entities.
	/// Scalar properties are primitives, strings, dates, guids, etc.
	/// </summary>
	private static bool IsNavigationProperty(PropertyInfo property)
	{
		var propertyType = property.PropertyType;

		// Check for nullable types - get underlying type
		var underlyingType = Nullable.GetUnderlyingType(propertyType);
		if (underlyingType is not null)
		{
			propertyType = underlyingType;
		}

		// Primitives are scalar
		if (propertyType.IsPrimitive)
		{
			return false;
		}

		// Common scalar types
		if (propertyType == typeof(string) ||
			propertyType == typeof(DateTime) ||
			propertyType == typeof(DateTimeOffset) ||
			propertyType == typeof(DateOnly) ||
			propertyType == typeof(TimeOnly) ||
			propertyType == typeof(TimeSpan) ||
			propertyType == typeof(Guid) ||
			propertyType == typeof(decimal))
		{
			return false;
		}

		// Enums are scalar
		if (propertyType.IsEnum)
		{
			return false;
		}

		// byte[] is scalar (used for binary data)
		if (propertyType == typeof(byte[]))
		{
			return false;
		}

		// Collections of entities are navigation properties (but not string which is IEnumerable<char>)
		if (typeof(System.Collections.IEnumerable).IsAssignableFrom(propertyType) &&
			propertyType != typeof(string))
		{
			return true;
		}

		// Reference types that are classes are typically navigation properties
		// (entity references like Tenant, Role, etc.)
		return propertyType.IsClass;
	}

	/// <summary>
	/// Builds nested expand syntax from a collection of member paths.
	/// Converts paths like ["BestFriend", "BestFriend/Trips", "Friends"] 
	/// into "BestFriend($expand=Trips),Friends".
	/// Handles scalar properties with $select instead of $expand.
	/// </summary>
	private static List<string> BuildNestedExpandFields(List<string> memberPaths)
	{
		// This overload is kept for backward compatibility but uses the new implementation
		// by treating all segments as navigation properties
		var rootNodes = new Dictionary<string, ExpandNode>();

		foreach (var path in memberPaths)
		{
			var segments = path.Split('/');
			var expandSegments = segments.Select(s => new ExpandSegment(s, true)).ToList();
			AddPathToTreeWithInfo(rootNodes, expandSegments, 0);
		}

		var result = new List<string>();
		foreach (var node in rootNodes.Values)
		{
			result.Add(node.ToODataSyntax());
		}

		return result;
	}

	/// <summary>
	/// Builds nested expand syntax from expand path information that includes property type info.
	/// </summary>
	private static List<string> BuildNestedExpandFieldsWithInfo(List<ExpandPathInfo> pathInfos)
	{
		var rootNodes = new Dictionary<string, ExpandNode>();

		foreach (var pathInfo in pathInfos)
		{
			AddPathToTreeWithInfo(rootNodes, pathInfo.Segments, 0);
		}

		var result = new List<string>();
		foreach (var node in rootNodes.Values)
		{
			result.Add(node.ToODataSyntax());
		}

		return result;
	}

	private static void AddPathToTree(Dictionary<string, ExpandNode> nodes, string[] segments, int index)
	{
		if (index >= segments.Length)
		{
			return;
		}

		var segment = segments[index];

		if (!nodes.TryGetValue(segment, out var node))
		{
			node = new ExpandNode(segment, isNavigation: true);
			nodes[segment] = node;
		}

		// Continue with remaining segments as children
		AddPathToTree(node.Children, segments, index + 1);
	}

	private static void AddPathToTreeWithInfo(Dictionary<string, ExpandNode> nodes, List<ExpandSegment> segments, int index)
	{
		if (index >= segments.Count)
		{
			return;
		}

		var segment = segments[index];

		if (!nodes.TryGetValue(segment.Name, out var node))
		{
			node = new ExpandNode(segment.Name, segment.IsNavigation);
			nodes[segment.Name] = node;
		}

		// Continue with remaining segments as children
		AddPathToTreeWithInfo(node.Children, segments, index + 1);
	}

	/// <summary>
	/// Represents a segment in an expand path with property type information.
	/// </summary>
	private sealed record ExpandSegment(string Name, bool IsNavigation);

	/// <summary>
	/// Represents an expand path with its segments and property type information.
	/// </summary>
	private sealed class ExpandPathInfo
	{
		public List<ExpandSegment> Segments { get; }
		public string Path => string.Join("/", Segments.Select(s => s.Name));

		public ExpandPathInfo(List<ExpandSegment> segments)
		{
			Segments = segments;
		}
	}

	/// <summary>
	/// Represents a node in the expand tree structure.
	/// </summary>
	private sealed class ExpandNode
	{
		public string Name { get; }
		public bool IsNavigation { get; }
		public Dictionary<string, ExpandNode> Children { get; } = [];

		public ExpandNode(string name, bool isNavigation = true)
		{
			Name = name;
			IsNavigation = isNavigation;
		}

		public string ToODataSyntax()
		{
			if (Children.Count == 0)
			{
				return Name;
			}

			// Separate children into navigation properties (use $expand) and scalar properties (use $select)
			var navigationChildren = Children.Values.Where(c => c.IsNavigation).ToList();
			var scalarChildren = Children.Values.Where(c => !c.IsNavigation).ToList();

			var options = new List<string>();

			// Add $select for scalar children
			if (scalarChildren.Count > 0)
			{
				var selectFields = string.Join(",", scalarChildren.Select(c => c.Name));
				options.Add($"$select={selectFields}");
			}

			// Add $expand for navigation children
			if (navigationChildren.Count > 0)
			{
				var expandFields = string.Join(",", navigationChildren.Select(c => c.ToODataSyntax()));
				options.Add($"$expand={expandFields}");
			}

			if (options.Count == 0)
			{
				return Name;
			}

			return $"{Name}({string.Join(";", options)})";
		}
	}
}
