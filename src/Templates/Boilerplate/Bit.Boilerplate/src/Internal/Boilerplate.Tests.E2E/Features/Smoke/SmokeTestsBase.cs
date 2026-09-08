namespace Boilerplate.Tests.E2E.Features.Smoke;

/// <summary>
/// Opens each app and proves it is not only rendered but actually interactive; the Web, Windows and Android classes
/// deriving from this decide where the app runs.
/// </summary>
public abstract class SmokeTestsBase : AppTestBase
{
    /// <summary>
    /// Waits for AppShell.razor's shell, then opens the header's account menu (AppMenu.razor's BitDropMenu):
    /// prerendered html shows both without a .NET app behind them, but only a booted app handles the click.
    /// <para>
    /// Sales has a Windows build but no Android one, so its Android row reports inconclusive - see
    /// <see cref="AppTestBase.OpenApp"/>, which is what keeps such a gap visible as skipped.
    /// </para>
    /// </summary>
    [TestMethod]
    [DataRow(App.Todo, DisplayName = nameof(App.Todo))]
    [DataRow(App.Sales, DisplayName = nameof(App.Sales))]
    [DataRow(App.AdminPanel, DisplayName = nameof(App.AdminPanel))]
    public virtual async Task App_Should_BecomeInteractive(App app)
    {
        var page = await OpenApp(app);

        await WaitUntilInteractive(page);

        var menuTrigger = page.Locator("header .bit-drm").First;
        var menuCallout = page.Locator(".app-menu-callout .app-menu-card").First;

        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromMinutes(2);

        // A click landing before the app takes over the prerendered html is swallowed, so it is retried against a
        // deadline (like PlaywrightLocatorExtensions' FillEnsuringStable).
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
