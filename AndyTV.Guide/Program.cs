using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Syncfusion.Blazor;
using Syncfusion.Licensing;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

SyncfusionLicenseProvider.RegisterLicense(
    "Ngo9BigBOggjHTQxAR8/V1JFaF5cXGRCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdmWXZccnVVR2ldVE1/W0tWYEg="
);

builder.Services.AddSyncfusionBlazor();

// Render GuidePage (or whatever your main component is) directly
builder.RootComponents.Add<Guide>("#app");

await builder.Build().RunAsync();