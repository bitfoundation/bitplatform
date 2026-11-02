using Microsoft.Extensions.Localization;

namespace Boilerplate.WorkbenchTests.Features.Shell;

/// <summary>What a page that throws shows instead of itself, in a view, on the shell and on the home page, and the way back from it. On a Workbench whose texts throw on request, as no page throws otherwise.</summary>
[TestClass, TestCategory("UITest")]
public class PageThatThrowsUITests : ShellPageTest
{
    private static readonly Lazy<Task<WorkbenchTestServer>> withAgent = new(() => StartServer(isDevMode: true));
    private static readonly Lazy<Task<WorkbenchTestServer>> withoutAgent = new(() => StartServer(isDevMode: false));
    // The shifts page's shell fails for whoever opens it, which this class's tests do only to see the way out of it.
    private static readonly ConcurrentDictionary<string, (string Key, string Path)> breakdowns = new() { [string.Empty] = (nameof(WorkbenchStrings.ShiftsTitle), "/workbench/shifts") };

    protected override Task<WorkbenchTestServer> ServerToTest => HasAgent ? withAgent.Value : withoutAgent.Value;

    private static Task<WorkbenchTestServer> StartServer(bool isDevMode)
    {
        return new WorkbenchTestServer().Start(services => services.AddTransient<IStringLocalizer<WorkbenchStrings>, ThrowingStrings>(), isDevMode);
    }

    [ClassCleanup]
    public static async Task StopServers()
    {
        foreach (var server in new[] { withAgent, withoutAgent }.Where(server => server.IsValueCreated))
        {
            await (await server.Value).DisposeAsync();
        }
    }

    /// <summary>An address's query that makes reading this text throw under this path, until <see cref="Heal"/>, which tests running side by side don't notice.</summary>
    private static string Break(string key, string path)
    {
        var query = $"?{WorkbenchScenario.SwitchQueryPrefix}broken={Guid.NewGuid():N}";

        Break(query, key, path);

        return query;
    }

    private static void Break(string query, string key, string path) => breakdowns[TokenOf(query)] = (key, path);

    private static void Heal(string query) => breakdowns.TryRemove(TokenOf(query), out _);

    private static string TokenOf(string query) => query[(query.IndexOf('=') + 1)..];

    /// <summary>Heals a breakdown the moment the browser asks for that document again, so only a real reload brings the page back.</summary>
    private Task HealOnReload(string query, string path) => Page.RouteAsync(url => url.Contains(TokenOf(query), StringComparison.Ordinal) && new Uri(url).AbsolutePath.StartsWith(path, StringComparison.Ordinal), route =>
    {
        Heal(query);

        return route.ContinueAsync();
    });

    private ILocator ViewError => View().GetByRole(AriaRole.Alert).Filter(new() { HasText = AppStrings.SomethingWentWrong });

