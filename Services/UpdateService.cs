using AndyTV.Helpers;
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

    public async Task CheckForUpdates()
    {
        try
        {
            CursorHelper.ShowWaiting();

            var info = await _updater.CheckForUpdatesAsync();

            if (info == null)
            {
                CursorHelper.ShowDefault();
                MessageBox.Show(
                    "You're already up to date.",
                    "Update",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
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
                _updater.ApplyUpdatesAndRestart(info.TargetFullRelease);
            }
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
        }
        finally
        {
            CursorHelper.Hide();
        }
    }
}