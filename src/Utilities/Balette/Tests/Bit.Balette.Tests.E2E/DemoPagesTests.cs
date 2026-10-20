using Bit.Balette.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

namespace Bit.Balette.Tests.E2E;

/// <summary>
/// Whether every page of the demo actually works: it turns interactive, logs no error, and every button on it runs
/// without one - the palette it opens and the site's own chrome (the docs search, the theme toggle) included. An
/// address that does not exist gets the not-found page, which is still a working page of the app.
/// </summary>
[TestClass]
public sealed class DemoPagesTests : BaletteTest
{
    [TestMethod]
    [DataRow("/", LandingHeading)]
    [DataRow("/getting-started", "Getting started")]
    [DataRow("/commands", "Registering commands")]
    [DataRow("/pages", "Nested pages & flows")]
    [DataRow("/search", "Search & ranking")]
    [DataRow("/modes", "Modes & sources")]
    [DataRow("/keyboard", "Keyboard & shortcuts")]
    [DataRow("/theming", "Theming & templates")]
    [DataRow("/performance", "Large registry")]
    [DataRow("/mcp-server", "MCP server")]
    public async Task Every_demo_page_loads_and_every_button_on_it_runs_without_console_errors(string path, string heading)
    {
        // The copy button the site kit puts on every code sample writes to the clipboard, which a browser lets a
        // click do; headless Chromium asks for the permission first, and refuses the write without it.
        var options = DefaultContextOptions();
        options.Permissions = ["clipboard-read", "clipboard-write"];
        var page = await NewPageAsync(await NewContextAsync(options));

        var problems = new List<string>();
        page.Console += (_, message) => { if (message.Type == "error") lock (problems) problems.Add("console: " + message.Text); };
        page.PageError += (_, error) => { lock (problems) problems.Add("unhandled: " + error); };

        var response = await GotoAsync(page, path);
        Assert.AreEqual(200, response?.Status, $"{path} did not answer 200");
        await WaitForReadyAsync(page);
        await Assertions.Expect(page.Locator("h1")).ToHaveTextAsync(heading);

        // Every enabled button of the page, the site header's and the palette bar's included, must run without
        // throwing: a dead or broken control is a demo bug. Many of them open a palette - Balette's, or the header's
        // docs search - which is modal: it is closed before the next click, so each click lands on its own button
        // rather than on a backdrop.
        var buttons = page.Locator(".page button:enabled:visible");
        int count = Math.Min(await buttons.CountAsync(), 60);
        for (int i = 0; i < count; i++)
        {
            await CloseThePaletteAsync(page);
            var button = buttons.Nth(i);
            if (await button.CountAsync() == 0) break; // the list shrank: a button disabled itself while it runs

            try
            {
                await button.ClickAsync(new LocatorClickOptions { Timeout = 5_000 });
            }
            catch (TimeoutException)
            {
                // The button was disabled or re-rendered away between the count and the click; that is not a page error.
            }
        }

        // Let what the last click started - a source search, a measurement - finish and report.
        await page.WaitForTimeoutAsync(1_000);
        await CloseThePaletteAsync(page);

        string[] found;
        lock (problems) found = [.. problems];
        TestContext.WriteLine($"{path}: {count} buttons exercised, {found.Length} problems");
        foreach (string problem in found) TestContext.WriteLine("  " + problem);
        Assert.AreEqual(0, found.Length, $"{path} logged errors:{Environment.NewLine}{string.Join(Environment.NewLine, found)}");
    }

    [TestMethod]
    public async Task An_unknown_address_gets_the_not_found_page_and_the_palette_still_works_there()
    {
        var page = await NewPageAsync(await NewContextAsync());

        var response = await GotoAsync(page, "/no-such-page");

        Assert.AreEqual(404, response?.Status, "an unknown address did not answer 404");
        await WaitForReadyAsync(page);
        await Assertions.Expect(page.Locator("h1")).ToHaveTextAsync("404");

        // The layout is there, and with it the palette: the way out is one search away.
        await OpenPaletteAsync(page);
        await TypeAsync(page, "introduction");
        await ExpectFirstItemAsync(page, "navigation.introduction");
        await page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(page.Locator("h1")).ToHaveTextAsync(LandingHeading);
    }

    /// <summary>The landing page's title (Pages/Home.razor).</summary>
    private const string LandingHeading = "A command palette for Blazor, every decision in C#.";

    /// <summary>
    /// Esc until both palettes are closed: the header's docs search takes one, and in Balette's a page or a
    /// confirmation takes one Esc each before the close.
    /// </summary>
    private static async Task CloseThePaletteAsync(IPage page)
    {
        if (await SiteSearch(page).IsVisibleAsync())
        {
            await page.Keyboard.PressAsync("Escape");
            await Assertions.Expect(SiteSearch(page)).ToHaveCountAsync(0);
        }

        for (int i = 0; i < 6; i++)
        {
            if (await Dialog(page).IsVisibleAsync() is false) return;

            await page.Keyboard.PressAsync("Escape");
            try
            {
                await Dialog(page).WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden, Timeout = 750 });
                return;
            }
            catch (TimeoutException)
            {
                // A page or a confirmation took that Esc: the next one goes further.
            }
        }
        await ExpectClosedAsync(page);
    }
}
