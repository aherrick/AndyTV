using AndyTV.Guide.Shared.Components;
using Microsoft.AspNetCore.Components.WebView.WindowsForms;
using Microsoft.Extensions.DependencyInjection;
using Syncfusion.Blazor;
using Syncfusion.Licensing;

namespace AndyTV.vNext;

sealed class GuideForm : Form
{
    public GuideForm()
    {
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96F, 96F);
        Text = "AndyTV Guide";
        Size = new Size(1400, 900);
        MinimumSize = new Size(1024, 720);
        StartPosition = FormStartPosition.CenterScreen;

        SyncfusionLicenseProvider.RegisterLicense(
            "Ngo9BigBOggjHTQxAR8/V1JAaF5cX2pCd1p/TH5YfUNzdUVEY1ZUTXxaS1ZhSXxVdkJjXn5YcnxRR2dVUUd9XEY="
        );

        var services = new ServiceCollection();
        services.AddWindowsFormsBlazorWebView();
        services.AddSyncfusionBlazor();
        services.AddScoped(_ => new HttpClient());

        var blazorWebView = new BlazorWebView
        {
            Dock = DockStyle.Fill,
            HostPage = "wwwroot/index.html",
            Services = services.BuildServiceProvider(),
        };
        blazorWebView.RootComponents.Add<GuideComponent>("#app");

        Controls.Add(blazorWebView);
    }
}
