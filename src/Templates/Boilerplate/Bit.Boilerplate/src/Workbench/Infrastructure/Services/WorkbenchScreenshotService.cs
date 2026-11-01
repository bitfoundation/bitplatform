using Bit.Bmage.Codecs.Png;
using Microsoft.Playwright;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.WebUtilities;

namespace Boilerplate.Workbench.Infrastructure.Services;

/// <summary>
/// Renders previews in a headless Chromium-based browser for screenshots, thumbnails, checks, flow screens and PDF review sheets.
/// The browser starts on first use and is shared; Screenshots:MaxConcurrentCaptures caps how many pages it renders at once.
/// </summary>
public sealed partial class WorkbenchScreenshotService : IAsyncDisposable
{
    [AutoInject] private IServer server = default!;
    [AutoInject] private IConfiguration configuration = default!;
    [AutoInject] private ILogger<WorkbenchScreenshotService> logger = default!;

    private readonly ConcurrentDictionary<string, Lazy<Task<byte[]>>> thumbnails = [];
    private readonly SemaphoreSlim browserLock = new(1, 1);
    private SemaphoreSlim? captureSlots;
    private int waitingCaptures;
    private IPlaywright? playwright;
    private IBrowser? browser;

    /// <summary>The pages the browser may render at once, created on first use from Screenshots:MaxConcurrentCaptures.</summary>
    private SemaphoreSlim CaptureSlots => LazyInitializer.EnsureInitialized(ref captureSlots, () => new SemaphoreSlim(Math.Max(1, configuration.GetValue("Screenshots:MaxConcurrentCaptures", 2))));

    /// <summary>Opens a preview and returns its PNG, once the fonts are loaded and two more frames are drawn.</summary>
    public async Task<byte[]> Capture(string relativeUrl, int width, int height, double pixelRatio, bool isMobile, WorkbenchThemeMode theme, string? culture, CancellationToken cancellationToken)
    {
        var png = await Open(relativeUrl, width, height, pixelRatio, isMobile, theme, culture, async page =>
        {
            await page.EvaluateAsync("() => document.fonts.ready.then(() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve))))");

            return await page.ScreenshotAsync(new() { Type = ScreenshotType.Png, Animations = ScreenshotAnimations.Disabled });
        }, cancellationToken);

        return RemoveAlphaChannel(png);
    }

    /// <summary>Runs the accessibility and theme-color checks in a view and returns their JSON.</summary>
    public Task<string> RunChecks(WorkbenchPageInfo page, string? query, CancellationToken cancellationToken)
    {
        var scenario = WorkbenchScenario.Parse(query);
        var device = scenario.DevicePreset;
        var (width, height) = device.GetViewport(scenario.Rotated);

        return Open(WorkbenchUrls.Preview(page.Key, scenario with { Frame = false, Backdrop = null }).TrimStart('/'),
                    width,
                    height,
                    device.PixelRatio,
                    device.IsHandheld,
                    scenario.Theme,
                    scenario.Culture,
                    async browserPage => await browserPage.EvaluateAsync("() => window.workbench.runChecks()") is { } result ? result.ToString() : "{}",
                    cancellationToken);
    }

    /// <summary>Prints an HTML document to an A4 PDF.</summary>
    public async Task<byte[]> RenderPdf(string html, CancellationToken cancellationToken)
    {
        var slots = await WaitForSlot(cancellationToken);

        try
        {
            var browser = await GetBrowser();

            await using var context = await browser.NewContextAsync();
            var page = await context.NewPageAsync();

            await page.SetContentAsync(html, new() { WaitUntil = WaitUntilState.Load, Timeout = 30_000 });

            return await page.PdfAsync(new() { Format = "A4", PrintBackground = true, Margin = new() { Top = "12mm", Bottom = "12mm", Left = "12mm", Right = "12mm" } });
        }
        finally
        {
            slots.Release();
        }
    }

    /// <summary>
    /// Waits for a free capture slot and returns the semaphore to release. Throws <see cref="WorkbenchCapturesBusyException"/>
    /// when more than Screenshots:MaxQueuedCaptures captures already wait.
    /// </summary>
    private async Task<SemaphoreSlim> WaitForSlot(CancellationToken cancellationToken)
    {
        var slots = CaptureSlots;

        if (Interlocked.Increment(ref waitingCaptures) > configuration.GetValue("Screenshots:MaxQueuedCaptures", 64))
        {
            Interlocked.Decrement(ref waitingCaptures);
            throw new WorkbenchCapturesBusyException();
        }

        try
        {
            await slots.WaitAsync(cancellationToken);
        }
        finally
        {
            Interlocked.Decrement(ref waitingCaptures);
        }

        return slots;
    }

