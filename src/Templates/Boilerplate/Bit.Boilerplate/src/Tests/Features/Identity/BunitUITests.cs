using Bunit;
using Microsoft.AspNetCore.Components.Authorization;
using Boilerplate.Client.Core.Components.Pages.Identity.SignIn;

namespace Boilerplate.Tests.Features.Identity;

/// <summary>
/// Two ways to write the very same UI tests, each with a different trade-off:
///
/// 1. <b>Playwright</b> (see <see cref="UITests"/>): drives a real (headless) browser against the
///    Blazor Server/WebAssembly client that our <see cref="AppTestServer"/> serves. It exercises the whole stack
///    end-to-end - real HTML/CSS/JS, the WASM runtime, routing, layouts and every component on the page -
///    so it gives the highest confidence that the app truly works for a user inside a browser. The price is
///    speed: booting a browser, downloading/starting the WASM app and waiting on the network makes each test
///    noticeably slower and heavier.
///
/// 2. <b>bUnit</b> (the tests below): renders a single page/component in-memory with Blazor's renderer -
///    NO browser and NO WASM runtime, JavaScript interop runs in "loose" mode. It is dramatically faster and
///    lets you focus on one page or component in isolation, which is ideal for tight, targeted feedback loops.
///    The trade-off is reduced coverage: it does not prove the real browser rendering/JS behavior works.
///
/// Both flavours below still run against the exact same real <see cref="AppTestServer"/> (a genuine backend +
/// real HTTP calls for signing in), so the difference is purely the UI layer: real browser vs. in-memory
/// component rendering. Reach for bUnit for fast, component-focused checks and Playwright for full end-to-end
/// confidence.
/// </summary>
[TestClass, TestCategory("UITest"), Retry(2)]
public class BunitUITests
{
    [TestMethod]
    public async Task SignIn_Should_WorkAsExpected()
    {
        await using var server = new AppTestServer();
        await server.Build().Start(CancellationToken.None);

        await using var ctx = server.CreateBunitContext();

        // Render just the sign-in panel (the focused component under test) instead of the whole page/app.
        // The panel is rendered with an explicit ReturnUrl on purpose: bUnit's NavigationManager starts at
        // "http://localhost/", which is byte-identical to ToAbsoluteUri(PageUrls.Home), so asserting a redirect to the
        // home page would hold before any navigation happened and could never catch a missing one.
        var cut = ctx.Render<CascadingAuthenticationState>(parameters => parameters
            .AddChildContent<SignInPanel>(panel => panel
                .Add(p => p.SignInPanelType, SignInPanelType.Full)
                .Add(p => p.ReturnUrl, PageUrls.Settings)));

        // Fill the credentials and submit. .Change() drives the components' (non-debounced) onchange path, so
        // the two-way bound model is updated synchronously in C# - no browser/JS required.
        cut.Find($"input[placeholder='{AppStrings.EmailPlaceholder}']").Change(TestData.DefaultTestEmail);
        cut.Find($"input[placeholder='{AppStrings.PasswordPlaceholder}']").Change(TestData.DefaultTestPassword);

        cut.Find("form").Submit();

        // Submitting performs a real HTTP sign-in against the running test server, stores the tokens in the
        // in-memory storage and raises AuthenticationStateChanged. We wait for that to settle, then assert the
        // user is the seeded default account.
        var authenticationStateProvider = ctx.Services.GetRequiredService<AuthenticationStateProvider>();

        await cut.WaitForAssertionAsync(async () =>
        {
            var user = (await authenticationStateProvider.GetAuthenticationStateAsync()).User;
            Assert.IsTrue(user.IsAuthenticated());
            Assert.AreEqual(Guid.Parse("8ff71671-a1d6-4f97-abb9-d87d7b47d6e7"), user.GetUserId());
        }, timeout: TimeSpan.FromSeconds(30));

        // A successful sign-in navigates the user to the return-url she came from - but only after StoreTokens has
        // raised AuthenticationStateChanged, which is the very thing the wait above returns on. Asserting the url
        // straight after that wait is a race the assertion reliably wins, so it gets a wait of its own.
        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();

        await cut.WaitForAssertionAsync(async () =>
        {
            Assert.AreEqual(navigationManager.ToAbsoluteUri(PageUrls.Settings).ToString(), navigationManager.Uri,
                "Signing in should have navigated to the panel's ReturnUrl (See SignInPanel.GetSafeReturnUrl).");
        }, timeout: TimeSpan.FromSeconds(30));
    }
}
