#:package NuGet.Versioning@6.9.1
#:package Alba.CsConsoleFormat@1.0.0

using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using NuGet.Versioning;
using Alba.CsConsoleFormat;

// Parse a single --ignore "Project=AndyTV.csproj&Package=Velopack" argument (URI-style query)
var ignore = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

for (int i = 0; i < args.Length - 1; i++)
{
    if (string.Equals(args[i], "--ignore", StringComparison.OrdinalIgnoreCase))
    {
        var spec = args[i + 1];
        var query = System.Web.HttpUtility.ParseQueryString(spec);
        var project = query["Project"];
        var package = query["Package"];

        if (!string.IsNullOrWhiteSpace(project) && !string.IsNullOrWhiteSpace(package))
        {
            ignore[project!] = package!;
            Console.WriteLine($"🔒 Ignoring: {project} → {package}");
        }
        break;
    }
}

var root = Directory.GetCurrentDirectory();
var csprojFiles = Directory.GetFiles(root, "*.csproj", SearchOption.AllDirectories);

using var http = new HttpClient();
var cache = new Dictionary<string, (string? LatestVersion, DateTimeOffset? Published)>(StringComparer.OrdinalIgnoreCase);

async Task<(string? LatestVersion, DateTimeOffset? Published)> GetLatestAsync(string packageId)
{
    if (cache.TryGetValue(packageId, out var cached))
        return cached;

    var lowerId = packageId.ToLowerInvariant();

    // Get latest version
    var indexUrl = $"https://api.nuget.org/v3-flatcontainer/{lowerId}/index.json";
    var indexJson = await http.GetStringAsync(indexUrl);
    using var indexDoc = JsonDocument.Parse(indexJson);
    var versions = indexDoc.RootElement.GetProperty("versions");
    var latestVersion = versions[versions.GetArrayLength() - 1].GetString();

    DateTimeOffset? published = null;

    if (!string.IsNullOrWhiteSpace(latestVersion))
    {
        // Try to get published date from registration endpoint
        try
        {
            var regUrl = $"https://api.nuget.org/v3/registration5-semver1/{lowerId}/{latestVersion.ToLowerInvariant()}.json";
            var regJson = await http.GetStringAsync(regUrl);
            using var regDoc = JsonDocument.Parse(regJson);
            if (regDoc.RootElement.TryGetProperty("published", out var pubProp) &&
                pubProp.ValueKind == JsonValueKind.String &&
                DateTimeOffset.TryParse(pubProp.GetString(), out var dto))
            {
                published = dto;
            }
        }
        catch
        {
            // Ignore errors fetching published date; we'll just leave it null
        }
    }

    var result = (latestVersion, published);
    cache[packageId] = result;
    return result;
}

var results = new List<PackageResult>();

foreach (var file in csprojFiles)
{
    var projectName = Path.GetFileName(file);
    var xml = XDocument.Load(file);

    var packageRefs = xml.Descendants("PackageReference")
        .Where(x => x.Attribute("Include") is not null && x.Attribute("Version") is not null);

    foreach (var pr in packageRefs)
    {
        var id = pr.Attribute("Include")!.Value;
        var current = pr.Attribute("Version")!.Value;

        bool isIgnored = ignore.TryGetValue(projectName, out var ignoredPackage) &&
                 string.Equals(ignoredPackage, id, StringComparison.OrdinalIgnoreCase);

        var (latest, published) = await GetLatestAsync(id);
        bool upToDate = latest is not null &&
                        NuGetVersion.Parse(current) == NuGetVersion.Parse(latest);

        if (isIgnored)
        {
            results.Add(new PackageResult(projectName, id, current, latest ?? current, published, true, true));
            continue;
        }

        results.Add(new PackageResult(projectName, id, current, latest, published, upToDate, false));
    }
}

bool anyFailures = false;

var ordered = results
    .OrderBy(r => r.Project, StringComparer.OrdinalIgnoreCase)
    .ThenBy(r => r.Package, StringComparer.OrdinalIgnoreCase)
    .ToList();

anyFailures = results.Any(r => !r.Ignored && !r.UpToDate);

Console.WriteLine();
Console.WriteLine("NuGet package status:");
Console.WriteLine();

var grid = new Grid
{
    Columns = { GridLength.Auto, GridLength.Auto, GridLength.Auto, GridLength.Auto, GridLength.Auto, GridLength.Auto },
    Children =
    {
        new Cell(nameof(PackageResult.Project)) { Stroke = LineThickness.Single },
        new Cell(nameof(PackageResult.Package)) { Stroke = LineThickness.Single },
        new Cell(nameof(PackageResult.Current)) { Stroke = LineThickness.Single },
        new Cell(nameof(PackageResult.Latest)) { Stroke = LineThickness.Single },
        new Cell(nameof(PackageResult.Published)) { Stroke = LineThickness.Single },
        new Cell("Status") { Stroke = LineThickness.Single },
        ordered.Select(r => new[]
        {
            new Cell(r.Project),
            new Cell(r.Package),
            new Cell(r.Current),
            new Cell(r.Latest ?? ""),
            new Cell(r.Published?.ToString("yyyy-MM-dd HH:mm:ss") ?? ""),
            new Cell(r.Ignored ? "🔒 Pinned" : (r.UpToDate ? "✅ Up-to-date" : "❌ Outdated"))
        })
    }
};

ConsoleRenderer.RenderDocument(new Document(grid));

Console.WriteLine();

// Fail if anything not ignored is outdated
if (anyFailures)
{
    Console.WriteLine("❌ Some packages are outdated (not ignored). Failing pipeline.");
    Environment.Exit(1);
}

Console.WriteLine("✅ All non-ignored packages are up to date.");

public record PackageResult(
    string Project,
    string Package,
    string Current,
    string? Latest,
    DateTimeOffset? Published,
    bool UpToDate,
    bool Ignored
);
