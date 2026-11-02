using Bit.BlazeDb.Tests.E2E.Infrastructure;
using Bit.Utilities.Testing;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.BlazeDb.Tests.E2E;

/// <summary>
/// An app that lazy loads Bit.BlazeDb on WebAssembly, set up as the README's "Lazy loading on WebAssembly" section says:
/// the lazy loading sample (<c>Samples/Lazy</c>), published - trimmed, with Bit.BlazeDb marked lazy - and driven in a
/// browser. Its client registers its own database service, <c>AppDatabase</c>, in <c>Program.cs</c> before the assembly is
/// there, keeping the engine's types in the nested class the README shows, and the layout every page shares creates a
/// <c>[BlazeDbTable]</c> type and reads its title. Its home page never downloads <c>Bit.BlazeDb.wasm</c>; its todos page has
/// the router download it before the page renders, then opens the tab's database through <c>AppDatabase</c> - on OPFS, or on
/// IndexedDB where the context cannot write OPFS - and writes rows through the generated table and indexes. A deep link to
/// the todos page is prerendered by the server, which registers the <c>LazyAssemblyLoader</c> its router injects, and the
/// rows it writes survive a reload. Published with AOT (<c>BLAZEDB_E2E_LAZY_AOT=1</c>), the sample loads Bit.BlazeDb with
/// the app, as the README says an AOT build must, and works just the same.
/// </summary>
[TestClass]
public sealed class LazyLoadingTests : BlazeDbTest
{
    private LazyLoadingSample? _sample;

    protected override string BaseUrl => _sample!.BaseUrl;

    [TestInitialize]
    public async Task StartSampleAsync()
    {
        try
        {
            _sample = await LazyLoadingSample.GetAsync("Bit.BlazeDb", "BLAZEDB", E2EEnvironment.Configuration);
        }
        catch (Exception ex) when (ex is FileNotFoundException or InvalidOperationException or TimeoutException)
        {
            var reason = $"the lazy loading sample could not be started in the {E2EEnvironment.Configuration} configuration: {ex.Message}";
            // A build server must not report a suite that never ran as anything but a failure.
            if (E2EEnvironment.IsContinuousIntegration) Assert.Fail("The browser tests could not run: " + reason);
            Assert.Inconclusive("The browser tests could not run: " + reason);
        }
    }

    [TestMethod]
    public async Task The_home_page_never_downloads_Bit_BlazeDb_and_the_todos_page_downloads_it_once_before_it_opens_the_database()
    {
        var page = await (await NewContextAsync()).NewPageAsync();
        var requests = _sample!.Track(page);
        var problems = TrackProblems(page, BaseUrl);

        // AppDatabase was registered and the app started without the assembly: the home page runs in the browser. Its
        // layout, which turned the page interactive, created a table type and read its title in that same render.
        await NavigateAsync(page, "/");
        await WaitForInteractiveAsync(page, problems);
        _sample.AssertNotLoaded(requests);
        await Expect(page.Locator("#suggestion")).ToHaveTextAsync("Water the plants");

        // In the app: the router's OnNavigateAsync downloads the assembly, then the todos page opens the database.
        await page.GetByRole(AriaRole.Link, new() { Name = "Todos", Exact = true }).ClickAsync();
        await page.WaitForURLAsync(BaseUrl + "/todos");
        await Expect(page.Locator("h1")).ToHaveTextAsync("Todos");
        await WaitForTodosAsync(page, problems);

        await AddAsync(page, "Buy milk");
        await Expect(page.Locator("#open")).ToHaveTextAsync("1 open");

        // Home and back: the router asks for the assembly again, and nothing is downloaded again. AppDatabase, a
        // singleton, hands the new page the database it opened the first time, row and all.
        await page.GetByRole(AriaRole.Link, new() { Name = "Home", Exact = true }).ClickAsync();
        await Expect(page.Locator("h1")).ToHaveTextAsync("Lazy loading");
        await page.GetByRole(AriaRole.Link, new() { Name = "Todos", Exact = true }).ClickAsync();
        await Expect(page.Locator("h1")).ToHaveTextAsync("Todos");
        await WaitForTodosAsync(page, problems);

        await Expect(page.Locator("#todos .title")).ToHaveTextAsync(["Buy milk"]);
        await MarkDoneAsync(page, "Buy milk", openAfter: 0);

        _sample.AssertLoadedOnce(requests);
        AssertNoProblems(problems);
    }

    [TestMethod]
    public async Task A_deep_link_to_the_todos_page_is_prerendered_and_the_rows_it_writes_survive_a_reload()
    {
        var page = await (await NewContextAsync()).NewPageAsync();
        var requests = _sample!.Track(page);
        var problems = TrackProblems(page, BaseUrl);

        // The server prerenders the router - LazyAssemblyLoader resolved there too - the layout with its table type, and
        // the page, which opens nothing there: it says the database is opening until it runs in the browser.
        var response = await NavigateAsync(page, "/todos");
        Assert.AreEqual(200, response?.Status);
        var prerendered = await response!.TextAsync();
        Assert.Contains("data-storage=\"Opening\"", prerendered);
        Assert.Contains("Water the plants", prerendered);

        var storage = await WaitForTodosAsync(page, problems);

        await AddAsync(page, "Pay the rent");
        await AddAsync(page, "Call the plumber");
        await MarkDoneAsync(page, "Pay the rent", openAfter: 1);

        _sample.AssertLoadedOnce(requests);

        // A reload ends this document, and the writer lock with it, and boots the next one: the rows come back out of
        // storage, replayed from the write-ahead log, rather than out of anything the old page kept.
        await page.ReloadAsync(new PageReloadOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 90_000 });
        Assert.AreEqual(storage, await WaitForTodosAsync(page, problems), "the reloaded page should open the database where the first one did");

