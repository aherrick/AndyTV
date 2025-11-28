using AndyTV.Data.Models;

namespace AndyTV.Data.Services;

public interface IRecentChannelService
{
    void AddOrPromote(Channel channel);

    Channel GetPrevious();

    List<Channel> GetRecentChannels();
}