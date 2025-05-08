//+:cnd:noEmit
using Boilerplate.Server.Api.Models.Identity;
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

    [HttpGet("{roleId}")]
    public async Task<IQueryable<UserDto>> GetUsers(Guid roleId, CancellationToken cancellationToken)
    {
        return DbContext.Users.Where(u => u.Roles.Any(r => r.RoleId == roleId)).Project();
    }

    [HttpGet("{roleId}")]
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
                                ?? throw new ResourceNotFoundException();

        roleDto.Patch(entityToUpdate);

        await DbContext.SaveChangesAsync(cancellationToken);

        return entityToUpdate.Map();
    }

    [HttpPost]
    [Authorize(Policy = AuthPolicies.ELEVATED_ACCESS)]
    public async Task<List<RoleClaimDto>> AddClaims(List<RoleClaimRequestDto> dtos, CancellationToken cancellationToken)
    {
        List<RoleClaim> entities = [];

        foreach (var dto in dtos)
        {
            //TODO: first check if this claim has already been added to the role or not!

            var entityToAdd = new RoleClaim { RoleId = dto.RoleId, ClaimType = dto.ClaimType, ClaimValue = dto.ClaimValue };

            await DbContext.RoleClaims.AddAsync(entityToAdd, cancellationToken);

            entities.Add(entityToAdd);
        }

        await DbContext.SaveChangesAsync(cancellationToken);

        return entities.Select(e => e.Map()).ToList();
    }

    [HttpPost]
    [Authorize(Policy = AuthPolicies.ELEVATED_ACCESS)]
    public async Task<List<RoleClaimDto>> DeleteClaims(List<RoleClaimRequestDto> dtos, CancellationToken cancellationToken)
    {
        List<RoleClaim> entities = [];

        foreach (var dto in dtos)
        {
            var entityToDelete = await DbContext.RoleClaims.FirstOrDefaultAsync(rc => rc.Id == dto.Id)
                                    ?? throw new ResourceNotFoundException();

            DbContext.Remove(entityToDelete);

            entities.Add(entityToDelete);
        }

        await DbContext.SaveChangesAsync(cancellationToken);

        return entities.Select(e => e.Map()).ToList();
    }

    [HttpPost]
    public async Task<List<RoleClaimDto>> UpdateClaims(List<RoleClaimRequestDto> dtos, CancellationToken cancellationToken)
    {
        List<RoleClaim> entities = [];

        foreach (var dto in dtos)
        {
            var entityToUpdate = await DbContext.RoleClaims.FirstOrDefaultAsync(rc => rc.Id == dto.Id)
                                    ?? throw new ResourceNotFoundException();

            entityToUpdate.ClaimValue = dto.ClaimValue;

            entities.Add(entityToUpdate);
        }

        await DbContext.SaveChangesAsync(cancellationToken);

        return entities.Select(e => e.Map()).ToList();
    }

    [HttpPost]
    [Authorize(Policy = AuthPolicies.ELEVATED_ACCESS)]
    public async Task ToggleUser(ToggleRoleUserDto dto, CancellationToken cancellationToken)
    {
        if (dto.IsAdd)
        {
            var entityToAdd = new UserRole { UserId = dto.UserId, RoleId = dto.RoleId };
            await DbContext.UserRoles.AddAsync(entityToAdd, cancellationToken);
        }
        else
        {
            var entityToDelete = await DbContext.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == dto.UserId && ur.RoleId == dto.RoleId, cancellationToken)
                                    ?? throw new ResourceNotFoundException();

            DbContext.Remove(entityToDelete);
        }

        await DbContext.SaveChangesAsync(cancellationToken);
    }

    [HttpPost]
    [Authorize(Policy = AuthPolicies.ELEVATED_ACCESS)]
    public async Task SendNotification(RoleNotificationDto dto, CancellationToken cancellationToken)
    {
        await Task.Delay(1000);
    }
}
