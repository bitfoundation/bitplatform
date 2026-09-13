namespace Boilerplate.Tests.Features.ForceUpdate;

[TestClass, TestCategory("UITest"), Retry(2)]
public partial class ForceUpdateUITests : AppPageTest
{
    /// <summary>
    /// Proves the Force Update system end-to-end for the Web platform: this test raises the server's
    /// <c>SupportedAppVersions:MinimumSupportedWebAppVersion</c> above the app's own version (whatever this build's
    /// <c>Version</c> property made it). Every internal API call carries an <c>X-App-Version</c> / <c>X-App-Platform</c>
    /// header (See <c>RequestHeadersDelegatingHandler</c>), so the very next call the browser makes is rejected by
    /// <c>ForceUpdateMiddleware</c> with a <c>ClientNotSupportedException</c>. The client turns that into a persistent
    /// <c>FORCE_UPDATE</c> message (See <c>ExceptionDelegatingHandler</c>), which <c>ForceUpdateSnackBar</c> shows.
    /// <para>
    /// SignalR is off by default, so an anonymous home-page load makes no internal API call on its own. We therefore
    /// trigger one the same way a real user would: requesting a magic link on the Sign in page fires an internal POST,
    /// which the middleware rejects - so instead of the OTP panel, the force update panel appears.
    /// </para>
    /// </summary>
    [TestMethod]
    public async Task WhenClientBelowMinimumSupportedVersion_Should_ShowForceUpdatePanel()
    {
        await using var server = new AppTestServer(Context);

        // The browser reports the app's real assembly version (See AppTelemetryContext.AppVersion), so the minimum is
        // derived from that same source rather than hard-coded - a literal like "2.0.0" would silently invert the test
        // the day a generated project's Version property reaches it.
        var appVersion = typeof(AppTelemetryContext).Assembly.GetName().Version ?? new Version(1, 0, 0);
        var minimumBeyondAppVersion = new Version(appVersion.Major + 1, 0, 0).ToString();

        await server.Build(
            configureTestConfigurations: configuration => configuration["SupportedAppVersions:MinimumSupportedWebAppVersion"] = minimumBeyondAppVersion
        ).Start(TestContext.CancellationToken);

        var serverAddress = server.WebAppServerAddress;

        // Open the Sign in page and ask for a magic link. This is the first internal API call the browser makes; the
        // server rejects it because the client version is below the raised minimum supported web app version.
        await Page.GotoAsync(new Uri(serverAddress, PageUrls.SignIn).ToString(),
            new() { WaitUntil = WaitUntilState.NetworkIdle });

        await Page.GetByPlaceholder(AppStrings.EmailPlaceholder).FillAsync(MagicLinkSignInUtils.NewTestEmail());

        // The button stays disabled until the debounced e-mail value is committed, so Playwright waits for it to enable.
        await Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SendMagicLinkButtonText }).ClickAsync();

        // The rejected request publishes the persistent FORCE_UPDATE message, so the force update panel shows up with its
        // title and body (See ForceUpdateSnackBar.razor) instead of the OTP panel the request would otherwise reveal.
        // Scoped to that panel: ClientNotSupportedException's own message IS AppStrings.ForceUpdateTitle, so whenever the
        // rethrown exception also reaches AppSnackBar the page carries a second element with the very same text, and an
        // unscoped locator then fails with a strict mode violation instead of finding the panel.
        var forceUpdatePanel = Page.Locator(".force-update-snack-bar");

        await Expect(forceUpdatePanel.GetByText(AppStrings.ForceUpdateTitle)).ToBeVisibleAsync();
        await Expect(forceUpdatePanel.GetByText(AppStrings.ForceUpdateBody)).ToBeVisibleAsync();
    }
}
