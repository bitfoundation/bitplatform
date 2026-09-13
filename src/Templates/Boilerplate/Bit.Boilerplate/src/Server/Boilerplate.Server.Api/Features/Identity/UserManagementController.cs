//+:cnd:noEmit

namespace Boilerplate.Server.Api.Features.Identity;

[ApiVersion(1)]
[ApiController, Route("api/v{v:apiVersion}/[controller]/[action]")]
[Authorize(Policy = AuthPolicies.PRIVILEGED_ACCESS),
    //#if (multitenant == true)
    Authorize(Policy = AuthPolicies.TENANT_SELECTED),
    //#endif
    Authorize(Policy = AppFeatures.Management.Users_Manage)]
public partial class UserManagementController : AppControllerBase, IUserManagementController
{
    [AutoInject] private UserManager<User> userManager = default!;
    //#if (signalR == true)
    [AutoInject] private IHubContext<AppHub> appHubContext = default!;
    //#endif
    [AutoInject] private ServerApiSettings serverApiSettings = default!;
    [AutoInject] private UserErasureService userErasureService = default!;


    [HttpGet, EnableQuery]
    public IQueryable<UserDto> GetAllUsers()
    {
        //#if (multitenant == true)
        if (User.HasFeature(AppFeatures.Management.Tenants_Manage_Global) is false)
        {
            // Non Global admins may only see the users of the current tenant that have accepted their invitation.
            var tenantId = User.GetTenantId();
            return userManager.Users.Where(u => u.Tenants.Any(tu => tu.TenantId == tenantId && tu.AcceptedOn != null)).Project();
        }
        //#endif
        return userManager.Users.Project();
    }

    [HttpGet]
    public async Task<int> GetOnlineUsersCount(CancellationToken cancellationToken)
    {
        var now = TimeProvider.GetUtcNow().ToUnixTimeSeconds();

        var usersQuery = DbContext.Users.AsQueryable();

        //#if (multitenant == true)
        var currentTenantId = User.HasFeature(AppFeatures.Management.Tenants_Manage_Global) ? null : User.GetTenantId();

        if (currentTenantId is not null)
        {
            usersQuery = usersQuery.Where(u => u.Tenants.Any(tu => tu.TenantId == currentTenantId && tu.AcceptedOn != null));
        }

        // The session predicate has to be tenant scoped too, otherwise a tenant admin's "online users" count includes
        // members of their tenant who are currently active only in a DIFFERENT tenant - a wrong number and a cross-tenant
        // presence signal. GetUserSessions applies the same rule.
        return await usersQuery.CountAsync(u => u.Sessions.Any(us => (currentTenantId == null || us.TenantId == currentTenantId) &&
                                                                     (now - (us.RenewedOn ?? us.StartedOn)) < serverApiSettings.Identity.BearerTokenExpiration.TotalSeconds), cancellationToken);
        //#endif
        //#if (IsInsideProjectTemplate == true)
        /*
        //#endif
        //#if (multitenant != true)
        return await usersQuery.CountAsync(u => u.Sessions.Any(us => (now - (us.RenewedOn ?? us.StartedOn)) < serverApiSettings.Identity.BearerTokenExpiration.TotalSeconds), cancellationToken);
        //#endif
        //#if (IsInsideProjectTemplate == true)
        */
        //#endif
    }

    [HttpGet("{userId}"), EnableQuery]
    public IQueryable<UserSessionDto> GetUserSessions(Guid userId)
    {
        var query = DbContext.UserSessions.Where(us => us.UserId == userId);

        //#if (multitenant == true)
        if (User.HasFeature(AppFeatures.Management.Tenants_Manage_Global) is false)
        {
            var tenantId = User.GetTenantId();
            query = query.Where(us => us.TenantId == tenantId &&
                                      us.User!.Tenants.Any(tu => tu.TenantId == tenantId && tu.AcceptedOn != null));
        }
        //#endif

        return query.Project();
    }

