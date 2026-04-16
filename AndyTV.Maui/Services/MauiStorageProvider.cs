using AndyTV.Data.Services;

namespace AndyTV.Maui.Services;

public class MauiStorageProvider : IStorageProvider
{
    private static string GetFilePath(string fileName) =>
        Path.Combine(FileSystem.AppDataDirectory, fileName);

    public string ReadText(string fileName)
    {
        var path = GetFilePath(fileName);
        return File.ReadAllText(path);
    }

    public void WriteText(string fileName, string content)
    {
        var path = GetFilePath(fileName);
        File.WriteAllText(path, content);
    }

    public bool FileExists(string fileName)
    {
        var path = GetFilePath(fileName);
        return File.Exists(path);
    }
}