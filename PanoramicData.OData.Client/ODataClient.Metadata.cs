namespace PanoramicData.OData.Client;

/// <summary>
/// ODataClient - Metadata operations.
/// </summary>
public partial class ODataClient
{
	private ODataMetadata? _cachedMetadata;
	private string? _cachedMetadataXml;
	private DateTime _metadataCacheTime;

	/// <summary>
	/// Gets the service metadata (CSDL) from the $metadata endpoint using default cache handling.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The parsed metadata.</returns>
	public Task<ODataMetadata> GetMetadataAsync(CancellationToken cancellationToken)
		=> GetMetadataAsync(CacheHandling.Default, cancellationToken);

	/// <summary>
	/// Gets the service metadata (CSDL) from the $metadata endpoint.
	/// </summary>
	/// <param name="cacheHandling">Specifies how caching should be handled.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The parsed metadata.</returns>
	public async Task<ODataMetadata> GetMetadataAsync(
		CacheHandling cacheHandling,
		CancellationToken cancellationToken)
	{
		var forceRefresh = cacheHandling == CacheHandling.ForceRefresh;

		// Check cache if enabled and not forcing refresh
		if (!forceRefresh && _options.MetadataCacheDuration.HasValue && _cachedMetadata is not null)
		{
			var cacheAge = DateTime.UtcNow - _metadataCacheTime;
			if (cacheAge < _options.MetadataCacheDuration.Value)
			{
				LoggerMessages.GetMetadataAsyncCached(_logger, cacheAge);
				return _cachedMetadata;
			}
		}

		LoggerMessages.GetMetadataAsyncFetching(_logger);

		// Fetch XML (this will also update the XML cache)
		var content = await GetMetadataXmlAsync(cacheHandling, cancellationToken).ConfigureAwait(false);

		LoggerMessages.GetMetadataAsyncParsing(_logger, content.Length);

		var metadata = ODataMetadataParser.Parse(content);

		// Cache if enabled
		if (_options.MetadataCacheDuration.HasValue)
		{
			_cachedMetadata = metadata;
			_metadataCacheTime = DateTime.UtcNow;
		}

		return metadata;
	}

	/// <summary>
	/// Gets the raw metadata XML (CSDL) from the $metadata endpoint using default cache handling.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The raw CSDL XML string.</returns>
	public Task<string> GetMetadataXmlAsync(CancellationToken cancellationToken)
		=> GetMetadataXmlAsync(CacheHandling.Default, cancellationToken);

	/// <summary>
	/// Gets the raw metadata XML (CSDL) from the $metadata endpoint.
	/// </summary>
	/// <param name="cacheHandling">Specifies how caching should be handled.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The raw CSDL XML string.</returns>
	public async Task<string> GetMetadataXmlAsync(
		CacheHandling cacheHandling,
		CancellationToken cancellationToken)
	{
		var forceRefresh = cacheHandling == CacheHandling.ForceRefresh;

		// Check cache if enabled and not forcing refresh
		if (!forceRefresh && _options.MetadataCacheDuration.HasValue && _cachedMetadataXml is not null)
		{
			var cacheAge = DateTime.UtcNow - _metadataCacheTime;
			if (cacheAge < _options.MetadataCacheDuration.Value)
			{
				LoggerMessages.GetMetadataXmlAsyncCached(_logger, cacheAge);
				return _cachedMetadataXml;
			}
		}

		LoggerMessages.GetMetadataXmlAsyncFetching(_logger);

		var request = CreateRequest(HttpMethod.Get, "$metadata");
		request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));

		var response = await SendWithRetryAsync(request, cancellationToken).ConfigureAwait(false);
		await EnsureSuccessAsync(response, "$metadata", cancellationToken).ConfigureAwait(false);

		var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

		// Cache if enabled
		if (_options.MetadataCacheDuration.HasValue)
		{
			_cachedMetadataXml = content;
			_metadataCacheTime = DateTime.UtcNow;
		}

		return content;
	}

	/// <summary>
	/// Invalidates the cached metadata, forcing the next call to <see cref="GetMetadataAsync(CancellationToken)"/> 
	/// or <see cref="GetMetadataXmlAsync(CancellationToken)"/> to fetch fresh data from the server.
	/// </summary>
	public void InvalidateMetadataCache()
	{
		LoggerMessages.InvalidateMetadataCache(_logger);
		_cachedMetadata = null;
		_cachedMetadataXml = null;
	}
}
