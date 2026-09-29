using Velopack;

using Microsoft.Extensions.Logging;

namespace Boilerplate.Client.Windows.Infrastructure.Services;

public partial class WindowsAppUpdateService : IAppUpdateService
{
    [AutoInject] private ClientWindowsSettings settings = default!;
    [AutoInject] private ILogger<WindowsAppUpdateService> logger = default!;

    /// <summary>
    /// Velopack runs one update at a time and fails any other on its lock file, so a <see cref="ForceUpdate"/> during
    /// the update Program.cs starts at launch waits for it instead. Static, since the service is scoped.
    /// </summary>
    private static readonly SemaphoreSlim updateLock = new(1, 1);

    public async Task ForceUpdate()
    {
        var windowsUpdateSettings = settings.WindowsUpdate;
        if (string.IsNullOrWhiteSpace(windowsUpdateSettings?.FilesUrl))
            return;
        windowsUpdateSettings.AutoReload = true; // Force update to reload the app after update
        await Update();
    }

    public async Task Update()
    {
        var windowsUpdateSettings = settings.WindowsUpdate;
        if (string.IsNullOrWhiteSpace(windowsUpdateSettings?.FilesUrl))
        {
            logger.LogWarning("No update feed is configured (WindowsUpdate.FilesUrl), so the update request did nothing.");
            return;
        }

        await updateLock.WaitAsync();

        try
        {
            var updateManager = new UpdateManager(windowsUpdateSettings.FilesUrl);
            var updateInfo = await updateManager.CheckForUpdatesAsync();
            if (updateInfo is null)
            {
                logger.LogInformation("No newer release is available at {FilesUrl}.", windowsUpdateSettings.FilesUrl);
                return;
            }

            await updateManager.DownloadUpdatesAsync(updateInfo);
            if (windowsUpdateSettings.AutoReload)
            {
                updateManager.ApplyUpdatesAndRestart(updateInfo);
            }
        }
        finally
        {
            updateLock.Release();
        }
    }
}
