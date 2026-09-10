using AndyTV.Watchlist.Configuration;
using AndyTV.Watchlist.Services;
using System.Net;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Shared HttpClient for the Cloudflare screenshot and Instagram publish calls.
builder.Services.AddSingleton(_ =>
{
    var handler = new SocketsHttpHandler { AutomaticDecompression = DecompressionMethods.All };
    return new HttpClient(handler);
});

builder.Services.AddSingleton(AppSettings.Load());
builder.Services.AddSingleton<GmailWatchlistService>();
builder.Services.AddSingleton<CloudflareScreenshotService>();
builder.Services.AddSingleton<BlobStore>();
builder.Services.AddSingleton<InstagramPublishService>();

if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")))
{
    builder.Services.AddOpenTelemetry()
        .UseFunctionsWorkerDefaults()
        .UseAzureMonitorExporter();
}

builder.Build().Run();
