using Fido2NetLib;

namespace Boilerplate.Client.Windows.Services;

public partial class WindowsWebAuthnService : WebAuthnServiceBase
{
    [AutoInject] private ILocalHttpServer localHttpServer = default!;
    [AutoInject] private IExternalNavigationService externalNavigationService = default!;

    public override async ValueTask<AuthenticatorAssertionRawResponse> GetWebAuthnCredential(AssertionOptions options, CancellationToken cancellationToken)
    {
        try
        {
            await externalNavigationService.NavigateToAsync($"{localHttpServer.Origin}/external-js-runner.html");

            var result = (await ExternalJSRunnerWebSocketModule.RequestToBeSent!.Invoke(JsonSerializer.SerializeToDocument(new { Type = "getCredential", Options = options }, JsonSerializerOptions.Web)))
                .Deserialize<AuthenticatorAssertionRawResponse>(JsonSerializerOptions.Web)!;

            return result ?? throw new TaskCanceledException();
        }
        finally
        {
            await CloseExternalBrowser();
        }
    }

    private static async Task CloseExternalBrowser()
    {
        await ExternalJSRunnerWebSocketModule.RequestToBeSent!.Invoke(JsonSerializer.SerializeToDocument(new { Type = "close" }, JsonSerializerOptions.Web));

        Application.OpenForms[0]!.Invoke(() =>
        {
            Application.OpenForms[0]!.Activate();
        });
    }

    public override async ValueTask<AuthenticatorAttestationRawResponse> CreateWebAuthnCredential(CredentialCreateOptions options)
    {
        try
        {
            await externalNavigationService.NavigateToAsync($"{localHttpServer.Origin}/external-js-runner.html");

            return (await ExternalJSRunnerWebSocketModule.RequestToBeSent!.Invoke(JsonSerializer.SerializeToDocument(new { Type = "createCredential", Options = options }, JsonSerializerOptions.Web)))
                .Deserialize<AuthenticatorAttestationRawResponse>(JsonSerializerOptions.Web)!;
        }
        finally
        {
            await CloseExternalBrowser();
        }
    }

    public override async ValueTask<bool> IsWebAuthnAvailable()
    {
        var osVersion = Environment.OSVersion.Version;

        // Windows 10 version 1903 is build 18362
        // Major version should be 10, Build number should be > 18362
        if (osVersion.Major >= 10 && osVersion.Build > 18362)
        {
            return true;
        }
        return false;
    }
}
