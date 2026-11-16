namespace PanoramicData.OData.Client;

/// <summary>
/// URL construction for <see cref="ODataQueryBuilder{T}"/>: turning the accumulated
/// clauses into a relative OData URL.
/// </summary>
public partial class ODataQueryBuilder<T> where T : class
{
	/// <summary>
	/// Builds the relative URL for this query.
	/// </summary>
	public string BuildUrl()
	{
		LoggerMessages.QueryBuilderBuildUrl(_logger, typeof(T).Name, _entitySet);

		var sb = new StringBuilder();
		sb.Append(_entitySet);

		AppendDerivedTypeToUrl(sb);
		AppendKeyToUrl(sb);
		AppendFunctionToUrl(sb);
		AppendQueryString(sb);

		var url = sb.ToString();
		LoggerMessages.QueryBuilderFinalUrl(_logger, typeof(T).Name, url);

		return url;
	}

	private void AppendDerivedTypeToUrl(StringBuilder sb)
	{
		if (string.IsNullOrEmpty(_derivedType))
		{
			return;
		}

		sb.Append('/');
		sb.Append(_derivedType);
		LoggerMessages.QueryBuilderDerivedType(_logger, _derivedType);
	}

	private void AppendKeyToUrl(StringBuilder sb)
	{
		if (_key is null)
		{
			return;
		}

		sb.Append('(');
		sb.Append(ODataLiteral.FormatKey(_key));
		sb.Append(')');
		LoggerMessages.QueryBuilderKey(_logger, _key);
	}

	private void AppendFunctionToUrl(StringBuilder sb)
	{
		if (string.IsNullOrEmpty(_function))
		{
			return;
		}

		sb.Append('/');
		sb.Append(_function);
		LoggerMessages.QueryBuilderFunction(_logger, _function);

		sb.Append('(');
		if (_functionParameters is not null)
		{
			sb.Append(ODataExpressionTranslator.FormatFunctionParameters(_functionParameters));
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

		AppendFilterParameter(queryParams);
		AppendSearchParameter(queryParams);
		AppendSelectParameter(queryParams);
		AppendExpandParameter(queryParams);
		AppendOrderByParameter(queryParams);
		AppendSkipParameter(queryParams);
		AppendTopParameter(queryParams);
		AppendCountParameter(queryParams);
		AppendApplyParameter(queryParams);
		AppendComputeParameter(queryParams);
		AppendRawQueryOptions(queryParams);

		return queryParams;
	}

	private void AppendFilterParameter(List<string> queryParams)
	{
		if (ODataQueryString.AddFilter(queryParams, _filterClauses) is { } filter)
		{
			LoggerMessages.QueryBuilderFilter(_logger, filter);
		}
	}

	private void AppendSearchParameter(List<string> queryParams)
	{
		if (ODataQueryString.AddEscaped(queryParams, "$search", _search) is { } search)
		{
			LoggerMessages.QueryBuilderSearch(_logger, search);
		}
	}

	private void AppendSelectParameter(List<string> queryParams)
	{
		if (ODataQueryString.AddJoined(queryParams, "$select", _selectFields) is { } select)
		{
			LoggerMessages.QueryBuilderSelect(_logger, select);
		}
	}

	private void AppendExpandParameter(List<string> queryParams)
	{
		if (ODataQueryString.AddJoined(queryParams, "$expand", _expandFields) is { } expand)
		{
			LoggerMessages.QueryBuilderExpand(_logger, expand);
		}
	}

	private void AppendOrderByParameter(List<string> queryParams)
	{
		if (ODataQueryString.AddJoined(queryParams, "$orderby", _orderByClauses) is { } orderBy)
		{
			LoggerMessages.QueryBuilderOrderBy(_logger, orderBy);
		}
	}

	private void AppendSkipParameter(List<string> queryParams)
	{
		if (!_skip.HasValue)
		{
			return;
		}

		queryParams.Add($"$skip={_skip.Value}");
		LoggerMessages.QueryBuilderSkip(_logger, _skip.Value);
	}

	private void AppendTopParameter(List<string> queryParams)
	{
		if (!_top.HasValue)
		{
			return;
		}

		queryParams.Add($"$top={_top.Value}");
		LoggerMessages.QueryBuilderTop(_logger, _top.Value);
	}

	private void AppendCountParameter(List<string> queryParams)
	{
		if (!_count)
		{
			return;
		}

		queryParams.Add("$count=true");
		LoggerMessages.QueryBuilderCount(_logger);
	}

	private void AppendApplyParameter(List<string> queryParams)
	{
		if (ODataQueryString.AddEscaped(queryParams, "$apply", _apply) is { } apply)
		{
			LoggerMessages.QueryBuilderApply(_logger, apply);
		}
	}

	private void AppendComputeParameter(List<string> queryParams)
	{
		if (_computeExpressions.Count <= 0)
		{
			return;
		}

		var computeClause = string.Join(",", _computeExpressions);
		queryParams.Add($"$compute={Uri.EscapeDataString(computeClause)}");
		LoggerMessages.QueryBuilderCompute(_logger, computeClause);
	}

	private void AppendRawQueryOptions(List<string> queryParams)
	{
		foreach (var option in _rawQueryOptions)
		{
			queryParams.Add(option);
		}
	}
}
