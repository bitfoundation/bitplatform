using Boilerplate.Tests.Infrastructure.Components;
using Boilerplate.Tests.E2E.Infrastructure.Services;
using Boilerplate.Client.Core.Infrastructure.Services.Contracts;

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
    /// <param name="currentCultureName">The culture the menu is in now, when it is not the test process's own.</param>
    protected async Task ChangeCulture(IPage page, string cultureName, string? currentCultureName = null)
    {
        var displayName = CultureInfoManager.SupportedCultures.First(sc => sc.Culture.Name == cultureName).DisplayName;
        var currentCulture = currentCultureName is null ? CultureInfo.CurrentUICulture : CultureInfoManager.GetCultureInfo(currentCultureName)!;

        // Signing in swaps the identity layout for the main one, where the menu below lives; without waiting, the
        // click resolves against a header on its way out.
        await Expect(page.Locator("main.non-identity")).ToBeVisibleAsync();

        await ClickAppMenuItem(page, AppStrings.ResourceManager.GetString(nameof(AppStrings.Language), currentCulture)!);
        await Expect(page.GetByText(AppStrings.ResourceManager.GetString(nameof(AppStrings.SelectLanguage), currentCulture)!)).ToBeVisibleAsync();
        await page.GetByText(displayName, new() { Exact = true }).ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    /// <summary>Opens the header's app menu and clicks the entry named <paramref name="itemName"/>.</summary>
    protected async Task ClickAppMenuItem(IPage page, string itemName)
    {
        var item = page.GetByRole(AriaRole.Button, new() { Name = itemName });

        await OpenAppMenu(page, item);

        try
        {
            await item.ClickAsync(new() { Timeout = 10_000 });
        }
        catch (TimeoutException)
        {
            // In a phone-sized Android WebView the menu is a swipeable panel that never passes Playwright's "stable"
            // check. Blazor handles clicks by event delegation, so the click event itself is enough.
            await item.DispatchEventAsync("click");
        }
    }

    /// <summary>
    /// Opens the header's app menu until <paramref name="item"/> shows. A click landing before the app takes over the
    /// prerendered header is swallowed, so the menu is clicked until it does (See SmokeTestsBase).
    /// </summary>
    protected async Task OpenAppMenu(IPage page, ILocator item)
    {
        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromMinutes(2);

        while (true)
        {
            // Already open: the menu button toggles, so another click would close it.
            if (await item.IsVisibleAsync())
                break;

            // The drop menu itself, not its chevron: AppMenu hides the chevron under 600px, which is every phone-sized
            // hybrid WebView.
            await page.Locator("header .bit-drm").First.ClickAsync();

            try
            {
                await Expect(item).ToBeVisibleAsync(new() { Timeout = 5_000 });
                break;
            }
            catch (PlaywrightException) when (DateTimeOffset.UtcNow < deadline)
            {
            }
        }
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

        var emailBox = page.GetByPlaceholder(AppStrings.EmailPlaceholder);

        await emailBox.FillEnsuringStable(email);
        await page.GetByPlaceholder(AppStrings.PasswordPlaceholder).FillEnsuringStable(password);

        // FillEnsuringStable watches one field. The hydration that resets the form can land while the password is being
        // filled, and then it is the email that is left empty - the password, typed after it, survives.
        if (await emailBox.InputValueAsync() != email)
            await emailBox.FillEnsuringStable(email);

        await page.GetByRole(AriaRole.Button, new() { Name = AppStrings.Continue, Exact = true }).ClickAsync();

        // Leaving the sign in page is the app saying it took the credentials; ChangeCulture waits for the layout swap
        // that follows.
        await Expect(page).Not.ToHaveURLAsync(new Regex("sign-in", RegexOptions.IgnoreCase));

        await DeleteSessionAtCleanup(page);
    }

    /// <summary>
    /// Signs <see cref="StoreAdmin"/> in to AdminPanel through the app's own form. The deployment has two factor
    /// authentication on for it, so the code comes the "another way" TfaPanel offers - by mail - read off the mail job.
    /// </summary>
    protected async Task SignInStoreAdmin(IPage page)
    {
        var mcp = (await DeployedApiClientProvider.GetGlobalApiClient(TestContext.CancellationToken)).McpClient!;

        await page.GotoAsync(DeployedApps.AdminPanel);
        await WaitUntilInteractive(page);
        await page.GoToInApp(PageUrls.SignIn);

        var storeAdminEmailBox = page.GetByPlaceholder(AppStrings.EmailPlaceholder);

        await storeAdminEmailBox.FillEnsuringStable(StoreAdmin.Email);
        await page.GetByPlaceholder(AppStrings.PasswordPlaceholder).FillEnsuringStable(StoreAdmin.Password);

        // See SignIn: a reset that lands while the password is filled leaves the email empty.
        if (await storeAdminEmailBox.InputValueAsync() != StoreAdmin.Email)
            await storeAdminEmailBox.FillEnsuringStable(StoreAdmin.Email);

        await page.GetByRole(AriaRole.Button, new() { Name = AppStrings.Continue, Exact = true }).ClickAsync();

        var getCode = page.GetByRole(AriaRole.Button, new() { Name = AppStrings.TfaPanelAnotherWayGetCode });
        await Expect(getCode).ToBeVisibleAsync();

        var mailedBefore = await mcp.HangfireJobIds(StoreAdmin.Email, TestContext.CancellationToken);
        await getCode.ClickAsync();

        var job = await mcp.WaitForHangfireJob(StoreAdmin.Email, mailedBefore, TestContext.CancellationToken);
        var code = job.SixDigitInArguments();
        Assert.IsFalse(string.IsNullOrWhiteSpace(code), $"The two factor mail carried no 6-digit code. Arguments: '{job.DecodedArguments()}'.");

        // TfaPanel submits on its own once all six digits are in.
        await BitOtpInputUtils.FillOtpInputs(page, code!);

        await Expect(page).Not.ToHaveURLAsync(new Regex("sign-in", RegexOptions.IgnoreCase));

        await DeleteSessionAtCleanup(page);
    }

    /// <summary>
    /// Signs a brand-new account in through the app's own form, which is also what creates it: the sign in endpoint
    /// registers an account for an address it does not know (See <c>IdentityController.SignIn</c>), mails it a
    /// confirmation code and answers "not confirmed" - which is what swaps the credentials form for the OTP panel. The
    /// code comes off the mail job, so the account ends up confirmed and signed in.
    /// <para>
    /// <paramref name="password"/> is what gets typed, not what the account ends up with: auto-provisioning calls
    /// <c>CreateUserWithDemoRole</c> without it and gives the row a random one instead, so an account made this way has
    /// no password its owner knows until she sets one through "forgot password".
    /// </para>
    /// </summary>
    protected async Task SignInNewUser(IPage page, string email, string password, McpClient mcp)
    {
        await page.GoToInApp(PageUrls.SignIn);
        await WaitUntilInteractive(page);

        await page.GetByPlaceholder(AppStrings.EmailPlaceholder).FillEnsuringStable(email);
        await page.GetByPlaceholder(AppStrings.PasswordPlaceholder).FillEnsuringStable(password);

        var mailedBefore = await mcp.HangfireJobIds(email, TestContext.CancellationToken);
        await page.GetByRole(AriaRole.Button, new() { Name = AppStrings.Continue, Exact = true }).ClickAsync();
        await page.Locator(".bit-otp-inp").First.WaitForAsync();

        var token = await WaitForSixDigit(mcp, email, mailedBefore);
        await BitOtpInputUtils.FillOtpInputs(page, token);

        await Expect(page).Not.ToHaveURLAsync(new Regex("sign-in", RegexOptions.IgnoreCase));
    }

    /// <summary>
    /// Answers the elevated access prompt if one opened, and does nothing if none did - a session that signed in with
    /// two factor authentication is elevated from the start, so the operation simply goes through.
    /// <para>
    /// The prompt is addressed as the modal that holds an OTP input rather than by its text, for two reasons: the page
    /// under it may render boxes of its own (the settings page has the two factor section's, collapsed but rendered),
    /// and the text is in whatever culture the app is running in.
    /// </para>
    /// <para>
    /// <c>recipient</c> is the mail address or phone number the code was sent to, as it appears in the job's arguments,
    /// and <c>sentBefore</c> is what <see cref="McpHangfireExtensions.HangfireJobIds"/> returned before the action -
    /// without it an older code for the same recipient could be read instead of this one.
    /// </para>
    /// </summary>
    protected async Task FillElevatedAccessIfPrompted(IPage page, string recipient, IReadOnlyCollection<string> sentBefore, McpClient mcp)
    {
        var prompt = page.Locator(".bit-mdl", new() { Has = page.Locator(".bit-otp-inp") });

        try
        {
            await prompt.WaitForAsync(new() { Timeout = 15_000 });
        }
        catch (TimeoutException)
        {
            return;
        }

        var token = await WaitForSixDigit(mcp, recipient, sentBefore);
        await BitOtpInputUtils.FillOtpInputs(prompt, token);
    }

    /// <summary>
    /// The six digit code in the next job addressed to <paramref name="argumentContains"/> - a mail's rendered body or
    /// an sms' text, whichever the deployment sent it in.
    /// </summary>
    protected async Task<string> WaitForSixDigit(McpClient mcp, string argumentContains, IReadOnlyCollection<string> sentBefore)
    {
        var job = await mcp.WaitForHangfireJob(argumentContains, sentBefore, TestContext.CancellationToken);
        var token = job.SixDigitInArguments();

        Assert.IsFalse(string.IsNullOrWhiteSpace(token),
            $"The Hangfire job matching '{argumentContains}' had no 6-digit token. Arguments: '{job.DecodedArguments()}'.");

        return token!;
    }

    /// <summary>
    /// A session left behind stays signed in on a live deployment. Exactly this page's session, read from its access
    /// token: tests sign the same seeded user in concurrently, so "that user's sessions" would sign the others out.
    /// </summary>
    protected async Task DeleteSessionAtCleanup(IPage page)
    {
        var sessionId = await GetSessionId(page);

        RegisterForCleanup(() => DeleteSession(sessionId));
    }

    /// <summary>The session the page is signed in with, read from its access token.</summary>
    protected static async Task<Guid> GetSessionId(IPage page) => (await ReadSignedInUser(page)).GetSessionId();

    /// <summary>The user the page is signed in as, read from its access token.</summary>
    protected static async Task<Guid> GetUserId(IPage page) => (await ReadSignedInUser(page)).GetUserId();

    private static async Task<ClaimsPrincipal> ReadSignedInUser(IPage page)
    {
        // WebStorageService: localStorage, or sessionStorage when the user was not remembered. A hybrid WebView keeps
        // it natively instead, which this cannot read.
        var accessToken = await (await page.WaitForFunctionAsync("() => localStorage.getItem('access_token') ?? sessionStorage.getItem('access_token')",
            options: new() { Timeout = 15_000 })).JsonValueAsync<string>();

        return IAuthTokenProvider.ParseAccessToken(accessToken, validateExpiry: false);
    }

    /// <inheritdoc cref="DeployedApiClientProvider.SkipWithoutGlobalAdminCredentials"/>
    /// <remarks>Task-returning so the call sites read like the awaits around them.</remarks>
    protected static Task SkipWithoutGlobalAdminCredentials()
    {
        DeployedApiClientProvider.SkipWithoutGlobalAdminCredentials();

        return Task.CompletedTask;
    }

    /// <summary>Not on the test's own token: a canceled or timed out test still owes the deployment its cleanup.</summary>
    private static async Task DeleteSession(Guid sessionId)
    {
        var globalApiClient = await DeployedApiClientProvider.GetGlobalApiClient(CancellationToken.None);
        await using var dbContext = await globalApiClient.DbContextFactory!.CreateDbContextAsync(CancellationToken.None);

        await dbContext.UserSessions.IgnoreQueryFilters()
            .Where(session => session.Id == sessionId)
            .ExecuteDeleteAsync(CancellationToken.None);
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
