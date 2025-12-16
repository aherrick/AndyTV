using AndyTV.Data.Services;
using AndyTV.Web;
using AndyTV.Web.Services;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped<IStorageProvider, BlazorStorageProvider>();
builder.Services.AddScoped<IPlaylistService, PlaylistService>();
builder.Services.AddScoped<ChannelManagerService>();
builder.Services.AddScoped<IRecentChannelService, RecentChannelService>();

await builder.Build().RunAsync();