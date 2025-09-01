using System.Diagnostics;
using AndyTV.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Velopack;

namespace AndyTV;

internal static class Program
{
    private static Mutex _mutex;
    private const string MutexName = @"Global\AndyTV_SingleInstance"; // drop "Global\" if you want per-session

    [STAThread]
    private static void Main()
    {
#pragma warning disable WFO5001
        Application.SetColorMode(SystemColorMode.Dark);
#pragma warning restore WFO5001

        // Single-instance guard
        _mutex = new Mutex(initiallyOwned: true, name: MutexName, createdNew: out bool isNew);
        if (!isNew)
        {
            // Another instance is already running
            return;
        }

        // Release mutex when the app exits normally
        Application.ApplicationExit += (_, __) =>
        {
            try
            {
                _mutex?.ReleaseMutex();
            }
            catch { }
            _mutex?.Dispose();
            _mutex = null;
        };

        // Velopack (updates/events) — only one instance should process this
        VelopackApp.Build().Run();

        Logger.WireGlobalHandlers();
        ApplicationConfiguration.Initialize();

        // DI + run main form
        var services = ServiceConfiguration.ConfigureServices();
        Application.Run(services.GetRequiredService<Form1>());

        // Fallback cleanup (in case ApplicationExit didn't fire)
        try
        {
            _mutex?.ReleaseMutex();
        }
        catch { }
        _mutex?.Dispose();
        _mutex = null;
    }

    /// <summary>
    /// Restart the application safely: release the mutex BEFORE launching the new process.
    /// </summary>
    public static void Restart()
    {
        try
        {
            // Release mutex first so the new instance can acquire it
            try
            {
                _mutex?.ReleaseMutex();
            }
            catch { }
            _mutex?.Dispose();
            _mutex = null;

            var psi = new ProcessStartInfo
            {
                FileName = Application.ExecutablePath,
                UseShellExecute = true,
                WorkingDirectory = AppContext.BaseDirectory,
            };
            Process.Start(psi);
        }
        catch
        {
            // ignore
        }
        finally
        {
            Application.Exit();
        }
    }
}