//using System.Diagnostics;
//using System.Runtime.InteropServices;
//using System.Runtime.InteropServices.Marshalling;

//namespace AndyTV.Helpers;

//public static partial class KeyboardHelper
//{
//    // ── Source-generated COM interface ───────────────────────────────────────────
//    [GeneratedComInterface]
//    [Guid("37C994E7-432B-4834-A2F7-DCE1F13B834B")]
//    internal partial interface ITipInvocation
//    {
//        void Toggle(nint hwnd);
//    }

//    private static readonly Guid CLSID_UIHostNoLaunch = new("4CE576FA-83DC-4F88-951C-9D0782B4E376");

//    private static readonly Guid IID_ITipInvocation = new("37C994E7-432B-4834-A2F7-DCE1F13B834B");

//    // ── P/Invoke (source-generated) ─────────────────────────────────────────────
//    [LibraryImport(
//        "user32.dll",
//        EntryPoint = "FindWindowW",
//        StringMarshalling = StringMarshalling.Utf16
//    )]
//    private static partial nint FindWindow(string lpClassName, nint lpWindowName);

//    [LibraryImport("user32.dll")]
//    [return: MarshalAs(UnmanagedType.Bool)]
//    private static partial bool IsWindowVisible(nint hWnd);

//    [LibraryImport("user32.dll")]
//    [return: MarshalAs(UnmanagedType.Bool)]
//    private static partial bool SetForegroundWindow(nint hWnd);

//    [LibraryImport("user32.dll")]
//    [return: MarshalAs(UnmanagedType.Bool)]
//    private static partial bool ShowWindow(nint hWnd, int nCmdShow);

//    [LibraryImport("ole32.dll")]
//    private static partial int CoCreateInstance(
//        ref Guid rclsid,
//        nint pUnkOuter,
//        uint dwClsContext,
//        ref Guid riid,
//        out nint ppv
//    );

//    private const int SW_SHOW = 5;

//    private const uint CLSCTX_INPROC_SERVER = 0x1;
//    private const uint CLSCTX_LOCAL_SERVER = 0x4;

//    private static readonly string[] TabTipPaths =
//    [
//        @"C:\Program Files\Common Files\microsoft shared\ink\TabTip.exe",
//        @"C:\Program Files (x86)\Common Files\microsoft shared\ink\TabTip.exe",
//    ];

//    public static void ShowOnScreenKeyboard()
//    {
//        Logger.Info("ShowOnScreenKeyboard: start");

//        EnsureTextServices(); // ctfmon.exe

//        if (!IsProcessRunning("TabTip"))
//        {
//            if (!TryStart("tabtip.exe", "", true, "Start TabTip via PATH"))
//            {
//                foreach (string path in TabTipPaths)
//                {
//                    if (File.Exists(path))
//                    {
//                        if (TryStart(path, "", true, "Start TabTip via '" + path + "'"))
//                        {
//                            break;
//                        }
//                    }
//                    else
//                    {
//                        Logger.Warn("TabTip not found at '" + path + "'");
//                    }
//                }
//            }
//        }
//        else
//        {
//            Logger.Info("TabTip already running.");
//        }

//        Thread.Sleep(200);

//        if (!BringTabTipToFront())
//        {
//            Logger.Info("TabTip window not visible; invoking COM Toggle.");
//            TryComToggle();
//            Thread.Sleep(200);
//            BringTabTipToFront();
//        }

//        if (!IsTabTipVisible())
//        {
//            Logger.Warn("TabTip still not visible; launching legacy OSK.");
//            if (!TryStart("osk.exe", "", true, "Start OSK"))
//            {
//                TryStart("cmd.exe", "/c start \"\" \"osk.exe\"", false, "Start OSK via cmd");
//            }
//        }
//    }

//    // ── helpers ─────────────────────────────────────────────────────────────────
//    private static bool BringTabTipToFront()
//    {
//        nint h = FindWindow("IPTip_Main_Window", 0);
//        if (h == 0)
//        {
//            Logger.Warn("IPTip_Main_Window not found.");
//            return false;
//        }

//        bool visible = IsWindowVisible(h);
//        Logger.Info(
//            "IPTip_Main_Window found: "
//                + (visible ? "visible" : "hidden")
//                + " (0x"
//                + h.ToString("X")
//                + ")"
//        );
//        _ = ShowWindow(h, SW_SHOW);
//        _ = SetForegroundWindow(h);
//        return visible;
//    }

//    private static bool IsTabTipVisible()
//    {
//        nint h = FindWindow("IPTip_Main_Window", 0);
//        return h != 0 && IsWindowVisible(h);
//    }

