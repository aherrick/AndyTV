using AndyTV.Watchlist.Configuration;
using AndyTV.Watchlist.Services;
using System.Net;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;

var builder = FunctionsApplication.CreateBuilder(args);

// Shared HttpClient for the sports feeds, Cloudflare screenshot and Instagram publish calls.
builder.Services.AddSingleton(_ =>
{
    var handler = new SocketsHttpHandler { AutomaticDecompression = DecompressionMethods.All };
    return new HttpClient(handler);
});

builder.Logging.AddProvider(new RunLog());

builder.Services.AddSingleton(_ => AppSettings.Load());
builder.Services.AddSingleton<ApiSportsService>();
builder.Services.AddSingleton<EspnRacingService>();
builder.Services.AddSingleton<WatchlistResearchService>();
builder.Services.AddSingleton<CloudflareScreenshotService>();
builder.Services.AddSingleton<BlobStore>();
builder.Services.AddSingleton<InstagramPublishService>();
builder.Services.AddSingleton<WatchlistPublishingService>();

if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")))
{
    builder.Services.AddOpenTelemetry()
        .UseFunctionsWorkerDefaults()
        .UseAzureMonitorExporter();
}

builder.Build().Run();
