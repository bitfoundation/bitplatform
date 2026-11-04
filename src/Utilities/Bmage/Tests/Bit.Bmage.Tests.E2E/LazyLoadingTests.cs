using System.Text.Json;
using System.Text.RegularExpressions;
using Bit.Bmage.Tests.E2E.Infrastructure;
using Bit.Utilities.Testing;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Bmage.Tests.E2E;

/// <summary>
/// An app that lazy loads Bmage on WebAssembly, set up as docs/getting-started.md's "Lazy loading Bmage on WebAssembly"
/// says: the lazy loading sample (<c>Samples/Lazy</c>), published - trimmed, with Bit.Bmage and Bit.Bmage.Blazor marked
/// lazy and none of Bmage's services registered in the browser - and driven in a browser. Its home page edits nothing
/// and never downloads <c>Bit.Bmage.wasm</c> or <c>Bit.Bmage.Blazor.wasm</c>; its editor page has the router download
/// both before it renders, and the <c>BmageEditor</c> there, which finds no registration, builds its services itself
/// and opens, turns and exports an image through them. A deep link to the editor is prerendered by the server, which
/// registers Bmage's services and the <c>LazyAssemblyLoader</c> its router injects. Published with AOT
/// (<c>BMAGE_E2E_LAZY_AOT=1</c>), the sample loads Bmage with the app, as the guide says an AOT build must, and works
/// just the same.
/// </summary>
[TestClass]
public sealed class LazyLoadingTests : DemoTest
{
    // Published before the first test, as the suite builds the demo and the hosting samples before theirs - only a
    // publish trims the client and writes the lazy marks into its boot configuration - unless the run says it is
    // published already (BMAGE_E2E_SKIP_BUILD=1, as CI does), drives a running copy, or drives an AOT publish, which needs
    // the wasm-tools workload and far longer than a test should wait.
    private static readonly Lazy<Task> _published = new(PublishAsync);

    private LazyLoadingSample? _sample;

    protected override async Task<string> StartHostAsync()
    {
        await _published.Value;
        _sample = await LazyLoadingSample.GetAsync("Bit.Bmage", "BMAGE", E2EEnvironment.Configuration);
        return _sample.BaseUrl;
    }

    [TestMethod]
    public async Task The_home_page_never_downloads_Bmage_and_the_editor_page_downloads_it_once_before_the_editor_renders()
    {
        var page = await NewPageAsync();
        var problems = TrackProblems(page);
        var requests = _sample!.Track(page, "Bit.Bmage.Blazor");

        await GotoAsync(page, "");
        _sample.AssertNotLoaded(requests);

        // In the app: the router's OnNavigateAsync downloads both assemblies, then the editor's page renders.
        await page.GetByRole(AriaRole.Link, new() { Name = "Editor", Exact = true }).ClickAsync();
        await page.WaitForURLAsync(BaseUrl + "/editor");
        await Expect(page.Locator("h1")).ToHaveTextAsync("Editor");

        await AssertTheEditorWorksAsync(page, problems);

        // Back and forth: the router asks for the assemblies again, and nothing is downloaded again. The new editor takes
        // the services the first one built, which last as long as the browser connection.
        await page.GetByRole(AriaRole.Link, new() { Name = "Home", Exact = true }).ClickAsync();
        await Expect(page.Locator("h1")).ToHaveTextAsync("Lazy loading");
        await page.GetByRole(AriaRole.Link, new() { Name = "Editor", Exact = true }).ClickAsync();
        await Expect(page.Locator("h1")).ToHaveTextAsync("Editor");

        await AssertTheEditorWorksAsync(page, problems);

        _sample.AssertLoadedOnce(requests);
    }

    [TestMethod]
    public async Task A_deep_link_to_the_editor_is_prerendered_and_the_editor_edits_once_the_browser_takes_over()
    {
        var page = await NewPageAsync();
        var problems = TrackProblems(page);
        var requests = _sample!.Track(page, "Bit.Bmage.Blazor");

        // The server prerenders the router - LazyAssemblyLoader resolved there too - and the editor's empty state, from
        // its own registration.
        var response = await page.GotoAsync(BaseUrl + "/editor", new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        Assert.AreEqual(200, response?.Status);
        var prerendered = await response!.TextAsync();
        Assert.Contains("bmg-editor", prerendered);
        Assert.Contains("Drop an image here, or click to choose one", prerendered);

        await WaitForInteractiveAsync(page);
        await Expect(page.Locator("h1")).ToHaveTextAsync("Editor");

        await AssertTheEditorWorksAsync(page, problems);

        _sample.AssertLoadedOnce(requests);
    }

    /// <summary>
    /// The sample image opened through the editor's own file input, turned a quarter and exported. The editor's loader,
    /// canvas and exporter, which it built itself as nothing was registered, each go through the trimmed browser module -
    /// decoding, drawing and encoding - and the page reads the export back through the editor's API.
    /// </summary>
    private static async Task AssertTheEditorWorksAsync(IPage page, List<string> problems)
    {
        await LoadSampleIntoEditorAsync(page);

        await page.Locator(".bmg-tool[title='Rotate right']").ClickAsync();
        await page.WaitForFunctionAsync($"() => document.querySelector('.bmg-canvas')?.width === {SampleImage.Height}", null,
            new PageWaitForFunctionOptions { Timeout = 60_000 });
        var size = await page.EvaluateAsync<JsonElement>("() => { const c = document.querySelector('.bmg-canvas'); return { w: c.width, h: c.height }; }");
        Assert.AreEqual(SampleImage.Height, size.GetProperty("w").GetInt32());
        Assert.AreEqual(SampleImage.Width, size.GetProperty("h").GetInt32());

        await page.ClickAsync("#export");
        await Expect(page.Locator("#exported")).ToHaveTextAsync(new Regex($@"^{SampleImage.Height} x {SampleImage.Width} Png, \d+ bytes$"));

        // Neither the page nor the editor showed an error, and the browser reported none.
        await Expect(page.GetByRole(AriaRole.Alert)).ToHaveCountAsync(0);
        lock (problems) Assert.IsEmpty(problems, "the page reported:" + Environment.NewLine + string.Join(Environment.NewLine, problems));
    }

    /// <summary>
    /// The suite's console and unhandled error collection (<see cref="DemoTest.CollectErrors"/>), and every failed
    /// request: a lazily loaded assembly, the browser module or its worker that does not arrive is one.
    /// </summary>
    private static List<string> TrackProblems(IPage page)
    {
        var problems = CollectErrors(page);
        page.RequestFailed += (_, request) => { lock (problems) problems.Add($"request failed: {request.Url} ({request.Failure})"); };
        page.Response += (_, response) => { if (response.Status >= 400) lock (problems) problems.Add($"HTTP {response.Status}: {response.Url}"); };
        return problems;
    }

    private static Task PublishAsync()
    {
        if (E2EEnvironment.SkipBuild || E2EEnvironment.LazyBaseUrl is not null || E2EEnvironment.LazyAot) return Task.CompletedTask;

        var project = RepoLayout.LazySampleProject();
        return ChildProcess.RunToCompletionAsync("dotnet",
            ["publish", project, "-c", E2EEnvironment.Configuration, "-nologo", "-v:q"],
            Path.GetDirectoryName(project)!,
            TimeSpan.FromMinutes(15));
    }
}
