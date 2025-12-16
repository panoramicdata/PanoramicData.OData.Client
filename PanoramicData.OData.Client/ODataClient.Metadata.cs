using Microsoft.Extensions.Logging;

namespace PanoramicData.OData.Client;

/// <summary>
/// ODataClient - Metadata operations.
/// </summary>
public partial class ODataClient
{
	/// <summary>
	/// Gets the service metadata (CSDL) from the $metadata endpoint.
	/// </summary>
	/// <param name="headers">Optional additional headers.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The parsed metadata.</returns>
	public async Task<ODataMetadata> GetMetadataAsync(
		IReadOnlyDictionary<string, string>? headers = null,
		CancellationToken cancellationToken = default)
	{
		_logger.LogDebug("GetMetadataAsync - Fetching metadata from $metadata");

		var request = CreateRequest(HttpMethod.Get, "$metadata", headers);
		request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/xml"));

		var response = await SendWithRetryAsync(request, cancellationToken).ConfigureAwait(false);
		await EnsureSuccessAsync(response, "$metadata", cancellationToken).ConfigureAwait(false);

		var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

		_logger.LogDebug("GetMetadataAsync - Parsing metadata, content length: {Length}", content.Length);

		return ODataMetadataParser.Parse(content);
	}
}
