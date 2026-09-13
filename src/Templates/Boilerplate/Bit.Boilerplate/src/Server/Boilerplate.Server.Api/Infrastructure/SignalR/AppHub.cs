//+:cnd:noEmit
using Boilerplate.Server.Api.Features.Identity;
using Microsoft.AspNetCore.Authentication.BearerToken;

namespace Boilerplate.Server.Api.Infrastructure.SignalR;

/// <summary>
/// SignalR supports basic scenarios like sending messages to all connected clients using `Clients.All()`, 
/// which broadcasts to all SignalR connections, whether authenticated or not. Similarly, `Clients.User(userId)`
/// sends messages to all open browser tabs or applications associated with a specific user.
///
/// In addition to these, the following enhanced scenarios are supported:
/// 1. `Clients.Group("AuthenticatedClients")`: Sends a message to all browser tabs and apps that are signed in.
///    It spans every tenant; for one tenant's data use `TenantGroupName`.
/// 2. Each user session knows its own <see cref="UserSession.SignalRConnectionId"/>. The application
///    already uses this approach in the `<see cref="UserController.RevokeSession(Guid, CancellationToken)"/>` method by sending a SignalR message to 
///    `Clients.Client(userSession.SignalRConnectionId)`. This ensures that the corresponding browser tab or app clears 
///    its access/refresh tokens from storage and navigates to the sign-in page automatically.
///    Read <see cref="UserSession.SignalRConnectionId"/>'s comments for which tab or app that is.
/// </summary>
[AllowAnonymous]
public partial class AppHub : Hub
{
    [AutoInject] private IServiceProvider serviceProvider = default!;
    [AutoInject] private IOptionsMonitor<BearerTokenOptions> bearerTokenOptions = default!;

    public override async Task OnConnectedAsync()
    {
        if (Context.GetHttpContext()?.ContainsExpiredAccessToken() is true)
            throw new HubException(nameof(AppStrings.UnauthorizedException)).WithData("ConnectionId", Context.ConnectionId);

        await ChangeAuthenticationStateImplementation(Context.User, isNewConnection: true);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // ChangeAuthenticationStateImplementation is NOT reachable from here: SignalR aborts the connection
        // before dispatching OnDisconnectedAsync, so its Context.ConnectionAborted guard always returns early.
        // The connection is gone either way, so clear it directly. The "AuthenticatedClients" group needs no
        // cleanup, the lifetime manager removes a disconnected connection from all of its groups.
        await using var scope = serviceProvider.CreateAsyncScope();
        await using var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.UserSessions.Where(us => us.SignalRConnectionId == Context.ConnectionId)
                                    .ExecuteUpdateAsync(us => us.SetProperty(x => x.SignalRConnectionId, (string?)null), CancellationToken.None);

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// While SignalR client is connected, the user might sign-in or sign-out.
    /// In this case, we need to update the authentication state of the SignalR connection.
    /// This method is called by AppClientCoordinator.cs
    /// </summary>
    [HubMethodName(SharedAppMessages.ChangeAuthenticationState)]
    public Task ChangeAuthenticationState(string? accessToken)
    {
        ClaimsPrincipal? user = null;

        if (string.IsNullOrWhiteSpace(accessToken) is false)
        {
            var bearerTokenProtector = bearerTokenOptions.Get(IdentityConstants.BearerScheme).BearerTokenProtector;
            var accessTokenTicket = bearerTokenProtector.Unprotect(accessToken);
            user = accessTokenTicket!.Principal;
        }

        return ChangeAuthenticationStateImplementation(user);
    }

    //#if (multitenant == true)
    /// <summary>
    /// Every signed-in connection of one tenant. Anything derived from a single tenant's data is published here rather
    /// than to "AuthenticatedClients", which spans all tenants.
    /// </summary>
    public static string TenantGroupName(Guid tenantId) => $"AuthenticatedClients_{tenantId}";

    /// <summary>Null when anonymous or no tenant is selected yet.</summary>
    private static string? GetTenantGroupName(ClaimsPrincipal? user)
    {
        return user?.IsAuthenticated() is true && user.GetTenantId() is Guid tenantId ? TenantGroupName(tenantId) : null;
    }
    //#endif

    private async Task ChangeAuthenticationStateImplementation(ClaimsPrincipal? user, bool isNewConnection = false)
    {
        if (Context.ConnectionAborted.IsCancellationRequested)
            return;

        var httpContext = Context.GetHttpContext()!;

        //#if (multitenant == true)
        // Read before the principal is overwritten: a connection outlives sign-out and tenant switches, and the
        // lifetime manager only drops its groups when the connection itself ends.
        var previousTenantGroup = isNewConnection ? null : GetTenantGroupName(httpContext.User);
        //#endif

        httpContext.User = user ?? new ClaimsPrincipal(new ClaimsIdentity()) /*Anonymous*/;

        if (user?.IsAuthenticated() is not true && isNewConnection)
            return; // A brand new connection id cannot be on any row yet, so looking for it would make every
                    // anonymous page view pay for a query that provably matches nothing.

        await using var scope = serviceProvider.CreateAsyncScope();
        await using var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (user?.IsAuthenticated() is true)
        {
            await dbContext.UserSessions.Where(us => us.Id == user.GetSessionId())
                                        .ExecuteUpdateAsync(us => us.SetProperty(x => x.SignalRConnectionId, Context.ConnectionId), Context.ConnectionAborted);
            await Groups.AddToGroupAsync(Context.ConnectionId, "AuthenticatedClients", Context.ConnectionAborted);

            //#if (multitenant == true)
            var tenantGroup = GetTenantGroupName(user);

            if (previousTenantGroup is not null && previousTenantGroup != tenantGroup)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, previousTenantGroup, Context.ConnectionAborted);
            }

            if (tenantGroup is not null)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, tenantGroup, Context.ConnectionAborted);
            }
            //#endif
        }
        else
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "AuthenticatedClients", Context.ConnectionAborted);

            //#if (multitenant == true)
            if (previousTenantGroup is not null)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, previousTenantGroup, Context.ConnectionAborted);
            }
            //#endif

            await dbContext.UserSessions.Where(us => us.SignalRConnectionId == Context.ConnectionId)
                                        .ExecuteUpdateAsync(us => us.SetProperty(x => x.SignalRConnectionId, (string?)null), Context.ConnectionAborted);
        }
    }
}
