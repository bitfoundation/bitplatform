//+:cnd:noEmit
using Boilerplate.Shared.Controllers.Identity;
using Boilerplate.Shared.Dtos.Identity;

namespace Boilerplate.Server.Api.Controllers.Identity;

[ApiController, Route("api/[controller]/[action]")]
[Authorize(Policy = AppPermissions.Management.ManageRoles)]
public partial class RoleController : AppControllerBase, IRoleController
{
    [HttpGet, EnableQuery]
    public async Task<IQueryable<RoleDto>> GetAllRoles(CancellationToken cancellationToken)
    {
        return DbContext.Roles.Where(r => r.Name != AppRoles.SuperAdmin).Project();
    }

    [HttpGet, EnableQuery]
    public async Task<IQueryable<UserDto>> GetAllUsers(CancellationToken cancellationToken)
    {
        return DbContext.Users.Project();
    }

    [HttpGet, EnableQuery]
    public async Task<IQueryable<UserDto>> GetUsers(Guid roleId, CancellationToken cancellationToken)
    {
        return DbContext.Users.Where(u => u.Roles.Any(r => r.RoleId == roleId)).Project();
    }

    [HttpGet, EnableQuery]
    public async Task<IQueryable<RoleClaimDto>> GetClaims(Guid roleId, CancellationToken cancellationToken)
    {
        return DbContext.RoleClaims.Where(rc => rc.RoleId == roleId).Project();
    }

    [HttpPost]
    [Authorize(Policy = AuthPolicies.ELEVATED_ACCESS)]
    public async Task<RoleDto> Create(RoleDto roleDto, CancellationToken cancellationToken)
    {
        var entityToAdd = roleDto.Map();

        await DbContext.Roles.AddAsync(entityToAdd, cancellationToken);

        await DbContext.SaveChangesAsync(cancellationToken);

        return entityToAdd.Map();
    }

    [HttpPost]
    [Authorize(Policy = AuthPolicies.ELEVATED_ACCESS)]
    public async Task<RoleDto> Update(RoleDto roleDto, CancellationToken cancellationToken)
    {
        var entityToUpdate = await DbContext.Roles.FindAsync([roleDto.Id], cancellationToken)
            ?? throw new ResourceNotFoundException(Localizer[nameof(AppStrings.ProductCouldNotBeFound)]);

        roleDto.Patch(entityToUpdate);

        await DbContext.SaveChangesAsync(cancellationToken);

        return entityToUpdate.Map();
    }

    [HttpPost]
    public async Task TogglePermission(ToggleRolePermissionDto dto, CancellationToken cancellationToken)
    {
        
    }

    [HttpPost]
    public async Task ToggleUser(ToggleRoleUserDto dto, CancellationToken cancellationToken)
    {

    }

    [HttpPost]
    [Authorize(Policy = AuthPolicies.ELEVATED_ACCESS)]
    public async Task SaveMaxPrivilegedSessions(int value, CancellationToken cancellationToken)
    {
        await Task.Delay(1000);
    }

    [HttpPost]
    [Authorize(Policy = AuthPolicies.ELEVATED_ACCESS)]
    public async Task SendNotification(RoleNotificationDto dto, CancellationToken cancellationToken)
    {
        await Task.Delay(1000);
    }
}
