using AndyTV.Data.Models;

namespace AndyTV.Data.Services;

public class LocalConfigService(IStorageProvider storage)
{
    private const string FileName = "local_config.json";

    public LocalConfig Load() => storage.ReadJson<LocalConfig>(FileName) ?? new LocalConfig();

    public void Save(LocalConfig config) => storage.WriteJson(FileName, config);
}
