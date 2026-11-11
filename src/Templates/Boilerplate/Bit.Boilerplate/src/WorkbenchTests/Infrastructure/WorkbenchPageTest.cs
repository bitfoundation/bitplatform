using Microsoft.Playwright.TestAdapter;

namespace Boilerplate.WorkbenchTests.Infrastructure;

public abstract class WorkbenchPageTest : PageTest
{
    private static readonly TimeSpan defaultTimeout = TimeSpan.FromSeconds(30);

    private protected WorkbenchTestServer Server { get; private set; } = default!;

    /// <summary>The surface this run of the test is on.</summary>
    protected WorkbenchSurface Surface => TestContext.TestData?.OfType<WorkbenchSurface>().Select(surface => (WorkbenchSurface?)surface).FirstOrDefault() ?? WorkbenchSurface.DesktopWithAgent;

    protected bool IsPhone => Surface is WorkbenchSurface.PhoneWithAgent or WorkbenchSurface.PhoneWithoutAgent;

    /// <summary>In dev mode, the Workbench lets an agent connect and lets things change; a copy without one shows a notice and is read-only.</summary>
    protected bool HasAgent => Surface is WorkbenchSurface.DesktopWithAgent or WorkbenchSurface.PhoneWithAgent;

    /// <summary>The Workbench these tests run against.</summary>
    private protected virtual Task<WorkbenchTestServer> ServerToTest => HasAgent ? WorkbenchTestsInitializer.SharedServer : WorkbenchTestsInitializer.ReadOnlyServer;

    [TestInitialize]
    public async Task WorkbenchPageSetup()
    {
        Server = await ServerToTest;

        Context.SetDefaultTimeout((float)defaultTimeout.TotalMilliseconds);
        Assertions.SetDefaultExpectTimeout((float)defaultTimeout.TotalMilliseconds);

        await Context.RouteAsync(url => new Uri(url).IsLoopback is false, route => route.AbortAsync());
    }

    public override BrowserNewContextOptions ContextOptions()
    {
        var options = base.ContextOptions();

        options.Locale = "en-US";

        if (IsPhone)
        {
            var phone = Playwright.Devices["Pixel 7"];

            options.ViewportSize = phone.ViewportSize;
            options.DeviceScaleFactor = phone.DeviceScaleFactor;
            options.UserAgent = phone.UserAgent;
            options.IsMobile = phone.IsMobile;
            options.HasTouch = phone.HasTouch;
        }
        else
        {
            options.ViewportSize = new() { Width = 1600, Height = 1000 };
        }

        return options;
    }

    /// <summary>Headless runs use chromium-headless-shell, which is all these tests need and starts faster.</summary>
    public override async Task<BrowserTypeLaunchOptions?> LaunchOptionsAsync()
    {
        var options = new BrowserTypeLaunchOptions(PlaywrightSettingsProvider.LaunchOptions);

        var isHeadedRun = options.Headless is false
            || Environment.GetEnvironmentVariable("HEADED") is "1"
            || string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("PWDEBUG")) is false;

        if (isHeadedRun is false && PlaywrightSettingsProvider.BrowserName is Microsoft.Playwright.BrowserType.Chromium)
        {
            options.Channel = "chromium-headless-shell";
        }

