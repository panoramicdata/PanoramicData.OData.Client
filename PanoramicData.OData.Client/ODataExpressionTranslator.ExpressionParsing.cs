using System.Reflection;

namespace PanoramicData.OData.Client;

/// <summary>
/// Expression parsing functionality for ODataQueryBuilder.
/// </summary>
internal static partial class ODataExpressionTranslator
{
	internal static string ExpressionToODataFilter(Expression expression) =>
		ExpressionToODataFilter(expression, parentOperator: null, expectedType: null);

	private static string ExpressionToODataFilter(Expression expression, ExpressionType? parentOperator, Type? expectedType) => expression switch
	{
		BinaryExpression binary => ParseBinaryExpression(binary, parentOperator),
		MethodCallExpression methodCall => ParseMethodCallExpression(methodCall),
		UnaryExpression unary when unary.NodeType == ExpressionType.Not => $"not ({ExpressionToODataFilter(unary.Operand, parentOperator, expectedType)})",
		UnaryExpression unary when unary.NodeType == ExpressionType.Convert => ExpressionToODataFilter(unary.Operand, parentOperator, expectedType),
		MemberExpression member when member.Type == typeof(bool) && !ShouldEvaluate(member) => GetMemberPath(member),
		MemberExpression member when ShouldEvaluate(member) => FormatValue(EvaluateExpression(member), expectedType),
		MemberExpression member => GetMemberPath(member),
		ConstantExpression constant => FormatValue(constant.Value, expectedType),
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

	/// <summary>
	/// Gets the effective type of an expression, unwrapping Convert nodes to reveal the actual enum type.
	/// The C# compiler wraps non-nullable enum operands in Convert(expr, int) for comparisons,
	/// which would hide the enum type from the expectedType logic.
	/// </summary>
	private static Type GetEffectiveType(Expression expression)
	{
		if (expression is UnaryExpression { NodeType: ExpressionType.Convert } unary)
		{
			var operandType = unary.Operand.Type;
			var underlying = Nullable.GetUnderlyingType(operandType) ?? operandType;

			if (underlying.IsEnum)
			{
				return operandType;
			}
		}

		return expression.Type;
	}
}
