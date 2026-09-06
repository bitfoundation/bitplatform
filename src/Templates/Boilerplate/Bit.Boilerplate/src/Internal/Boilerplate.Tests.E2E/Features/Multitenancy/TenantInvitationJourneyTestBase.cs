using Boilerplate.Server.Api.Features.Tenants;
using Boilerplate.Tests.Infrastructure.Components;
using Boilerplate.Server.Api.Features.Identity.Resources;

namespace Boilerplate.Tests.E2E.Features.Multitenancy;

/// <summary>
/// The invitation journey against the AdminPanel deployment, run once per platform the invitee can be on. The only
/// thing that differs between them is how the invitee's app receives the mailed link.
/// </summary>
public abstract class TenantInvitationJourneyTestBase : AppTestBase
{
    private static readonly Uri adminPanelAppUrl = new(DeployedApps.AdminPanel);
    private const string password = "123456";
    private const string e2eTenantFallback = "e2e";

    /// <summary>
    /// Opens the link the invitation mailed. On a hybrid app it goes to the OS rather than to the WebView, which is
    /// the point: the app link has to route into the installed app.
    /// </summary>
    protected abstract Task OpenInvitationLink(IPage page, string invitationLink);

    [TestMethod]
    public async Task InvitedUser_Should_JoinE2ETenantFromPersianInvitation_ThenLeave()
    {
        var configuration = DeployedApiClientProvider.Services.GetRequiredService<IConfiguration>();
        var tenantAdminEmail = configuration["TenantAdminEmail"]!;
        var tenantAdminPassword = configuration["TenantAdminPassword"]!;
        var e2eTenantName = configuration["E2ETenantName"] ?? e2eTenantFallback;

        var globalApiClient = await DeployedApiClientProvider.GetGlobalApiClient(TestContext.CancellationToken);
        var mcp = globalApiClient.McpClient!;
        await using var dbContext = await globalApiClient.DbContextFactory!.CreateDbContextAsync(TestContext.CancellationToken);

        var tenant = await dbContext.Tenants.IgnoreQueryFilters()
            .SingleAsync(t => t.Name == e2eTenantName, TestContext.CancellationToken);

        var tenantDisplayName = tenant.DisplayName!;
        var aUserToBeInvitedEmail = $"{Guid.NewGuid()}@bitplatform.dev";
        var faCulture = CultureInfoManager.GetCultureInfo("fa-IR")!;

        try
        {
            var adminPanelAppForToBeInvitedUser = await OpenApp(App.AdminPanel);
            await SignInNewUser(adminPanelAppForToBeInvitedUser, aUserToBeInvitedEmail, mcp);

            await ChangeCultureToPersian(adminPanelAppForToBeInvitedUser);
            await GoTo(adminPanelAppForToBeInvitedUser, PageUrls.Home, "fa-IR");
            await WaitUntilInteractive(adminPanelAppForToBeInvitedUser);

            await using var tenantAdminBrowserContext = await NewBrowserContext(adminPanelAppUrl);
            var tenantAdminBrowser = await tenantAdminBrowserContext.NewPageAsync();
            await tenantAdminBrowser.GotoAsync(adminPanelAppUrl.ToString(), new() { WaitUntil = WaitUntilState.NetworkIdle });
            await SignInExistingUser(tenantAdminBrowser, tenantAdminEmail, tenantAdminPassword, mcp);
            await SwitchToTenant(tenantAdminBrowser, e2eTenantName);

            // The sign-in above already mailed this address, so the invitation is the job that is not one of these.
            var hangfireJobIdsRelatedToInviteesEmailAddress = await mcp.HangfireJobIds(aUserToBeInvitedEmail, TestContext.CancellationToken);

            await InviteUser(tenantAdminBrowser, aUserToBeInvitedEmail, tenantAdminEmail, mcp);

            var invitation = await mcp.WaitForHangfireJob(aUserToBeInvitedEmail, hangfireJobIdsRelatedToInviteesEmailAddress, TestContext.CancellationToken);

            var body = invitation.DecodedArguments();
            Assert.Contains(AppStrings.ResourceManager.GetString(nameof(AppStrings.Invite), faCulture)!, body, "The Hangfire job for the invitation must carry the Persian copy.");
            Assert.Contains("lang=\"fa-IR\"", body, "The invitation must declare fa-IR.");
            Assert.Contains(EmailStrings.ResourceManager.GetString(nameof(EmailStrings.TenantInvitationLinkMessage), faCulture)!, body,
                "The invitation body must be rendered in the recipient's Persian session culture.");

            var invitationLink = invitation.HttpLinksInArguments().First();

            // Parked off the route the link opens, so that the link being what opened the app stays observable.
            await GoTo(adminPanelAppForToBeInvitedUser, PageUrls.Settings, "fa-IR");

            await OpenInvitationLink(adminPanelAppForToBeInvitedUser, invitationLink);
            await AcceptInvitation(adminPanelAppForToBeInvitedUser, e2eTenantName, tenantDisplayName, faCulture);

            await AssertUserInTenantUsersList(tenantAdminBrowser, aUserToBeInvitedEmail, shouldExist: true);

            await AssertDashboardAccessible(adminPanelAppForToBeInvitedUser, accessible: true, faCulture);

            await LeaveTenant(adminPanelAppForToBeInvitedUser, aUserToBeInvitedEmail, faCulture, mcp);

            await AssertDashboardAccessible(adminPanelAppForToBeInvitedUser, accessible: false, faCulture);

            await AssertUserInTenantUsersList(tenantAdminBrowser, aUserToBeInvitedEmail, shouldExist: false);
        }
        finally
        {
            await DeleteUser(dbContext, aUserToBeInvitedEmail);
        }
    }

