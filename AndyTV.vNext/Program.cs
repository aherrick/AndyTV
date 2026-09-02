using LibVLCSharp.Shared;
using Velopack;

namespace AndyTV.vNext;

static class Program
{
    private const string NewInstanceArg = "--new-instance";
    private const string RightArg = "--right";

    public static bool StartOnRight { get; private set; }

    [STAThread]
    static void Main(string[] args)
    {
        var isNewInstance = args.Any(a => a.Equals(NewInstanceArg, StringComparison.OrdinalIgnoreCase));
        StartOnRight = args.Any(a => a.Equals(RightArg, StringComparison.OrdinalIgnoreCase));

        // A New Window launches with --new-instance to bypass the single-instance mutex.
        Mutex mutex = null;
        if (!isNewInstance)
        {
            // Own single-instance mutex, but shares the stable app's data folder (com.ajh.AndyTV).
            mutex = new Mutex(initiallyOwned: true, @"Global\AndyTV_vNext_SingleInstance", out var isNew);
            if (!isNew)
            {
                return;
            }

            // Must run first so Velopack can handle install/update hooks.
            VelopackApp.Build().Run();
        }

        using (mutex)
        {
            Logger.WireGlobalHandlers();
            Logger.Info("[STARTUP] AndyTV vNext starting");

            Core.Initialize();
            ApplicationConfiguration.Initialize();
            Application.SetColorMode(SystemColorMode.System);
            Application.Run(new PlayerForm());
        }
    }
}
