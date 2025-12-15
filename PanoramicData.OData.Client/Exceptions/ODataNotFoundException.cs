namespace PanoramicData.OData.Client.Exceptions;

/// <summary>
/// Exception thrown when an entity is not found.
/// </summary>
public class ODataNotFoundException : ODataClientException
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ODataNotFoundException"/> class with a specified error message.
	/// </summary>
	/// <param name="message">The error message.</param>
	public ODataNotFoundException(string message) : base(message)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ODataNotFoundException"/> class with the request URL.
	/// </summary>
	/// <param name="message">The error message.</param>
	/// <param name="requestUrl">The request URL that was not found.</param>
	public ODataNotFoundException(string message, string? requestUrl)
		: base(message, 404, null, requestUrl)
	{
	}
}
