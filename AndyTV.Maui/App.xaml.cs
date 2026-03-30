namespace AndyTV.Maui;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState activationState)
    {
        return new Window(
            new ContentPage
            {
                Content = new Grid
                {
                    Children =
                    {
                        new Label
                        {
                            Text = "AndyTV startup test",
                            HorizontalOptions = LayoutOptions.Center,
                            VerticalOptions = LayoutOptions.Center,
                        },
                    },
                },
            }
        );
    }
}