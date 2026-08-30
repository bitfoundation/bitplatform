namespace Boilerplate.Tests.E2E.Features.Core;

/// <summary>
/// Opens each app and proves it not only rendered but is actually interactive. Written once here; the Web, Windows and
/// Android shells decide where the app runs, and the web shell additionally re-targets chromium/firefox/webkit through
/// BROWSER.
/// </summary>
public abstract class SmokeTestsBase : AppsTestBase
{
    /// <summary>
    /// Waits for the app shell every app renders through Client.Core's AppShell.razor, then opens the header's account
    /// menu (AppMenu.razor's BitDropMenu) and expects its callout. Prerendered html can show the shell - and even the
    /// menu's trigger - with no .NET app running behind it, but only a booted app handles the click.
    /// </summary>
    [TestMethod]
    [DataRow(App.AdminPanel, DisplayName = nameof(App.AdminPanel))]
    [DataRow(App.AdminPanelWasmStandalone, DisplayName = nameof(App.AdminPanelWasmStandalone))]
    [DataRow(App.Todo, DisplayName = nameof(App.Todo))]
    [DataRow(App.TodoAot, DisplayName = nameof(App.TodoAot))]
    [DataRow(App.TodoSmall, DisplayName = nameof(App.TodoSmall))]
    [DataRow(App.TodoOffline, DisplayName = nameof(App.TodoOffline))]
    [DataRow(App.Sales, DisplayName = nameof(App.Sales))]
    public async Task App_Should_BecomeInteractive(App app)
    {
        var page = await OpenApp(app);

        // Generous because a first visit includes the WebAssembly boot / bswup precache on a cold cache.
        await Expect(page.Locator("main .main-container").First)
            .ToBeVisibleAsync(new() { Timeout = (float)TimeSpan.FromMinutes(2).TotalMilliseconds });

        var menuTrigger = page.Locator("header .bit-drm").First;
        var menuCallout = page.Locator(".app-menu-callout .app-menu-card").First;

        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromMinutes(2);

        // A click landing in the gap between prerendered html and the app taking over is simply swallowed, so the
        // click is retried against a deadline rather than trusted once (the same reasoning as
        // PlaywrightLocatorExtensions' FillEnsuringStable).
        while (true)
        {
            await menuTrigger.ClickAsync();

            try
            {
                await Expect(menuCallout).ToBeVisibleAsync(new() { Timeout = 2_000 });
                return;
            }
            catch (PlaywrightException) when (DateTimeOffset.UtcNow < deadline)
            {
            }
        }
    }
}
