namespace Boilerplate.Shared.Controllers.Diagnostics;

[Route("api/[controller]/[action]/")]
public interface IDiagnosticsController : IAppController
{
    [HttpGet("{?signalRConnectionId,pushNotificationSubscriptionDeviceId}")]
    Task<string> PerformDiagnostics(string? signalRConnectionId, string? pushNotificationSubscriptionDeviceId, CancellationToken cancellationToken);
}
