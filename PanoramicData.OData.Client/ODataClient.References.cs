using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text;

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
		IReadOnlyDictionary<string, string>? headers = null,
		CancellationToken cancellationToken = default)
	{
		var url = $"{entitySet}({FormatKey(key)})/{navigationProperty}/$ref";
		var targetUrl = $"{targetEntitySet}({FormatKey(targetKey)})";

		_logger.LogDebug("AddReferenceAsync - URL: {Url}, Target: {Target}", url, targetUrl);

		var request = CreateRequest(HttpMethod.Post, url, headers);

		// Use @odata.id property name
		var refBody = $"{{\"@odata.id\": \"{targetUrl}\"}}";
		request.Content = new StringContent(refBody, Encoding.UTF8, "application/json");

		var response = await SendWithRetryAsync(request, cancellationToken).ConfigureAwait(false);
		await EnsureSuccessAsync(response, url, cancellationToken).ConfigureAwait(false);
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
		IReadOnlyDictionary<string, string>? headers = null,
		CancellationToken cancellationToken = default)
	{
		var targetUrl = $"{targetEntitySet}({FormatKey(targetKey)})";
		var encodedTargetUrl = Uri.EscapeDataString(targetUrl);
		var url = $"{entitySet}({FormatKey(key)})/{navigationProperty}/$ref?$id={encodedTargetUrl}";

		_logger.LogDebug("RemoveReferenceAsync - URL: {Url}", url);

		var request = CreateRequest(HttpMethod.Delete, url, headers);

		var response = await SendWithRetryAsync(request, cancellationToken).ConfigureAwait(false);
		await EnsureSuccessAsync(response, url, cancellationToken).ConfigureAwait(false);
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
		IReadOnlyDictionary<string, string>? headers = null,
		CancellationToken cancellationToken = default)
	{
		var url = $"{entitySet}({FormatKey(key)})/{navigationProperty}/$ref";
		var targetUrl = $"{targetEntitySet}({FormatKey(targetKey)})";

		_logger.LogDebug("SetReferenceAsync - URL: {Url}, Target: {Target}", url, targetUrl);

		var request = CreateRequest(HttpMethod.Put, url, headers);
		var refBody = $"{{\"@odata.id\": \"{targetUrl}\"}}";
		request.Content = new StringContent(refBody, Encoding.UTF8, "application/json");

		var response = await SendWithRetryAsync(request, cancellationToken).ConfigureAwait(false);
		await EnsureSuccessAsync(response, url, cancellationToken).ConfigureAwait(false);
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
		IReadOnlyDictionary<string, string>? headers = null,
		CancellationToken cancellationToken = default)
	{
		var url = $"{entitySet}({FormatKey(key)})/{navigationProperty}/$ref";

		_logger.LogDebug("DeleteReferenceAsync - URL: {Url}", url);

		var request = CreateRequest(HttpMethod.Delete, url, headers);

		var response = await SendWithRetryAsync(request, cancellationToken).ConfigureAwait(false);
		await EnsureSuccessAsync(response, url, cancellationToken).ConfigureAwait(false);
	}
}
