namespace AndyTV.Data.Services;

// File-backed storage shared by the Windows desktop apps (AndyTV, vNext).
public sealed class LocalStorageProvider : IStorageProvider
{
    // Data folder name under %APPDATA%; set once at startup for side-by-side apps (e.g. vNext).
    public static string AppName { get; set; } = "com.ajh.AndyTV";

    private static string _folder;
    public static string Folder => _folder ??= Init();

    private static string Init()
    {
        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            AppName);
        Directory.CreateDirectory(path);
        return path;
    }

    public bool FileExists(string fileName) => File.Exists(PathFor(fileName));

    public string ReadText(string fileName) => File.ReadAllText(PathFor(fileName));

    public void WriteText(string fileName, string content) =>
        File.WriteAllText(PathFor(fileName), content);

    public static string PathFor(string fileName) => Path.Combine(Folder, fileName);
}
