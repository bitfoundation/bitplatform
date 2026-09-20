//+:cnd:noEmit
namespace Boilerplate.Shared.Features.Identity.OAuth.Dtos;

/// <summary>
/// What the consent screen shows. Everything here is server-resolved: presenting a client name the browser supplied
/// would make the request itself the phishing vector.
/// </summary>
public partial class OAuthConsentDto
{
    /// <summary>Untrusted text from a third party. Render as text, never as markup.</summary>
    public string? ClientName { get; set; }

    /// <summary>The host serving the client's metadata document - the part it cannot forge.</summary>
    public string? ClientOrigin { get; set; }

    /// <summary>Shown because a loopback address means "an app on this machine" and a remote one does not.</summary>
    public string? RedirectUri { get; set; }

    /// <summary>Whether an operator declared this client, rather than it describing itself.</summary>
    public bool IsPreRegistered { get; set; }

    public string? Resource { get; set; }

    /// <summary>What the token will really carry: requested scopes narrowed to the user's own features.</summary>
    public string[] Scopes { get; set; } = [];

    /// <summary>Scopes the user does not hold. The grant proceeds with what is left, unless nothing is.</summary>
    public string[] DeniedScopes { get; set; } = [];

    //#if (multitenant == true)
    public string? TenantName { get; set; }
    //#endif
}
