namespace AndyTV.Maui.Services;

public interface IHlsPlayer
{
    Task<string> PlayHls(string url);
}
