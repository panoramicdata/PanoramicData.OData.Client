namespace PanoramicData.OData.Client;

/// <summary>
/// Lambda expression parsing functionality for ODataQueryBuilder.
/// Handles Any/All collection operations and lambda body parsing.
/// </summary>
internal static partial class ODataExpressionTranslator
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
		var predicateBody = ParseLambdaBody(predicateLambda.Body, predicateLambda.Parameters[0], parameterName, parentOperator: null);
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
}
