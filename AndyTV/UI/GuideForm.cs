using Microsoft.AspNetCore.Components.WebView.WindowsForms;
using Microsoft.Extensions.DependencyInjection;
using Syncfusion.Blazor;
using Syncfusion.Licensing;

namespace AndyTV.UI;

public partial class GuideForm : Form
{
    private readonly BlazorWebView _blazorWebView;

    public GuideForm()
    {
        Text = "AndyTV Guide";
        Width = 1400;
        Height = 900;
        StartPosition = FormStartPosition.CenterScreen;

        // Allow users to maximize if they want
        MaximizeBox = true;
        MinimizeBox = true;
        FormBorderStyle = FormBorderStyle.Sizable;

        // Register Syncfusion license
        SyncfusionLicenseProvider.RegisterLicense(
            "Ngo9BigBOggjHTQxAR8/V1JFaF5cXGRCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdmWXZccnVVR2ldVE1/W0tWYEg="
        );

        var services = new ServiceCollection();
        services.AddWindowsFormsBlazorWebView();
        services.AddSyncfusionBlazor();

        // Register HttpClient for the guide component
        services.AddScoped(_ => new HttpClient());

        _blazorWebView = new BlazorWebView
        {
            Dock = DockStyle.Fill,
            HostPage = "wwwroot/index.html",
            Services = services.BuildServiceProvider(),
        };

        _blazorWebView.RootComponents.Add<Guide.Shared.Components.GuideComponent>("#app");

        Controls.Add(_blazorWebView);
    }
}