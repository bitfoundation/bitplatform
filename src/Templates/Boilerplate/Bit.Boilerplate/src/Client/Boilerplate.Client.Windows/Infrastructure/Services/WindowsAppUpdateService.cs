using Velopack;

using Microsoft.Extensions.Logging;

namespace Boilerplate.Client.Windows.Infrastructure.Services;

public partial class WindowsAppUpdateService : IAppUpdateService
{
    [AutoInject] private ClientWindowsSettings settings = default!;
    [AutoInject] private ILogger<WindowsAppUpdateService> logger = default!;

    public async Task ForceUpdate()
    {
        var windowsUpdateSettings = settings.WindowsUpdate;
        if (string.IsNullOrEmpty(windowsUpdateSettings?.FilesUrl))
            return;
        windowsUpdateSettings.AutoReload = true; // Force update to reload the app after update
        await Update();
    }

    public async Task Update()
    {
        var windowsUpdateSettings = settings.WindowsUpdate;
        if (string.IsNullOrEmpty(windowsUpdateSettings?.FilesUrl))
        {
            logger.LogWarning("No update feed is configured (WindowsUpdate.FilesUrl), so the update request did nothing.");
            return;
        }

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
}
