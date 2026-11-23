using System.Reflection;

namespace PanoramicData.OData.Client;

/// <summary>
/// Binary, method-call and Contains parsing for ODataQueryBuilder.
/// </summary>
internal static partial class ODataExpressionTranslator
{
	private static string ParseBinaryExpression(BinaryExpression binary, ExpressionType? parentOperator)
	{
		// Pass the current operator as parent to child expressions.
		// Use GetEffectiveType to unwrap Convert nodes so non-nullable enum types are preserved.
		var left = ExpressionToODataFilter(binary.Left, binary.NodeType, GetEffectiveType(binary.Right));
		var right = ExpressionToODataFilter(binary.Right, binary.NodeType, GetEffectiveType(binary.Left));

		if (!ODataExpressionLookups.OperatorMap.TryGetValue(binary.NodeType, out var op))
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
			"Contains" => StringFunction("contains", stringPath, methodCall),
			"StartsWith" => StringFunction("startswith", stringPath, methodCall),
			"EndsWith" => StringFunction("endswith", stringPath, methodCall),
			"ToLower" => $"tolower({stringPath})",
			"ToUpper" => $"toupper({stringPath})",
			"Trim" => $"trim({stringPath})",
			_ => throw new NotSupportedException($"Method {methodName} is not supported")
		};
	}

	private static string StringFunction(string name, string stringPath, MethodCallExpression methodCall)
		=> $"{name}({stringPath},{FormatValue(GetValue(methodCall.Arguments[0]))})";

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
		=> TryFormatInClause(methodCall.Arguments[0], methodCall.Arguments[1]);

	private static string? TryParseInstanceContains(MethodCallExpression methodCall)
		=> TryFormatInClause(methodCall.Object!, methodCall.Arguments[0]);

	private static string? TryFormatInClause(Expression collectionExpression, Expression propertyArg)
	{
		var collection = GetValue(UnwrapSpanConversion(collectionExpression));

		if (propertyArg is MemberExpression memberExpr && collection is System.Collections.IEnumerable enumerable)
		{
			return FormatInClause(GetMemberPath(memberExpr), enumerable);
		}

		return null;
	}

	// C# 14 wraps an array receiver in an implicit conversion to a span, which cannot be boxed or compiled.
	private static Expression UnwrapSpanConversion(Expression expression)
	{
		var type = expression.Type;
		var isSpan = type.IsGenericType
			&& (type.GetGenericTypeDefinition() == typeof(ReadOnlySpan<>) || type.GetGenericTypeDefinition() == typeof(Span<>));

		if (!isSpan)
		{
			return expression;
		}

		return expression switch
		{
			MethodCallExpression { Method.Name: "op_Implicit", Arguments.Count: 1 } call => call.Arguments[0],
			UnaryExpression { NodeType: ExpressionType.Convert } convert => convert.Operand,
			_ => expression
		};
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
}
