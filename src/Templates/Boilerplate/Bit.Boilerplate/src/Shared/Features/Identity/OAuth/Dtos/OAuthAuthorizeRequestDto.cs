//+:cnd:noEmit
namespace Boilerplate.Shared.Features.Identity.OAuth.Dtos;

/// <summary>
/// An OAuth 2.1 authorization request, carried unchanged from /oauth/authorize through the consent page and back to
/// Approve. Property names mirror the wire names because the browser arrives with them in the query string.
/// </summary>
[DtoResourceType(typeof(AppStrings))]
public partial class OAuthAuthorizeRequestDto
{
    /// <summary>An https url to a Client ID Metadata Document, or a pre-registered client id.</summary>
    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    public string? ClientId { get; set; }

    /// <summary>Never redirected to before it is matched against the client's registration.</summary>
    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    public string? RedirectUri { get; set; }

    /// <summary>Only <c>code</c> exists in OAuth 2.1.</summary>
    public string? ResponseType { get; set; }

    /// <summary>Space delimited; absent means "whatever the resource is for".</summary>
    public string? Scope { get; set; }

    /// <summary>Opaque to us, echoed back on the redirect.</summary>
    public string? State { get; set; }

    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    public string? CodeChallenge { get; set; }

    /// <summary>Must be <c>S256</c>; <c>plain</c> is refused rather than downgraded.</summary>
    public string? CodeChallengeMethod { get; set; }

    /// <summary>RFC 8707. Becomes the token's <c>aud</c>, which is what keeps it out of the rest of the api.</summary>
    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    public string? Resource { get; set; }

    //#if (multitenant == true)
    /// <summary>Optional; the user must be an accepted member of that active tenant. Absent means no tenant claim.</summary>
    public Guid? TenantId { get; set; }
    //#endif
}
