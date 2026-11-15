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
		_ => key?.ToString() ?? throw new ArgumentException("Invalid key value")
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
