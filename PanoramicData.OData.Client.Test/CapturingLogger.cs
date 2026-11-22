namespace PanoramicData.OData.Client.Test;

/// <summary>
/// A minimal ILogger that captures log entries for assertions.
/// </summary>
internal sealed class CapturingLogger : ILogger
{
	public List<(LogLevel Level, EventId EventId, string Message)> Entries { get; } = [];

	public IDisposable? BeginScope<TState>(TState state) where TState : notnull
	{
		ArgumentNullException.ThrowIfNull(state);
		return null;
	}

	public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

	public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
		=> Entries.Add((logLevel, eventId, formatter(state, exception)));
}
