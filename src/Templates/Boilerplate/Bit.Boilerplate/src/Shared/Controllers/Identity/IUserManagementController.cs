//+:cnd:noEmit
using Boilerplate.Shared.Dtos.Identity;

namespace Boilerplate.Shared.Controllers.Identity;

[Route("api/[controller]/[action]/"), AuthorizedApi]
public interface IUserManagementController : IAppController
{
    [HttpGet]
    Task<List<UserDto>> GetAllUsers(CancellationToken cancellationToken) => default!;

    [HttpGet("{userId}")]
    Task<List<UserSessionDto>> GetUserSessions(Guid userId, CancellationToken cancellationToken) => default!;

    [HttpPost("{id}")]
    Task DeleteUserSession(Guid id, CancellationToken cancellationToken);
}
