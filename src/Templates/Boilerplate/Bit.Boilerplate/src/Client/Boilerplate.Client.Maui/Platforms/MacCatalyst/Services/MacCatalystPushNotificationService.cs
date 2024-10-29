﻿using UIKit;
using Plugin.LocalNotification;
using Boilerplate.Shared.Dtos.PushNotification;
using Microsoft.Extensions.Logging;

namespace Boilerplate.Client.Maui.Platforms.MacCatalyst.Services;

public partial class MacCatalystPushNotificationService : PushNotificationServiceBase
{
    public async override Task<bool> IsNotificationSupported(CancellationToken cancellationToken)
    {
        return await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            if (await LocalNotificationCenter.Current.AreNotificationsEnabled() is false)
            {
                await LocalNotificationCenter.Current.RequestNotificationPermission();
            }

            return await LocalNotificationCenter.Current.AreNotificationsEnabled();
        });
    }

    public string GetDeviceId() => UIDevice.CurrentDevice.IdentifierForVendor.ToString();

    public override async Task<DeviceInstallationDto> GetDeviceInstallation(CancellationToken cancellationToken)
    {
        using CancellationTokenSource cts = new(TimeSpan.FromSeconds(30));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);

        try
        {
            while (string.IsNullOrEmpty(Token))
            {
                // After the NotificationsSupported Task completes with a result of true,
                // we use UNUserNotificationCenter.Current.Delegate.
                // This method is asynchronous and we need to wait for it to complete.
                await Task.Delay(TimeSpan.FromSeconds(1), linkedCts.Token);
            }
        }
        finally
        {
            if (Token is null)
            {
                Logger.LogError("Unable to resolve token for APNS.");
            }
        }

        var installation = new DeviceInstallationDto
        {
            InstallationId = GetDeviceId(),
            Platform = "apns",
            PushChannel = Token
        };

        return installation;
    }
}
