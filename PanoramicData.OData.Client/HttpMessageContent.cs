namespace PanoramicData.OData.Client;

/// <summary>
/// Provides HTTP content based on an HttpRequestMessage for OData batch requests.
/// </summary>
internal sealed class HttpMessageContent : HttpContent
{
	private readonly HttpRequestMessage _request;
	private readonly Uri? _baseUri;
	private byte[]? _content;

	/// <summary>
	/// Creates a new HttpMessageContent wrapping an HTTP request.
	/// </summary>
	/// <param name="request">The HTTP request message to wrap.</param>
	/// <param name="baseUri">The base URI used to resolve relative request URIs and supply the Host header.</param>
	public HttpMessageContent(HttpRequestMessage request, Uri? baseUri = null)
	{
		_request = request ?? throw new ArgumentNullException(nameof(request));
		_baseUri = baseUri;

		Headers.ContentType = new MediaTypeHeaderValue("application/http");
		Headers.TryAddWithoutValidation("Content-Transfer-Encoding", "binary");
	}

	/// <inheritdoc/>
	protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
	{
		_content ??= await BuildContentAsync().ConfigureAwait(false);

		await stream.WriteAsync(_content).ConfigureAwait(false);
	}

	/// <inheritdoc/>
	protected override bool TryComputeLength(out long length)
	{
		// We need to compute the content to know the length
		length = 0;
		return false;
	}

	private async Task<byte[]> BuildContentAsync()
	{
		var sb = new StringBuilder();

		// Resolve to absolute URI if relative, so PathAndQuery and Host are available
		var effectiveUri = _request.RequestUri is { IsAbsoluteUri: false } && _baseUri is not null
			? new Uri(_baseUri, _request.RequestUri)
			: _request.RequestUri;

		// Request line: METHOD path HTTP/1.1
		var requestUri = effectiveUri?.PathAndQuery ?? "/";
		sb.Append(_request.Method.ToString());
		sb.Append(' ');
		sb.Append(requestUri);
		sb.AppendLine(" HTTP/1.1");

		// Host header
		if (effectiveUri?.Host is not null)
		{
			sb.Append("Host: ");
			sb.AppendLine(effectiveUri.Host);
		}

		// Request headers
		foreach (var header in _request.Headers)
		{
			sb.Append(header.Key);
			sb.Append(": ");
			sb.AppendLine(string.Join(", ", header.Value));
		}

		// Content headers and body
		if (_request.Content is not null)
		{
			foreach (var header in _request.Content.Headers)
			{
				sb.Append(header.Key);
				sb.Append(": ");
				sb.AppendLine(string.Join(", ", header.Value));
			}

			sb.AppendLine(); // Empty line before body

			var body = await _request.Content.ReadAsStringAsync().ConfigureAwait(false);
			sb.Append(body);
		}
		else
		{
			sb.AppendLine(); // Empty line (no body)
		}

		return Encoding.UTF8.GetBytes(sb.ToString());
	}
}