    private async Task SignInNewUser(IPage page, string email, McpClient mcp)
    {
        await GoTo(page, PageUrls.SignIn);
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

    private async Task SignInExistingUser(IPage page, string email, string userPassword, McpClient mcp)
    {
        await page.GotoAsync(AppUrl(PageUrls.SignIn), new() { WaitUntil = WaitUntilState.NetworkIdle });
        await WaitUntilInteractive(page);

        await page.GetByPlaceholder(AppStrings.EmailPlaceholder).FillEnsuringStable(email);
        await page.GetByPlaceholder(AppStrings.PasswordPlaceholder).FillEnsuringStable(userPassword);

        var mailedBefore = await mcp.HangfireJobIds(email, TestContext.CancellationToken);
        await page.GetByRole(AriaRole.Button, new() { Name = AppStrings.Continue, Exact = true }).ClickAsync();
        await FillElevatedAccessIfPrompted(page, email, mailedBefore, mcp);

        await Expect(page).Not.ToHaveURLAsync(new Regex("sign-in", RegexOptions.IgnoreCase));
    }

    private async Task ChangeCultureToPersian(IPage page)
    {
        var faDisplayName = CultureInfoManager.SupportedCultures.First(sc => sc.Culture.Name == "fa-IR").DisplayName;

        // The drop menu itself, not its chevron: AppMenu hides the chevron under 600px, which is every phone-sized
        // hybrid WebView.
        await page.Locator("header .bit-drm").First.ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = AppStrings.Language }).ClickAsync();
        await Expect(page.GetByText(AppStrings.SelectLanguage)).ToBeVisibleAsync();
        await page.GetByText(faDisplayName, new() { Exact = true }).ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    /// <summary>No Switch button on the card means this tenant is already the selected one.</summary>
    private async Task SwitchToTenant(IPage page, string tenantName)
    {
        await page.GotoAsync(AppUrl(PageUrls.ManageMyTenants), new() { WaitUntil = WaitUntilState.NetworkIdle });
        await Expect(page.GetByText(tenantName).First).ToBeVisibleAsync();

        var cardSwitch = page.Locator(".tenant-card", new() { HasText = tenantName })
            .GetByRole(AriaRole.Button, new() { Name = AppStrings.Switch });

        if (await cardSwitch.CountAsync() > 0)
        {
            await cardSwitch.First.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }
    }

