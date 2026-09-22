//+:cnd:noEmit
using Fido2NetLib;
using Fido2NetLib.Objects;
using System.Buffers.Text;

namespace Boilerplate.Server.Api.Features.Identity;

public partial class IdentityController
{
    [AutoInject] private IFido2 fido2 = default!;
    [AutoInject] private IFusionCache cache = default!;
    [AutoInject] protected JsonSerializerOptions jsonSerializerOptions = default!;


    [HttpPost]
    public async Task<AssertionOptions> GetWebAuthnAssertionOptions(WebAuthnAssertionOptionsRequestDto request, CancellationToken cancellationToken)
    {
        var existingKeys = new List<PublicKeyCredentialDescriptor>();

        if (request.UserIds is not null)
        {
            var existingCredentials = await DbContext.WebAuthnCredential.Where(c => request.UserIds.Contains(c.UserId))
                                                                        .OrderByDescending(c => c.RegDate)
                                                                        .Select(c => new { c.Id, c.Transports })
                                                                        .ToArrayAsync(cancellationToken);
            existingKeys.AddRange(existingCredentials.Select(c => new PublicKeyCredentialDescriptor(PublicKeyCredentialType.PublicKey, c.Id, c.Transports)));
        }

        var extensions = new AuthenticationExtensionsClientInputs
        {
            Extensions = true,
            UserVerificationMethod = true,
        };

        var options = fido2.GetAssertionOptions(new GetAssertionOptionsParams
        {
            //Extensions = extensions,
            AllowedCredentials = existingKeys,
            UserVerification = UserVerificationRequirement.Required,
        });

        await cache.SetAsync(GetAssertionOptionsCacheKey(options.Challenge), options,
            options => options.SetDuration(TimeSpan.FromMinutes(3)).SetPriority(CacheItemPriority.NeverRemove),
            cancellationToken);

        return options;
    }

    /// <summary>
    /// A WebAuthn challenge is raw bytes, so it's base64url encoded to keep the cache key printable.
    /// Mapping each byte to a char instead would put NUL and control characters inside the Redis key,
    /// making the entry unreadable in cache tooling and in logs.
    /// </summary>
    private static string GetAssertionOptionsCacheKey(byte[] challenge) => $"WebAuthn_AssertionOptions_{Base64Url.EncodeToString(challenge)}";

    [HttpPost]
    public async Task<VerifyAssertionResult> VerifyWebAuthAssertion(AuthenticatorAssertionRawResponse clientResponse, CancellationToken cancellationToken)
    {
        var (verifyResult, _, assertionOptionsCacheKey) = await Verify(clientResponse, cancellationToken);

        await cache.RemoveAsync(assertionOptionsCacheKey, token: cancellationToken);

        return verifyResult;
    }

    [HttpPost, Produces<SignInResponseDto>()]
    public async Task VerifyWebAuthAndSignIn(VerifyWebAuthnAndSignInRequestDto<AuthenticatorAssertionRawResponse> request, CancellationToken cancellationToken)
    {
        var (verifyResult, credential, assertionOptionsCacheKey) = await Verify(request.ClientResponse, cancellationToken);

        var user = await userManager.FindByIdAsync(credential.UserId.ToString())
                    ?? throw new ResourceNotFoundException().WithData("Reason", "User not found.");

        var (otp, _) = await GenerateAutomaticSignInLink(user, null, "WebAuthn");

        // When two factor is enabled and no code has been supplied yet, SignIn below responds with RequiresTwoFactor and the
        // client repeats this action with the code, verifying the very same client response. Only the final step is terminal.
        var isFinalStep = user.TwoFactorEnabled is false || request.TfaCode is not null;

        if (isFinalStep)
        {
            credential.SignCount = verifyResult.SignCount;
            DbContext.WebAuthnCredential.Update(credential);
            await DbContext.SaveChangesAsync(cancellationToken);
        }

        await SignIn(new() { Otp = otp, TwoFactorCode = request.TfaCode }, user, cancellationToken);

        if (isFinalStep)
        {
            await cache.RemoveAsync(assertionOptionsCacheKey, token: cancellationToken);
        }
    }

