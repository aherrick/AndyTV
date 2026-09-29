using AndyTV.Guide.Pages;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Syncfusion.Blazor;
using Syncfusion.Licensing;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

SyncfusionLicenseProvider.RegisterLicense(
    "Ngo9BigBOggjHTQxAR8/V1JBaF5cXmRCd1p/TH5YfUNzdUVEY1ZUTXxaS1ZhSXxVdkxhUH5acHxXR2JaUUF9XEc="
);

builder.Services.AddSyncfusionBlazor();

// Render GuidePage (or whatever your main component is) directly
builder.RootComponents.Add<Guide>("#app");

await builder.Build().RunAsync();