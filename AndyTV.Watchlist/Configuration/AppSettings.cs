using Microsoft.Extensions.Configuration;

namespace AndyTV.Watchlist.Configuration;

public sealed record AppSettings(
    string GmailAddress,
    string GmailAppPassword,
    string GmailSender,
    string? CloudflareAccountId,
    string? CloudflareApiToken,
    string BlobConnectionString,
    string? InstagramUserId,
    string? InstagramAccessToken,
    string? XConsumerKey,
    string? XConsumerSecret,
    string? XAccessToken,
    string? XAccessTokenSecret,
    string? ApiSportsKey1,
    string? ApiSportsKey2
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
            Required(config, "GMAIL_ADDRESS"),
            Required(config, "GMAIL_APP_PASSWORD"),
            config["GMAIL_SENDER"] ?? "andy.ai.automation@gmail.com",
            config["CLOUDFLARE_ACCOUNT_ID"],
            config["CLOUDFLARE_API_TOKEN"],
            config["BLOB_CONNECTION_STRING"] ?? config["AzureWebJobsStorage"] ?? "",
            config["INSTAGRAM_USER_ID"],
            config["INSTAGRAM_ACCESS_TOKEN"],
            config["X_CONSUMER_KEY"],
            config["X_CONSUMER_SECRET"],
            config["X_ACCESS_TOKEN"],
            config["X_ACCESS_TOKEN_SECRET"],
            config["ApiSportsKey1"] ?? config["API_SPORTS_KEY1"],
            config["ApiSportsKey2"] ?? config["API_SPORTS_KEY2"]
        );
    }

    private static string Required(IConfiguration config, string name) =>
        string.IsNullOrWhiteSpace(config[name])
            ? throw new InvalidOperationException(
                $"Missing user secret '{name}'. Set it with: dotnet user-secrets set \"{name}\" \"<value>\"")
            : config[name]!;
}
