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

    [HttpGet("{roleId}")]
    Task<List<UserDto>> GetUsers(Guid roleId, CancellationToken cancellationToken) => default!;

    [HttpGet("{roleId}")]
    Task<List<RoleClaimDto>> GetClaims(Guid roleId, CancellationToken cancellationToken) => default!;

    [HttpPost]
    Task<RoleDto> Create(RoleDto roleDto, CancellationToken cancellationToken);

    [HttpPost]
    Task<RoleDto> Update(RoleDto roleDto, CancellationToken cancellationToken);

    [HttpPost]
    Task<List<RoleClaimDto>> AddClaims(List<RoleClaimRequestDto> dtos, CancellationToken cancellationToken);

    [HttpPost]
    Task DeleteClaims(List<RoleClaimRequestDto> dtos, CancellationToken cancellationToken);

    [HttpPost]
    Task<List<RoleClaimDto>> UpdateClaims(List<RoleClaimRequestDto> dtos, CancellationToken cancellationToken);

    [HttpPost]
    Task ToggleUser(ToggleRoleUserDto dto, CancellationToken cancellationToken);

    [HttpPost]
    Task SendNotification(RoleNotificationDto dto, CancellationToken cancellationToken);
}
