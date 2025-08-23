using System.Diagnostics;
using LibVLCSharp.Shared;
using Velopack;
using Velopack.Sources;

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
    private readonly Action _saveCurrentChannel;
    private readonly UpdateManager _updater;

    public MenuSettingsHelper(
        ContextMenuStrip menu,
        string appVersionName,
        MediaPlayer mediaPlayer,
        Func<Form> createFavoritesForm,
        Action rebuildFavoritesMenu,
        Action saveCurrentChannel // Add this parameter
    )
    {
        _menu = menu;
        _appVersionName = appVersionName;
        _mediaPlayer = mediaPlayer;
        _createFavoritesForm = createFavoritesForm;
        _rebuildFavoritesMenu = rebuildFavoritesMenu;
        _saveCurrentChannel = saveCurrentChannel;
        _header = MenuHelper.AddHeader(_menu, appVersionName);
        int headerIndex = _menu.Items.IndexOf(_header);

        // Check for Update (bold)
        _checkUpdatesItem = new ToolStripMenuItem("Check for Update")
        {
            Font = new Font(SystemFonts.MenuFont, FontStyle.Bold),
        };
        _checkUpdatesItem.Click += async (_, __) => await CheckForUpdates();
        _menu.Items.Insert(headerIndex + 2, _checkUpdatesItem);

        // Mute
        _muteItem = new ToolStripMenuItem("Mute");
        _muteItem.Click += (_, _) => _mediaPlayer.Mute = !_mediaPlayer.Mute;
        _menu.Items.Insert(headerIndex + 3, _muteItem);

        // Logs
        var logsItem = new ToolStripMenuItem("Logs");
        logsItem.Click += (_, _) =>
        {
            try
            {
                var logsPath = PathHelper.GetPath("logs");
                Directory.CreateDirectory(logsPath);
                Process.Start(
                    new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = logsPath,
                        UseShellExecute = true,
                    }
                );
            }
            catch { }
        };
        _menu.Items.Insert(headerIndex + 4, logsItem);

        var favoritesItem = new ToolStripMenuItem("Favorites");
        favoritesItem.Click += (_, _) =>
        {
            // Always restore cursor before showing dialog
            CursorHelper.ShowDefault();

            using var form = _createFavoritesForm();

            form.FormClosed += (_, _) =>
            {
                CursorHelper.Hide();
                _rebuildFavoritesMenu();
            };

            form.ShowDialog(_menu.SourceControl.FindForm());
        };
        _menu.Items.Insert(headerIndex + 5, favoritesItem);

        // Exit
        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += (_, _) => Application.Exit();
        _menu.Items.Insert(headerIndex + 6, exitItem);

        _menu.Opening += (_, _) =>
        {
            _muteItem.Text = _mediaPlayer.Mute ? "Unmute" : "Mute";
        };

        _updater = new UpdateManager(
            new GithubSource(
                "https://github.com/aherrick/AndyTV",
                accessToken: null,
                prerelease: false
            )
        );
    }

    private async Task CheckForUpdates()
    {
        try
        {
            CursorHelper.ShowWaiting();

            var info = await _updater.CheckForUpdatesAsync();

            if (info == null)
            {
                CursorHelper.ShowDefault();
                MessageBox.Show(
                    "You’re already up to date.",
                    "Update",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
                CursorHelper.Hide();
                return;
            }

            CursorHelper.ShowDefault();
            var result = MessageBox.Show(
                $"Update {info.TargetFullRelease.Version} is available.\n\nDownload and restart to update?",
                "Update Available",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.Yes)
            {
                CursorHelper.ShowWaiting();
                await _updater.DownloadUpdatesAsync(info);

                // Save current channel before restart
                _saveCurrentChannel?.Invoke(); // Call it here!

                _updater.ApplyUpdatesAndRestart(info.TargetFullRelease);
            }

            CursorHelper.Hide();
        }
        catch (Exception ex)
        {
            CursorHelper.ShowDefault();
            Logger.Error($"Unexpected error while checking updates: {ex}");
            MessageBox.Show(
                "An error occurred while checking for updates. Please try again.",
                "Update Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
            CursorHelper.Hide();
        }
    }
}