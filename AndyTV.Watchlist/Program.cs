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

// Single shared HttpClient for the whole app. ESPN sits behind Akamai bot protection that 403s
// requests missing a realistic browser header set, so mimic Chrome (UA + Accept + Sec-Fetch).
builder.Services.AddSingleton(_ =>
{
    var handler = new SocketsHttpHandler { AutomaticDecompression = DecompressionMethods.All };
    var client = new HttpClient(handler);
    var headers = client.DefaultRequestHeaders;
    headers.UserAgent.ParseAdd(
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0.0.0 Safari/537.36"
    );
    headers.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
    headers.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
    headers.Add("Sec-Fetch-Dest", "document");
    headers.Add("Sec-Fetch-Mode", "navigate");
    headers.Add("Sec-Fetch-Site", "none");
    headers.Add("Sec-Fetch-User", "?1");
    headers.Add("Upgrade-Insecure-Requests", "1");
    return client;
});

builder.Services.AddSingleton(AppSettings.Load());
builder.Services.AddSingleton<SportsGuideService>();
builder.Services.AddSingleton<InstaCardRenderer>();
builder.Services.AddSingleton<CloudflareScreenshotService>();
builder.Services.AddSingleton<SportsFeedService, ApiSportsService>();
builder.Services.AddSingleton<SportsFeedService, EspnRacingService>();

if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")))
{
    builder.Services.AddOpenTelemetry()
        .UseFunctionsWorkerDefaults()
        .UseAzureMonitorExporter();
}

builder.Build().Run();
