using System.Text.Json;
using AndyTV.Watchlist.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var services = new ServiceCollection();
services.AddLogging(logging => logging.AddSimpleConsole().SetMinimumLevel(LogLevel.Warning));
services.AddWatchlistScores();
await using var provider = services.BuildServiceProvider();
var service = provider.GetRequiredService<WatchlistScoreService>();
var json = new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true };

while (true)
{
    Console.WriteLine(JsonSerializer.Serialize(await service.GetAsync(), json));
    await Task.Delay(TimeSpan.FromSeconds(30));
}
