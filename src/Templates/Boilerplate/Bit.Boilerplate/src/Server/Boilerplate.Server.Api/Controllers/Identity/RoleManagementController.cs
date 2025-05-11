//+:cnd:noEmit
using Boilerplate.Server.Api.Services;
using Boilerplate.Shared.Dtos.Identity;
using Boilerplate.Server.Api.Models.Identity;
using Boilerplate.Shared.Controllers.Identity;
//#if (signalR == true)
using Microsoft.AspNetCore.SignalR;
using Boilerplate.Server.Api.SignalR;
//#endif

namespace Boilerplate.Server.Api.Controllers.Identity;

[ApiController, Route("api/[controller]/[action]")]
[Authorize(Policy = AppFeatures.Management.ManageRoles)]
public partial class RoleManagementController : AppControllerBase, IRoleManagementController
{
    //#if (signalR == true)
    [AutoInject] private IHubContext<AppHub> appHubContext = default!;
    //#endif
    //#if (notification == true)
    [AutoInject] private PushNotificationService pushNotificationService = default!;
    //#endif

    [AutoInject] private UserManager<User> userManager = default!;
    [AutoInject] private RoleManager<Role> roleManager = default!;

    [HttpGet, EnableQuery]
    public IQueryable<RoleDto> GetAllRoles()
    {
        var isUserSuperAdmin = User.IsInRole(AppRoles.SuperAdmin);

        return roleManager.Roles
                          .WhereIf(isUserSuperAdmin is false, r => r.Name != AppRoles.SuperAdmin)
                          .Project();
    }

    [HttpGet, EnableQuery]
    public IQueryable<UserDto> GetAllUsers()
    {
        return userManager.Users
            .Where(u => u.EmailConfirmed || u.PhoneNumberConfirmed || u.Logins.Any() /*Social sign-in*/)
            .Project();
    }

    [HttpGet("{roleId}"), EnableQuery]
    public async Task<IQueryable<UserDto>> GetUsers(Guid roleId)
    {
        return userManager.Users.Where(u => u.Roles.Any(r => r.RoleId == roleId)).Project();
    }

    [HttpGet("{roleId}"), EnableQuery]
    public IQueryable<ClaimDto> GetClaims(Guid roleId)
    {
        return DbContext.RoleClaims.Where(rc => rc.RoleId == roleId).Project();
    }

    [HttpPost]
    [Authorize(Policy = AuthPolicies.ELEVATED_ACCESS)]
    public async Task<RoleDto> Create(RoleDto roleDto, CancellationToken cancellationToken)
    {
        var role = roleDto.Map();

        var result = await roleManager.CreateAsync(role);

        if (result.Succeeded is false)
            throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());

