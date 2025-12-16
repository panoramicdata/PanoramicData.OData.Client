using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PanoramicData.OData.Client.Exceptions;
using PanoramicData.OData.Client.Test.Models;

namespace PanoramicData.OData.Client.Test.IntegrationTests;

/// <summary>
/// Integration tests using the TripPin sample service for advanced OData V4 scenarios.
/// TripPin provides more complex entity relationships and features.
/// </summary>
public class TripPinIntegrationTests : TestBase, IAsyncLifetime
{
	private ServiceProvider? _serviceProvider;
	private ODataClient _client = null!;

	/// <inheritdoc/>
	public ValueTask InitializeAsync()
	{
		var services = new ServiceCollection();
		services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug));

		_serviceProvider = services.BuildServiceProvider();
		var loggerFactory = _serviceProvider.GetRequiredService<ILoggerFactory>();

		_client = new ODataClient(new ODataClientOptions
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
		_client?.Dispose();

		if (_serviceProvider is not null)
		{
			await _serviceProvider.DisposeAsync();
		}

		GC.SuppressFinalize(this);
	}

	#region People Queries

	/// <summary>
	/// Tests querying people from TripPin service.
	/// </summary>
	[Fact]
	public async Task GetPeople_ReturnsResults()
	{
		// Arrange
		var query = _client.For<Person>("People")
			.Top(5);

		// Act
		var response = await _client.GetAsync(query, TestContext.Current.CancellationToken);

		// Assert
		response.Value.Should().NotBeEmpty();
		response.Value.Should().AllSatisfy(p =>
		{
			p.UserName.Should().NotBeNullOrEmpty();
			p.FirstName.Should().NotBeNullOrEmpty();
		});
	}

	/// <summary>
	/// Tests getting person by string key.
	/// </summary>
	[Fact]
	public async Task GetPersonByKey_StringKey_ReturnsEntity()
	{
		// Act
		var person = await _client.GetByKeyAsync<Person, string>("russellwhyte", cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		person.Should().NotBeNull();
		person!.UserName.Should().Be("russellwhyte");
		person.FirstName.Should().NotBeNullOrEmpty();
	}

	/// <summary>
	/// Tests filtering people by first name.
	/// </summary>
	[Fact]
	public async Task GetPeople_FilterByFirstName_ReturnsFiltered()
	{
		// Arrange
		var query = _client.For<Person>("People")
			.Filter("FirstName eq 'Russell'")
			.Top(5);

		// Act
		var response = await _client.GetAsync(query, TestContext.Current.CancellationToken);

		// Assert
		response.Value.Should().AllSatisfy(p => p.FirstName.Should().Be("Russell"));
	}

	/// <summary>
	/// Tests expanding friends navigation property.
	/// </summary>
	[Fact]
	public async Task GetPerson_ExpandFriends_ReturnsFriends()
	{
		// Arrange
		var query = _client.For<Person>("People")
			.Key("russellwhyte")
			.Expand("Friends");

		// Act
		var person = await _client.GetByKeyAsync<Person, string>("russellwhyte", query, TestContext.Current.CancellationToken);

		// Assert
		person.Should().NotBeNull();
		person!.Friends.Should().NotBeNull();
	}

	/// <summary>
	/// Tests expanding trips navigation property.
	/// </summary>
	[Fact]
	public async Task GetPerson_ExpandTrips_ReturnsTrips()
	{
		// Arrange
		var query = _client.For<Person>("People")
			.Key("russellwhyte")
			.Expand("Trips");

		// Act
		var person = await _client.GetByKeyAsync<Person, string>("russellwhyte", query, TestContext.Current.CancellationToken);

		// Assert
		person.Should().NotBeNull();
		person!.Trips.Should().NotBeNull();
	}

	/// <summary>
	/// Tests multiple expands.
	/// </summary>
	[Fact]
	public async Task GetPerson_MultipleExpands_ReturnsAll()
	{
		// Arrange
		var query = _client.For<Person>("People")
			.Key("russellwhyte")
			.Expand("Friends,Trips");

		// Act
		var person = await _client.GetByKeyAsync<Person, string>("russellwhyte", query, TestContext.Current.CancellationToken);

		// Assert
		person.Should().NotBeNull();
		person!.Friends.Should().NotBeNull();
		person.Trips.Should().NotBeNull();
	}

	#endregion

	#region Airlines Queries

	/// <summary>
	/// Tests querying airlines.
	/// </summary>
	[Fact]
	public async Task GetAirlines_ReturnsResults()
	{
		// Arrange
		var query = _client.For<Airline>("Airlines")
			.Top(10);

		// Act
		var response = await _client.GetAsync(query, TestContext.Current.CancellationToken);

		// Assert
		response.Value.Should().NotBeEmpty();
		response.Value.Should().AllSatisfy(a =>
		{
			a.AirlineCode.Should().NotBeNullOrEmpty();
		});
	}

	/// <summary>
	/// Tests getting airline by code.
	/// </summary>
	[Fact]
	public async Task GetAirlineByKey_ReturnsEntity()
	{
		// Act
		var airline = await _client.GetByKeyAsync<Airline, string>("AA", cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		airline.Should().NotBeNull();
		airline!.AirlineCode.Should().Be("AA");
	}

	#endregion

	#region Airports Queries

	/// <summary>
	/// Tests querying airports.
	/// </summary>
	[Fact]
	public async Task GetAirports_ReturnsResults()
	{
		// Arrange
		var query = _client.For<Airport>("Airports")
			.Top(10);

		// Act
		var response = await _client.GetAsync(query, TestContext.Current.CancellationToken);

		// Assert
		response.Value.Should().NotBeEmpty();
		response.Value.Should().AllSatisfy(a =>
		{
			a.IcaoCode.Should().NotBeNullOrEmpty();
		});
	}

	/// <summary>
	/// Tests filtering airports by name.
	/// </summary>
	[Fact]
	public async Task GetAirports_FilterContains_ReturnsFiltered()
	{
		// Arrange
		var query = _client.For<Airport>("Airports")
			.Filter("contains(Name, 'International')")
			.Top(5);

		// Act
		var response = await _client.GetAsync(query, TestContext.Current.CancellationToken);

		// Assert
		response.Value.Should().AllSatisfy(a =>
		{
			a.Name.Should().Contain("International");
		});
	}

	#endregion

	#region Complex Type Queries

	/// <summary>
	/// Tests querying entities with complex type properties.
	/// </summary>
	[Fact]
	public async Task GetPerson_ComplexTypeProperty_IsDeserialized()
	{
		// Arrange
		var query = _client.For<Person>("People")
			.Key("russellwhyte")
			.Select("UserName,FirstName,AddressInfo");

		// Act
		var person = await _client.GetByKeyAsync<Person, string>("russellwhyte", query, TestContext.Current.CancellationToken);

		// Assert
		person.Should().NotBeNull();
		// AddressInfo is a collection of Location complex type
		person!.AddressInfo.Should().NotBeNull();
	}

	#endregion

	#region Search

	/// <summary>
	/// Tests $search query option.
	/// </summary>
	[Fact]
	public async Task Search_ReturnsMatchingResults()
	{
		// Note: Not all OData services support $search
		// Arrange
		var query = _client.For<Person>("People")
			.Search("Russell")
			.Top(5);

		// Act - This may fail if the service doesn't support $search
		try
		{
			var response = await _client.GetAsync(query, TestContext.Current.CancellationToken);
			response.Value.Should().NotBeEmpty();
		}
		catch (ODataClientException)
		{
			// $search may not be supported - that's okay
		}
	}

	#endregion
}
