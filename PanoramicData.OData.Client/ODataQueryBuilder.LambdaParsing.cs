using System.Linq.Expressions;

namespace PanoramicData.OData.Client;

/// <summary>
/// Lambda expression parsing functionality for ODataQueryBuilder.
/// Handles Any/All collection operations and lambda body parsing.
/// </summary>
public partial class ODataQueryBuilder<T> where T : class
{
	private static string? TryParseAnyAll(MethodCallExpression methodCall, string methodName)
	{
		var (collectionExpr, predicateLambda) = ExtractAnyAllComponents(methodCall);

		if (collectionExpr is null)
		{
			return null;
		}

		var collectionPath = GetCollectionPath(collectionExpr);
		if (string.IsNullOrEmpty(collectionPath))
		{
			return null;
		}

		var odataMethodName = methodName.ToLowerInvariant();

		if (predicateLambda is null)
		{
			return $"{collectionPath}/{odataMethodName}()";
		}

		var parameterName = predicateLambda.Parameters[0].Name ?? "x";
		var predicateBody = ParseLambdaBody(predicateLambda.Body, predicateLambda.Parameters[0], parameterName);

		return $"{collectionPath}/{odataMethodName}({parameterName}: {predicateBody})";
	}

	private static (Expression? collectionExpr, LambdaExpression? predicateLambda) ExtractAnyAllComponents(MethodCallExpression methodCall)
	{
		if (methodCall.Object is not null)
		{
			return ExtractInstanceAnyAllComponents(methodCall);
		}

		return methodCall.Arguments.Count >= 1
			? ExtractStaticAnyAllComponents(methodCall)
			: (null, null);
	}

	private static (Expression?, LambdaExpression?) ExtractInstanceAnyAllComponents(MethodCallExpression methodCall)
	{
		var lambda = methodCall.Arguments.Count > 0 ? methodCall.Arguments[0] as LambdaExpression : null;
		return (methodCall.Object, lambda);
	}

	private static (Expression?, LambdaExpression?) ExtractStaticAnyAllComponents(MethodCallExpression methodCall)
	{
		var predicateLambda = methodCall.Arguments.Count > 1
			? ExtractLambdaFromArgument(methodCall.Arguments[1])
			: null;
		return (methodCall.Arguments[0], predicateLambda);
	}

	private static LambdaExpression? ExtractLambdaFromArgument(Expression argument)
	{
		if (argument is UnaryExpression quote && quote.NodeType == ExpressionType.Quote)
		{
			return quote.Operand as LambdaExpression;
		}

		return argument as LambdaExpression;
	}

	private static string GetCollectionPath(Expression expression) => expression switch
	{
		MemberExpression member => GetMemberPath(member),
		MethodCallExpression mc when mc.Method.Name == "Select" => GetCollectionPath(mc.Arguments[0]),
		UnaryExpression unary => GetCollectionPath(unary.Operand),
		_ => string.Empty
	};

	private static string ParseLambdaBody(Expression body, ParameterExpression lambdaParam, string odataParamName) => body switch
	{
		BinaryExpression binary => ParseLambdaBinaryExpression(binary, lambdaParam, odataParamName),
		MethodCallExpression methodCall => ParseLambdaMethodCall(methodCall, lambdaParam, odataParamName),
		UnaryExpression u when u.NodeType == ExpressionType.Not => $"not ({ParseLambdaBody(u.Operand, lambdaParam, odataParamName)})",
		UnaryExpression u when u.NodeType == ExpressionType.Convert => ParseLambdaBody(u.Operand, lambdaParam, odataParamName),
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

		if (methodCall.Object?.Type == typeof(string))
		{
			return ParseLambdaStringMethod(methodCall, lambdaParam, odataParamName, methodName);
		}

		if (methodCall.Method.DeclaringType == typeof(string) && methodName == "IsNullOrEmpty")
		{
			var argPath = GetLambdaExpressionPath(methodCall.Arguments[0], lambdaParam, odataParamName);
			return $"({argPath} eq null or {argPath} eq '')";
		}

		if (methodName is "Any" or "All")
		{
			var nestedResult = TryParseNestedAnyAll(methodCall, methodName, lambdaParam, odataParamName);
			if (nestedResult is not null)
			{
				return nestedResult;
			}
		}

		throw new NotSupportedException($"Method {methodName} is not supported in lambda body");
	}

	private static string ParseLambdaStringMethod(MethodCallExpression methodCall, ParameterExpression lambdaParam, string odataParamName, string methodName)
	{
		var stringPath = GetLambdaExpressionPath(methodCall.Object!, lambdaParam, odataParamName);
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

	private static string? TryParseNestedAnyAll(MethodCallExpression methodCall, string methodName, ParameterExpression outerLambdaParam, string outerODataParamName)
	{
		var (collectionExpr, predicateLambda) = ExtractAnyAllComponents(methodCall);

		if (collectionExpr is null)
		{
			return null;
		}

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
		UnaryExpression u when u.NodeType == ExpressionType.Convert => GetLambdaExpressionPath(u.Operand, lambdaParam, odataParamName),
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

		if (current == lambdaParam)
		{
			path.Insert(0, odataParamName);
		}
		else if (current is ConstantExpression)
		{
			return FormatValue(EvaluateExpression(member));
		}

		return string.Join("/", path);
	}
}
