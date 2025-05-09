//+:cnd:noEmit
using Boilerplate.Server.Api.Models.Identity;
using Boilerplate.Server.Api.Services;
using Boilerplate.Shared.Controllers.Identity;
using Boilerplate.Shared.Dtos.Identity;
//#if (signalR == true)
using Microsoft.AspNetCore.SignalR;
using Boilerplate.Server.Api.SignalR;
//#endif

namespace Boilerplate.Server.Api.Controllers.Identity;

[ApiController, Route("api/[controller]/[action]")]
[Authorize(Policy = AppPermissions.Management.ManageRoles)]
public partial class RoleController : AppControllerBase, IRoleController
{
    //#if (signalR == true)
    [AutoInject] private IHubContext<AppHub> appHubContext = default!;
    //#endif
    //#if (notification == true)
    [AutoInject] private PushNotificationService pushNotificationService = default!;
    //#endif

    [HttpGet, EnableQuery]
    public IQueryable<RoleDto> GetAllRoles()
    {
        return DbContext.Roles.Where(r => r.Name != AppRoles.SuperAdmin).Project();
    }

    [HttpGet, EnableQuery]
    public IQueryable<UserDto> GetAllUsers()
    {
        return DbContext.Users.Project();
    }

    [HttpGet("{roleId}"), EnableQuery]
    public IQueryable<UserDto> GetUsers(Guid roleId)
    {
        return DbContext.Users.Where(u => u.Roles.Any(r => r.RoleId == roleId)).Project();
    }

    [HttpGet("{roleId}"), EnableQuery]
    public IQueryable<RoleClaimDto> GetClaims(Guid roleId)
    {
        return DbContext.RoleClaims.Where(rc => rc.RoleId == roleId).Project();
    }

    [HttpPost]
    [Authorize(Policy = AuthPolicies.ELEVATED_ACCESS)]
    public async Task<RoleDto> Create(RoleDto roleDto, CancellationToken cancellationToken)
    {
        roleDto.NormalizedName = roleDto.Name!.ToUpperInvariant();
        var existingRole = await DbContext.Roles.FirstOrDefaultAsync(r => r.Name == roleDto.Name || r.NormalizedName == roleDto.NormalizedName, cancellationToken);

        if (existingRole is not null)
            throw new BadRequestException(Localizer[nameof(AppStrings.RoleExistErrorMessage)]);

        var entityToAdd = roleDto.Map();

        await DbContext.Roles.AddAsync(entityToAdd, cancellationToken);

        await DbContext.SaveChangesAsync(cancellationToken);

        return entityToAdd.Map();
    }

    [HttpPost]
    [Authorize(Policy = AuthPolicies.ELEVATED_ACCESS)]
    public async Task<RoleDto> Update(RoleDto roleDto, CancellationToken cancellationToken)
    {
        roleDto.NormalizedName = roleDto.Name!.ToUpperInvariant();
        var existingRole = await DbContext.Roles.FirstOrDefaultAsync(r => r.Id != roleDto.Id && (r.Name == roleDto.Name || r.NormalizedName == roleDto.NormalizedName), cancellationToken);

        if (existingRole is not null)
            throw new BadRequestException(Localizer[nameof(AppStrings.RoleExistErrorMessage)]);

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

        return [.. entities.Select(e => e.Map())];
    }

    [HttpPost]
    [Authorize(Policy = AuthPolicies.ELEVATED_ACCESS)]
    public async Task DeleteClaims(List<RoleClaimRequestDto> dtos, CancellationToken cancellationToken)
    {
        await DbContext.RoleClaims.Where(rc => dtos.Select(d => d.Id).Contains(rc.Id))
            .ExecuteDeleteAsync(cancellationToken);
    }

    [HttpPost]
    [Authorize(Policy = AuthPolicies.ELEVATED_ACCESS)]
    public async Task<List<RoleClaimDto>> UpdateClaims(List<RoleClaimRequestDto> dtos, CancellationToken cancellationToken)
    {
        List<RoleClaim> entities = [];

        foreach (var dto in dtos)
        {
            var entityToUpdate = await DbContext.RoleClaims.FirstOrDefaultAsync(rc => rc.Id == dto.Id, cancellationToken)
                                    ?? throw new ResourceNotFoundException();

            entityToUpdate.ClaimValue = dto.ClaimValue;

            entities.Add(entityToUpdate);
        }

        await DbContext.SaveChangesAsync(cancellationToken);

        return [.. entities.Select(e => e.Map())];
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

    //#if (notification == true || signalR == true)
    [HttpPost]
    [Authorize(Policy = AuthPolicies.ELEVATED_ACCESS)]
    public async Task SendNotification(SendNotificationToRoleDto dto, CancellationToken cancellationToken)
    {
        //#if (signalR == true)
        var signalRConnectionIds = await DbContext.UserSessions.Where(us => us.SignalRConnectionId != null && us.User!.Roles.Any(r => r.RoleId == dto.RoleId)).Select(us => us.SignalRConnectionId!).ToArrayAsync(cancellationToken);
        await appHubContext.Clients.Clients(signalRConnectionIds).SendAsync(SignalREvents.SHOW_MESSAGE, dto.Message, cancellationToken);
        //#endif

        //#if (notification == true)
        await pushNotificationService.RequestPush(message: dto.Message, userRelatedPush: true, customSubscriptionFilter: s => s.UserSession!.User!.Roles.Any(r => r.RoleId == dto.RoleId), cancellationToken: cancellationToken);
        //#endif
    }
    //#endif
}
