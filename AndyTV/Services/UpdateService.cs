using Velopack;
using Velopack.Sources;

namespace AndyTV;

static class UpdateService
{
    public const string RepoUrl = "https://github.com/aherrick/AndyTV";

    // Manual, menu-driven check: reports up-to-date, or prompts to download & restart.
    public static async Task Check()
    {
        try
        {
            var updater = new UpdateManager(
                new GithubSource(RepoUrl, accessToken: null, prerelease: false));

            var info = await updater.CheckForUpdatesAsync();
            if (info is null)
            {
                MessageBox.Show(
                    "AndyTV is already up to date.",
                    "AndyTV",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Update {info.TargetFullRelease.Version} is available.\n\nDownload and restart to update?",
                "AndyTV",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                Logger.Info($"[UPDATE] Downloading {info.TargetFullRelease.Version}");
                await updater.DownloadUpdatesAsync(info);
                updater.ApplyUpdatesAndRestart(info.TargetFullRelease);
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Update check failed");
            MessageBox.Show(
                "Update check failed. See logs for details.",
                "AndyTV",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }
}
