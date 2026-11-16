namespace PanoramicData.OData.Client;

/// <summary>
/// ODataClient - function and action calls.
/// </summary>
public partial class ODataClient
{
	/// <summary>
	/// Calls an OData function and returns the result.
	/// </summary>
	public async Task<TResult?> CallFunctionAsync<T, TResult>(
		ODataQueryBuilder<T> query,
		[Optional] CancellationToken cancellationToken) where T : class
	{
		var url = query.BuildUrl();
		LoggerMessages.CallFunctionAsync(_logger, typeof(T).Name, typeof(TResult).Name, url);

		var content = await SendForContentAsync(HttpMethod.Get, url, query.CustomHeaders, cancellationToken).ConfigureAwait(false);
		LoggerMessages.CallFunctionAsyncResponseLength(_logger, content.Length);

		// Try to parse as OData response first
		try
		{
			using var doc = JsonDocument.Parse(content);

			// Check if it's a collection result
			if (doc.RootElement.TryGetProperty("value", out var valueElement))
			{
				LoggerMessages.CallFunctionAsyncParsingValue(_logger);
				return JsonSerializer.Deserialize<TResult>(valueElement.GetRawText(), _jsonOptions);
			}

			// Otherwise deserialize the whole thing
			LoggerMessages.CallFunctionAsyncParsingEntire(_logger, typeof(TResult).Name);
			return JsonSerializer.Deserialize<TResult>(content, _jsonOptions);
		}
		catch (Exception)
		{
			LoggerMessages.CallFunctionAsyncFallback(_logger);
			return JsonSerializer.Deserialize<TResult>(content, _jsonOptions);
		}
	}

	/// <summary>
	/// Calls an OData action (POST).
	/// </summary>
	public async Task<TResult?> CallActionAsync<TResult>(
		string actionUrl,
		[Optional, DefaultParameterValue(null)] object? parameters,
		[Optional] IReadOnlyDictionary<string, string>? headers,
		[Optional] CancellationToken cancellationToken)
	{
		LoggerMessages.CallActionAsync(_logger, typeof(TResult).Name, actionUrl);

		var request = CreateRequest(HttpMethod.Post, actionUrl, headers);

		if (parameters is not null)
		{
			// Use action-specific JSON options that preserve Pascal case for OData action parameters
			request.Content = JsonContent.Create(parameters, options: _actionJsonOptions);
		}

		var response = await SendAndEnsureSuccessAsync(request, actionUrl, cancellationToken).ConfigureAwait(false);

		if (response.StatusCode == HttpStatusCode.NoContent)
		{
			LoggerMessages.CallActionAsyncNoContent(_logger);
			return default;
		}

		return await response.Content.ReadFromJsonAsync<TResult>(_jsonOptions, cancellationToken).ConfigureAwait(false);
	}
}
