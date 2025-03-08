//+:cnd:noEmit
using System.Text;
using Boilerplate.Server.Api.Models.Identity;
using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.Extensions.Caching.Distributed;

namespace Boilerplate.Server.Api.Controllers.Identity;

public partial class UserController
{
    [AutoInject] private IFido2 fido2 = default!;
    [AutoInject] private IDistributedCache cache = default!;


    [HttpGet]
    public async Task<CredentialCreateOptions> GetWebAuthnCredentialOptions(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var user = await userManager.FindByIdAsync(userId.ToString())
                    ?? throw new ResourceNotFoundException("User");

        var existingCredentials = DbContext.WebAuthnCredential.Where(c => c.UserId == user.Id);
        var existingKeys = existingCredentials.Select(c => new PublicKeyCredentialDescriptor(PublicKeyCredentialType.PublicKey,
                                                                                             c.Id,
                                                                                             c.Transports));
        var fidoUser = new Fido2User
        {
            Id = Encoding.UTF8.GetBytes(user.Id.ToString()),
            Name = user.Email ?? user.PhoneNumber ?? user.UserName,
            DisplayName = user.DisplayName
        };

        var options = fido2.RequestNewCredential(new RequestNewCredentialParams
        {
            User = fidoUser,
            ExcludeCredentials = [.. existingKeys],
            AuthenticatorSelection = AuthenticatorSelection.Default,
            AttestationPreference = AttestationConveyancePreference.None,
            Extensions = new AuthenticationExtensionsClientInputs
            {
                CredProps = true,
                Extensions = true,
                UserVerificationMethod = true,
            }
        });

        var key = GetWebAuthnKey(user.Id);
        await cache.SetAsync(key, Encoding.UTF8.GetBytes(options.ToJson()), cancellationToken);

        return options;
    }

    [HttpPut]
    public async Task CreateWebAuthnCredential(AuthenticatorAttestationRawResponse attestationResponse, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var user = await userManager.FindByIdAsync(userId.ToString())
                    ?? throw new ResourceNotFoundException("User");

        var key = GetWebAuthnKey(user.Id);
        var cachedBytes = await cache.GetAsync(key, cancellationToken)
                            ?? throw new InvalidOperationException("no create credential options found in the cache.");

        var jsonOptions = Encoding.UTF8.GetString(cachedBytes);
        var options = CredentialCreateOptions.FromJson(jsonOptions);

        var makeCredentialParams = new MakeNewCredentialParams
        {
            AttestationResponse = attestationResponse,
            OriginalOptions = options,
            IsCredentialIdUniqueToUserCallback = IsCredentialIdUniqueToUser
        };

        var credential = await fido2.MakeNewCredentialAsync(makeCredentialParams, cancellationToken);

        var newCredential = new WebAuthnCredential
        {
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

        await cache.RemoveAsync(key, cancellationToken);

        await DbContext.SaveChangesAsync(cancellationToken);
    }


    private static string GetWebAuthnKey(Guid userId) => $"WebAuthn_Options_{userId}";

    private async Task<bool> IsCredentialIdUniqueToUser(IsCredentialIdUniqueToUserParams args, CancellationToken cancellationToken)
    {
        var count = await DbContext.WebAuthnCredential.CountAsync(c => c.Id == args.CredentialId, cancellationToken);
        return count <= 0;
    }
}
