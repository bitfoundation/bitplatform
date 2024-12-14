using Microsoft.AspNetCore.SignalR;

namespace Boilerplate.Server.Api.SignalR;

/// <summary>
/// SignalR supports basic scenarios like sending messages to all connected clients using `Clients.All()`, 
/// which broadcasts to all SignalR connections, whether authenticated or not. Similarly, `Clients.User(userId)`
/// sends messages to all open browser tabs or applications associated with a specific user.
///
/// In addition to these, the following enhanced scenarios are supported:
/// 1. `Clients.Group("AuthenticatedClients")`: Sends a message to all browser tabs and apps that are signed in.
/// 2. Each user session knows its own SignalR connection Id. For instance, the application 
///    already uses this approach in the `UserController's RevokeSession` method by sending a SignalR message to 
///    `Clients.Client(userSession.SignalRConnectionId)`. This ensures that the corresponding browser tab or app clears 
///    its access/refresh tokens from storage and navigates to the sign-in page if necessary.
/// </summary>
[AllowAnonymous]
public partial class AppHub : Hub
{
    [AutoInject] private RootServiceScopeProvider rootScopeProvider = default!;

    public override async Task OnConnectedAsync()
    {
        if (Context.User.IsAuthenticated() is false)
        {
            if (Context.GetHttpContext()?.Request.Headers.Authorization.Any() is true)
            {
                // AppHub allows anonymous connections. However, if an Authorization is included
                // and the user is not authenticated, it indicates the client has sent an invalid or expired access token.
                // In this scenario, we should refresh the access token and attempt to reconnect.
                throw new HubException(nameof(AppStrings.UnauthorizedException));
            }
        }
        else
        {
            await using var scope = rootScopeProvider.Invoke();
            await using var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.UserSessions.Where(us => us.Id == Context.User!.GetSessionId()).ExecuteUpdateAsync(us => us.SetProperty(x => x.SignalRConnectionId, Context.ConnectionId));

            await Groups.AddToGroupAsync(Context.ConnectionId, "AuthenticatedClients");
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (Context.User.IsAuthenticated())
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "AuthenticatedClients");

            await using var scope = rootScopeProvider.Invoke();
            await using var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.UserSessions.Where(us => us.Id == Context.User!.GetSessionId()).ExecuteUpdateAsync(us => us.SetProperty(x => x.SignalRConnectionId, (string?)null));
        }

        await base.OnDisconnectedAsync(exception);
    }
}
