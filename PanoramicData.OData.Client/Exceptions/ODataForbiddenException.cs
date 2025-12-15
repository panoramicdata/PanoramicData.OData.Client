namespace PanoramicData.OData.Client.Exceptions;

/// <summary>
/// Exception thrown when a request is forbidden.
/// </summary>
public class ODataForbiddenException : ODataClientException
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ODataForbiddenException"/> class with a specified error message.
	/// </summary>
	/// <param name="message">The error message.</param>
	public ODataForbiddenException(string message) : base(message)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ODataForbiddenException"/> class with detailed error information.
	/// </summary>
	/// <param name="message">The error message.</param>
	/// <param name="responseBody">The response body.</param>
	/// <param name="requestUrl">The request URL.</param>
	public ODataForbiddenException(string message, string? responseBody, string? requestUrl)
		: base(message, 403, responseBody, requestUrl)
	{
	}
}
