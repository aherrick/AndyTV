using System.Text;

namespace AndyTV.Helpers;

public static class Logger
{
    private static readonly string _dir;

    static Logger()
    {
        _dir = PathHelper.GetPath("logs");
        Directory.CreateDirectory(_dir);
    }

    /// <summary>
    /// The root folder where all log files are written.
    /// </summary>
    public static string LogFolder => _dir;

    private static string CurrentFile => Path.Combine(_dir, $"{DateTime.UtcNow:yyyy-MM-dd}.log");

    public static void Info(string message) => Write("INFO", message);

    public static void Warn(string message) => Write("WARN", message);

    public static void Error(string message) => Write("ERROR", message);

    public static void Error(Exception ex, string message = null) =>
        Write("ERROR", (message is null ? "" : message + Environment.NewLine) + ex);

    public static void WireGlobalHandlers()
    {
        Application.ThreadException += (_, e) => Error(e.Exception, "UI thread exception");

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            Error(
                e.ExceptionObject as Exception
                    ?? new Exception(e.ExceptionObject?.ToString() ?? "Unknown"),
                "Unhandled AppDomain exception"
            );

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
                $"{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss} [{level}] "
                + $"[T{Environment.CurrentManagedThreadId}] {message}{Environment.NewLine}";

            File.AppendAllText(CurrentFile, line, Encoding.UTF8);
        }
        catch
        {
            // never throw from logging
        }
    }
}