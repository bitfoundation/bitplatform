//+:cnd:noEmit
using Boilerplate.Shared.Features.Identity.Dtos;

namespace Boilerplate.Shared.Features.Identity;

[Route("api/v1/[controller]/[action]/"), AuthorizedApi]
public interface IRoleManagementController : IAppController
{
    [HttpGet]
    Task<List<RoleDto>> GetAllRoles(CancellationToken cancellationToken) => default!;

    [HttpGet]
    Task<List<UserDto>> GetAllUsers(CancellationToken cancellationToken) => default!;

    [HttpGet("{roleId}")]
    Task<List<UserDto>> GetUsers(Guid roleId, CancellationToken cancellationToken) => default!;

    [HttpGet("{roleId}")]
    Task<List<ClaimDto>> GetClaims(Guid roleId, CancellationToken cancellationToken) => default!;

    [HttpPost]
    Task<RoleDto> Create(RoleDto roleDto, CancellationToken cancellationToken);

    [HttpPost]
    Task<RoleDto> Update(RoleDto roleDto, CancellationToken cancellationToken);

    [HttpDelete("{roleId}")]
    Task Delete(Guid roleId, CancellationToken cancellationToken);

    [HttpPost("{roleId}")]
    Task AddClaims(Guid roleId, List<ClaimDto> roleClaims, CancellationToken cancellationToken);

    [HttpPost("{roleId}")]
    Task UpdateClaims(Guid roleId, List<ClaimDto> roleClaims, CancellationToken cancellationToken);

    [HttpPost("{roleId}")]
    Task DeleteClaims(Guid roleId, List<ClaimDto> roleClaims, CancellationToken cancellationToken);

    [HttpPost]
    Task ToggleUserRole(UserRoleDto dto, CancellationToken cancellationToken);

    [HttpPost("{roleId}")]
    Task RemoveAllUsersFromRole(Guid roleId, CancellationToken cancellationToken);

    //#if (notification == true || signalR == true)
    [HttpPost]
    Task SendNotification(SendNotificationToRoleDto dto, CancellationToken cancellationToken);
    //#endif
}
