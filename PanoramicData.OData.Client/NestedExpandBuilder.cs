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
		MemberPathResolver.AddSelectedNames(selector.Body, _selectFields);

		return this;
	}

	/// <summary>
	/// Selects specific properties from the expanded entity using a comma-separated string.
	/// </summary>
	public NestedExpandBuilder<T> Select(string fields)
	{
		ODataListText.AddCommaSeparated(_selectFields, fields);

		return this;
	}

	/// <summary>
	/// Expands a navigation property within the expanded entity.
	/// </summary>
	public NestedExpandBuilder<T> Expand(Expression<Func<T, object?>> selector)
	{
		var memberName = MemberPathResolver.GetLeafMemberNameOrEmpty(selector.Body);

		if (!string.IsNullOrEmpty(memberName))
		{
			_expandFields.Add(memberName);
		}

		return this;
	}

	/// <summary>
	/// Expands navigation properties using a comma-separated string.
	/// </summary>
	public NestedExpandBuilder<T> Expand(string fields)
	{
		ODataListText.AddCommaSeparated(_expandFields, fields);

		return this;
	}

	/// <summary>
	/// Adds a filter to the expanded collection.
	/// </summary>
	public NestedExpandBuilder<T> Filter(string filterExpression)
	{
		ODataListText.AddIfNotBlank(_filterClauses, filterExpression);

		return this;
	}

	/// <summary>
	/// Adds ordering to the expanded collection.
	/// </summary>
	public NestedExpandBuilder<T> OrderBy(string orderByExpression)
	{
		ODataListText.AddIfNotBlank(_orderByClauses, orderByExpression);

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

		ODataQueryString.AddJoined(options, "$select", _selectFields);
		ODataQueryString.AddJoined(options, "$expand", _expandFields);

		if (_filterClauses.Count > 0)
		{
			var combinedFilter = string.Join(" and ", _filterClauses.Select(f => $"({f})"));
			options.Add($"$filter={combinedFilter}");
		}

		ODataQueryString.AddJoined(options, "$orderby", _orderByClauses);

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
}
