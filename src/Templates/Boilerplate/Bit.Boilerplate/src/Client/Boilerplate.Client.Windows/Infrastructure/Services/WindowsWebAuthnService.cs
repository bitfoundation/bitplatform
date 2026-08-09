// [mirror] webauthn ceremony driven through the loopback server - keep in sync with:
// - src/Client/Boilerplate.Client.Maui/Infrastructure/Services/MauiWebAuthnService.cs

namespace Boilerplate.Client.Windows.Infrastructure.Services;

public partial class WindowsWebAuthnService : WebAuthnServiceBase
{
    [AutoInject] private ILocalHttpServer localHttpServer = default!;
    [AutoInject] private IExternalNavigationService externalNavigationService = default!;

    public JsonElement? GetWebAuthnCredentialOptions;
    public TaskCompletionSource<JsonElement>? GetWebAuthnCredentialTcs;

    public override async ValueTask<JsonElement> GetWebAuthnCredential(JsonElement options)
    {
        GetWebAuthnCredentialOptions = options;

        GetWebAuthnCredentialTcs = new();

        ((WindowsLocalHttpServer)localHttpServer).WebAuthnService = this;

        var port = localHttpServer.EnsureStarted();

        await externalNavigationService.NavigateTo($"http://localhost:{port}/{PageUrls.WebInteropApp}?actionName=GetWebAuthnCredential&token={Uri.EscapeDataString(localHttpServer.SessionToken)}&localHttpPort={port}");

        return await GetWebAuthnCredentialTcs.Task;
    }

    public JsonElement? CreateWebAuthnCredentialOptions;
    public TaskCompletionSource<JsonElement>? CreateWebAuthnCredentialTcs;

    public override async ValueTask<JsonElement> CreateWebAuthnCredential(JsonElement options)
    {
        CreateWebAuthnCredentialOptions = options;

        CreateWebAuthnCredentialTcs = new();

        ((WindowsLocalHttpServer)localHttpServer).WebAuthnService = this;

        var port = localHttpServer.EnsureStarted();

        await externalNavigationService.NavigateTo($"http://localhost:{port}/{PageUrls.WebInteropApp}?actionName=CreateWebAuthnCredential&token={Uri.EscapeDataString(localHttpServer.SessionToken)}&localHttpPort={port}");

        return await CreateWebAuthnCredentialTcs.Task;
    }

    public override async ValueTask<bool> IsWebAuthnAvailable()
    {
        var osVersion = Environment.OSVersion.Version;

        // Windows 10 version 1903 is build 18362
        // Major version should be 10, Build number should be >= 18362
        return osVersion.Major >= 10 && osVersion.Build >= 18362;
    }
}
