namespace PanoramicData.OData.Client;

/// <summary>
/// Specifies how caching should be handled for a request.
/// </summary>
public enum CacheHandling
{
	/// <summary>
	/// Use cached data if available and not expired; otherwise fetch from the server.
	/// </summary>
	Default = 0,

	/// <summary>
	/// Bypass the cache and always fetch fresh data from the server.
	/// The cache will be updated with the new data.
	/// </summary>
	ForceRefresh = 1
}
