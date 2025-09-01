using System.Diagnostics;
using AndyTV.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Velopack;

namespace AndyTV;

internal static class Program
{
    private static Mutex _mutex;
    private const string MutexName = @"Global\AndyTV_SingleInstance";

    [STAThread]
    private static void Main()
    {
        // Try to own the mutex
        _mutex = new Mutex(true, MutexName, out bool isNew);
        if (!isNew)
            return; // another instance is running

        Application.ApplicationExit += (_, __) => _mutex?.ReleaseMutex();

        VelopackApp.Build().Run();
        Logger.WireGlobalHandlers();
        ApplicationConfiguration.Initialize();

        using var services = ServiceConfiguration.ConfigureServices();
        Application.Run(services.GetRequiredService<Form1>());
    }

    public static void Restart()
    {
        try
        {
            Process.Start(Application.ExecutablePath);
        }
        catch { }
        finally
        {
            _mutex?.ReleaseMutex();
            Application.Exit();
        }
    }
}