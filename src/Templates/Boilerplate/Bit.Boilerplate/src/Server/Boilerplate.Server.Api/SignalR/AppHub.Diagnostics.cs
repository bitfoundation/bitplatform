//+:cnd:noEmit
using Boilerplate.Shared.Dtos.Diagnostic;
using Microsoft.AspNetCore.SignalR;

namespace Boilerplate.Server.Api.SignalR;

public partial class AppHub
{
    /// <summary>
    /// Adds the admin caller to a temp group and requests the target client to upload logs.
    /// Returns a correlationId (group name) or null if target not connected.
    /// </summary>
    [Authorize(Policy = AppFeatures.System.ManageLogs)]
    public async Task<string?> BeginUserSessionLogs(
        Guid userSessionId,
        [FromServices] AppDbContext dbContext)
    {
        var targetConnId = await dbContext.UserSessions
            .Where(us => us.Id == userSessionId)
            .Select(us => us.SignalRConnectionId)
            .FirstOrDefaultAsync();

        if (string.IsNullOrEmpty(targetConnId))
            return null;

        var correlationId = Guid.NewGuid().ToString("N");

        await Groups.AddToGroupAsync(Context.ConnectionId, correlationId, Context.ConnectionAborted);

        await Clients.Client(targetConnId)
            .SendAsync(SignalRMethods.REQUEST_UPLOAD_DIAGNOSTIC_LOGGER_BATCH, correlationId, Context.ConnectionAborted);

        return correlationId;
    }

    [Authorize(Policy = AppFeatures.System.ManageLogs)]
    public Task EndUserSessionLogs(string correlationId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, correlationId);

    /// <summary>
    /// Target client -> Hub: streams its logs once; hub forwards each item to the admin correlation group.
    /// Handles completion, cancellation and errors.
    /// </summary>
    [HubMethodName(SignalRMethods.UPLOAD_DIAGNOSTIC_LOGGER_STREAM)]
    public async Task UploadDiagnosticLoggerStream(
        string correlationId,
        IAsyncEnumerable<DiagnosticLogDto> logs)
    {
        try
        {
            await foreach (var log in logs)
            {
                await Clients.Group(correlationId)
                    .SendAsync(SignalRMethods.UPLOAD_DIAGNOSTIC_LOGGER_STORE, log);
            }

            await Clients.Group(correlationId)
                .SendAsync(SignalRMethods.DIAGNOSTIC_LOGS_COMPLETE, correlationId);
        }
        catch (OperationCanceledException)
        {
            await Clients.Group(correlationId)
                .SendAsync(SignalRMethods.DIAGNOSTIC_LOGS_ABORTED, correlationId);
        }
        catch
        {
            await Clients.Group(correlationId)
                .SendAsync(SignalRMethods.DIAGNOSTIC_LOGS_ERROR, correlationId);
        }
    }
}
