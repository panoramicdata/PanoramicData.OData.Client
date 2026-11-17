using System.Reflection;

namespace PanoramicData.OData.Client;

/// <summary>
/// Formatting of OData literal values, per the OData ABNF for primitive literals.
/// </summary>
internal static class ODataLiteral
{
	private const string SingleQuote = "'";
	private const string EscapedSingleQuote = "''";

	/// <summary>
	/// Renders a key value for use inside the parentheses of a resource URL.
	/// </summary>
	internal static string FormatKey(object? key) => key switch
	{
		int i => i.ToString(CultureInfo.InvariantCulture),
		long l => l.ToString(CultureInfo.InvariantCulture),
		Guid g => g.ToString(),
		string s => Quote(s),
		null => throw new ArgumentException("Invalid key value"),
		IDictionary<string, object?> d => FormatCompositeKey(d.Select(kv => (kv.Key, kv.Value))),
		_ when IsCompositeKeyObject(key) => FormatCompositeKey(
			key.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
				.Where(p => p.GetIndexParameters().Length == 0)
				.Select(p => (p.Name, p.GetValue(key)))),
		_ => key.ToString() ?? throw new ArgumentException("Invalid key value")
	};

	// Anonymous types and POCOs; types with a meaningful ToString (primitives, dates, enums) stay single keys.
	private static bool IsCompositeKeyObject(object key)
	{
		var type = key.GetType();
		if (type.IsPrimitive || type.IsEnum || type.Namespace == nameof(System))
		{
			return false;
		}

		return type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Length > 1;
	}

	private static string FormatCompositeKey(IEnumerable<(string Name, object? Value)> parts)
		=> string.Join(",", parts.Select(part => string.Concat(part.Name, "=", FormatKeyPart(part.Value))));

	private static string FormatKeyPart(object? value) => value switch
	{
		bool b => b ? "true" : "false",
		DateTime dt => dt.ToString("yyyy-MM-dd'T'HH:mm:ss.FFFFFFFK", CultureInfo.InvariantCulture),
		DateTimeOffset dto => dto.ToString("yyyy-MM-dd'T'HH:mm:ss.FFFFFFFK", CultureInfo.InvariantCulture),
		IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
		_ => FormatKey(value)
	};

	/// <summary>
	/// Renders a string as an OData string literal: wrapped in single quotes, with any
	/// embedded single quote doubled.
	/// </summary>
	/// <remarks>
	/// Written with <see cref="string.Concat(string, string, string)"/> rather than
	/// interpolation because an interpolation holding a quote-bearing literal defeats the
	/// tokenizer Lizard uses to measure this codebase, which then attributes every following
	/// method's lines to whichever method contains it.
	/// </remarks>
	internal static string Quote(string value)
		=> string.Concat(
			SingleQuote,
			value.Replace(SingleQuote, EscapedSingleQuote, StringComparison.Ordinal),
			SingleQuote);
}
