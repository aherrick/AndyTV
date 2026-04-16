namespace AndyTV.Maui;

public partial class App : Application
{
    public static event EventHandler AppResumed;
    public static bool IsIosStartupIsolationEnabled
    {
        get
        {
#if IOS
            return true;
#else
            return false;
#endif
        }
    }

    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState activationState)
    {
        var window = new Window();

        window.Resumed += (_, _) => AppResumed?.Invoke(this, EventArgs.Empty);

#if IOS
        window.Page = CreateIosStartupTestPage(window);
#else
        window.Page = new AppShell();
#endif

        return window;
    }

#if IOS
    private static ContentPage CreateIosStartupTestPage(Window window)
    {
        var openAppButton = new Button
        {
            HorizontalOptions = LayoutOptions.Fill,
            Text = "Open Full App",
        };

        openAppButton.Clicked += (_, _) => window.Page = new AppShell();

        return new ContentPage
        {
            Title = "AndyTV",
            Content = new Grid
            {
                Padding = 24,
                Children =
                {
                    new VerticalStackLayout
                    {
                        HorizontalOptions = LayoutOptions.Center,
                        Spacing = 16,
                        VerticalOptions = LayoutOptions.Center,
                        Children =
                        {
                            new Label
                            {
                                FontAttributes = FontAttributes.Bold,
                                FontSize = 28,
                                HorizontalOptions = LayoutOptions.Center,
                                HorizontalTextAlignment = TextAlignment.Center,
                                Text = "AndyTV",
                            },
                            new Label
                            {
                                HorizontalOptions = LayoutOptions.Center,
                                HorizontalTextAlignment = TextAlignment.Center,
                                Text = "Minimal iOS startup page loaded.",
                            },
                            openAppButton,
                        },
                    },
                },
            },
        };
    }
#endif
}