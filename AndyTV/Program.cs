using System.Diagnostics;
using AndyTV.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Velopack;

namespace AndyTV;

internal static class Program
{
    private static Mutex _mutex;
    private const string MutexName = @"Global\AndyTV_SingleInstance";
    private const string RestartArg = "--restart";

    [STAThread]
    private static void Main()
    {
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.SetColorMode(SystemColorMode.Dark);

        string[] args = Environment.GetCommandLineArgs();
        bool isRestart = args.Contains(RestartArg);

        _mutex = new Mutex(initiallyOwned: true, name: MutexName, createdNew: out bool isNew);
        if (!isNew && !isRestart)
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
        _mutex.ReleaseMutex();
        _mutex.Dispose();
        _mutex = null;

        Process.Start(
            new ProcessStartInfo
            {
                FileName = Environment.ProcessPath ?? Application.ExecutablePath,
                Arguments = RestartArg,
                UseShellExecute = true,
                WorkingDirectory = AppContext.BaseDirectory,
            }
        );

        Application.Exit();
    }
}