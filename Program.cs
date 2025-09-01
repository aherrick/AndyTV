using System.Diagnostics;
using AndyTV.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Velopack;

namespace AndyTV;

internal static class Program
{
    // Keep a reference so the mutex isn't GC'd
    private static Mutex _singleInstanceMutex;

    [STAThread]
    private static void Main()
    {
#pragma warning disable WFO5001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        Application.SetColorMode(SystemColorMode.Dark);
#pragma warning restore WFO5001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        // One name for the whole machine/session. Use "Global\" to block across user sessions.
        const string MutexName = @"Global\AndyTV_SingleInstance";

        _singleInstanceMutex = new Mutex(
            initiallyOwned: true,
            name: MutexName,
            createdNew: out bool isNew
        );
        if (!isNew)
        {
            // Another instance is running — just bail out.
            return;
        }

        VelopackApp.Build().Run();
        Logger.WireGlobalHandlers();

        ApplicationConfiguration.Initialize();

        // Setup dependency injection
        using var serviceProvider = ServiceConfiguration.ConfigureServices();
        var mainForm = serviceProvider.GetRequiredService<Form1>();

        Application.Run(mainForm);
    }
}