//+:cnd:noEmit

using Fido2NetLib;

namespace Microsoft.JSInterop;

public static partial class IJSRuntimeWebAuthnExtensions
{
    public static ValueTask<bool> IsWebAuthnAvailable(this IJSRuntime jsRuntime)
    {
        return jsRuntime.InvokeAsync<bool>("WebAuthn.isAvailable");
    }

    public static ValueTask StoreWebAuthnConfigured(this IJSRuntime jsRuntime, string username)
    {
        return jsRuntime.InvokeVoidAsync("WebAuthn.storeConfigured", username);
    }

    public static ValueTask<bool> IsWebAuthnConfigured(this IJSRuntime jsRuntime, string username)
    {
        return jsRuntime.InvokeAsync<bool>("WebAuthn.isConfigured", username);
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
