namespace PanoramicData.OData.Client;

/// <summary>
/// ODataClient - Stream/Media entity operations.
/// </summary>
public partial class ODataClient
{
	/// <summary>
	/// Gets the binary stream content of a media entity.
	/// OData V4 media entities store binary content accessed via /$value.
	/// </summary>
	/// <typeparam name="TKey">The key type.</typeparam>
	/// <param name="entitySet">The entity set name.</param>
	/// <param name="key">The entity key.</param>
	/// <param name="headers">Optional additional headers.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A stream containing the binary content.</returns>
	public async Task<Stream> GetStreamAsync<TKey>(
		string entitySet,
		TKey key,
		[Optional] IReadOnlyDictionary<string, string>? headers,
		[Optional] CancellationToken cancellationToken)
	{
		var url = $"{entitySet}({FormatKey(key)})/$value";
		LoggerMessages.GetStreamAsync(_logger, url);

		var response = await SendRequestAsync(HttpMethod.Get, url, headers, cancellationToken).ConfigureAwait(false);

		return await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
	}

	/// <summary>
	/// Sets the binary stream content of a media entity.
	/// </summary>
	/// <typeparam name="TKey">The key type.</typeparam>
	/// <param name="entitySet">The entity set name.</param>
	/// <param name="key">The entity key.</param>
	/// <param name="stream">The stream containing the binary content.</param>
	/// <param name="contentType">The MIME content type of the stream.</param>
	/// <param name="headers">Optional additional headers.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	public async Task SetStreamAsync<TKey>(
		string entitySet,
		TKey key,
		Stream stream,
		[Optional, DefaultParameterValue("application/octet-stream")] string contentType,
		[Optional] IReadOnlyDictionary<string, string>? headers,
		[Optional] CancellationToken cancellationToken)
	{
		var url = $"{entitySet}({FormatKey(key)})/$value";
		LoggerMessages.SetStreamAsync(_logger, url, contentType);

		var request = CreateRequest(HttpMethod.Put, url, headers);
		request.Content = new StreamContent(stream);
		request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);

		var response = await SendAndEnsureSuccessAsync(request, url, cancellationToken).ConfigureAwait(false);
	}

	/// <summary>
	/// Gets a named stream property from an entity.
	/// OData V4 supports named stream properties for entities with multiple binary properties.
	/// </summary>
	/// <typeparam name="TKey">The key type.</typeparam>
	/// <param name="entitySet">The entity set name.</param>
	/// <param name="key">The entity key.</param>
	/// <param name="propertyName">The name of the stream property.</param>
	/// <param name="headers">Optional additional headers.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A stream containing the binary content.</returns>
	public async Task<Stream> GetStreamPropertyAsync<TKey>(
		string entitySet,
		TKey key,
		string propertyName,
		[Optional] IReadOnlyDictionary<string, string>? headers,
		[Optional] CancellationToken cancellationToken)
	{
		var url = $"{entitySet}({FormatKey(key)})/{propertyName}";
		LoggerMessages.GetStreamPropertyAsync(_logger, url);

		var response = await SendRequestAsync(HttpMethod.Get, url, headers, cancellationToken).ConfigureAwait(false);

		return await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
	}

	/// <summary>
	/// Sets a named stream property on an entity.
	/// </summary>
	/// <typeparam name="TKey">The key type.</typeparam>
	/// <param name="entitySet">The entity set name.</param>
	/// <param name="key">The entity key.</param>
	/// <param name="propertyName">The name of the stream property.</param>
	/// <param name="stream">The stream containing the binary content.</param>
	/// <param name="contentType">The MIME content type of the stream.</param>
	/// <param name="headers">Optional additional headers.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	public async Task SetStreamPropertyAsync<TKey>(
		string entitySet,
		TKey key,
		string propertyName,
		Stream stream,
		[Optional, DefaultParameterValue("application/octet-stream")] string contentType,
		[Optional] IReadOnlyDictionary<string, string>? headers,
		[Optional] CancellationToken cancellationToken)
	{
		var url = $"{entitySet}({FormatKey(key)})/{propertyName}";
		LoggerMessages.SetStreamPropertyAsync(_logger, url, contentType);

		var request = CreateRequest(HttpMethod.Put, url, headers);
		request.Content = new StreamContent(stream);
		request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);

		var response = await SendAndEnsureSuccessAsync(request, url, cancellationToken).ConfigureAwait(false);
	}
}
