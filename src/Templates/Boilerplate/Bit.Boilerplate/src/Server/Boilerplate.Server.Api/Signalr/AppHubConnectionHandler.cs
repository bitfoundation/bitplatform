using Microsoft.AspNetCore.SignalR;
using Boilerplate.Server.Api.SignalR;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections;

namespace Boilerplate.Server.Api.Signalr;

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

