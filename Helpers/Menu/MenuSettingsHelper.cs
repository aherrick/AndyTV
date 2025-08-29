using System.Diagnostics;
using AndyTV.Services;
using LibVLCSharp.Shared;
using LibVLCSharp.WinForms;
using Microsoft.VisualBasic; // for Interaction.InputBox

namespace AndyTV.Helpers.Menu;

public class MenuSettingsHelper
{
    private readonly ContextMenuStrip _menu;
    private readonly string _appVersionName;
    private readonly VideoView _videoView;
    private readonly LibVLC _libVLC;

    private readonly ToolStripMenuItem _header;
    private readonly ToolStripMenuItem _muteItem;
    private readonly ToolStripMenuItem _checkUpdatesItem;

    private readonly Func<Form> _createFavoritesForm;
    private readonly Action _rebuildFavoritesMenu;
    private readonly UpdateService _updateService;

    public MenuSettingsHelper(
        ContextMenuStrip menu,
        string appVersionName,
        UpdateService updateService,
        Func<Form> createFavoritesForm,
        Action rebuildFavoritesMenu,
        VideoView videoView,
        LibVLC libVLC
    )
    {
        _menu = menu;
        _appVersionName = appVersionName;
        _updateService = updateService;
        _createFavoritesForm = createFavoritesForm;
        _rebuildFavoritesMenu = rebuildFavoritesMenu;
        _videoView = videoView;
        _libVLC = libVLC;

        _header = MenuHelper.AddHeader(_menu, appVersionName);
        int headerIndex = _menu.Items.IndexOf(_header);

        // Update
        _checkUpdatesItem = new ToolStripMenuItem("Update");
        _checkUpdatesItem.Click += async (_, __) =>
        {
            await _updateService.CheckForUpdates();
        };
        _menu.Items.Insert(headerIndex + 2, _checkUpdatesItem);

        // Swap (raw URL)
        var swapItem = new ToolStripMenuItem("Swap");
        swapItem.Click += (_, __) =>
        {
            // Show cursor for dialog
            _videoView.ShowDefault();

            string input = Interaction.InputBox("Enter media URL:", "Swap Stream", "").Trim();

            if (string.IsNullOrEmpty(input))
                return;

            videoView.ShowWaiting();

            _videoView.MediaPlayer.Stop();
            _videoView.MediaPlayer.Play(new Media(_libVLC, input, FromType.FromLocation));
        };
        _menu.Items.Insert(headerIndex + 3, swapItem);

        // Mute
        _muteItem = new ToolStripMenuItem("Mute");
        _muteItem.Click += (_, __) =>
        {
            _videoView.MediaPlayer.Mute = !_videoView.MediaPlayer.Mute;
        };
        _menu.Items.Insert(headerIndex + 4, _muteItem);

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
        _menu.Items.Insert(headerIndex + 5, logsItem);

        // Favorites
        var favoritesItem = new ToolStripMenuItem("Favorites");
        favoritesItem.Click += (_, __) =>
        {
            _videoView.ShowDefault();
            using var form = _createFavoritesForm();
            form.FormClosed += (_, __2) =>
            {
                _rebuildFavoritesMenu();

                var owner = _menu.SourceControl?.FindForm();
                bool isFullscreen = owner?.FormBorderStyle == FormBorderStyle.None;
                if (isFullscreen)
                    _videoView.HideCursor();
                else
                    _videoView.ShowDefault();
            };
            form.ShowDialog(_menu.SourceControl?.FindForm());
        };
        _menu.Items.Insert(headerIndex + 6, favoritesItem);

        // Exit
        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += (_, __) => Application.Exit();
        _menu.Items.Insert(headerIndex + 7, exitItem);

        _menu.Opening += (_, __) =>
        {
            _muteItem.Text = _videoView.MediaPlayer.Mute ? "Unmute" : "Mute";
            _videoView.ShowDefault();
        };
    }
}