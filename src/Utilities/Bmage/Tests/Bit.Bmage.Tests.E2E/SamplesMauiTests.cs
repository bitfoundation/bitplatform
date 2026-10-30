using System.Text.RegularExpressions;
using Bit.Bmage.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Bmage.Tests.E2E;

/// <summary>
/// The MAUI sample as a Windows app: the same pages as <see cref="SamplesBrowserTests"/>, on the Blazor Hybrid renderer,
/// with .NET in the app's own process and the WebView2 reached over JS interop - the host Bmage reports as
/// <c>BrowserServer</c>, so images are coded by .NET and only AVIF exports by the WebView.
/// </summary>
/// <remarks>
/// One app for the whole class, as a person would use it: every test starts from the page the previous one left and
/// moves through the app's own navigation. Inconclusive off Windows (see <see cref="MauiSampleApp"/>).
/// </remarks>
[TestClass]
public class SamplesMauiTests
{
    [TestMethod]
    public async Task The_app_runs_on_the_webview_renderer_and_probes_the_webview()
    {
        var (app, page) = await GoToAsync("Bmage samples");

        await Expect(page.Locator("#renderer")).ToHaveTextAsync("WebView");
        await Expect(page.Locator("#environment")).ToHaveTextAsync("BrowserServer");

        await GoToAsync("Capabilities");
        await Expect(page.Locator("#capabilities-environment")).ToHaveTextAsync("BrowserServer");
        // WebView2 is Chromium: it writes WebP, and AVIF only on some builds.
        await Expect(page.Locator("#capabilities-choice")).ToHaveTextAsync(new Regex("^(Avif|WebP)$"));

        await AssertNoErrorsAsync(app);
    }

    [TestMethod]
    public async Task The_managed_engine_runs_in_the_app_process()
    {
        var (app, page) = await GoToAsync("Engine");

        await page.GetByRole(AriaRole.Button, new() { Name = "Duotone" }).ClickAsync();

        var caption = page.Locator("#engine-caption");
        await Expect(caption).ToContainTextAsync("Duotone: 360 × 240 PNG");
        await Expect(caption).ToContainTextAsync("encoded on the device, in the app's own process");
        await WaitForImageAsync(page, "#engine-output", 360);
        await AssertNoErrorsAsync(app);
    }

    [TestMethod]
    public async Task The_process_page_loads_processes_encodes_and_downloads_through_the_webview()
    {
        var (app, page) = await GoToAsync("Process");

        // The scene is encoded to PNG and decoded back by .NET, which writes the WebP too.
        await page.ClickAsync("#process-sample");
        await Expect(page.Locator("#process-source")).ToHaveTextAsync("Sample scene: 1600 × 1066");
        await Expect(page.Locator("#process-result")).ToContainTextAsync(new Regex("^800 × 533 WebP,"));
        await WaitForImageAsync(page, "#process-output", 800);

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

        // An upload through BmageDropZone's file input, which the WebView hands to .NET as an IBrowserFile.
        var file = SampleImage.WriteToTempFile();
        try
        {
            await page.SetInputFilesAsync(".bmg-dropzone-input", file);
            await Expect(page.Locator("#process-source")).ToContainTextAsync($"{SampleImage.Width} × {SampleImage.Height}");
            await Expect(page.Locator("#process-result")).ToContainTextAsync(new Regex($"^{SampleImage.Width} × {SampleImage.Height} WebP,"));
        }
        finally
        {
            File.Delete(file);
        }

        // WebView2 saves a download as Edge would; Android's WebView and iOS's WKWebView leave it to the app
        // (docs/browser-support.md, Blazor Hybrid).
        var download = await page.RunAndWaitForDownloadAsync(() => page.ClickAsync("#process-download"));
        Assert.AreEqual("bmage-sample.webp", download.SuggestedFilename);
        await download.DeleteAsync();

        await AssertNoErrorsAsync(app);
    }

    [TestMethod]
    public async Task The_editor_loads_edits_and_exports()
    {
        var (app, page) = await GoToAsync("Editor");

        await page.ClickAsync("#editor-sample");
        await page.WaitForFunctionAsync("() => document.querySelector('.bmg-canvas')?.width > 1", null, new() { Timeout = 60_000 });

        await page.GetByRole(AriaRole.Button, new() { Name = "Rotate right" }).ClickAsync();
        await page.ClickAsync("#editor-export");

        await Expect(page.Locator("#editor-exported")).ToContainTextAsync(new Regex("^Exported 800 × 1200 WebP,"));
        await AssertNoErrorsAsync(app);
    }

    /// <summary>Follows one of the layout's links and waits for its page; the app starts on the home page.</summary>
    private static async Task<(MauiSampleApp App, IPage Page)> GoToAsync(string link)
    {
        var app = await MauiSampleApp.SharedAsync();
        var page = app.Page;
        app.TakeErrors();

        await page.WaitForSelectorAsync(".renderer[data-interactive='true']", new() { Timeout = 120_000 });
        await page.Locator(".topbar").GetByRole(AriaRole.Link, new() { Name = link, Exact = true }).ClickAsync();
        await page.WaitForSelectorAsync("h1", new() { Timeout = 60_000 });
        return (app, page);
    }

    private static Task WaitForImageAsync(IPage page, string selector, int width) =>
        page.WaitForFunctionAsync($"() => {{ const img = document.querySelector('{selector}'); return img?.complete && img.naturalWidth === {width}; }}",
            null, new() { Timeout = 60_000 });

    private static async Task AssertNoErrorsAsync(MauiSampleApp app)
    {
        await Expect(app.Page.Locator("#blazor-error-ui")).ToBeHiddenAsync();
        var errors = app.TakeErrors();
        Assert.IsEmpty(errors, string.Join(Environment.NewLine, errors));
    }
}
