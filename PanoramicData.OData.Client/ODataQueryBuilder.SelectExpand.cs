namespace PanoramicData.OData.Client;

/// <summary>
/// ODataQueryBuilder - select and expand.
/// </summary>
public partial class ODataQueryBuilder<T> where T : class
{
	/// <summary>
	/// Adds select fields from an expression.
	/// </summary>
	public ODataQueryBuilder<T> Select(Expression<Func<T, object?>> selector)
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
		ODataListText.AddCommaSeparated(_selectFields, fields);
		return this;
	}

	/// <summary>
	/// Adds expand fields from an expression.
	/// Supports nested expands via expressions like p => new { p.Parent, p.Parent!.Children }
	/// which produces $expand=Parent($expand=Children).
	/// Also supports scalar property access like p => p.Parent.Name which produces $expand=Parent($select=Name).
	/// </summary>
	public ODataQueryBuilder<T> Expand(Expression<Func<T, object?>> selector)
	{
		var pathInfos = GetExpandMemberPathsWithInfo(selector);
		var nestedExpands = BuildNestedExpandFieldsWithInfo(pathInfos);
		_expandFields.AddRange(nestedExpands);
		return this;
	}

	/// <summary>
	/// Adds expand fields from a comma-separated string.
	/// </summary>
	public ODataQueryBuilder<T> Expand(string fields)
	{
		ODataListText.AddCommaSeparated(_expandFields, fields);
		return this;
	}

	/// <summary>
	/// Expands a navigation property and selects specific scalar properties within it.
	/// Produces $expand=NavProperty($select=Prop1,Prop2,Prop3).
	/// </summary>
	/// <typeparam name="TNav">The type of the navigation property.</typeparam>
	/// <param name="navigationProperty">Expression selecting the navigation property to expand.</param>
	/// <param name="selectProperties">Expression selecting the scalar properties to include from the expanded entity.</param>
	/// <returns>This query builder for method chaining.</returns>
	/// <example>
	/// <code>
	/// // Produces: $expand=ReportSchedule($select=Id,Name,Description)
	/// query.ExpandWithSelect(
	///     x => x.ReportSchedule,
	///     rs => new { rs.Id, rs.Name, rs.Description });
	/// </code>
	/// </example>
	public ODataQueryBuilder<T> ExpandWithSelect<TNav>(
		Expression<Func<T, TNav?>> navigationProperty,
		Expression<Func<TNav, object?>> selectProperties) where TNav : class
	{
		var navPropertyName = GetNavigationPropertyName(navigationProperty);
		var selectedFields = GetSelectFieldNames(selectProperties);

		if (string.IsNullOrEmpty(navPropertyName) || selectedFields.Count == 0)
		{
			return this;
		}

		var expandClause = $"{navPropertyName}($select={string.Join(",", selectedFields)})";
		_expandFields.Add(expandClause);

		return this;
	}

	/// <summary>
	/// Expands a navigation property with nested expand and/or select options.
	/// </summary>
	/// <typeparam name="TNav">The type of the navigation property.</typeparam>
	/// <param name="navigationProperty">Expression selecting the navigation property to expand.</param>
	/// <param name="configure">Action to configure nested expand options.</param>
	/// <returns>This query builder for method chaining.</returns>
	/// <example>
	/// <code>
	/// // Produces: $expand=ReportSchedule($select=Id,Name;$expand=Owner)
	/// query.Expand(
	///     x => x.ReportSchedule,
	///     nested => nested
	///         .Select(rs => new { rs.Id, rs.Name })
	///         .Expand(rs => rs.Owner));
	/// </code>
	/// </example>
	public ODataQueryBuilder<T> Expand<TNav>(
		Expression<Func<T, TNav?>> navigationProperty,
		Action<NestedExpandBuilder<TNav>> configure) where TNav : class
	{
		ExpandNavigation(GetNavigationPropertyName(navigationProperty), configure);
		return this;
	}

	/// <summary>
	/// Expands a collection navigation property with nested expand and/or select options.
	/// </summary>
	/// <typeparam name="TNav">The element type of the collection navigation property.</typeparam>
	/// <param name="navigationProperty">Expression selecting the collection navigation property to expand.</param>
	/// <param name="configure">Action to configure nested expand options.</param>
	/// <returns>This query builder for method chaining.</returns>
	public ODataQueryBuilder<T> Expand<TNav>(
		Expression<Func<T, IEnumerable<TNav>?>> navigationProperty,
		Action<NestedExpandBuilder<TNav>> configure) where TNav : class
	{
		ExpandNavigation(GetCollectionNavigationPropertyName(navigationProperty), configure);
		return this;
	}

	private void ExpandNavigation<TNav>(string navPropertyName, Action<NestedExpandBuilder<TNav>> configure) where TNav : class
	{
		if (string.IsNullOrEmpty(navPropertyName))
		{
			return;
		}

		var nestedBuilder = new NestedExpandBuilder<TNav>();
		configure(nestedBuilder);

		var nestedOptions = nestedBuilder.Build();
		_expandFields.Add(string.IsNullOrEmpty(nestedOptions) ? navPropertyName : $"{navPropertyName}({nestedOptions})");
	}

	private static string GetNavigationPropertyName<TNav>(Expression<Func<T, TNav?>> selector) =>
		MemberPathResolver.GetLeafMemberNameOrEmpty(selector.Body);

	private static string GetCollectionNavigationPropertyName<TNav>(Expression<Func<T, IEnumerable<TNav>?>> selector) =>
		MemberPathResolver.GetLeafMemberNameOrEmpty(selector.Body);

	private static List<string> GetSelectFieldNames<TEntity>(Expression<Func<TEntity, object?>> selector)
	{
		var results = new List<string>();
		MemberPathResolver.AddSelectedNames(selector.Body, results);

		return results;
	}
}
