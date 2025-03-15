//+:cnd:noEmit
using Fido2NetLib;

namespace Microsoft.JSInterop;

public static partial class IJSRuntimeWebAuthnExtensions
{
    public static ValueTask<bool> IsWebAuthnAvailable(this IJSRuntime jsRuntime)
    {
        return jsRuntime.InvokeAsync<bool>("WebAuthn.isAvailable");
    }

    public static ValueTask<bool> IsWebAuthnConfigured(this IJSRuntime jsRuntime, Guid? userId = null)
    {
        return jsRuntime.InvokeAsync<bool>("WebAuthn.isConfigured", userId);
    }

    public static ValueTask<Guid[]?> GetWebAuthnConfiguredUserIds(this IJSRuntime jsRuntime)
    {
        return jsRuntime.InvokeAsync<Guid[]?>("WebAuthn.getConfiguredUserIds");
    }

    public static ValueTask SetWebAuthnConfiguredUserId(this IJSRuntime jsRuntime, Guid userId)
    {
        return jsRuntime.InvokeVoidAsync("WebAuthn.setConfiguredUserId", userId);
    }

    public static ValueTask RemoveWebAuthnConfiguredUserId(this IJSRuntime jsRuntime, Guid? userId = null)
    {
        return jsRuntime.InvokeVoidAsync("WebAuthn.removeConfiguredUserId", userId);
    }

    public static ValueTask<AuthenticatorAttestationRawResponse> CreateWebAuthnCredential(this IJSRuntime jsRuntime, CredentialCreateOptions options)
    {
        return jsRuntime.InvokeAsync<AuthenticatorAttestationRawResponse>("WebAuthn.createCredential", options);
    }

    public static ValueTask<AuthenticatorAssertionRawResponse> GetWebAuthnCredential(this IJSRuntime jsRuntime, AssertionOptions options)
    {
        return jsRuntime.InvokeAsync<AuthenticatorAssertionRawResponse>("WebAuthn.getCredential", options);
    }
}
