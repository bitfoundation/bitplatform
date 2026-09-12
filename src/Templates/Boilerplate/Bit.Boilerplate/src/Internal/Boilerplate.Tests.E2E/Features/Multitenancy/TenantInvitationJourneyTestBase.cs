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
        var globalApiClient = await DeployedApiClientProvider.GetGlobalApiClient(TestContext.CancellationToken);
        var mcp = globalApiClient.McpClient!;
        await using var dbContext = await globalApiClient.DbContextFactory!.CreateDbContextAsync(TestContext.CancellationToken);

        var configuration = globalApiClient.Services.GetRequiredService<IConfiguration>();
        var tenantAdminEmail = configuration["TenantAdminEmail"]!;
        var tenantAdminPassword = configuration["TenantAdminPassword"]!;
        var e2eTenantName = configuration["E2ETenantName"] ?? e2eTenantFallback;

        var tenant = await dbContext.Tenants.IgnoreQueryFilters()
            .SingleAsync(t => t.Name == e2eTenantName, TestContext.CancellationToken);

        var tenantDisplayName = tenant.DisplayName!;
        var aUserToBeInvitedEmail = $"{Guid.NewGuid()}@bitplatform.dev";
        var faCulture = CultureInfoManager.GetCultureInfo("fa-IR")!;

        try
        {
            var adminPanelAppForToBeInvitedUser = await OpenApp(App.AdminPanel);
            await SignInNewUser(adminPanelAppForToBeInvitedUser, aUserToBeInvitedEmail, password, mcp);

            await ChangeCultureToPersian(adminPanelAppForToBeInvitedUser);
            await adminPanelAppForToBeInvitedUser.GoToInApp(PageUrls.Home);
            await WaitUntilInteractive(adminPanelAppForToBeInvitedUser);

            await using var tenantAdminBrowserContext = await NewBrowserContext(adminPanelAppUrl);
            var tenantAdminBrowser = await tenantAdminBrowserContext.NewPageAsync();
            await Navigate(tenantAdminBrowser, adminPanelAppUrl.ToString());
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
            await adminPanelAppForToBeInvitedUser.GoToInApp(PageUrls.Settings);

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

    private async Task SignInExistingUser(IPage page, string email, string userPassword, McpClient mcp)
    {
        await page.GoToInApp(PageUrls.SignIn);
        await WaitUntilInteractive(page);

        await page.GetByPlaceholder(AppStrings.EmailPlaceholder).FillEnsuringStable(email);
        await page.GetByPlaceholder(AppStrings.PasswordPlaceholder).FillEnsuringStable(userPassword);

        var mailedBefore = await mcp.HangfireJobIds(email, TestContext.CancellationToken);
        await page.GetByRole(AriaRole.Button, new() { Name = AppStrings.Continue, Exact = true }).ClickAsync();
        await FillElevatedAccessIfPrompted(page, email, mailedBefore, mcp);

        await Expect(page).Not.ToHaveURLAsync(new Regex("sign-in", RegexOptions.IgnoreCase));

        // The invitee is deleted along with its sessions; the tenant admin is a seeded user that outlives the run.
        await DeleteSessionAtCleanup(page);
    }

    private Task ChangeCultureToPersian(IPage page) => ChangeCulture(page, "fa-IR");

    /// <summary>No Switch button on the card means this tenant is already the selected one.</summary>
    private async Task SwitchToTenant(IPage page, string tenantName)
    {
        await page.GoToInApp(PageUrls.ManageMyTenants);
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
        await page.GoToInApp(PageUrls.ManageMyTenants);

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
        await page.GoToInApp(PageUrls.ManageMyTenants, "fa-IR");
        await WaitUntilInteractive(page);

        var accept = Localized(nameof(AppStrings.AcceptInvitation), faCulture);
        await Expect(page.GetByText(tenantName).Or(page.GetByText(tenantTitle)).First).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Button, new() { NameRegex = LocalizedButton(accept, AppStrings.AcceptInvitation) }).ClickAsync();

        await Expect(page.GetByRole(AriaRole.Button, new() { NameRegex = LocalizedButton(accept, AppStrings.AcceptInvitation) }))
            .ToHaveCountAsync(0);
    }

    private async Task LeaveTenant(IPage page, string email, CultureInfo faCulture, McpClient mcp)
    {
        await page.GoToInApp(PageUrls.ManageMyTenants, "fa-IR");

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

    private async Task AssertUserInTenantUsersList(IPage page, string email, bool shouldExist)
    {
        await page.GoToInApp(PageUrls.Users);
        await page.GetByPlaceholder(AppStrings.SearchUsersPlaceholder).FillAsync(email);

        await page.GetByRole(AriaRole.Button, new() { Name = AppStrings.Refresh, Exact = true }).ClickAsync();

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
        await page.GoToInApp(PageUrls.Dashboard, "fa-IR");

        var expected = Localized(accessible ? nameof(AppStrings.DashboardPageTitle) : nameof(AppStrings.NotAuthorizedPageTitle), faCulture);
        var english = accessible ? AppStrings.DashboardPageTitle : AppStrings.NotAuthorizedPageTitle;
        await Expect(page).ToHaveTitleAsync(new Regex($"{Regex.Escape(expected)}|{Regex.Escape(english)}"));
    }

    /// <summary>
    /// A real document load, for the two moments that need one: arriving at the app, and the mailed link opening it.
    /// Every hop inside a running app goes through <see cref="PlaywrightPageExtensions.GoToInApp"/> instead.
    /// <para>
    /// The app navigates on its own here - the culture switch redirects the url it was applied on - and where chromium
    /// lets the later navigation win, firefox and webkit raise the interrupted one; so ask again once it settles.
    /// </para>
    /// </summary>
    private static async Task Navigate(IPage page, string url)
    {
        try
        {
            await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.NetworkIdle });
        }
        catch (PlaywrightException exp) when (exp.Message.Contains("interrupted by another navigation", StringComparison.Ordinal)
                                              || exp.Message.Contains("NS_BINDING_ABORTED", StringComparison.Ordinal)
                                              || exp.Message.Contains("net::ERR_ABORTED", StringComparison.Ordinal))
        {
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.NetworkIdle });
        }
    }

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
