namespace PanoramicData.OData.Client;

/// <summary>
/// FluentODataQueryBuilder - URL building.
/// </summary>
public partial class FluentODataQueryBuilder
{

	/// <summary>
	/// Builds the relative URL for this query.
	/// </summary>
	/// <returns>The OData query URL.</returns>
	public string BuildUrl()
	{
		var sb = new StringBuilder();
		sb.Append(_entitySet);

		ODataQueryString.AppendKey(sb, _key);
		ODataQueryString.AppendFunction(sb, _function, _functionParameters is null ? null : FormatFunctionParameters(_functionParameters));
		ODataQueryString.AppendQueryString(sb, BuildQueryParameters());

		return sb.ToString();
	}

	private List<string> BuildQueryParameters()
	{
		var queryParams = new List<string>();

		ODataQueryString.AddFilter(queryParams, _filterClauses);
		ODataQueryString.AddEscaped(queryParams, "$search", _search);
		ODataQueryString.AddJoined(queryParams, "$select", _selectFields);
		ODataQueryString.AddJoined(queryParams, "$expand", _expandFields);
		ODataQueryString.AddJoined(queryParams, "$orderby", _orderByClauses);
		ODataQueryString.AddNumber(queryParams, "$skip", _skip);
		ODataQueryString.AddNumber(queryParams, "$top", _top);
		ODataQueryString.AddCount(queryParams, _count);
		ODataQueryString.AddEscaped(queryParams, "$apply", _apply);
		queryParams.AddRange(_rawQueryOptions);

		return queryParams;
	}

	private static string FormatFunctionParameters(object parameters)
	{
		var props = parameters.GetType().GetProperties();
		var parts = new List<string>();

		foreach (var prop in props)
		{
			var value = prop.GetValue(parameters);
			var formattedValue = value switch
			{
				null => "null",
				string s => ODataLiteral.Quote(s),
				bool b => b.ToString().ToLowerInvariant(),
				DateTime dt => dt.ToString("O", CultureInfo.InvariantCulture),
				DateTimeOffset dto => dto.ToString("O", CultureInfo.InvariantCulture),
				_ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "null"
			};

			parts.Add($"{prop.Name}={formattedValue}");
		}

		return string.Join(",", parts);
	}

}
