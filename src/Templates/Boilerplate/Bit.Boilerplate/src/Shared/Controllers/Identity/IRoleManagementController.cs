//+:cnd:noEmit
using Boilerplate.Shared.Dtos.Identity;

namespace Boilerplate.Shared.Controllers.Identity;

[Route("api/[controller]/[action]/"), AuthorizedApi]
public interface IRoleManagementController : IAppController
{
    [HttpGet]
    Task<List<RoleDto>> GetAllRoles(CancellationToken cancellationToken) => default!;

    [HttpGet("{roleId}")]
    Task<List<UserDto>> GetUsers(Guid roleId, CancellationToken cancellationToken) => default!;

    [HttpGet("{roleId}")]
    Task<List<ClaimDto>> GetClaims(Guid roleId, CancellationToken cancellationToken) => default!;

    [HttpPost]
    Task<RoleDto> Create(RoleDto roleDto, CancellationToken cancellationToken);

    [HttpPost]
    Task<RoleDto> Update(RoleDto roleDto, CancellationToken cancellationToken);

    [HttpPost("{roleId}")]
    Task Delete(Guid roleId, CancellationToken cancellationToken);

    [HttpPost("{roleId}")]
    Task AddClaims(Guid roleId, List<ClaimDto> roleClaims, CancellationToken cancellationToken);

    [HttpPost("{roleId}")]
    Task UpdateClaims(Guid roleId, List<ClaimDto> roleClaims, CancellationToken cancellationToken);

    [HttpPost("{roleId}")]
    Task DeleteClaims(Guid roleId, List<ClaimDto> roleClaims, CancellationToken cancellationToken);

    [HttpPost]
    Task ToggleUser(UserRoleDto dto, CancellationToken cancellationToken);

    //#if (notification == true || signalR == true)
    [HttpPost]
    Task SendNotification(SendNotificationToRoleDto dto, CancellationToken cancellationToken);
    //#endif
}
