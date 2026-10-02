using AndyTV.Data.Models;

namespace AndyTV.Data.Services;

public class LastChannelService(IStorageProvider storage) : ILastChannelService
{
    private const string LastChannelFile = "last_channel.json";

    public void SaveLastChannel(Channel channel)
    {
        if (channel is not null)
        {
            storage.WriteJson(LastChannelFile, channel);
        }
    }

    public Channel LoadLastChannel() => storage.ReadJson<Channel>(LastChannelFile);
}