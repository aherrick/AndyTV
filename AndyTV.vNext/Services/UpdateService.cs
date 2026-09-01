using Velopack;
using Velopack.Sources;

namespace AndyTV.vNext;

static class UpdateService
{
    public const string RepoUrl = "https://github.com/aherrick/AndyTV";

    // Manual, menu-driven check: reports up-to-date, or prompts to download & restart.
    public static async Task Check()
    {
        try
        {
            // vNext ships as GitHub prereleases on its own channel, separate from stable AndyTV.
            var updater = new UpdateManager(
                new GithubSource(RepoUrl, accessToken: null, prerelease: true));

            var info = await updater.CheckForUpdatesAsync();
            if (info is null)
            {
                MessageBox.Show(
                    "AndyTV vNext is already up to date.",
                    "AndyTV vNext",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Update {info.TargetFullRelease.Version} is available.\n\nDownload and restart to update?",
                "AndyTV vNext",
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
                "AndyTV vNext",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }
}
