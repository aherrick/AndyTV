namespace AndyTV.Helpers;

public static class PathHelper
{
    private static readonly string AppDataFolder;

    static PathHelper()
    {
        string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        AppDataFolder = Path.Combine(
            local,
            "com.ajh.AndyTV" /* set in velopak */
            ,
            "data"
        );
        Directory.CreateDirectory(AppDataFolder);
    }

    public static string GetPath(string fileName)
    {
        return Path.Combine(AppDataFolder, fileName);
    }
}