using Microsoft.AspNetCore.SignalR;

namespace Boilerplate.Server.Api.Signalr;

[AllowAnonymous]
public partial class AppHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        if (Context.User.IsAuthenticated() is false)
        {
            if (Context.GetHttpContext()?.Request?.Query?.TryGetValue("access_token", out var _) is true)
            {
                // AppHub allows anonymous connections. However, if an Authorization is included
                // and the user is not authenticated, it indicates the client has sent an invalid or expired access token.
                // In this scenario, we should refresh the access token and attempt to reconnect.

                throw new HubException(nameof(AppStrings.UnauthorizedException));
            }
        }
        else
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "AuthenticatedClients");
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "AuthenticatedClients");

        await base.OnDisconnectedAsync(exception);
    }
}
