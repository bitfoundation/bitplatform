//+:cnd:noEmit
using Boilerplate.Shared.Features.Identity.Dtos;

namespace Boilerplate.Shared.Features.Identity;

[Route("api/[controller]/[action]/"), AuthorizedApi]
public interface IUserManagementController : IAppController
{
    [HttpGet]
    Task<List<UserDto>> GetAllUsers(CancellationToken cancellationToken) => default!;

    [HttpGet]
    Task<int> GetOnlineUsersCount(CancellationToken cancellationToken);

    [HttpGet("{userId}")]
    Task<List<UserSessionDto>> GetUserSessions(Guid userId, CancellationToken cancellationToken) => default!;

    [HttpPost("{userId}")]
    Task Delete(Guid userId, CancellationToken cancellationToken);

    [HttpPost("{id}")]
    Task RevokeUserSession(Guid id, CancellationToken cancellationToken);

    [HttpPost("{userId}")]
    Task RevokeAllUserSessions(Guid userId, CancellationToken cancellationToken);
}
