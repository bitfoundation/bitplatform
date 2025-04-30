using Fido2NetLib;

namespace Boilerplate.Client.Maui.Services;

public partial class MauiWebAuthnService : WebAuthnServiceBase
{
    [AutoInject] private ILocalHttpServer localHttpServer = default!;
    [AutoInject] private IExternalNavigationService externalNavigationService = default!;

    public AssertionOptions? GetWebAuthnCredentialOptions;
    public TaskCompletionSource<AuthenticatorAssertionRawResponse>? GetWebAuthnCredentialTcs;
    public override async ValueTask<AuthenticatorAssertionRawResponse> GetWebAuthnCredential(AssertionOptions options, CancellationToken cancellationToken)
    {
        GetWebAuthnCredentialOptions = options;

        GetWebAuthnCredentialTcs = new();

        ((MauiLocalHttpServer)localHttpServer).WebAuthnService = this;

        await externalNavigationService.NavigateToAsync($"http://localhost:{localHttpServer.Port}/web-interop?actionName=GetWebAuthnCredential");

        return await GetWebAuthnCredentialTcs.Task;
    }

    public CredentialCreateOptions? CreateWebAuthnCredentialOptions;
    public TaskCompletionSource<AuthenticatorAttestationRawResponse>? CreateWebAuthnCredentialTcs;
    public override async ValueTask<AuthenticatorAttestationRawResponse> CreateWebAuthnCredential(CredentialCreateOptions options)
    {
        CreateWebAuthnCredentialOptions = options;

        CreateWebAuthnCredentialTcs = new();

        ((MauiLocalHttpServer)localHttpServer).WebAuthnService = this;

        await externalNavigationService.NavigateToAsync($"http://localhost:{localHttpServer.Port}/web-interop?actionName=CreateWebAuthnCredential");

        return await CreateWebAuthnCredentialTcs.Task;
    }

    public override async ValueTask<bool> IsWebAuthnAvailable()
    {
        var osVersion = Environment.OSVersion.Version;

        return OperatingSystem.IsWindowsVersionAtLeast(10, 0, 18362)
            || true /* Checkout SupportedOSPlatformVersion in Directory.Build.props */;
    }
}
