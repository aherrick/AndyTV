using System.Text.Json;
using AndyTV.Watchlist.Configuration;
using AndyTV.Watchlist.Models;
using RestSharp;
using RestSharp.Authenticators;

namespace AndyTV.Watchlist.Services;

// Only constructed when AppSettings.CanPostToX is true.
public sealed class XPostingService(AppSettings settings) : IDisposable
{
    private readonly RestClient _client = new(
        new RestClientOptions("https://api.x.com")
        {
            Authenticator = OAuth1Authenticator.ForProtectedResource(
                settings.XConsumerKey,
                settings.XConsumerSecret,
                settings.XAccessToken,
                settings.XAccessTokenSecret
            ),
        }
    );

    public async Task<string> PostThread(SportsPosts posts, CancellationToken cancellationToken = default)
    {
        var post1Id = await CreatePost(posts.Post1, null, cancellationToken);
        var post2Id = await CreatePost(posts.Post2, post1Id, cancellationToken);
        await CreatePost(posts.Post3, post2Id, cancellationToken);
        return post1Id;
    }

    private async Task<string> CreatePost(
        string text,
        string replyToPostId,
        CancellationToken cancellationToken
    )
    {
        object payload = replyToPostId is null
            ? new { text }
            : new { text, reply = new { in_reply_to_tweet_id = replyToPostId } };

        var request = new RestRequest("/2/tweets", Method.Post).AddJsonBody(payload);
        var response = await _client.ExecuteAsync(request, cancellationToken);

        if (!response.IsSuccessful)
        {
            throw new InvalidOperationException(
                $"X rejected the post ({(int)response.StatusCode}): {response.Content}"
            );
        }

        using var document = JsonDocument.Parse(response.Content);
        return document.RootElement.GetProperty("data").GetProperty("id").GetString()
            ?? throw new InvalidOperationException("X did not return a post ID.");
    }

    public void Dispose() => _client.Dispose();
}
