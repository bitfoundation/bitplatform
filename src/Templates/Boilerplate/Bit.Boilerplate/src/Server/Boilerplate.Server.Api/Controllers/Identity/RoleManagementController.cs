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
    public IQueryable<UserDto> GetUsers(Guid roleId)
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

        role.ConcurrencyStamp = Guid.NewGuid().ToString();
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

        if (AppRoles.IsBuiltInRole(role.Name!))
            throw new BadRequestException(Localizer[nameof(AppStrings.CanNotChangeBuiltInRole), role.Name!]);

        if (role.ConcurrencyStamp != roleDto.ConcurrencyStamp)
            throw new ConflictException();

        roleDto.Patch(role);

        var result = await roleManager.UpdateAsync(role);

        if (result.Succeeded is false)
            throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());

        return role.Map();
    }

    [HttpDelete("{roleId}/{concurrencyStamp}")]
    [Authorize(Policy = AuthPolicies.ELEVATED_ACCESS)]
    public async Task Delete(Guid roleId, string concurrencyStamp, CancellationToken cancellationToken)
    {
        var role = await GetRoleByIdAsync(roleId, cancellationToken);

        if (AppRoles.IsBuiltInRole(role.Name!))
            throw new BadRequestException(Localizer[nameof(AppStrings.CanNotChangeBuiltInRole), role.Name!]);

        if (role.ConcurrencyStamp != concurrencyStamp)
            throw new ConflictException();

        await roleManager.DeleteAsync(role);
    }

    [HttpPost("{roleId}")]
    [Authorize(Policy = AuthPolicies.ELEVATED_ACCESS)]
    public async Task AddClaims(Guid roleId, List<ClaimDto> claims, CancellationToken cancellationToken)
    {
        List<RoleClaim> entities = [];

        var role = await GetRoleByIdAsync(roleId, cancellationToken);

        if (role.Name == AppRoles.SuperAdmin)
            throw new BadRequestException(Localizer[nameof(AppStrings.UserCantChangeSuperAdminRoleClaimsErrorMessage)]);

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

        if (role.Name == AppRoles.SuperAdmin)
            throw new BadRequestException(Localizer[nameof(AppStrings.UserCantChangeSuperAdminRoleClaimsErrorMessage)]);

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

        if (role.Name == AppRoles.SuperAdmin)
            throw new BadRequestException(Localizer[nameof(AppStrings.UserCantChangeSuperAdminRoleClaimsErrorMessage)]);

        foreach (var claim in claims)
        {
            var result = await roleManager.RemoveClaimAsync(role, new(claim.ClaimType!, claim.ClaimValue!));

            if (result.Succeeded is false)
                throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());
        }
    }

    [HttpPost]
    [Authorize(Policy = AuthPolicies.ELEVATED_ACCESS)]
    public async Task ToggleUserRole(UserRoleDto dto, CancellationToken cancellationToken)
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
                var otherSuperAdminsCount = await userManager.Users.CountAsync(u => u.Roles.Any(r => r.RoleId == role.Id) && u.Id != user.Id, cancellationToken);

                if (otherSuperAdminsCount == 0)
                    throw new BadRequestException(Localizer[nameof(AppStrings.UserCantUnassignAllSuperAdminsErrorMessage)]);
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

    [HttpPost("{roleId}")]
    [Authorize(Policy = AuthPolicies.ELEVATED_ACCESS)]
    public async Task RemoveAllUsersFromRole(Guid roleId, CancellationToken cancellationToken)
    {
        var role = await GetRoleByIdAsync(roleId, cancellationToken);

        await DbContext.UserRoles.Where(ur => ur.RoleId == roleId).ExecuteDeleteAsync(cancellationToken);
    }

    //#if (notification == true || signalR == true)
    [HttpPost]
    [Authorize(Policy = AuthPolicies.ELEVATED_ACCESS)]
    public async Task SendNotification(SendNotificationToRoleDto dto, CancellationToken cancellationToken)
    {
        //#if (signalR == true)
        var signalRConnectionIds = await DbContext.UserSessions.Where(us => us.NotificationStatus == UserSessionNotificationStatus.Allowed &&
                                                                            us.SignalRConnectionId != null &&
                                                                            us.User!.Roles.Any(r => r.RoleId == dto.RoleId))
                                                               .Select(us => us.SignalRConnectionId!).ToArrayAsync(cancellationToken);

        await appHubContext.Clients.Clients(signalRConnectionIds)
                                   .SendAsync(SharedAppMessages.SHOW_MESSAGE, dto.Message, dto.PageUrl is null ? null : new Dictionary<string, string?> { { "pageUrl", dto.PageUrl } }, cancellationToken);
        //#endif

        //#if (notification == true)
        await pushNotificationService.RequestPush(new()
        {
            Message = dto.Message,
            PageUrl = dto.PageUrl,
            UserRelatedPush = true,
            RequesterUserSessionId = User.GetSessionId()
        }, customSubscriptionFilter: s => s.UserSession!.User!.Roles.Any(r => r.RoleId == dto.RoleId),
                                                  cancellationToken: cancellationToken);
        //#endif
    }
    //#endif


    private async Task<Role> GetRoleByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var role = await roleManager.Roles.FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
                    ?? throw new ResourceNotFoundException();

        return role;
    }
}
