using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;

namespace PanoramicData.OData.Client;

/// <summary>
/// Expression parsing functionality for ODataQueryBuilder.
/// </summary>
public partial class ODataQueryBuilder<T> where T : class
{
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

	private static string? TryParseAnyAll(MethodCallExpression methodCall, string methodName)
	{
		// Handle two patterns:
		// 1. Instance method: collection.Any(x => predicate) - methodCall.Object is the collection
		// 2. Static method: Enumerable.Any(collection, x => predicate) - methodCall.Arguments[0] is the collection

		Expression? collectionExpr = null;
		LambdaExpression? predicateLambda = null;

		if (methodCall.Object is not null)
		{
			// Instance method pattern: collection.Any() or collection.Any(x => predicate)
			collectionExpr = methodCall.Object;
			if (methodCall.Arguments.Count > 0 && methodCall.Arguments[0] is LambdaExpression lambda)
			{
				predicateLambda = lambda;
			}
		}
		else if (methodCall.Arguments.Count >= 1)
		{
			// Static method pattern: Enumerable.Any(collection) or Enumerable.Any(collection, x => predicate)
			collectionExpr = methodCall.Arguments[0];
			if (methodCall.Arguments.Count > 1)
			{
				// Need to unwrap the Quote expression that wraps the lambda
				var predicateArg = methodCall.Arguments[1];
				if (predicateArg is UnaryExpression quote && quote.NodeType == ExpressionType.Quote)
				{
					predicateLambda = quote.Operand as LambdaExpression;
				}
				else if (predicateArg is LambdaExpression lambda)
				{
					predicateLambda = lambda;
				}
			}
		}

		if (collectionExpr is null)
		{
			return null;
		}

		// Get the collection path (e.g., "Emails", "Friends", "Orders/Items")
		var collectionPath = GetCollectionPath(collectionExpr);
		if (string.IsNullOrEmpty(collectionPath))
		{
			return null;
		}

		var odataMethodName = methodName.ToLowerInvariant(); // "any" or "all"

		if (predicateLambda is null)
		{
			// No predicate: collection.Any() -> CollectionName/any()
			return $"{collectionPath}/{odataMethodName}()";
		}

		// Get the lambda parameter name (e.g., "e" from e => e.Contains("@"))
		var parameterName = predicateLambda.Parameters[0].Name ?? "x";

		// Parse the predicate body, replacing parameter references with the OData lambda variable
		var predicateBody = ParseLambdaBody(predicateLambda.Body, predicateLambda.Parameters[0], parameterName);

		return $"{collectionPath}/{odataMethodName}({parameterName}: {predicateBody})";
	}

	private static string GetCollectionPath(Expression expression) => expression switch
	{
		MemberExpression member => GetMemberPath(member),
		MethodCallExpression methodCall when methodCall.Method.Name == "Select" =>
			// Handle .Select().Any() pattern - get the source collection
			GetCollectionPath(methodCall.Arguments[0]),
		UnaryExpression unary => GetCollectionPath(unary.Operand),
		_ => string.Empty
	};

	private static string ParseLambdaBody(Expression body, ParameterExpression lambdaParam, string odataParamName) => body switch
	{
		BinaryExpression binary => ParseLambdaBinaryExpression(binary, lambdaParam, odataParamName),
		MethodCallExpression methodCall => ParseLambdaMethodCall(methodCall, lambdaParam, odataParamName),
		UnaryExpression unary when unary.NodeType == ExpressionType.Not =>
			$"not ({ParseLambdaBody(unary.Operand, lambdaParam, odataParamName)})",
		UnaryExpression unary when unary.NodeType == ExpressionType.Convert =>
			ParseLambdaBody(unary.Operand, lambdaParam, odataParamName),
		MemberExpression member when member.Type == typeof(bool) =>
			GetLambdaMemberPath(member, lambdaParam, odataParamName),
		MemberExpression member => GetLambdaMemberPath(member, lambdaParam, odataParamName),
		ConstantExpression constant => FormatValue(constant.Value),
		ParameterExpression param when param == lambdaParam => odataParamName,
		_ => throw new NotSupportedException($"Expression type {body.NodeType} is not supported in lambda body")
	};

	private static string ParseLambdaBinaryExpression(BinaryExpression binary, ParameterExpression lambdaParam, string odataParamName)
	{
		var left = ParseLambdaBody(binary.Left, lambdaParam, odataParamName);
		var right = ParseLambdaBody(binary.Right, lambdaParam, odataParamName);

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
			_ => throw new NotSupportedException($"Binary operator {binary.NodeType} is not supported in lambda")
		};

