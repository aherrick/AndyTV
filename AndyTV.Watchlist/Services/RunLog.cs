using System.Text;
using Microsoft.Extensions.Logging;

namespace AndyTV.Watchlist.Services;

#pragma warning disable CA1822 // Interface implementations; CI scan still flags explicit members.

// Captures every log written in the current async flow so each run gets its own text log.
public sealed class RunLog : ILoggerProvider
{
    private static readonly AsyncLocal<StringBuilder> Current = new();

    // Call at the top of an async method so the buffer stays scoped to that run.
    public static StringBuilder Start() => Current.Value = new StringBuilder();

    ILogger ILoggerProvider.CreateLogger(string categoryName) => new Logger(categoryName);

    void IDisposable.Dispose() { }

    private sealed class Logger(string category) : ILogger
    {
        IDisposable ILogger.BeginScope<TState>(TState _) => null;

        bool ILogger.IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None && Current.Value is not null;

        void ILogger.Log<TState>(
            LogLevel logLevel,
            EventId _,
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
