//+:cnd:noEmit
using Boilerplate.Server.Api.Features.Identity.OAuth.Services;

namespace Boilerplate.Server.Api.Features.Identity.OAuth.Models;

/// <summary>
/// The OAuth half of a <see cref="UserSession"/>. Its own table so the sessions table stays untouched, and so the
/// management page and revocation read grants rather than scanning every sign-in.
/// </summary>
public partial class OAuthGrant
{
    /// <summary>Also the primary key: a grant neither outlives its session nor is looked up by anything else.</summary>
    public Guid UserSessionId { get; set; }

    public UserSession? UserSession { get; set; }

    /// <summary>An https url to the client's metadata document, or a configured client id.</summary>
    [MaxLength(OAuthClientResolver.MaxClientIdLength)]
    public string ClientId { get; set; } = default!;

    /// <summary>Untrusted text the client chose for itself. Never render as markup.</summary>
    [MaxLength(256)]
    public string? ClientName { get; set; }

    /// <summary>Space delimited, as granted. The only durable record: the code is gone and the token is the client's.</summary>
    [MaxLength(512)]
    public string Scope { get; set; } = default!;

    /// <summary>RFC 8707 resource identifier; the <c>aud</c> of the tokens issued for this grant.</summary>
    [MaxLength(2048)]
    public string Resource { get; set; } = default!;

    /// <summary>The one refresh token currently valid here; any other id is a copy somebody kept.</summary>
    public Guid RefreshTokenId { get; set; }
}
