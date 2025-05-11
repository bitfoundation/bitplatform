//+:cnd:noEmit
using Boilerplate.Server.Api.Models.Identity;
using Boilerplate.Shared.Controllers.Identity;
using Boilerplate.Shared.Dtos.Identity;
//#if (signalR == true)
using Boilerplate.Server.Api.SignalR;
using Microsoft.AspNetCore.SignalR;
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


    [HttpGet, EnableQuery]
    public IQueryable<UserDto> GetAllUsers()
    {
        return userManager.Users.Project();
    }

    //#if (signalR == true)
    [HttpGet]
    public async Task<int> GetOnlineUsersCount(CancellationToken cancellationToken)
    {
        return await DbContext.UserSessions.CountAsync(us => us.SignalRConnectionId != null, cancellationToken);
    }
    //#endif

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
            throw new BadRequestException();

        var user = await GetUserByIdAsync(userId, cancellationToken);

        if (await userManager.IsInRoleAsync(user, AppRoles.SuperAdmin))
        {
            if (User.IsInRole(AppRoles.SuperAdmin) is false)
                throw new BadRequestException();
        }

        //#if (signalR == true)
        var userSessions = await DbContext.UserSessions.Where(us => us.UserId == userId).ToListAsync(cancellationToken);
        //await DbContext.UserSessions.Where(us => us.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        //#endif

        await userManager.DeleteAsync(user);

        //#if (signalR == true)
        foreach (var session in userSessions.Where(us => us.SignalRConnectionId is not null))
        {
            await RevokeSession(session.SignalRConnectionId!, cancellationToken);
        }
        //#endif
    }

    [HttpPost("{id}")]
    [Authorize(Policy = AuthPolicies.ELEVATED_ACCESS)]
    public async Task DeleteUserSession(Guid id, CancellationToken cancellationToken)
    {
        var entityToDelete = await DbContext.UserSessions.FindAsync([id], cancellationToken)
            ?? throw new ResourceNotFoundException();

        //#if (signalR == true)
        if (entityToDelete.SignalRConnectionId is not null)
        {
            await RevokeSession(entityToDelete.SignalRConnectionId, cancellationToken);
        }
        //#endif

        DbContext.Remove(entityToDelete);

        await DbContext.SaveChangesAsync(cancellationToken);
    }


    private async Task<User> GetUserByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await userManager.Users.FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
                    ?? throw new ResourceNotFoundException();

        return user;
    }

    private async Task RevokeSession(string connectionId, CancellationToken cancellationToken)
    {
        // Checkout AppHub's comments for more info.
        await appHubContext.Clients.Client(connectionId)
                                   .SendAsync(SignalREvents.PUBLISH_MESSAGE, SharedPubSubMessages.SESSION_REVOKED, null, cancellationToken);
    }
}
