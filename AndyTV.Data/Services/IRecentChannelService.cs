using AndyTV.Data.Models;

namespace AndyTV.Data.Services;

public interface IRecentChannelService
{
    void AddOrPromote(Channel channel);

    Channel GetPrevious();

    Channel GetRelative(string currentUrl, int direction);

    List<Channel> GetRecentChannels();
}