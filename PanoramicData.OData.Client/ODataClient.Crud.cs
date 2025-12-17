using Microsoft.Extensions.Logging;
using PanoramicData.OData.Client.Exceptions;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;

namespace PanoramicData.OData.Client;

/// <summary>
/// ODataClient - CRUD operations (Create, Update, Delete).
/// </summary>
public partial class ODataClient
{
	/// <summary>
	/// Creates a new entity.
	/// </summary>
	public async Task<T> CreateAsync<T>(
		string entitySet,
		T entity,
		IReadOnlyDictionary<string, string>? headers = null,
		CancellationToken cancellationToken = default) where T : class
	{
		var url = entitySet;
		var typeName = typeof(T).Name;
		LoggerMessages.CreateAsync(_logger, typeName, entitySet);

		var request = CreateRequest(HttpMethod.Post, url, headers);
		request.Content = JsonContent.Create(entity, options: _jsonOptions);

		// Log the request body for debugging
		if (_logger.IsEnabled(LogLevel.Debug))
		{
			var requestBody = await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
			LoggerMessages.CreateAsyncRequestBody(_logger, typeName, requestBody);
		}

		var response = await SendWithRetryAsync(request, cancellationToken).ConfigureAwait(false);
		await EnsureSuccessAsync(response, url, cancellationToken).ConfigureAwait(false);

		return await response.Content.ReadFromJsonAsync<T>(_jsonOptions, cancellationToken).ConfigureAwait(false)
			?? throw new ODataClientException("Failed to deserialize created entity");
	}

	/// <summary>
	/// Updates an entity using PATCH.
	/// </summary>
	public Task<T> UpdateAsync<T>(
		string entitySet,
		object key,
		object patchValues,
		IReadOnlyDictionary<string, string>? headers = null,
		CancellationToken cancellationToken = default) where T : class
		=> UpdateAsync<T, object>(entitySet, key, patchValues, etag: null, headers, cancellationToken);

	/// <summary>
	/// Updates an entity using PATCH with optimistic concurrency via ETag.
	/// </summary>
	/// <param name="entitySet">The entity set name.</param>
	/// <param name="key">The entity key.</param>
	/// <param name="patchValues">The values to update.</param>
	/// <param name="etag">The ETag value for concurrency control. If provided, an If-Match header is sent.</param>
	/// <param name="headers">Optional additional headers.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The updated entity.</returns>
	/// <exception cref="ODataConcurrencyException">Thrown when the server returns 412 Precondition Failed.</exception>
	public Task<T> UpdateAsync<T>(
		string entitySet,
		object key,
		object patchValues,
		string? etag,
		IReadOnlyDictionary<string, string>? headers = null,
		CancellationToken cancellationToken = default) where T : class
		=> UpdateAsync<T, object>(entitySet, key, patchValues, etag, headers, cancellationToken);

	/// <summary>
	/// Updates an entity using PATCH with strongly-typed key.
	/// </summary>
	public Task<T> UpdateAsync<T, TKey>(
		string entitySet,
		TKey key,
		object patchValues,
		IReadOnlyDictionary<string, string>? headers = null,
		CancellationToken cancellationToken = default) where T : class
		=> UpdateAsync<T, TKey>(entitySet, key, patchValues, etag: null, headers, cancellationToken);

