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
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96F, 96F);
        Text = "AndyTV Guide";
        Width = 1400;
        Height = 900;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1024, 720);

        // Allow users to maximize if they want
        MaximizeBox = true;
        MinimizeBox = true;
        FormBorderStyle = FormBorderStyle.Sizable;

        // Register Syncfusion license
        SyncfusionLicenseProvider.RegisterLicense(
            "Ngo9BigBOggjHTQxAR8/V1JGaF5cXGpCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdmWH1ceXVQRGBcVUd3XUdWYEs="
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