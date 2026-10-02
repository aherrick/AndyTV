using System.Text.Json;

namespace AndyTV.Data.Services;

public static class StorageProviderExtensions
{
    // Missing or corrupt files read as default so bad data never breaks startup.
    public static T ReadJson<T>(this IStorageProvider storage, string fileName)
    {
        try
        {
            return storage.FileExists(fileName)
                ? JsonSerializer.Deserialize<T>(storage.ReadText(fileName))
                : default;
        }
        catch
        {
            return default;
        }
    }

    public static void WriteJson<T>(this IStorageProvider storage, string fileName, T value) =>
        storage.WriteText(fileName, JsonSerializer.Serialize(value));
}
