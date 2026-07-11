//+:cnd:noEmit
using Boilerplate.Shared.Features.Identity.Dtos;
using Boilerplate.Server.Api.Features.Identity.Models;
using Boilerplate.Shared.Features.Identity;
//#if (signalR == true)
using Microsoft.AspNetCore.SignalR;
using Boilerplate.Server.Api.Infrastructure.SignalR;
//#endif
//#if (notification == true)
using Boilerplate.Server.Api.Features.PushNotification;
//#endif

namespace Boilerplate.Server.Api.Features.Identity;

[ApiVersion(1)]
[ApiController, Route("api/v{v:apiVersion}/[controller]/[action]")]
[Authorize(Policy = AuthPolicies.PRIVILEGED_ACCESS),
    //#if (multitenancy == true)
    Authorize(Policy = AuthPolicies.TENANT_SELECTED),
    //#endif
    Authorize(Policy = AppFeatures.Management.Roles_Write)]
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
        //#if (multitenancy == true)
        var currentTenantId = User.GetTenantId();
        var canManageAllTenants = User.HasFeature(AppFeatures.Management.Tenants_Write_Global);

        return roleManager.Roles
                          .WhereIf(canManageAllTenants is false, r => r.TenantId == currentTenantId) // Non Global admins may only see the roles of the current tenant.
                          .Project();
        //#endif
        //#if (IsInsideProjectTemplate == true)
        /*
        //#endif
        //#if (multitenancy != true)
        var isUserGlobalAdmin = User.IsInRole(AppRoles.GlobalAdmin);

        return roleManager.Roles
                          .WhereIf(isUserGlobalAdmin is false, r => r.Name != AppRoles.GlobalAdmin)
                          .Project();
        //#endif
        //#if (IsInsideProjectTemplate == true)
        */
        //#endif
    }

    [HttpGet, EnableQuery]
    public IQueryable<UserDto> GetAllUsers()
    {
        var query = userManager.Users
                          .Where(u => u.EmailConfirmed || u.PhoneNumberConfirmed || u.Logins.Any() /*External sign-in*/);

        //#if (multitenancy == true)
        if (User.HasFeature(AppFeatures.Management.Tenants_Write_Global) is false)
        {
            // Non Global admins may only see the users of the current tenant that have accepted their invitation.
            var tenantId = User.GetTenantId();
            query = query.Where(u => u.Tenants.Any(tu => tu.TenantId == tenantId && tu.AcceptedOn != null));
        }
        //#endif

        return query.Project();
    }

    [HttpGet("{roleId}"), EnableQuery]
    public IQueryable<UserDto> GetUsers(Guid roleId)
    {
        var query = userManager.Users.Where(u => u.Roles.Any(r => r.RoleId == roleId));

        //#if (multitenancy == true)
        if (User.HasFeature(AppFeatures.Management.Tenants_Write_Global) is false)
        {
            // Non Global admins may only see the roles of the current tenant.
            var tenantId = User.GetTenantId();
            query = query.Where(u => u.Roles.Any(r => r.RoleId == roleId && r.Role!.TenantId == tenantId));
        }
        //#endif

        return query.Project();
    }

    [HttpGet("{roleId}"), EnableQuery]
    public IQueryable<ClaimDto> GetClaims(Guid roleId)
    {
        var query = DbContext.RoleClaims.Where(rc => rc.RoleId == roleId);

        //#if (multitenancy == true)
        if (User.HasFeature(AppFeatures.Management.Tenants_Write_Global) is false)
        {
            // Non Global admins may only see the roles of the current tenant.
            var tenantId = User.GetTenantId();
            query = query.Where(rc => rc.Role!.TenantId == tenantId);
        }
        //#endif

        return query.Project();
    }

    [HttpPost]
    [Authorize(Policy = AuthPolicies.ELEVATED_ACCESS)]
    public async Task<RoleDto> Create(RoleDto roleDto, CancellationToken cancellationToken)
    {
        var role = roleDto.Map();

        //#if (multitenancy == true)
        role.TenantId = User.GetTenantId();
        //#endif

        var result = await roleManager.CreateAsync(role);

        if (result.Succeeded is false)
            throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());

        return role.Map();
    }

    [HttpPost]
    [Authorize(Policy = AuthPolicies.ELEVATED_ACCESS)]
    public async Task<RoleDto> Update(RoleDto roleDto, CancellationToken cancellationToken)
    {
        var role = await GetRoleById(roleDto.Id, cancellationToken);

        if (AppRoles.IsBuiltInRole(role.Name!))
            throw new BadRequestException(Localizer[nameof(AppStrings.CanNotChangeBuiltInRole), role.Name!]);

        roleDto.Patch(role);

        var result = await roleManager.UpdateAsync(role);

        if (result.Succeeded is false)
            throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());

        return role.Map();
    }

    [HttpDelete("{roleId}")]
    [Authorize(Policy = AuthPolicies.ELEVATED_ACCESS)]
    public async Task Delete(Guid roleId, CancellationToken cancellationToken)
    {
        var role = await GetRoleById(roleId, cancellationToken);

        if (AppRoles.IsBuiltInRole(role.Name!))
            throw new BadRequestException(Localizer[nameof(AppStrings.CanNotChangeBuiltInRole), role.Name!]);

        await roleManager.DeleteAsync(role);
    }

    [HttpPost("{roleId}")]
    [Authorize(Policy = AuthPolicies.ELEVATED_ACCESS)]
    public async Task AddClaims(Guid roleId, List<ClaimDto> claims, CancellationToken cancellationToken)
    {
        List<RoleClaim> entities = [];

        var role = await GetRoleById(roleId, cancellationToken);

        EnsureRoleClaimsAreEditable(role);

        EnsureCallerCanGrantClaims(claims);

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
        var role = await GetRoleById(roleId, cancellationToken);

        EnsureRoleClaimsAreEditable(role);

        EnsureCallerCanGrantClaims(claims);

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
        var role = await GetRoleById(roleId, cancellationToken);

        EnsureRoleClaimsAreEditable(role);

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
            ?? throw new ResourceNotFoundException().WithData("Reason", "User not found.");

        var role = await GetRoleById(dto.RoleId, cancellationToken);

        var isGlobalAdminRole = role.Name == AppRoles.GlobalAdmin;
        var isGlobalAdminUser = User.IsInRole(AppRoles.GlobalAdmin);

        if (isGlobalAdminRole && isGlobalAdminUser is false)
            throw new UnauthorizedException();

        //#if (multitenancy == true)
        // Non Global admins may only toggle roles on users of the current tenant that have accepted their invitation.
        if (User.HasFeature(AppFeatures.Management.Tenants_Write_Global) is false)
        {
            var tenantId = User.GetTenantId();

            if (await DbContext.TenantUsers.AnyAsync(tu => tu.UserId == user.Id && tu.TenantId == tenantId && tu.AcceptedOn != null, cancellationToken) is false)
                throw new ResourceNotFoundException().WithData("Reason", "User not found in the current tenant.");
        }

        // userManager.AddToRoleAsync/RemoveFromRoleAsync find the role by its name which is not unique under multi-tenancy
        // (each tenant has its own t-admin role for example), so the UserRoles are managed directly here.
        var userRole = await DbContext.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == user.Id && ur.RoleId == role.Id, cancellationToken);

        if (userRole is not null)
        {
            if (isGlobalAdminRole)
            {
                var otherGlobalAdminsCount = await userManager.Users.CountAsync(u => u.Roles.Any(r => r.RoleId == role.Id) && u.Id != user.Id, cancellationToken);

                if (otherGlobalAdminsCount == 0)
                    throw new BadRequestException(Localizer[nameof(AppStrings.UserCantUnassignAllSuperAdminsErrorMessage)]);
            }

            DbContext.UserRoles.Remove(userRole);
        }
        else
        {
            await DbContext.UserRoles.AddAsync(new() { UserId = user.Id, RoleId = role.Id, TenantId = role.TenantId }, cancellationToken);
        }

        await DbContext.SaveChangesAsync(cancellationToken);
        //#endif
        //#if (IsInsideProjectTemplate == true)
        /*
        //#endif
        //#if (multitenancy != true)
        if (await userManager.IsInRoleAsync(user, role.Name!))
        {
            if (isGlobalAdminRole)
            {
                var otherGlobalAdminsCount = await userManager.Users.CountAsync(u => u.Roles.Any(r => r.RoleId == role.Id) && u.Id != user.Id, cancellationToken);

                if (otherGlobalAdminsCount == 0)
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
        //#endif
        //#if (IsInsideProjectTemplate == true)
        */
        //#endif
    }

    [HttpPost("{roleId}")]
    [Authorize(Policy = AuthPolicies.ELEVATED_ACCESS)]
    public async Task RemoveAllUsersFromRole(Guid roleId, CancellationToken cancellationToken)
    {
        var role = await GetRoleById(roleId, cancellationToken);

        await DbContext.UserRoles.Where(ur => ur.RoleId == roleId).ExecuteDeleteAsync(cancellationToken);
    }

    //#if (notification == true || signalR == true)
    [HttpPost]
    [Authorize(Policy = AuthPolicies.ELEVATED_ACCESS)]
    public async Task SendNotification(SendNotificationToRoleDto dto, CancellationToken cancellationToken)
    {
        // Ensure the target role exists and (for non global admins) belongs to the caller's tenant before broadcasting
        // to its users - otherwise a tenant admin could push an in-app notification to another tenant's users.
        await GetRoleById(dto.RoleId, cancellationToken);

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
            //#if (signalR == true)
            RequesterUserSessionId = User.GetSessionId()
            //#endif
        }, customSubscriptionFilter: s => s.UserSession!.User!.Roles.Any(r => r.RoleId == dto.RoleId),
                                                  cancellationToken: cancellationToken);
        //#endif
    }
    //#endif


    private async Task<Role> GetRoleById(Guid id, CancellationToken cancellationToken)
    {
        var role = await roleManager.Roles.FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
                    ?? throw new ResourceNotFoundException().WithData("Reason", "Role not found.");

        //#if (multitenancy == true)
        // Non Global admins may only manage the roles of the current tenant.
        if (User.HasFeature(AppFeatures.Management.Tenants_Write_Global) is false && role.TenantId != User.GetTenantId())
            throw new ResourceNotFoundException().WithData("Reason", "Role not found in the current tenant.");
        //#endif

        return role;
    }

    private void EnsureRoleClaimsAreEditable(Role role)
    {
        if (role.Name is AppRoles.GlobalAdmin
            //#if (multitenancy == true)
            or AppRoles.TenantAdmin
            //#endif
            )
            throw new BadRequestException(Localizer[nameof(AppStrings.UserCantChangeSuperAdminRoleClaimsErrorMessage)]);
    }

    /// <summary>
    /// A role manager may only grant feature claims they themselves possess, so they cannot escalate privileges by
    /// assigning a feature they lack - for example granting a <see cref="AppFeatures.System"/> feature, or (under
    /// multi-tenancy) the global-admin-only Tenants_Write_Global feature, to a role and thereby gaining those capabilities.
    /// Non-feature claims (e.g. <see cref="AppClaimTypes.MAX_PRIVILEGED_SESSIONS"/>) are not restricted here.
    /// </summary>
    private void EnsureCallerCanGrantClaims(IEnumerable<ClaimDto> claims)
    {
        foreach (var claim in claims)
        {
            if (claim.ClaimType is AppClaimTypes.FEATURES && User.HasFeature(claim.ClaimValue!) is false)
                throw new UnauthorizedException().WithData("Reason", $"Caller does not have the feature claim '{claim.ClaimValue}' and cannot grant it to a role.");
        }
    }
}
