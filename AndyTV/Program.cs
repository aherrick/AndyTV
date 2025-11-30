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
    private const string NewInstanceArg = "--new-instance";
    private const string RightArg = "--right";

    public static bool StartOnRight { get; private set; }

    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.SetColorMode(SystemColorMode.Dark);

        var isNewInstance = args.Any(a =>
            a.Equals(NewInstanceArg, StringComparison.OrdinalIgnoreCase)
        );
        StartOnRight = args.Any(a => a.Equals(RightArg, StringComparison.OrdinalIgnoreCase));

        if (!isNewInstance)
        {
            _mutex = new Mutex(initiallyOwned: true, name: MutexName, createdNew: out bool isNew);
            if (!isNew && !args.Any(a => a.Equals(RestartArg, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            VelopackApp.Build().Run();
        }

        Logger.WireGlobalHandlers();
        Logger.Info($"[STARTUP] Args: {string.Join(", ", args)}");

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