namespace PanoramicData.OData.Client.Test.Fixtures;

/// <summary>
/// XUnit 3 test fixture that provides a configured ODataClient with full logging support.
/// </summary>
/// <remarks>
/// This fixture uses Microsoft.Extensions.DependencyInjection to wire up logging
/// and the ODataClient. The logger is configured to output to the console at Debug level,
/// showing full request/response details including headers and body content.
/// </remarks>
public sealed class ODataClientFixture : IAsyncLifetime
{
	private ServiceProvider? _serviceProvider;

	/// <summary>
	/// Gets the configured ODataClient instance with logging enabled.
	/// </summary>
	public ODataClient Client { get; private set; } = null!;

	/// <summary>
	/// Gets the logger factory for creating additional loggers if needed.
	/// </summary>
	public ILoggerFactory LoggerFactory { get; private set; } = null!;

	/// <summary>
	/// Gets the base URL of the OData service being used.
	/// </summary>
	public string BaseUrl { get; } = "https://services.odata.org/V4/OData/OData.svc/";

	/// <inheritdoc/>
	public ValueTask InitializeAsync()
	{
		var services = new ServiceCollection();

		// Configure logging to show detailed output
		services.AddLogging(builder =>
		{
			builder
				.SetMinimumLevel(LogLevel.Debug)
				.AddSimpleConsole(options =>
				{
					options.IncludeScopes = true;
					options.SingleLine = false;
					options.TimestampFormat = "HH:mm:ss.fff ";
				});
		});

		// Build the service provider
		_serviceProvider = services.BuildServiceProvider();
		LoggerFactory = _serviceProvider.GetRequiredService<ILoggerFactory>();

		// Create the ODataClient with logging enabled
		var logger = LoggerFactory.CreateLogger<ODataClient>();
		Client = new ODataClient(new ODataClientOptions
		{
			BaseUrl = BaseUrl,
			Logger = logger,
			RetryCount = 1,
			RetryDelay = TimeSpan.FromMilliseconds(500)
		});

		return ValueTask.CompletedTask;
	}

	/// <inheritdoc/>
	public async ValueTask DisposeAsync()
	{
		Client?.Dispose();

		if (_serviceProvider is not null)
		{
			await _serviceProvider.DisposeAsync();
		}
	}
}