//    private static void TryComToggle()
//    {
//        nint unk = 0;
//        try
//        {
//            // Create COM instance of UIHostNoLaunch and request ITipInvocation
//            Guid clsid = CLSID_UIHostNoLaunch;
//            Guid iid = IID_ITipInvocation;
//            int hr = CoCreateInstance(
//                ref clsid,
//                0,
//                CLSCTX_INPROC_SERVER | CLSCTX_LOCAL_SERVER,
//                ref iid,
//                out unk
//            );

//            if (hr != 0 || unk == 0)
//            {
//                Logger.Warn(
//                    "CoCreateInstance(UIHostNoLaunch) failed. HRESULT=0x"
//                        + hr.ToString("X")
//                        + ", ptr=0x"
//                        + unk.ToString("X")
//                );
//                return;
//            }

//            object obj = Marshal.GetObjectForIUnknown(unk);
//            ITipInvocation tip = (ITipInvocation)obj;

//            tip.Toggle(0);
//            Logger.Info("ITipInvocation.Toggle called.");
//        }
//        catch (Exception ex)
//        {
//            Logger.Error(ex, "COM Toggle failed.");
//        }
//        finally
//        {
//            if (unk != 0)
//            {
//                try
//                {
//                    Marshal.Release(unk);
//                }
//                catch { }
//            }
//        }
//    }

//    private static void EnsureTextServices()
//    {
//        try
//        {
//            if (!IsProcessRunning("ctfmon"))
//            {
//                string ctf = Path.Combine(Environment.SystemDirectory, "ctfmon.exe");
//                TryStart(ctf, "", true, "Start ctfmon.exe");
//            }
//        }
//        catch (Exception ex)
//        {
//            Logger.Error(ex, "EnsureTextServices failed.");
//        }
//    }

//    private static bool TryStart(string fileName, string arguments, bool useShell, string label)
//    {
//        try
//        {
//            var psi = new ProcessStartInfo
//            {
//                FileName = fileName,
//                Arguments = arguments,
//                UseShellExecute = useShell,
//                CreateNoWindow = !useShell,
//                WindowStyle = useShell ? ProcessWindowStyle.Normal : ProcessWindowStyle.Hidden,
//            };

//            Process p = Process.Start(psi);
//            string pid = p != null ? p.Id.ToString() : "unknown";
//            Logger.Info(label + " ok (file='" + fileName + "', pid=" + pid + ").");
//            return true;
//        }
//        catch (Exception ex)
//        {
//            string argsForLog = string.IsNullOrEmpty(arguments) ? "<none>" : arguments;
//            Logger.Error(ex, label + " failed (file='" + fileName + "', args=" + argsForLog + ")");
//            return false;
//        }
//    }

//    private static bool IsProcessRunning(string name)
//    {
//        try
//        {
//            return Process.GetProcessesByName(name).Length > 0;
//        }
//        catch (Exception ex)
//        {
//            Logger.Error(ex, "Process enumeration failed for '" + name + "'.");
//            return false;
//        }
//    }
//}

using System;
using System.Diagnostics;
using System.IO;
using AndyTV.Helpers;

public static class KeyboardHelper
{
    public static void ShowOnScreenKeyboard()
    {
        Logger.Info("OSK: request to show");

        if (IsProcessRunning("osk"))
        {
            Logger.Info("OSK already running; not launching another instance.");
            return;
        }

        string windowsDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        string oskPath = Path.Combine(windowsDir, "System32", "osk.exe");

        // If 32-bit process on 64-bit OS, prefer Sysnative to get the real 64-bit osk.exe
        if (!Environment.Is64BitProcess && Environment.Is64BitOperatingSystem)
        {
            string sysnativePath = Path.Combine(windowsDir, "Sysnative", "osk.exe");
            if (File.Exists(sysnativePath))
            {
                oskPath = sysnativePath;
            }
        }

        // Fallback to PATH if the computed path isn't present
        if (!File.Exists(oskPath))
        {
            Logger.Warn("Computed OSK path not found; falling back to 'osk.exe' on PATH.");
            oskPath = "osk.exe";
        }

        TryStart(oskPath, "", true, "Launch OSK");
    }

    private static bool TryStart(string fileName, string arguments, bool useShell, string label)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = useShell,
                CreateNoWindow = !useShell,
                WindowStyle = useShell ? ProcessWindowStyle.Normal : ProcessWindowStyle.Hidden,
            };

            Process p = Process.Start(psi);
            string pid = p != null ? p.Id.ToString() : "unknown";
            Logger.Info(label + " ok (file='" + fileName + "', pid=" + pid + ").");
            return true;
        }
        catch (Exception ex)
        {
            string argsForLog = string.IsNullOrEmpty(arguments) ? "<none>" : arguments;
            Logger.Error(ex, label + " failed (file='" + fileName + "', args=" + argsForLog + ")");
            return false;
        }
    }

    private static bool IsProcessRunning(string name)
    {
        try
        {
            return Process.GetProcessesByName(name).Length > 0;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Process enumeration failed for '" + name + "'.");
            return false;
        }
    }
}