namespace AndyTV.Data.Models;

public class LocalConfig
{
    public string ServerUrl { get; set; }
    public string Quality { get; set; }
    public bool Enabled { get; set; }
    public bool DisableHardwareAcceleration { get; set; }
    public int? NetworkBufferMilliseconds { get; set; }
}
