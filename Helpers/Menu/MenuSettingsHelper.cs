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
    private readonly CursorHelper _cursorHelper;
    private readonly ToolStripMenuItem _header;
    private readonly ToolStripMenuItem _muteItem;
    private readonly ToolStripMenuItem _checkUpdatesItem;

    private readonly UpdateManager _updater;

    public MenuSettingsHelper(
        ContextMenuStrip menu,
        string appVersionName,
        MediaPlayer mediaPlayer,
        CursorHelper cursorHelper
    )
    {
        _menu = menu;
        _appVersionName = appVersionName;
        _mediaPlayer = mediaPlayer;
        _cursorHelper = cursorHelper;
        _header = MenuHelper.AddHeader(_menu, appVersionName);
        int headerIndex = _menu.Items.IndexOf(_header);

        _checkUpdatesItem = new ToolStripMenuItem("Check for Update")
        {
            Font = new Font(SystemFonts.MenuFont, FontStyle.Bold),
        };
        _checkUpdatesItem.Click += async (_, __) => await CheckForUpdates();
        _menu.Items.Insert(headerIndex + 2, _checkUpdatesItem);

        _muteItem = new ToolStripMenuItem("Mute");
        _muteItem.Click += (_, __) => _mediaPlayer.Mute = !_mediaPlayer.Mute;
        _menu.Items.Insert(headerIndex + 3, _muteItem);

        // Logs (between Mute and Exit)
        var logsItem = new ToolStripMenuItem("Logs");
        logsItem.Click += (_, __) =>
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

        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += (_, __) => Application.Exit();
        _menu.Items.Insert(headerIndex + 5, exitItem);

        _menu.Opening += (_, __) =>
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
            _cursorHelper.ShowDefault();

            var info = await _updater.CheckForUpdatesAsync();
            if (info == null)
            {
                MessageBox.Show(
                    "You’re already up to date.",
                    "Update",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
                return;
            }

            var result = MessageBox.Show(
                $"Update {info.TargetFullRelease.Version} is available.\n\nDownload and restart to update?",
                "Update Available",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.Yes)
            {
                await _updater.DownloadUpdatesAsync(info);
                _updater.ApplyUpdatesAndRestart(info.TargetFullRelease);
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Unexpected error while checking updates: {ex}");
            MessageBox.Show(
                "An error occurred while checking for updates. Please try again.",
                "Update Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
    }
}