namespace PanoramicData.OData.Client.Exceptions;

/// <summary>
/// Exception thrown when a request is unauthorized.
/// </summary>
public class ODataUnauthorizedException : ODataClientException
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ODataUnauthorizedException"/> class with a specified error message.
	/// </summary>
	/// <param name="message">The error message.</param>
	public ODataUnauthorizedException(string message) : base(message)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ODataUnauthorizedException"/> class with detailed error information.
	/// </summary>
	/// <param name="message">The error message.</param>
	/// <param name="responseBody">The response body.</param>
	/// <param name="requestUrl">The request URL.</param>
	public ODataUnauthorizedException(string message, string? responseBody, string? requestUrl)
		: base(message, 401, responseBody, requestUrl)
	{
	}
}