    [HttpPost("{userId}")]
    [Authorize(Policy = AuthPolicies.ELEVATED_ACCESS)]
    public async Task Delete(Guid userId, CancellationToken cancellationToken)
    {
        if (User.GetUserId() == userId)
            throw new BadRequestException(Localizer[nameof(AppStrings.UserCantRemoveItselfErrorMessage)]);

        //#if (multitenant == true)
        // Tenant scoping comes first. Both guards below throw, but with different status codes - 400 from the
        // global-admin check, 404 from this one - so asking "is the target a global admin?" before "is the target
        // even in my tenant?" lets a tenant admin probe global-admin membership across tenants by the status code.
        await EnsureUserIsInCurrentTenant(userId, cancellationToken);
        //#endif

        // The guard has to run here, ahead of the multitenant branch below: that branch deletes the target's
        // UserSession rows for the current tenant, so moving the check after it would re-open the denial of
        // service the guard exists to prevent. Only the message differs - the caller pressed Delete, not Revoke.
        await EnsureCallerCanRevokeSessionsOf(userId, cancellationToken,
                                              Localizer[nameof(AppStrings.UserCantRemoveSuperAdminErrorMessage)]);

        //#if (multitenant == true)
        if (User.HasFeature(AppFeatures.Management.Tenants_Manage_Global) is false)
        {
            // Only global admins can actually delete a user account. Deleting a user as a tenant
            // admin means removing her from the current tenant instead.
            await RemoveUserFromCurrentTenant(userId, cancellationToken);
            return;
        }
        //#endif

        await userErasureService.Erase(userId, exceptSessionId: null, cancellationToken);
    }

    [HttpPost("{id}")]
    [Authorize(Policy = AuthPolicies.ELEVATED_ACCESS)]
    public async Task RevokeUserSession(Guid id, CancellationToken cancellationToken)
    {
        if (id == User.GetSessionId())
            throw new BadRequestException(Localizer[nameof(AppStrings.UserCantRemoveItsCurrentSessionsErrorMessage)]);

        var entityToDelete = await DbContext.UserSessions.FindAsync([id], cancellationToken)
            ?? throw new ResourceNotFoundException().WithData("Reason", "User session not found.");

        //#if (multitenant == true)
        // Before the global-admin guard, so its 400 cannot be told apart from this one's 404 for a target outside
        // the current tenant - that difference answers "is this user a global admin?" across tenants (See Delete).
        await EnsureUserIsInCurrentTenant(entityToDelete.UserId, cancellationToken);
        //#endif

        await EnsureCallerCanRevokeSessionsOf(entityToDelete.UserId, cancellationToken);

        //#if (multitenant == true)
        // Non Global admins may only revoke the sessions that are signed into the current tenant (See GetUserSessions).
        if (User.HasFeature(AppFeatures.Management.Tenants_Manage_Global) is false && entityToDelete.TenantId != User.GetTenantId())
            throw new ResourceNotFoundException().WithData("Reason", "Non Global admins may only revoke the sessions that are signed into the current tenant.");
        //#endif

        DbContext.Remove(entityToDelete);

        await DbContext.SaveChangesAsync(cancellationToken);

        //#if (signalR == true)
        if (entityToDelete.SignalRConnectionId is not null)
        {
            await RevokeSessions([entityToDelete.SignalRConnectionId], cancellationToken);
        }
        //#endif
    }

    [HttpPost("{userId}")]
    [Authorize(Policy = AuthPolicies.ELEVATED_ACCESS)]
    public async Task RevokeAllUserSessions(Guid userId, CancellationToken cancellationToken)
    {
        //#if (multitenant == true)
        // Before the global-admin guard, for the reason Delete spells out: the two throw different status codes.
        await EnsureUserIsInCurrentTenant(userId, cancellationToken);
        //#endif

        await EnsureCallerCanRevokeSessionsOf(userId, cancellationToken);

        var userSessionId = User.GetSessionId();

        var sessionsToRevokeQuery = DbContext.UserSessions.Where(us => us.Id != userSessionId && us.UserId == userId);

        //#if (multitenant == true)
        if (User.HasFeature(AppFeatures.Management.Tenants_Manage_Global) is false)
        {
            // Non Global admins may only revoke the sessions that are signed into the current tenant (See GetUserSessions).
            var tenantId = User.GetTenantId();
            sessionsToRevokeQuery = sessionsToRevokeQuery.Where(us => us.TenantId == tenantId);
        }
        //#endif

        //#if (signalR == true)
        var userSessionConnectionIdsToRevoke = await sessionsToRevokeQuery.Where(us => us.SignalRConnectionId != null)
                                                                          .Select(us => us.SignalRConnectionId!)
                                                                          .ToListAsync(cancellationToken);
        //#endif

        await sessionsToRevokeQuery.ExecuteDeleteAsync(cancellationToken);

        //#if (signalR == true)
        await RevokeSessions(userSessionConnectionIdsToRevoke, cancellationToken);
        //#endif
    }


