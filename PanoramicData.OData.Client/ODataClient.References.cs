namespace PanoramicData.OData.Client;

/// <summary>
/// ODataClient - Entity reference ($ref) operations.
/// </summary>
public partial class ODataClient
{
	/// <summary>
	/// Adds a reference to a collection navigation property.
	/// POST {entitySet}({key})/{navigationProperty}/$ref
	/// Body: { "@odata.id": "{targetEntitySet}({targetKey})" }
	/// </summary>
	/// <param name="entitySet">The source entity set name.</param>
	/// <param name="key">The source entity key.</param>
	/// <param name="navigationProperty">The navigation property name.</param>
	/// <param name="targetEntitySet">The target entity set name.</param>
	/// <param name="targetKey">The target entity key.</param>
	/// <param name="headers">Optional additional headers.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	public async Task AddReferenceAsync(
		string entitySet,
		object key,
		string navigationProperty,
		string targetEntitySet,
		object targetKey,
		[Optional] IReadOnlyDictionary<string, string>? headers,
		[Optional] CancellationToken cancellationToken)
	{
		var url = NavigationReferenceUrl(entitySet, key, navigationProperty);
		var targetUrl = EntityReferenceUrl(targetEntitySet, targetKey);

		LoggerMessages.AddReferenceAsync(_logger, url, targetUrl);

		await SendReferenceBodyAsync(HttpMethod.Post, url, targetUrl, headers, cancellationToken).ConfigureAwait(false);
	}

	/// <summary>
	/// Removes a reference from a collection navigation property.
	/// DELETE {entitySet}({key})/{navigationProperty}/$ref?$id={targetEntitySet}({targetKey})
	/// </summary>
	/// <param name="entitySet">The source entity set name.</param>
	/// <param name="key">The source entity key.</param>
	/// <param name="navigationProperty">The navigation property name.</param>
	/// <param name="targetEntitySet">The target entity set name.</param>
	/// <param name="targetKey">The target entity key.</param>
	/// <param name="headers">Optional additional headers.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	public async Task RemoveReferenceAsync(
		string entitySet,
		object key,
		string navigationProperty,
		string targetEntitySet,
		object targetKey,
		[Optional] IReadOnlyDictionary<string, string>? headers,
		[Optional] CancellationToken cancellationToken)
	{
		var targetUrl = EntityReferenceUrl(targetEntitySet, targetKey);
		var encodedTargetUrl = Uri.EscapeDataString(targetUrl);
		var url = $"{entitySet}({ODataLiteral.FormatKey(key)})/{navigationProperty}/$ref?$id={encodedTargetUrl}";

		LoggerMessages.RemoveReferenceAsync(_logger, url);

		await SendRequestAsync(HttpMethod.Delete, url, headers, cancellationToken).ConfigureAwait(false);
	}

	/// <summary>
	/// Sets a single-valued navigation property reference.
	/// PUT {entitySet}({key})/{navigationProperty}/$ref
	/// Body: { "@odata.id": "{targetEntitySet}({targetKey})" }
	/// </summary>
	/// <param name="entitySet">The source entity set name.</param>
	/// <param name="key">The source entity key.</param>
	/// <param name="navigationProperty">The navigation property name.</param>
	/// <param name="targetEntitySet">The target entity set name.</param>
	/// <param name="targetKey">The target entity key.</param>
	/// <param name="headers">Optional additional headers.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	public async Task SetReferenceAsync(
		string entitySet,
		object key,
		string navigationProperty,
		string targetEntitySet,
		object targetKey,
		[Optional] IReadOnlyDictionary<string, string>? headers,
		[Optional] CancellationToken cancellationToken)
	{
		var url = NavigationReferenceUrl(entitySet, key, navigationProperty);
		var targetUrl = EntityReferenceUrl(targetEntitySet, targetKey);

		LoggerMessages.SetReferenceAsync(_logger, url, targetUrl);

		await SendReferenceBodyAsync(HttpMethod.Put, url, targetUrl, headers, cancellationToken).ConfigureAwait(false);
	}

	/// <summary>
	/// Deletes a single-valued navigation property reference.
	/// DELETE {entitySet}({key})/{navigationProperty}/$ref
	/// </summary>
	/// <param name="entitySet">The source entity set name.</param>
	/// <param name="key">The source entity key.</param>
	/// <param name="navigationProperty">The navigation property name.</param>
	/// <param name="headers">Optional additional headers.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	public async Task DeleteReferenceAsync(
		string entitySet,
		object key,
		string navigationProperty,
		[Optional] IReadOnlyDictionary<string, string>? headers,
		[Optional] CancellationToken cancellationToken)
	{
		var url = NavigationReferenceUrl(entitySet, key, navigationProperty);

		LoggerMessages.DeleteReferenceAsync(_logger, url);

		await SendRequestAsync(HttpMethod.Delete, url, headers, cancellationToken).ConfigureAwait(false);
	}

	private static string NavigationReferenceUrl(string entitySet, object key, string navigationProperty)
		=> $"{EntityReferenceUrl(entitySet, key)}/{navigationProperty}/$ref";

	private static string EntityReferenceUrl(string entitySet, object key)
		=> $"{entitySet}({ODataLiteral.FormatKey(key)})";

	private async Task SendReferenceBodyAsync(
		HttpMethod method,
		string url,
		string targetUrl,
		IReadOnlyDictionary<string, string>? headers,
		CancellationToken cancellationToken)
	{
		var request = CreateRequest(method, url, headers);
		request.Content = new StringContent($"{{\"@odata.id\": \"{targetUrl}\"}}", Encoding.UTF8, "application/json");

		await SendAndEnsureSuccessAsync(request, url, cancellationToken).ConfigureAwait(false);
	}
}
