using System.Text.Json;
using AndyTV.Data.Models;

namespace AndyTV.Data.Services;

public class LastChannelService : ILastChannelService
{
    private const string LastChannelFile = "last_channel.json";

    private readonly IStorageProvider _storageProvider;

    public LastChannelService(IStorageProvider storageProvider)
    {
        _storageProvider = storageProvider;
    }

    public void SaveLastChannel(Channel channel)
    {
        if (channel == null)
            return;

        var json = JsonSerializer.Serialize(channel);
        _storageProvider.WriteText(LastChannelFile, json);
    }

    public Channel LoadLastChannel()
    {
        try
        {
            if (!_storageProvider.FileExists(LastChannelFile))
                return null;

            var json = _storageProvider.ReadText(LastChannelFile);
            return JsonSerializer.Deserialize<Channel>(json);
        }
        catch
        {
            return null;
        }
    }
}