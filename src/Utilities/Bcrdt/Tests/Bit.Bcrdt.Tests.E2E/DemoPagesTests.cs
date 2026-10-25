using System.Text.RegularExpressions;
using Bit.Bcrdt.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

namespace Bit.Bcrdt.Tests.E2E;

/// <summary>
/// Whether every page of the demo actually works: it answers, turns interactive, opens its replicas where it has
/// them, and logs no error doing so. An address that does not exist gets the not-found page with a 404, and the live
/// page without a room sends the visitor to a fresh one.
/// </summary>
[TestClass]
public sealed class DemoPagesTests : BcrdtTest
{
    /// <summary>
    /// Every routable page of the demo, read from the <c>@page</c> directives of its sources together with the
    /// <c>h1</c> each one spells out. A route parameter (the live page's room) gets a room of the sweep's own, and
    /// the live page is swept on both of its relays.
    /// </summary>
    public static IEnumerable<object[]> DemoPages => RepoLayout.DemoPages()
        .SelectMany(page => Addresses(page.Route).Select(path => new object[] { path, page.Heading }));

    [TestMethod]
    [DynamicData(nameof(DemoPages))]
    public async Task Every_demo_page_loads_interactively_without_an_error(string path, string heading)
    {
        var page = await NewPageAsync(await NewContextAsync());
        var problems = TrackProblems(page, BaseUrl);

        var response = await GotoAsync(page, path);
        Assert.AreEqual(200, response?.Status, $"{path} did not answer 200");
        await WaitForInteractiveAsync(page);
        if (heading.Length > 0) await Assertions.Expect(page.Locator("h1")).ToHaveTextAsync(heading);
        else await Assertions.Expect(page.Locator("h1")).ToHaveTextAsync(new Regex("\\S"));

        // A page with replicas shows an "Opening…" placeholder until they are open, then a panel apiece, and has
        // connected every one of them: each panel's badge says synced. On the live page the one panel is this tab's
        // replica, connected through the demo server's relay.
        await Assertions.Expect(page.Locator(".loading")).ToHaveCountAsync(0, new() { Timeout = 60_000 });
        int panels = await Panels(page).CountAsync();
        if (panels > 0) await WaitForReplicasAsync(page, panels);

        // A page with an editor on it has loaded the library's browser module, which every BcrdtTextArea imports
        // after its first render to attach itself to its textarea; without it nothing typed would reach the replica.
        int editors = await page.Locator("section.panel textarea").CountAsync();
        if (editors > 0)
        {
            await page.WaitForFunctionAsync(
                // The import map points the module at its fingerprinted file, bit-bcrdt.<hash>.js, in some builds.
                @"() => performance.getEntriesByType('resource').some(e => /^\/_content\/Bit\.Bcrdt\/bit-bcrdt(\.\w+)?\.js$/.test(new URL(e.name).pathname))",
                null, new PageWaitForFunctionOptions { Timeout = 20_000 });
        }

        Assert.IsFalse(await page.Locator("#blazor-error-ui").IsVisibleAsync(), $"{path} shows the unhandled-error banner");

        string[] found;
        lock (problems) found = [.. problems];
        TestContext.WriteLine($"{path}: {panels} replica panel(s), {editors} editor(s), {found.Length} problem(s)");
        foreach (var problem in found) TestContext.WriteLine("  " + problem);
        Assert.IsEmpty(found, $"{path} reported:{Environment.NewLine}{string.Join(Environment.NewLine, found)}");
    }

    [TestMethod]
    public void The_page_sweep_finds_every_demo()
    {
        // A sanity check on the sweep's own source: if the routes stopped being found, the sweep would pass on nothing.
        var routes = RepoLayout.DemoPages().Select(page => page.Route).ToArray();
        foreach (var route in new[] { "/", "/getting-started", "/text", "/list", "/form", "/conflicts", "/undo", "/internals", "/live", "/live/{Room}", "/mcp-server", "/not-found" })
            CollectionAssert.Contains(routes, route, $"the sweep no longer finds {route}");
    }

