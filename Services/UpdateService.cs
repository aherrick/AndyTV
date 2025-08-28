using AndyTV.Helpers;
using LibVLCSharp.WinForms;
using Velopack;
using Velopack.Sources;

namespace AndyTV.Services;

public class UpdateService(VideoView videoView)
{
    public async Task CheckForUpdates()
    {
        var owner = videoView.FindForm();

        bool IsFullscreen() => owner != null && owner.FormBorderStyle == FormBorderStyle.None;

        try
        {
            videoView.ShowWaiting();

            var updater = new UpdateManager(
                new GithubSource(
                    "https://github.com/aherrick/AndyTV",
                    accessToken: null,
                    prerelease: false
                )
            );

            var info = await updater.CheckForUpdatesAsync();

            // Show default cursor while prompting the user
            videoView.ShowDefault();

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
                videoView.ShowWaiting();

                await updater.DownloadUpdatesAsync(info);

                updater.ApplyUpdatesAndRestart(info.TargetFullRelease);
            }
        }
        catch (Exception ex)
        {
            videoView.ShowDefault();

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
            {
                videoView.HideCursor();
            }
            else
            {
                videoView.ShowDefault();
            }
        }
    }
}