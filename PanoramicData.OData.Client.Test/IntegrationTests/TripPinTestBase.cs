namespace PanoramicData.OData.Client.Test.IntegrationTests;

/// <summary>
/// Base class for tests that talk to the TripPin read-write sample service with debug logging.
/// </summary>
public abstract class TripPinTestBase : TestBase, IAsyncLifetime
{
	private ServiceProvider? _serviceProvider;

	/// <summary>
	/// The client pointed at the TripPin service, created by <see cref="InitializeAsync"/>.
	/// </summary>
	protected ODataClient Client { get; private set; } = null!;

	/// <inheritdoc/>
	public ValueTask InitializeAsync()
	{
		var services = new ServiceCollection();
		services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug));

		_serviceProvider = services.BuildServiceProvider();
		var loggerFactory = _serviceProvider.GetRequiredService<ILoggerFactory>();

		Client = new ODataClient(new ODataClientOptions
		{
			BaseUrl = TripPinV4ReadWriteUri,
			Logger = loggerFactory.CreateLogger<ODataClient>(),
			RetryCount = 2,
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

		GC.SuppressFinalize(this);
	}
}
