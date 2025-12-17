namespace PanoramicData.OData.Client;

/// <summary>
/// ODataClient - Singleton entity operations.
/// </summary>
public partial class ODataClient
{
	/// <summary>
	/// Gets a singleton entity (an entity without a key, like /Me or /Company).
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="singletonName">The singleton name (e.g., "Me", "Company").</param>
	/// <param name="headers">Optional additional headers.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The singleton entity.</returns>
	public async Task<T?> GetSingletonAsync<T>(
		string singletonName,
		IReadOnlyDictionary<string, string>? headers = null,
		CancellationToken cancellationToken = default) where T : class
	{
		LoggerMessages.GetSingletonAsync(_logger, typeof(T).Name, singletonName);

		var request = CreateRequest(HttpMethod.Get, singletonName, headers);
		var response = await SendWithRetryAsync(request, cancellationToken).ConfigureAwait(false);
		await EnsureSuccessAsync(response, singletonName, cancellationToken).ConfigureAwait(false);

		return await response.Content.ReadFromJsonAsync<T>(_jsonOptions, cancellationToken).ConfigureAwait(false);
	}

	/// <summary>
	/// Gets a singleton entity along with its ETag for concurrency control.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="singletonName">The singleton name (e.g., "Me", "Company").</param>
	/// <param name="headers">Optional additional headers.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A response containing the singleton entity and its ETag.</returns>
	public async Task<ODataSingleResponse<T>> GetSingletonWithETagAsync<T>(
		string singletonName,
		IReadOnlyDictionary<string, string>? headers = null,
		CancellationToken cancellationToken = default) where T : class
	{
		var typeName = typeof(T).Name;
		LoggerMessages.GetSingletonWithETagAsync(_logger, typeName, singletonName);

		var request = CreateRequest(HttpMethod.Get, singletonName, headers);
		var response = await SendWithRetryAsync(request, cancellationToken).ConfigureAwait(false);
		await EnsureSuccessAsync(response, singletonName, cancellationToken).ConfigureAwait(false);

		var result = new ODataSingleResponse<T>
		{
			Value = await response.Content.ReadFromJsonAsync<T>(_jsonOptions, cancellationToken).ConfigureAwait(false)
		};

		if (response.Headers.ETag is not null)
		{
			result.ETag = response.Headers.ETag.ToString();
			LoggerMessages.GetSingletonWithETagResult(_logger, typeName, result.ETag);
		}

		return result;
	}

	/// <summary>
	/// Updates a singleton entity using PATCH.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="singletonName">The singleton name (e.g., "Me", "Company").</param>
	/// <param name="patchValues">The values to update.</param>
	/// <param name="headers">Optional additional headers.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The updated singleton entity.</returns>
	public Task<T> UpdateSingletonAsync<T>(
		string singletonName,
		object patchValues,
		IReadOnlyDictionary<string, string>? headers = null,
		CancellationToken cancellationToken = default) where T : class
		=> UpdateSingletonAsync<T>(singletonName, patchValues, etag: null, headers, cancellationToken);

	/// <summary>
	/// Updates a singleton entity using PATCH with optimistic concurrency via ETag.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="singletonName">The singleton name (e.g., "Me", "Company").</param>
	/// <param name="patchValues">The values to update.</param>
	/// <param name="etag">The ETag value for concurrency control.</param>
	/// <param name="headers">Optional additional headers.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The updated singleton entity.</returns>
	/// <exception cref="ODataConcurrencyException">Thrown when the server returns 412 Precondition Failed.</exception>
	public async Task<T> UpdateSingletonAsync<T>(
		string singletonName,
		object patchValues,
		string? etag,
		IReadOnlyDictionary<string, string>? headers = null,
		CancellationToken cancellationToken = default) where T : class
	{
		var typeName = typeof(T).Name;
		LoggerMessages.UpdateSingletonAsync(_logger, typeName, singletonName, etag ?? "(none)");

		var request = CreateRequest(new HttpMethod("PATCH"), singletonName, headers);
		request.Content = JsonContent.Create(patchValues, options: _jsonOptions);

		if (!string.IsNullOrEmpty(etag))
		{
			request.Headers.TryAddWithoutValidation("If-Match", etag);
		}

		var response = await SendWithRetryAsync(request, cancellationToken).ConfigureAwait(false);
		await EnsureSuccessAsync(response, singletonName, etag, cancellationToken).ConfigureAwait(false);

		// Handle 204 No Content response by fetching the updated entity
		if (response.StatusCode == HttpStatusCode.NoContent)
		{
			LoggerMessages.UpdateSingletonAsyncNoContentRefetch(_logger, typeName);
			var getRequest = CreateRequest(HttpMethod.Get, singletonName, headers);
			var getResponse = await SendWithRetryAsync(getRequest, cancellationToken).ConfigureAwait(false);
			await EnsureSuccessAsync(getResponse, singletonName, cancellationToken).ConfigureAwait(false);
			return await getResponse.Content.ReadFromJsonAsync<T>(_jsonOptions, cancellationToken).ConfigureAwait(false)
				?? throw new ODataClientException("Failed to deserialize updated singleton after refetch");
		}

		return await response.Content.ReadFromJsonAsync<T>(_jsonOptions, cancellationToken).ConfigureAwait(false)
			?? throw new ODataClientException("Failed to deserialize updated singleton");
	}
}
