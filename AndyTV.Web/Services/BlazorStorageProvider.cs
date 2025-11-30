using AndyTV.Data.Services;
using Blazored.LocalStorage;

namespace AndyTV.Web.Services;

public class BlazorStorageProvider : IStorageProvider
{
    private readonly ISyncLocalStorageService _localStorage;

    public BlazorStorageProvider(ISyncLocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    public bool FileExists(string fileName)
    {
        return _localStorage.ContainKey(fileName);
    }

    public string ReadText(string fileName)
    {
        return _localStorage.GetItemAsString(fileName) ?? string.Empty;
    }

    public void WriteText(string fileName, string content)
    {
        _localStorage.SetItemAsString(fileName, content);
    }
}