    /// <summary>
    /// Only a global admin may act on another global admin - the rule <see cref="Delete"/> already applies, extended to
    /// the session revocation endpoints.
    /// <para>
    /// <see cref="AppFeatures.Management.Users_Manage"/> is delegable: a global admin can grant it to any user-group, so
    /// its holder is normally NOT a global admin herself. Revoking a session deletes the <see cref="UserSession"/> row,
    /// which kills both the access and the refresh token bound to it. So without this check a delegated user manager
    /// could call <see cref="RevokeAllUserSessions"/> against the global admin in a loop: every time he signs in he is
    /// thrown out again within a second, and he can never complete the several requests it takes to reach the roles page
    /// and revoke that delegation. The one account able to undo the attack is exactly the one being denied service.
    /// </para>
    /// </summary>
    /// <remarks>
    /// There is exactly one g-admin role, and it cannot go away: <c>RoleManagementController.Create</c> and
    /// <c>Update</c> both refuse <see cref="AppRoles.IsBuiltInRole"/> names, so no second one can be created and no
    /// other role can be renamed to it, and <c>Delete</c> refuses to remove it. Hence the single membership query -
    /// there is no "which g-admin role" question to answer and no missing-role case to handle.
    /// </remarks>
    private async Task EnsureCallerCanRevokeSessionsOf(Guid targetUserId, CancellationToken cancellationToken, string? errorMessage = null)
    {
        if (User.IsInRole(AppRoles.GlobalAdmin))
            return;

        if (await DbContext.UserRoles.AnyAsync(ur => ur.UserId == targetUserId && ur.Role!.Name == AppRoles.GlobalAdmin, cancellationToken))
            throw new BadRequestException(errorMessage ?? Localizer[nameof(AppStrings.UserCantRevokeSuperAdminSessionsErrorMessage)]);
    }

    //#if (multitenant == true)
    /// <summary>
    /// Non Global admins may only manage the users of the current tenant that have accepted their invitation.
    /// </summary>
    private async Task EnsureUserIsInCurrentTenant(Guid userId, CancellationToken cancellationToken)
    {
        if (User.HasFeature(AppFeatures.Management.Tenants_Manage_Global))
            return;

        var tenantId = User.GetTenantId();

        if (await DbContext.TenantUsers.AnyAsync(tu => tu.UserId == userId && tu.TenantId == tenantId && tu.AcceptedOn != null, cancellationToken) is false)
            throw new ResourceNotFoundException().WithData("Reason", "Non Global admins may only manage the users of the current tenant that have accepted their invitation.");
    }

    /// <summary>
    /// Removes the user from the current tenant (instead of deleting her account) by deleting her TenantUser record
    /// alongside her tenant scoped roles/claims, and revokes the sessions she has signed into the current tenant so
    /// her already-issued access token can't retain that tenant's access/roles until it expires (mirrors
    /// TenantManagementController.KickOutTenantUsers); sessions signed into her other tenants are left intact. Unlike
    /// leaving, the user can't re-join afterwards unless she gets invited again, because the TenantUser record no longer exists.
    /// </summary>
    private async Task RemoveUserFromCurrentTenant(Guid userId, CancellationToken cancellationToken)
    {
        var tenantId = User.GetTenantId();

        //#if (signalR == true)
        var userSessionConnectionIdsInTenant = await DbContext.UserSessions
            .Where(us => us.UserId == userId && us.TenantId == tenantId && us.SignalRConnectionId != null)
            .Select(us => us.SignalRConnectionId!)
            .ToListAsync(cancellationToken);
        //#endif

        await DbContext.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await DbContext.Database.BeginTransactionAsync(cancellationToken);

            await DbContext.TenantUsers.Where(tu => tu.UserId == userId && tu.TenantId == tenantId).ExecuteDeleteAsync(cancellationToken);
            await DbContext.UserRoles.Where(ur => ur.UserId == userId && ur.TenantId == tenantId).ExecuteDeleteAsync(cancellationToken);
            await DbContext.UserClaims.Where(uc => uc.UserId == userId && uc.TenantId == tenantId).ExecuteDeleteAsync(cancellationToken);
            await DbContext.UserSessions.Where(us => us.UserId == userId && us.TenantId == tenantId).ExecuteDeleteAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        });

        //#if (signalR == true)
        await RevokeSessions(userSessionConnectionIdsInTenant, cancellationToken);
        //#endif
    }
    //#endif

    //#if (signalR == true)
    private async Task RevokeSessions(IReadOnlyList<string> signalRConnectionIds, CancellationToken cancellationToken)
    {
        // Check out AppHub's comments for more info.
        await appHubContext.Clients.Clients(signalRConnectionIds)
            .Publish(SharedAppMessages.SESSION_REVOKED, null, cancellationToken);
    }
    //#endif
}

