using System.Text.RegularExpressions;
using Bit.Bmage.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Bmage.Tests.E2E;

/// <summary>
/// The hosting samples driven in a real browser: the same pages under Interactive Server, Interactive WebAssembly and
/// Interactive Auto, so every render mode the README promises is checked end to end - the capability probe, the
/// managed engine, loading, processing, encoding through the browser's codecs, downloading and the editor.
/// </summary>
/// <remarks>
/// Auto's first visit always runs on the server, because a fresh browser context has not downloaded the WebAssembly
/// runtime yet, so its rows expect Server; <see cref="Auto_moves_to_WebAssembly_once_the_runtime_is_downloaded"/>
/// covers its second half.
/// </remarks>
[TestClass]
public class SamplesBrowserTests
{
    private readonly List<IBrowserContext> _contexts = [];

    [TestCleanup]
    public async Task ClosePagesAsync()
    {
        foreach (var context in _contexts)
        {
            try { await context.CloseAsync(); }
            catch (PlaywrightException) { /* the browser is already gone */ }
        }

        _contexts.Clear();
    }

    [TestMethod]
    [DataRow(SampleHosts.Server, "Server", "BrowserServer")]
    [DataRow(SampleHosts.Wasm, "WebAssembly", "BrowserWebAssembly")]
    [DataRow(SampleHosts.Auto, "Server", "BrowserServer")]
    public async Task Each_sample_runs_in_its_render_mode_and_probes_the_browser(string sample, string renderer, string environment)
    {
        var (page, errors) = await OpenAsync(sample, "");

        await Expect(page.Locator("#renderer")).ToHaveTextAsync(renderer);
        await Expect(page.Locator("#environment")).ToHaveTextAsync(environment);

        await page.GetByRole(AriaRole.Link, new() { Name = "Capabilities", Exact = true }).First.ClickAsync();
        await Expect(page.Locator("#capabilities-environment")).ToHaveTextAsync(environment);
        await Expect(page.Locator("#capabilities-choice")).ToHaveTextAsync(new Regex("^(Avif|WebP|Jpeg)$"));

        await AssertNoErrorsAsync(page, errors);
    }

    [TestMethod]
    public async Task Auto_moves_to_WebAssembly_once_the_runtime_is_downloaded()
    {
        var (page, errors) = await OpenAsync(SampleHosts.Auto, "");
        await Expect(page.Locator("#renderer")).ToHaveTextAsync("Server");

        // The first visit downloads the runtime in the background; a later load that finds it ready starts there.
        var deadline = DateTime.UtcNow.AddMinutes(2);
        while (await page.Locator("#renderer").TextContentAsync() != "WebAssembly" && DateTime.UtcNow < deadline)
        {
            await page.WaitForTimeoutAsync(2_000);
            await page.ReloadAsync();
            await WaitForInteractiveAsync(page);
        }

        await Expect(page.Locator("#renderer")).ToHaveTextAsync("WebAssembly");
        await Expect(page.Locator("#environment")).ToHaveTextAsync("BrowserWebAssembly");
        await AssertNoErrorsAsync(page, errors);
    }

    [TestMethod]
    [DataRow(SampleHosts.Server, "the server")]
    [DataRow(SampleHosts.Wasm, "WebAssembly, in this browser")]
    [DataRow(SampleHosts.Auto, "the server")]
    public async Task The_managed_engine_runs_where_the_component_runs(string sample, string ranOn)
    {
        var (page, errors) = await OpenAsync(sample, "engine");

        await page.GetByRole(AriaRole.Button, new() { Name = "Sepia" }).ClickAsync();

        var caption = page.Locator("#engine-caption");
        await Expect(caption).ToContainTextAsync("Sepia: 360 × 240 PNG");
        await Expect(caption).ToContainTextAsync($"encoded on {ranOn}");
        await WaitForImageAsync(page, "#engine-output", 360);
        await AssertNoErrorsAsync(page, errors);
    }

