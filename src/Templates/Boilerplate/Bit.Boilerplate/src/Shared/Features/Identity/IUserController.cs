//+:cnd:noEmit
using Boilerplate.Shared.Features.Identity.Dtos;
//#if (multitenant == true)
using Boilerplate.Shared.Features.Tenants.Dtos;
//#endif

namespace Boilerplate.Shared.Features.Identity;

[Route("api/v1/[controller]/[action]/"), AuthorizedApi]
public interface IUserController : IAppController
{
    [HttpGet]
    Task<UserDto> GetCurrentUser(CancellationToken cancellationToken);

    [HttpGet]
    Task<List<UserSessionDto>> GetUserSessions(CancellationToken cancellationToken) => default!;

    [HttpPost, NoRetryPolicy]
    Task SignOut(CancellationToken cancellationToken);

    [HttpPost("{id}")]
    Task RevokeSession(Guid id, CancellationToken cancellationToken);

    [HttpPost]
    Task UpdateSession(UpdateUserSessionRequestDto request, CancellationToken cancellationToken);

    [HttpPut]
    Task<UserDto> Update(EditUserRequestDto userDto, CancellationToken cancellationToken);

    [HttpPost]
    Task ChangePassword(ChangePasswordRequestDto request, CancellationToken cancellationToken);

    [HttpPost]
    Task ChangeUserName(ChangeUserNameRequestDto request, CancellationToken cancellationToken);

    [HttpPost]
    Task SendChangeEmailToken(SendEmailTokenRequestDto request, CancellationToken cancellationToken);

    [HttpPost]
    Task ChangeEmail(ChangeEmailRequestDto request, CancellationToken cancellationToken);

    [HttpPost]
    Task SendChangePhoneNumberToken(SendPhoneTokenRequestDto request, CancellationToken cancellationToken);

    [HttpPost]
    Task ChangePhoneNumber(ChangePhoneNumberRequestDto request, CancellationToken cancellationToken);

    [HttpDelete]
    Task Delete(CancellationToken cancellationToken);

    [HttpPost]
    [Route("~/api/v1/[controller]/2fa")]
    Task<TwoFactorAuthResponseDto> TwoFactorAuth(TwoFactorAuthRequestDto request, CancellationToken cancellationToken) => default!;

    [HttpPost]
    Task SendElevatedAccessToken(CancellationToken cancellationToken);

    [HttpGet]
    Task<JsonElement> GetWebAuthnCredentialOptions(CancellationToken cancellationToken) => default!;

    [HttpPut]
    Task CreateWebAuthnCredential(JsonElement attestationResponse, CancellationToken cancellationToken) => default!;

    [HttpDelete]
    Task DeleteWebAuthnCredential(JsonElement clientResponse, CancellationToken cancellationToken) => default!;

    //#if (signalR == true || notification == true)
    [HttpPost("{userSessionId}")]
    Task<UserSessionNotificationStatus> ToggleNotification(Guid userSessionId, CancellationToken cancellationToken);
    //#endif

    //#if (multitenant == true)
    /// <summary>
    /// Returns the active tenants the user can switch into.
    /// Returns all active tenants if the user has the <see cref="AppFeatures.Management.Tenants_Manage_Global"/> feature.
    /// </summary>
    [HttpGet]
    Task<List<TenantDto>> GetTenants(CancellationToken cancellationToken);

    /// <summary>
    /// Leaves the tenant the user is currently signed into by clearing TenantUser's AcceptedOn.
    /// </summary>
    [HttpPost("{tenantId}")]
    Task LeaveTenant(Guid tenantId, CancellationToken cancellationToken);
    //#endif
}
