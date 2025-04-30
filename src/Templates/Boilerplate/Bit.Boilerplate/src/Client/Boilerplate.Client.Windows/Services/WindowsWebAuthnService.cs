using Fido2NetLib;

namespace Boilerplate.Client.Windows.Services;

public partial class WindowsWebAuthnService : WebAuthnServiceBase
{
    [AutoInject] private ILocalHttpServer localHttpServer = default!;
    [AutoInject] private IExternalNavigationService externalNavigationService = default!;

    public AssertionOptions? GetWebAuthnCredentialOptions;
    public TaskCompletionSource<AuthenticatorAssertionRawResponse>? GetWebAuthnCredentialTcs;
    public override async ValueTask<AuthenticatorAssertionRawResponse> GetWebAuthnCredential(AssertionOptions options, CancellationToken cancellationToken)
    {
        GetWebAuthnCredentialOptions = options;

        GetWebAuthnCredentialTcs = new();

        ((WindowsLocalHttpServer)localHttpServer).WebAuthnService = this;

        await externalNavigationService.NavigateToAsync($"http://localhost:{localHttpServer.Port}/web-interop?actionName=GetWebAuthnCredential");

        return await GetWebAuthnCredentialTcs.Task;
    }

    public CredentialCreateOptions? CreateWebAuthnCredentialOptions;
    public TaskCompletionSource<AuthenticatorAttestationRawResponse>? CreateWebAuthnCredentialTcs;
    public override async ValueTask<AuthenticatorAttestationRawResponse> CreateWebAuthnCredential(CredentialCreateOptions options)
    {
        CreateWebAuthnCredentialOptions = options;

        CreateWebAuthnCredentialTcs = new();

        ((WindowsLocalHttpServer)localHttpServer).WebAuthnService = this;

        await externalNavigationService.NavigateToAsync($"http://localhost:{localHttpServer.Port}/web-interop?actionName=CreateWebAuthnCredential");

        return await CreateWebAuthnCredentialTcs.Task;
    }

    public override async ValueTask<bool> IsWebAuthnAvailable()
    {
        var osVersion = Environment.OSVersion.Version;

        // Windows 10 version 1903 is build 18362
        // Major version should be 10, Build number should be > 18362
        return osVersion.Major >= 10 && osVersion.Build > 18362;
    }
}
