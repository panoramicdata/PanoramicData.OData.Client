namespace PanoramicData.OData.Client;

/// <summary>
/// Shared construction of query string options. Each method returns the option's value when it
/// added one, or null when there was nothing to add.
/// </summary>
internal static class ODataQueryString
{
	internal static string? AddFilter(List<string> target, List<string> clauses)
	{
		if (clauses.Count == 0)
		{
			return null;
		}

		var combined = string.Join(" and ", clauses.Select(f => $"({f})"));
		target.Add($"$filter={Uri.EscapeDataString(combined)}");
		return combined;
	}

	internal static string? AddEscaped(List<string> target, string name, string? text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}

		target.Add($"{name}={Uri.EscapeDataString(text)}");
		return text;
	}

	internal static string? AddJoined(List<string> target, string name, List<string> values)
	{
		if (values.Count == 0)
		{
			return null;
		}

		var joined = string.Join(",", values);
		target.Add($"{name}={joined}");
		return joined;
	}

	internal static bool AppendKey(StringBuilder sb, object? key)
	{
		if (key is null)
		{
			return false;
		}

		sb.Append('(').Append(ODataLiteral.FormatKey(key)).Append(')');
		return true;
	}

	internal static bool AppendFunction(StringBuilder sb, string? function, string? formattedParameters)
	{
		if (string.IsNullOrEmpty(function))
		{
			return false;
		}

		sb.Append('/').Append(function).Append('(').Append(formattedParameters).Append(')');
		return true;
	}

	internal static void AppendQueryString(StringBuilder sb, List<string> queryParams)
	{
		if (queryParams.Count > 0)
		{
			sb.Append('?').Append(string.Join("&", queryParams));
		}
	}

	internal static long? AddNumber(List<string> target, string name, long? value)
	{
		if (value.HasValue)
		{
			target.Add($"{name}={value.Value}");
		}

		return value;
	}

	internal static bool AddCount(List<string> target, bool count)
	{
		if (count)
		{
			target.Add("$count=true");
		}

		return count;
	}
}