		return $"{left} {op} {right}";
	}

	private static string ParseLambdaMethodCall(MethodCallExpression methodCall, ParameterExpression lambdaParam, string odataParamName)
	{
		var methodName = methodCall.Method.Name;

		// Handle string methods on lambda parameter properties
		if (methodCall.Object?.Type == typeof(string))
		{
			var stringPath = GetLambdaExpressionPath(methodCall.Object, lambdaParam, odataParamName);
			return methodName switch
			{
				"Contains" => $"contains({stringPath},{FormatValue(GetValue(methodCall.Arguments[0]))})",
				"StartsWith" => $"startswith({stringPath},{FormatValue(GetValue(methodCall.Arguments[0]))})",
				"EndsWith" => $"endswith({stringPath},{FormatValue(GetValue(methodCall.Arguments[0]))})",
				"ToLower" => $"tolower({stringPath})",
				"ToUpper" => $"toupper({stringPath})",
				"Trim" => $"trim({stringPath})",
				_ => throw new NotSupportedException($"String method {methodName} is not supported in lambda")
			};
		}

		// Handle static string methods like string.IsNullOrEmpty
		if (methodCall.Method.DeclaringType == typeof(string))
		{
			if (methodName == "IsNullOrEmpty" && methodCall.Arguments.Count == 1)
			{
				var argPath = GetLambdaExpressionPath(methodCall.Arguments[0], lambdaParam, odataParamName);
				return $"({argPath} eq null or {argPath} eq '')";
			}
		}

		// Handle nested Any/All
		if (methodName is "Any" or "All")
		{
			// For nested any/all, we need to handle it recursively
			var nestedResult = TryParseNestedAnyAll(methodCall, methodName, lambdaParam, odataParamName);
			if (nestedResult is not null)
			{
				return nestedResult;
			}
		}

		throw new NotSupportedException($"Method {methodName} is not supported in lambda body");
	}

	private static string? TryParseNestedAnyAll(MethodCallExpression methodCall, string methodName, ParameterExpression outerLambdaParam, string outerODataParamName)
	{
		Expression? collectionExpr = null;
		LambdaExpression? predicateLambda = null;

		if (methodCall.Object is not null)
		{
			collectionExpr = methodCall.Object;
			if (methodCall.Arguments.Count > 0)
			{
				var arg = methodCall.Arguments[0];
				if (arg is UnaryExpression quote && quote.NodeType == ExpressionType.Quote)
				{
					predicateLambda = quote.Operand as LambdaExpression;
				}
				else if (arg is LambdaExpression lambda)
				{
					predicateLambda = lambda;
				}
			}
		}
		else if (methodCall.Arguments.Count >= 1)
		{
			collectionExpr = methodCall.Arguments[0];
			if (methodCall.Arguments.Count > 1)
			{
				var arg = methodCall.Arguments[1];
				if (arg is UnaryExpression quote && quote.NodeType == ExpressionType.Quote)
				{
					predicateLambda = quote.Operand as LambdaExpression;
				}
				else if (arg is LambdaExpression lambda)
				{
					predicateLambda = lambda;
				}
			}
		}

		if (collectionExpr is null)
		{
			return null;
		}

		// Get the collection path relative to the outer lambda parameter
		var collectionPath = GetLambdaExpressionPath(collectionExpr, outerLambdaParam, outerODataParamName);
		var odataMethodName = methodName.ToLowerInvariant();

		if (predicateLambda is null)
		{
			return $"{collectionPath}/{odataMethodName}()";
		}

		var innerParamName = predicateLambda.Parameters[0].Name ?? "x";
		var predicateBody = ParseLambdaBody(predicateLambda.Body, predicateLambda.Parameters[0], innerParamName);

		return $"{collectionPath}/{odataMethodName}({innerParamName}: {predicateBody})";
	}

	private static string GetLambdaExpressionPath(Expression expression, ParameterExpression lambdaParam, string odataParamName) => expression switch
	{
		ParameterExpression param when param == lambdaParam => odataParamName,
		MemberExpression member => GetLambdaMemberPath(member, lambdaParam, odataParamName),
		MethodCallExpression methodCall => ParseLambdaMethodCall(methodCall, lambdaParam, odataParamName),
		UnaryExpression unary when unary.NodeType == ExpressionType.Convert =>
			GetLambdaExpressionPath(unary.Operand, lambdaParam, odataParamName),
		_ => throw new NotSupportedException($"Expression type {expression.GetType().Name} is not supported in lambda path")
	};

	private static string GetLambdaMemberPath(MemberExpression member, ParameterExpression lambdaParam, string odataParamName)
	{
		var path = new List<string>();
		Expression? current = member;

		while (current is MemberExpression memberExpr)
		{
			path.Insert(0, memberExpr.Member.Name);
			current = memberExpr.Expression;
		}

		// Check if the expression chain ends at the lambda parameter
		if (current == lambdaParam)
		{
			// Prefix with the OData parameter name
			path.Insert(0, odataParamName);
		}
		else if (current is ConstantExpression)
		{
			// This is a closure variable - evaluate it
			return FormatValue(EvaluateExpression(member));
		}

		return string.Join("/", path);
	}
}
