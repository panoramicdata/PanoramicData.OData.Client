using System.Text.Json;

namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Tests for navigation property hydration when using $expand with $select.
/// Verifies that expanded navigation properties are properly deserialized.
/// See: https://github.com/panoramicdata/PanoramicData.OData.Client/issues/1
/// </summary>
public class NavigationPropertyDeserializationTests : IDisposable
{
	private readonly Mock<HttpMessageHandler> _mockHandler;
	private readonly HttpClient _httpClient;
	private readonly ODataClient _client;
	private static readonly JsonSerializerOptions _jsonOptions = new()
	{
		PropertyNameCaseInsensitive = true
	};

	/// <summary>
	/// Initializes a new instance of the test class.
	/// </summary>
	public NavigationPropertyDeserializationTests()
	{
		_mockHandler = new Mock<HttpMessageHandler>();
		_httpClient = new HttpClient(_mockHandler.Object)
		{
			BaseAddress = new Uri("https://test.odata.org/")
		};
		_client = new ODataClient(new ODataClientOptions
		{
			BaseUrl = "https://test.odata.org/",
			HttpClient = _httpClient,
			Logger = NullLogger.Instance,
			RetryCount = 0
		});
	}

	/// <inheritdoc/>
	public void Dispose()
	{
		_client.Dispose();
		_httpClient.Dispose();
		GC.SuppressFinalize(this);
	}

	#region Test Models

	/// <summary>
	/// Simulates a ReportJob entity with navigation property.
	/// </summary>
	public class ReportJob
	{
		/// <summary>Gets or sets the ID.</summary>
		public int Id { get; set; }

		/// <summary>Gets or sets the name.</summary>
		public string Name { get; set; } = string.Empty;

		/// <summary>Gets or sets the foreign key to ReportBatchJob.</summary>
		public int? ReportBatchJobId { get; set; }

		/// <summary>Gets or sets the navigation property to ReportBatchJob.</summary>
		public ReportBatchJob? ReportBatchJob { get; set; }
	}

	/// <summary>
	/// Simulates a ReportBatchJob entity with nested navigation.
	/// </summary>
	public class ReportBatchJob
	{
		/// <summary>Gets or sets the ID.</summary>
		public int Id { get; set; }

		/// <summary>Gets or sets the foreign key to ReportSchedule.</summary>
		public int? ReportScheduleId { get; set; }

		/// <summary>Gets or sets the navigation property to ReportSchedule.</summary>
		public ReportSchedule? ReportSchedule { get; set; }
	}

	/// <summary>
	/// Simulates a ReportSchedule entity.
	/// </summary>
	public class ReportSchedule
	{
		/// <summary>Gets or sets the ID.</summary>
		public int Id { get; set; }

		/// <summary>Gets or sets the name.</summary>
		public string Name { get; set; } = string.Empty;

		/// <summary>Gets or sets whether to suppress warnings in UI.</summary>
		public bool SuppressWarningsInUi { get; set; }
	}

	#endregion

	/// <summary>
	/// Tests that expanded navigation properties are properly deserialized.
	/// This is the core issue from GitHub issue #1.
	/// </summary>
	[Fact]
	public async Task GetAsync_WithExpandedNavigationProperty_ShouldHydrateNavigationObject()
	{
		// Arrange - OData response with expanded navigation property
		var responseJson = """
		{
			"@odata.context": "https://test.odata.org/$metadata#ReportJobs",
			"value": [
				{
					"Id": 541,
					"Name": "Daily Report",
					"ReportBatchJobId": 635,
					"ReportBatchJob": {
						"Id": 635,
						"ReportScheduleId": 123
					}
				}
			]
		}
		""";

		SetupMockResponse(responseJson);

		// Act
		var query = _client.For<ReportJob>("ReportJobs")
			.Expand("ReportBatchJob");
		var response = await _client.GetAsync(query, CancellationToken.None);

		// Assert
		response.Value.Should().ContainSingle();
		var reportJob = response.Value[0];

		reportJob.Id.Should().Be(541);
		reportJob.Name.Should().Be("Daily Report");
		reportJob.ReportBatchJobId.Should().Be(635);

		// This is the key assertion - the navigation property should be hydrated
		reportJob.ReportBatchJob.Should().NotBeNull("Navigation property should be hydrated from expanded data");
		reportJob.ReportBatchJob!.Id.Should().Be(635);
		reportJob.ReportBatchJob.ReportScheduleId.Should().Be(123);
	}

	/// <summary>
	/// Tests that nested expanded navigation properties are properly deserialized.
	/// </summary>
	[Fact]
	public async Task GetAsync_WithNestedExpandedNavigation_ShouldHydrateAllLevels()
	{
		// Arrange - OData response with nested expanded navigation properties
		var responseJson = """
		{
			"@odata.context": "https://test.odata.org/$metadata#ReportJobs",
			"value": [
				{
					"Id": 541,
					"Name": "Daily Report",
					"ReportBatchJobId": 635,
					"ReportBatchJob": {
						"Id": 635,
						"ReportScheduleId": 123,
						"ReportSchedule": {
							"Id": 123,
							"Name": "Daily Schedule",
							"SuppressWarningsInUi": false
						}
					}
				}
			]
		}
		""";

		SetupMockResponse(responseJson);

		// Act
		var query = _client.For<ReportJob>("ReportJobs")
			.Expand("ReportBatchJob($expand=ReportSchedule)");
		var response = await _client.GetAsync(query, CancellationToken.None);

		// Assert
		response.Value.Should().ContainSingle();
		var reportJob = response.Value[0];

		reportJob.ReportBatchJob.Should().NotBeNull("First level navigation should be hydrated");
		reportJob.ReportBatchJob!.ReportSchedule.Should().NotBeNull("Nested navigation should be hydrated");
		reportJob.ReportBatchJob.ReportSchedule!.Id.Should().Be(123);
		reportJob.ReportBatchJob.ReportSchedule.Name.Should().Be("Daily Schedule");
		reportJob.ReportBatchJob.ReportSchedule.SuppressWarningsInUi.Should().BeFalse();
	}

