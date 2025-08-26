using System.Diagnostics;
using AndyTV.Services;
using LibVLCSharp.Shared;

namespace AndyTV.Helpers.Menu;

public class MenuSettingsHelper
{
    private readonly ContextMenuStrip _menu;
    private readonly string _appVersionName;
    private readonly MediaPlayer _mediaPlayer;
    private readonly ToolStripMenuItem _header;
    private readonly ToolStripMenuItem _muteItem;
    private readonly ToolStripMenuItem _checkUpdatesItem;

    private readonly Func<Form> _createFavoritesForm;
    private readonly Action _rebuildFavoritesMenu;

    private readonly UpdateService _updateService;

    public MenuSettingsHelper(
        ContextMenuStrip menu,
        string appVersionName,
        MediaPlayer mediaPlayer,
        UpdateService updateService,
        Func<Form> createFavoritesForm,
        Action rebuildFavoritesMenu
    )
    {
        _menu = menu;
        _appVersionName = appVersionName;
        _mediaPlayer = mediaPlayer;
        _updateService = updateService;
        _createFavoritesForm = createFavoritesForm;
        _rebuildFavoritesMenu = rebuildFavoritesMenu;

        _header = MenuHelper.AddHeader(_menu, appVersionName);
        int headerIndex = _menu.Items.IndexOf(_header);

        // Update
        _checkUpdatesItem = new ToolStripMenuItem("Update");
        _menu.Items.Insert(headerIndex + 2, _checkUpdatesItem);

        // Mute
        _muteItem = new ToolStripMenuItem("Mute");
        _muteItem.Click += (_, __) =>
        {
            _mediaPlayer.Mute = !_mediaPlayer.Mute;
        };
        _menu.Items.Insert(headerIndex + 3, _muteItem);

        // Logs
        var logsItem = new ToolStripMenuItem("Logs");
        logsItem.Click += (_, __) =>
        {
            var path = PathHelper.GetPath("logs");
            Directory.CreateDirectory(path);
            Process.Start(
                new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = path,
                    UseShellExecute = true,
                }
            );
        };
        _menu.Items.Insert(headerIndex + 4, logsItem);

        // Favorites
        var favoritesItem = new ToolStripMenuItem("Favorites");
        favoritesItem.Click += (_, __) =>
        {
            CursorHelper.ShowDefault();

            using var form = _createFavoritesForm();
            form.FormClosed += (_, __2) =>
            {
                CursorHelper.Hide();
                _rebuildFavoritesMenu();
            };

            form.ShowDialog(_menu.SourceControl.FindForm());
        };
        _menu.Items.Insert(headerIndex + 5, favoritesItem);

        // Exit
        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += (_, __) =>
        {
            Application.Exit();
        };
        _menu.Items.Insert(headerIndex + 6, exitItem);

        _menu.Opening += (_, __) =>
        {
            _muteItem.Text = _mediaPlayer.Mute ? "Unmute" : "Mute";
        };
    }
}