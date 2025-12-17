using PanoramicData.OData.Client.Exceptions;

namespace PanoramicData.OData.Client.Test.IntegrationTests;

/// <summary>
/// Integration tests for CRUD operations (Create, Read, Update, Delete).
/// Uses the TripPin read-write sample service which provides a unique session per request.
/// Note: The TripPin service has specific behavior - entities may not persist across calls.
/// </summary>
public class CrudIntegrationTests : TestBase, IAsyncLifetime
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

	#region Create Tests

	/// <summary>
	/// Tests that CreateAsync creates a new entity without throwing.
	/// Note: TripPin service may not echo back all fields correctly.
	/// </summary>
	[Fact]
	public async Task CreateAsync_NewPerson_Succeeds()
	{
		// Arrange
		var uniqueId = Guid.NewGuid().ToString("N")[..8];
		var newPerson = new Person
		{
			UserName = $"testuser{uniqueId}",
			FirstName = "Test",
			LastName = "User",
			Emails = [$"test{uniqueId}@example.com"]
		};

		// Act
		var created = await _client.CreateAsync("People", newPerson, cancellationToken: CancellationToken);

		// Assert - Service creates the entity (TripPin may return minimal response)
		created.Should().NotBeNull();
	}

	#endregion

	#region Read Tests

	/// <summary>
	/// Tests that GetByKeyAsync returns an existing entity.
	/// </summary>
	[Fact]
	public async Task GetByKeyAsync_ExistingPerson_ReturnsEntity()
	{
		// Arrange - Create a query with explicit entity set name
		var query = _client.For<Person>("People");

		// Act
		var person = await _client.GetByKeyAsync<Person, string>("russellwhyte", query, CancellationToken);

		// Assert
		person.Should().NotBeNull();
		person!.UserName.Should().Be("russellwhyte");
		person.FirstName.Should().Be("Russell");
	}

	/// <summary>
	/// Tests that GetByKeyAsync throws when entity not found.
	/// </summary>
	[Fact]
	public async Task GetByKeyAsync_NonExistentEntity_ThrowsException()
	{
		// Arrange
		var query = _client.For<Person>("People");

		// Act
		var act = async () => await _client.GetByKeyAsync<Person, string>(
			"nonexistent_user_xyz_12345",
			query,
			CancellationToken);

		// Assert - Service may throw different exception types
		await act.Should().ThrowAsync<Exception>();
	}

	#endregion

	#region Delete Tests

	/// <summary>
	/// Tests that DeleteAsync on non-existent entity throws.
	/// </summary>
	[Fact]
	public async Task DeleteAsync_NonExistentEntity_ThrowsException()
	{
		// Arrange & Act
		var act = async () => await _client.DeleteAsync(
			"People",
			"nonexistent_user_to_delete_xyz",
			cancellationToken: CancellationToken);

		// Assert - Service may throw ODataClientException or ODataNotFoundException
		await act.Should().ThrowAsync<ODataClientException>();
	}

	#endregion

	#region Query Tests

	/// <summary>
	/// Tests querying entities with filters.
	/// </summary>
	[Fact]
	public async Task GetAsync_WithFilter_ReturnsFilteredResults()
	{
		// Arrange
		var query = _client.For<Person>("People")
			.Filter("FirstName eq 'Russell'")
			.Top(5);

		// Act
		var response = await _client.GetAsync(query, CancellationToken);

		// Assert
		response.Value.Should().AllSatisfy(p => p.FirstName.Should().Be("Russell"));
	}

	/// <summary>
	/// Tests querying entities with expand.
	/// </summary>
	[Fact]
	public async Task GetAsync_WithExpand_ReturnsRelatedEntities()
	{
		// Arrange
		var query = _client.For<Person>("People")
			.Key("russellwhyte")
			.Expand("Friends");

		// Act
		var person = await _client.GetByKeyAsync<Person, string>("russellwhyte", query, CancellationToken);

		// Assert
		person.Should().NotBeNull();
		person!.Friends.Should().NotBeNull();
	}

	/// <summary>
	/// Tests querying entities with select.
	/// </summary>
	[Fact]
	public async Task GetAsync_WithSelect_ReturnsSelectedFields()
	{
		// Arrange
		var query = _client.For<Person>("People")
			.Select("UserName,FirstName")
			.Top(3);

		// Act
		var response = await _client.GetAsync(query, CancellationToken);

		// Assert
		response.Value.Should().NotBeEmpty();
		response.Value.Should().AllSatisfy(p =>
		{
			p.UserName.Should().NotBeNullOrEmpty();
			p.FirstName.Should().NotBeNullOrEmpty();
		});
	}

	/// <summary>
	/// Tests querying entities with order by.
	/// </summary>
	[Fact]
	public async Task GetAsync_WithOrderBy_ReturnsOrderedResults()
	{
		// Arrange
		var query = _client.For<Person>("People")
			.OrderBy("FirstName")
			.Top(5);

		// Act
		var response = await _client.GetAsync(query, CancellationToken);

		// Assert
		response.Value.Should().NotBeEmpty();
		var names = response.Value.Select(p => p.FirstName).ToList();
		names.Should().BeInAscendingOrder();
	}

	/// <summary>
	/// Tests querying entities with skip and top.
	/// </summary>
	[Fact]
	public async Task GetAsync_WithSkipAndTop_ReturnsPaginatedResults()
	{
		// Arrange
		var queryPage1 = _client.For<Person>("People")
			.OrderBy("UserName")
			.Skip(0)
			.Top(2);

		var queryPage2 = _client.For<Person>("People")
			.OrderBy("UserName")
			.Skip(2)
			.Top(2);

		// Act
		var page1 = await _client.GetAsync(queryPage1, CancellationToken);
		var page2 = await _client.GetAsync(queryPage2, CancellationToken);

		// Assert
		page1.Value.Should().NotBeEmpty();
		page2.Value.Should().NotBeEmpty();
		page1.Value.Select(p => p.UserName).Should().NotIntersectWith(page2.Value.Select(p => p.UserName));
	}

	#endregion
}
