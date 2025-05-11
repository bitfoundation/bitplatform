//+:cnd:noEmit
using Boilerplate.Server.Api.Models.Identity;
using Boilerplate.Server.Api.SignalR;
using Boilerplate.Shared.Controllers.Identity;
using Boilerplate.Shared.Dtos.Identity;
using Microsoft.AspNetCore.SignalR;

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
        var rolesOfUser = await userManager.GetRolesAsync(new() { Id = userId });

        if (rolesOfUser.Contains(AppRoles.SuperAdmin))
        {
            var rolesOfCurrentUser = await userManager.GetRolesAsync(new() { Id = User.GetUserId() });
            if (rolesOfCurrentUser.Contains(AppRoles.SuperAdmin) is false)
                throw new BadRequestException();

            var superAdminUsers = await userManager.GetUsersInRoleAsync(AppRoles.SuperAdmin);
            if (superAdminUsers.Count < 2)
                throw new BadRequestException();
        }

        await userManager.DeleteAsync(user);
    }

    [HttpPost("{id}")]
    [Authorize(Policy = AuthPolicies.ELEVATED_ACCESS)]
    public async Task DeleteUserSession(Guid id, CancellationToken cancellationToken)
    {
        var entityToDelete = await DbContext.UserSessions.FindAsync([id], cancellationToken)
            ?? throw new ResourceNotFoundException();

        DbContext.Remove(entityToDelete);

        await DbContext.SaveChangesAsync(cancellationToken);

        //#if (signalR == true)
        // Checkout AppHub's comments for more info.
        if (entityToDelete.SignalRConnectionId is not null)
        {
            await appHubContext.Clients.Client(entityToDelete.SignalRConnectionId).SendAsync(SignalREvents.PUBLISH_MESSAGE, SharedPubSubMessages.SESSION_REVOKED, null, cancellationToken);
        }
        //#endif
    }


    private async Task<User> GetUserByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await userManager.Users.FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
                    ?? throw new ResourceNotFoundException();

        return user;
    }
}
