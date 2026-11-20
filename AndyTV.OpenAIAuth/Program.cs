using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

// One-time Sign in with ChatGPT login. Writes the credentials that CodexJobFn keeps refreshing.
// Usage: dotnet run --project AndyTV.OpenAIAuth [output-file]

const string Callback = "http://127.0.0.1:1455/auth/callback";
const string Resource = "https://api.openai.com/v1";
const string Scope = "openid profile email offline_access resource.invoke chatgpt.tokens.use.direct";

var outFile =
    args.FirstOrDefault()
    ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "chatgpt-session.json"
    );

var hostId = $"urn:uuid:{Guid.NewGuid()}";
var state = Base64Url(RandomNumberGenerator.GetBytes(32));
var nonce = Base64Url(RandomNumberGenerator.GetBytes(32));
var verifier = Base64Url(RandomNumberGenerator.GetBytes(32));
var challenge = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));

var authorize =
    "https://auth.openai.com/api/accounts/authorize?"
    + string.Join(
        "&",
        new Dictionary<string, string>
        {
            ["client_id"] = "dynamic_agent_client",
            ["agent_name_hint"] = "AndyTV",
            ["ext_agent_host_id"] = hostId,
            ["response_type"] = "code",
            ["redirect_uri"] = Callback,
            ["scope"] = Scope,
            ["resource"] = Resource,
            ["state"] = state,
            ["nonce"] = nonce,
            ["code_challenge_method"] = "S256",
            ["code_challenge"] = challenge,
        }.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}")
    );

// The listener must be running before the browser opens.
using var listener = new HttpListener();
listener.Prefixes.Add("http://127.0.0.1:1455/");
listener.Start();

Console.WriteLine("Opening the browser to sign in with ChatGPT...");
Process.Start(new ProcessStartInfo(authorize) { UseShellExecute = true });

var context = await listener.GetContextAsync();
var query = context.Request.QueryString;
await using (var body = context.Response.OutputStream)
{
    await body.WriteAsync(Encoding.UTF8.GetBytes("Signed in. You can close this tab."));
}
listener.Stop();

if (query["error"] is { } error)
{
    throw new InvalidOperationException($"Authorization failed: {error}");
}

if (query["state"] != state)
{
    throw new InvalidOperationException("State mismatch.");
}

var clientId = query["client_id"] ?? throw new InvalidOperationException("No client_id returned.");
var code = query["code"] ?? throw new InvalidOperationException("No code returned.");

using var http = new HttpClient();
using var response = await http.PostAsync(
    "https://auth.openai.com/api/accounts/oauth/token",
    new FormUrlEncodedContent(
        new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = clientId,
            ["code"] = code,
            ["code_verifier"] = verifier,
            ["redirect_uri"] = Callback,
            ["resource"] = Resource,
        }
    )
);
var text = await response.Content.ReadAsStringAsync();
if (!response.IsSuccessStatusCode)
{
    throw new InvalidOperationException($"Token exchange failed: {text}");
}

var tokens = JsonNode.Parse(text)!;
var scopes = (string)tokens["scope"]!;
if (!scopes.Split(' ').Contains("chatgpt.tokens.use.direct"))
{
    throw new InvalidOperationException($"ChatGPT plan usage was not granted. Scopes: {scopes}");
}

var session = new JsonObject
{
    ["client_id"] = clientId,
    ["ext_agent_host_id"] = hostId,
    ["id_token"] = (string)tokens["id_token"]!,
    ["access_token"] = (string)tokens["access_token"]!,
    ["refresh_token"] = (string)tokens["refresh_token"]!,
};
await File.WriteAllTextAsync(outFile, session.ToJsonString(new() { WriteIndented = true }));
Console.WriteLine($"Saved {outFile}");

static string Base64Url(byte[] bytes) =>
    Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