        return options;
    }

    /// <summary>Opens a page in the shell and waits until every view on the canvas has settled.</summary>
    protected async Task OpenWorkbench(string pageKey, string query = "")
    {
        await Page.GotoAsync(new Uri(Server.Address, $"workbench/{pageKey}{query}").ToString());

        await WaitForViews();
    }

    /// <summary>Waits until the canvas shows its views and none of them is still loading.</summary>
    protected async Task WaitForViews()
    {
        await Expect(Page.Locator(".pane-stage.ready").First).ToBeVisibleAsync();
        await Expect(Page.Locator(".pane-stage:not(.ready)")).ToHaveCountAsync(0);
    }

    /// <summary>Opens a page alone, the way each view loads it, and waits until it reports it has settled.</summary>
    protected async Task OpenPreview(string pageKey, string query = "")
    {
        await Page.GotoAsync(new Uri(Server.Address, $"preview/{pageKey}{query}").ToString());

        await Page.WaitForFunctionAsync("() => window.__workbenchReady === true");
    }

    /// <summary>The iframe of the view with this id on the shell.</summary>
    protected IFrameLocator View(string paneId = "1") => Page.FrameLocator($"iframe[data-pane-id='{paneId}']");

    /// <summary>A button of the shell by its exact name.</summary>
    protected ILocator Button(string name) => Page.GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    /// <summary>Uses a control the way this surface does: a finger on a phone, the mouse on a desktop.</summary>
    protected Task Press(ILocator target) => IsPhone ? target.TapAsync() : target.ClickAsync();

    /// <summary>
    /// Where the shell keeps the selected view's settings, its comments and its flows: a desktop's side column, or a phone's Tools
    /// panel, in which they follow each other instead of being tabs.
    /// </summary>
    protected ILocator Tools => Page.Locator(IsPhone ? ".bit-pnl.tools-panel:not([data-bit-callout-link])" : "#wb-inspector");

    /// <summary>Brings the tools into sight, which a desktop has in sight already.</summary>
    protected async Task OpenTools()
    {
        if (IsPhone)
        {
            await Page.Locator(".tools-button").TapAsync();
            await Expect(Page.Locator(".tools-header")).ToBeVisibleAsync();
            await Expect(Tools).Not.ToHaveAttributeAsync("inert", string.Empty);
        }
    }

    /// <summary>Gets the tools out of the way of the view, which a desktop doesn't need.</summary>
    protected async Task CloseTools()
    {
        if (IsPhone)
        {
            await Page.Locator(".tools-header").GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.Close, Exact = true }).TapAsync();
            await Expect(Tools).ToHaveAttributeAsync("inert", string.Empty);
        }
    }

    /// <summary>Opens a tab of a desktop's side column, which a phone shows all at once.</summary>
    protected async Task OpenTab(string column)
    {
        if (IsPhone)
        {
            await OpenTools();
        }
        else
        {
            await Page.Locator($"#wb-tab-{column}").ClickAsync();
            await Expect(Page.Locator($"#wb-tab-{column}")).ToHaveAttributeAsync("aria-selected", "true");
        }
    }

    /// <summary>Waits until a page the server drew first has come alive, as a press before that goes nowhere.</summary>
    protected Task WaitUntilInteractive() => Page.WaitForFunctionAsync("() => [...document.querySelectorAll('button')].some(button => [...button.attributes].some(attribute => attribute.name.startsWith('_bl_')))");

    /// <summary>Waits until the browser has saved a store's state with this in it, which comes a moment after the change, as a reload before that loses it.</summary>
    protected Task WaitUntilSaved(string text) => WorkbenchTestReviewer.WaitFor(() => Page.EvaluateAsync<string>("""
        () => new Promise(resolve => {
            const open = indexedDB.open('bit-workbench');
            open.onerror = () => resolve('');
            // Looking before the app has made the database doesn't make an empty one in its place.
            open.onupgradeneeded = () => open.transaction.abort();
            open.onsuccess = () => {
                const database = open.result;
                const done = value => {
                    database.close();
                    resolve(value);
                };
                if (database.objectStoreNames.contains('state') === false) return done('');
                const all = database.transaction('state').objectStore('state').getAll();
                all.onerror = () => done('');
                all.onsuccess = () => done(JSON.stringify(all.result));
            };
        })
        """), saved => saved.Contains(text, StringComparison.OrdinalIgnoreCase));

    /// <summary>The same text in another language.</summary>
    protected static string InCulture(string culture, string key) => new System.Resources.ResourceManager(typeof(WorkbenchStrings)).GetString(key, CultureInfo.GetCultureInfo(culture))!;
}
