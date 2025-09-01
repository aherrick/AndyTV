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
#pragma warning disable WFO5001
        Application.SetColorMode(SystemColorMode.Dark);
#pragma warning restore WFO5001

        _mutex = new Mutex(initiallyOwned: true, name: MutexName, createdNew: out bool isNew);
        if (!isNew)
        {
            return;
        }

        VelopackApp.Build().Run();
        Logger.WireGlobalHandlers();
        ApplicationConfiguration.Initialize();

        var services = ServiceConfiguration.ConfigureServices();
        Application.Run(services.GetRequiredService<Form1>());
    }

    public static void Restart()
    {
        var exe = Environment.ProcessPath ?? Application.ExecutablePath;

        try
        {
            _mutex?.ReleaseMutex();
        }
        catch { }
        _mutex?.Dispose();
        _mutex = null;

        Process.Start(
            new ProcessStartInfo
            {
                FileName = exe,
                UseShellExecute = true,
                WorkingDirectory = AppContext.BaseDirectory,
            }
        );

        Application.Exit();
    }
}