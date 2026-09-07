using LibVLCSharp.Shared;
using Velopack;

namespace AndyTV;

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
            mutex = new Mutex(initiallyOwned: true, @"Global\AndyTV_SingleInstance", out var isNew);
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
            Logger.Info("[STARTUP] AndyTV starting");

            Core.Initialize();
            ApplicationConfiguration.Initialize();
            Application.SetColorMode(SystemColorMode.System);
            Application.Run(new PlayerForm());
        }
    }
}
