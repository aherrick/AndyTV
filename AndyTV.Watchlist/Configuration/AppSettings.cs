using Microsoft.Extensions.Configuration;

namespace AndyTV.Watchlist.Configuration;

public sealed record AppSettings(
    string SportsApiKey,
    Uri AzureOpenAiEndpoint,
    string AzureOpenAiApiKey,
    string AzureOpenAiDeployment,
    string DeveloperPrompt,
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

    public static AppSettings Load()
    {
        var config = new ConfigurationBuilder()
            .AddUserSecrets<AppSettings>()
            .AddEnvironmentVariables()
            .Build();

        return new AppSettings(
            Required(config, "SPORTS_API_KEY"),
            new Uri(Required(config, "AZURE_OPENAI_ENDPOINT")),
            Required(config, "AZURE_OPENAI_API_KEY"),
            Required(config, "AZURE_OPENAI_DEPLOYMENT"),
            Required(config, "AI_DEVELOPER_PROMPT"),
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
