using Bit.Bcrdt.Tests.E2E.Infrastructure;
using Bit.Utilities.Testing;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Bcrdt.Tests.E2E;

/// <summary>
/// An app that lazy loads Bit.Bcrdt on WebAssembly, set up as the README's "Lazy loading on WebAssembly" section says:
/// the lazy loading sample (<c>Samples/Lazy</c>), published - trimmed, with Bit.Bcrdt marked lazy and none of its
/// services registered in the browser - and driven in a browser. Its home page collaborates on nothing and never
/// downloads <c>Bit.Bcrdt.wasm</c>; its notes have the router download it before they render, and their layout
/// configures the collaboration service through <c>GetBcrdtCollaborationService(...)</c> - the WebSocket transport to the
/// sample's relay and the IndexedDB store - which <c>BcrdtCollaborationScope</c> opens the notes through. A second
/// browser context, another person, edits the same notes with them, and the page's own code takes the same service from
/// <c>GetBcrdtCollaborationService()</c>. A deep link to the notes is prerendered by the server, which registers Bcrdt's
/// services and the <c>LazyAssemblyLoader</c> its router injects. Published with AOT (<c>BCRDT_E2E_LAZY_AOT=1</c>), the
/// sample loads Bit.Bcrdt with the app, as the README says an AOT build must, and works just the same.
/// </summary>
[TestClass]
public sealed class LazyLoadingTests : BcrdtTest
{
    private const string Written = "Typed in the first browser.";
    private const string Answered = " Answered in the second.";

    private readonly List<List<string>> _problems = [];
    private LazyLoadingSample? _sample;

    protected override string BaseUrl => _sample!.BaseUrl;

