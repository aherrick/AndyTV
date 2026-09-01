using LibVLCSharp.Shared;
using Velopack;

namespace AndyTV.vNext;

static class Program
{
    [STAThread]
    static void Main()
    {
        // Own single-instance mutex, but shares the stable app's data folder (com.ajh.AndyTV).
        using var mutex = new Mutex(initiallyOwned: true, @"Global\AndyTV_vNext_SingleInstance", out var isNew);
        if (!isNew)
        {
            return;
        }

        // Must run first so Velopack can handle install/update hooks.
        VelopackApp.Build().Run();

        Logger.WireGlobalHandlers();
        Logger.Info("[STARTUP] AndyTV vNext starting");

        Core.Initialize();
        ApplicationConfiguration.Initialize();
        Application.SetColorMode(SystemColorMode.System);
        Application.Run(new PlayerForm());
    }
}
