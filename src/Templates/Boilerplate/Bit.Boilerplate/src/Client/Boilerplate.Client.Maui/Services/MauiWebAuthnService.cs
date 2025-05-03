namespace Boilerplate.Client.Maui.Services;

public partial class MauiWebAuthnService : WebAuthnServiceBase
{
    [AutoInject] private ILocalHttpServer localHttpServer = default!;
    [AutoInject] private IExternalNavigationService externalNavigationService = default!;

    public JsonElement? GetWebAuthnCredentialOptions;
    public TaskCompletionSource<JsonElement>? GetWebAuthnCredentialTcs;
    public override async ValueTask<JsonElement> GetWebAuthnCredential(JsonElement options)
    {
        GetWebAuthnCredentialOptions = options;

        GetWebAuthnCredentialTcs = new();

        ((MauiLocalHttpServer)localHttpServer).WebAuthnService = this;

        await externalNavigationService.NavigateToAsync($"http://localhost:{localHttpServer.Port}/hybrid-app-web-interop?actionName=GetWebAuthnCredential");

        return await GetWebAuthnCredentialTcs.Task;
    }

    public JsonElement? CreateWebAuthnCredentialOptions;
    public TaskCompletionSource<JsonElement>? CreateWebAuthnCredentialTcs;
    public override async ValueTask<JsonElement> CreateWebAuthnCredential(JsonElement options)
    {
        CreateWebAuthnCredentialOptions = options;

        CreateWebAuthnCredentialTcs = new();

        ((MauiLocalHttpServer)localHttpServer).WebAuthnService = this;

        await externalNavigationService.NavigateToAsync($"http://localhost:{localHttpServer.Port}/hybrid-app-web-interop?actionName=CreateWebAuthnCredential");

        return await CreateWebAuthnCredentialTcs.Task;
    }

    public override async ValueTask<bool> IsWebAuthnAvailable()
    {
        var osVersion = Environment.OSVersion.Version;

        return OperatingSystem.IsWindowsVersionAtLeast(10, 0, 18362)
            || true /* Checkout SupportedOSPlatformVersion in Directory.Build.props */;
    }
}
