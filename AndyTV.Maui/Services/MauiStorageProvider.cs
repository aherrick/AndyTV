using AndyTV.Data.Services;

namespace AndyTV.Maui.Services;

public class MauiStorageProvider : IStorageProvider
{
    private static string PathFor(string fileName) => Path.Combine(FileSystem.AppDataDirectory, fileName);

    public string ReadText(string fileName) => File.ReadAllText(PathFor(fileName));

    public void WriteText(string fileName, string content) => File.WriteAllText(PathFor(fileName), content);

    public bool FileExists(string fileName) => File.Exists(PathFor(fileName));
}