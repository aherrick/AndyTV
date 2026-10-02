using AndyTV.Guide.Pages;
using AndyTV.Guide.Shared;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Syncfusion.Blazor;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

GuideLicense.Register();
builder.Services.AddSyncfusionBlazor();
builder.RootComponents.Add<Guide>("#app");

await builder.Build().RunAsync();