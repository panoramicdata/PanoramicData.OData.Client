using PanoramicData.OData.Client.Exceptions;
using System.Net.Http.Headers;

namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Unit tests for ETag and optimistic concurrency support.
/// </summary>
public class ODataClientETagTests : MockedODataClientTestBase
{
	private const string WidgetJson = """{"Id": 1, "Name": "Widget"}""";
	private const string WeakAbc = "W/\"abc123\"";

	/// <summary>
	/// Tests that GetByKeyWithETagAsync returns the ETag header, or null when absent.
	/// </summary>
	[Fact]
	public async Task GetByKeyWithETagAsync_ReturnsETagWhenPresent()
	{
		SetupResponse(Respond(HttpStatusCode.OK, WidgetJson, "abc123"));
		var withETag = await Client.GetByKeyWithETagAsync<Product, int>(1, cancellationToken: CancellationToken.None);

		SetupResponse(Respond(HttpStatusCode.OK, WidgetJson));
		var withoutETag = await Client.GetByKeyWithETagAsync<Product, int>(1, cancellationToken: CancellationToken.None);

		withETag.Value!.Id.Should().Be(1);
		withETag.ETag.Should().Be(WeakAbc);
		withoutETag.Value.Should().NotBeNull();
		withoutETag.ETag.Should().BeNull();
	}

	/// <summary>
	/// Tests that UpdateAsync sends If-Match only when an ETag is supplied.
	/// </summary>
	[Fact]
	public async Task UpdateAsync_SendsIfMatchOnlyWithETag()
	{
		SetupCapturingResponse(Respond(HttpStatusCode.OK, WidgetJson));

		await Client.UpdateAsync<Product>("Products", 1, new { Name = "Updated Widget" }, WeakAbc, cancellationToken: CancellationToken.None);
		CapturedRequest!.Headers.IfMatch.Should().ContainSingle().Which.ToString().Should().Be(WeakAbc);

		SetupCapturingResponse(Respond(HttpStatusCode.OK, WidgetJson));
		await Client.UpdateAsync<Product>("Products", 1, new { Name = "Updated Widget" }, cancellationToken: CancellationToken.None);
		CapturedRequest!.Headers.IfMatch.Should().BeEmpty();
	}

	/// <summary>
	/// Tests that DeleteAsync sends If-Match when an ETag is supplied.
	/// </summary>
	[Fact]
	public async Task DeleteAsync_WithETag_SendsIfMatchHeader()
	{
		SetupCapturingResponse(new HttpResponseMessage(HttpStatusCode.NoContent));

		await Client.DeleteAsync("Products", 1, WeakAbc, cancellationToken: CancellationToken.None);

		CapturedRequest!.Headers.IfMatch.Should().ContainSingle().Which.ToString().Should().Be(WeakAbc);
	}

	/// <summary>
	/// Tests that a 412 on update surfaces both ETags in the concurrency exception.
	/// </summary>
	[Fact]
	public async Task UpdateAsync_ConcurrencyConflict_ThrowsODataConcurrencyException()
	{
		SetupResponse(Respond(HttpStatusCode.PreconditionFailed, """{"error": {"message": "Precondition Failed"}}""", "def456"));

		var act = () => Client.UpdateAsync<Product>("Products", 1, new { Name = "Updated" }, WeakAbc, cancellationToken: CancellationToken.None);

		var exception = await act.Should().ThrowAsync<ODataConcurrencyException>();
		exception.Which.RequestETag.Should().Be(WeakAbc);
		exception.Which.CurrentETag.Should().Be("W/\"def456\"");
		exception.Which.StatusCode.Should().Be(412);
	}

	/// <summary>
	/// Tests that a 412 on delete throws the concurrency exception.
	/// </summary>
	[Fact]
	public async Task DeleteAsync_ConcurrencyConflict_ThrowsODataConcurrencyException()
	{
		SetupResponse(Respond(HttpStatusCode.PreconditionFailed, """{"error": {"message": "Precondition Failed"}}"""));

		var act = () => Client.DeleteAsync("Products", 1, WeakAbc, cancellationToken: CancellationToken.None);

		await act.Should().ThrowAsync<ODataConcurrencyException>();
	}

	/// <summary>
	/// Tests that a collection response carries its ETag.
	/// </summary>
	[Fact]
	public async Task GetAsync_WithETag_ReturnsETagInResponse()
	{
		SetupResponse(Respond(HttpStatusCode.OK, """{"value": [{"Id": 1, "Name": "Widget"}]}""", "collection123"));

		var result = await Client.GetAsync(Client.For<Product>("Products").Top(10), CancellationToken.None);

		result.ETag.Should().Be("W/\"collection123\"");
		result.Value.Should().ContainSingle();
	}

	/// <summary>
	/// Tests the get-then-update workflow reuses the ETag from the get.
	/// </summary>
	[Fact]
	public async Task OptimisticConcurrency_GetThenUpdate_WorksCorrectly()
	{
		HttpRequestMessage? updateRequest = null;
		var callCount = 0;
		SetupSendAsync().ReturnsAsync((HttpRequestMessage request, CancellationToken _) =>
		{
			callCount++;
			if (callCount == 1)
			{
				return Respond(HttpStatusCode.OK, """{"Id": 1, "Name": "Widget", "Price": 100}""", "v1");
			}

			updateRequest = request;
			return Respond(HttpStatusCode.OK, """{"Id": 1, "Name": "Updated Widget", "Price": 100}""");
		});

		var etag = (await Client.GetByKeyWithETagAsync<Product, int>(1, cancellationToken: CancellationToken.None)).ETag;
		var updated = await Client.UpdateAsync<Product>("Products", 1, new { Name = "Updated Widget" }, etag, cancellationToken: CancellationToken.None);

		etag.Should().Be("W/\"v1\"");
		updateRequest!.Headers.IfMatch.Should().ContainSingle().Which.ToString().Should().Be("W/\"v1\"");
		updated.Name.Should().Be("Updated Widget");
	}

	private static HttpResponseMessage Respond(HttpStatusCode status, string json, string? weakETag = null)
	{
		var response = new HttpResponseMessage(status) { Content = new StringContent(json) };
		if (weakETag is not null)
		{
			response.Headers.ETag = new EntityTagHeaderValue($"\"{weakETag}\"", isWeak: true);
		}

		return response;
	}
}
