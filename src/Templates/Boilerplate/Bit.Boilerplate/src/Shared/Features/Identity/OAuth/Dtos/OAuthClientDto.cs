//+:cnd:noEmit
namespace Boilerplate.Shared.Features.Identity.OAuth.Dtos;

/// <summary>How a client became known. Neither kind is stored, so revoking its grants is all an operator can do.</summary>
public enum OAuthClientKind
{
    /// <summary>Self-described at its own https url. Nothing is stored, so it appears only because it holds grants.</summary>
    MetadataDocument = 0,

    /// <summary>Declared under <c>OAuth:Clients</c>. Removed by editing configuration.</summary>
    Configured = 1
}

/// <summary>One external application as an operator needs to see it.</summary>
public partial class OAuthClientDto
{
    public string ClientId { get; set; } = default!;

    /// <summary>Untrusted text the client chose for itself, where one is known.</summary>
    public string? ClientName { get; set; }

    /// <summary>The host it identifies itself by - what an operator should actually read.</summary>
    public string? Origin { get; set; }

    public string[] RedirectUris { get; set; } = [];

    public OAuthClientKind Kind { get; set; }

    /// <summary>Live grants to this client. The number that decides whether revoking is safe.</summary>
    public int ActiveGrants { get; set; }
}
