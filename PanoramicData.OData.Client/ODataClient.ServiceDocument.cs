using Microsoft.Extensions.Logging;
using System.Text.Json;

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

/// <summary>
/// Represents an OData service document.
/// </summary>
public class ODataServiceDocument
{
	/// <summary>
	/// Gets or sets the @odata.context URL.
	/// </summary>
	public string? Context { get; set; }

	/// <summary>
	/// Gets the list of available resources (entity sets, singletons, function imports).
	/// </summary>
	public List<ODataServiceResource> Resources { get; } = [];

	/// <summary>
	/// Gets all entity sets from the service document.
	/// </summary>
	public IEnumerable<ODataServiceResource> EntitySets
		=> Resources.Where(r => r.Kind == ODataServiceResourceKind.EntitySet);

	/// <summary>
	/// Gets all singletons from the service document.
	/// </summary>
	public IEnumerable<ODataServiceResource> Singletons
		=> Resources.Where(r => r.Kind == ODataServiceResourceKind.Singleton);

	/// <summary>
	/// Gets all function imports from the service document.
	/// </summary>
	public IEnumerable<ODataServiceResource> FunctionImports
		=> Resources.Where(r => r.Kind == ODataServiceResourceKind.FunctionImport);

	/// <summary>
	/// Gets a resource by name.
	/// </summary>
	/// <param name="name">The resource name.</param>
	/// <returns>The resource, or null if not found.</returns>
	public ODataServiceResource? GetResource(string name)
		=> Resources.FirstOrDefault(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
}

/// <summary>
/// Represents a resource in an OData service document.
/// </summary>
public class ODataServiceResource
{
	/// <summary>
	/// Gets or sets the resource name.
	/// </summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the resource kind.
	/// </summary>
	public ODataServiceResourceKind Kind { get; set; } = ODataServiceResourceKind.EntitySet;

	/// <summary>
	/// Gets or sets the URL to access this resource.
	/// </summary>
	public string Url { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the human-readable title (optional).
	/// </summary>
	public string? Title { get; set; }
}

/// <summary>
/// The kind of resource in an OData service document.
/// </summary>
public enum ODataServiceResourceKind
{
	/// <summary>
	/// An entity set (collection of entities).
	/// </summary>
	EntitySet,

	/// <summary>
	/// A singleton (single entity instance).
	/// </summary>
	Singleton,

	/// <summary>
	/// A function import (unbound function).
	/// </summary>
	FunctionImport,

	/// <summary>
	/// A nested service document.
	/// </summary>
	ServiceDocument
}
