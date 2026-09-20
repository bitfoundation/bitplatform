//+:cnd:noEmit
using Boilerplate.Shared.Features.Identity.OAuth;
using Boilerplate.Shared.Features.Identity.OAuth.Dtos;
using Boilerplate.Server.Api.Features.Identity.OAuth.Services;

namespace Boilerplate.Server.Api.Features.Identity.OAuth;

/// <summary>
/// Not a grid over a table: no client is stored anywhere - a modern one describes itself at its own url, and the rest
/// are configuration. The list is assembled from what is configured and the grants that exist.
/// </summary>
[ApiVersion(1)]
[ApiController, Route("api/v{v:apiVersion}/[controller]/[action]")]
[Authorize(Policy = AuthPolicies.PRIVILEGED_ACCESS),
    Authorize(Policy = AppFeatures.System.OAuthClients_Manage)]
public partial class OAuthClientManagementController : AppControllerBase, IOAuthClientManagementController
{
    [HttpGet]
    public async Task<List<OAuthClientDto>> GetAllClients(CancellationToken cancellationToken)
    {
        // Grants first: the only source that sees every kind of client, and the count an operator acts on.
        var grantsByClient = await DbContext.OAuthGrants
            .GroupBy(grant => grant.ClientId)
            .Select(group => new { ClientId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.ClientId, item => item.Count, cancellationToken);

        List<OAuthClientDto> clients = [];

        foreach (var configured in AppSettings.OAuth.Clients)
        {
            clients.Add(new OAuthClientDto
            {
                ClientId = configured.ClientId,
                ClientName = configured.ClientName ?? configured.ClientId,
                Origin = OAuthClient.OriginOf(configured.ClientId),
                RedirectUris = configured.RedirectUris ?? [],
                Kind = OAuthClientKind.Configured
            });
        }

        // A grant that is not configured belongs to a metadata-document client. Its name is not fetched: that is an
        // outbound request per row, to hosts we do not control, on every page load.
        foreach (var clientId in grantsByClient.Keys.Except(clients.Select(client => client.ClientId), StringComparer.Ordinal))
        {
            var origin = OAuthClient.OriginOf(clientId);

            clients.Add(new OAuthClientDto
            {
                ClientId = clientId,
                ClientName = origin,
                Origin = origin,
                Kind = OAuthClientKind.MetadataDocument
            });
        }

        foreach (var client in clients)
        {
            client.ActiveGrants = grantsByClient.GetValueOrDefault(client.ClientId);
        }

        return [.. clients.OrderByDescending(client => client.ActiveGrants)
                          .ThenBy(client => client.ClientName, StringComparer.OrdinalIgnoreCase)];
    }

    [HttpPost]
    public async Task<int> RevokeGrants(RevokeOAuthClientRequestDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ClientId))
            throw new BadRequestException();

        // First, or a code minted moments ago is still exchangeable for a brand new session right after this returns.
        await DbContext.OAuthAuthorizationCodes
            .Where(code => code.ClientId == request.ClientId)
            .ExecuteDeleteAsync(cancellationToken);

        // What actually ends access: a refresh whose session is gone is refused, and access tokens expire in minutes.
        // The session is deleted, not the grant - the grant cascades with it, and it is the session that holds a token.
        // Through the grants, not session.OAuthGrant: a delete whose predicate walks a navigation becomes a subquery
        // over the table being deleted from, which MySQL refuses. OAuthGrants is a different table, and staying a
        // queryable avoids the collection parameter its provider cannot map either.
        var grantedSessionIds = DbContext.OAuthGrants
            .Where(grant => grant.ClientId == request.ClientId)
            .Select(grant => grant.UserSessionId);

        var revoked = await DbContext.UserSessions
            .Where(session => grantedSessionIds.Contains(session.Id))
            .ExecuteDeleteAsync(cancellationToken);

        return revoked;
    }
}
