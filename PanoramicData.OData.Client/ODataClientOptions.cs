using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace PanoramicData.OData.Client;

/// <summary>
/// Configuration options for the OData client.
/// </summary>
public class ODataClientOptions
{
	/// <summary>
	/// The base URL of the OData service (e.g., "https://api.example.com/odata").
	/// </summary>
	public required string BaseUrl { get; set; }

	/// <summary>
	/// Optional HttpClient to use. If not provided, a new one will be created.
	/// </summary>
	public HttpClient? HttpClient { get; set; }

	/// <summary>
	/// Request timeout. Default is 5 minutes.
	/// </summary>
	public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);

	/// <summary>
	/// Action to configure request headers before each request.
	/// </summary>
	public Action<HttpRequestMessage>? ConfigureRequest { get; set; }

	/// <summary>
	/// Number of retry attempts for transient failures. Default is 3.
	/// </summary>
	public int RetryCount { get; set; } = 3;

	/// <summary>
	/// Delay between retry attempts. Default is 1 second.
	/// </summary>
	public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);

	/// <summary>
	/// Gets or sets the options to use when serializing or deserializing JSON content.
	/// </summary>
	/// <remarks>If not set, default serialization options are used. Use this property to customize serialization
	/// behavior, such as property naming policies, converters, or formatting.</remarks>
	public JsonSerializerOptions? JsonSerializerOptions { get; set; }

	/// <summary>
	/// Gets or sets the logger used to record diagnostic and operational messages.
	/// </summary>
	/// <remarks>Assign an implementation of <see cref="ILogger"/> to enable logging. If not set, logging is
	/// disabled.</remarks>
	public ILogger? Logger { get; set; }
}
