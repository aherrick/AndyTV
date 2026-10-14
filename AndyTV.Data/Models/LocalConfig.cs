namespace AndyTV.Data.Models;

public class LocalConfig
{
    public string ServerUrl { get; set; }
    public string Quality { get; set; }
    public bool Enabled { get; set; }
    public bool DisableHardwareAcceleration { get; set; }
    // VLC vout module (e.g. "glwin32"); null = automatic.
    public string VideoOutput { get; set; }
    public int? NetworkBufferMilliseconds { get; set; }
    public SleepAction SleepAction { get; set; }
}

public enum SleepAction
{
    CloseApp,
    StopPlayback,
    Mute,
}
