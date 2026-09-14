//+:cnd:noEmit
using Boilerplate.Shared.Features.Identity.OAuth.Dtos;

namespace Boilerplate.Shared.Features.Identity.OAuth;

/// <summary>
/// What the consent page calls, as ordinary authenticated api calls carrying the app's own access token - which is
/// what lets consent be a normal Blazor page and inherit every sign-in method the app has. The protocol endpoints a
/// client talks to are not here: they are anonymous and speak form encoding, not json.
/// </summary>
[Route("api/v1/[controller]/[action]/")]
public interface IOAuthController : IAppController
{
    /// <summary>What would be granted, without granting it. A POST because the request is a dozen fields wide.</summary>
    [HttpPost]
    Task<OAuthConsentDto> Review(OAuthAuthorizeRequestDto request, CancellationToken cancellationToken) => default!;

    /// <summary>Issues the authorization code and returns where to send the browser.</summary>
    [HttpPost]
    Task<OAuthApprovalDto> Approve(OAuthAuthorizeRequestDto request, CancellationToken cancellationToken) => default!;

    /// <summary>
    /// Returns the redirect carrying <c>error=access_denied</c>: a client left on a tab that simply closed cannot tell
    /// refusal from a crash.
    /// </summary>
    [HttpPost]
    Task<OAuthApprovalDto> Deny(OAuthAuthorizeRequestDto request, CancellationToken cancellationToken) => default!;
}
