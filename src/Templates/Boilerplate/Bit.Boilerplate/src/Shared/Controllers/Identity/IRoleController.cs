//+:cnd:noEmit
using Boilerplate.Shared.Dtos.Identity;

namespace Boilerplate.Shared.Controllers.Identity;

[Route("api/[controller]/[action]/")]
public interface IRoleController : IAppController
{
    [HttpGet]
    Task<List<RoleDto>> GetAllRoles(CancellationToken cancellationToken);

    [HttpGet]
    Task<List<UserDto>> GetAllUsers(CancellationToken cancellationToken);

    [HttpGet]
    Task<List<RoleClaimDto>> GetClaims(Guid roleId, CancellationToken cancellationToken);

    [HttpGet]
    Task<List<UserDto>> GetUsers(RoleDto roleDto, CancellationToken cancellationToken);

    [HttpPost]
    Task<RoleDto> Create(RoleDto roleDto, CancellationToken cancellationToken);

    [HttpPost]
    Task<RoleDto> Update(RoleDto roleDto, CancellationToken cancellationToken);

    [HttpPost]
    Task SaveMaxPrivilegedSessions(CancellationToken cancellationToken);
}
