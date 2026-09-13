using AndyTV.Watchlist.Models;
using Refit;

namespace AndyTV.Watchlist.Services;

public interface IEspnApi
{
    [Get("/apis/site/v2/sports/{sport}/{league}/scoreboard?limit=1000")]
    Task<EspnScoreboard> GetScoreboardAsync(
        string sport,
        string league,
        [AliasAs("dates")] string dates,
        [AliasAs("groups")] int? groups = null,
        CancellationToken cancellationToken = default);
}
