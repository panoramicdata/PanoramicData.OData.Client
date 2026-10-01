namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Fixture for tests that feed $metadata documents to the client: a client with metadata
/// caching disabled, and the helpers for stubbing a metadata response.
/// </summary>
public abstract class ODataMetadataTestBase : MockedODataClientTestBase
{
	/// <summary>
	/// Initializes the fixture with metadata caching disabled, so each test sees its own
	/// document rather than the previous test's.
	/// </summary>
	protected ODataMetadataTestBase()
		: base(options => options.MetadataCacheDuration = TimeSpan.Zero)
	{
	}

	/// <summary>
	/// Answers the next request with <paramref name="xml"/> as an application/xml $metadata document.
	/// </summary>
	protected void SetupMetadataResponse(string xml)
	{
		var response = new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent(xml, System.Text.Encoding.UTF8, "application/xml")
		};

		SetupResponse(response);
	}

	/// <summary>
	/// The edmx and edm namespace URIs, as constants rather than inline in the document below.
	/// Lizard, which measures this codebase, reads the "//" of a URL inside a raw string literal
	/// as the start of a line comment - it then swallows the closing delimiter and folds every
	/// following method into this one, reporting a 20-line test as a 700-line one.
	/// </summary>
	private const string EdmxNamespace = "http://docs.oasis-open.org/odata/ns/edmx";

	private const string EdmNamespace = "http://docs.oasis-open.org/odata/ns/edm";

	/// <summary>
	/// The schema namespace <see cref="CreateMetadataXml(string)"/> declares.
	/// </summary>
	protected const string DefaultSchemaNamespace = "Test";

	/// <summary>
	/// Wraps <paramref name="schemaContent"/> in the edmx envelope every $metadata document needs,
	/// so a test only has to state the schema fragment it is about.
	/// </summary>
	protected static string CreateMetadataXml(string schemaContent)
		=> CreateMetadataXml(schemaContent, DefaultSchemaNamespace);

	/// <summary>
	/// Wraps <paramref name="schemaContent"/> in the edmx envelope, under a chosen schema namespace.
	/// </summary>
	protected static string CreateMetadataXml(string schemaContent, string schemaNamespace) => $"""
		<?xml version="1.0" encoding="utf-8"?>
		<edmx:Edmx Version="4.0" xmlns:edmx="{EdmxNamespace}">
			<edmx:DataServices>
				<Schema Namespace="{schemaNamespace}" xmlns="{EdmNamespace}">
					{schemaContent}
				</Schema>
			</edmx:DataServices>
		</edmx:Edmx>
		""";
}