    [HttpPost]
    public async Task VerifyWebAuthAndSendTwoFactorToken(AuthenticatorAssertionRawResponse clientResponse, CancellationToken cancellationToken)
    {
        var (verifyResult, credential, _) = await Verify(clientResponse, cancellationToken);

        var user = await userManager.FindByIdAsync(credential.UserId.ToString())
                    ?? throw new ResourceNotFoundException().WithData("Reason", "User not found.");

        var (otp, _) = await GenerateAutomaticSignInLink(user, null, "WebAuthn");

        await SendTwoFactorToken(new() { Otp = otp }, user, cancellationToken);
    }


    /// <summary>
    /// Elevates the calling session against a passkey instead of the 6-digit code, returning when that window closes
    /// (<see cref="AuthPolicies.ELEVATED_ACCESS"/>). The assertion is asked for with <c>UserVerification.Required</c>.
    /// </summary>
    private async Task<DateTimeOffset> ElevateByWebAuthn(User user, JsonElement clientResponse, CancellationToken cancellationToken)
    {
        if (await userManager.IsLockedOutAsync(user))
            throw UserLockedOutException(user);

        var assertion = clientResponse.Deserialize(jsonSerializerOptions.GetTypeInfo<AuthenticatorAssertionRawResponse>())
                        ?? throw new BadRequestException(nameof(AppStrings.InvalidToken)).WithData("Reason", "Invalid WebAuthn client response.");

        var (verifyResult, credential, assertionOptionsCacheKey) = await Verify(assertion, cancellationToken);

        // GetWebAuthnAssertionOptions is anonymous and takes the user ids to offer from its caller, so without this
        // anyone holding a refresh token could elevate with somebody else's passkey.
        if (credential.UserId != user.Id)
            throw new UnauthorizedException().WithData("Reason", "The WebAuthn credential belongs to another user.");

        credential.SignCount = verifyResult.SignCount;

        await cache.RemoveAsync(assertionOptionsCacheKey, token: cancellationToken); // One assertion, one elevation.

        return NewElevatedSessionExpiresOn();
    }

    private async Task<(VerifyAssertionResult VerifyResult, WebAuthnCredential Credential, string AssertionOptionsCacheKey)> Verify(AuthenticatorAssertionRawResponse clientResponse, CancellationToken cancellationToken)
    {
        var response = JsonSerializer.Deserialize(clientResponse.Response.ClientDataJson, jsonSerializerOptions.GetTypeInfo<AuthenticatorResponse>())
                        ?? throw new InvalidOperationException("Invalid client data.");

        var key = GetAssertionOptionsCacheKey(response.Challenge);
        var options = await cache.GetOrSetAsync<AssertionOptions>(key,
            async _ => throw new ResourceNotFoundException().WithData("Reason", "Assertion options not found in cache."),
            token: cancellationToken);

        var credential = (await DbContext.WebAuthnCredential.FirstOrDefaultAsync(c => c.Id == clientResponse.RawId, cancellationToken))
                            ?? throw new ResourceNotFoundException().WithData("Reason", "WebAuthn credential not found.");

        var verifyResult = await fido2.MakeAssertionAsync(new MakeAssertionParams
        {
            AssertionResponse = clientResponse,
            OriginalOptions = options,
            StoredPublicKey = credential.PublicKey!,
            StoredSignatureCounter = credential.SignCount,
            IsUserHandleOwnerOfCredentialIdCallback = IsUserHandleOwnerOfCredentialId
        }, cancellationToken);

        return (verifyResult, credential, key);
    }

    private async Task<bool> IsUserHandleOwnerOfCredentialId(IsUserHandleOwnerOfCredentialIdParams args, CancellationToken cancellationToken)
    {
        var storedCreds = await DbContext.WebAuthnCredential.Where(c => c.UserHandle == args.UserHandle).ToListAsync(cancellationToken);
        return storedCreds.Exists(c => new PublicKeyCredentialDescriptor(PublicKeyCredentialType.PublicKey, c.Id, c.Transports).Id.SequenceEqual(args.CredentialId));
    }
}
