using Microsoft.AspNetCore.SignalR;
using Boilerplate.Server.Api.SignalR;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections;

namespace Boilerplate.Server.Api.Signalr;

/// <summary>
/// SignalR supports basic scenarios like sending messages to all connected clients using `Clients.All()`, 
/// which broadcasts to all SignalR connections, whether authenticated or not. Similarly, `Clients.User(userId)`
/// sends messages to all open browser tabs or applications associated with a specific user.
///
/// In addition to these, the following enhanced scenarios are supported:
/// 1. `Clients.Group("AuthenticatedClients")`: Sends a message to all browser tabs and apps that are signed in.
/// 2. User session IDs can function as an equivalent to SignalR connection IDs. For instance, the application 
///    already uses this approach in the `UserController's RevokeSession` method by sending a SignalR message to 
///    `Clients.Client(userSession.Id.ToString())`. This ensures that the corresponding browser tab or app clears 
///    its access/refresh tokens from storage and navigates to the sign-in page if necessary.
/// </summary>
public class AppHubConnectionHandler : HubConnectionHandler<AppHub>
{
    public AppHubConnectionHandler(HubLifetimeManager<AppHub> lifetimeManager, IHubProtocolResolver protocolResolver, IOptions<HubOptions> globalHubOptions, IOptions<HubOptions<AppHub>> hubOptions, ILoggerFactory loggerFactory, IUserIdProvider userIdProvider, IServiceScopeFactory serviceScopeFactory)
        : base(lifetimeManager, protocolResolver, globalHubOptions, hubOptions, loggerFactory, userIdProvider, serviceScopeFactory)
    {
    }

    public override async Task OnConnectedAsync(ConnectionContext connection)
    {
        var user = connection.GetHttpContext()?.User;
        if (user?.IsAuthenticated() is true)
        {
            connection.ConnectionId = user!.GetSessionId().ToString();
        }

        await base.OnConnectedAsync(connection);
    }
}

