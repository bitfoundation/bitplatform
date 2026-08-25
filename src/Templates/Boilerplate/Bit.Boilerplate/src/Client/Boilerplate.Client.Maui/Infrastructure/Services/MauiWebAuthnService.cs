// [mirror] webauthn ceremony driven through the loopback server - keep in sync with:
// - src/Client/Boilerplate.Client.Windows/Infrastructure/Services/WindowsWebAuthnService.cs

namespace Boilerplate.Client.Maui.Infrastructure.Services;

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

        ((MauiLocalHttpServer)localHttpServer).WebAuthnService = this;

        var port = localHttpServer.EnsureStarted();

        await externalNavigationService.NavigateTo($"http://localhost:{port}/{PageUrls.WebInteropApp}?actionName=CreateWebAuthnCredential&token={Uri.EscapeDataString(localHttpServer.SessionToken)}&localHttpPort={port}");

        return await CreateWebAuthnCredentialTcs.Task;
    }

    /// <summary>
    /// Android, iOS and macOS always have it. On Windows the platform authenticator (Windows Hello) needs
    /// Windows 10 version 1903 / build 18362, and SupportedOSPlatformVersion in Directory.Build.props floors the
    /// app at 10.0.17763 - lower than that - so the app can run on a Windows where the feature is unavailable
    /// and the runtime check is doing real work.
    /// </summary>
    public override async ValueTask<bool> IsWebAuthnAvailable()
    {
        return OperatingSystem.IsWindows() is false
            || OperatingSystem.IsWindowsVersionAtLeast(10, 0, 18362);
    }
}
