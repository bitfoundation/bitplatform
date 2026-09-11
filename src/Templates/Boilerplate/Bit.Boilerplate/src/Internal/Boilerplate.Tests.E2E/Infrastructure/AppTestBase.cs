using Boilerplate.Tests.E2E.Infrastructure.Services;

namespace Boilerplate.Tests.E2E.Infrastructure;

/// <summary>
/// Base for a test that drives one of the apps; the <see cref="IAppOpener"/> a derived class picks is what decides
/// which platform it runs on.
/// </summary>
public abstract class AppTestBase : AppPageTest
{
    private readonly List<Func<Task>> cleanups = [];

    protected abstract IAppOpener AppOpener { get; }

    /// <summary>Stops what an <see cref="IAppOpener"/> launched, at the end of exactly this test.</summary>
    public void RegisterForCleanup(Func<Task> stop) => cleanups.Add(stop);

    /// <summary>
    /// A live page of <paramref name="app"/>; inconclusive when it has no build on this platform, so a coverage gap
    /// shows up as skipped rather than hiding as a pass.
    /// </summary>
    protected async Task<IPage> OpenApp(App app)
    {
        var page = await AppOpener.TryOpen(this, app);

        if (page is null)
            Assert.Inconclusive($"{app} has no build on this platform.");

        return page!;
    }

    /// <summary>Generous: a first visit includes the WebAssembly boot / bswup precache on a cold cache.</summary>
    protected async Task WaitUntilInteractive(IPage page)
    {
        await Expect(page.Locator("main .main-container").First)
            .ToBeVisibleAsync(new() { Timeout = (float)TimeSpan.FromMinutes(2).TotalMilliseconds });
    }

    /// <summary>
    /// Picks <paramref name="cultureName"/> from the header's language menu, the way a user does. On a hybrid app it
    /// also puts that culture in storage, which a link's own culture later has to outrank.
    /// </summary>
    protected async Task ChangeCulture(IPage page, string cultureName)
    {
        var displayName = CultureInfoManager.SupportedCultures.First(sc => sc.Culture.Name == cultureName).DisplayName;

        // Signing in swaps the identity layout for the main one, where the menu below lives; without waiting, the
        // click resolves against a header on its way out.
        await Expect(page.Locator("main.non-identity")).ToBeVisibleAsync();

        // The drop menu itself, not its chevron: AppMenu hides the chevron under 600px, which is every phone-sized
        // hybrid WebView.
        await page.Locator("header .bit-drm").First.ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = AppStrings.Language }).ClickAsync();
        await Expect(page.GetByText(AppStrings.SelectLanguage)).ToBeVisibleAsync();
        await page.GetByText(displayName, new() { Exact = true }).ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    /// <summary>
    /// Signs in through the app's own form, for a user whose password is all it takes - the seeded members, not the
    /// global admin (whose second factor <c>GlobalAdminTwoFactorCodeTests</c> covers). For tests that need an identity
    /// only as setup; the journeys about signing in have their own steps.
    /// </summary>
    protected async Task SignIn(IPage page, string email, string password)
    {
        await GoToWhenInteractive(page, PageUrls.SignIn);
        await WaitUntilInteractive(page);

        await page.GetByPlaceholder(AppStrings.EmailPlaceholder).FillEnsuringStable(email);
        await page.GetByPlaceholder(AppStrings.PasswordPlaceholder).FillEnsuringStable(password);
        await page.GetByRole(AriaRole.Button, new() { Name = AppStrings.Continue, Exact = true }).ClickAsync();

        // Leaving the sign in page is the app saying it took the credentials; ChangeCulture waits for the layout swap
        // that follows.
        await Expect(page).Not.ToHaveURLAsync(new Regex("sign-in", RegexOptions.IgnoreCase));
    }

    /// <summary>
    /// <see cref="PlaywrightPageExtensions.GoToInApp"/> against a deadline: in-app navigation is a message only a
    /// running app is subscribed to, and one posted before a prerendered app boots is dropped silently.
    /// </summary>
    private static async Task GoToWhenInteractive(IPage page, string path)
    {
        // Not the real signal - a prerendered page is idle long before its WebAssembly boots - but it keeps the
        // ordinary case to one attempt.
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromMinutes(2);

        while (true)
        {
            try
            {
                await page.GoToInApp(path);
                return;
            }
            catch (PlaywrightException) when (DateTimeOffset.UtcNow < deadline)
            {
            }
        }
    }

    [TestCleanup]
    public async ValueTask AppsCleanup()
    {
        foreach (var cleanup in cleanups)
            await cleanup();

        cleanups.Clear();
    }
}
