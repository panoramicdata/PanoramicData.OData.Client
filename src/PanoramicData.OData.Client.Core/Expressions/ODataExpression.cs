﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace PanoramicData.OData.Client;

public partial class ODataExpression
{
	private readonly ODataExpression _functionCaller;
	private readonly ODataExpression _left;
	private readonly ODataExpression _right;
	private readonly ExpressionType _operator = ExpressionType.Default;
	private readonly Type _conversionType;

	public string Reference { get; private set; }
	public object Value { get; private set; }
	public ExpressionFunction Function { get; private set; }
	public bool IsValueConversion => _conversionType != null;

	internal ODataExpression()
	{
	}

	public ODataExpression(Expression expression)
		: this(FromLinqExpression(expression))
	{
	}

	internal protected ODataExpression(ODataExpression expression)
	{
		_functionCaller = expression._functionCaller;
		_left = expression._left;
		_right = expression._right;
		_operator = expression._operator;
		_conversionType = expression._conversionType;

		Reference = expression.Reference;
		Value = expression.Value;
		Function = expression.Function;
	}

	internal protected ODataExpression(object value)
	{
		Value = value;
	}

	internal protected ODataExpression(string reference)
	{
		Reference = reference;
	}

	internal protected ODataExpression(string reference, object value)
	{
		Reference = reference;
		Value = value;
	}

	internal protected ODataExpression(ExpressionFunction function)
	{
		Function = function;
	}

	internal protected ODataExpression(ODataExpression left, ODataExpression right, ExpressionType expressionOperator)
	{
		_left = left;
		_right = right;
		_operator = expressionOperator;
	}

	internal protected ODataExpression(ODataExpression caller, string reference)
	{
		_functionCaller = caller;
		Reference = reference;
	}

	internal protected ODataExpression(ODataExpression caller, ExpressionFunction function)
	{
		_functionCaller = caller;
		Function = function;
	}

	internal protected ODataExpression(ODataExpression expr, Type conversionType)
	{
		_conversionType = conversionType;
		Value = expr;
	}

	internal static ODataExpression FromReference(string reference) => new(reference, (object)null);

	internal static ODataExpression FromValue(object value) => new(value);

	internal static ODataExpression FromFunction(ExpressionFunction function) => new(function);

	internal static ODataExpression FromFunction(string functionName, ODataExpression targetExpression, IEnumerable<object> arguments) => new(
			targetExpression,
			new ExpressionFunction(functionName, arguments));

	internal static ODataExpression FromFunction(string functionName, ODataExpression targetExpression, IEnumerable<Expression> arguments) => new(
			targetExpression,
			new ExpressionFunction(functionName, arguments));

	internal static ODataExpression FromLinqExpression(Expression expression) => ParseLinqExpression(expression);

	public bool IsNull => Value == null &&
			Reference == null &&
			Function == null &&
			_operator == ExpressionType.Default;

	public string AsString(ISession session) => Format(new ExpressionContext(session));

	private static readonly char[] _propertySeperator = { '.', '/' };
	internal bool ExtractLookupColumns(IDictionary<string, object> lookupColumns)
	{
		switch (_operator)
		{
			case ExpressionType.And:
				var ok = _left.ExtractLookupColumns(lookupColumns);
				if (ok)
				{
					ok = _right.ExtractLookupColumns(lookupColumns);
				}

				return ok;

			case ExpressionType.Equal:
				var expr = IsValueConversion ? this : _left;
				while (expr.IsValueConversion)
				{
					expr = expr.Value as ODataExpression;
				}
				if (!string.IsNullOrEmpty(expr.Reference))
				{
					if (expr.Reference.IndexOfAny(_propertySeperator) >= 0)
					{
						//skip child entity - may result in false positives
						return false;
					}
					var key = expr.Reference;
					if (key != null && !lookupColumns.ContainsKey(key))
					{
						lookupColumns.Add(key, _right);
					}
				}
				return true;

			default:
				if (IsValueConversion)
				{
					return (Value as ODataExpression).ExtractLookupColumns(lookupColumns);
				}
				else
				{
					return false;
				}
		}
	}

	internal bool HasTypeConstraint(string typeName)
	{
		if (_operator == ExpressionType.And)
		{
			return _left.HasTypeConstraint(typeName) || _right.HasTypeConstraint(typeName);
		}
		else if (Function != null && Function.FunctionName == ODataLiteral.IsOf)
		{
			return Function.Arguments.Last().HasTypeConstraint(typeName);
		}
		else if (Value != null)
		{
			return Value is Type && (Value as Type).Name == typeName;
		}
		else
		{
			return false;
		}
	}
}

public partial class ODataExpression<T> : ODataExpression

{
	public ODataExpression(Expression<Predicate<T>> predicate)
		: base(predicate)
	{
	}

	internal ODataExpression(ODataExpression expression)
		: base(expression)
	{
	}
}
