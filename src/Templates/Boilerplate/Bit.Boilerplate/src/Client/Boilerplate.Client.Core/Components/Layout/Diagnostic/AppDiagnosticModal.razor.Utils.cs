using System.Text;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Boilerplate.Shared.Controllers.Identity;
//#if (signalR == true)
using Microsoft.AspNetCore.SignalR.Client;
//#endif
//#if (offlineDb == true)
using Boilerplate.Client.Core.Data;
using Microsoft.EntityFrameworkCore;
//#endif

namespace Boilerplate.Client.Core.Components.Layout.Diagnostic;

public partial class AppDiagnosticModal
{
    [AutoInject] private Cookie cookie = default!;
    [AutoInject] private AuthManager authManager = default!;
    [AutoInject] private IStorageService storageService = default!;
    [AutoInject] private IUserController userController = default!;
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

    private async Task CallDiagnosticsApi()
    {
        string? signalRConnectionId = null;
        string? pushNotificationSubscriptionDeviceId = null;

        //#if (signalR == true)
        try
        {
            signalRConnectionId = hubConnection.State == HubConnectionState.Connected ? hubConnection.ConnectionId : null;
        }
        catch (Exception exp)
        {
            logger.LogWarning(exp, "Failed to get SignalR ConnectionId for diagnostics.");
        }
        //#endif

        //#if (notification == true)
        try
        {
            pushNotificationSubscriptionDeviceId = (await pushNotificationService.GetSubscription(CurrentCancellationToken))!.DeviceId;
        }
        catch (Exception exp)
        {
            logger.LogWarning(exp, "Failed to get Push Notification Subscription DeviceId for diagnostics.");
        }
        //#endif

        var serverResult = await diagnosticsController.PerformDiagnostics(signalRConnectionId, pushNotificationSubscriptionDeviceId, CurrentCancellationToken);

        StringBuilder resultBuilder = new(serverResult);
        try
        {
            resultBuilder.AppendLine();

            resultBuilder.AppendLine($"IsDynamicCodeCompiled: {RuntimeFeature.IsDynamicCodeCompiled}");
            resultBuilder.AppendLine($"IsDynamicCodeSupported: {RuntimeFeature.IsDynamicCodeSupported}");
            resultBuilder.AppendLine($"Is Aot: {new StackTrace(false).GetFrame(0)?.GetMethod() is null}"); // No 100% Guaranteed way to detect AOT.

            resultBuilder.AppendLine();

            resultBuilder.AppendLine($"Env version: {Environment.Version}");
            resultBuilder.AppendLine($"64 bit process: {Environment.Is64BitProcess}");
            resultBuilder.AppendLine($"Privilaged process: {Environment.IsPrivilegedProcess}");

            resultBuilder.AppendLine();

            if (GC.GetConfigurationVariables().TryGetValue("ServerGC", out var serverGC))
                resultBuilder.AppendLine($"ServerGC: {serverGC}");

            if (GC.GetConfigurationVariables().TryGetValue("ConcurrentGC", out var concurrentGC))
                resultBuilder.AppendLine($"ConcurrentGC: {concurrentGC}");
        }
        catch (Exception exp)
        {
            resultBuilder.AppendLine($"{Environment.NewLine}Error while getting diagnostic data: {exp.Message}");
        }

        await messageBoxService.Show("Diagnostics Result", resultBuilder.ToString());
    }

    private async Task OpenDevTools()
    {
        await JSRuntime.InvokeVoidAsync("App.openDevTools");
    }

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
        try
        {
            await userController.DeleteAllWebAuthnCredentials(CurrentCancellationToken);
            // RemoveWebAuthnConfiguredUserId would be deleted using StorageService.Clear below.
        }
        catch (Exception exp)
        {
            logger.LogWarning(exp, "Failed to delete WebAuthn credentials during ClearAppStorage.");
        }

        //#if (offlineDb == true)
        try
        {
            await syncService.Push(); // Try to push any pending changes before clearing the DB.
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

        await storageService.Clear(); // Blazor Hybrid stores key/value pairs outside webview's storage.

        await JSRuntime.ClearWebStorages();

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

    private async Task UpdateApp()
    {
        await appUpdateService.ForceUpdate();
    }
}
