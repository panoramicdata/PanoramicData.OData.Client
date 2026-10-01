using PanoramicData.OData.Client.Exceptions;
using System.Net.Http.Headers;

namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Unit tests for OData singleton entity support.
/// </summary>
public class ODataClientSingletonTests : MockedODataClientTestBase
{
	#region GetSingletonAsync Tests

	/// <summary>
	/// Tests that GetSingletonAsync returns the singleton entity.
	/// </summary>
	[Fact]
	public async Task GetSingletonAsync_ReturnsSingletonEntity()
	{
		// Arrange
		MockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.Is<HttpRequestMessage>(m => m.RequestUri!.PathAndQuery == "/Me"),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("""
				{
					"UserName": "currentuser",
					"FirstName": "John",
					"LastName": "Doe"
				}
				""")
			});

		// Act
		var result = await Client.GetSingletonAsync<Person>("Me", cancellationToken: CancellationToken.None);

		// Assert
		result.Should().NotBeNull();
		result!.UserName.Should().Be("currentuser");
		result.FirstName.Should().Be("John");
	}

	/// <summary>
	/// Tests that GetSingletonAsync sends correct request URL.
	/// </summary>
	[Fact]
	public async Task GetSingletonAsync_SendsCorrectUrl()
	{
		// Arrange
		HttpRequestMessage? capturedRequest = null;

		SetupSendAsync()
			.Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("""{"UserName": "test"}""")
			});

		// Act
		await Client.GetSingletonAsync<Person>("Me", cancellationToken: CancellationToken.None);

		// Assert
		capturedRequest.Should().NotBeNull();
		capturedRequest!.Method.Should().Be(HttpMethod.Get);
		capturedRequest.RequestUri!.PathAndQuery.Should().Be("/Me");
	}

	/// <summary>
	/// Tests that GetSingletonAsync returns null for 404 response.
	/// </summary>
	[Fact]
	public async Task GetSingletonAsync_NotFound_ThrowsNotFoundException()
	{
		// Arrange
		SetupSendAsync()
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound)
			{
				Content = new StringContent("""{"error": {"message": "Singleton not found"}}""")
			});

		// Act
		var act = async () => await Client.GetSingletonAsync<Person>("Me", cancellationToken: CancellationToken.None);

		// Assert
		await act.Should().ThrowAsync<ODataNotFoundException>();
	}

	#endregion

	#region GetSingletonWithETagAsync Tests

	/// <summary>
	/// Tests that GetSingletonWithETagAsync returns the entity with ETag.
	/// </summary>
	[Fact]
	public async Task GetSingletonWithETagAsync_ReturnsEntityWithETag()
	{
		// Arrange
		var response = new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent("""{"UserName": "currentuser", "FirstName": "John"}""")
		};
		response.Headers.ETag = new EntityTagHeaderValue("\"singleton-v1\"", isWeak: true);

		SetupResponse(response);

		// Act
		var result = await Client.GetSingletonWithETagAsync<Person>("Me", cancellationToken: CancellationToken.None);

		// Assert
		result.Value.Should().NotBeNull();
		result.Value!.UserName.Should().Be("currentuser");
		result.ETag.Should().Be("W/\"singleton-v1\"");
	}

	#endregion

	#region UpdateSingletonAsync Tests

	/// <summary>
	/// Tests that UpdateSingletonAsync sends PATCH request to singleton URL.
	/// </summary>
	[Fact]
	public async Task UpdateSingletonAsync_SendsPatchRequest()
	{
		// Arrange
		HttpRequestMessage? capturedRequest = null;

		SetupSendAsync()
			.Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("""{"UserName": "currentuser", "FirstName": "Jane"}""")
			});

		// Act
		var result = await Client.UpdateSingletonAsync<Person>("Me", new { FirstName = "Jane" }, cancellationToken: CancellationToken.None);

		// Assert
		capturedRequest.Should().NotBeNull();
		capturedRequest!.Method.Should().Be(new HttpMethod("PATCH"));
		capturedRequest.RequestUri!.PathAndQuery.Should().Be("/Me");
		result.FirstName.Should().Be("Jane");
	}

	/// <summary>
	/// Tests that UpdateSingletonAsync with ETag sends If-Match header.
	/// </summary>
	[Fact]
	public async Task UpdateSingletonAsync_WithETag_SendsIfMatchHeader()
	{
		// Arrange
		HttpRequestMessage? capturedRequest = null;

		SetupSendAsync()
			.Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("""{"UserName": "currentuser"}""")
			});

		// Act
		await Client.UpdateSingletonAsync<Person>("Me", new { FirstName = "Jane" }, "W/\"v1\"", cancellationToken: CancellationToken.None);

		// Assert
		capturedRequest.Should().NotBeNull();
		capturedRequest!.Headers.IfMatch.Should().ContainSingle();
		capturedRequest.Headers.IfMatch.First().ToString().Should().Be("W/\"v1\"");
	}

	/// <summary>
	/// Tests that UpdateSingletonAsync with 204 response refetches the entity.
	/// </summary>
	[Fact]
	public async Task UpdateSingletonAsync_NoContent_RefetchesEntity()
	{
		// Arrange
		var callCount = 0;

		SetupSendAsync()
			.ReturnsAsync((HttpRequestMessage req, CancellationToken _) =>
			{
				callCount++;
				if (req.Method == new HttpMethod("PATCH"))
				{
					return new HttpResponseMessage(HttpStatusCode.NoContent);
				}
				else
				{
					return new HttpResponseMessage(HttpStatusCode.OK)
					{
						Content = new StringContent("""{"UserName": "currentuser", "FirstName": "Jane"}""")
					};
				}
			});

		// Act
		var result = await Client.UpdateSingletonAsync<Person>("Me", new { FirstName = "Jane" }, cancellationToken: CancellationToken.None);

		// Assert
		callCount.Should().Be(2); // PATCH + GET
		result.FirstName.Should().Be("Jane");
	}

	/// <summary>
	/// Tests that UpdateSingletonAsync throws ODataConcurrencyException on 412.
	/// </summary>
	[Fact]
	public async Task UpdateSingletonAsync_ConcurrencyConflict_ThrowsException()
	{
		// Arrange
		SetupSendAsync()
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.PreconditionFailed)
			{
				Content = new StringContent("""{"error": {"message": "Precondition Failed"}}""")
			});

		// Act
		var act = async () => await Client.UpdateSingletonAsync<Person>("Me", new { FirstName = "Jane" }, "W/\"old\"", cancellationToken: CancellationToken.None);

		// Assert
		await act.Should().ThrowAsync<ODataConcurrencyException>();
	}

	#endregion

	#region Complete Singleton Workflow Test

	/// <summary>
	/// Tests a complete singleton workflow: Get -> Update with concurrency.
	/// </summary>
	[Fact]
	public async Task Singleton_GetUpdateWorkflow_WorksCorrectly()
	{
		// Arrange
		var callCount = 0;

		SetupSendAsync()
			.ReturnsAsync((HttpRequestMessage _, CancellationToken _) =>
			{
				callCount++;
				if (callCount == 1)
				{
					var response = new HttpResponseMessage(HttpStatusCode.OK)
					{
						Content = new StringContent("""{"UserName": "me", "FirstName": "John"}""")
					};
					response.Headers.ETag = new EntityTagHeaderValue("\"v1\"", isWeak: true);
					return response;
				}
				else
				{
					return new HttpResponseMessage(HttpStatusCode.OK)
					{
						Content = new StringContent("""{"UserName": "me", "FirstName": "Jane"}""")
					};
				}
			});

		// Act
		var meWithETag = await Client.GetSingletonWithETagAsync<Person>("Me", cancellationToken: CancellationToken.None);
		var updated = await Client.UpdateSingletonAsync<Person>("Me", new { FirstName = "Jane" }, meWithETag.ETag, cancellationToken: CancellationToken.None);

		// Assert
		meWithETag.ETag.Should().Be("W/\"v1\"");
		updated.FirstName.Should().Be("Jane");
	}

	#endregion
}