        return role.Map();
    }

    [HttpPost]
    [Authorize(Policy = AuthPolicies.ELEVATED_ACCESS)]
    public async Task<RoleDto> Update(RoleDto roleDto, CancellationToken cancellationToken)
    {
        var role = await GetRoleByIdAsync(roleDto.Id, cancellationToken);

        roleDto.Patch(role);

        var result = await roleManager.UpdateAsync(role);

        if (result.Succeeded is false)
            throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());

        return role.Map();
    }

    [HttpPost("{roleId}")]
    [Authorize(Policy = AuthPolicies.ELEVATED_ACCESS)]
    public async Task Delete(Guid roleId, CancellationToken cancellationToken)
    {
        var role = await GetRoleByIdAsync(roleId, cancellationToken);

        await roleManager.DeleteAsync(role);
    }

    [HttpPost("{roleId}")]
    [Authorize(Policy = AuthPolicies.ELEVATED_ACCESS)]
    public async Task AddClaims(Guid roleId, List<ClaimDto> claims, CancellationToken cancellationToken)
    {
        List<RoleClaim> entities = [];

        var role = await GetRoleByIdAsync(roleId, cancellationToken);

        foreach (var claim in claims)
        {
            var result = await roleManager.AddClaimAsync(role, new(claim.ClaimType!, claim.ClaimValue!));

            if (result.Succeeded is false)
                throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());
        }
    }

    [HttpPost("{roleId}")]
    [Authorize(Policy = AuthPolicies.ELEVATED_ACCESS)]
    public async Task UpdateClaims(Guid roleId, List<ClaimDto> claims, CancellationToken cancellationToken)
    {
        var role = await GetRoleByIdAsync(roleId, cancellationToken);

        foreach (var claim in claims)
        {
            var result = await roleManager.RemoveClaimAsync(role, new(claim.ClaimType!, claim.ClaimValue!));

            if (result.Succeeded is false)
                throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());

            result = await roleManager.AddClaimAsync(role, new(claim.ClaimType!, claim.ClaimValue!));

            if (result.Succeeded is false)
                throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());
        }
    }

    [HttpPost("{roleId}")]
    [Authorize(Policy = AuthPolicies.ELEVATED_ACCESS)]
    public async Task DeleteClaims(Guid roleId, List<ClaimDto> claims, CancellationToken cancellationToken)
    {
        var role = await GetRoleByIdAsync(roleId, cancellationToken);

        foreach (var claim in claims)
        {
            var result = await roleManager.RemoveClaimAsync(role, new(claim.ClaimType!, claim.ClaimValue!));

            if (result.Succeeded is false)
                throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());
        }
    }

    [HttpPost]
    [Authorize(Policy = AuthPolicies.ELEVATED_ACCESS)]
    public async Task ToggleUser(UserRoleDto dto, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(dto.UserId.ToString())
            ?? throw new ResourceNotFoundException();

        var role = await roleManager.FindByIdAsync(dto.RoleId.ToString())
            ?? throw new ResourceNotFoundException();

        var isSuperAdminRole = role.Name == AppRoles.SuperAdmin;
        var isSuperAdminUser = User.IsInRole(AppRoles.SuperAdmin);

        if (isSuperAdminRole && isSuperAdminUser is false)
            throw new UnauthorizedException();

        if (await userManager.IsInRoleAsync(user, role.Name!))
        {
            if (isSuperAdminRole)
            {
                var existingSuperAdminRoleUsersExceptCurrentUserCount = await userManager.Users.CountAsync(u => u.Roles.Any(r => r.RoleId == role.Id) && u.Id != user.Id, cancellationToken);

                if (existingSuperAdminRoleUsersExceptCurrentUserCount == 0)
                    throw new BadRequestException();
            }
            var result = await userManager.RemoveFromRoleAsync(user, role.Name!);
            if (result.Succeeded is false)
                throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());
        }
        else
        {
            var result = await userManager.AddToRoleAsync(user, role.Name!);
            if (result.Succeeded is false)
                throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());
        }
    }

    //#if (notification == true || signalR == true)
    [HttpPost]
    [Authorize(Policy = AuthPolicies.ELEVATED_ACCESS)]
    public async Task SendNotification(SendNotificationToRoleDto dto, CancellationToken cancellationToken)
    {
        //#if (signalR == true)
        var signalRConnectionIds = await DbContext.UserSessions.Where(us => us.SignalRConnectionId != null && 
                                                                            us.User!.Roles.Any(r => r.RoleId == dto.RoleId))
                                                               .Select(us => us.SignalRConnectionId!).ToArrayAsync(cancellationToken);

        await appHubContext.Clients.Clients(signalRConnectionIds)
                                   .SendAsync(SignalREvents.SHOW_MESSAGE, dto.Message, cancellationToken);
        //#endif

        //#if (notification == true)
        await pushNotificationService.RequestPush(message: dto.Message, 
                                                  userRelatedPush: true, 
                                                  customSubscriptionFilter: s => s.UserSession!.User!.Roles.Any(r => r.RoleId == dto.RoleId), 
                                                  cancellationToken: cancellationToken);
        //#endif
    }
    //#endif

    private async Task<Role> GetRoleByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var role = await roleManager.Roles.FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
                    ?? throw new ResourceNotFoundException();

        if (role.Name == AppRoles.SuperAdmin)
            throw new BadRequestException();

        return role;
    }
}
