﻿using UIKit;
using Plugin.LocalNotification;
using Boilerplate.Shared.Dtos.PushNotification;

namespace Boilerplate.Client.Maui.Platforms.iOS.Services;

public partial class iOSPushNotificationService : PushNotificationServiceBase
{
    public async override Task<bool> IsNotificationSupported()
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

    public override async Task<DeviceInstallationDto> GetDeviceInstallation()
    {
        var installation = new DeviceInstallationDto
        {
            InstallationId = GetDeviceId(),
            Platform = "apns",
            PushChannel = Token
        };

        return installation;
    }
}