	/// <summary>
	/// Tests expand with select still hydrates navigation properties.
	/// </summary>
	[Fact]
	public async Task GetAsync_WithExpandAndSelect_ShouldHydrateNavigationObject()
	{
		// Arrange - OData response when using $expand with $select on navigation property fields
		var responseJson = """
		{
			"@odata.context": "https://test.odata.org/$metadata#ReportJobs",
			"value": [
				{
					"Id": 541,
					"Name": "Daily Report",
					"ReportBatchJobId": 635,
					"ReportBatchJob": {
						"ReportScheduleId": 123
					}
				}
			]
		}
		""";

		SetupMockResponse(responseJson);

		// Act
		var query = _client.For<ReportJob>("ReportJobs")
			.Expand("ReportBatchJob($select=ReportScheduleId)")
			.Select("Id,Name,ReportBatchJobId");
		var response = await _client.GetAsync(query, CancellationToken.None);

		// Assert
		response.Value.Should().ContainSingle();
		var reportJob = response.Value[0];

		reportJob.ReportBatchJob.Should().NotBeNull("Navigation property should be hydrated even with $select");
		reportJob.ReportBatchJob!.ReportScheduleId.Should().Be(123);
	}

	/// <summary>
	/// Tests that null navigation properties remain null.
	/// </summary>
	[Fact]
	public async Task GetAsync_WithNullNavigationProperty_ShouldRemainNull()
	{
		// Arrange - OData response with null navigation property
		var responseJson = """
		{
			"@odata.context": "https://test.odata.org/$metadata#ReportJobs",
			"value": [
				{
					"Id": 541,
					"Name": "Orphan Report",
					"ReportBatchJobId": null,
					"ReportBatchJob": null
				}
			]
		}
		""";

		SetupMockResponse(responseJson);

		// Act
		var query = _client.For<ReportJob>("ReportJobs")
			.Expand("ReportBatchJob");
		var response = await _client.GetAsync(query, CancellationToken.None);

		// Assert
		response.Value.Should().ContainSingle();
		var reportJob = response.Value[0];

		reportJob.ReportBatchJobId.Should().BeNull();
		reportJob.ReportBatchJob.Should().BeNull();
	}

	/// <summary>
	/// Verifies that System.Text.Json correctly deserializes nested objects.
	/// This is a baseline test to confirm JSON deserialization works as expected.
	/// </summary>
	[Fact]
	public void JsonSerializer_Deserialize_ShouldHydrateNestedObjects()
	{
		// Arrange
		var json = """
		{
			"Id": 541,
			"Name": "Daily Report",
			"ReportBatchJobId": 635,
			"ReportBatchJob": {
				"Id": 635,
				"ReportScheduleId": 123,
				"ReportSchedule": {
					"Id": 123,
					"Name": "Daily Schedule",
					"SuppressWarningsInUi": false
				}
			}
		}
		""";

		// Act
		var reportJob = JsonSerializer.Deserialize<ReportJob>(json, _jsonOptions);

		// Assert
		reportJob.Should().NotBeNull();
		reportJob!.ReportBatchJob.Should().NotBeNull("Nested object should be deserialized");
		reportJob.ReportBatchJob!.ReportSchedule.Should().NotBeNull("Deeply nested object should be deserialized");
	}

	/// <summary>
	/// Tests GetByKeyAsync with expanded navigation properties.
	/// </summary>
	[Fact]
	public async Task GetByKeyAsync_WithExpand_ShouldHydrateNavigationObject()
	{
		// Arrange - Single entity response with expanded navigation
		var responseJson = """
		{
			"@odata.context": "https://test.odata.org/$metadata#ReportJobs/$entity",
			"Id": 541,
			"Name": "Daily Report",
			"ReportBatchJobId": 635,
			"ReportBatchJob": {
				"Id": 635,
				"ReportScheduleId": 123
			}
		}
		""";

		SetupMockResponse(responseJson);

		// Act
		var query = _client.For<ReportJob>("ReportJobs")
			.Expand("ReportBatchJob");
		var reportJob = await _client.GetByKeyAsync<ReportJob, int>(541, query, CancellationToken.None);

		// Assert
		reportJob.Should().NotBeNull();
		reportJob!.ReportBatchJob.Should().NotBeNull("Navigation property should be hydrated");
		reportJob.ReportBatchJob!.Id.Should().Be(635);
		reportJob.ReportBatchJob.ReportScheduleId.Should().Be(123);
	}

	private void SetupMockResponse(string responseJson) => _mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json")
			});
}
