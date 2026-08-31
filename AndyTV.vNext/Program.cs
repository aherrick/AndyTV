using LibVLCSharp.Shared;

namespace AndyTV.vNext;

static class Program
{
    [STAThread]
    static void Main()
    {
        Core.Initialize();
        ApplicationConfiguration.Initialize();
        Application.SetColorMode(SystemColorMode.System);
        Application.Run(new PlayerForm());
    }
}
