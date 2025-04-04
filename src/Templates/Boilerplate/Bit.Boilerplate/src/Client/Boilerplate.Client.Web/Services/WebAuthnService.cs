using Fido2NetLib;

namespace Boilerplate.Client.Web.Services;

public partial class WebAuthnService : WebAuthnServiceBase
{
    [AutoInject] private IJSRuntime jsRuntime = default!;

    public override async ValueTask<bool> IsWebAuthnAvailable()
    {
        return await jsRuntime.InvokeAsync<bool>("WebAuthn.isAvailable");
    }

    public override async ValueTask<AuthenticatorAttestationRawResponse> CreateWebAuthnCredential(CredentialCreateOptions options)
    {
        return await jsRuntime.InvokeAsync<AuthenticatorAttestationRawResponse>("WebAuthn.createCredential", options);
    }

    public override async ValueTask<AuthenticatorAssertionRawResponse> GetWebAuthnCredential(AssertionOptions options, CancellationToken cancellationToken)
    {
        return await jsRuntime.InvokeAsync<AuthenticatorAssertionRawResponse>("WebAuthn.getCredential", options);
    }
}