    [TestInitialize]
    public async Task StartSampleAsync()
    {
        try
        {
            _sample = await LazyLoadingSample.GetAsync("Bit.Bcrdt", "BCRDT", E2EEnvironment.Configuration);
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
    public async Task The_home_page_never_downloads_Bit_Bcrdt_and_the_notes_download_it_once_before_they_open()
    {
        var page = await NewTrackedPageAsync();
        var requests = _sample!.Track(page);

        await GotoAsync(page, "/");
        await WaitForInteractiveAsync(page);
        _sample.AssertNotLoaded(requests);

        // In the app: the router's OnNavigateAsync downloads the assembly, then the notes render, their layout configures
        // the service and the scope opens the document through it.
        await page.GetByRole(AriaRole.Link, new() { Name = "Notes", Exact = true }).ClickAsync();
        await page.WaitForURLAsync(BaseUrl + "/notes");
        await Expect(page.Locator("h1")).ToHaveTextAsync("Notes");
        await WaitForSyncedAsync(page);

        await AssertTheNotesAreSharedAsync(page, await OpenNotesAsync("/notes"));

        // Back and forth: the router asks for the assembly again, and nothing is downloaded again; the layout gets the
        // service it configured the first time back, and the notes open again through it with everything typed so far.
        await page.GetByRole(AriaRole.Link, new() { Name = "Home", Exact = true }).ClickAsync();
        await Expect(page.Locator("h1")).ToHaveTextAsync("Lazy loading");
        await page.GetByRole(AriaRole.Link, new() { Name = "Notes", Exact = true }).ClickAsync();
        await Expect(page.Locator("h1")).ToHaveTextAsync("Notes");
        await WaitForSyncedAsync(page);
        await Expect(Editor(page)).ToHaveValueAsync(Written + Answered);

        _sample.AssertLoadedOnce(requests);
        AssertNoProblems();
    }

    [TestMethod]
    public async Task A_deep_link_to_the_notes_is_prerendered_and_they_open_once_the_browser_takes_over()
    {
        var room = "e2e-" + Guid.NewGuid().ToString("N")[..12];
        var page = await NewTrackedPageAsync();
        var requests = _sample!.Track(page);

        // The server prerenders the router - LazyAssemblyLoader resolved there too - and the notes: their layout gets the
        // server's own registration back from GetBcrdtCollaborationService, and the scope shows its loading content,
        // as it opens a document only once the page runs in the browser.
        var response = await GotoAsync(page, $"/notes/{room}");
        Assert.AreEqual(200, response?.Status);
        var prerendered = await response!.TextAsync();
        Assert.Contains($"Opening notes-{room}", prerendered);

        await WaitForInteractiveAsync(page);
        await WaitForSyncedAsync(page);

        await AssertTheNotesAreSharedAsync(page, await OpenNotesAsync($"/notes/{room}"));

        _sample.AssertLoadedOnce(requests);
        AssertNoProblems();
    }

    [TestMethod]
    public async Task Two_tabs_of_one_browser_share_the_notes_directly()
    {
        var room = "e2e-" + Guid.NewGuid().ToString("N")[..12];
        var browser = await NewContextAsync();

        var first = await NewPageAsync(browser);
        _problems.Add(TrackProblems(first, BaseUrl));
        await GotoAsync(first, $"/notes/{room}");
        await WaitForInteractiveAsync(first);
        await WaitForSyncedAsync(first);

        // The second tab's socket to the relay is answered by nobody, so it sits alone in a room of its own: whatever
        // reaches it comes from the first tab, over the BroadcastChannel the layout's UseCrossTabSync() opened - in the
        // trimmed, lazy loaded library.
        var second = await NewPageAsync(browser);
        _problems.Add(TrackProblems(second, BaseUrl));
        await second.RouteWebSocketAsync(new System.Text.RegularExpressions.Regex("/ws/bcrdt/"), _ => { });
        await GotoAsync(second, $"/notes/{room}");
        await WaitForInteractiveAsync(second);
        await WaitForSyncedAsync(second);

        await TypeAtEndAsync(Editor(first), Written);
        await Expect(Editor(second)).ToHaveValueAsync(Written);

        await TypeAtEndAsync(Editor(second), Answered);
        await Expect(Editor(first)).ToHaveValueAsync(Written + Answered);

        // Presence crosses only the network, and the second tab has none.
        await Expect(Presence(first)).Not.ToContainTextAsync(await NameAsync(second));
        AssertNoProblems();
    }

    /// <summary>
    /// Two people on the same notes, each in a browser of their own: each lists the other, and what either types reaches
    /// the other - through the trimmed library's WebSocket transport, which the layout configured without a
    /// registration - and the page's own count, taken through <c>GetBcrdtCollaborationService()</c>, reads the document
    /// the scope opened.
    /// </summary>
    private static async Task AssertTheNotesAreSharedAsync(IPage first, IPage second)
    {
        await Expect(Presence(first)).ToContainTextAsync(await NameAsync(second));
        await Expect(Presence(second)).ToContainTextAsync(await NameAsync(first));

        await TypeAtEndAsync(Editor(first), Written);
        await Expect(Editor(second)).ToHaveValueAsync(Written);

        await TypeAtEndAsync(Editor(second), Answered);
        await Expect(Editor(first)).ToHaveValueAsync(Written + Answered);

        await first.GetByRole(AriaRole.Button, new() { Name = "Count the characters" }).ClickAsync();
        await Expect(first.Locator("#count")).ToHaveTextAsync($"{(Written + Answered).Length} characters");
    }

    /// <summary>Opens the notes at <paramref name="path"/> in a browser context of its own - another person's browser.</summary>
    private async Task<IPage> OpenNotesAsync(string path)
    {
        var page = await NewTrackedPageAsync();
        await GotoAsync(page, path);
        await WaitForInteractiveAsync(page);
        await WaitForSyncedAsync(page);
        return page;
    }

    /// <summary>A page in a context of its own, whose console errors, uncaught exceptions and failed requests fail the test.</summary>
    private async Task<IPage> NewTrackedPageAsync()
    {
        var page = await NewPageAsync(await NewContextAsync());
        _problems.Add(TrackProblems(page, BaseUrl));
        return page;
    }

    private void AssertNoProblems()
    {
        string[] found = [.. _problems.SelectMany(problems => { lock (problems) return problems.ToArray(); })];
        Assert.IsEmpty(found, string.Join(Environment.NewLine, found));
    }

    /// <summary>
    /// Waits until the notes are open and connected to the relay. The status only renders once the scope has opened the
    /// document, which happens once the page runs in the browser, and reads "synced" once the handshake is done.
    /// </summary>
    private static Task WaitForSyncedAsync(IPage page) =>
        Expect(page.Locator("#status")).ToHaveTextAsync("synced", new() { Timeout = 60_000 });

    private static ILocator Editor(IPage page) => page.Locator(".notes textarea");

    private static ILocator Presence(IPage page) => page.Locator(".notes .presence");

    /// <summary>The guest name the tab gave itself.</summary>
    private static Task<string> NameAsync(IPage page) => page.Locator("#me").InnerTextAsync();
}
