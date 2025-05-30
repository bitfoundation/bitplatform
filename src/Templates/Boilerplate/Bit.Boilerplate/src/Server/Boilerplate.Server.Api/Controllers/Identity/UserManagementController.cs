﻿//+:cnd:noEmit
using Boilerplate.Shared.Dtos.Identity;
using Boilerplate.Server.Api.Models.Identity;
using Boilerplate.Shared.Controllers.Identity;
//#if (signalR == true)
using Microsoft.AspNetCore.SignalR;
using Boilerplate.Server.Api.SignalR;
//#endif

namespace Boilerplate.Server.Api.Controllers.Identity;

[ApiController, Route("api/[controller]/[action]")]
[Authorize(Policy = AppFeatures.Management.ManageUsers)]
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
        return userManager.Users.Project();
    }

    [HttpGet]
    public async Task<int> GetOnlineUsersCount(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        return await DbContext.Users.CountAsync(u => u.Sessions.Any(us => (now - (us.RenewedOn ?? us.StartedOn)) < serverApiSettings.Identity.BearerTokenExpiration.TotalSeconds), cancellationToken);
    }

    [HttpGet("{userId}"), EnableQuery]
    public IQueryable<UserSessionDto> GetUserSessions(Guid userId)
    {
        return DbContext.UserSessions.Where(us => us.UserId == userId).Project();
    }

    [HttpPost("{userId}")]
    [Authorize(Policy = AuthPolicies.ELEVATED_ACCESS)]
    public async Task Delete(Guid userId, CancellationToken cancellationToken)
    {
        if (User.GetUserId() == userId)
            throw new BadRequestException(Localizer[nameof(AppStrings.UserCantRemoveItselfErrorMessage)]);

        var user = await GetUserByIdAsync(userId, cancellationToken);

        if (await userManager.IsInRoleAsync(user, AppRoles.SuperAdmin))
        {
            if (User.IsInRole(AppRoles.SuperAdmin) is false)
                throw new BadRequestException(Localizer[nameof(AppStrings.UserCantRemoveSuperAdminErrorMessage)]);
        }
        
        //#if (signalR == true)
        var userSessionConnectionIds = await DbContext.UserSessions.Where(us => us.UserId == userId && us.SignalRConnectionId != null)
                                                                   .Select(us => us.SignalRConnectionId!)
                                                                   .ToListAsync(cancellationToken);
        //#endif

        await DbContext.UserSessions.Where(us => us.UserId == userId).ExecuteDeleteAsync(cancellationToken);

        await userManager.DeleteAsync(user);

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
            ?? throw new ResourceNotFoundException();

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
        if (userId == User.GetUserId())
            throw new BadRequestException(Localizer[nameof(AppStrings.UserCantRemoveAllItsSessionsErrorMessage)]);
        
        //#if (signalR == true)
        var userSessionConnectionIds = await DbContext.UserSessions.Where(us => us.UserId == userId && us.SignalRConnectionId != null)
                                                                   .Select(us => us.SignalRConnectionId!)
                                                                   .ToListAsync(cancellationToken);
        //#endif
        
        await DbContext.UserSessions.Where(us => us.UserId == userId).ExecuteDeleteAsync(cancellationToken);

        //#if (signalR == true)
        foreach (var id in userSessionConnectionIds)
        {
            await RevokeSession(id, cancellationToken);
        }
        //#endif
    }


    private async Task<User> GetUserByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await userManager.Users.FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
                    ?? throw new ResourceNotFoundException();

        return user;
    }

    //#if (signalR == true)
    private async Task RevokeSession(string connectionId, CancellationToken cancellationToken)
    {
        // Check out AppHub's comments for more info.
        await appHubContext.Clients.Client(connectionId)
                                   .SendAsync(SignalREvents.PUBLISH_MESSAGE, SharedPubSubMessages.SESSION_REVOKED, null, cancellationToken);
    }
    //#endif
}
