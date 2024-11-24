using Microsoft.AspNetCore.SignalR;

namespace Boilerplate.Server.Api.SignalR;

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
            // Checkout AppHubConnectionHandler's comments for more info.
            await Groups.AddToGroupAsync(Context.ConnectionId, "AuthenticatedClients");
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "AuthenticatedClients");

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// <inheritdoc cref="SignalREvents.PONG"/>
    /// </summary>
    [Authorize]
    public async Task Ping()
    {
        await Clients.Client(Context.User!.GetSessionId().ToString()).SendAsync(SignalREvents.PONG);
    }
}
