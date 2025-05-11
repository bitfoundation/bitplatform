//+:cnd:noEmit
using Boilerplate.Server.Api.Services;
using Boilerplate.Shared.Dtos.Identity;
using Boilerplate.Server.Api.Models.Identity;
using Boilerplate.Shared.Controllers.Identity;

namespace Boilerplate.Server.Api.Controllers.Identity;

[ApiController, Route("api/[controller]/[action]")]
[Authorize(Policy = AppFeatures.Management.ManageUsers)]
public partial class UserManagementController : AppControllerBase, IUserManagementController
{
    [AutoInject] private PhoneService phoneService = default!;
    [AutoInject] private UserManager<User> userManager = default!;

    //#if (signalR == true)
    [HttpGet]
    public async Task<int> GetOnlineUsersCount(CancellationToken cancellationToken)
    {
        return await DbContext.UserSessions.CountAsync(us => us.SignalRConnectionId != null, cancellationToken);
    }
    //#endif

    [HttpGet, EnableQuery]
    public IQueryable<UserDto> GetAllUsers()
    {
        return userManager.Users.Project();
    }

    [HttpGet("{userId}")]
    public IQueryable<UserSessionDto> GetUserSessions(Guid userId)
    {
        return DbContext.UserSessions.Where(us => us.UserId == userId).Project();
    }

    [HttpPost("{id}")]
    [Authorize(Policy = AuthPolicies.ELEVATED_ACCESS)]
    public async Task DeleteUserSession(Guid id, CancellationToken cancellationToken)
    {
        var entityToDelete = await DbContext.UserSessions.FindAsync([id], cancellationToken)
            ?? throw new ResourceNotFoundException();

        DbContext.Remove(entityToDelete);

        await DbContext.SaveChangesAsync(cancellationToken);
    }


    [HttpPost]
    [Authorize(Policy = AuthPolicies.ELEVATED_ACCESS)]
    public async Task<UserDto> Create(UserDto userDto, CancellationToken cancellationToken)
    {
        var user = userDto.Map();

        if (string.IsNullOrEmpty(userDto.PhoneNumber) is false)
        {
            userDto.PhoneNumber = phoneService.NormalizePhoneNumber(userDto.PhoneNumber!);
        }

        var result = await userManager.CreateAsync(user);

        if (result.Succeeded is false)
            throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());

        return user.Map();
    }

    [HttpPost]
    [Authorize(Policy = AuthPolicies.ELEVATED_ACCESS)]
    public async Task<UserDto> Update(UserDto userDto, CancellationToken cancellationToken)
    {
        var user = await GetUserByIdAsync(userDto.Id, cancellationToken);

        userDto.Patch(user);

        var result = await userManager.UpdateAsync(user);

        if (result.Succeeded is false)
            throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());

        return user.Map();
    }

    [HttpPost("{userId}")]
    [Authorize(Policy = AuthPolicies.ELEVATED_ACCESS)]
    public async Task Delete(Guid userId, CancellationToken cancellationToken)
    {
        var user = await GetUserByIdAsync(userId, cancellationToken);

        await userManager.DeleteAsync(user);
    }


    private async Task<User> GetUserByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await userManager.Users.FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
                    ?? throw new ResourceNotFoundException();

        return user;
    }
}
