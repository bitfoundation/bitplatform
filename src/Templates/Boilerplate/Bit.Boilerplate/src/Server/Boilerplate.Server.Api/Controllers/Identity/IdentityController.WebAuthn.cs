//+:cnd:noEmit
using System.Text;
using Microsoft.Extensions.Caching.Distributed;
using Fido2NetLib;
using Fido2NetLib.Objects;
using Boilerplate.Shared.Dtos.Identity;
using Boilerplate.Server.Api.Models.Identity;

namespace Boilerplate.Server.Api.Controllers.Identity;

public partial class IdentityController
{
    [AutoInject] private IFido2 fido2 = default!;
    [AutoInject] private IDistributedCache cache = default!;
    [AutoInject] protected JsonSerializerOptions jsonSerializerOptions = default!;


    [HttpGet]
    public async Task<AssertionOptions> GetWebAuthnAssertionOptions(CancellationToken cancellationToken)
    {
        var existingKeys = new List<PublicKeyCredentialDescriptor>();

        var extensions = new AuthenticationExtensionsClientInputs
        {
            Extensions = true,
            UserVerificationMethod = true,
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

    [HttpPost]
    public async Task<VerifyAssertionResult> VerifyWebAuthAssertion(AuthenticatorAssertionRawResponse clientResponse, CancellationToken cancellationToken)
    {
        var (verifyResult, _) = await Verify(clientResponse, cancellationToken);

        return verifyResult;
    }

    [HttpPost, Produces<SignInResponseDto>()]
    public async Task VerifyWebAuthAndSignIn(AuthenticatorAssertionRawResponse clientResponse, CancellationToken cancellationToken)
    {
        var (verifyResult, credential) = await Verify(clientResponse, cancellationToken);

        var user = await userManager.FindByIdAsync(credential.UserId.ToString())
                    ?? throw new ResourceNotFoundException();

        var (otp, _) = await GenerateAutomaticSignInLink(user, null, "WebAuthn");

        credential.SignCount = verifyResult.SignCount;

        DbContext.WebAuthnCredential.Update(credential);

        await DbContext.SaveChangesAsync(cancellationToken);

        await SignIn(new() { Otp = otp }, user, cancellationToken);
    }


    private async Task<(VerifyAssertionResult, WebAuthnCredential)> Verify(AuthenticatorAssertionRawResponse clientResponse, CancellationToken cancellationToken)
    {
        var response = JsonSerializer.Deserialize(clientResponse.Response.ClientDataJson, jsonSerializerOptions.GetTypeInfo<AuthenticatorResponse>())
                        ?? throw new InvalidOperationException("Invalid client data.");

        var key = new string([.. response.Challenge.Select(b => (char)b)]);
        var cachedBytes = await cache.GetAsync(key, cancellationToken)
                             ?? throw new ResourceNotFoundException();

        var jsonOptions = Encoding.UTF8.GetString(cachedBytes);
        var options = AssertionOptions.FromJson(jsonOptions);

        await cache.RemoveAsync(key, cancellationToken);

        var credential = (await DbContext.WebAuthnCredential.FirstOrDefaultAsync(c => c.Id == clientResponse.Id, cancellationToken))
                            ?? throw new ResourceNotFoundException();

        var verifyResult = await fido2.MakeAssertionAsync(new MakeAssertionParams
        {
            AssertionResponse = clientResponse,
            OriginalOptions = options,
            StoredPublicKey = credential.PublicKey!,
            StoredSignatureCounter = credential.SignCount,
            IsUserHandleOwnerOfCredentialIdCallback = IsUserHandleOwnerOfCredentialId
        }, cancellationToken);

        return (verifyResult, credential);
    }

    private async Task<bool> IsUserHandleOwnerOfCredentialId(IsUserHandleOwnerOfCredentialIdParams args, CancellationToken cancellationToken)
    {
        var storedCreds = await DbContext.WebAuthnCredential.Where(c => c.UserHandle == args.UserHandle).ToListAsync(cancellationToken);
        return storedCreds.Exists(c => new PublicKeyCredentialDescriptor(PublicKeyCredentialType.PublicKey, c.Id, c.Transports).Id.SequenceEqual(args.CredentialId));
    }
}
