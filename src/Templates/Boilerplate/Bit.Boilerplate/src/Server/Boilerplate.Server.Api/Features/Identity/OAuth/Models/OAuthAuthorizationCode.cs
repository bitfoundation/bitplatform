//+:cnd:noEmit
//#if (multitenant == true)
using Boilerplate.Server.Api.Features.Tenants;
//#endif
using Boilerplate.Server.Api.Features.Identity.OAuth.Services;

namespace Boilerplate.Server.Api.Features.Identity.OAuth.Models;

/// <summary>
/// An issued authorization code - the only server state the OAuth flow keeps, because single use cannot be proven from
/// the code alone.
/// </summary>
public partial class OAuthAuthorizationCode
{
    public Guid Id { get; set; }

    /// <summary>SHA-256 of the code, base64url. The code itself is never stored.</summary>
    [MaxLength(64)]
    public string CodeHash { get; set; } = default!;

    [MaxLength(OAuthClientResolver.MaxClientIdLength)]
    public string ClientId { get; set; } = default!;

    /// <summary>Bound here, and the token request must present the same one.</summary>
    [MaxLength(2048)]
    public string RedirectUri { get; set; } = default!;

    /// <summary>RFC 8707 resource identifier; becomes the issued token's <c>aud</c>.</summary>
    [MaxLength(2048)]
    public string Resource { get; set; } = default!;

    /// <summary>Space delimited, already narrowed to what the user holds.</summary>
    [MaxLength(512)]
    public string Scope { get; set; } = default!;

    [MaxLength(128)]
    public string CodeChallenge { get; set; } = default!;

    public Guid UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    /// <summary>Authentication methods of the granting session; /dev-mcp asks for <see cref="AuthPolicies.TFA_ENABLED"/>.</summary>
    [MaxLength(128)]
    public string? Amr { get; set; }

    //#if (multitenant == true)
    /// <summary>Null when the request named no tenant, and then the token carries no tenant claim either.</summary>
    public Guid? TenantId { get; set; }

    [ForeignKey(nameof(TenantId))]
    public Tenant? Tenant { get; set; }
    //#endif

    /// <summary>Unix time seconds.</summary>
    public long ExpiresOn { get; set; }

    /// <summary>Unix time seconds, set by the first exchange; a code travels once, so a second exchange is theft.</summary>
    public long? ConsumedOn { get; set; }

    /// <summary>The session the first exchange created, so a replay has something to revoke.</summary>
    public Guid? UserSessionId { get; set; }

    /// <summary>
    /// Concurrency token (<c>AppDbContext.ConfigureConcurrencyToken</c>). Consuming a code is read-check-write, so
    /// without it two exchanges racing each other both pass the check and both mint a session.
    /// </summary>
    public long Version { get; set; }
}
