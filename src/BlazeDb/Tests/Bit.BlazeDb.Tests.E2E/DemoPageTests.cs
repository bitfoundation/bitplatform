using System.Net;
using Bit.BlazeDb.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

namespace Bit.BlazeDb.Tests.E2E;

/// <summary>
/// Every page of the demo, reached the way a visitor reaches it, renders without an error - and the host
/// page around them is what the hosting model says it is: nothing prerendered, a boot screen while the
/// runtime downloads, a styled 404 for a route that does not exist.
/// </summary>
[TestClass]
public class DemoPageTests : BlazeDbTest
{
    /// <summary>The site map, page by page: the route, and a heading the page renders once it is running.</summary>
    private static readonly (string Path, string Heading)[] _pages =
    [
        ("todos", "A working app on the real engine"),
        ("schema", "Schema comes from your types"),
        ("indexes", "Two index kinds, both maintained for you"),
        ("queries", "Querying without a query language"),
        ("efcore", "The same engine, reached through LINQ"),
        ("transactions", "One writer, one ambient batch"),
        ("durability", "Crash it on purpose"),
        ("storage", "Four methods are the entire storage contract"),
        ("serialization", "See the bytes a row becomes"),
        ("errors", "Every way BlazeDb says no"),
        ("bench", "Measured here, in your browser, right now"),
        ("roadmap", "What ships, what is next, and what never will"),
        ("mcp-server", "An MCP server for AI agents"),
    ];

    [TestMethod]
    public async Task Every_page_renders_through_the_navigation_without_an_error()
    {
        var page = await OpenAsync("/");
        var errors = CollectErrors(page);
        var misses = new List<string>();

        foreach (var (path, heading) in _pages)
        {
            await page.Locator($"a.nav-link[href='{path}']").First.ClickAsync();
            await page.WaitForURLAsync($"**/{path}");

            try
            {
                await Assertions.Expect(page.Locator("h1")).ToHaveTextAsync(heading);
            }
            catch (PlaywrightException ex)
            {
                misses.Add($"/{path}: {ex.Message.Split('\n')[0]}");
            }

            if (await page.Locator("#blazor-error-ui").IsVisibleAsync()) misses.Add($"/{path}: the error UI is showing.");
        }

        Assert.IsEmpty(misses, string.Join(Environment.NewLine, misses));
        Assert.IsEmpty(errors, string.Join(Environment.NewLine, errors));
    }

    [TestMethod]
    public async Task A_deep_link_opens_its_page_directly()
    {
        var page = await OpenAsync("/efcore");

        await Assertions.Expect(page.Locator("h1")).ToHaveTextAsync("The same engine, reached through LINQ");
        await Assertions.Expect(page.Locator(".crumbs .here")).ToHaveTextAsync("EF Core");
    }

    [TestMethod]
    public async Task An_unknown_route_answers_404_with_the_site_s_own_page()
    {
        var page = await (await NewContextAsync()).NewPageAsync();

        var response = await page.GotoAsync(BaseUrl + "/no-such-page", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 90_000 });
        await WaitForDatabaseAsync(page);

        Assert.AreEqual((int)HttpStatusCode.NotFound, response!.Status);
        await Assertions.Expect(page.Locator("h1")).ToContainTextAsync("That page does not exist");
    }

    [TestMethod]
    public async Task The_host_page_carries_the_boot_screen_and_no_prerendered_page()
    {
        using var http = new HttpClient();

        var html = await http.GetStringAsync(BaseUrl + "/todos");

        Assert.Contains("class=\"boot\"", html);
        Assert.Contains("_framework/blazor.web.js", html);
        Assert.DoesNotContain("class=\"shell", html, "Nothing is prerendered: the database a page shows lives only in the browser.");
    }

    [TestMethod]
    public async Task The_browser_modules_are_served_as_static_web_assets()
    {
        using var http = new HttpClient();

        foreach (var module in BrowserModules.All)
        {
            using var response = await http.GetAsync($"{BaseUrl}/_content/Bit.BlazeDb.Browser/{module}.js");

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, module);
            Assert.Contains("javascript", response.Content.Headers.ContentType?.MediaType ?? string.Empty, module);
        }
    }
}
