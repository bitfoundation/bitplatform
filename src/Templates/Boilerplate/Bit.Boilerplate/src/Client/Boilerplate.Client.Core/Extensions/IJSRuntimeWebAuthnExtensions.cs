//+:cnd:noEmit

using Fido2NetLib;

namespace Microsoft.JSInterop;

public static partial class IJSRuntimeWebAuthnExtensions
{
    public static ValueTask<bool> IsWebAuthnAvailable(this IJSRuntime jsRuntime)
    {
        return jsRuntime.InvokeAsync<bool>("WebAuthn.isAvailable");
    }

    public static ValueTask<AuthenticatorAttestationRawResponse> CreateWebAuthnCredential(this IJSRuntime jsRuntime, CredentialCreateOptions options)
    {
        return jsRuntime.InvokeAsync<AuthenticatorAttestationRawResponse>("WebAuthn.createCredential", options);
    }

    public static ValueTask<AuthenticatorAssertionRawResponse> VerifyWebAuthnCredential(this IJSRuntime jsRuntime, AssertionOptions options)
    {
        return jsRuntime.InvokeAsync<AuthenticatorAssertionRawResponse>("WebAuthn.verifyCredential", options);
    }
}
