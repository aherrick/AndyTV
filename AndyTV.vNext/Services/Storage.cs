using AndyTV.Data.Services;

namespace AndyTV.vNext;

sealed class Storage : IStorageProvider
{
    private static readonly string Folder = Init();

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

    private static string PathFor(string fileName) => Path.Combine(Folder, fileName);
}
