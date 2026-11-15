namespace PanoramicData.OData.Client;

/// <summary>
/// ODataClient - shared request/response plumbing.
/// </summary>
public partial class ODataClient
{
	private async Task<HttpResponseMessage> SendAndEnsureSuccessAsync(
		HttpRequestMessage request,
		string url,
		CancellationToken cancellationToken)
	{
		var response = await SendWithRetryAsync(request, cancellationToken).ConfigureAwait(false);
		await EnsureSuccessAsync(response, url, cancellationToken).ConfigureAwait(false);
		return response;
	}

	private Task<HttpResponseMessage> SendRequestAsync(
		HttpMethod method,
		string url,
		IReadOnlyDictionary<string, string>? headers,
		CancellationToken cancellationToken)
		=> SendAndEnsureSuccessAsync(CreateRequest(method, url, headers), url, cancellationToken);

	private async Task<string> SendForContentAsync(
		HttpMethod method,
		string url,
		IReadOnlyDictionary<string, string>? headers,
		CancellationToken cancellationToken)
	{
		var response = await SendRequestAsync(method, url, headers, cancellationToken).ConfigureAwait(false);
		return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
	}
}
