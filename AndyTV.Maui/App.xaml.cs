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
        var window = new Window(new AppShell());

        window.Resumed += (_, _) => AppResumed?.Invoke(this, EventArgs.Empty);

        return window;
    }
}