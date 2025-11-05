using System.Diagnostics;
using AndyTV.VLC.Services;
using Blazored.LocalStorage;
using M3UManager; // for future extension/save operations
using M3UManager.Models;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Only configure custom URLs (HTTP + HTTPS) for Release/Production; use defaults in Development
if (!builder.Environment.IsDevelopment())
{
    builder.WebHost.UseUrls("https://localhost:5001");
}

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services.AddMudServices();

builder.Services.AddSingleton<VlcService>();
builder.Services.AddBlazoredLocalStorage();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

// Auto-launch browser only outside Development (Release publish scenario)
if (!app.Environment.IsDevelopment())
{
    app.Lifetime.ApplicationStarted.Register(() =>
    {
        try
        {
            var targetUrl = "https://localhost:5001/"; // prefer HTTPS in Release
            Process.Start(new ProcessStartInfo { FileName = targetUrl, UseShellExecute = true });
        }
        catch
        { /* Ignore failures to open browser */
        }
    });
}

app.Run();