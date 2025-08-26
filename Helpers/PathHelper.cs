namespace AndyTV.Helpers;

public static class PathHelper
{
    private static readonly string AppDataFolder;

    static PathHelper()
    {
        string roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        AppDataFolder = Path.Combine(roaming, "com.ajh.AndyTV");
        Directory.CreateDirectory(AppDataFolder);
    }

    public static string GetPath(string fileName)
    {
        return Path.Combine(AppDataFolder, fileName);
    }
}