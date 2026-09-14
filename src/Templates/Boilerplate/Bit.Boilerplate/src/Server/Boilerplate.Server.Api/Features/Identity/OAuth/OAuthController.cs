//+:cnd:noEmit
using Boilerplate.Shared.Features.Identity.OAuth;
using Boilerplate.Shared.Features.Identity.OAuth.Dtos;
using Boilerplate.Server.Api.Features.Identity.OAuth.Services;

namespace Boilerplate.Server.Api.Features.Identity.OAuth;

/// <summary>
/// What the consent page calls, as the signed-in user and on the app's ordinary access token - which is what lets the
/// consent screen be a normal page and inherit every existing way of signing in.
/// </summary>
[ApiVersion(1)]
[ApiController]
[Route("api/v{v:apiVersion}/[controller]/[action]")]
public partial class OAuthController : AppControllerBase, IOAuthController
{
    [AutoInject] private OAuthService oauthService = default!;
    [AutoInject] private IAuthorizationService authorizationService = default!;

    /// <summary>Says what would be granted, without granting it.</summary>
    [HttpPost]
    public async Task<OAuthConsentDto> Review(OAuthAuthorizeRequestDto request, CancellationToken cancellationToken)
    {
        var (validated, grantedScopes, deniedScopes) = await ValidateForUser(request, cancellationToken);

        var client = validated.Client!;

        return new OAuthConsentDto
        {
            ClientName = client.ClientName,
            ClientOrigin = OAuthClient.OriginOf(client.ClientId),
            RedirectUri = request.RedirectUri,
            IsPreRegistered = client.IsPreRegistered,
            Resource = validated.Resource,
            Scopes = grantedScopes,
            DeniedScopes = deniedScopes,
            //#if (multitenant == true)
            // Resolved, not read: TenantNameOf queries by id alone, so the request's own value would let anyone learn a
            // workspace's name by putting its id in the query string.
            TenantName = await TenantNameOf(await ResolveTenantId(request.TenantId, cancellationToken), cancellationToken)
            //#endif
        };
    }

    [HttpPost]
    public async Task<OAuthApprovalDto> Approve(OAuthAuthorizeRequestDto request, CancellationToken cancellationToken)
    {
        var (validated, grantedScopes, _) = await ValidateForUser(request, cancellationToken);

        Guid? tenantId = null;
        //#if (multitenant == true)
        tenantId = await ResolveTenantId(request.TenantId, cancellationToken);
        //#endif

        var redirectUrl = await oauthService.IssueCode(validated, request, grantedScopes, User, tenantId, cancellationToken);

        return new OAuthApprovalDto { RedirectUrl = redirectUrl };
    }

    [HttpPost]
    public async Task<OAuthApprovalDto> Deny(OAuthAuthorizeRequestDto request, CancellationToken cancellationToken)
    {
        var validated = await oauthService.Validate(request, cancellationToken);

        // A refusal still goes back to a client whose redirect uri checked out, or it waits on a tab that never returns.
        if (validated.CanRedirect is false)
            throw new BadRequestException(Localizer[nameof(AppStrings.OAuthErrorClientNotIdentified)]);

        return new OAuthApprovalDto
        {
            RedirectUrl = oauthService.BuildErrorRedirectUrl(request.RedirectUri!, "access_denied", request.State)
        };
    }

    /// <summary>
    /// Re-validates on every call: the consent page holds the request in a query string the browser can edit, so scopes
    /// are recomputed from current claims rather than trusted from what Review returned.
    /// </summary>
    private async Task<(OAuthValidationResult Validated, string[] Granted, string[] Denied)> ValidateForUser(
        OAuthAuthorizeRequestDto request, CancellationToken cancellationToken)
    {
        var validated = await oauthService.Validate(request, cancellationToken);

        if (validated.IsValid is false)
            throw new BadRequestException(Localizer[nameof(AppStrings.OAuthErrorInvalidRequestWithReason), validated.Error!]);

        var (granted, denied) = OAuthService.NarrowScopes(validated.Scopes, User);

        if (granted is { Length: 0 })
            throw new BadRequestException(Localizer[nameof(AppStrings.OAuthErrorInsufficientPermissions)]);

        // The resource endpoint's own policies, against the consenting session: a grant carries only what that session
        // has - its amr, for one - so failing them here would otherwise mean a token the resource silently 403s.
        foreach (var policy in OAuthResources.PoliciesFor(validated.Resource))
        {
            if ((await authorizationService.AuthorizeAsync(User, policy)).Succeeded)
                continue;

            throw new BadRequestException(policy is AuthPolicies.TFA_ENABLED
                ? Localizer[nameof(AppStrings.OAuthErrorTwoFactorRequired)]
                : Localizer[nameof(AppStrings.OAuthErrorInsufficientPermissions)]);
        }

        return (validated, granted, denied);
    }

    //#if (multitenant == true)
    /// <summary>
    /// A tenant is accepted only for an accepted member of an active one; a request naming none gets no tenant claim
    /// rather than being quietly placed in one.
    /// </summary>
    private async Task<Guid?> ResolveTenantId(string? requestedTenantId, CancellationToken cancellationToken)
    {
        // A malformed one never gets here - OAuthService.Validate turns it into invalid_request first.
        if (Guid.TryParse(requestedTenantId, out var tenantId) is false)
            return null;

        var userId = User.GetUserId();

        var isMember = await DbContext.TenantUsers
            .AnyAsync(tu => tu.UserId == userId && tu.TenantId == tenantId && tu.AcceptedOn != null && tu.Tenant!.IsActive, cancellationToken);

        if (isMember is false)
            throw new BadRequestException(Localizer[nameof(AppStrings.OAuthErrorNotAMemberOfWorkspace)]);

        return tenantId;
    }

    private async Task<string?> TenantNameOf(Guid? tenantId, CancellationToken cancellationToken)
    {
        if (tenantId is null)
            return null;

        return await DbContext.Tenants.Where(tenant => tenant.Id == tenantId)
                                      .Select(tenant => tenant.Name)
                                      .FirstOrDefaultAsync(cancellationToken);
    }
    //#endif
}
