//+:cnd:noEmit
using Fido2NetLib;
using Fido2NetLib.Objects;

namespace Boilerplate.Server.Api.Features.Identity;

public partial class UserController
{
    [AutoInject] private IFido2 fido2 = default!;
    [AutoInject] private IFusionCache cache = default!;


    [HttpGet]
    public async Task<CredentialCreateOptions> GetWebAuthnCredentialOptions(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var user = await userManager.FindByIdAsync(userId.ToString())
                    ?? throw new ResourceNotFoundException().WithData("Reason", "User not found.");

        var existingCredentials = await DbContext.WebAuthnCredential.Where(c => c.UserId == userId)
                                                                    .Select(c => new { c.Id, c.Transports })
                                                                    .ToArrayAsync(cancellationToken);
        var existingKeys = existingCredentials.Select(c => new PublicKeyCredentialDescriptor(PublicKeyCredentialType.PublicKey, c.Id, c.Transports));
        var fidoUser = new Fido2User
        {
            Id = Encoding.UTF8.GetBytes(userId.ToString()),
            Name = user.DisplayUserName,
            DisplayName = user.DisplayName,
        };

        var authenticatorSelection = new AuthenticatorSelection
        {
            RequireResidentKey = false,
            ResidentKey = ResidentKeyRequirement.Discouraged,
            UserVerification = UserVerificationRequirement.Required,
            AuthenticatorAttachment = AuthenticatorAttachment.Platform
        };

        var extensions = new AuthenticationExtensionsClientInputs
        {
            CredProps = true,
            Extensions = true,
            UserVerificationMethod = true,
        };

        var options = fido2.RequestNewCredential(new RequestNewCredentialParams
        {
            User = fidoUser,
            // Deliberately EMPTY. A user is expected to enrol a passkey on several devices, and existingKeys holds
            // the credentials from ALL of them - passing it would make the authenticator answer InvalidStateError
            // whenever this device already holds one, permanently blocking re-enrolment for anyone whose local
            // flag was cleared (reinstall, cleared storage) while the server row survived.
            ExcludeCredentials = [], //[.. existingKeys],
            AuthenticatorSelection = authenticatorSelection,
            AttestationPreference = AttestationConveyancePreference.None,
            //Extensions = extensions
        });

        var key = GetWebAuthnCacheKey(userId);
        await cache.SetAsync(key, options,
            options => options.Duration = TimeSpan.FromMinutes(3),
            cancellationToken);

        return options;
    }

    /// <summary>
    /// Enrolling a passkey adds a NEW way to sign in, so it belongs with the other account-factor changes behind elevated
    /// access (compare <see cref="Delete"/>, <see cref="ChangeUserName"/> and <see cref="RevokeSession"/>). Without this,
    /// an access token stolen for a few minutes buys a credential that survives a password change AND revoking every
    /// session, because nothing on those paths touches WebAuthnCredential - and the account owner has no UI that lists it.
    /// </summary>
    [HttpPut, Authorize(Policy = AuthPolicies.ELEVATED_ACCESS)]
    public async Task CreateWebAuthnCredential(AuthenticatorAttestationRawResponse attestationResponse, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var user = await userManager.FindByIdAsync(userId.ToString())
                    ?? throw new ResourceNotFoundException().WithData("Reason", "User not found.");

        var key = GetWebAuthnCacheKey(userId);
        var options = await cache.GetOrSetAsync<CredentialCreateOptions>(key,
            async _ => throw new ResourceNotFoundException().WithData("Reason", "WebAuthn credential options not found."),
            token: cancellationToken);


        var makeCredentialParams = new MakeNewCredentialParams
        {
            AttestationResponse = attestationResponse,
            OriginalOptions = options,
            IsCredentialIdUniqueToUserCallback = IsCredentialIdUniqueToUser
        };

        var credential = await fido2.MakeNewCredentialAsync(makeCredentialParams, cancellationToken);

        var newCredential = new WebAuthnCredential
        {
            UserId = userId,
            Id = credential.Id,
            PublicKey = credential.PublicKey,
            UserHandle = credential.User.Id,
            SignCount = credential.SignCount,
            RegDate = TimeProvider.GetUtcNow(),
            AaGuid = credential.AaGuid,
            Transports = credential.Transports,
            AttestationFormat = credential.AttestationFormat,
            IsBackupEligible = credential.IsBackupEligible,
            IsBackedUp = credential.IsBackedUp,
            AttestationObject = credential.AttestationObject,
            AttestationClientDataJson = credential.AttestationClientDataJson,
        };

        await DbContext.WebAuthnCredential.AddAsync(newCredential, cancellationToken);

        await cache.RemoveAsync(key, token: cancellationToken);

        await DbContext.SaveChangesAsync(cancellationToken);
    }

    [HttpDelete]
    public async Task DeleteWebAuthnCredential(AuthenticatorAssertionRawResponse assertionResponse, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var affectedRows = await DbContext.WebAuthnCredential
            .Where(webAuthCred => webAuthCred.Id == assertionResponse.RawId && webAuthCred.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);

        if (affectedRows == 0)
            throw new ResourceNotFoundException().WithData("Reason", "WebAuthn credential not found.");
    }

    private static string GetWebAuthnCacheKey(Guid userId) => $"WebAuthn_Options_{userId}";

    private async Task<bool> IsCredentialIdUniqueToUser(IsCredentialIdUniqueToUserParams args, CancellationToken cancellationToken)
    {
        var count = await DbContext.WebAuthnCredential.CountAsync(c => c.Id == args.CredentialId, cancellationToken);
        return count <= 0;
    }
}