	/// <summary>
	/// Updates an entity using PATCH with strongly-typed key and optimistic concurrency via ETag.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <typeparam name="TKey">The key type.</typeparam>
	/// <param name="entitySet">The entity set name.</param>
	/// <param name="key">The entity key.</param>
	/// <param name="patchValues">The values to update.</param>
	/// <param name="etag">The ETag value for concurrency control. If provided, an If-Match header is sent.</param>
	/// <param name="headers">Optional additional headers.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The updated entity.</returns>
	/// <exception cref="ODataConcurrencyException">Thrown when the server returns 412 Precondition Failed.</exception>
	public async Task<T> UpdateAsync<T, TKey>(
		string entitySet,
		TKey key,
		object patchValues,
		string? etag,
		IReadOnlyDictionary<string, string>? headers = null,
		CancellationToken cancellationToken = default) where T : class
	{
		var url = $"{entitySet}({FormatKey(key)})";
		var typeName = typeof(T).Name;
		LoggerMessages.UpdateAsync(_logger, typeName, url, etag ?? "(none)");

		var request = CreateRequest(new HttpMethod("PATCH"), url, headers);
		request.Content = JsonContent.Create(patchValues, options: _jsonOptions);

		// Add If-Match header for concurrency control
		if (!string.IsNullOrEmpty(etag))
		{
			request.Headers.TryAddWithoutValidation("If-Match", etag);
			LoggerMessages.UpdateAsyncIfMatch(_logger, typeName, etag);
		}

		var response = await SendWithRetryAsync(request, cancellationToken).ConfigureAwait(false);
		await EnsureSuccessAsync(response, url, etag, cancellationToken).ConfigureAwait(false);

		// Handle 204 No Content response by fetching the updated entity
		if (response.StatusCode == HttpStatusCode.NoContent)
		{
			LoggerMessages.UpdateAsyncNoContentRefetch(_logger, typeName);
			var getRequest = CreateRequest(HttpMethod.Get, url, headers);
			var getResponse = await SendWithRetryAsync(getRequest, cancellationToken).ConfigureAwait(false);
			await EnsureSuccessAsync(getResponse, url, cancellationToken).ConfigureAwait(false);
			return await getResponse.Content.ReadFromJsonAsync<T>(_jsonOptions, cancellationToken).ConfigureAwait(false)
				?? throw new ODataClientException("Failed to deserialize updated entity after refetch");
		}

		return await response.Content.ReadFromJsonAsync<T>(_jsonOptions, cancellationToken).ConfigureAwait(false)
			?? throw new ODataClientException("Failed to deserialize updated entity");
	}

	/// <summary>
	/// Deletes an entity.
	/// </summary>
	public Task DeleteAsync(
		string entitySet,
		object key,
		IReadOnlyDictionary<string, string>? headers = null,
		CancellationToken cancellationToken = default)
		=> DeleteAsync(entitySet, key, etag: null, headers, cancellationToken);

	/// <summary>
	/// Deletes an entity with optimistic concurrency via ETag.
	/// </summary>
	/// <param name="entitySet">The entity set name.</param>
	/// <param name="key">The entity key.</param>
	/// <param name="etag">The ETag value for concurrency control. If provided, an If-Match header is sent.</param>
	/// <param name="headers">Optional additional headers.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <exception cref="ODataConcurrencyException">Thrown when the server returns 412 Precondition Failed.</exception>
	public async Task DeleteAsync(
		string entitySet,
		object key,
		string? etag,
		IReadOnlyDictionary<string, string>? headers = null,
		CancellationToken cancellationToken = default)
	{
		var url = $"{entitySet}({FormatKey(key)})";
		LoggerMessages.DeleteAsync(_logger, entitySet, key, url, etag ?? "(none)");

		var request = CreateRequest(HttpMethod.Delete, url, headers);

		// Add If-Match header for concurrency control
		if (!string.IsNullOrEmpty(etag))
		{
			request.Headers.TryAddWithoutValidation("If-Match", etag);
			LoggerMessages.DeleteAsyncIfMatch(_logger, etag);
		}

		var response = await SendWithRetryAsync(request, cancellationToken).ConfigureAwait(false);
		await EnsureSuccessAsync(response, url, etag, cancellationToken).ConfigureAwait(false);
	}

	/// <summary>
	/// Replaces an entire entity using PUT.
	/// Unlike PATCH which merges changes, PUT replaces the entire entity.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="entitySet">The entity set name.</param>
	/// <param name="key">The entity key.</param>
	/// <param name="entity">The complete entity to replace with.</param>
	/// <param name="headers">Optional additional headers.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The replaced entity.</returns>
	public Task<T> ReplaceAsync<T>(
		string entitySet,
		object key,
		T entity,
		IReadOnlyDictionary<string, string>? headers = null,
		CancellationToken cancellationToken = default) where T : class
		=> ReplaceAsync<T, object>(entitySet, key, entity, etag: null, headers, cancellationToken);

