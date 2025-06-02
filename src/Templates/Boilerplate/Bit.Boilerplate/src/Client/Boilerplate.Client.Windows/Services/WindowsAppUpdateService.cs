using Velopack;

namespace Boilerplate.Client.Windows.Services;

public partial class WindowsAppUpdateService : IAppUpdateService
{
    [AutoInject] private ClientWindowsSettings settings = default!;

    public async Task Update()
    {
        var windowsUpdateSettings = settings.WindowsUpdate;
        if (string.IsNullOrEmpty(windowsUpdateSettings?.FilesUrl))
            return;
        var updateManager = new UpdateManager(windowsUpdateSettings.FilesUrl);
        var updateInfo = await updateManager.CheckForUpdatesAsync();
        if (updateInfo is not null)
        {
            await updateManager.DownloadUpdatesAsync(updateInfo);
            if (windowsUpdateSettings.AutoReload)
            {
                updateManager.ApplyUpdatesAndRestart(updateInfo, Environment.GetCommandLineArgs());
            }
        }
    }
}
