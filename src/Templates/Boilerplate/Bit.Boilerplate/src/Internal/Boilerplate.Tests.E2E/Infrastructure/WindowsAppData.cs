namespace Boilerplate.Tests.E2E.Infrastructure;

/// <summary>
/// What a Client.Windows app remembers: WindowsStorageService's isolated storage file (access token, culture, consent
/// answer) and the WebView2 profile. Every launch clears it, so the machine's own data - a signed-in session - is moved
/// aside before the first clear and moved back at assembly cleanup.
/// </summary>
public static class WindowsAppData
{
    private static readonly string[] windowsAppIds = [DeployedApps.TodoWindowsAppId, DeployedApps.AdminPanelWindowsAppId, DeployedApps.SalesWindowsAppId];

    private static readonly string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

    /// <summary>Next to the data itself, so backing up and restoring are renames rather than copies.</summary>
    private static readonly string backupRoot = Path.Combine(localAppData, "Boilerplate.Tests.E2E.WindowsAppDataBackup");

    private static readonly Lock backUpLock = new();

    private static bool backedUp;

    /// <summary>
    /// Once per run, with every Client.Windows app stopped. An item already in the backup is left alone: it is the
    /// real data, from a run that died before restoring, while what sits in its place now is a test's.
    /// </summary>
    public static void BackUpOnce()
    {
        lock (backUpLock)
        {
            if (backedUp)
                return;

            foreach (var path in windowsAppIds.SelectMany(PathsOf))
            {
                var backup = Path.Combine(backupRoot, Path.GetRelativePath(localAppData, path));

                if (Path.Exists(backup))
                    continue;

                Directory.CreateDirectory(Path.GetDirectoryName(backup)!);

                if (Directory.Exists(path))
                    Directory.Move(path, backup);
                else
                    File.Move(path, backup);
            }

            backedUp = true;
        }
    }

    /// <summary>
    /// Drops what the tests left and moves the backup back - whichever run took it. Kills the apps only when there is
    /// a backup, so a stage that never launched one leaves the machine's apps running.
    /// </summary>
    public static void Restore()
    {
        if (Directory.Exists(backupRoot) is false)
            return;

        IPlaywrightExtensions.StopWindowsApps();

        foreach (var windowsAppId in windowsAppIds)
            Clear(windowsAppId);

        MoveBack(backupRoot, localAppData);

        Directory.Delete(backupRoot, recursive: true);
    }

    /// <summary>The counterpart of the Android launch's <c>pm clear</c>. Best effort - what it cannot delete, the app recreates.</summary>
    public static void Clear(string windowsAppId)
    {
        foreach (var path in PathsOf(windowsAppId))
        {
            try
            {
                if (Directory.Exists(path))
                    Directory.Delete(path, recursive: true);
                else
                    File.Delete(path);
            }
            catch (Exception exp) when (exp is IOException or UnauthorizedAccessException)
            {
            }
        }
    }

    private static IEnumerable<string> PathsOf(string windowsAppId)
    {
        // Named after the app by WebView2 itself, since Client.Windows sets no user data folder of its own.
        var webView2 = Path.Combine(localAppData, $"{windowsAppId}.WebView2");

        if (Directory.Exists(webView2))
            yield return webView2;

        // The store's path is hashed out of the assembly's evidence, so it is searched for by the file name
        // WindowsStorageService writes - the assembly name, which is windowsAppId - rather than derived from a path.
        var isolatedStorage = Path.Combine(localAppData, "IsolatedStorage");

        if (Directory.Exists(isolatedStorage) is false)
            yield break;

        foreach (var store in Directory.EnumerateFiles(isolatedStorage, $"{windowsAppId}.storage.json", SearchOption.AllDirectories))
            yield return store;
    }

    /// <summary>A folder whose place is free goes back in one rename; one the clear could not fully delete is merged into.</summary>
    private static void MoveBack(string source, string target)
    {
        foreach (var file in Directory.EnumerateFiles(source))
            File.Move(file, Path.Combine(target, Path.GetFileName(file)), overwrite: true);

        foreach (var directory in Directory.EnumerateDirectories(source))
        {
            var targetDirectory = Path.Combine(target, Path.GetFileName(directory));

            if (Directory.Exists(targetDirectory))
                MoveBack(directory, targetDirectory);
            else
                Directory.Move(directory, targetDirectory);
        }
    }
}
