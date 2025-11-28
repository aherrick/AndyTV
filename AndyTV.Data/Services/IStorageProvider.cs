namespace AndyTV.Data.Services;

public interface IStorageProvider
{
    string ReadText(string fileName);

    void WriteText(string fileName, string content);

    bool FileExists(string fileName);
}