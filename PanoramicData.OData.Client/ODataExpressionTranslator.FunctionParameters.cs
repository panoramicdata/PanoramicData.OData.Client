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
		Enum e => $"'{e}'",
		System.Collections.IEnumerable enumerable => FormatSequence(enumerable),
		_ => FormatScalar(value)
	};

	private static string FormatArrayElementValue(object? value) => value switch
	{
		null => "null",
		string s => s,
		_ => FormatScalar(value)
	};

	private static string FormatScalar(object value) => value switch
	{
		bool b => b.ToString().ToLowerInvariant(),
		int i => i.ToString(CultureInfo.InvariantCulture),
		long l => l.ToString(CultureInfo.InvariantCulture),
		double d => d.ToString(CultureInfo.InvariantCulture),
		decimal dec => dec.ToString(CultureInfo.InvariantCulture),
		DateTime dt => FormatDateTime(dt),
		DateTimeOffset dto => $"{dto.UtcDateTime:yyyy-MM-ddTHH:mm:ssZ}",
		Guid g => g.ToString(),
		_ => value.ToString() ?? "null"
	};

	private static string FormatSequence(System.Collections.IEnumerable items)
		=> $"[{string.Join(",", items.Cast<object?>().Select(FormatArrayElementValue))}]";
}
