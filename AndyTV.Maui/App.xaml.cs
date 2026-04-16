namespace AndyTV.Maui;

public partial class App : Application
{
    public static event EventHandler AppResumed;

    public App()
    {
#if IOS
#else
        InitializeComponent();
#endif
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
        return new ContentPage
        {
            Title = "AndyTV",
            BackgroundColor = Colors.Black,
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
                                TextColor = Colors.White,
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
                                TextColor = Colors.White,
                            },
                        },
                    },
                },
            },
        };
    }
#endif
}