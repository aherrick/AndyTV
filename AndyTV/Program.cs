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
        try
        {
            // Initialize Application Configuration first to prevent "SetCompatibleTextRenderingDefault" errors
            ApplicationConfiguration.Initialize();
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.SetColorMode(SystemColorMode.Dark);

            // Log startup args immediately using the standard logger
            Logger.Info($"[STARTUP] Raw Args: {(args != null ? string.Join(" ", args) : "null")}");

            // Use Main args directly, more reliable than Environment.GetCommandLineArgs()
            var isNewInstance = args.Any(a => a.Equals(NewInstanceArg, StringComparison.OrdinalIgnoreCase));
            StartOnRight = args.Any(a => a.Equals(RightArg, StringComparison.OrdinalIgnoreCase));

            if (!isNewInstance)
            {
                _mutex = new Mutex(initiallyOwned: true, name: MutexName, createdNew: out bool isNew);
                if (!isNew && !args.Any(a => a.Equals(RestartArg, StringComparison.OrdinalIgnoreCase)))
                {
                    Logger.Info("[STARTUP] Mutex detected existing instance and no --new-instance flag found. Exiting.");
                    return;
                }

                VelopackApp.Build().Run();
            }
            else
            {
                Logger.Info("[STARTUP] Skipping Velopack initialization for secondary instance.");
            }

            Logger.WireGlobalHandlers();

            Logger.Info($"[STARTUP] Args: {string.Join(", ", args)}");
            Logger.Info($"[STARTUP] isNewInstance={isNewInstance}, StartOnRight={StartOnRight}");

            var services = ServiceConfiguration.ConfigureServices();
            Application.Run(services.GetRequiredService<Form1>());
        }
        catch (Exception ex)
        {
            var crashLog = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "andytv_crash.txt");
            File.AppendAllText(crashLog, $"{DateTime.Now} CRASH: {ex}\n");
            Logger.Error(ex, "FATAL CRASH IN MAIN");
            MessageBox.Show($"AndyTV Failed to Start:\n{ex.Message}", "AndyTV Error");
        }
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