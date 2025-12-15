namespace PanoramicData.OData.Client.Exceptions;

/// <summary>
/// Exception thrown when an OData request fails.
/// </summary>
public class ODataClientException : Exception
{
	/// <summary>
	/// The HTTP status code of the response.
	/// </summary>
	public int? StatusCode { get; }

	/// <summary>
	/// The raw response body.
	/// </summary>
	public string? ResponseBody { get; }

	/// <summary>
	/// The request URL that failed.
	/// </summary>
	public string? RequestUrl { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="ODataClientException"/> class with a specified error message.
	/// </summary>
	/// <param name="message">The error message.</param>
	public ODataClientException(string message) : base(message)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ODataClientException"/> class with a specified error message and inner exception.
	/// </summary>
	/// <param name="message">The error message.</param>
	/// <param name="innerException">The inner exception.</param>
	public ODataClientException(string message, Exception innerException) : base(message, innerException)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ODataClientException"/> class with detailed error information.
	/// </summary>
	/// <param name="message">The error message.</param>
	/// <param name="statusCode">The HTTP status code.</param>
	/// <param name="responseBody">The response body.</param>
	/// <param name="requestUrl">The request URL.</param>
	public ODataClientException(string message, int statusCode, string? responseBody, string? requestUrl)
		: base(message)
	{
		StatusCode = statusCode;
		ResponseBody = responseBody;
		RequestUrl = requestUrl;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ODataClientException"/> class with detailed error information and inner exception.
	/// </summary>
	/// <param name="message">The error message.</param>
	/// <param name="statusCode">The HTTP status code.</param>
	/// <param name="responseBody">The response body.</param>
	/// <param name="requestUrl">The request URL.</param>
	/// <param name="innerException">The inner exception.</param>
	public ODataClientException(string message, int statusCode, string? responseBody, string? requestUrl, Exception innerException)
		: base(message, innerException)
	{
		StatusCode = statusCode;
		ResponseBody = responseBody;
		RequestUrl = requestUrl;
	}
}
