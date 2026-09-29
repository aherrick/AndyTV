using UIKit;

namespace AndyTV.Maui;

public static class Program
{
    // This is the main entry point of the application.
    static void Main(string[] args)
    {
        Console.WriteLine("[AndyTV] Main start");
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            Console.WriteLine($"[AndyTV] UNHANDLED: {e.ExceptionObject}");
        TaskScheduler.UnobservedTaskException += (_, e) =>
            Console.WriteLine($"[AndyTV] UNOBSERVED TASK: {e.Exception}");
        ObjCRuntime.Runtime.MarshalManagedException += (_, e) =>
            Console.WriteLine($"[AndyTV] MARSHAL MANAGED: {e.Exception}");

        // if you want to use a different Application Delegate class from "AppDelegate"
        // you can specify it here.
        UIApplication.Main(args, null, typeof(AppDelegate));
    }
}