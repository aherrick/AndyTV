using System.Text.Json;
using AndyTV.Data.Models;

namespace AndyTV.Data.Services;

public class LocalConfigService(IStorageProvider storage) : ILocalConfigService
{
    private const string FileName = "local_config.json";

    public LocalConfig Load()
    {
        try
        {
            if (!storage.FileExists(FileName))
            {
                return new LocalConfig();
            }

            var json = storage.ReadText(FileName);
            return JsonSerializer.Deserialize<LocalConfig>(json) ?? new LocalConfig();
        }
        catch
        {
            return new LocalConfig();
        }
    }

    public void Save(LocalConfig config)
    {
        var json = JsonSerializer.Serialize(config);
        storage.WriteText(FileName, json);
    }
}
