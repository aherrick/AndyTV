using AndyTV.Maui.Views;

namespace AndyTV.Maui;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        
        Routing.RegisterRoute("player", typeof(PlayerPage));
    }
}