    private async Task InviteUser(IPage page, string email, string tenantAdminEmail, McpClient mcp)
    {
        await page.GotoAsync(AppUrl(PageUrls.ManageMyTenants), new() { WaitUntil = WaitUntilState.NetworkIdle });

        var inviteHeaderPrefix = AppStrings.InviteUserToTenant.Replace("{0}", "").Trim();
        await page.GetByText(inviteHeaderPrefix).First.ClickAsync();
        await page.GetByPlaceholder(AppStrings.EmailPlaceholder).FillEnsuringStable(email);

        var mailedBefore = await mcp.HangfireJobIds(tenantAdminEmail, TestContext.CancellationToken);
        await page.GetByRole(AriaRole.Button, new() { Name = AppStrings.Invite, Exact = true }).ClickAsync();
        await FillElevatedAccessIfPrompted(page, tenantAdminEmail, mailedBefore, mcp);

        await Expect(BitSnackBarUtils.GetSnackBar(page, AppStrings.UserInvitedSuccessfullyMessage)).ToBeVisibleAsync();
    }

    private async Task AcceptInvitation(IPage page, string tenantName, string tenantTitle, CultureInfo faCulture)
    {
        await GoTo(page, PageUrls.ManageMyTenants, "fa-IR");
        await WaitUntilInteractive(page);

        var accept = Localized(nameof(AppStrings.AcceptInvitation), faCulture);
        await Expect(page.GetByText(tenantName).Or(page.GetByText(tenantTitle)).First).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Button, new() { NameRegex = LocalizedButton(accept, AppStrings.AcceptInvitation) }).ClickAsync();