	/// <summary>
	/// Replaces an entire entity using PUT with optimistic concurrency via ETag.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="entitySet">The entity set name.</param>
	/// <param name="key">The entity key.</param>
	/// <param name="entity">The complete entity to replace with.</param>
	/// <param name="etag">The ETag value for concurrency control.</param>
	/// <param name="headers">Optional additional headers.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The replaced entity.</returns>
	/// <exception cref="ODataConcurrencyException">Thrown when the server returns 412 Precondition Failed.</exception>
	public Task<T> ReplaceAsync<T>(
		string entitySet,
		object key,
		T entity,
		string? etag,
		IReadOnlyDictionary<string, string>? headers = null,
		CancellationToken cancellationToken = default) where T : class
		=> ReplaceAsync<T, object>(entitySet, key, entity, etag, headers, cancellationToken);

	/// <summary>
	/// Replaces an entire entity using PUT with strongly-typed key and optimistic concurrency via ETag.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <typeparam name="TKey">The key type.</typeparam>
	/// <param name="entitySet">The entity set name.</param>
	/// <param name="key">The entity key.</param>
	/// <param name="entity">The complete entity to replace with.</param>
	/// <param name="etag">The ETag value for concurrency control.</param>
	/// <param name="headers">Optional additional headers.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The replaced entity.</returns>
	/// <exception cref="ODataConcurrencyException">Thrown when the server returns 412 Precondition Failed.</exception>
	public async Task<T> ReplaceAsync<T, TKey>(
		string entitySet,
		TKey key,
		T entity,
		string? etag = null,
		IReadOnlyDictionary<string, string>? headers = null,
		CancellationToken cancellationToken = default) where T : class
	{
		var url = $"{entitySet}({FormatKey(key)})";
		var typeName = typeof(T).Name;
		LoggerMessages.ReplaceAsync(_logger, typeName, url, etag ?? "(none)");

		var request = CreateRequest(HttpMethod.Put, url, headers);
		request.Content = JsonContent.Create(entity, options: _jsonOptions);

		// Add If-Match header for concurrency control
		if (!string.IsNullOrEmpty(etag))
		{
			request.Headers.TryAddWithoutValidation("If-Match", etag);
			LoggerMessages.ReplaceAsyncIfMatch(_logger, typeName, etag);
		}

		var response = await SendWithRetryAsync(request, cancellationToken).ConfigureAwait(false);
		await EnsureSuccessAsync(response, url, etag, cancellationToken).ConfigureAwait(false);

		// Handle 204 No Content response by fetching the updated entity
		if (response.StatusCode == HttpStatusCode.NoContent)
		{
			LoggerMessages.ReplaceAsyncNoContentRefetch(_logger, typeName);
			var getRequest = CreateRequest(HttpMethod.Get, url, headers);
			var getResponse = await SendWithRetryAsync(getRequest, cancellationToken).ConfigureAwait(false);
			await EnsureSuccessAsync(getResponse, url, cancellationToken).ConfigureAwait(false);
			return await getResponse.Content.ReadFromJsonAsync<T>(_jsonOptions, cancellationToken).ConfigureAwait(false)
				?? throw new ODataClientException("Failed to deserialize replaced entity after refetch");
		}

		return await response.Content.ReadFromJsonAsync<T>(_jsonOptions, cancellationToken).ConfigureAwait(false)
			?? throw new ODataClientException("Failed to deserialize replaced entity");
	}

	/// <summary>
	/// Deletes an entity by its full URL path.
	/// </summary>
	/// <param name="url">The full URL path to the entity (e.g., "Products(123)").</param>
	/// <param name="headers">Optional additional headers.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <remarks>
	/// This method is primarily for internal use and legacy compatibility.
	/// Prefer using <see cref="DeleteAsync(string, object, IReadOnlyDictionary{string, string}?, CancellationToken)"/> instead.
	/// </remarks>
	internal async Task DeleteByUrlAsync(
		string url,
		IReadOnlyDictionary<string, string>? headers = null,
		CancellationToken cancellationToken = default)
	{
		LoggerMessages.DeleteByUrlAsync(_logger, url);

		var request = CreateRequest(HttpMethod.Delete, url, headers);
		var response = await SendWithRetryAsync(request, cancellationToken).ConfigureAwait(false);
		await EnsureSuccessAsync(response, url, cancellationToken).ConfigureAwait(false);
	}
}
