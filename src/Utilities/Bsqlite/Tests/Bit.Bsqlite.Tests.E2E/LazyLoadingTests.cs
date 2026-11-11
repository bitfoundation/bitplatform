using Bit.Bsqlite.Tests.E2E.Infrastructure;
using Bit.Utilities.Testing;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Bsqlite.Tests.E2E;

/// <summary>
/// An app that lazy loads Bit.Bsqlite and EF Core on WebAssembly, set up as docs/getting-started.md's "Lazy loading on
/// WebAssembly" says: the lazy loading sample (<c>Samples/Lazy</c>), published - trimmed, with every assembly the guide
/// lists marked lazy, its context and migration in a library of their own that is marked too, and none of Bsqlite's
/// services registered in the browser - and driven in a browser. Its home page uses no database and downloads none of
/// them; its products page has the router download them all before it renders, then builds Bsqlite's services itself
/// through <c>GetBitBsqliteServices</c>, opens the database - migrating it - and writes to it, and a reload finds the row
/// in the browser's storage. A deep link to that page is prerendered by the server, which registers Bsqlite's services
/// with the same method and the <c>LazyAssemblyLoader</c> its router injects. Published with AOT
/// (<c>BSQLITE_E2E_LAZY_AOT=1</c>), the sample loads them all with the app, as the guide says an AOT build must, and works
/// just the same.
/// </summary>
[TestClass]
public sealed class LazyLoadingTests : DemoTest
{
    // Every assembly the sample's client marks lazy besides Bit.Bsqlite, which the harness counts by itself: the guide's
    // list, and the library with the context.
    private static readonly string[] LoadedWithBsqlite =
    [
        "Microsoft.EntityFrameworkCore", "Microsoft.EntityFrameworkCore.Relational", "Microsoft.EntityFrameworkCore.Sqlite",
        "Microsoft.Data.Sqlite", "SQLitePCLRaw.core", "Microsoft.Extensions.Caching.Abstractions",
        "Microsoft.Extensions.Caching.Memory", "Microsoft.Extensions.DependencyModel", "System.Data.Common",
        "System.Private.Xml", "System.Transactions.Local", "Bit.Bsqlite.Samples.Lazy.Data",
    ];

    private LazyLoadingSample? _sample;

    protected override async Task<string> StartHostAsync()
    {
        string? unavailableReason = null;
        try
        {
            _sample = await LazyLoadingSample.GetAsync("Bit.Bsqlite", "BSQLITE", E2EEnvironment.Configuration);
        }
        catch (Exception ex) when (ex is FileNotFoundException or InvalidOperationException or TimeoutException)
        {
            unavailableReason = $"the lazy loading sample could not be started in the {E2EEnvironment.Configuration} configuration: {ex.Message}";
        }

        if (unavailableReason is not null)
        {            // A build server must not report a suite that never ran as anything but a failure.
            if (E2EEnvironment.IsContinuousIntegration) Assert.Fail("The lazy loading tests could not run: " + unavailableReason);
            Assert.Inconclusive("The lazy loading tests could not run: " + unavailableReason);
        }

        return _sample!.BaseUrl;
    }

    [TestMethod]
    public async Task The_home_page_downloads_none_of_the_database_assemblies_and_the_products_page_downloads_each_once_before_it_renders()
    {
        var problems = new List<string>();
        var page = await NewSamplePageAsync(problems);
        var requests = _sample!.Track(page, LoadedWithBsqlite);

        await GotoSampleAsync(page, problems, "/");
        await Expect(page.Locator("h1")).ToHaveTextAsync("Lazy loading");
        _sample.AssertNotLoaded(requests);

        // In the app: the router's OnNavigateAsync downloads the assemblies, then the products page renders.
        await page.GetByRole(AriaRole.Link, new() { Name = "Products", Exact = true }).ClickAsync();
        await page.WaitForURLAsync(DemoUrl + "/products");
        await Expect(page.Locator("h1")).ToHaveTextAsync("Products");

        await AddProductAsync(page, problems, "Gadget");
        await ExpectProductsAsync(page, problems, "Gadget");

        // Back and forth: the router asks for the assemblies again, and nothing is downloaded again. The new page takes
        // the services the first one built, and the database they opened.
        await page.GetByRole(AriaRole.Link, new() { Name = "Home", Exact = true }).ClickAsync();
        await Expect(page.Locator("h1")).ToHaveTextAsync("Lazy loading");
        await page.GetByRole(AriaRole.Link, new() { Name = "Products", Exact = true }).ClickAsync();
        await Expect(page.Locator("h1")).ToHaveTextAsync("Products");
        await ExpectProductsAsync(page, problems, "Gadget");

        _sample.AssertLoadedOnce(requests);
        AssertNoProblems(problems);
    }

