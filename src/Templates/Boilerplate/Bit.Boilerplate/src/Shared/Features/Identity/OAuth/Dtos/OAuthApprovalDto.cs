//+:cnd:noEmit
namespace Boilerplate.Shared.Features.Identity.OAuth.Dtos;

/// <summary>The one url the browser should now leave for.</summary>
public partial class OAuthApprovalDto
{
    /// <summary>
    /// Built server-side from the redirect uri already validated against the client's registration, so the page can
    /// navigate to it without validating anything itself.
    /// </summary>
    public string RedirectUrl { get; set; } = default!;
}
