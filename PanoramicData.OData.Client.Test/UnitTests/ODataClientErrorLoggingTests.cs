using PanoramicData.OData.Client.Exceptions;

namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Unit tests for the level at which failed responses are logged before the typed exception is thrown.
/// </summary>
public class ODataClientErrorLoggingTests : MockedODataClientTestBase
{
	private readonly List<LogLevel> _levels = [];

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
		_levels.Should().Equal(LogLevel.Information);
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
		_levels.Should().Equal(LogLevel.Error);
	}

	private async Task GetAsync(HttpStatusCode status)
	{
		SetupSendAsync().ReturnsAsync(() => new HttpResponseMessage(status) { Content = new StringContent("{}") });
		using var client = CreateClient(options => options.Logger = new LevelRecordingLogger(_levels));

		await client.GetAsync(client.For<Product>("Products"), CancellationToken);
	}

	private sealed class LevelRecordingLogger(List<LogLevel> levels) : ILogger
	{
		public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

		public bool IsEnabled(LogLevel logLevel) => true;

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
		{
			if (logLevel >= LogLevel.Information)
			{
				levels.Add(logLevel);
			}
		}
	}
}
