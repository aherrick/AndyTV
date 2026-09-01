namespace AndyTV.Data.Services;

// File-backed storage shared by the Windows desktop apps (AndyTV, vNext).
public sealed class LocalStorageProvider : IStorageProvider
{
    public static string Folder { get; } = Init();

    private static string Init()
    {
        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "com.ajh.AndyTV");
        Directory.CreateDirectory(path);
        return path;
    }

    public bool FileExists(string fileName) => File.Exists(PathFor(fileName));

    public string ReadText(string fileName) => File.ReadAllText(PathFor(fileName));

    public void WriteText(string fileName, string content) =>
        File.WriteAllText(PathFor(fileName), content);

    public static string PathFor(string fileName) => Path.Combine(Folder, fileName);
}
