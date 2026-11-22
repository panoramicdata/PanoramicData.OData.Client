using PanoramicData.OData.Client.Exceptions;

namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Unit tests for the level at which failed responses are logged before the typed exception is thrown.
/// </summary>
public class ODataClientErrorLoggingTests : MockedODataClientTestBase
{
	private readonly CapturingLogger _logger = new();

	/// <summary>
	/// Tests that 401, 403 and 404 are logged below Error while the typed exception is still thrown.
	/// </summary>
	[Theory]
	[InlineData(HttpStatusCode.Unauthorized, typeof(ODataUnauthorizedException))]
	[InlineData(HttpStatusCode.Forbidden, typeof(ODataForbiddenException))]
	[InlineData(HttpStatusCode.NotFound, typeof(ODataNotFoundException))]
	public async Task Request_ExpectedClientError_LogsInformationAndThrows(HttpStatusCode status, Type exceptionType)
	{
		var exception = await Record.ExceptionAsync(() => GetAsync(status));

		exception.Should().BeOfType(exceptionType);
		FailureLevels().Should().Equal(LogLevel.Information);
	}

	/// <summary>
	/// Tests that other failures keep logging at Error.
	/// </summary>
	[Theory]
	[InlineData(HttpStatusCode.BadRequest)]
	[InlineData(HttpStatusCode.InternalServerError)]
	public async Task Request_OtherFailure_LogsError(HttpStatusCode status)
	{
		var exception = await Record.ExceptionAsync(() => GetAsync(status));

		exception.Should().BeOfType<ODataClientException>();
		FailureLevels().Should().Equal(LogLevel.Error);
	}

	private IEnumerable<LogLevel> FailureLevels()
		=> _logger.Entries.Where(entry => entry.Level >= LogLevel.Information).Select(entry => entry.Level);

	private async Task GetAsync(HttpStatusCode status)
	{
		SetupSendAsync().ReturnsAsync(() => new HttpResponseMessage(status) { Content = new StringContent("{}") });
		using var client = CreateClient(options => options.Logger = _logger);

		await client.GetAsync(client.For<Product>("Products"), CancellationToken);
	}
}
