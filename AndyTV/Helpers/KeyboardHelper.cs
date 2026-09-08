using System.Diagnostics;

namespace AndyTV;

static class KeyboardHelper
{
    public static void ShowOnScreenKeyboard()
    {
        if (Process.GetProcessesByName("osk").Length > 0)
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo("osk.exe") { UseShellExecute = true });
            Logger.Info("OSK started");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "OSK failed");
        }
    }

    public static void HideOnScreenKeyboard()
    {
        foreach (var p in Process.GetProcessesByName("osk"))
        {
            try
            {
                p.Kill();
                Logger.Info("OSK closed");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "OSK close failed");
            }
            finally
            {
                p.Dispose();
            }
        }
    }
}
