﻿using Boilerplate.Shared.Dtos.PushNotification;

namespace Boilerplate.Client.Maui.Platforms.Windows.Services;

public partial class WindowsPushNotificationService : PushNotificationServiceBase
{
    public override Task<DeviceInstallationDto> GetDeviceInstallation() => 
        throw new NotImplementedException();

}
