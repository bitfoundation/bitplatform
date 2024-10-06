﻿using Boilerplate.Shared.Dtos.PushNotification;

namespace Boilerplate.Shared.Controllers;

[Route("api/[controller]/[action]/")]
public interface INotificationHubController : IAppController
{
    [HttpPost]
    Task CreateOrUpdateInstallation([Required] DeviceInstallationDto deviceInstallation, CancellationToken cancellationToken);
}
