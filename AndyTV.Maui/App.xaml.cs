using AndyTV.Maui.Views;

namespace AndyTV.Maui;

public partial class App : Application
{
    public static event EventHandler AppResumed;

    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState activationState)
    {
#if IOS
        return new Window(new MinimalPlayerPage());
#else
        var window = new Window(new AppShell());

        window.Resumed += (_, _) => AppResumed?.Invoke(this, EventArgs.Empty);

        return window;
#endif
    }
}