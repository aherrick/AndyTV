using AndyTV.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Velopack;

namespace AndyTV;

internal static class Program
{
    // Keep a reference so the mutex isn't GC'd
    private static Mutex _singleInstanceMutex;

    [STAThread]
    private static void Main()
    {
        const string MutexName = @"Global\AndyTV_SingleInstance";
        _singleInstanceMutex = new Mutex(true, MutexName, out bool isNew);
        if (!isNew)
            return;

        VelopackApp.Build().Run();
        Logger.WireGlobalHandlers();

        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        using var serviceProvider = ServiceConfiguration.ConfigureServices();
        var mainForm = serviceProvider.GetRequiredService<Form1>();
        Application.Run(mainForm);
    }
}