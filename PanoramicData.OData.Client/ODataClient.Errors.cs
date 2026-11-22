namespace PanoramicData.OData.Client;

/// <summary>
/// ODataClient - error response handling.
/// </summary>
public partial class ODataClient
{
	private Task EnsureSuccessAsync(
		HttpResponseMessage response,
		string requestUrl,
		CancellationToken cancellationToken)
		=> EnsureSuccessAsync(response, requestUrl, requestETag: null, cancellationToken);

	private async Task EnsureSuccessAsync(
		HttpResponseMessage response,
		string requestUrl,
		string? requestETag,
		CancellationToken cancellationToken)
	{
		var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
		var statusCode = (int)response.StatusCode;

		if (!response.IsSuccessStatusCode)
		{
			ThrowForErrorStatusCode(response, requestUrl, requestETag, statusCode, responseBody);
		}

		ThrowForUnexpectedHtmlResponse(response, requestUrl, statusCode, responseBody);
	}

	private void ThrowForErrorStatusCode(
		HttpResponseMessage response,
		string requestUrl,
		string? requestETag,
		int statusCode,
		string responseBody)
	{
		if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or HttpStatusCode.NotFound)
		{
			// The caller decides whether these are expected, so they are not Error.
			LoggerMessages.RequestFailedClientError(_logger, requestUrl, response.StatusCode, responseBody);
		}
		else
		{
			LoggerMessages.RequestFailed(_logger, requestUrl, response.StatusCode, responseBody);
		}

		if (response.StatusCode == HttpStatusCode.PreconditionFailed)
		{
			var currentETag = response.Headers.ETag?.ToString();
			throw new ODataConcurrencyException(
				$"Concurrency conflict: The entity was modified since it was last retrieved. Request URL: {requestUrl}",
				requestUrl,
				requestETag,
				currentETag,
				responseBody);
		}

		throw response.StatusCode switch
		{
			HttpStatusCode.NotFound => new ODataNotFoundException($"Resource not found: {requestUrl}", requestUrl),
			HttpStatusCode.Unauthorized => new ODataUnauthorizedException("Unauthorized", responseBody, requestUrl),
			HttpStatusCode.Forbidden => new ODataForbiddenException("Forbidden", responseBody, requestUrl),
			_ => new ODataClientException($"Request failed with status {response.StatusCode}", statusCode, responseBody, requestUrl)
		};
	}

	private void ThrowForUnexpectedHtmlResponse(
		HttpResponseMessage response,
		string requestUrl,
		int statusCode,
		string responseBody)
	{
		var contentType = response.Content.Headers.ContentType?.MediaType ?? "unknown";
		var isXmlContentType = contentType.Contains("xml", StringComparison.OrdinalIgnoreCase);
		var isMetadataRequest = requestUrl.Contains("$metadata", StringComparison.OrdinalIgnoreCase);

		if (responseBody.Length > 0 &&
			responseBody.TrimStart().StartsWith('<') &&
			!isXmlContentType &&
			!isMetadataRequest)
		{
			var preview = responseBody.Length > 500 ? responseBody[..500] + "..." : responseBody;

			LoggerMessages.UnexpectedHtmlResponse(_logger, requestUrl, contentType, preview);

			throw new ODataClientException(
				$"Expected JSON response but received HTML. The server may have returned an error page, login redirect, or the endpoint doesn't exist. Content-Type: {contentType}, URL: {requestUrl}",
				statusCode,
				responseBody,
				requestUrl);
		}
	}
}
