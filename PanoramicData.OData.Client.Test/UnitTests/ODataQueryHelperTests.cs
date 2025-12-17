using System.Net.Http.Headers;

namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Tests for new query helper methods (GetCountAsync, GetFirstOrDefaultAsync, etc.).
/// </summary>
public partial class ODataQueryHelperTests : TestBase
{
	/// <summary>
	/// Tests that GetCountAsync returns the correct count.
	/// </summary>
	[Fact]
	public async Task GetCountAsync_ShouldReturnCount()
	{
		// Arrange
		var handler = new TestMockHttpMessageHandler(request =>
		{
			request.RequestUri!.ToString().Should().Contain("/$count");
			return new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("42")
			};
		});

		using var httpClient = new HttpClient(handler)
		{
			BaseAddress = new Uri("https://services.odata.org/TripPinRESTierService/")
		};

		using var client = new ODataClient(new ODataClientOptions
		{
			BaseUrl = "https://services.odata.org/TripPinRESTierService/",
			HttpClient = httpClient
		});

		// Act
		var count = await client.GetCountAsync<Person>(cancellationToken: CancellationToken);

		// Assert
		count.Should().Be(42);
	}

	/// <summary>
	/// Tests that GetCountAsync includes filter in the URL when provided.
	/// </summary>
	[Fact]
	public async Task GetCountAsync_WithFilter_ShouldIncludeFilter()
	{
		// Arrange
		var handler = new TestMockHttpMessageHandler(request =>
		{
			var url = request.RequestUri!.ToString();
			url.Should().Contain("/$count");
			url.Should().Contain("$filter=");
			return new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("10")
			};
		});

		using var httpClient = new HttpClient(handler)
		{
			BaseAddress = new Uri("https://services.odata.org/TripPinRESTierService/")
		};

		using var client = new ODataClient(new ODataClientOptions
		{
			BaseUrl = "https://services.odata.org/TripPinRESTierService/",
			HttpClient = httpClient
		});

		var query = client.For<Person>().Filter("Age gt 21");

		// Act
		var count = await client.GetCountAsync(query, CancellationToken);

		// Assert
		count.Should().Be(10);
	}

	/// <summary>
	/// Tests that GetFirstOrDefaultAsync returns the first item from the result set.
	/// </summary>
	[Fact]
	public async Task GetFirstOrDefaultAsync_ShouldReturnFirstItem()
	{
		// Arrange
		var responseJson = """
		{
			"value": [
				{ "UserName": "john", "FirstName": "John", "LastName": "Doe" }
			]
		}
		""";

		var handler = new TestMockHttpMessageHandler(request =>
		{
			request.RequestUri!.ToString().Should().Contain("$top=1");
			var response = new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(responseJson)
			};
			response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
			return response;
		});

		using var httpClient = new HttpClient(handler)
		{
			BaseAddress = new Uri("https://services.odata.org/TripPinRESTierService/")
		};

		using var client = new ODataClient(new ODataClientOptions
		{
			BaseUrl = "https://services.odata.org/TripPinRESTierService/",
			HttpClient = httpClient
		});

		var query = client.For<Person>();

		// Act
		var result = await client.GetFirstOrDefaultAsync(query, CancellationToken);

		// Assert
		result.Should().NotBeNull();
		result.UserName.Should().Be("john");
	}

	/// <summary>
	/// Tests that GetFirstOrDefaultAsync returns null when no results match.
	/// </summary>
	[Fact]
	public async Task GetFirstOrDefaultAsync_EmptyResult_ShouldReturnNull()
	{
		// Arrange
		var responseJson = """{ "value": [] }""";

		var handler = new TestMockHttpMessageHandler(request =>
		{
			var response = new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(responseJson)
			};
			response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
			return response;
		});

		using var httpClient = new HttpClient(handler)
		{
			BaseAddress = new Uri("https://services.odata.org/TripPinRESTierService/")
		};

		using var client = new ODataClient(new ODataClientOptions
		{
			BaseUrl = "https://services.odata.org/TripPinRESTierService/",
			HttpClient = httpClient
		});

		var query = client.For<Person>().Filter("UserName eq 'nonexistent'");

		// Act
		var result = await client.GetFirstOrDefaultAsync(query, CancellationToken);

		// Assert
		result.Should().BeNull();
	}

	/// <summary>
	/// Tests that GetSingleAsync returns the single matching item.
	/// </summary>
	[Fact]
	public async Task GetSingleAsync_SingleResult_ShouldReturnItem()
	{
		// Arrange
		var responseJson = """
		{
			"value": [
				{ "UserName": "john", "FirstName": "John", "LastName": "Doe" }
			]
		}
		""";

		var handler = new TestMockHttpMessageHandler(request =>
		{
			var response = new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(responseJson)
			};
			response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
			return response;
		});

		using var httpClient = new HttpClient(handler)
		{
			BaseAddress = new Uri("https://services.odata.org/TripPinRESTierService/")
		};

		using var client = new ODataClient(new ODataClientOptions
		{
			BaseUrl = "https://services.odata.org/TripPinRESTierService/",
			HttpClient = httpClient
		});

		var query = client.For<Person>().Filter("UserName eq 'john'");

		// Act
		var result = await client.GetSingleAsync(query, CancellationToken);

		// Assert
		result.Should().NotBeNull();
		result.UserName.Should().Be("john");
	}

	/// <summary>
	/// Tests that GetSingleAsync throws when no results match.
	/// </summary>
	[Fact]
	public async Task GetSingleAsync_EmptyResult_ShouldThrow()
	{
		// Arrange
		var responseJson = """{ "value": [] }""";

		var handler = new TestMockHttpMessageHandler(request =>
		{
			var response = new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(responseJson)
			};
			response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
			return response;
		});

		using var httpClient = new HttpClient(handler)
		{
			BaseAddress = new Uri("https://services.odata.org/TripPinRESTierService/")
		};

		using var client = new ODataClient(new ODataClientOptions
		{
			BaseUrl = "https://services.odata.org/TripPinRESTierService/",
			HttpClient = httpClient
		});

		var query = client.For<Person>().Filter("UserName eq 'nonexistent'");

		// Act & Assert
		var act = () => client.GetSingleAsync(query, CancellationToken);
		await act.Should().ThrowExactlyAsync<InvalidOperationException>();
	}

	/// <summary>
	/// Tests that GetSingleAsync throws when multiple results match.
	/// </summary>
	[Fact]
	public async Task GetSingleAsync_MultipleResults_ShouldThrow()
	{
		// Arrange
		var responseJson = """
		{
			"value": [
				{ "UserName": "john", "FirstName": "John", "LastName": "Doe" },
				{ "UserName": "jane", "FirstName": "Jane", "LastName": "Doe" }
			]
		}
		""";

		var handler = new TestMockHttpMessageHandler(request =>
		{
			var response = new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(responseJson)
			};
			response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
			return response;
		});

		using var httpClient = new HttpClient(handler)
		{
			BaseAddress = new Uri("https://services.odata.org/TripPinRESTierService/")
		};

		using var client = new ODataClient(new ODataClientOptions
		{
			BaseUrl = "https://services.odata.org/TripPinRESTierService/",
			HttpClient = httpClient
		});

		var query = client.For<Person>().Filter("LastName eq 'Doe'");

		// Act & Assert
		var act = () => client.GetSingleAsync(query, CancellationToken);
		await act.Should().ThrowExactlyAsync<InvalidOperationException>();
	}

	/// <summary>
	/// Test entity representing a person.
	/// </summary>
	public class Person
	{
		/// <summary>
		/// Gets or sets the user name.
		/// </summary>
		public string UserName { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the first name.
		/// </summary>
		public string FirstName { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the last name.
		/// </summary>
		public string LastName { get; set; } = string.Empty;
	}
}