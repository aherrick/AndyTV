using Microsoft.Extensions.Configuration;

namespace AndyTV.Watchlist.Configuration;

public sealed record AppSettings(
    string SportsApiKey,
    string? CopilotGitHubToken,
    string WatchlistPrompt,
    string? CloudflareAccountId,
    string? CloudflareApiToken,
    string BlobConnectionString,
    string? InstagramUserId,
    string? InstagramAccessToken,
    string? XConsumerKey,
    string? XConsumerSecret,
    string? XAccessToken,
    string? XAccessTokenSecret
)
{
    public bool CanPostToX =>
        !string.IsNullOrWhiteSpace(XConsumerKey)
        && !string.IsNullOrWhiteSpace(XConsumerSecret)
        && !string.IsNullOrWhiteSpace(XAccessToken)
        && !string.IsNullOrWhiteSpace(XAccessTokenSecret);

    public bool CanScreenshot =>
        !string.IsNullOrWhiteSpace(CloudflareAccountId)
        && !string.IsNullOrWhiteSpace(CloudflareApiToken);

    public bool CanPublishInstagram =>
        !string.IsNullOrWhiteSpace(InstagramUserId)
        && !string.IsNullOrWhiteSpace(InstagramAccessToken);

    public bool CanPublishSite => !string.IsNullOrWhiteSpace(BlobConnectionString);

    public static AppSettings Load()
    {
        var config = new ConfigurationBuilder()
            .AddUserSecrets<AppSettings>()
            .AddEnvironmentVariables()
            .Build();

        return new AppSettings(
            Required(config, "SPORTS_API_KEY"),
            // Fine-grained PAT with "Copilot Requests"; falls back to the logged-in Copilot CLI user locally.
            config["COPILOT_GITHUB_TOKEN"],
            Required(config, "WATCHLIST_PROMPT"),
            config["CLOUDFLARE_ACCOUNT_ID"],
            config["CLOUDFLARE_API_TOKEN"],
            config["BLOB_CONNECTION_STRING"] ?? config["AzureWebJobsStorage"] ?? "",
            config["INSTAGRAM_USER_ID"],
            config["INSTAGRAM_ACCESS_TOKEN"],
            config["X_CONSUMER_KEY"],
            config["X_CONSUMER_SECRET"],
            config["X_ACCESS_TOKEN"],
            config["X_ACCESS_TOKEN_SECRET"]
        );
    }

    private static string Required(IConfiguration config, string name) =>
        string.IsNullOrWhiteSpace(config[name])
            ? throw new InvalidOperationException(
                $"Missing user secret '{name}'. Set it with: dotnet user-secrets set \"{name}\" \"<value>\"")
            : config[name]!;
}
