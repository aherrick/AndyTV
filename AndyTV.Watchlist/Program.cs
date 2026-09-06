using AndyTV.Watchlist.Configuration;
using AndyTV.Watchlist.Services;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Single shared HttpClient for the whole app; browser User-Agent is required or ESPN returns 403.
builder.Services.AddSingleton(_ =>
{
    var client = new HttpClient();
    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0.0.0 Safari/537.36"
    );
    return client;
});

builder.Services.AddSingleton(AppSettings.Load());
builder.Services.AddSingleton<SportsGuideService>();
builder.Services.AddSingleton<SportsFeedService, ApiSportsService>();
builder.Services.AddSingleton<SportsFeedService, EspnRacingService>();

if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")))
{
    builder.Services.AddOpenTelemetry()
        .UseFunctionsWorkerDefaults()
        .UseAzureMonitorExporter();
}

builder.Build().Run();
