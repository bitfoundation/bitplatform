namespace Boilerplate.Shared.Features.Diagnostic;

[Route("api/[controller]/[action]/")]
public interface IDiagnosticController : IAppController
{
    [HttpGet("{?signalRConnectionId,pushNotificationSubscriptionDeviceId}")]
    Task<string> PerformDiagnostic(string? signalRConnectionId, string? pushNotificationSubscriptionDeviceId, CancellationToken cancellationToken);
}
