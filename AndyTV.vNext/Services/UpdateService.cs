using Velopack;
using Velopack.Sources;

namespace AndyTV.vNext;

static class UpdateService
{
    private const string RepoUrl = "https://github.com/aherrick/AndyTV";

    // Silent check on startup; applies and restarts only when an update exists
    // and the app was installed via Velopack.
    public static async Task Check()
    {
        try
        {
            // vNext ships as GitHub prereleases on its own channel, separate from stable AndyTV.
            var updater = new UpdateManager(
                new GithubSource(RepoUrl, accessToken: null, prerelease: true));

            if (!updater.IsInstalled)
            {
                return;
            }

            var info = await updater.CheckForUpdatesAsync();
            if (info is null)
            {
                return;
            }

            Logger.Info($"[UPDATE] Downloading {info.TargetFullRelease.Version}");
            await updater.DownloadUpdatesAsync(info);
            Logger.Info($"[UPDATE] Applying {info.TargetFullRelease.Version} and restarting");
            updater.ApplyUpdatesAndRestart(info.TargetFullRelease);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Update check failed");
        }
    }
}
