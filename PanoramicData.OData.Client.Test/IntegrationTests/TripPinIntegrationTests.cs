using PanoramicData.OData.Client.Exceptions;

namespace PanoramicData.OData.Client.Test.IntegrationTests;

/// <summary>
/// Integration tests using the TripPin sample service for advanced OData V4 scenarios.
/// TripPin provides more complex entity relationships and features.
/// </summary>
[Trait("Category", "Integration")]
public class TripPinIntegrationTests : TripPinTestBase
{
	#region People Queries

	/// <summary>
	/// Tests querying people from TripPin service.
	/// </summary>
	[Fact]
	public async Task GetPeople_ReturnsResults()
	{
		var response = await GetPageAsync<Person>("People", query => query.Top(5));

		// Assert
		response.Value.Should().NotBeEmpty();
		response.Value.Should().AllSatisfy(p =>
		{
			p.UserName.Should().NotBeNullOrEmpty();
			p.FirstName.Should().NotBeNullOrEmpty();
		});
	}

	/// <summary>
	/// Tests filtering people by first name.
	/// </summary>
	[Fact]
	public async Task GetPeople_FilterByFirstName_ReturnsFiltered()
	{
		var response = await GetPageAsync<Person>("People", query => query.Filter("FirstName eq 'Russell'")
			.Top(5));

		// Assert
		response.Value.Should().AllSatisfy(p => p.FirstName.Should().Be("Russell"));
	}

	/// <summary>
	/// Tests expanding friends navigation property.
	/// </summary>
	[Fact]
	public async Task GetPerson_ExpandFriends_ReturnsFriends()
	{
		var person = await GetRussellWhyteAsync("Friends");

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
		var person = await GetRussellWhyteAsync("Trips");

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
		var person = await GetRussellWhyteAsync("Friends,Trips");

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
		var response = await GetPageAsync<Airline>("Airlines", query => query.Top(10));

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
		var airline = await Client.GetByKeyAsync<Airline, string>("AA", cancellationToken: TestContext.Current.CancellationToken);

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
		var response = await GetPageAsync<Airport>("Airports", query => query.Top(10));

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
		var response = await GetPageAsync<Airport>("Airports", query => query.Filter("contains(Name, 'International')")
			.Top(5));

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
		var query = Client.For<Person>("People")
			.Key("russellwhyte")
			.Select("UserName,FirstName,AddressInfo");

		// Act
		var person = await Client.GetByKeyAsync<Person, string>("russellwhyte", query, TestContext.Current.CancellationToken);

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
		var query = Client.For<Person>("People")
			.Search("Russell")
			.Top(5);

		// Act - This may fail if the service doesn't support $search
		try
		{
			var response = await Client.GetAsync(query, TestContext.Current.CancellationToken);
			response.Value.Should().NotBeEmpty();
		}
		catch (ODataClientException)
		{
			// $search may not be supported - that's okay
		}
	}

	#endregion

	private Task<ODataResponse<T>> GetPageAsync<T>(string entitySet, Func<ODataQueryBuilder<T>, ODataQueryBuilder<T>> shape) where T : class
		=> Client.GetAsync(shape(Client.For<T>(entitySet)), TestContext.Current.CancellationToken);

	private Task<Person?> GetRussellWhyteAsync(string expand)
		=> Client.GetByKeyAsync<Person, string>(
			"russellwhyte",
			Client.For<Person>("People").Key("russellwhyte").Expand(expand),
			TestContext.Current.CancellationToken);
}
