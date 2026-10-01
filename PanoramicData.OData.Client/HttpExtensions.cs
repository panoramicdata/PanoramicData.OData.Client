namespace PanoramicData.OData.Client;

/// <summary>
/// Redaction of credential-bearing HTTP header values for diagnostic output.
/// </summary>
/// <remarks>
/// <para>
/// This client differs from the sibling API packages in an important way: it applies no credential
/// of its own. Headers arrive from the caller, through <c>ODataQueryBuilder</c> custom headers and
/// through <c>ODataClientOptions.ConfigureRequest</c>, so the credential could be named anything.
/// The list below is therefore a general one rather than a scheme this library controls, and the
/// name test is deliberately broad.
/// </para>
/// <para>
/// This is only for logging. Header rendering that forms part of a request on the wire, such as the
/// batch payload built by <c>HttpMessageContent</c>, must never be redacted, or the request itself
/// would be corrupted.
/// </para>
/// </remarks>
internal static class HttpExtensions
{
	/// <summary>
	/// Header names whose values carry a credential and must never be rendered into a log message or
	/// an exception message.
	/// </summary>
	private static readonly HashSet<string> SensitiveHeaderNames = new(StringComparer.OrdinalIgnoreCase)
	{
		"Authorization",
		"Proxy-Authorization",
		"Cookie",
		"Set-Cookie",
		"X-API-Key",
		"Api-Key",
		"X-Api-Token",
		"X-Auth-Token",
		"X-Cisco-Meraki-API-Key",
		"DD-API-KEY",
		"DD-APPLICATION-KEY",
		"Secret",
		"Password",
		// AutoTask sends this alongside Secret. It matches none of the fragments below, which is
		// exactly why the exact-match list is kept as well as the fragment test.
		"ApiIntegrationCode",
	};

	/// <summary>
	/// Name fragments that mark a header as credential-bearing wherever they appear in the name.
	/// </summary>
	/// <remarks>
	/// Because callers choose the header names here, an exact-match list cannot be complete. These
	/// fragments catch vendor-specific names that follow the usual conventions, such as
	/// "X-Samanage-Authorization". They are not sufficient on their own: AutoTask's
	/// "ApiIntegrationCode" contains none of them, which is why the exact-match list above is kept
	/// as well. A unit test covers that case. Over-redacting a header that merely mentions a token
	/// is a far cheaper mistake than printing a credential.
	/// </remarks>
	private static readonly string[] SensitiveNameFragments =
	[
		"authorization",
		"apikey",
		"api-key",
		"apitoken",
		"api-token",
		"authtoken",
		"auth-token",
		"secret",
		"password",
		"credential",
	];

	/// <summary>
	/// The subset of sensitive headers whose value is of the form "&lt;scheme&gt; &lt;credential&gt;",
	/// where the scheme is safe to keep and useful to see.
	/// </summary>
	private static readonly HashSet<string> SchemePrefixedHeaderNames = new(StringComparer.OrdinalIgnoreCase)
	{
		"Authorization",
		"Proxy-Authorization",
	};

	/// <summary>
	/// Whether a header name denotes a credential-bearing header.
	/// </summary>
	internal static bool IsSensitive(string name)
	{
		if (SensitiveHeaderNames.Contains(name))
		{
			return true;
		}

		foreach (var fragment in SensitiveNameFragments)
		{
			if (name.Contains(fragment, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Whether a header's grammar is "&lt;scheme&gt; &lt;credential&gt;", so its scheme can be kept.
	/// </summary>
	private static bool IsSchemePrefixed(string name)
		=> SchemePrefixedHeaderNames.Contains(name)
		|| name.EndsWith("Authorization", StringComparison.OrdinalIgnoreCase);

	/// <summary>
	/// Replaces the value with a redaction marker when the header is a credential-bearing one.
	/// </summary>
	/// <remarks>
	/// The authentication scheme and the credential length are preserved. That is enough to tell an
	/// engineer that a credential was sent and roughly what shape it had, which is all diagnosis needs,
	/// without writing the credential itself somewhere it will be retained and widely readable.
	/// </remarks>
	/// <param name="name">The header name.</param>
	/// <param name="value">The header value, already joined if the header had several.</param>
	/// <returns>The value, or a redaction marker in its place.</returns>
	internal static string RedactIfSensitive(string name, string value)
	{
		if (string.IsNullOrEmpty(value) || !IsSensitive(name))
		{
			return value;
		}

		// Only headers whose grammar is "<scheme> <credential>" keep their scheme, so that which
		// authentication mechanism was used remains visible. Applying this to any header containing a
		// space would be unsafe: a cookie such as "session=abc123; HttpOnly" also contains one, and
		// treating the text before it as a scheme would preserve the very value being redacted.
		if (IsSchemePrefixed(name))
		{
			var schemeLength = value.IndexOf(' ', StringComparison.Ordinal);

			if (schemeLength > 0)
			{
				return $"{value[..schemeLength]} <redacted, length {value.Length - schemeLength - 1}>";
			}
		}

		return $"<redacted, length {value.Length}>";
	}

	/// <summary>
	/// Joins a header's values and redacts them if the header is a credential-bearing one.
	/// </summary>
	/// <param name="name">The header name.</param>
	/// <param name="values">The header values.</param>
	/// <returns>The joined values, or a redaction marker in their place.</returns>
	internal static string RedactIfSensitive(string name, IEnumerable<string> values)
		=> RedactIfSensitive(name, string.Join(", ", values));
}
