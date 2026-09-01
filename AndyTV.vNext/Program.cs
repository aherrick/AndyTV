using LibVLCSharp.Shared;
using Velopack;

namespace AndyTV.vNext;

static class Program
{
    [STAThread]
    static void Main()
    {
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
