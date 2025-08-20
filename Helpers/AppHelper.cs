using System.Reflection;

namespace AndyTV.Helpers;

public static class AppHelper
{
    public static string Version =>
        Assembly
            .GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ?? Application.ProductVersion;
}