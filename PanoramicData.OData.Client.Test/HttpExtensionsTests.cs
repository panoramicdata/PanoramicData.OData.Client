using PanoramicData.OData.Client;

namespace PanoramicData.OData.Client.Test;

/// <summary>
/// Tests for header redaction in diagnostic output.
///
/// <para>
/// <c>ODataClient</c> wrote caller-supplied header values into two places: one Debug line per header
/// as it was added, and the Trace request and response dumps. Any credential the caller attached to
/// a query was therefore written wherever those messages ended up.
/// </para>
///
/// <para>
/// This package applies no credential of its own, so the redaction cannot key on a scheme this
/// library controls. It matches a general set of names plus fragments, which is why the fragment
/// cases below matter more here than in the sibling packages.
/// </para>
///
/// <para>
/// These are pure unit tests requiring no credentials, configuration or live service.
/// </para>
/// </summary>
public class HttpExtensionsTests
{
	private const string Secret = "s3cr3t-value-that-must-not-be-logged";

	/// <summary>
	/// The standard credential-bearing header names are redacted.
	/// </summary>
	/// <param name="headerName">The header name under test.</param>
	[Theory]
	[InlineData("Authorization")]
	[InlineData("Proxy-Authorization")]
	[InlineData("Cookie")]
	[InlineData("Set-Cookie")]
	[InlineData("X-API-Key")]
	[InlineData("Api-Key")]
	[InlineData("X-Api-Token")]
	[InlineData("X-Auth-Token")]
	public void RedactIfSensitive_StandardCredentialHeaders_AreRedacted(string headerName)
	{
		var redacted = HttpExtensions.RedactIfSensitive(headerName, Secret);

		redacted.Should().NotContain(Secret);
		redacted.Should().Contain("<redacted");
	}

	/// <summary>
	/// The vendor-specific names found across the sibling packages during the MS-25680 sweep. Callers
	/// choose the header names here, so an exact-match list alone could not cover them.
	/// </summary>
	/// <param name="headerName">The vendor-specific header name under test.</param>
	[Theory]
	[InlineData("X-Samanage-Authorization")]
	[InlineData("X-Cisco-Meraki-API-Key")]
	[InlineData("DD-API-KEY")]
	[InlineData("DD-APPLICATION-KEY")]
	[InlineData("ApiIntegrationCode")]
	[InlineData("Secret")]
	[InlineData("Password")]
	public void RedactIfSensitive_VendorCredentialHeaders_AreRedacted(string headerName)
	{
		var redacted = HttpExtensions.RedactIfSensitive(headerName, Secret);

		redacted.Should().NotContain(Secret);
		redacted.Should().Contain("<redacted");
	}

	/// <summary>
	/// A name the sweep has not seen, but following the same conventions, is still caught.
	/// </summary>
	/// <param name="headerName">The unfamiliar but credential-shaped header name under test.</param>
	[Theory]
	[InlineData("X-Acme-ApiKey")]
	[InlineData("My-Auth-Token")]
	[InlineData("Client-Secret")]
	[InlineData("X-Vendor-Credential")]
	public void RedactIfSensitive_UnfamiliarCredentialShapedNames_AreRedacted(string headerName)
	{
		var redacted = HttpExtensions.RedactIfSensitive(headerName, Secret);

		redacted.Should().NotContain(Secret);
		redacted.Should().Contain("<redacted");
	}

	/// <summary>
	/// Matching is case-insensitive, since callers supply the names verbatim.
	/// </summary>
	/// <param name="headerName">The header name casing under test.</param>
	[Theory]
	[InlineData("authorization")]
	[InlineData("AUTHORIZATION")]
	[InlineData("x-api-key")]
	[InlineData("X-API-KEY")]
	public void RedactIfSensitive_IsCaseInsensitive(string headerName)
	{
		var redacted = HttpExtensions.RedactIfSensitive(headerName, Secret);

		redacted.Should().NotContain(Secret);
		redacted.Should().Contain("<redacted");
	}

	/// <summary>
	/// Where a scheme is present it is kept, because knowing which mechanism was used aids diagnosis.
	/// </summary>
	[Fact]
	public void RedactIfSensitive_BearerToken_KeepsTheSchemeAndLength()
	{
		const string token = "abcdefghijklmnopqrstuvwxyz0123456789";

		var redacted = HttpExtensions.RedactIfSensitive("Authorization", $"Bearer {token}");

		redacted.Should().Be($"Bearer <redacted, length {token.Length}>");
		redacted.Should().NotContain(token);
	}

	/// <summary>
	/// A cookie value also contains a space, so treating the text before the first space as a scheme
	/// would preserve the very value being redacted. Only Authorization style headers keep a scheme.
	/// </summary>
	[Fact]
	public void RedactIfSensitive_CookieValueContainingASpace_IsRedactedWhole()
	{
		const string cookie = "session=abc123def456; HttpOnly";

		var redacted = HttpExtensions.RedactIfSensitive("Cookie", cookie);

		redacted.Should().Be($"<redacted, length {cookie.Length}>");
		redacted.Should().NotContain("session=abc");
	}

	/// <summary>
	/// A credential with no scheme prefix has nothing safe to preserve, so all of it goes.
	/// </summary>
	[Fact]
	public void RedactIfSensitive_CredentialWithoutAScheme_IsRedactedEntirely()
	{
		var redacted = HttpExtensions.RedactIfSensitive("X-API-Key", "abcdef123456");

		redacted.Should().Be("<redacted, length 12>");
	}

	/// <summary>
	/// Ordinary headers must survive untouched, or the logs stop being worth reading.
	/// </summary>
	/// <param name="headerName">The non-credential header name under test.</param>
	[Theory]
	[InlineData("Accept")]
	[InlineData("Content-Type")]
	[InlineData("OData-Version")]
	[InlineData("If-Match")]
	[InlineData("Prefer")]
	[InlineData("User-Agent")]
	[InlineData("traceparent")]
	public void RedactIfSensitive_OrdinaryHeaders_AreUnchanged(string headerName)
	{
		const string value = "application/json;odata.metadata=minimal";

		HttpExtensions.RedactIfSensitive(headerName, value).Should().Be(value);
	}

	/// <summary>
	/// An empty value has nothing to redact and must not gain a misleading marker.
	/// </summary>
	[Fact]
	public void RedactIfSensitive_EmptyValue_IsUnchanged()
	{
		HttpExtensions.RedactIfSensitive("Authorization", string.Empty).Should().BeEmpty();
	}

	/// <summary>
	/// A header carrying several values is joined before redaction, so no part of any value escapes.
	/// </summary>
	[Fact]
	public void RedactIfSensitive_MultipleValues_AreRedactedTogether()
	{
		string[] values = ["first-secret-value", "second-secret-value"];

		var redacted = HttpExtensions.RedactIfSensitive("X-Api-Token", values);

		redacted.Should().NotContain("first-secret-value");
		redacted.Should().NotContain("second-secret-value");
		redacted.Should().Contain("<redacted");
	}

	/// <summary>
	/// A non-credential header carrying several values is joined and left alone.
	/// </summary>
	[Fact]
	public void RedactIfSensitive_MultipleOrdinaryValues_AreJoinedUnchanged()
	{
		string[] values = ["gzip", "deflate"];

		HttpExtensions.RedactIfSensitive("Accept-Encoding", values).Should().Be("gzip, deflate");
	}
}
