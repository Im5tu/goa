using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Goa.Clients.Sqs.Tests.Infrastructure;

/// <summary>
/// Logger provider that records the level and message of every log entry.
/// </summary>
public sealed class CapturingLoggerProvider : ILoggerProvider
{
    public ConcurrentQueue<(LogLevel Level, string Message)> Entries { get; } = new();

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(Entries);

    public void Dispose()
    {
    }

    private sealed class CapturingLogger(ConcurrentQueue<(LogLevel Level, string Message)> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => entries.Enqueue((logLevel, formatter(state, exception)));
    }
}
