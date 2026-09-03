namespace PanoramicData.OData.Client;

/// <summary>
/// Formatting of OData literal values, per the OData ABNF for primitive literals.
/// </summary>
internal static class ODataLiteral
{
	private const string SingleQuote = "'";
	private const string EscapedSingleQuote = "''";

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
