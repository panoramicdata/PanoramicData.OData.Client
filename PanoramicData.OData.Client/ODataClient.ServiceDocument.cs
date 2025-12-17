namespace PanoramicData.OData.Client;

/// <summary>
/// ODataClient - Service document operations.
/// </summary>
public partial class ODataClient
{
	/// <summary>
	/// Gets the OData service document from the service root.
	/// The service document lists all available entity sets, singletons, and function imports.
	/// </summary>
	/// <param name="headers">Optional additional headers.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The service document.</returns>
	public async Task<ODataServiceDocument> GetServiceDocumentAsync(
		IReadOnlyDictionary<string, string>? headers = null,
		CancellationToken cancellationToken = default)
	{
		LoggerMessages.GetServiceDocumentAsyncFetching(_logger);

		var request = CreateRequest(HttpMethod.Get, "", headers);

		var response = await SendWithRetryAsync(request, cancellationToken).ConfigureAwait(false);
		await EnsureSuccessAsync(response, "", cancellationToken).ConfigureAwait(false);

		var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

		LoggerMessages.GetServiceDocumentAsyncParsing(_logger, content.Length);

		return ParseServiceDocument(content);
	}

	private ODataServiceDocument ParseServiceDocument(string content)
	{
		using var doc = JsonDocument.Parse(content);
		var result = new ODataServiceDocument();

		if (doc.RootElement.TryGetProperty("@odata.context", out var contextElement))
		{
			result.Context = contextElement.GetString();
		}

		if (doc.RootElement.TryGetProperty("value", out var valueElement))
		{
			ParseServiceResources(valueElement, result);
		}

		LoggerMessages.ParseServiceDocumentComplete(_logger, result.Resources.Count);

		return result;
	}

	private void ParseServiceResources(JsonElement valueElement, ODataServiceDocument result)
	{
		foreach (var item in valueElement.EnumerateArray())
		{
			var resource = ParseServiceResource(item);
			result.Resources.Add(resource);
			LoggerMessages.ParseServiceDocumentResource(_logger, resource.Name, resource.Kind);
		}
	}

	private static ODataServiceResource ParseServiceResource(JsonElement item)
	{
		var resource = new ODataServiceResource();

		if (item.TryGetProperty("name", out var nameElement))
		{
			resource.Name = nameElement.GetString() ?? string.Empty;
		}

		if (item.TryGetProperty("kind", out var kindElement))
		{
			resource.Kind = ParseResourceKind(kindElement.GetString());
		}

		resource.Url = item.TryGetProperty("url", out var urlElement)
			? urlElement.GetString() ?? resource.Name
			: resource.Name;

		if (item.TryGetProperty("title", out var titleElement))
		{
			resource.Title = titleElement.GetString();
		}

		return resource;
	}

	private static ODataServiceResourceKind ParseResourceKind(string? kindStr) => kindStr switch
	{
		"Singleton" => ODataServiceResourceKind.Singleton,
		"FunctionImport" => ODataServiceResourceKind.FunctionImport,
		"ServiceDocument" => ODataServiceResourceKind.ServiceDocument,
		_ => ODataServiceResourceKind.EntitySet
	};
}
