//+:cnd:noEmit
using Boilerplate.Shared.Features.Identity.Dtos;
using Boilerplate.Server.Api.Features.Identity.Models;
using Boilerplate.Shared.Features.Identity;
//#if (signalR == true)
using Microsoft.AspNetCore.SignalR;
using Boilerplate.Server.Api.Infrastructure.SignalR;
//#endif

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
        if (User.HasFeature(AppFeatures.Management.Tenants_Manage_Global) is false)
        {
            var tenantId = User.GetTenantId();
            usersQuery = usersQuery.Where(u => u.Tenants.Any(tu => tu.TenantId == tenantId && tu.AcceptedOn != null));
        }
        //#endif

        return await usersQuery.CountAsync(u => u.Sessions.Any(us => (now - (us.RenewedOn ?? us.StartedOn)) < serverApiSettings.Identity.BearerTokenExpiration.TotalSeconds), cancellationToken);
    }

    [HttpGet("{userId}"), EnableQuery]
    public IQueryable<UserSessionDto> GetUserSessions(Guid userId)
    {
        var query = DbContext.UserSessions.Where(us => us.UserId == userId);

        //#if (multitenant == true)
        if (User.HasFeature(AppFeatures.Management.Tenants_Manage_Global) is false)
        {
            // Non Global admins may only see the sessions that are created in (signed into) the current tenant.
            var tenantId = User.GetTenantId();
            query = query.Where(us => us.TenantId == tenantId);
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
        await EnsureUserIsInCurrentTenant(userId, cancellationToken);

        if (User.HasFeature(AppFeatures.Management.Tenants_Manage_Global) is false)
        {
            // Only global admins can actually delete a user account. Deleting a user as a tenant
            // admin means removing her from the current tenant instead.
            await RemoveUserFromCurrentTenant(userId, cancellationToken);
            return;
        }
        //#endif

        var user = await GetUserById(userId, cancellationToken);

        if (await userManager.IsInRoleAsync(user, AppRoles.GlobalAdmin))
        {
            if (User.IsInRole(AppRoles.GlobalAdmin) is false)
                throw new BadRequestException(Localizer[nameof(AppStrings.UserCantRemoveSuperAdminErrorMessage)]);
        }

        //#if (signalR == true)
        var userSessionConnectionIds = await DbContext.UserSessions.Where(us => us.UserId == userId && us.SignalRConnectionId != null)
                                                                   .Select(us => us.SignalRConnectionId!)
                                                                   .ToListAsync(cancellationToken);
        //#endif

        var strategy = DbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await DbContext.Database.BeginTransactionAsync(cancellationToken);

            await DbContext.UserSessions.Where(us => us.UserId == userId).ExecuteDeleteAsync(cancellationToken);

            await userManager.DeleteAsync(user);

            await transaction.CommitAsync(cancellationToken);
        });

        //#if (signalR == true)
        foreach (var id in userSessionConnectionIds)
        {
            await RevokeSession(id, cancellationToken);
        }
        //#endif
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
        await EnsureUserIsInCurrentTenant(entityToDelete.UserId, cancellationToken);

        // Non Global admins may only revoke the sessions that are signed into the current tenant (See GetUserSessions).
        if (User.HasFeature(AppFeatures.Management.Tenants_Manage_Global) is false && entityToDelete.TenantId != User.GetTenantId())
            throw new ResourceNotFoundException().WithData("Reason", "Non Global admins may only revoke the sessions that are signed into the current tenant.");
        //#endif

        DbContext.Remove(entityToDelete);

        await DbContext.SaveChangesAsync(cancellationToken);

        //#if (signalR == true)
        if (entityToDelete.SignalRConnectionId is not null)
        {
            await RevokeSession(entityToDelete.SignalRConnectionId, cancellationToken);
        }
        //#endif
    }

    [HttpPost("{userId}")]
    [Authorize(Policy = AuthPolicies.ELEVATED_ACCESS)]
    public async Task RevokeAllUserSessions(Guid userId, CancellationToken cancellationToken)
    {
        //#if (multitenant == true)
        await EnsureUserIsInCurrentTenant(userId, cancellationToken);
        //#endif

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
        var userSessionConnectionIds = await sessionsToRevokeQuery.Where(us => us.SignalRConnectionId != null)
                                                                  .Select(us => us.SignalRConnectionId!)
                                                                  .ToListAsync(cancellationToken);
        //#endif

        await sessionsToRevokeQuery.ExecuteDeleteAsync(cancellationToken);

        //#if (signalR == true)
        foreach (var id in userSessionConnectionIds)
        {
            await RevokeSession(id, cancellationToken);
        }
        //#endif
    }


    private async Task<User> GetUserById(Guid id, CancellationToken cancellationToken)
    {
        var user = await userManager.Users.FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
                    ?? throw new ResourceNotFoundException().WithData("Reason", "User not found.");

        return user;
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
        var userSessionConnectionIds = await DbContext.UserSessions
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
        foreach (var connectionId in userSessionConnectionIds)
        {
            await RevokeSession(connectionId, cancellationToken);
        }
        //#endif
    }
    //#endif

    //#if (signalR == true)
    private async Task RevokeSession(string connectionId, CancellationToken cancellationToken)
    {
        // Check out AppHub's comments for more info.
        await appHubContext.Clients.Client(connectionId)
            .Publish(SharedAppMessages.SESSION_REVOKED, null, cancellationToken);
    }
    //#endif
}
