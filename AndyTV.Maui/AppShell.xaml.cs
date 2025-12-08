using AndyTV.Maui.Views;

namespace AndyTV.Maui;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute("player", typeof(PlayerPage));

        Navigated += OnNavigated;
    }

    private void OnNavigated(object sender, ShellNavigatedEventArgs e)
    {
        // Centrally manage nav bar and tab bar visibility based on page type
        // Use Dispatcher to ensure UI updates after navigation completes
        Dispatcher.Dispatch(() =>
        {
            var page = CurrentPage;
            if (page == null)
                return;

            if (page is PlayerPage)
            {
                Shell.SetNavBarIsVisible(page, false);
                Shell.SetTabBarIsVisible(page, false);
            }
            else
            {
                Shell.SetNavBarIsVisible(page, true);
                Shell.SetTabBarIsVisible(page, true);
            }
        });
    }
}