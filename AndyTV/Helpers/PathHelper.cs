namespace AndyTV.Helpers;

public static class PathHelper
{
    private static readonly string AppDataFolder = InitAppDataFolder();

    private static string InitAppDataFolder()
    {
        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "com.ajh.AndyTV"
        );
        Directory.CreateDirectory(path);
        return path;
    }

    public static string GetPath(string fileName) => Path.Combine(AppDataFolder, fileName);
}