    [TestMethod]
    public async Task An_unknown_address_answers_404_with_the_not_found_page()
    {
        var page = await NewPageAsync(await NewContextAsync());
        // The 404 of the document itself is the point of the test; the browser logs it as a failed load.
        var problems = TrackProblems(page, BaseUrl, expected: problem => problem.Contains("404", StringComparison.Ordinal));

        var response = await GotoAsync(page, "/no-such-page");

        Assert.AreEqual(404, response?.Status, "an unknown address did not answer 404");
        await WaitForInteractiveAsync(page);
        await Assertions.Expect(page.Locator("h1")).ToHaveTextAsync("404");
        await Assertions.Expect(page.Locator("main")).ToContainTextAsync("There is no page at this address");

        // The layout is there, and with it the way back: the site's links still navigate, in the browser.
        await Assertions.Expect(page.Locator("header.bb-header")).ToBeVisibleAsync();
        await page.Locator("footer.bb-footer").GetByRole(AriaRole.Link, new() { Name = "Collaborative text", Exact = true }).ClickAsync();
        await page.WaitForURLAsync(BaseUrl + "/text");
        await WaitForReplicasAsync(page, 3);

        Assert.IsFalse(await page.Locator("#blazor-error-ui").IsVisibleAsync(), "the not-found page shows the unhandled-error banner");
        string[] found;
        lock (problems) found = [.. problems];
        Assert.IsEmpty(found, string.Join(Environment.NewLine, found));
    }

    [TestMethod]
    public async Task The_live_page_without_a_room_sends_the_visitor_to_a_fresh_one()
    {
        var page = await OpenAsync("/live");

        // The redirect happens in the browser, once the page runs there: it replaces the address with a new room.
        await page.WaitForURLAsync(new Regex("/live/[0-9a-f]{10}$"), new() { Timeout = 30_000 });
        await WaitForReplicasAsync(page, 1);

        // The share link names that room, on the relay the page uses (the SignalR hub unless asked otherwise).
        await Assertions.Expect(page.Locator(".live-link")).ToHaveTextAsync(page.Url);
        await Assertions.Expect(page.Locator(".live-relay-active")).ToHaveTextAsync("SignalR hub");

        // Each visit gets a room of its own: a second visitor without a link does not land in the first one's room.
        var second = await OpenAsync("/live");
        await second.WaitForURLAsync(new Regex("/live/[0-9a-f]{10}$"), new() { Timeout = 30_000 });
        Assert.AreNotEqual(page.Url, second.Url, "two visits to /live were sent to the same room");

        // The relay asked for is kept: /live?relay=websocket goes to a fresh room on the WebSocket endpoint.
        var websocket = await OpenAsync("/live?relay=websocket");
        await websocket.WaitForURLAsync(new Regex("/live/[0-9a-f]{10}\\?relay=websocket$"), new() { Timeout = 30_000 });
        await Assertions.Expect(websocket.Locator(".live-relay-active")).ToHaveTextAsync("WebSocket endpoint");
    }

    [TestMethod]
    public async Task The_narrow_width_menus_take_the_focus_and_close_on_escape()
    {
        var page = await OpenAsync("/text");
        await page.SetViewportSizeAsync(390, 844);

        // The docs drawer. Its close button takes the focus as it opens - Safari does not focus a clicked button, so
        // without that the Escape below would reach <body>, where nothing listens for it - and the drawer gives the
        // focus back to its toggle.
        var docsToggle = page.Locator(".bb-docs-menu-toggle");
        var docsPanel = page.Locator("aside.bb-docs-sidebar");
        await docsToggle.ClickAsync();
        await Assertions.Expect(docsPanel).ToHaveClassAsync(new Regex("\\bbb-open\\b"));
        await Assertions.Expect(docsPanel.Locator(".bb-docs-close")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(docsPanel).Not.ToHaveClassAsync(new Regex("\\bbb-open\\b"));
        await Assertions.Expect(docsToggle).ToBeFocusedAsync();

        // The site menu, the same way.
        var menuButton = page.Locator(".bb-menu-btn");
        var drawer = page.Locator("#bb-header-drawer");
        await menuButton.ClickAsync();
        await Assertions.Expect(drawer).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(drawer).ToHaveCountAsync(0);
        await Assertions.Expect(menuButton).ToBeFocusedAsync();

        // A link in the drawer still navigates, and the drawer closes behind it.
        await docsToggle.ClickAsync();
        await docsPanel.GetByRole(AriaRole.Link, new() { Name = "Conflict lab", Exact = true }).ClickAsync();
        await page.WaitForURLAsync(BaseUrl + "/conflicts");
        await Assertions.Expect(page.Locator("h1")).ToHaveTextAsync("Conflict lab");
        await Assertions.Expect(docsPanel).Not.ToHaveClassAsync(new Regex("\\bbb-open\\b"));
    }

    private static IEnumerable<string> Addresses(string route)
    {
        var path = Regex.Replace(route, "\\{[^}]+\\}", "e2e-pages");
        yield return path;
        if (path.StartsWith("/live/", StringComparison.Ordinal)) yield return path + "?relay=websocket";
    }
}
