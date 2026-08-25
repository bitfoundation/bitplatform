//+:cnd:noEmit
using Boilerplate.Shared.Features.Diagnostic;
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

        if (string.IsNullOrEmpty(accessToken) is false)
        {
            var bearerTokenProtector = bearerTokenOptions.Get(IdentityConstants.BearerScheme).BearerTokenProtector;
            var accessTokenTicket = bearerTokenProtector.Unprotect(accessToken);
            user = accessTokenTicket!.Principal;
        }

        return ChangeAuthenticationStateImplementation(user);
    }

    /// <summary>
    /// <inheritdoc cref="SharedAppMessages.UPLOAD_DIAGNOSTIC_LOGGER_STORE"/>
    /// </summary>
    /// <remarks>
    /// The authorization check is imperative rather than a [Authorize] attribute on purpose. A hub method's
    /// [Authorize] is evaluated against HubCallerContext.User, which is captured once at the handshake and can
    /// never be updated afterwards, while this app maintains its principal imperatively through
    /// <see cref="ChangeAuthenticationState(string?)"/>. With the attribute, a user who signs in AFTER the hub
    /// connected (the ordinary path, since the connection is never restarted on sign-in) is refused, and a user
    /// who signs out keeps whatever authorization the handshake granted.
    /// </remarks>
    [HubMethodName(SharedAppMessages.GetUserSessionLogs)]
    public async Task<DiagnosticLogDto[]> GetUserSessionLogs(Guid userSessionId, [FromServices] AppDbContext dbContext, [FromServices] IAuthorizationService authorizationService)
    {
        var user = Context.GetHttpContext()!.User;

        if ((await authorizationService.AuthorizeAsync(user, AppFeatures.System.Logs_View)).Succeeded is false)
            throw new HubException(nameof(AppStrings.UnauthorizedException)).WithData("ConnectionId", Context.ConnectionId);

        var query = dbContext.UserSessions.Where(us => us.Id == userSessionId);

        //#if (multitenant == true)
        // Scoped exactly like UserManagementController.GetUserSessions: a caller without Tenants_Manage_Global
        // may only reach sessions of accepted members of their own tenant. UserSession is not ITenantAware, so
        // nothing supplies this implicitly.
        if (user.HasFeature(AppFeatures.Management.Tenants_Manage_Global) is false)
        {
            var tenantId = user.GetTenantId();
            query = query.Where(us => us.TenantId == tenantId &&
                                      us.User!.Tenants.Any(tu => tu.TenantId == tenantId && tu.AcceptedOn != null));
        }
        //#endif

        var userSession = await query
            .Select(us => new { us.UserId, us.SignalRConnectionId })
            .FirstOrDefaultAsync(Context.ConnectionAborted);

        if (userSession is null || string.IsNullOrEmpty(userSession.SignalRConnectionId))
            return [];

        // Same rule as UserManagementController.EnsureCallerCanRevokeSessionsOf: Logs_View is delegable to an
        // ordinary role, so its holder is normally not a global admin and must not reach a global admin's device.
        if (user.IsInRole(AppRoles.GlobalAdmin) is false &&
            await dbContext.UserRoles.AnyAsync(ur => ur.UserId == userSession.UserId && ur.Role!.Name == AppRoles.GlobalAdmin, Context.ConnectionAborted))
            throw new HubException(nameof(AppStrings.ForbiddenException)).WithData("ConnectionId", Context.ConnectionId);

        return await Clients.Client(userSession.SignalRConnectionId).InvokeAsync<DiagnosticLogDto[]>(SharedAppMessages.UPLOAD_DIAGNOSTIC_LOGGER_STORE, Context.ConnectionAborted);
    }

    private async Task ChangeAuthenticationStateImplementation(ClaimsPrincipal? user, bool isNewConnection = false)
    {
        if (Context.ConnectionAborted.IsCancellationRequested)
            return;

        Context.GetHttpContext()!.User = user ?? new ClaimsPrincipal(new ClaimsIdentity()) /*Anonymous*/;

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
        }
        else
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "AuthenticatedClients", Context.ConnectionAborted);

            await dbContext.UserSessions.Where(us => us.SignalRConnectionId == Context.ConnectionId)
                                        .ExecuteUpdateAsync(us => us.SetProperty(x => x.SignalRConnectionId, (string?)null), Context.ConnectionAborted);
        }
    }
}
