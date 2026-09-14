//+:cnd:noEmit
using Boilerplate.Shared.Features.Identity.OAuth.Dtos;

namespace Boilerplate.Shared.Features.Identity.OAuth;

/// <summary>
/// What an operator can do about authorized applications. Global admins only - a client is registered against the
/// deployment, not a tenant.
/// </summary>
[Route("api/v1/[controller]/[action]/"), AuthorizedApi]
public interface IOAuthClientManagementController : IAppController
{
    /// <summary>Every client this deployment knows of, from both registration routes, with its grant count.</summary>
    [HttpGet]
    Task<List<OAuthClientDto>> GetAllClients(CancellationToken cancellationToken) => default!;

    /// <summary>Revokes every user's grant, so its tokens stop working. It may be authorized again; this is not a ban.</summary>
    [HttpPost]
    Task<int> RevokeGrants(RevokeOAuthClientRequestDto request, CancellationToken cancellationToken) => default!;
}
