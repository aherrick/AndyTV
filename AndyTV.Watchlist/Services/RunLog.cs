using System.Text;
using Microsoft.Extensions.Logging;

namespace AndyTV.Watchlist.Services;

// Captures every log written in the current async flow so each run gets its own text log.
public sealed class RunLog : ILoggerProvider
{
    private static readonly AsyncLocal<StringBuilder> Current = new();

    // Call at the top of an async method so the buffer stays scoped to that run.
    public static StringBuilder Start() => Current.Value = new StringBuilder();

    public ILogger CreateLogger(string categoryName) => new Logger(categoryName);

    public void Dispose() { }

    private sealed class Logger(string category) : ILogger
    {
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None && Current.Value is not null;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception exception,
            Func<TState, Exception, string> formatter
        )
        {
            if (Current.Value is not { } log)
            {
                return;
            }

            // Instagram and X post concurrently within one run.
            lock (log)
            {
                log.AppendLine($"{EasternTimeZone.Now:HH:mm:ss} [{logLevel}] {category}: {formatter(state, exception)}");
                if (exception is not null)
                {
                    log.AppendLine(exception.ToString());
                }
            }
        }
    }
}
