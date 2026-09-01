using System.Text;
using AndyTV.Data.Services;

namespace AndyTV.vNext;

static class Logger
{
    private static readonly string LogFolder = InitLogFolder();

    private static string InitLogFolder()
    {
        var path = Path.Combine(LocalStorageProvider.Folder, "logs");
        Directory.CreateDirectory(path);
        return path;
    }

    private static string CurrentFile =>
        Path.Combine(LogFolder, $"{DateTime.UtcNow:yyyy-MM-dd}.log");

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
                e.ExceptionObject as Exception ?? new Exception("Unknown"),
                "Unhandled AppDomain exception");

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
