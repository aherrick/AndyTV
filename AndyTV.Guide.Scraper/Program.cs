using System.Text.Json;
using AndyTV.Data.Models;
using AndyTV.Guide.Scraper;

Console.WriteLine("--- Starting Guide Refresh ---");

Console.WriteLine("Fetching Streaming Guide...");
var streamingShows = await StreamingScraper.GetStreamingGuide();
Console.WriteLine($"Fetched {streamingShows.Count} streaming shows.");

Console.WriteLine("Fetching Local Guide...");
var localShows = await LocalScraper.GetLocalGuide();
Console.WriteLine($"Fetched {localShows.Count} local shows.");

List<Show> allShows = [.. localShows, .. streamingShows];
Console.WriteLine($"Total Combined Shows: {allShows.Count}");

// GITHUB_WORKSPACE in Actions, otherwise the working directory.
var repoRoot =
    Environment.GetEnvironmentVariable("GITHUB_WORKSPACE") ?? Directory.GetCurrentDirectory();
var outDir = Path.Combine(repoRoot, "out");
Directory.CreateDirectory(outDir);

var outFile = Path.Combine(outDir, "guide.json");
await File.WriteAllTextAsync(outFile, JsonSerializer.Serialize(allShows));
Console.WriteLine($"Done. Written {allShows.Count} shows to {outFile}");