    [TestMethod]
    public async Task A_deep_link_to_the_products_page_is_prerendered_and_what_it_adds_once_the_browser_takes_over_survives_a_reload()
    {
        var problems = new List<string>();
        var page = await NewSamplePageAsync(problems);
        var requests = _sample!.Track(page, LoadedWithBsqlite);

        // The server prerenders the router - LazyAssemblyLoader resolved there too - and the page, which opens no
        // database there.
        var response = await GotoSampleAsync(page, problems, "/products");
        var actualValue = response?.Status;
        Assert.AreEqual(200, actualValue);
        var prerendered = await response!.TextAsync();
        Assert.Contains("<h1>Products</h1>", prerendered);
        Assert.Contains("Opening the database once the page runs in the browser", prerendered);
        Assert.Contains("data-loaded=\"false\"", prerendered);

        await AddProductAsync(page, problems, "Widget");
        await ExpectProductsAsync(page, problems, "Widget");

        _sample.AssertLoadedOnce(requests);

        // A new document: the runtime starts over, downloads the assemblies again and opens the database again, from the
        // browser's storage the row was written to.
        await ReportingProblemsAsync(problems, "The reloaded page did not run in the browser", async () =>
        {
            await page.ReloadAsync(new() { Timeout = 90_000 });
            await page.WaitForSelectorAsync(".page[data-interactive='true']", new() { Timeout = 90_000 });
        });
        await ExpectProductsAsync(page, problems, "Widget");

        AssertNoProblems(problems);
    }

    /// <summary>
    /// A page of the sample in a fresh context - so a fresh origin storage, with no database yet - whose console errors,
    /// uncaught exceptions, failed requests and error responses are all recorded in <paramref name="problems"/>: an
    /// assembly, the worker, the engine or the VFS that does not arrive is one.
    /// </summary>
    private async Task<IPage> NewSamplePageAsync(List<string> problems)
    {
        var page = await NewDemoPageAsync(problems);
        page.RequestFailed += (_, request) => { lock (problems) problems.Add($"request failed: {request.Url} ({request.Failure})"); };
        page.Response += (_, response) => { if (response.Status >= 400) lock (problems) problems.Add($"HTTP {response.Status}: {response.Url}"); };
        return page;
    }

    /// <summary>
    /// A product added through the page, which goes through the context factory the page built for itself: the first
    /// write opens the database, migrates it and inserts the row.
    /// </summary>
    private static async Task AddProductAsync(IPage page, List<string> problems, string name)
    {
        await WaitForTheDatabaseAsync(page, problems);
        await page.Locator("#name").FillAsync(name);
        await page.Locator("#add").ClickAsync();
    }

    /// <summary>The page has opened the database and lists exactly <paramref name="names"/>, read back through EF Core.</summary>
    private static async Task ExpectProductsAsync(IPage page, List<string> problems, params string[] names)
    {
        await WaitForTheDatabaseAsync(page, problems);
        await Expect(page.Locator("#products li")).ToHaveTextAsync(names);
    }

    /// <summary><see cref="DemoTest.GotoInteractiveAsync"/>, failing with what the page reported when it never runs in the browser.</summary>
    private static async Task<IResponse?> GotoSampleAsync(IPage page, List<string> problems, string path)
    {
        IResponse? response = null;
        await ReportingProblemsAsync(problems, $"{path} did not run in the browser", async () => response = await GotoInteractiveAsync(page, path));
        return response;
    }

    private static Task WaitForTheDatabaseAsync(IPage page, List<string> problems)
        => ReportingProblemsAsync(problems, "The products page did not open its database",
            () => page.WaitForSelectorAsync("#products[data-loaded='true']", new() { Timeout = 60_000 }));

    // A wait that times out says only what it waited for; why it never came is in what the page reported.
    private static async Task ReportingProblemsAsync(List<string> problems, string failure, Func<Task> wait)
    {
        try
        {
            await wait();
        }
        catch (Exception ex) when (ex is PlaywrightException or System.TimeoutException)
        {
            lock (problems) throw new global::System.InvalidOperationException($"{failure} ({ex.Message}). The page reported:{Environment.NewLine}{string.Join(Environment.NewLine, problems)}", ex);
        }
    }

    private static void AssertNoProblems(List<string> problems)
    {
        lock (problems) Assert.IsEmpty(problems, "the page reported:" + Environment.NewLine + string.Join(Environment.NewLine, problems));
    }
}
