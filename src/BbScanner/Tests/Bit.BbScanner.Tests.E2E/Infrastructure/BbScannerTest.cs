using Microsoft.Playwright;

namespace Bit.BbScanner.Tests.E2E.Infrastructure;

/// <summary>
/// Base of every suite: gives each test a browser whose camera is one of the generated clips, against the
/// shared demo, and closes whatever the test opened when it ends.
/// </summary>
public abstract class BbScannerTest
{
    private readonly List<ScannerSession> _sessions = [];
    private PlaywrightSession.Session? _session;

    public TestContext TestContext { get; set; } = default!;

    /// <summary>The origin the demo is served from, without a trailing slash.</summary>
    protected string BaseUrl => Session.Host!.BaseUrl;

    /// <summary>The generated camera clips, and the directory a test may write files of its own into.</summary>
    protected CameraClips Clips => Session.Clips!;

    /// <summary>The rotating multi symbology clip.</summary>
    protected string RotatingClip => Clips.Rotating;

    /// <summary>A clip that contains no barcode.</summary>
    protected string BlankClip => Clips.Blank;

    /// <summary>A clip showing a single QR code for its whole length.</summary>
    protected string SingleQrClip => Clips.SingleQr;

    private PlaywrightSession.Session Session => _session ?? throw new InvalidOperationException("The session is opened in TestInitialize.");

    [TestInitialize]
    public async Task OpenSessionAsync()
    {
        _session = await PlaywrightSession.GetAsync();
        if (_session.Unavailable is not { } reason) return;

        // A build server must not report a suite that never ran as anything but a failure.
        if (E2EEnvironment.IsContinuousIntegration) Assert.Fail("The browser tests could not run: " + reason);
        Assert.Inconclusive("The browser tests could not run: " + reason);
    }

    [TestCleanup]
    public async Task CloseSessionsAsync()
    {
        foreach (var session in _sessions)
        {
            await session.DisposeAsync();
        }

        _sessions.Clear();
    }

    /// <summary>Opens a browser whose camera is the given clip, and a page on the demo.</summary>
    /// <param name="clipPath">Y4M file the fake camera plays, or <see langword="null"/> for the rotating clip.</param>
    /// <param name="grantPermission">Whether the camera permission prompt is auto-accepted.</param>
    protected async Task<ScannerSession> OpenAsync(string? clipPath = null, bool grantPermission = true)
    {
        var launch = Session.Launch!;

        var arguments = new List<string>
        {
            "--use-fake-device-for-media-stream",
            $"--use-file-for-fake-video-capture={clipPath ?? RotatingClip}",
            "--autoplay-policy=no-user-gesture-required",

            // Exposes window.gc so a heap measurement can collect first; without it the figure
            // is whatever garbage V8 has not got round to yet, which is not a leak.
            "--js-flags=--expose-gc",
        };

        arguments.Add(grantPermission ? "--use-fake-ui-for-media-stream" : "--deny-permission-prompts");

        var browser = await Session.Playwright!.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Channel = launch.Channel,
            ExecutablePath = launch.ExecutablePath,
            Headless = launch.Headless,
            Args = arguments,
        });

        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 900 },
            Permissions = grantPermission ? ["camera"] : [],
        });

        var page = await context.NewPageAsync();
        var session = new ScannerSession(browser, context, page, BaseUrl);
        _sessions.Add(session);

        await session.AttachDiagnosticsAsync();
        return session;
    }
}