        await Expect(page.Locator("#todos .title")).ToHaveTextAsync(["Pay the rent", "Call the plumber"]);
        await Expect(Checkbox(page, "Pay the rent")).ToBeCheckedAsync();
        await Expect(Checkbox(page, "Call the plumber")).Not.ToBeCheckedAsync();
        await Expect(page.Locator("#open")).ToHaveTextAsync("1 open");

        AssertNoProblems(problems);
    }

    /// <summary>
    /// Waits until the todos page has opened the tab's database, and returns where: OPFS, or IndexedDB in a context that
    /// cannot write OPFS - WebKit's Playwright contexts are ephemeral, like a private window. Chromium and Firefox offer
    /// OPFS, so there the page has to use it.
    /// </summary>
    private static async Task<string> WaitForTodosAsync(IPage page, List<string> problems)
    {
        await WaitForAppAsync(page, problems,
            "() => { const database = document.querySelector('#database'); return database && database.dataset.storage !== 'Opening'; }");

        var database = page.Locator("#database");
        var storage = await database.GetAttributeAsync("data-storage") ?? string.Empty;
        Assert.AreNotEqual("Failed", storage, await database.TextContentAsync());
        if (E2EEnvironment.Browser != "webkit") Assert.AreEqual("OPFS", storage, $"{E2EEnvironment.Browser} offers OPFS, so the page should keep its rows there.");

        return storage;
    }

    /// <summary>
    /// Adds a todo through the page's form, and waits until it is listed - read back through the generated ordered index -
    /// and flushed to storage.
    /// </summary>
    private static async Task AddAsync(IPage page, string title)
    {
        await page.Locator("#new-title").FillAsync(title);
        await page.GetByRole(AriaRole.Button, new() { Name = "Add", Exact = true }).ClickAsync();
        await Expect(page.Locator("#todos li", new() { HasText = title })).ToBeVisibleAsync();
        // Rendered with the row, or after it: the page flushes after the write.
        await Expect(page.Locator("#saved")).ToHaveTextAsync("saved");
    }

    /// <summary>
    /// Marks a todo done. The page writes it as a new row, which moves it to the other key of the Done index the open
    /// count reads, and flushes it.
    /// </summary>
    private static async Task MarkDoneAsync(IPage page, string title, int openAfter)
    {
        await Checkbox(page, title).CheckAsync();
        await Expect(page.Locator("#open")).ToHaveTextAsync($"{openAfter} open");
        await Expect(page.Locator("#saved")).ToHaveTextAsync("saved");
    }

    private static ILocator Checkbox(IPage page, string title) =>
        page.Locator("#todos li", new() { HasText = title }).Locator("input[type=checkbox]");

    /// <summary>Opens <paramref name="path"/> of the sample. The base class's GotoAsync waits for the demo's storage badge, which the sample has not.</summary>
    private Task<IResponse?> NavigateAsync(IPage page, string path) =>
        page.GotoAsync(BaseUrl + path, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 90_000 });

    private static Task WaitForInteractiveAsync(IPage page, List<string> problems) =>
        WaitForAppAsync(page, problems, "() => document.querySelector('.page')?.dataset.interactive === 'true'");

    /// <summary>
    /// Waits for the app to get where <paramref name="condition"/> says. An app that cannot - one whose startup named the
    /// engine before it was downloaded, say - fails with what the page reported rather than a bare timeout.
    /// </summary>
    private static async Task WaitForAppAsync(IPage page, List<string> problems, string condition)
    {
        try
        {
            await page.WaitForFunctionAsync(condition, null, new PageWaitForFunctionOptions { Timeout = 90_000 });
        }
        catch (TimeoutException)
        {
            AssertNoProblems(problems);
            throw;
        }
    }

    /// <summary>
    /// Every console error, unhandled exception, failed request and error response of the sample the page reports -
    /// stricter than <see cref="BlazeDbTest.CollectErrors"/>: an app that lazy loads must run clean, and a request for the
    /// assembly or a browser module that fails is exactly what it would get wrong.
    /// </summary>
    private static List<string> TrackProblems(IPage page, string origin)
    {
        var problems = new List<string>();
        void Add(string problem) { lock (problems) problems.Add(problem); }

        page.Console += (_, message) => { if (message.Type == "error") Add($"console error: {message.Text}"); };
        page.PageError += (_, error) => Add("unhandled error: " + error);
        page.RequestFailed += (_, request) =>
        {
            if (request.Url.StartsWith(origin, StringComparison.OrdinalIgnoreCase)) Add($"request failed: {request.Url} ({request.Failure})");
        };
        page.Response += (_, response) =>
        {
            if (response.Status >= 400 && response.Url.StartsWith(origin, StringComparison.OrdinalIgnoreCase)) Add($"HTTP {response.Status}: {response.Url}");
        };

        return problems;
    }

    private static void AssertNoProblems(List<string> problems)
    {
        string[] found;
        lock (problems) found = [.. problems];
        Assert.IsEmpty(found, "the page reported:" + Environment.NewLine + string.Join(Environment.NewLine, found));
    }
}
