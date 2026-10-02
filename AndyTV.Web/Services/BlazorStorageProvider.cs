using AndyTV.Data.Services;
using Blazored.LocalStorage;

namespace AndyTV.Web.Services;

public class BlazorStorageProvider(ISyncLocalStorageService localStorage) : IStorageProvider
{
    public bool FileExists(string fileName) => localStorage.ContainKey(fileName);

    public string ReadText(string fileName) => localStorage.GetItemAsString(fileName) ?? string.Empty;

    public void WriteText(string fileName, string content) =>
        localStorage.SetItemAsString(fileName, content);
}