﻿using System.Text;
using System.Diagnostics;
using System.Runtime.CompilerServices;
//#if (signalR == true)
using Microsoft.AspNetCore.SignalR.Client;
//#endif

namespace Boilerplate.Client.Core.Components.Layout;

public partial class AppDiagnosticModal
{
    [AutoInject] Cookie cookie = default!;
    [AutoInject] AuthManager authManager = default!;
    [AutoInject] IStorageService storageService = default!;

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
        catch { }
        //#endif

        //#if (notification == true)
        try
        {
            pushNotificationSubscriptionDeviceId = (await pushNotificationService.GetSubscription(CurrentCancellationToken)).DeviceId;
        }
        catch { }
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

    string GetMemoryUsage()
    {
        long memory = Environment.WorkingSet;
        return $"{memory / (1024.0 * 1024.0):F2} MB";
    }

    async Task ClearCache()
    {
        try
        {
            await authManager.SignOut(default);
        }
        catch { }

        await storageService.Clear();

        foreach (var item in await cookie.GetAll())
        {
            await cookie.Remove(item.Name!);
        }

        if (AppPlatform.IsBlazorHybrid is false)
        {
            await JSRuntime.InvokeVoidAsync("BitBswup.forceRefresh"); // Clears cache storages and uninstalls service-worker.
        }
        else
        {
            NavigationManager.Refresh(forceReload: true);
        }
    }
}
