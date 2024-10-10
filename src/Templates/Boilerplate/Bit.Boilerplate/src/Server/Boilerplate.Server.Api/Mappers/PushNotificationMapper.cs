﻿using Boilerplate.Server.Api.Models.PushNotification;
using Boilerplate.Shared.Dtos.PushNotification;
using Riok.Mapperly.Abstractions;

namespace Boilerplate.Server.Api.Mappers;

/// <summary>
/// More info at Server/Mappers/README.md
/// </summary>
[Mapper(UseDeepCloning = true)]
public static partial class PushNotificationMapper
{
    public static partial void Patch(this DeviceInstallationDto source, DeviceInstallation destination);
}
