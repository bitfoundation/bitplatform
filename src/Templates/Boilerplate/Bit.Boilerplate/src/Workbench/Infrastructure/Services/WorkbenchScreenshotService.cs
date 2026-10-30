using Bit.Bmage.Codecs.Png;
using Microsoft.Playwright;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace Boilerplate.Workbench.Infrastructure.Services;

public sealed partial class WorkbenchScreenshotService : IAsyncDisposable
{
    [AutoInject] private IServer server = default!;
    [AutoInject] private IConfiguration configuration = default!;
    [AutoInject] private ILogger<WorkbenchScreenshotService> logger = default!;

    private readonly SemaphoreSlim browserLock = new(1, 1);
    private SemaphoreSlim? captureSlots;
    private IPlaywright? playwright;
    private IBrowser? browser;

    public async Task<byte[]> Capture(string relativeUrl, int width, int height, double pixelRatio, bool isMobile, WorkbenchThemeMode theme, string? culture, CancellationToken cancellationToken)
    {
        captureSlots ??= new(configuration.GetValue("Screenshots:MaxConcurrentCaptures", 2));

        await captureSlots.WaitAsync(cancellationToken);

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

            var page = await context.NewPageAsync();

            await page.GotoAsync(new Uri(GetLocalBaseAddress(), relativeUrl).ToString(), new() { WaitUntil = WaitUntilState.Load, Timeout = 30_000 });

            await page.WaitForFunctionAsync("() => window.__workbenchReady === true", null, new() { Timeout = 30_000 });

            await page.EvaluateAsync("() => document.fonts.ready.then(() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve))))");

            var png = await page.ScreenshotAsync(new() { Type = ScreenshotType.Png, Animations = ScreenshotAnimations.Disabled });

            return RemoveAlphaChannel(png);
        }
        finally
        {
            captureSlots.Release();
        }
    }

    private static byte[] RemoveAlphaChannel(byte[] png)
    {
        using var image = PngDecoder.Decode(png);

        return PngEncoder.Encode(image).Data;
    }

    private Uri GetLocalBaseAddress()
    {
        var address = server.Features.Get<IServerAddressesFeature>()?.Addresses
            .OrderBy(a => a.StartsWith("https", StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault() ?? "http://localhost:5000";

        address = address.Replace("://+", "://localhost")
                         .Replace("://*", "://localhost")
                         .Replace("://0.0.0.0", "://localhost")
                         .Replace("://[::]", "://localhost");

        return new Uri(address.EndsWith('/') ? address : $"{address}/");
    }

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

            foreach (var channel in new string?[] { null, "msedge", "chrome" })
            {
                try
                {
                    browser = await playwright.Chromium.LaunchAsync(new() { Headless = true, Channel = channel });
                    return browser;
                }
                catch (PlaywrightException exp)
                {
                    logger.LogInformation(exp, "Chromium channel {Channel} is not available.", channel ?? "default");
                }
            }

            throw new InvalidOperationException("No Chromium-based browser was found for taking screenshots. Install one with: pwsh bin/Debug/net11.0/playwright.ps1 install chromium");
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
