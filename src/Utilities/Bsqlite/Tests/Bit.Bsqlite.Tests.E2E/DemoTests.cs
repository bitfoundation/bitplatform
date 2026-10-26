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
    /// The documentation pages that open no database of this tab: they describe the setup, or the MCP server,
    /// whose labs the server answers. Every other page in the docs panel runs against the database and opens
    /// with the session bar.
    /// </summary>
    private static readonly string[] PagesWithoutADatabase = ["/getting-started", "/mcp-server"];

    /// <summary>
    /// Every page in the docs panel, discovered from the panel itself so a new page is covered without a new
    /// test: the server answers it prerendered, and in the browser - reached the way a reader reaches it, through
    /// the panel, with the runtime and the database already up - it renders against the open database without
    /// anything reaching the console. The landing page last, through the header, with its live window open.
    /// </summary>
    [TestMethod]
    public async Task Every_page_prerenders_hydrates_and_opens_its_database_without_a_console_error()
    {
        var errors = new List<string>();
        var page = await NewDemoPageAsync(errors);

        await GotoInteractiveAsync(page, "/catalog");
        await WaitForSessionAsync(page);

        var routes = await page.Locator(".bb-docs-sidebar .bb-docs-section a").EvaluateAllAsync<string[]>(
            "links => [...new Set(links.map(a => a.getAttribute('href')).filter(h => h !== null && !/^[a-z]+:/i.test(h)))]");

        Assert.IsGreaterThan(5, routes.Length, $"The docs panel lists {routes.Length} pages: {string.Join(", ", routes)}.");

        using var client = new HttpClient();
        foreach (var route in routes.Append("/"))
        {
            var path = "/" + route.TrimStart('/');
            using var response = await client.GetAsync(DemoUrl + path);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"{path} answered {response.StatusCode}.");
            Assert.MatchesRegex(new Regex("<title>[^<]*Bsqlite[^<]*</title>"), await response.Content.ReadAsStringAsync(), $"{path} is not prerendered with a title of its own.");
        }

        foreach (var route in routes)
        {
            await page.Locator($".bb-docs-sidebar a[href='{route}']").First.ClickAsync();
            await page.WaitForURLAsync(DemoUrl + "/" + route.TrimStart('/'));

            if (PagesWithoutADatabase.Contains(route) is false)
            {
                await WaitForSessionAsync(page);
            }

            await Assertions.Expect(page.Locator(".page[data-interactive='true'] main h1").First).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator(".bb-docs-sidebar a.active")).ToHaveAttributeAsync("href", route);
        }

        // The landing page is not in the docs panel; the header's Home link leads to it. Its hero window is a live
        // query over this tab's database.
        await page.Locator(".bb-nav a[href='/']").ClickAsync();
        await page.WaitForURLAsync(DemoUrl + "/");
        await Assertions.Expect(page.Locator(".page[data-interactive='true'] main h1").First).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator("[data-live='open'] .bq-hero-rows li").First).ToBeVisibleAsync(new() { Timeout = 60_000 });

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
        await Assertions.Expect(page.Locator(".bb-error-404 h1")).ToHaveTextAsync("404");
        await Assertions.Expect(page.Locator(".bb-error-404")).ToContainTextAsync("That page is not in this demo");
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
        var products = page.Locator("table[data-rows='products'] tbody tr");
        await Assertions.Expect(products.First).ToBeVisibleAsync();
        var count = await products.CountAsync();

        var target = first == "indexeddb" ? "cache-storage" : "indexeddb";
        await page.Locator("section[data-session] select").SelectOptionAsync(target);
        await page.Locator("section[data-session] button", new PageLocatorOptions { HasText = "Move data" }).ClickAsync();

        Assert.AreEqual(target, await WaitForSessionAsync(page, target));
        await Assertions.Expect(products).ToHaveCountAsync(count);

        // The reload needs the copy to outlive the worker that wrote it, which Cache Storage does not do in
        // Playwright's WebKit - the target there, since that WebKit opens on IndexedDB (see CacheStorageTests).
        SkipWhen(BrowserName == "webkit" && target == "cache-storage",
            "Playwright's WebKit discards Cache Storage entries once no context holds the cache open");

        await page.ReloadAsync();
        await page.WaitForSelectorAsync(".page[data-interactive='true']");
        Assert.AreEqual(target, await WaitForSessionAsync(page, target), "The reload did not reopen the database where it was moved to.");
        await Assertions.Expect(products).ToHaveCountAsync(count);

        Assert.AreEqual(0, errors.Count, "Console errors:" + Environment.NewLine + string.Join(Environment.NewLine, errors));
    }

    /// <summary>
    /// The session bar used the moment it names a backend - while the page's own first load is still running
    /// against the database - lands where it was asked to. Nothing the page was doing may reopen the database
    /// on the old binding while it is being rebound, which would leave the session on one backend, the binding
    /// on another, and the next reload on a database the reader never saw.
    /// </summary>
    [TestMethod]
    public async Task Switching_backends_while_the_page_is_still_loading_lands_where_it_was_asked_to()
    {
        // Not asserted: the load the switch cuts off fails on the closing worker, and says so in the console.
        var page = await NewDemoPageAsync([]);

        await GotoInteractiveAsync(page, "/catalog");
        var first = await WaitForSessionAsync(page);
        var target = first == "indexeddb" ? "cache-storage" : "indexeddb";

        // Reloaded the moment the database opens, so the first load - migrations, the seed and the first-use
        // lock - is cut off, and runs again after the reload while the bar is used.
        await page.ReloadAsync();
        await page.WaitForSelectorAsync(".page[data-interactive='true']");
        await WaitForSessionAsync(page);
        await page.Locator("section[data-session] select").SelectOptionAsync(target);
        await page.Locator("section[data-session] button", new PageLocatorOptions { HasText = "Start empty" }).ClickAsync();

        Assert.AreEqual(target, await WaitForSessionAsync(page, target));
        await Assertions.Expect(page.Locator("table[data-rows='products'] tbody tr").First).ToBeVisibleAsync();
        Assert.AreEqual(target, await page.EvaluateAsync<string>("() => localStorage.getItem('bit-bsqlite.binding.app.db')"));

        await page.ReloadAsync();
        await page.WaitForSelectorAsync(".page[data-interactive='true']");
        Assert.AreEqual(target, await WaitForSessionAsync(page, target), "The reload did not reopen the database where it was switched to.");
        await Assertions.Expect(page.Locator("#blazor-error-ui")).ToBeHiddenAsync();
    }

    /// <summary>
    /// A database on the in-memory backend lives in the worker's own heap, out of reach of the admin API a move
    /// is built from: a move onto it would delete the source and open an empty database. The bar offers only
    /// "Start empty" there, and says why.
    /// </summary>
    [TestMethod]
    public async Task The_session_bar_does_not_offer_to_move_data_onto_the_in_memory_backend()
    {
        var page = await NewDemoPageAsync([]);

        await GotoInteractiveAsync(page, "/storage");
        await WaitForSessionAsync(page);

        var session = page.Locator("section[data-session]");
        await session.Locator("select").SelectOptionAsync("in-memory");

        await Assertions.Expect(session.Locator("button", new LocatorLocatorOptions { HasText = "Move data" })).ToBeDisabledAsync();
        await Assertions.Expect(session.Locator("button", new LocatorLocatorOptions { HasText = "Start empty" })).ToBeEnabledAsync();
        await Assertions.Expect(session.Locator(".bq-session-status[role=note]")).ToContainTextAsync("Start empty instead");
    }

    /// <summary>
    /// A write the engine refuses is the demo working, not failing: the page names SQLite's reason rather than
    /// the "see the inner exception" of the DbUpdateException around it, and Blazor's "An unhandled error has
    /// occurred" bar stays down.
    /// </summary>
    [TestMethod]
    public async Task A_refused_write_shows_SQLites_reason_and_no_error_bar()
    {
        var page = await NewDemoPageAsync([]);

        await GotoInteractiveAsync(page, "/catalog");
        await WaitForSessionAsync(page);

        // TL-100 is on an order, and OrderLine -> Product is Restrict.
        var products = page.Locator("table[data-rows='products'] tbody tr");
        await products.Filter(new LocatorFilterOptions { HasText = "TL-100" })
            .Locator("button", new LocatorLocatorOptions { HasText = "Delete" })
            .ClickAsync();

        await Assertions.Expect(page.Locator(".bb-note-danger[role=alert]")).ToHaveTextAsync("FOREIGN KEY constraint failed");
        await Assertions.Expect(page.Locator("#blazor-error-ui")).ToBeHiddenAsync();
    }

    /// <summary>
    /// The order editor works on a copy: an edit shows in the grid once it is saved, not while it is typed, and
    /// not at all when the reader moves on without saving.
    /// </summary>
    [TestMethod]
    public async Task An_order_edited_but_not_saved_leaves_the_grid_as_it_was()
    {
        var page = await NewDemoPageAsync([]);

        await GotoInteractiveAsync(page, "/orders");
        await WaitForSessionAsync(page);

        var orders = page.Locator("table[data-rows='orders'] tbody tr");
        var edited = orders.Filter(new LocatorFilterOptions { HasText = "SO-1043" });
        await edited.ClickAsync();
        var before = await edited.InnerTextAsync();

        // Set the way a reader's edit ends - the value, then change - since not every browser Playwright
        // drives has a native date widget to fill.
        await page.Locator(".bb-stage form.bq-form label", new PageLocatorOptions { HasText = "Ship by" }).Locator("input").EvaluateAsync(
            "input => { input.value = '2030-01-01'; input.dispatchEvent(new Event('change', { bubbles: true })); }");
        await orders.Filter(new LocatorFilterOptions { HasText = "SO-1042" }).ClickAsync();
        await Assertions.Expect(page.Locator(".bb-stage .bq-panel-title").First).ToHaveTextAsync("SO-1042");

        Assert.AreEqual(before, await edited.InnerTextAsync());
    }

    /// <summary>
    /// A file that is not a SQLite database is refused when it is picked, before it can replace the database
    /// and leave every page failing to open it.
    /// </summary>
    [TestMethod]
    public async Task A_file_that_is_not_a_SQLite_database_is_refused_before_it_replaces_anything()
    {
        var page = await NewDemoPageAsync([]);

        await GotoInteractiveAsync(page, "/admin");
        await WaitForSessionAsync(page);

        await page.Locator("section:has(h3:has-text('Seed a tab from a file')) input[type=file]").SetInputFilesAsync(new FilePayload
        {
            Name = "notes.db",
            MimeType = "application/octet-stream",
            Buffer = "these are notes, not a database"u8.ToArray(),
        });

        await Assertions.Expect(page.Locator(".bb-note-danger[role=alert]")).ToContainTextAsync("notes.db is not a SQLite database");
        await Assertions.Expect(page.Locator("button", new PageLocatorOptions { HasText = "Import and reopen" })).ToBeDisabledAsync();
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

        var products = page.Locator("table[data-rows='products'] tbody tr");
        await Assertions.Expect(products).ToHaveCountAsync(serverSkus.Length);

        // The SKU is the grid's first column.
        var browserSkus = (await products.Locator("td:first-child").AllTextContentsAsync()).Select(s => s.Trim()).Order().ToArray();
        CollectionAssert.AreEqual(serverSkus, browserSkus);
    }
}
