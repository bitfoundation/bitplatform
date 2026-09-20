using Microsoft.Playwright;

namespace Bit.Brouter.Tests.E2E.Infrastructure;

/// <summary>A browser context of its own against the shared web harness host of one render mode.</summary>
public sealed class WebSession
{
    private WebHarnessHost? _host;
    private IBrowserContext? _context;

    public WebHarnessHost Host => _host ?? throw new InvalidOperationException("The session has not been opened.");

    public IBrowserContext Context => _context ?? throw new InvalidOperationException("The session has not been opened.");

    public async Task<IPage> OpenAsync(string mode)
    {
        _host = await HarnessHosts.GetAsync(mode);

        var browser = await PlaywrightSession.BrowserAsync();
        _context = await browser.NewContextAsync(new() { ViewportSize = new() { Width = 1280, Height = 720 } });

        return await _context.NewPageAsync();
    }

    public async Task CloseAsync()
    {
        if (_context is null) return;

        await _context.CloseAsync();
        _context = null;
    }
}
