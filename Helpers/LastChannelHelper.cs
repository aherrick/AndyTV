namespace AndyTV.Helpers;

public static class LastChannelHelper
{
    private static readonly string FileName = PathHelper.GetPath("last_channel.txt");

    public static void Save(string name, string url)
    {
        File.WriteAllLines(FileName, [name, url]);
    }

    public static (string Name, string Url)? Load()
    {
        if (!File.Exists(FileName))
        {
            return null;
        }

        var lines = File.ReadAllLines(FileName);

        return (lines[0], lines[1]);
    }
}