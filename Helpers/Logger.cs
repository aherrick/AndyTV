using System.Text;

namespace AndyTV.Helpers;

public static class Logger
{
    private static readonly Lock _gate = new();
    private static readonly string _dir;

    static Logger()
    {
        _dir = PathHelper.GetPath("logs");
        Directory.CreateDirectory(_dir);
    }

    private static string CurrentFile => Path.Combine(_dir, $"{DateTime.UtcNow:yyyy-MM-dd}.log");

    public static void Info(string message) => Write("INFO", message);

    public static void Warn(string message) => Write("WARN", message);

    public static void Error(string message) => Write("ERROR", message);

    public static void Error(Exception ex, string message = null) =>
        Write("ERROR", (message is null ? "" : message + Environment.NewLine) + ex);

    public static void WireGlobalHandlers()
    {
        // UI thread exceptions
        Application.ThreadException += (_, e) => Error(e.Exception, "UI thread exception");

        // Non-UI / AppDomain exceptions
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            Error(
                e.ExceptionObject as Exception
                    ?? new Exception(e.ExceptionObject?.ToString() ?? "Unknown"),
                "Unhandled AppDomain exception"
            );

        // Unobserved Task exceptions
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Error(e.Exception, "Unobserved task exception");
            e.SetObserved();
        };
    }

    private static void Write(string level, string message)
    {
        try
        {
            var line =
                $"{DateTime.UtcNow:O} [{level}] "
                + $"[T{Environment.CurrentManagedThreadId}] {message}{Environment.NewLine}";

            lock (_gate)
            {
                Directory.CreateDirectory(_dir);
                File.AppendAllText(CurrentFile, line, Encoding.UTF8);
            }
        }
        catch
        { /* never throw from logging */
        }
    }
}