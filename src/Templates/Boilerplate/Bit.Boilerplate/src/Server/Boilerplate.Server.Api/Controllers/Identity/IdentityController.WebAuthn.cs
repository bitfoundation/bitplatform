//+:cnd:noEmit
using System.Text;
using Boilerplate.Shared.Dtos.Identity;
using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.Extensions.Caching.Distributed;

namespace Boilerplate.Server.Api.Controllers.Identity;

public partial class IdentityController
{
    [AutoInject] private IFido2 fido2 = default!;
    [AutoInject] private IDistributedCache cache = default!;


    [HttpGet]
    public async Task<AssertionOptions> GetWebAuthnAssertionOptions(CancellationToken cancellationToken)
    {
        var existingKeys = new List<PublicKeyCredentialDescriptor>();

        var extensions = new AuthenticationExtensionsClientInputs
        {
            UserVerificationMethod = true,
            Extensions = true
        };

        var options = fido2.GetAssertionOptions(new GetAssertionOptionsParams
        {
            Extensions = extensions,
            AllowedCredentials = existingKeys,
            UserVerification = UserVerificationRequirement.Discouraged,
        });

        var key = new string([.. options.Challenge.Select(b => (char)b)]);
        await cache.SetAsync(key, Encoding.UTF8.GetBytes(options.ToJson()), cancellationToken);

        return options;
    }

    [HttpPost, Produces<SignInResponseDto>()]
    public async Task VerifyWebAuthAssertionAndSignIn(AuthenticatorAssertionRawResponse clientResponse, CancellationToken cancellationToken)
    {
        var response = JsonSerializer.Deserialize<AuthenticatorResponse>(clientResponse.Response.ClientDataJson)
                        ?? throw new InvalidOperationException("Invalid client data.");

        var key = new string([.. response.Challenge.Select(b => (char)b)]);
        var cachedBytes = await cache.GetAsync(key, cancellationToken)
                             ?? throw new InvalidOperationException("no assertion credential options found in the cache.");

        var jsonOptions = Encoding.UTF8.GetString(cachedBytes);
        var options = AssertionOptions.FromJson(jsonOptions);

        await cache.RemoveAsync(key, cancellationToken);

        var credential = (await DbContext.WebAuthnCredential.FirstOrDefaultAsync(c => c.Id == clientResponse.Id, cancellationToken))
                        ?? throw new ResourceNotFoundException("Credential");

        var user = await userManager.FindByIdAsync(credential.UserId.ToString())
                    ?? throw new ResourceNotFoundException("User");

        var verifyResult = await fido2.MakeAssertionAsync(new MakeAssertionParams
        {
            AssertionResponse = clientResponse,
            OriginalOptions = options,
            StoredPublicKey = credential.PublicKey!,
            StoredSignatureCounter = credential.SignCount,
            IsUserHandleOwnerOfCredentialIdCallback = IsUserHandleOwnerOfCredentialId
        }, cancellationToken);

        credential.SignCount = verifyResult.SignCount;

        DbContext.WebAuthnCredential.Update(credential);

        var otp = await userManager.GenerateUserTokenAsync(user, 
                                                           TokenOptions.DefaultPhoneProvider, 
                                                           FormattableString.Invariant($"Otp_WebAuth,{user.OtpRequestedOn?.ToUniversalTime()}"));

        await SignIn(new() { Otp = otp }, user, cancellationToken);
    }

    private async Task<bool> IsUserHandleOwnerOfCredentialId(IsUserHandleOwnerOfCredentialIdParams args, CancellationToken cancellationToken)
    {
        var storedCreds = await DbContext.WebAuthnCredential.Where(c => c.Id == args.UserHandle).ToListAsync(cancellationToken);
        return storedCreds.Exists(c => new PublicKeyCredentialDescriptor(PublicKeyCredentialType.PublicKey, c.Id, c.Transports).Id.SequenceEqual(args.CredentialId));
    }
}