    /// <summary>
    /// Opens a preview in a fresh browser context at the device's viewport, pixel ratio, theme and locale, waits until the page
    /// reports it is ready, and hands the page to <paramref name="use"/>.
    /// </summary>
    private async Task<T> Open<T>(string relativeUrl, int width, int height, double pixelRatio, bool isMobile, WorkbenchThemeMode theme, string? culture, Func<IPage, Task<T>> use, CancellationToken cancellationToken)
    {
        var slots = await WaitForSlot(cancellationToken);

        try
        {
            var browser = await GetBrowser();

            await using var context = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = width, Height = height },
                DeviceScaleFactor = (float)pixelRatio,
                IsMobile = isMobile,
                HasTouch = isMobile,
                Locale = culture,
                ReducedMotion = ReducedMotion.Reduce,
                ColorScheme = theme is WorkbenchThemeMode.Dark ? ColorScheme.Dark : ColorScheme.Light
            });

            await AddPairingHeader(context);

            var page = await context.NewPageAsync();

            await page.GotoAsync(new Uri(WorkbenchUrls.LocalBaseAddress(server), relativeUrl).ToString(), new() { WaitUntil = WaitUntilState.Load, Timeout = 30_000 });

            await page.WaitForFunctionAsync("() => window.__workbenchReady === true", null, new() { Timeout = 30_000 });

            try
            {
                // A page that keeps the network busy is captured anyway after a few seconds.
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = 8_000 });
            }
            catch (TimeoutException)
            {
            }

            return await use(page);
        }
        catch (Exception exp) when (exp is not OperationCanceledException)
        {
            logger.LogError(exp, "Opening {Url} in the headless browser failed.", relativeUrl);
            throw;
        }
        finally
        {
            slots.Release();
        }
    }

    /// <summary>Captures a view and names the file after its page, device, pixel size, culture, theme and data state.</summary>
    public async Task<(byte[] Png, string FileName)> CapturePage(WorkbenchPageInfo page, string? query, CancellationToken cancellationToken)
    {
        var scenario = WorkbenchScenario.Parse(query);
        var device = scenario.DevicePreset;
        var (width, height) = device.GetViewport(scenario.Rotated);
        var snapshotId = QueryHelpers.ParseQuery(query).TryGetValue(WorkbenchUrls.SnapshotQueryKey, out var id) ? id.ToString() : null;

        // The device frame is drawn only around a store screenshot with a backdrop.
        var png = await Capture(WorkbenchUrls.Preview(page.Key, scenario with { Frame = scenario.Backdrop is not null && scenario.Frame }, snapshotId: snapshotId).TrimStart('/'),
                                width,
                                height,
                                device.PixelRatio,
                                device.IsHandheld,
                                scenario.Theme,
                                scenario.Culture,
                                cancellationToken);

        var (pixelWidth, pixelHeight) = device.GetPixels(scenario.Rotated);
        var look = scenario.Backdrop is null ? scenario.Data.ToString() : "framed";
        var fileName = $"{page.Key}-{device.Key}-{pixelWidth}x{pixelHeight}-{scenario.Culture ?? CultureInfoManager.DefaultCulture.Name}-{scenario.Theme}-{look}.png".ToLowerInvariant();

        return (png, fileName);
    }

    /// <summary>Taps through a flow screen's taps, then finds where each arrow starts and reads the page's state.</summary>
    public Task<WorkbenchFlowCapture> CaptureFlowScreen(WorkbenchPageInfo page, WorkbenchScenario scenario, IReadOnlyList<string> taps, IReadOnlyList<string> spots, CancellationToken cancellationToken)
    {
        var device = scenario.DevicePreset;
        var (width, height) = device.GetViewport(scenario.Rotated);

        return Open(WorkbenchUrls.Preview(page.Key, scenario with { Backdrop = null, Caption = null }).TrimStart('/'), width, height, device.PixelRatio, device.IsHandheld, scenario.Theme, scenario.Culture, async browserPage =>
        {
            foreach (var tap in taps)
            {
                var target = Tappable(browserPage, tap);

                try
                {
                    await target.ClickAsync(new() { Timeout = 5_000 });
                }
                catch (TimeoutException)
                {
                    return new(null, [], tap);
                }

                await browserPage.WaitForTimeoutAsync(500);
            }

            List<WorkbenchFlowSpot?> found = [];

            foreach (var spot in spots)
            {
                LocatorBoundingBoxResult? box = null;

                try
                {
                    box = await Tappable(browserPage, spot).BoundingBoxAsync(new() { Timeout = 2_000 });
                }
                catch (TimeoutException)
                {
                }

                found.Add(box is null || box.Y + box.Height > height ? null : new(box.X / width, box.Y / height, box.Width / width, box.Height / height));
            }

            var state = taps.Count == 0 ? null : await browserPage.EvaluateAsync<string?>("() => window.workbench.captureState()");

            return new WorkbenchFlowCapture(state, [.. found], null);
        }, cancellationToken);
    }

    /// <summary>The first button, link or tab named by the text, or else an element with exactly that text.</summary>
    private static ILocator Tappable(IPage page, string text)
    {
        return page.GetByRole(AriaRole.Button, new() { Name = text })
                   .Or(page.GetByRole(AriaRole.Link, new() { Name = text }))
                   .Or(page.GetByRole(AriaRole.Tab, new() { Name = text }))
                   .Or(page.GetByText(text, new() { Exact = true }))
                   .First;
    }

    /// <summary>A page's thumbnail for the home page, captured once and kept; a failed capture is tried again next time.</summary>
    public Task<byte[]> GetThumbnail(string pageKey, bool phone, WorkbenchThemeMode theme)
    {
        var key = $"{pageKey}|{phone}|{theme}";

        return thumbnails.GetOrAdd(key, _ => new(() => CaptureThumbnail(key, pageKey, phone, theme))).Value;
    }

    private async Task<byte[]> CaptureThumbnail(string key, string pageKey, bool phone, WorkbenchThemeMode theme)
    {
        try
        {
            var scenario = new WorkbenchScenario { Device = phone ? DevicePresets.DefaultKey : "laptop-hidpi", Theme = theme, Frame = false };
            var (width, height) = scenario.DevicePreset.GetViewport(rotated: false);

            return await Capture(WorkbenchUrls.Preview(pageKey, scenario).TrimStart('/'), width, height, phone ? 1 : 0.5, phone, theme, null, CancellationToken.None);
        }
        catch
        {
            thumbnails.TryRemove(key, out _);
            throw;
        }
    }

    /// <summary>
    /// Adds the IIS pairing token, which Kestrel asks for on every request when it runs behind IIS, to the requests the
    /// page makes to this app only, so no other site receives it.
    /// </summary>
    private async Task AddPairingHeader(IBrowserContext context)
    {
        if (Environment.GetEnvironmentVariable("ASPNETCORE_TOKEN") is not { Length: > 0 } pairingToken)
            return;

        await context.RouteAsync($"{WorkbenchUrls.LocalBaseAddress(server)}**", route => route.ContinueAsync(new()
        {
            Headers = new Dictionary<string, string>(route.Request.Headers) { ["MS-ASPNETCORE-TOKEN"] = pairingToken }
        }));
    }

    /// <summary>Writes the PNG again without an alpha channel, which the App Store does not accept in screenshots.</summary>
    private static byte[] RemoveAlphaChannel(byte[] png)
    {
        using var image = PngDecoder.Decode(png);

        return PngEncoder.Encode(image).Data;
    }

    /// <summary>Starts the shared browser on first use: Playwright's Chromium, otherwise Microsoft Edge or Google Chrome.</summary>
    private async Task<IBrowser> GetBrowser()
    {
        if (browser?.IsConnected is true)
            return browser;

        await browserLock.WaitAsync();

        try
        {
            if (browser?.IsConnected is true)
                return browser;

            playwright ??= await Playwright.CreateAsync();

            List<Exception> failures = [];

            foreach (var channel in new string?[] { null, "msedge", "chrome" })
            {
                try
                {
                    browser = await playwright.Chromium.LaunchAsync(new() { Headless = true, Channel = channel });
                    return browser;
                }
                catch (PlaywrightException exp)
                {
                    failures.Add(exp);
                }
            }

            throw new AggregateException("No Chromium-based browser could be started for taking screenshots. Install one with: pwsh bin/Debug/net11.0/playwright.ps1 install chromium", failures);
        }
        finally
        {
            browserLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (browser is not null)
        {
            await browser.DisposeAsync();
        }

        playwright?.Dispose();
        browserLock.Dispose();
        captureSlots?.Dispose();
    }
}
