namespace Boilerplate.Client.Maui.Services;

public partial class MauiWebAuthnService : WebAuthnServiceBase
{
    [AutoInject] private ILocalHttpServer localHttpServer = default!;
    [AutoInject] private IExternalNavigationService externalNavigationService = default!;

    public object? GetWebAuthnCredentialOptions;
    public TaskCompletionSource<object>? GetWebAuthnCredentialTcs;
    public override async ValueTask<object> GetWebAuthnCredential(object options)
    {
        GetWebAuthnCredentialOptions = options;

        GetWebAuthnCredentialTcs = new();

        ((MauiLocalHttpServer)localHttpServer).WebAuthnService = this;

        await externalNavigationService.NavigateToAsync($"http://localhost:{localHttpServer.Port}/web-interop?actionName=GetWebAuthnCredential");

        return await GetWebAuthnCredentialTcs.Task;
    }

    public object? CreateWebAuthnCredentialOptions;
    public TaskCompletionSource<object>? CreateWebAuthnCredentialTcs;
    public override async ValueTask<object> CreateWebAuthnCredential(object options)
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
