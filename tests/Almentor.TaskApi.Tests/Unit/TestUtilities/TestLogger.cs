using Microsoft.Extensions.Logging;

namespace Almentor.TaskApi.Tests.Unit.TestUtilities;

/// <summary>
/// Records log calls for assertion. Preferred over mocking ILogger.Log&lt;TState&gt;
/// with NSubstitute — that method is generic and invoked with a compiler-
/// synthesized state type, which makes mock argument matching brittle. A real
/// implementation that just records is simpler and doesn't depend on framework internals.
/// </summary>
public class TestLogger<T> : ILogger<T>
{
    public List<(LogLevel Level, string Message)> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter) =>
        Entries.Add((logLevel, formatter(state, exception)));
}
