﻿using Boilerplate.Shared.Dtos.PushNotification;

namespace Boilerplate.Client.Core.Services.Contracts;

public interface IPushNotificationService
{
    string Token { get; set; }
    Task<bool> IsNotificationSupported(CancellationToken cancellationToken);
    Task<DeviceInstallationDto> GetDeviceInstallation(CancellationToken cancellationToken);
    Task RegisterDevice(CancellationToken cancellationToken);
}
