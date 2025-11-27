using AndyTV.Data.Services;
using AndyTV.Helpers;

namespace AndyTV.Services;

public class WinFormsStorageProvider : IStorageProvider
{
    public bool FileExists(string fileName) => File.Exists(PathHelper.GetPath(fileName));

    public string ReadText(string fileName) => File.ReadAllText(PathHelper.GetPath(fileName));

    public void WriteText(string fileName, string content) => File.WriteAllText(PathHelper.GetPath(fileName), content);
}
