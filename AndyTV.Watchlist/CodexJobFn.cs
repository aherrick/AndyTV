using AndyTV.Watchlist.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AndyTV.Watchlist;

/// <summary>
/// Every 30 minutes asks ChatGPT a small question, which also keeps the saved Sign in with ChatGPT
/// session alive (ChatGptClient refreshes it on every call).
/// </summary>
public sealed class CodexJobFn(ChatGptClient chatGpt, ILogger<CodexJobFn> logger)
{
    [Function(nameof(CodexJobFn))]
    public async Task Run(
        [TimerTrigger("0 */30 * * * *")] TimerInfo _,
        CancellationToken cancellationToken
    )
    {
        var text = await chatGpt.Ask(
            "Say hello from my Azure Function.",
            cancellationToken: cancellationToken
        );
        logger.LogInformation("Response: {text}", text);
    }
}
