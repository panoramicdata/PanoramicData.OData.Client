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

		AppendKeySegment(sb);
		AppendFunctionSegment(sb);
		AppendQueryString(sb);

		return sb.ToString();
	}

	private void AppendKeySegment(StringBuilder sb)
	{
		if (_key is null)
		{
			return;
		}

		sb.Append('(');
		sb.Append(ODataLiteral.FormatKey(_key));
		sb.Append(')');
	}

	private void AppendFunctionSegment(StringBuilder sb)
	{
		if (string.IsNullOrEmpty(_function))
		{
			return;
		}

		sb.Append('/');
		sb.Append(_function);
		sb.Append('(');
		if (_functionParameters is not null)
		{
			sb.Append(FormatFunctionParameters(_functionParameters));
		}

		sb.Append(')');
	}

	private void AppendQueryString(StringBuilder sb)
	{
		var queryParams = BuildQueryParameters();

		if (queryParams.Count > 0)
		{
			sb.Append('?');
			sb.Append(string.Join("&", queryParams));
		}
	}

	private List<string> BuildQueryParameters()
	{
		var queryParams = new List<string>();

		ODataQueryString.AddFilter(queryParams, _filterClauses);
		ODataQueryString.AddEscaped(queryParams, "$search", _search);
		ODataQueryString.AddJoined(queryParams, "$select", _selectFields);
		ODataQueryString.AddJoined(queryParams, "$expand", _expandFields);
		ODataQueryString.AddJoined(queryParams, "$orderby", _orderByClauses);
		AddPagingParameters(queryParams);
		ODataQueryString.AddEscaped(queryParams, "$apply", _apply);
		AddRawQueryOptions(queryParams);

		return queryParams;
	}

	private void AddPagingParameters(List<string> queryParams)
	{
		if (_skip.HasValue)
		{
			queryParams.Add($"$skip={_skip.Value}");
		}

		if (_top.HasValue)
		{
			queryParams.Add($"$top={_top.Value}");
		}

		if (_count)
		{
			queryParams.Add("$count=true");
		}
	}

	private void AddRawQueryOptions(List<string> queryParams)
	{
		if (_rawQueryOptions.Count > 0)
		{
			foreach (var option in _rawQueryOptions)
			{
				queryParams.Add(option);
			}
		}
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
