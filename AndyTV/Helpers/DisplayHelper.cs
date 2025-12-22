using System.Runtime.InteropServices;

namespace AndyTV.Helpers;

/// <summary>
/// Helper class to manage display settings, particularly refresh rate.
/// </summary>
public static class DisplayHelper
{
    private const int ENUM_CURRENT_SETTINGS = -1;
    private const int CDS_UPDATEREGISTRY = 0x01;
    private const int CDS_TEST = 0x02;
    private const int DISP_CHANGE_SUCCESSFUL = 0;
    private const int DISP_CHANGE_RESTART = 1;
    private const int DM_DISPLAYFREQUENCY = 0x400000;
    private const int DM_PELSWIDTH = 0x80000;
    private const int DM_PELSHEIGHT = 0x100000;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    private struct DEVMODE
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmDeviceName;

        public short dmSpecVersion;
        public short dmDriverVersion;
        public short dmSize;
        public short dmDriverExtra;
        public int dmFields;
        public int dmPositionX;
        public int dmPositionY;
        public int dmDisplayOrientation;
        public int dmDisplayFixedOutput;
        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmFormName;

        public short dmLogPixels;
        public int dmBitsPerPel;
        public int dmPelsWidth;
        public int dmPelsHeight;
        public int dmDisplayFlags;
        public int dmDisplayFrequency;
        public int dmICMMethod;
        public int dmICMIntent;
        public int dmMediaType;
        public int dmDitherType;
        public int dmReserved1;
        public int dmReserved2;
        public int dmPanningWidth;
        public int dmPanningHeight;
    }

    [DllImport("user32.dll", CharSet = CharSet.Ansi)]
    private static extern int EnumDisplaySettings(
        string deviceName,
        int modeNum,
        ref DEVMODE devMode
    );

    [DllImport("user32.dll", CharSet = CharSet.Ansi)]
    private static extern int ChangeDisplaySettings(ref DEVMODE devMode, int flags);

    private static int _originalRefreshRate = 0;
    private static bool _refreshRateChanged = false;

    /// <summary>
    /// Gets the current display refresh rate in Hz.
    /// </summary>
    public static int GetCurrentRefreshRate()
    {
        var devMode = new DEVMODE();
        devMode.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));

        if (EnumDisplaySettings(null, ENUM_CURRENT_SETTINGS, ref devMode) != 0)
        {
            return devMode.dmDisplayFrequency;
        }

        return 0;
    }

    /// <summary>
    /// Forces the display to run at 60Hz refresh rate.
    /// Saves the original refresh rate for later restoration.
    /// </summary>
    /// <returns>True if successful, false otherwise.</returns>
    public static bool Force60Hz()
    {
        try
        {
            var devMode = new DEVMODE();
            devMode.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));

            if (EnumDisplaySettings(null, ENUM_CURRENT_SETTINGS, ref devMode) == 0)
            {
                Logger.Error(null, "Failed to get current display settings");
                return false;
            }

            _originalRefreshRate = devMode.dmDisplayFrequency;

            if (_originalRefreshRate == 60)
            {
                Logger.Info("[DISPLAY] Already at 60Hz, no change needed");
                return true;
            }

            Logger.Info(
                $"[DISPLAY] Current refresh rate: {_originalRefreshRate}Hz, changing to 60Hz"
            );

            devMode.dmDisplayFrequency = 60;
            devMode.dmFields = DM_DISPLAYFREQUENCY;

            // Test if the change is valid
            int testResult = ChangeDisplaySettings(ref devMode, CDS_TEST);
            if (testResult != DISP_CHANGE_SUCCESSFUL)
            {
                Logger.Error(
                    null,
                    $"[DISPLAY] 60Hz is not supported on this display (test result: {testResult})"
                );
                return false;
            }

            // Apply the change
            int result = ChangeDisplaySettings(ref devMode, CDS_UPDATEREGISTRY);

            if (result == DISP_CHANGE_SUCCESSFUL)
            {
                _refreshRateChanged = true;
                Logger.Info("[DISPLAY] Successfully changed to 60Hz");
                return true;
            }
            else if (result == DISP_CHANGE_RESTART)
            {
                Logger.Info("[DISPLAY] Change to 60Hz requires restart");
                return false;
            }
            else
            {
                Logger.Error(null, $"[DISPLAY] Failed to change to 60Hz (result: {result})");
                return false;
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "[DISPLAY] Exception while changing refresh rate");
            return false;
        }
    }

    /// <summary>
    /// Restores the original refresh rate that was saved before Force60Hz was called.
    /// </summary>
    /// <returns>True if successful or no restoration needed, false otherwise.</returns>
    public static bool RestoreOriginalRefreshRate()
    {
        if (!_refreshRateChanged || _originalRefreshRate == 0 || _originalRefreshRate == 60)
        {
            Logger.Info("[DISPLAY] No refresh rate restoration needed");
            return true;
        }

        try
        {
            var devMode = new DEVMODE();
            devMode.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));

            if (EnumDisplaySettings(null, ENUM_CURRENT_SETTINGS, ref devMode) == 0)
            {
                Logger.Error(null, "Failed to get current display settings for restoration");
                return false;
            }

            Logger.Info($"[DISPLAY] Restoring refresh rate to {_originalRefreshRate}Hz");

            devMode.dmDisplayFrequency = _originalRefreshRate;
            devMode.dmFields = DM_DISPLAYFREQUENCY;

            int result = ChangeDisplaySettings(ref devMode, CDS_UPDATEREGISTRY);

            if (result == DISP_CHANGE_SUCCESSFUL)
            {
                _refreshRateChanged = false;
                Logger.Info($"[DISPLAY] Successfully restored to {_originalRefreshRate}Hz");
                return true;
            }
            else
            {
                Logger.Error(null, $"[DISPLAY] Failed to restore refresh rate (result: {result})");
                return false;
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "[DISPLAY] Exception while restoring refresh rate");
            return false;
        }
    }
}