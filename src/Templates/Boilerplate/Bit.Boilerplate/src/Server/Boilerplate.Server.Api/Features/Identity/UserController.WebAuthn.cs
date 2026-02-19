//+:cnd:noEmit
using System.Text;
using Fido2NetLib;
using Fido2NetLib.Objects;
using Boilerplate.Server.Api.Features.Identity.Models;

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
                    ?? throw new ResourceNotFoundException();

        var existingCredentials = DbContext.WebAuthnCredential.Where(c => c.UserId == userId);
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

    [HttpPut]
    public async Task CreateWebAuthnCredential(AuthenticatorAttestationRawResponse attestationResponse, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var user = await userManager.FindByIdAsync(userId.ToString())
                    ?? throw new ResourceNotFoundException();

        var key = GetWebAuthnCacheKey(userId);
        var options = await cache.GetOrSetAsync<CredentialCreateOptions>(key,
            async _ => throw new ResourceNotFoundException(),
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
            RegDate = DateTimeOffset.UtcNow,
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
        var user = await userManager.FindByIdAsync(userId.ToString())
                    ?? throw new ResourceNotFoundException();

        var affectedRows = await DbContext.WebAuthnCredential
            .Where(webAuthCred => webAuthCred.Id == assertionResponse.RawId)
            .ExecuteDeleteAsync(cancellationToken);

        if (affectedRows == 0)
            throw new ResourceNotFoundException();
    }

    private static string GetWebAuthnCacheKey(Guid userId) => $"WebAuthn_Options_{userId}";

    private async Task<bool> IsCredentialIdUniqueToUser(IsCredentialIdUniqueToUserParams args, CancellationToken cancellationToken)
    {
        var count = await DbContext.WebAuthnCredential.CountAsync(c => c.Id == args.CredentialId, cancellationToken);
        return count <= 0;
    }
}
