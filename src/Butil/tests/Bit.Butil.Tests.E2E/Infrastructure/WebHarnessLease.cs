using Microsoft.Playwright;

namespace Bit.Butil.Tests.E2E.Infrastructure;

/// <summary>A browser context of its own against a shared web host: its own storage, circuit and WebAssembly instance.</summary>
public sealed class WebHarnessLease : IHarnessLease
{
    private readonly IBrowserContext _context;

    private WebHarnessLease(IBrowserContext context, IPage page, string baseUrl)
    {
        _context = context;
        Page = page;
        BaseUrl = baseUrl;
    }

    public IPage Page { get; }

    public string BaseUrl { get; }

    public static async Task<WebHarnessLease> OpenAsync(string baseUrl)
    {
        var browser = await PlaywrightSession.BrowserAsync();
        var context = await browser.NewContextAsync(new()
        {
            BaseURL = baseUrl,
            IgnoreHTTPSErrors = true,
            ViewportSize = new() { Width = 1280, Height = 720 }
        });

        return new WebHarnessLease(context, await context.NewPageAsync(), baseUrl);
    }

    public ValueTask DisposeAsync() => new(_context.CloseAsync());
}