    [TestMethod]
    [DataRow(SampleHosts.Server)]
    [DataRow(SampleHosts.Wasm)]
    [DataRow(SampleHosts.Auto)]
    public async Task The_process_page_loads_processes_encodes_and_downloads(string sample)
    {
        var (page, errors) = await OpenAsync(sample, "process");

        // The sample scene, encoded to PNG and loaded back through ImageLoader, then capped at 800 px and encoded.
        await page.ClickAsync("#process-sample");
        await Expect(page.Locator("#process-source")).ToHaveTextAsync("Sample scene: 1600 × 1066");
        await Expect(page.Locator("#process-result")).ToContainTextAsync(new Regex("^800 × 533 (WebP|Jpeg),"));
        await WaitForImageAsync(page, "#process-output", 800);

        // A pipeline change re-encodes; a grayscale result has equal channels, give or take the lossy encoder.
        await page.SelectOptionAsync("#process-look", "Grayscale");
        await page.WaitForFunctionAsync("""
            () => {
                const img = document.querySelector('#process-output');
                if (!img?.complete || img.naturalWidth !== 800) return false;
                const canvas = document.createElement('canvas');
                canvas.width = img.naturalWidth;
                canvas.height = img.naturalHeight;
                const context = canvas.getContext('2d');
                context.drawImage(img, 0, 0);
                const [r, g, b] = context.getImageData(400, 100, 1, 1).data;
                return Math.max(r, g, b) - Math.min(r, g, b) <= 8;
            }
            """, null, new() { Timeout = 60_000 });

        // An upload through BmageDropZone.
        var file = SampleImage.WriteToTempFile();
        try
        {
            await page.SetInputFilesAsync(".bmg-dropzone-input", file);
            await Expect(page.Locator("#process-source")).ToContainTextAsync($"{SampleImage.Width} × {SampleImage.Height}");
            await Expect(page.Locator("#process-result")).ToContainTextAsync(new Regex($"^{SampleImage.Width} × {SampleImage.Height} (WebP|Jpeg),"));
        }
        finally
        {
            File.Delete(file);
        }

        var download = await page.RunAndWaitForDownloadAsync(() => page.ClickAsync("#process-download"));
        StringAssert.Matches(download.SuggestedFilename, new Regex(@"^bmage-sample\.(webp|jpg)$"));

        await AssertNoErrorsAsync(page, errors);
    }

    [TestMethod]
    [DataRow(SampleHosts.Server)]
    [DataRow(SampleHosts.Wasm)]
    [DataRow(SampleHosts.Auto)]
    public async Task The_editor_loads_edits_and_exports(string sample)
    {
        var (page, errors) = await OpenAsync(sample, "editor");

        await page.ClickAsync("#editor-sample");
        await page.WaitForFunctionAsync("() => document.querySelector('.bmg-canvas')?.width > 1", null, new() { Timeout = 60_000 });

        await page.GetByRole(AriaRole.Button, new() { Name = "Rotate right" }).ClickAsync();
        await page.ClickAsync("#editor-export");

        // The 1200 × 800 sample, turned a quarter.
        await Expect(page.Locator("#editor-exported")).ToContainTextAsync(new Regex("^Exported 800 × 1200 (WebP|Png),"));
        await AssertNoErrorsAsync(page, errors);
    }

    /// <summary>Opens a route of a sample in a browser context of its own and waits for it to become interactive.</summary>
    private async Task<(IPage Page, List<string> Errors)> OpenAsync(string sample, string route)
    {
        var (browser, failure) = await PlaywrightSession.BrowserAsync();
        if (browser is null) Assert.Inconclusive(failure ?? "No browser could be launched.");

        var baseUrl = await SampleHosts.BaseUrlAsync(sample);
        var context = await browser.NewContextAsync(new() { AcceptDownloads = true });
        _contexts.Add(context);

        var page = await context.NewPageAsync();
        var errors = new List<string>();
        page.PageError += (_, e) => { lock (errors) errors.Add(e); };
        page.Console += (_, e) => { if (e.Type == "error") lock (errors) errors.Add(e.Text); };

        await page.GotoAsync($"{baseUrl}/{route}", new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await WaitForInteractiveAsync(page);
        return (page, errors);
    }

    /// <summary>The layout's renderer flag is "false" in the prerendered markup and flips on the first interactive render.</summary>
    private static Task WaitForInteractiveAsync(IPage page) =>
        page.WaitForSelectorAsync(".renderer[data-interactive='true']", new() { Timeout = 120_000 });

    private static Task WaitForImageAsync(IPage page, string selector, int width) =>
        page.WaitForFunctionAsync($"() => {{ const img = document.querySelector('{selector}'); return img?.complete && img.naturalWidth === {width}; }}",
            null, new() { Timeout = 60_000 });

    private static async Task AssertNoErrorsAsync(IPage page, List<string> errors)
    {
        await Expect(page.Locator("#blazor-error-ui")).ToBeHiddenAsync();
        lock (errors) Assert.IsEmpty(errors, string.Join(Environment.NewLine, errors));
    }
}
