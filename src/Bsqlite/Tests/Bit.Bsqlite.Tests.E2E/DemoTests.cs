using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Bit.Bsqlite.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

namespace Bit.Bsqlite.Tests.E2E;

/// <summary>
/// The demo website, end to end: prerendered on the server, hydrated on WebAssembly, and running every
/// database it opens in the browser. Where the library suites prove the modules, these prove the composition
/// an application ships - the static web assets, the registration, prerendering, and moving a database between
/// backends from the UI.
/// </summary>
[TestClass]
public sealed class DemoTests : DemoTest
{
    /// <summary>
    /// Every page in the sidebar, discovered from the sidebar itself so a new page is covered without a new
    /// test: the server answers it prerendered, and in the browser - reached the way a reader reaches it, through
    /// the sidebar, with the runtime and the database already up - it renders against the open database without
    /// anything reaching the console.
    /// </summary>
    [TestMethod]
    public async Task Every_page_prerenders_hydrates_and_opens_its_database_without_a_console_error()
    {
        var errors = new List<string>();
        var page = await NewDemoPageAsync(errors);

        await GotoInteractiveAsync(page, "/catalog");
        await WaitForSessionAsync(page);

        var routes = await page.Locator("nav a.nav-link").EvaluateAllAsync<string[]>(
            "links => [...new Set(links.map(a => a.getAttribute('href')).filter(h => h !== null && !/^[a-z]+:/i.test(h)))]");

        Assert.IsGreaterThan(5, routes.Length, $"The sidebar lists {routes.Length} pages: {string.Join(", ", routes)}.");

        using var client = new HttpClient();
        foreach (var route in routes)
        {
            var path = "/" + route.TrimStart('/');
            using var response = await client.GetAsync(DemoUrl + path);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"{path} answered {response.StatusCode}.");
            Assert.MatchesRegex(new Regex("<title>[^<]*Bsqlite[^<]*</title>"), await response.Content.ReadAsStringAsync(), $"{path} is not prerendered with a title of its own.");
        }

        // The overview last: it runs full-bleed, without the sidebar or the session bar.
        foreach (var route in routes.OrderBy(r => r.Trim('/').Length == 0))
        {
            await page.Locator($"nav a.nav-link[href='{route}']").First.ClickAsync();
            await page.WaitForURLAsync(DemoUrl + "/" + route.TrimStart('/'));

            if (route.Trim('/').Length > 0)
            {
                await WaitForSessionAsync(page);
            }

            await Assertions.Expect(page.Locator("main[data-interactive='true'] h1").First).ToBeVisibleAsync();
        }

        Assert.AreEqual(0, errors.Count, "Console errors:" + Environment.NewLine + string.Join(Environment.NewLine, errors));
    }

    /// <summary>
    /// The prerendered HTML is the page, not a loading screen: the text is readable before WebAssembly starts,
    /// which is why every page opens its database only once it is interactive.
    /// </summary>
    [TestMethod]
    public async Task Pages_are_readable_before_WebAssembly_starts()
    {
        using var client = new HttpClient();
        var html = await client.GetStringAsync(DemoUrl + "/catalog");

        StringAssert.Contains(html, "Products and categories, read and written entirely on the client");
        StringAssert.Contains(html, "data-interactive=\"false\"");
        StringAssert.Contains(html, "data-session=\"closed\"");
    }

    [TestMethod]
    public async Task An_unknown_route_answers_404_with_the_demos_own_page()
    {
        var page = await NewDemoPageAsync([]);

        var response = await page.GotoAsync("/no-such-page");

        Assert.IsNotNull(response);
        Assert.AreEqual((int)HttpStatusCode.NotFound, response.Status);
        await Assertions.Expect(page.Locator("h1")).ToHaveTextAsync("That page is not in this demo");
    }

    /// <summary>
    /// The storage switcher's "Move data": the image is exported, imported on the target, verified, the sticky
    /// binding flips, and the catalog the page reads afterwards is the one it read before. A reload then reopens
    /// the database where it now lives - existing data outranks the configured preference.
    /// </summary>
    [TestMethod]
    public async Task Moving_the_database_to_another_backend_keeps_its_data_and_the_binding_sticks()
    {
        var errors = new List<string>();
        var page = await NewDemoPageAsync(errors);

        await GotoInteractiveAsync(page, "/catalog");
        var first = await WaitForSessionAsync(page);
        var products = page.Locator("section.card:has(h2:text-is('Products')) tbody tr");
        await Assertions.Expect(products.First).ToBeVisibleAsync();
        var count = await products.CountAsync();

        var target = first == "indexeddb" ? "cache-storage" : "indexeddb";
        await page.Locator("section.session-bar select").SelectOptionAsync(target);
        await page.Locator("section.session-bar button", new PageLocatorOptions { HasText = "Move data" }).ClickAsync();

        Assert.AreEqual(target, await WaitForSessionAsync(page, target));
        await Assertions.Expect(products).ToHaveCountAsync(count);

        await page.ReloadAsync();
        await page.WaitForSelectorAsync("main[data-interactive='true']");
        Assert.AreEqual(target, await WaitForSessionAsync(page, target), "The reload did not reopen the database where it was moved to.");
        await Assertions.Expect(products).ToHaveCountAsync(count);

        Assert.AreEqual(0, errors.Count, "Console errors:" + Environment.NewLine + string.Join(Environment.NewLine, errors));
    }

    /// <summary>
    /// The claim the demo exists to make: one AppDbContext and one migration set on both sides. The server ran
    /// the migrations with the stock provider and seeded its copy; a fresh tab ran the same migrations through
    /// UseBitBsqlite and seeded its own - and the two catalogs agree.
    /// </summary>
    [TestMethod]
    public async Task The_browser_and_the_server_run_the_same_model_and_seed_the_same_catalog()
    {
        using var client = new HttpClient();
        var server = JsonDocument.Parse(await client.GetStringAsync(DemoUrl + "/api/products")).RootElement;
        var serverSkus = server.EnumerateArray().Select(p => p.GetProperty("sku").GetString()).Order().ToArray();

        var page = await NewDemoPageAsync([]);
        await GotoInteractiveAsync(page, "/catalog");
        await WaitForSessionAsync(page);

        var products = page.Locator("section.card:has(h2:text-is('Products')) tbody tr");
        await Assertions.Expect(products).ToHaveCountAsync(serverSkus.Length);

        var browserSkus = (await products.Locator("td.mono").AllTextContentsAsync()).Select(s => s.Trim()).Order().ToArray();
        CollectionAssert.AreEqual(serverSkus, browserSkus);
    }
}
