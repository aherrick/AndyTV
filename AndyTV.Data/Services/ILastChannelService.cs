using AndyTV.Data.Models;

namespace AndyTV.Data.Services;

public interface ILastChannelService
{
    void SaveLastChannel(Channel channel);

    Channel LoadLastChannel();
}