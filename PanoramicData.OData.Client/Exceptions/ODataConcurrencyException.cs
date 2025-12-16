namespace PanoramicData.OData.Client.Exceptions;

/// <summary>
/// Exception thrown when an OData request fails due to a concurrency conflict (HTTP 412 Precondition Failed).
/// This typically occurs when the entity was modified by another user since it was last retrieved.
/// </summary>
public class ODataConcurrencyException : ODataClientException
{
	/// <summary>
	/// The ETag value that was sent with the request.
	/// </summary>
	public string? RequestETag { get; }

	/// <summary>
	/// The current ETag value from the server (if available in the response).
	/// </summary>
	public string? CurrentETag { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="ODataConcurrencyException"/> class.
	/// </summary>
	/// <param name="message">The error message.</param>
	/// <param name="requestUrl">The request URL.</param>
	public ODataConcurrencyException(string message, string? requestUrl)
		: base(message, 412, null, requestUrl)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ODataConcurrencyException"/> class with ETag information.
	/// </summary>
	/// <param name="message">The error message.</param>
	/// <param name="requestUrl">The request URL.</param>
	/// <param name="requestETag">The ETag that was sent with the request.</param>
	/// <param name="currentETag">The current ETag from the server.</param>
	/// <param name="responseBody">The response body.</param>
	public ODataConcurrencyException(
		string message,
		string? requestUrl,
		string? requestETag,
		string? currentETag,
		string? responseBody)
		: base(message, 412, responseBody, requestUrl)
	{
		RequestETag = requestETag;
		CurrentETag = currentETag;
	}
}
