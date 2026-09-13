using System.Text;

namespace Boilerplate.Client.Core.Components.Layout.Diagnostic;

public partial class AppDiagnosticModal
{
    [AutoInject] private Cookie cookie = default!;
    [AutoInject] private AuthManager authManager = default!;
    [AutoInject] private LocalStorage localStorage = default!;
    [AutoInject] private CacheStorage cacheStorage = default!;
    //#if (signalR == true)
    [AutoInject] private IndexedDb indexedDb = default!;
    //#endif
    [AutoInject] private SessionStorage sessionStorage = default!;
    //#if (api == "Integrated")
    [AutoInject] private IUserController userController = default!;
    //#if (signalR == true || notification == true)
    [AutoInject] private NotificationPreferenceService notificationPreferenceService = default!;
    //#endif
    //#endif
    [AutoInject] private IStorageService storageService = default!;
    //#if (api == "Integrated")
    [AutoInject] private IExternalNavigationService externalNavigationService = default!;
    //#endif
    [AutoInject] private IAppUpdateService appUpdateService = default!;
    [AutoInject] private ILogger<AppDiagnosticModal> logger = default!;
    //#if (offlineDb == true)
    [AutoInject] private SyncService syncService = default!;
    [AutoInject] private IDbContextFactory<AppOfflineDbContext> dbContextFactory = default!;
    //#endif

    private static async Task ThrowTestException()
    {
        await Task.Delay(250);

        showKnownException = !showKnownException;

        throw showKnownException
            ? new InvalidOperationException("Something critical happened.").WithData("TestData", 1)
            : new DomainLogicException("Something bad happened.").WithData("TestData", 2);
    }

    private async Task OpenDevTools()
    {
        await JSRuntime.InvokeVoidAsync("App.openDevTools");
    }

    //#if (api == "Integrated")
    /// <summary>
    /// Opens Hangfire's dashboard on the api, already signed in as this user. A plain browser navigation carries only
    /// a cookie, which <see cref="IUserController.UpdateSession"/> writes with the token's own expiry - so the token
    /// is refreshed first to buy a full lifetime rather than whatever is left of the current one.
    /// </summary>
    private async Task OpenHangfireDashboard()
    {
        await AuthManager.RefreshToken(requestedBy: nameof(OpenHangfireDashboard));

        await userController.UpdateSession(new()
        {
            AppVersion = TelemetryContext.AppVersion,
            DeviceInfo = TelemetryContext.Platform,
            CultureName = CultureInfoManager.InvariantGlobalization ? null : CultureInfo.CurrentUICulture.Name,
            //#if (signalR == true || notification == true)
            NotificationStatus = await notificationPreferenceService.GetSessionStatus(), // Left out, it would mute the session.
            //#endif
            PlatformType = AppPlatform.Type
        }, CurrentCancellationToken);

        await externalNavigationService.NavigateTo(new Uri(AbsoluteServerAddress, "hangfire").ToString());
    }
    //#endif

    private async Task CallGC()
    {
        SnackBarService.Show("Memory Before GC", GetMemoryUsage());

        await Task.Run(() =>
        {
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
            GC.WaitForPendingFinalizers();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
        });

        SnackBarService.Show("Memory After GC", GetMemoryUsage());
    }

    private string GetMemoryUsage()
    {
        long memory = Environment.WorkingSet;
        return $"{memory / (1024.0 * 1024.0):F2} MB";
    }

    private async Task ClearAppFiles()
    {
        //#if (offlineDb == true)
        try
        {
            // Try to push any pending changes before clearing the DB. This deliberately takes the overload that
            // accepts a DbContext rather than SyncService.Push(), which returns without doing anything when the app
            // believes it is offline - the state in which unpushed changes are most likely to exist.
            using var pushCts = CancellationTokenSource.CreateLinkedTokenSource(CurrentCancellationToken);
            pushCts.CancelAfter(TimeSpan.FromSeconds(5));

            await using var dbContext = await dbContextFactory.CreateDbContextAsync(pushCts.Token);
            await syncService.Sync(dbContext, pullRecentChanges: false, pushCts.Token);
        }
        catch (Exception exp)
        {
            logger.LogWarning(exp, "Failed to push Offline Database changes during ClearAppStorage.");
        }
        //#endif

        try
        {
            await authManager.SignOut(default);
        }
        catch (Exception exp)
        {
            logger.LogWarning(exp, "Failed to sign out during ClearAppStorage.");
        }

        try
        {
            await storageService.Clear(); // Blazor Hybrid stores key/value pairs outside webview's storage.
        }
        catch (Exception exp)
        {
            logger.LogWarning(exp, "Failed to clear the storage service during ClearAppStorage.");
        }

        await ClearWebStorages();

        //#if (offlineDb == true)
        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(CurrentCancellationToken);
            await dbContext.Database.EnsureDeletedAsync(CurrentCancellationToken); // Blazor Hybrid stores the db outside webview's storage.
            await dbContext.Database.ConfigureSqliteJournalMode();
            await dbContext.Database.MigrateAsync(CurrentCancellationToken);
        }
        catch (Exception exp)
        {
            logger.LogWarning(exp, "Failed to reset Offline Database during ClearAppStorage.");
        }
        //#endif

        if (AppPlatform.IsBlazorHybrid is false)
        {
            await JSRuntime.InvokeVoidAsync("BitBswup.forceRefresh"); // Clears cache storages and uninstalls service-worker.
        }
        else
        {
            NavigationManager.Refresh(forceReload: true);
        }
    }

    /// <summary>
    /// Clears the browser / web view storages of this origin.
    /// </summary>
    private async Task ClearWebStorages()
    {
        await Attempt(nameof(CacheStorage), async () =>
        {
            if (await cacheStorage.IsSupported() is false) return;

            foreach (var cacheName in await cacheStorage.Keys())
            {
                await cacheStorage.Delete(cacheName);
            }
        });

        await Attempt(nameof(LocalStorage), localStorage.Clear);

        await Attempt(nameof(SessionStorage), sessionStorage.Clear);

        //#if (signalR == true)
        // The conversation the AI chat panel keeps on this device (See AppAiChatPanel.RestoreHistory). The panel drops
        // its connection on delete, which would otherwise block it.
        await Attempt(nameof(IndexedDb), () => indexedDb.DeleteDatabase(AppAiChatPanel.HistoryDatabase).AsTask());
        //#endif

        await Attempt(nameof(Cookie), async () =>
        {
            foreach (var item in await cookie.GetAll())
            {
                await cookie.Remove(new ButilCookie { Name = item.Name, Path = "/" });
            }
        });

        async Task Attempt(string storageName, Func<Task> clear)
        {
            try
            {
                await clear();
            }
            catch (Exception exp)
            {
                logger.LogWarning(exp, "Failed to clear {Storage} during ClearAppStorage.", storageName);
            }
        }
    }

    private async Task UpdateApp()
    {
        await appUpdateService.ForceUpdate();
    }
}
