using System.Linq.Expressions;

namespace PanoramicData.OData.Client;

/// <summary>
/// Builder for nested expand options ($select, $expand, $filter, $orderby, $top, $skip within an expand).
/// </summary>
/// <typeparam name="T">The entity type being expanded.</typeparam>
public class NestedExpandBuilder<T> where T : class
{
	private readonly List<string> _selectFields = [];
	private readonly List<string> _expandFields = [];
	private readonly List<string> _filterClauses = [];
	private readonly List<string> _orderByClauses = [];
	private long? _top;
	private long? _skip;

	/// <summary>
	/// Selects specific properties from the expanded entity.
	/// </summary>
	public NestedExpandBuilder<T> Select(Expression<Func<T, object?>> selector)
	{
		var body = selector.Body;

		if (body is UnaryExpression unary && unary.NodeType == ExpressionType.Convert)
		{
			body = unary.Operand;
		}

		if (body is NewExpression newExpr)
		{
			foreach (var arg in newExpr.Arguments)
			{
				var memberName = GetDirectMemberName(arg);
				if (!string.IsNullOrEmpty(memberName) && !_selectFields.Contains(memberName))
				{
					_selectFields.Add(memberName);
				}
			}
		}
		else if (body is MemberExpression member)
		{
			_selectFields.Add(member.Member.Name);
		}

		return this;
	}

	/// <summary>
	/// Selects specific properties from the expanded entity using a comma-separated string.
	/// </summary>
	public NestedExpandBuilder<T> Select(string fields)
	{
		if (!string.IsNullOrWhiteSpace(fields))
		{
			_selectFields.AddRange(fields.Split(',').Select(f => f.Trim()));
		}

		return this;
	}

	/// <summary>
	/// Expands a navigation property within the expanded entity.
	/// </summary>
	public NestedExpandBuilder<T> Expand(Expression<Func<T, object?>> selector)
	{
		var body = selector.Body;

		if (body is UnaryExpression unary && unary.NodeType == ExpressionType.Convert)
		{
			body = unary.Operand;
		}

		if (body is MemberExpression member)
		{
			_expandFields.Add(member.Member.Name);
		}

		return this;
	}

	/// <summary>
	/// Expands navigation properties using a comma-separated string.
	/// </summary>
	public NestedExpandBuilder<T> Expand(string fields)
	{
		if (!string.IsNullOrWhiteSpace(fields))
		{
			_expandFields.AddRange(fields.Split(',').Select(f => f.Trim()));
		}

		return this;
	}

	/// <summary>
	/// Adds a filter to the expanded collection.
	/// </summary>
	public NestedExpandBuilder<T> Filter(string filter)
	{
		if (!string.IsNullOrWhiteSpace(filter))
		{
			_filterClauses.Add(filter);
		}

		return this;
	}

	/// <summary>
	/// Adds ordering to the expanded collection.
	/// </summary>
	public NestedExpandBuilder<T> OrderBy(string orderBy)
	{
		if (!string.IsNullOrWhiteSpace(orderBy))
		{
			_orderByClauses.Add(orderBy);
		}

		return this;
	}

	/// <summary>
	/// Limits the number of items in the expanded collection.
	/// </summary>
	public NestedExpandBuilder<T> Top(long count)
	{
		_top = count;
		return this;
	}

	/// <summary>
	/// Skips items in the expanded collection.
	/// </summary>
	public NestedExpandBuilder<T> Skip(long count)
	{
		_skip = count;
		return this;
	}

	/// <summary>
	/// Builds the nested options string.
	/// </summary>
	internal string Build()
	{
		var options = new List<string>();

		if (_selectFields.Count > 0)
		{
			options.Add($"$select={string.Join(",", _selectFields)}");
		}

		if (_expandFields.Count > 0)
		{
			options.Add($"$expand={string.Join(",", _expandFields)}");
		}

		if (_filterClauses.Count > 0)
		{
			var combinedFilter = string.Join(" and ", _filterClauses.Select(f => $"({f})"));
			options.Add($"$filter={combinedFilter}");
		}

		if (_orderByClauses.Count > 0)
		{
			options.Add($"$orderby={string.Join(",", _orderByClauses)}");
		}

		if (_top.HasValue)
		{
			options.Add($"$top={_top.Value}");
		}

		if (_skip.HasValue)
		{
			options.Add($"$skip={_skip.Value}");
		}

		return string.Join(";", options);
	}

	private static string GetDirectMemberName(Expression expression)
	{
		if (expression is UnaryExpression unary && unary.NodeType == ExpressionType.Convert)
		{
			expression = unary.Operand;
		}

		if (expression is MemberExpression member)
		{
			return member.Member.Name;
		}

		return string.Empty;
	}
}
