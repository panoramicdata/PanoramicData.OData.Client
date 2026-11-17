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

	/// <summary>
	/// Runs a typed query over <paramref name="entitySet"/> shaped by <paramref name="shape"/> and returns one page.
	/// </summary>
	protected Task<ODataResponse<T>> GetPageAsync<T>(string entitySet, Func<ODataQueryBuilder<T>, ODataQueryBuilder<T>> shape)
		where T : class
		=> GetPageAsync(Client, entitySet, shape);

	/// <summary>
	/// Fetches the russellwhyte person expanding <paramref name="expand"/>.
	/// </summary>
	protected Task<Person?> GetRussellWhyteAsync(string expand)
		=> Client.GetByKeyAsync<Person, string>(
			"russellwhyte",
			Client.For<Person>("People").Key("russellwhyte").Expand(expand),
			CancellationToken);

	/// <summary>
	/// Asserts a page of people is not empty and everyone has a user name and first name.
	/// </summary>
	protected static void AssertPeopleHaveNames(ODataResponse<Person> response)
	{
		response.Value.Should().NotBeEmpty();
		response.Value.Should().AllSatisfy(p =>
		{
			p.UserName.Should().NotBeNullOrEmpty();
			p.FirstName.Should().NotBeNullOrEmpty();
		});
	}

	/// <summary>
	/// Asserts that filtering people by first name returns only Russells.
	/// </summary>
	protected async Task AssertFilterByFirstNameAsync()
	{
		var response = await GetPageAsync<Person>("People", query => query.Filter("FirstName eq 'Russell'")
			.Top(5));

		response.Value.Should().AllSatisfy(p => p.FirstName.Should().Be("Russell"));
	}

	/// <summary>
	/// Asserts that expanding Friends on russellwhyte loads the navigation property.
	/// </summary>
	protected async Task AssertExpandFriendsAsync()
	{
		var person = await GetRussellWhyteAsync("Friends");

		person.Should().NotBeNull();
		person!.Friends.Should().NotBeNull();
	}
}
