//+:cnd:noEmit
using Boilerplate.Shared.Dtos.Identity;

namespace Boilerplate.Shared.Controllers.Identity;

[Route("api/[controller]/[action]/")]
public interface IRoleController : IAppController
{
    [HttpGet]
    Task<List<RoleDto>> GetAllRoles(CancellationToken cancellationToken) => default!;

    [HttpGet]
    Task<List<UserDto>> GetAllUsers(CancellationToken cancellationToken) => default!;

    [HttpGet]
    Task<List<UserDto>> GetUsers(Guid roleId, CancellationToken cancellationToken) => default!;

    [HttpGet]
    Task<List<RoleClaimDto>> GetClaims(Guid roleId, CancellationToken cancellationToken) => default!;

    [HttpPost]
    Task<RoleDto> Create(RoleDto roleDto, CancellationToken cancellationToken);

    [HttpPost]
    Task<RoleDto> Update(RoleDto roleDto, CancellationToken cancellationToken);

    [HttpPost]
    Task SaveMaxPrivilegedSessions(int value, CancellationToken cancellationToken);

    [HttpPost]
    Task SendNotification(RoleNotificationDto dto, CancellationToken cancellationToken);
}
