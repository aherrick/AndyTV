using System.Runtime.InteropServices;
using LibVLCSharp.Shared;
using Velopack;

namespace AndyTV;

static class Program
{
    private const string NewInstanceArg = "--new-instance";
    private const string RightArg = "--right";

    public static bool StartOnRight { get; private set; }

    // Shortcut path is unchanged across updates, so Explorer keeps the cached icon unless told to refresh.
    private const int SHCNE_ASSOCCHANGED = 0x08000000;
    private const uint SHCNF_IDLIST = 0x0000;

#pragma warning disable SYSLIB1054 // Use LibraryImport - not worth enabling unsafe blocks for one call
    [DllImport("shell32.dll")]
    private static extern void SHChangeNotify(int wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);
#pragma warning restore SYSLIB1054

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
            VelopackApp.Build()
                .OnAfterUpdateFastCallback(_ => SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero))
                .Run();
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
