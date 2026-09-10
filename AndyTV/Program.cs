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
        // Wire crash logging before anything else so failures during update handling
        // or native init are still recorded.
        Logger.WireGlobalHandlers();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Logger.Info("[STARTUP] AndyTV starting");

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
            try
            {
                Core.Initialize();
                ApplicationConfiguration.Initialize();
                Application.SetColorMode(SystemColorMode.System);
                Application.Run(new PlayerForm());
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "[STARTUP] Fatal error during startup");
                throw;
            }
        }
    }
}
