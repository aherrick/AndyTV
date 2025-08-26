using AndyTV.Helpers;
using LibVLCSharp.WinForms;
using Velopack;
using Velopack.Sources;

namespace AndyTV.Services;

public class UpdateService
{
    private readonly UpdateManager _updater;

    public UpdateService()
    {
        _updater = new UpdateManager(
            new GithubSource(
                "https://github.com/aherrick/AndyTV",
                accessToken: null,
                prerelease: false
            )
        );
    }

    public async Task CheckForUpdates(VideoView cursorSurface)
    {
        var owner = cursorSurface.FindForm();

        bool IsFullscreen() => owner != null && owner.FormBorderStyle == FormBorderStyle.None;

        try
        {
            cursorSurface.ShowWaiting();

            var info = await _updater.CheckForUpdatesAsync();

            // Show default cursor while prompting the user
            cursorSurface.ShowDefault();

            if (info == null)
            {
                MessageBox.Show(
                    owner,
                    "You're already up to date.",
                    "Update",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
                return;
            }

            var result = MessageBox.Show(
                owner,
                $"Update {info.TargetFullRelease.Version} is available.\n\nDownload and restart to update?",
                "Update Available",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.Yes)
            {
                cursorSurface.ShowWaiting();

                await _updater.DownloadUpdatesAsync(info);

                _updater.ApplyUpdatesAndRestart(info.TargetFullRelease);
            }
        }
        catch (Exception ex)
        {
            cursorSurface.ShowDefault();

            Logger.Error($"Unexpected error while checking updates: {ex}");
            MessageBox.Show(
                owner,
                "An error occurred while checking for updates. Please try again.",
                "Update Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
        finally
        {
            if (IsFullscreen())
                cursorSurface.HideCursor();
            else
                cursorSurface.ShowDefault();
        }
    }
}