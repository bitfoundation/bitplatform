//+:cnd:noEmit
using Boilerplate.Server.Api.Controllers.Identity;
using Boilerplate.Server.Api.Models.Identity;
using Boilerplate.Shared.Dtos.Diagnostic;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.SignalR;

namespace Boilerplate.Server.Api.SignalR;

/// <summary>
/// SignalR supports basic scenarios like sending messages to all connected clients using `Clients.All()`, 
/// which broadcasts to all SignalR connections, whether authenticated or not. Similarly, `Clients.User(userId)`
/// sends messages to all open browser tabs or applications associated with a specific user.
///
/// In addition to these, the following enhanced scenarios are supported:
/// 1. `Clients.Group("AuthenticatedClients")`: Sends a message to all browser tabs and apps that are signed in.
/// 2. Each user session knows its own <see cref="UserSession.SignalRConnectionId"/>. The application 
///    already uses this approach in the `<see cref="UserController.RevokeSession(Guid, CancellationToken)"/>` method by sending a SignalR message to 
///    `Clients.Client(userSession.SignalRConnectionId)`. This ensures that the corresponding browser tab or app clears 
///    its access/refresh tokens from storage and navigates to the sign-in page automatically.
/// </summary>
[AllowAnonymous]
public partial class AppHub : Hub
{
    [AutoInject] private ServerApiSettings settings = default!;
    [AutoInject] private IServiceProvider serviceProvider = default!;
    [AutoInject] private IOptionsMonitor<BearerTokenOptions> bearerTokenOptions = default!;

    public override async Task OnConnectedAsync()
    {
        if (Context.GetHttpContext()?.ContainsExpiredAccessToken() is true)
            throw new HubException(nameof(AppStrings.UnauthorizedException)).WithData("ConnectionId", Context.ConnectionId);

        await ChangeAuthenticationStateImplementation(Context.User);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await ChangeAuthenticationStateImplementation(null);

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// While SignalR client is connected, the user might sign-in or sign-out.
    /// In this case, we need to update the authentication state of the SignalR connection.
    /// This method is called by AppClientCoordinator.cs
    /// </summary>
    public Task ChangeAuthenticationState(string? accessToken)
    {
        ClaimsPrincipal? user = null;

        if (string.IsNullOrEmpty(accessToken) is false)
        {
            var bearerTokenProtector = bearerTokenOptions.Get(IdentityConstants.BearerScheme).BearerTokenProtector;
            var accessTokenTicket = bearerTokenProtector.Unprotect(accessToken);
            user = accessTokenTicket!.Principal;
        }

        return ChangeAuthenticationStateImplementation(user);
    }

    /// <summary>
    /// <inheritdoc cref="SignalRMethods.UPLOAD_DIAGNOSTIC_LOGGER_STORE"/>
    /// </summary>
    [Authorize(Policy = AppFeatures.System.ManageLogs)]
    public async Task<DiagnosticLogDto[]> GetUserSessionLogs(Guid userSessionId, [FromServices] AppDbContext dbContext)
    {
        var userSessionSignalRConnectionId = await dbContext.UserSessions
            .Where(us => us.Id == userSessionId)
            .Select(us => us.SignalRConnectionId)
            .FirstOrDefaultAsync(Context.ConnectionAborted);

        if (string.IsNullOrEmpty(userSessionSignalRConnectionId))
            return [];

        return await Clients.Client(userSessionSignalRConnectionId).InvokeAsync<DiagnosticLogDto[]>(SignalRMethods.UPLOAD_DIAGNOSTIC_LOGGER_STORE, Context.ConnectionAborted);
    }

    private async Task ChangeAuthenticationStateImplementation(ClaimsPrincipal? user)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        await using var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (user?.IsAuthenticated() is true)
        {
            await dbContext.UserSessions.Where(us => us.Id == user.GetSessionId()).ExecuteUpdateAsync(us => us.SetProperty(x => x.SignalRConnectionId, Context.ConnectionId));
            await Groups.AddToGroupAsync(Context.ConnectionId, "AuthenticatedClients");
        }
        else
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "AuthenticatedClients");
            await dbContext.UserSessions.Where(us => us.SignalRConnectionId == Context.ConnectionId).ExecuteUpdateAsync(us => us.SetProperty(x => x.SignalRConnectionId, (string?)null));
        }
    }
}