    private ILocator ResetThisView => View().GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ResetThisView, Exact = true });

    private ILocator ThemeHeading => View().GetByRole(AriaRole.Heading, new() { Name = WorkbenchStrings.ThemeHeading, Level = 2 }).First;

    [TestMethod, EverySurface]
    public async Task APageThatThrows_Should_SaySoInItsViewAndLeaveTheShellToUse(WorkbenchSurface surface)
    {
        var broken = Break(nameof(WorkbenchStrings.ThemeHeading), "/preview/");

        await OpenWorkbench("theme", broken);

        await Expect(ViewError).ToBeVisibleAsync();
        await Expect(ViewError).ToContainTextAsync(ThrowingStrings.Message);
        await Expect(ThemeHeading).ToHaveCountAsync(0);

        // The app's chrome and the shell around the page go on.
        await Expect(View().Locator(".persona").First).ToBeVisibleAsync();

        await OpenTools();
        await Press(Tools.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ThemeDark, Exact = true }));
        await CloseTools();

        await WaitForViews();
        await Expect(View().Locator("html")).ToHaveAttributeAsync("bit-theme", new Regex("dark"));
        await Expect(ViewError).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task ResetThisView_Should_BringThePageBackWithoutWhatWasDoneOnIt(WorkbenchSurface surface)
    {
        var broken = Break(nameof(WorkbenchStrings.BuildingsHeading), "/not-yet/");

        await OpenWorkbench("buildings", broken);

        await BuildingsSearch().FillAsync("Rotterdam");
        await WaitUntilSaved("Rotterdam");

        // A page that fails on what the browser saved fails again on every reload.
        Break(broken, nameof(WorkbenchStrings.BuildingsHeading), "/preview/");

        await Page.ReloadAsync();
        await WaitForViews();

        await Expect(ViewError).ToBeVisibleAsync();

        Heal(broken);

        await Press(ResetThisView);

        await Expect(ViewError).ToHaveCountAsync(0);
        await Expect(BuildingsSearch()).ToHaveValueAsync(string.Empty);

        await WorkbenchTestReviewer.WaitFor(SavedState, saved => saved.Contains("Rotterdam", StringComparison.Ordinal) is false);

        await Page.ReloadAsync();
        await WaitForViews();

        await Expect(BuildingsSearch()).ToHaveValueAsync(string.Empty);
    }

    [TestMethod, EverySurface]
    public async Task AViewThatThrowsOutsideItsPage_Should_OfferAReload(WorkbenchSurface surface)
    {
        var broken = Break(nameof(WorkbenchStrings.ThemePageTitle), "/preview/");

        await OpenWorkbench("theme", broken);

        var error = View().GetByRole(AriaRole.Alert).Filter(new() { HasText = WorkbenchStrings.PageFailed });

        await Expect(error).ToBeVisibleAsync();
        await Expect(ThemeHeading).ToHaveCountAsync(0);

        // It counts as settled, for the shell and for a screenshot.
        Assert.IsTrue(await View().Locator("html").EvaluateAsync<bool>("() => window.__workbenchReady === true"));

        await HealOnReload(broken, "/preview/");

        await Press(error.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ReloadPage, Exact = true }));

        await Expect(ThemeHeading).ToBeVisibleAsync();
        await Expect(error).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task AShellThatThrows_Should_OfferAReload(WorkbenchSurface surface)
    {
        var broken = Break(nameof(WorkbenchStrings.ThemePageTitle), "/workbench/");

        await Page.GotoAsync(new Uri(Server.Address, $"workbench/theme{broken}").ToString());

        var error = Page.GetByRole(AriaRole.Alert).Filter(new() { HasText = WorkbenchStrings.PageFailed });

        await Expect(error).ToBeVisibleAsync();
        await Expect(Page.Locator("iframe[data-pane-id]")).ToHaveCountAsync(0);

        await WaitUntilInteractive();

        await HealOnReload(broken, "/workbench/");

        await Press(error.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ReloadPage, Exact = true }));

        await WaitForViews();
        await Expect(ThemeHeading).ToBeVisibleAsync();
        await Expect(error).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task AHomePageThatThrows_Should_OfferAReload(WorkbenchSurface surface)
    {
        var broken = Break(nameof(WorkbenchStrings.HomeHeadline), "/");

        await Page.GotoAsync(new Uri(Server.Address, broken).ToString());

        var error = Page.GetByRole(AriaRole.Alert).Filter(new() { HasText = WorkbenchStrings.PageFailed });

        await Expect(error).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = WorkbenchStrings.HomeHeadline, Level = 1 })).ToHaveCountAsync(0);

        await WaitUntilInteractive();

        await HealOnReload(broken, "/");

        await Press(error.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ReloadPage, Exact = true }));

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = WorkbenchStrings.HomeHeadline, Level = 1 })).ToBeVisibleAsync();
        await Expect(error).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task AnError_Should_LastOnlyUntilAnotherPageOpens(WorkbenchSurface surface)
    {
        await Page.GotoAsync(Server.Address.ToString());
        await WaitUntilInteractive();

        var error = Page.GetByRole(AriaRole.Alert).Filter(new() { HasText = WorkbenchStrings.PageFailed });
        var headline = Page.GetByRole(AriaRole.Heading, new() { Name = WorkbenchStrings.HomeHeadline, Level = 1 });

        await Press(Page.Locator("a.page-card").Filter(new() { HasText = WorkbenchStrings.ShiftsDescription }));

        await Expect(error).ToBeVisibleAsync();
        await Expect(headline).ToHaveCountAsync(0);

        await Page.GoBackAsync();

        await Expect(headline).ToBeVisibleAsync();
        await Expect(error).ToHaveCountAsync(0);
    }

    /// <summary>The Workbench's texts, of which one throws under a path while the address names its breakdown.</summary>
    private sealed class ThrowingStrings(IStringLocalizerFactory factory, NavigationManager navigationManager) : IStringLocalizer<WorkbenchStrings>
    {
        public const string Message = "A page that throws";

        private readonly IStringLocalizer inner = factory.Create(typeof(WorkbenchStrings));

        public LocalizedString this[string name]
        {
            get
            {
                ThrowIfBroken(name);

                return inner[name];
            }
        }

        public LocalizedString this[string name, params object[] arguments]
        {
            get
            {
                ThrowIfBroken(name);

                return inner[name, arguments];
            }
        }

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => inner.GetAllStrings(includeParentCultures);

        private void ThrowIfBroken(string name)
        {
            if (breakdowns.Values.Any(breakdown => breakdown.Key == name) is false)
                return;

            var address = new Uri(navigationManager.Uri);

            if (breakdowns.Any(breakdown => breakdown.Value.Key == name && address.AbsolutePath.StartsWith(breakdown.Value.Path, StringComparison.Ordinal) && address.Query.Contains(breakdown.Key, StringComparison.Ordinal)))
                throw new InvalidOperationException(Message);
        }
    }
}