        await Expect(page.GetByRole(AriaRole.Button, new() { NameRegex = LocalizedButton(accept, AppStrings.AcceptInvitation) }))
            .ToHaveCountAsync(0);
    }

    private async Task LeaveTenant(IPage page, string email, CultureInfo faCulture, McpClient mcp)
    {
        await GoTo(page, PageUrls.ManageMyTenants, "fa-IR");

        var leave = Localized(nameof(AppStrings.LeaveTenant), faCulture);
        var yes = Localized(nameof(AppStrings.Yes), faCulture);
        await page.GetByRole(AriaRole.Button, new() { NameRegex = LocalizedButton(leave, AppStrings.LeaveTenant) }).ClickAsync();

        var mailedBefore = await mcp.HangfireJobIds(email, TestContext.CancellationToken);
        await page.GetByRole(AriaRole.Button, new() { NameRegex = LocalizedButton(yes, AppStrings.Yes) }).ClickAsync();
        await FillElevatedAccessIfPrompted(page, email, mailedBefore, mcp);

        var accept = Localized(nameof(AppStrings.AcceptInvitation), faCulture);
        await Expect(page.GetByRole(AriaRole.Button, new() { NameRegex = LocalizedButton(accept, AppStrings.AcceptInvitation) }))
            .ToBeVisibleAsync();
    }

    private async Task FillElevatedAccessIfPrompted(IPage page, string recipient, IReadOnlyCollection<string> mailedBefore, McpClient mcp)
    {
        var otp = page.Locator(".bit-otp-inp").First;
        try
        {
            await otp.WaitForAsync(new() { Timeout = 15_000 });
        }
        catch (TimeoutException)
        {
            return;
        }

        var token = await WaitForSixDigit(mcp, recipient, mailedBefore);
        await BitOtpInputUtils.FillOtpInputs(page, token);
    }

    private async Task<string> WaitForSixDigit(McpClient mcp, string argumentContains, IReadOnlyCollection<string> mailedBefore)
    {
        var job = await mcp.WaitForHangfireJob(argumentContains, mailedBefore, TestContext.CancellationToken);
        var token = job.SixDigitInArguments();
        Assert.IsFalse(string.IsNullOrWhiteSpace(token),
            $"The Hangfire job matching '{argumentContains}' had no 6-digit token. Arguments: '{job.DecodedArguments()}'.");
        return token!;
    }

    private async Task AssertUserInTenantUsersList(IPage page, string email, bool shouldExist)
    {
        await page.GotoAsync(AppUrl(PageUrls.Users), new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.GetByPlaceholder(AppStrings.SearchUsersPlaceholder).FillAsync(email);

        var userItem = page.GetByText(email);
        if (shouldExist)
        {
            await Expect(userItem.First).ToBeVisibleAsync();
        }
        else
        {
            await Expect(page.GetByText(AppStrings.NoUserMessage)).ToBeVisibleAsync();
            await Expect(userItem).ToHaveCountAsync(0);
        }
    }

    private async Task AssertDashboardAccessible(IPage page, bool accessible, CultureInfo faCulture)
    {
        await GoTo(page, PageUrls.Dashboard, "fa-IR");

        var expected = Localized(accessible ? nameof(AppStrings.DashboardPageTitle) : nameof(AppStrings.NotAuthorizedPageTitle), faCulture);
        var english = accessible ? AppStrings.DashboardPageTitle : AppStrings.NotAuthorizedPageTitle;
        await Expect(page).ToHaveTitleAsync(new Regex($"{Regex.Escape(expected)}|{Regex.Escape(english)}"));
    }

    /// <summary>
    /// Moves the invitee's app to <paramref name="path"/> on whichever origin that app already lives at: the
    /// deployment in a browser, the WebView's own one in a hybrid app - where the deployment's url would leave the
    /// app for the website. The tenant admin's browser is always on the deployment, so it uses <see cref="AppUrl"/>.
    /// </summary>
    private Task GoTo(IPage page, string path, string? culture = null)
        => page.GotoAsync(new Uri(new Uri(page.Url), RouteOf(path, culture)).ToString(), new() { WaitUntil = WaitUntilState.NetworkIdle });

    /// <summary>The app relative route, culture prefixed the way the pages' own route templates are.</summary>
    private static string RouteOf(string path, string? culture = null) => culture is null ? path : $"/{culture}{path}";

    /// <summary>The same route on the deployed web app.</summary>
    private static string AppUrl(string path, string? culture = null) => new Uri(adminPanelAppUrl, RouteOf(path, culture)).ToString();

    private static string Localized(string key, CultureInfo culture)
        => AppStrings.ResourceManager.GetString(key, culture) ?? key;

    private static Regex LocalizedButton(string localized, string english)
        => new($"{Regex.Escape(localized)}|{Regex.Escape(english)}");

    private static async Task DeleteUser(AppDbContext db, string email)
    {
        var normalized = email.ToUpperInvariant();
        var userId = await db.Users.IgnoreQueryFilters()
            .Where(user => user.NormalizedEmail == normalized)
            .Select(user => user.Id)
            .SingleOrDefaultAsync(CancellationToken.None);

        // This runs in the journey's finally, so a run that failed before the sign-up has nothing to delete - and
        // throwing here would replace the failure that is the reason to be here at all.
        if (userId == default)
            return;

        var sessionIds = await db.UserSessions.IgnoreQueryFilters()
            .Where(session => session.UserId == userId)
            .Select(session => session.Id)
            .ToListAsync(CancellationToken.None);

        if (sessionIds.Count > 0)
        {
            await db.PushNotificationSubscriptions.IgnoreQueryFilters()
                .Where(subscription => subscription.UserSessionId != null && sessionIds.Contains(subscription.UserSessionId.Value))
                .ExecuteDeleteAsync(CancellationToken.None);
        }

        await db.UserSessions.IgnoreQueryFilters().Where(session => session.UserId == userId).ExecuteDeleteAsync(CancellationToken.None);
        await db.TenantUsers.IgnoreQueryFilters().Where(membership => membership.UserId == userId).ExecuteDeleteAsync(CancellationToken.None);
        await db.UserRoles.IgnoreQueryFilters().Where(role => role.UserId == userId).ExecuteDeleteAsync(CancellationToken.None);
        await db.WebAuthnCredential.IgnoreQueryFilters().Where(credential => credential.UserId == userId).ExecuteDeleteAsync(CancellationToken.None);
        await db.Users.IgnoreQueryFilters().Where(user => user.Id == userId).ExecuteDeleteAsync(CancellationToken.None);
    }
}
