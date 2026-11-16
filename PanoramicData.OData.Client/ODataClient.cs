using Microsoft.Extensions.Logging.Abstractions;
using PanoramicData.OData.Client.Converters;

namespace PanoramicData.OData.Client;

/// <summary>
/// A lightweight OData client for REST API communication.
/// </summary>
public partial class ODataClient : IDisposable
{
	private readonly HttpClient _httpClient;
	private readonly ODataClientOptions _options;
	private readonly ILogger _logger;
	private readonly bool _ownsHttpClient;
	private bool _disposed;

	private readonly JsonSerializerOptions _jsonOptions = new()
	{
		PropertyNameCaseInsensitive = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		Converters =
		{
			new ODataTypeAnnotationConverter(), // Must be first to handle @odata.type annotation for TPH inheritance
			new JsonStringEnumConverter(),
			new ODataDateTimeConverter(),
			new ODataNullableDateTimeConverter()
		}
	};

	// Add a field for action-specific JSON options after _jsonOptions
	private readonly JsonSerializerOptions _actionJsonOptions = new()
	{
		PropertyNameCaseInsensitive = true,
		// Note: Do NOT use PropertyNamingPolicy for actions - OData requires Pascal case matching the EDM model
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		Converters =
		{
			new JsonStringEnumConverter(),
			new ODataDateTimeConverter(),
			new ODataNullableDateTimeConverter()
		}
	};

	/// <summary>
	/// Creates a new OData client.
	/// </summary>
	public ODataClient(ODataClientOptions options)
	{
		_options = options ?? throw new ArgumentNullException(nameof(options));
		_logger = options.Logger ?? NullLogger.Instance;

		LoggerMessages.ClientInitialized(_logger, options.BaseUrl);

		if (options.HttpClient is not null)
		{
			_httpClient = options.HttpClient;
			_ownsHttpClient = false;

			// If the provided HttpClient has no BaseAddress, set it from options so relative URLs resolve correctly
			_httpClient.BaseAddress ??= new Uri(options.BaseUrl.TrimEnd('/') + "/");

			LoggerMessages.UsingProvidedHttpClient(_logger, _httpClient.BaseAddress);
		}
		else
		{
			_httpClient = new HttpClient
			{
				BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/"),
				Timeout = options.Timeout
			};
			_ownsHttpClient = true;
			LoggerMessages.CreatedHttpClient(_logger, _httpClient.BaseAddress);
		}

		_jsonOptions = options.JsonSerializerOptions ?? _jsonOptions;
	}

	#region Private Helper Methods

	private HttpRequestMessage CreateRequest(
		HttpMethod method,
		string url,
		[Optional] IReadOnlyDictionary<string, string>? headers)
	{
		var request = new HttpRequestMessage(method, url);

		LoggerMessages.CreateRequest(_logger, method, url);

		// Apply custom headers from options
		_options.ConfigureRequest?.Invoke(request);

		// Apply query-specific headers
		if (headers is not null)
		{
			foreach (var header in headers)
			{
				request.Headers.TryAddWithoutValidation(header.Key, header.Value);
				LoggerMessages.AddingHeader(_logger, header.Key, HttpExtensions.RedactIfSensitive(header.Key, header.Value));
			}
		}

		return request;
	}

	#endregion

	#region IDisposable

	/// <summary>
	/// Releases the unmanaged resources used by the <see cref="ODataClient"/> and optionally releases the managed resources.
	/// </summary>
	/// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
	protected virtual void Dispose(bool disposing)
	{
		if (!_disposed)
		{
			if (disposing && _ownsHttpClient)
			{
				_httpClient.Dispose();
			}

			_disposed = true;
		}
	}

	/// <inheritdoc />
	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	#endregion
}
