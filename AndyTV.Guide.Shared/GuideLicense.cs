using Syncfusion.Licensing;

namespace AndyTV.Guide.Shared;

// Every host of GuideComponent must register before rendering.
public static class GuideLicense
{
    public static void Register() =>
        SyncfusionLicenseProvider.RegisterLicense(
            "Ngo9BigBOggjHTQxAR8/V1JBaF5cXmRCd1p/TH5YfUNzdUVEY1ZUTXxaS1ZhSXxVdkxhUH5acHxXR2JaUUF9XEc="
        );
}
