using AndyTV.Data.Models;

namespace AndyTV.Data.Services;

public interface ILocalConfigService
{
    LocalConfig Load();
    void Save(LocalConfig config);
}
