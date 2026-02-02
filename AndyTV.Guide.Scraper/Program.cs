using System.Text.Json;
using AndyTV.Data.Models;
using AndyTV.Guide.Scraper;

Console.WriteLine("--- Starting Guide Refresh ---");

// 1. Fetch Streaming Shows (original logic moved to Helper)
Console.WriteLine("Fetching Streaming Guide...");
var streamingShows = await StreamingScraper.GetStreamingGuide();
Console.WriteLine($"Fetched {streamingShows.Count} streaming shows.");

// 2. Fetch Local Network Shows (new logic)
Console.WriteLine("Fetching Local Guide...");
var localShows = await LocalScraper.GetLocalGuide();
Console.WriteLine($"Fetched {localShows.Count} local shows.");

// 3. Merge
var allShows = new List<Show>();
allShows.AddRange(streamingShows);
allShows.AddRange(localShows);

Console.WriteLine($"Total Combined Shows: {allShows.Count}");

// Determine the repository root:
// - Prefer GITHUB_WORKSPACE env var (in GitHub Actions)
// - Otherwise, fallback to the current working directory
var repoRoot =
    Environment.GetEnvironmentVariable("GITHUB_WORKSPACE") ?? Directory.GetCurrentDirectory();

// Build the "out" directory path under the repo root
var outDir = Path.Combine(repoRoot, "out");

// Ensure the "out" directory exists (create if missing)
Directory.CreateDirectory(outDir);

// Serialize the shows list to JSON and write it to out/guide.json
await File.WriteAllTextAsync(Path.Combine(outDir, "guide.json"), JsonSerializer.Serialize(allShows));

// Print confirmation with the total show count
Console.WriteLine($"Done. Written {allShows.Count} shows to {Path.Combine(outDir, "guide.json")}");