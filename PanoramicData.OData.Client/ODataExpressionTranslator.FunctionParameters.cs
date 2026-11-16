using System.Reflection;

namespace PanoramicData.OData.Client;

/// <summary>
/// Function parameter formatting for ODataQueryBuilder.
/// </summary>
internal static partial class ODataExpressionTranslator
{
	internal static string FormatFunctionParameters(object parameters)
	{
		var type = parameters.GetType();

		// Get or create cached PropertyInfo array
		var props = ODataExpressionLookups.PropertyCache.GetValue(type, t =>
			t.GetProperties(BindingFlags.Public | BindingFlags.Instance));

		var paramStrings = props
			.Select(p => (Name: p.Name, Value: p.GetValue(parameters)))
			.Where(p => p.Value is not null)
			.Select(p =>
			{
				var formattedValue = FormatFunctionParameterValue(p.Value);
				return $"{char.ToLowerInvariant(p.Name[0])}{p.Name[1..]}={formattedValue}";
			});

		return string.Join(",", paramStrings);
	}

	private static string FormatFunctionParameterValue(object? value) => value switch
	{
		null => "null",
		string s => ODataLiteral.Quote(s),
		bool b => b.ToString().ToLowerInvariant(),
		int i => i.ToString(CultureInfo.InvariantCulture),
		long l => l.ToString(CultureInfo.InvariantCulture),
		double d => d.ToString(CultureInfo.InvariantCulture),
		decimal dec => dec.ToString(CultureInfo.InvariantCulture),
		DateTime dt => FormatDateTime(dt),
		DateTimeOffset dto => $"{dto.UtcDateTime:yyyy-MM-ddTHH:mm:ssZ}",
		Guid g => g.ToString(),
		Enum e => $"'{e}'",
		Array arr => FormatArrayParameter(arr),
		System.Collections.IEnumerable enumerable => FormatEnumerableParameter(enumerable),
		_ => value?.ToString() ?? "null"
	};

	private static string FormatArrayElementValue(object? value) => value switch
	{
		null => "null",
		string s => s,
		bool b => b.ToString().ToLowerInvariant(),
		int i => i.ToString(CultureInfo.InvariantCulture),
		long l => l.ToString(CultureInfo.InvariantCulture),
		double d => d.ToString(CultureInfo.InvariantCulture),
		decimal dec => dec.ToString(CultureInfo.InvariantCulture),
		DateTime dt => FormatDateTime(dt),
		DateTimeOffset dto => $"{dto.UtcDateTime:yyyy-MM-ddTHH:mm:ssZ}",
		Guid g => g.ToString(),
		Enum e => e.ToString(),
		_ => value?.ToString() ?? "null"
	};

	private static string FormatArrayParameter(Array arr)
	{
		var elements = new List<string>();
		foreach (var item in arr)
		{
			elements.Add(FormatArrayElementValue(item));
		}

		return $"[{string.Join(",", elements)}]";
	}

	private static string FormatEnumerableParameter(System.Collections.IEnumerable enumerable)
	{
		var elements = new List<string>();
		foreach (var item in enumerable)
		{
			elements.Add(FormatArrayElementValue(item));
		}

		return $"[{string.Join(",", elements)}]";
	}
}
