//+:cnd:noEmit
using System.Text;
using Microsoft.Extensions.Caching.Distributed;
using Fido2NetLib;
using Fido2NetLib.Objects;
using Boilerplate.Server.Api.Models.Identity;

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
                    ?? throw new ResourceNotFoundException();

        var existingCredentials = DbContext.WebAuthnCredential.Where(c => c.UserId == userId);
        var existingKeys = existingCredentials.Select(c => new PublicKeyCredentialDescriptor(PublicKeyCredentialType.PublicKey,
                                                                                             c.Id,
                                                                                             c.Transports));
        var fidoUser = new Fido2User
        {
            Id = Encoding.UTF8.GetBytes(userId.ToString()),
            Name = user.DisplayUserName,
            DisplayName = user.DisplayName,
        };

        //var authenticatorSelection = new AuthenticatorSelection
        //{
        //    ResidentKey = ResidentKeyRequirement.Required,
        //    UserVerification = UserVerificationRequirement.Preferred
        //};

        var authenticatorSelection = new AuthenticatorSelection
        {
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
            Extensions = extensions
        });

        var key = GetWebAuthnCacheKey(userId);
        await cache.SetAsync(key, Encoding.UTF8.GetBytes(options.ToJson()), new() { SlidingExpiration = TimeSpan.FromMinutes(3) }, cancellationToken);

        return options;
    }

    [HttpPut]
    public async Task CreateWebAuthnCredential(AuthenticatorAttestationRawResponse attestationResponse, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var user = await userManager.FindByIdAsync(userId.ToString())
                    ?? throw new ResourceNotFoundException();

        var key = GetWebAuthnCacheKey(userId);
        var cachedBytes = await cache.GetAsync(key, cancellationToken)
                            ?? throw new ResourceNotFoundException();

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

        await cache.RemoveAsync(key, cancellationToken);

        await DbContext.SaveChangesAsync(cancellationToken);
    }

    [HttpDelete]
    public async Task DeleteWebAuthnCredential(byte[] credentialId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var user = await userManager.FindByIdAsync(userId.ToString())
                    ?? throw new ResourceNotFoundException();

        var entityToDelete = await DbContext.WebAuthnCredential.FindAsync([credentialId], cancellationToken)
                                ?? throw new ResourceNotFoundException();

        DbContext.WebAuthnCredential.Remove(entityToDelete);

        await DbContext.SaveChangesAsync(cancellationToken);
    }

    [HttpDelete]
    public async Task DeleteAllWebAuthnCredentials(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var user = await userManager.FindByIdAsync(userId.ToString())
                    ?? throw new ResourceNotFoundException();

        var entities = await DbContext.WebAuthnCredential.Where(c => c.UserId == userId).ToListAsync(cancellationToken);

        if (entities is null || entities.Count == 0) return;

        DbContext.WebAuthnCredential.RemoveRange(entities);

        await DbContext.SaveChangesAsync(cancellationToken);
    }

    private static string GetWebAuthnCacheKey(Guid userId) => $"WebAuthn_Options_{userId}";

    private async Task<bool> IsCredentialIdUniqueToUser(IsCredentialIdUniqueToUserParams args, CancellationToken cancellationToken)
    {
        var count = await DbContext.WebAuthnCredential.CountAsync(c => c.Id == args.CredentialId, cancellationToken);
        return count <= 0;
    }
}
