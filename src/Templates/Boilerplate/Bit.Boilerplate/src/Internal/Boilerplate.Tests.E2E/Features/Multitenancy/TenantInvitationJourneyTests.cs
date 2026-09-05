using System.Globalization;
using Boilerplate.Server.Api.Features.Identity.Resources;
using Boilerplate.Server.Api.Features.Tenants;
using Boilerplate.Shared;
using Boilerplate.Shared.Infrastructure.Services;
using Boilerplate.Tests.Infrastructure.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Boilerplate.Tests.E2E.Features.Multitenancy;

[TestClass, TestCategory(TestCategories.Web), Retry(2), DoNotParallelize]
public partial class TenantInvitationJourneyTests : AppsTestBase
{
    protected override IAppOpener AppOpener => new WebAppOpener();

    private static readonly Uri adminPanel = new(DeployedApps.AdminPanel);
    private const string password = "123456";
    private const string e2eTenantFallback = "e2e";

    [TestMethod]
    public async Task InvitedUser_Should_JoinE2ETenantFromPersianInvitation_ThenLeave()
    {
        var configuration = TestHost.Services.GetRequiredService<IConfiguration>();
        var tenantAdminEmail = configuration["TenantAdminEmail"]
            ?? throw new InvalidOperationException("User secrets / env are missing TenantAdminEmail.");
        var tenantAdminPassword = configuration["TenantAdminPassword"]
            ?? throw new InvalidOperationException("User secrets / env are missing TenantAdminPassword.");
        var e2eTenantName = configuration["E2ETenantName"] ?? e2eTenantFallback;

        var backend = await TestHost.GetBackend(TestContext.CancellationToken);
        var mcp = backend.McpClient;
        var db = backend.DbContext;

        var tenant = await db.Tenants.IgnoreQueryFilters()
            .SingleOrDefaultAsync(t => t.Name == e2eTenantName, TestContext.CancellationToken);
        if (tenant is null)
            Assert.Inconclusive($"The '{e2eTenantName}' tenant is not in the deployment's database.");

        var tenantAdmin = await db.Users.IgnoreQueryFilters()
            .SingleOrDefaultAsync(user => user.NormalizedEmail == tenantAdminEmail.ToUpperInvariant(), TestContext.CancellationToken);
        if (tenantAdmin is null)
            Assert.Inconclusive($"TenantAdminEmail '{tenantAdminEmail}' is not in the deployment's database.");
        await TestHost.EnsureUserCanSignIn(db, tenantAdmin.Id, tenantAdminPassword);
        await EnsureTenantAdminOf(db, tenantAdmin.Id, tenant);
        await EnsureDemoRoleHasProductCatalog(db, tenant);

        var tenantTitle = string.IsNullOrWhiteSpace(tenant.Title) ? tenant.Name! : tenant.Title;
        var invitedEmail = $"{Guid.NewGuid()}@bitplatform.dev";
        var faCulture = CultureInfoManager.GetCultureInfo("fa-IR")!;

        try
        {
            var invitedPage = await OpenApp(App.AdminPanel);
            await SignInNewUser(invitedPage, invitedEmail, mcp);

            await ChangeCultureToPersian(invitedPage);
            await invitedPage.GotoAsync(AppUrl(PageUrls.Home, "fa-IR"), new() { WaitUntil = WaitUntilState.NetworkIdle });
            await WaitUntilInteractive(invitedPage);

            await using var adminContext = await NewBrowserContext(adminPanel);
            var adminPage = await adminContext.NewPageAsync();
            await adminPage.GotoAsync(adminPanel.ToString(), new() { WaitUntil = WaitUntilState.NetworkIdle });
            await SignInExistingUser(adminPage, tenantAdminEmail, tenantAdminPassword, mcp);
            await SwitchToTenant(adminPage, e2eTenantName, tenantTitle);

            await InviteUser(adminPage, invitedEmail, tenantAdminEmail, mcp);

            var invitation = await mcp.WaitForHangfireJob(invitedEmail, DateTimeOffset.UtcNow.AddMinutes(-2),
                TestContext.CancellationToken);

            var body = invitation.DecodedArguments();
            Assert.Contains("دعوت", body, "The Hangfire job for the invitation must carry the Persian copy.");
            Assert.Contains("lang=\"fa-IR\"", body, "The invitation must declare fa-IR.");
            Assert.Contains(EmailStrings.ResourceManager.GetString(nameof(EmailStrings.TenantInvitationLinkMessage), faCulture)!, body,
                "The invitation body must be rendered in the recipient's Persian session culture.");

            var invitationLink = invitation.HttpLinksInArguments().FirstOrDefault()
                ?? throw new AssertFailedException("The invitation Hangfire job had no http(s) link to open.");
            await invitedPage.GotoAsync(invitationLink, new() { WaitUntil = WaitUntilState.NetworkIdle });
            await AcceptInvitation(invitedPage, e2eTenantName, tenantTitle, faCulture);

            await AssertUserInTenantUsersList(adminPage, invitedEmail, shouldExist: true);
            await AssertDemoRoleHasProductCatalog(adminPage);

            await AssertDashboardAccessible(invitedPage, accessible: true, faCulture);

            await LeaveTenant(invitedPage, invitedEmail, faCulture, mcp);

            await AssertDashboardAccessible(invitedPage, accessible: false, faCulture);
            await AssertUserInTenantUsersList(adminPage, invitedEmail, shouldExist: false);
        }
        finally
        {
            await DeleteUser(db, invitedEmail);
        }
    }

    private async Task SignInNewUser(IPage page, string email, McpClient mcp)
    {
        await page.GotoAsync(AppUrl(PageUrls.SignIn), new() { WaitUntil = WaitUntilState.NetworkIdle });
        await WaitUntilInteractive(page);

        await page.GetByPlaceholder(AppStrings.EmailPlaceholder).FillEnsuringStable(email);
        await page.GetByPlaceholder(AppStrings.PasswordPlaceholder).FillEnsuringStable(password);

        var since = DateTimeOffset.UtcNow.AddSeconds(-5);
        await page.GetByRole(AriaRole.Button, new() { Name = AppStrings.Continue, Exact = true }).ClickAsync();
        await page.Locator(".bit-otp-inp").First.WaitForAsync();

        var token = await WaitForSixDigit(mcp, email, since);
        await BitOtpInputUtils.FillOtpInputs(page, token);

        await Expect(page).Not.ToHaveURLAsync(new Regex("sign-in", RegexOptions.IgnoreCase));
    }

    private async Task SignInExistingUser(IPage page, string email, string userPassword, McpClient mcp)
    {
        await page.GotoAsync(AppUrl(PageUrls.SignIn), new() { WaitUntil = WaitUntilState.NetworkIdle });
        await WaitUntilInteractive(page);

        await page.GetByPlaceholder(AppStrings.EmailPlaceholder).FillEnsuringStable(email);
        await page.GetByPlaceholder(AppStrings.PasswordPlaceholder).FillEnsuringStable(userPassword);

        var since = DateTimeOffset.UtcNow.AddSeconds(-5);
        await page.GetByRole(AriaRole.Button, new() { Name = AppStrings.Continue, Exact = true }).ClickAsync();
        await FillElevatedAccessIfPrompted(page, email, since, mcp);

        await Expect(page).Not.ToHaveURLAsync(new Regex("sign-in", RegexOptions.IgnoreCase));
    }

    private async Task ChangeCultureToPersian(IPage page)
    {
        var faDisplayName = CultureInfoManager.SupportedCultures.First(sc => sc.Culture.Name == "fa-IR").DisplayName;

        await page.Locator(".menu-chevron").ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = AppStrings.Language }).ClickAsync();
        await Expect(page.GetByText(AppStrings.SelectLanguage)).ToBeVisibleAsync();
        await page.GetByText(faDisplayName, new() { Exact = true }).ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    private async Task SwitchToTenant(IPage page, string tenantName, string tenantTitle)
    {
        await page.GotoAsync(AppUrl(PageUrls.ManageMyTenants), new() { WaitUntil = WaitUntilState.NetworkIdle });
        await Expect(page.GetByText(tenantName).First).ToBeVisibleAsync();

        var switchButton = page.GetByRole(AriaRole.Button, new() { Name = AppStrings.Switch });
        if (await switchButton.CountAsync() > 0 && await switchButton.First.IsVisibleAsync())
        {
            var card = page.Locator(".tenant-card", new() { HasText = tenantName });
            var cardSwitch = card.GetByRole(AriaRole.Button, new() { Name = AppStrings.Switch });
            if (await cardSwitch.CountAsync() > 0)
            {
                await cardSwitch.ClickAsync();
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            }
        }

        _ = tenantTitle;
    }

    private async Task InviteUser(IPage page, string email, string tenantAdminEmail, McpClient mcp)
    {
        await page.GotoAsync(AppUrl(PageUrls.ManageMyTenants), new() { WaitUntil = WaitUntilState.NetworkIdle });

        var inviteHeaderPrefix = AppStrings.InviteUserToTenant.Replace("{0}", "").Trim();
        await page.GetByText(inviteHeaderPrefix).First.ClickAsync();
        await page.GetByPlaceholder(AppStrings.EmailPlaceholder).FillEnsuringStable(email);

        var since = DateTimeOffset.UtcNow.AddSeconds(-5);
        await page.GetByRole(AriaRole.Button, new() { Name = AppStrings.Invite, Exact = true }).ClickAsync();
        await FillElevatedAccessIfPrompted(page, tenantAdminEmail, since, mcp);

        await Expect(BitSnackBarUtils.GetSnackBar(page, AppStrings.UserInvitedSuccessfullyMessage)).ToBeVisibleAsync();
    }

    private async Task AcceptInvitation(IPage page, string tenantName, string tenantTitle, CultureInfo faCulture)
    {
        await page.GotoAsync(AppUrl(PageUrls.ManageMyTenants, "fa-IR"), new() { WaitUntil = WaitUntilState.NetworkIdle });
        await WaitUntilInteractive(page);

        var accept = Localized(nameof(AppStrings.AcceptInvitation), faCulture);
        await Expect(page.GetByText(tenantName).Or(page.GetByText(tenantTitle)).First).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Button, new() { NameRegex = LocalizedButton(accept, AppStrings.AcceptInvitation) }).ClickAsync();

        await Expect(page.GetByRole(AriaRole.Button, new() { NameRegex = LocalizedButton(accept, AppStrings.AcceptInvitation) }))
            .ToHaveCountAsync(0);
    }

    private async Task LeaveTenant(IPage page, string email, CultureInfo faCulture, McpClient mcp)
    {
        await page.GotoAsync(AppUrl(PageUrls.ManageMyTenants, "fa-IR"), new() { WaitUntil = WaitUntilState.NetworkIdle });

        var leave = Localized(nameof(AppStrings.LeaveTenant), faCulture);
        var yes = Localized(nameof(AppStrings.Yes), faCulture);
        await page.GetByRole(AriaRole.Button, new() { NameRegex = LocalizedButton(leave, AppStrings.LeaveTenant) }).ClickAsync();

        var since = DateTimeOffset.UtcNow.AddSeconds(-5);
        await page.GetByRole(AriaRole.Button, new() { NameRegex = LocalizedButton(yes, AppStrings.Yes) }).ClickAsync();
        await FillElevatedAccessIfPrompted(page, email, since, mcp);

        var accept = Localized(nameof(AppStrings.AcceptInvitation), faCulture);
        await Expect(page.GetByRole(AriaRole.Button, new() { NameRegex = LocalizedButton(accept, AppStrings.AcceptInvitation) }))
            .ToBeVisibleAsync();
    }

    private async Task FillElevatedAccessIfPrompted(IPage page, string recipient, DateTimeOffset since, McpClient mcp)
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

        var token = await WaitForSixDigit(mcp, recipient, since);
        await BitOtpInputUtils.FillOtpInputs(page, token);
    }

    private async Task<string> WaitForSixDigit(McpClient mcp, string argumentContains, DateTimeOffset since)
    {
        var job = await mcp.WaitForHangfireJob(argumentContains, since, TestContext.CancellationToken);
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

    private async Task AssertDemoRoleHasProductCatalog(IPage page)
    {
        await page.GotoAsync(AppUrl(PageUrls.Roles), new() { WaitUntil = WaitUntilState.NetworkIdle });
        var rolesCard = page.Locator(".roles-card");
        await rolesCard.GetByPlaceholder(AppStrings.SearchRolesPlaceholder).FillAsync(AppRoles.Demo);
        await Expect(rolesCard.GetByText(AppRoles.TenantAdmin, new() { Exact = true })).ToBeHiddenAsync();
        await rolesCard.GetByText(AppRoles.Demo, new() { Exact = true }).ClickAsync();
        await page.GetByText(AppStrings.Features, new() { Exact = true }).ClickAsync();

        var toggle = page.GetByText(nameof(AppFeatures.AdminPanel.ProductCatalog_Manage), new() { Exact = true })
            .Locator("xpath=following::button[1]");
        await Expect(toggle).ToBeEnabledAsync();
        await Expect(toggle.Locator(".bit-icon--RemoveFrom")).ToBeVisibleAsync();
    }

    private async Task AssertDashboardAccessible(IPage page, bool accessible, CultureInfo faCulture)
    {
        await page.GotoAsync(AppUrl(PageUrls.Dashboard, "fa-IR"), new() { WaitUntil = WaitUntilState.NetworkIdle });

        var expected = Localized(accessible ? nameof(AppStrings.DashboardPageTitle) : nameof(AppStrings.NotAuthorizedPageTitle), faCulture);
        var english = accessible ? AppStrings.DashboardPageTitle : AppStrings.NotAuthorizedPageTitle;
        await Expect(page).ToHaveTitleAsync(new Regex($"{Regex.Escape(expected)}|{Regex.Escape(english)}"));
    }

    private async Task WaitUntilInteractive(IPage page)
    {
        await Expect(page.Locator("main .main-container").First)
            .ToBeVisibleAsync(new() { Timeout = (float)TimeSpan.FromMinutes(2).TotalMilliseconds });
    }

    private static string AppUrl(string path, string? culture = null)
    {
        var relative = culture is null ? path.TrimStart('/') : $"{culture}{path}";
        return new Uri(adminPanel, relative).ToString();
    }

    private static string Localized(string key, CultureInfo culture)
        => AppStrings.ResourceManager.GetString(key, culture) ?? key;

    private static Regex LocalizedButton(string localized, string english)
        => new($"{Regex.Escape(localized)}|{Regex.Escape(english)}");

    private static async Task EnsureTenantAdminOf(AppDbContext db, Guid userId, Tenant tenant)
    {
        var tenantAdminRoleId = await db.Roles
            .Where(role => role.Name == AppRoles.TenantAdmin && role.TenantId == tenant.Id)
            .Select(role => (Guid?)role.Id)
            .SingleOrDefaultAsync();

        if (tenantAdminRoleId is null)
            Assert.Inconclusive($"The '{tenant.Name}' tenant has no {AppRoles.TenantAdmin} role.");

        var membership = await db.TenantUsers
            .SingleOrDefaultAsync(item => item.TenantId == tenant.Id && item.UserId == userId);

        if (membership is null)
        {
            await db.TenantUsers.AddAsync(new TenantUser
            {
                TenantId = tenant.Id,
                UserId = userId,
                AcceptedOn = DateTimeOffset.UtcNow
            });
        }
        else if (membership.AcceptedOn is null)
        {
            membership.AcceptedOn = DateTimeOffset.UtcNow;
        }

        if (await db.UserRoles.AnyAsync(userRole => userRole.UserId == userId && userRole.RoleId == tenantAdminRoleId.Value) is false)
        {
            await db.UserRoles.AddAsync(new UserRole
            {
                UserId = userId,
                RoleId = tenantAdminRoleId.Value,
                TenantId = tenant.Id
            });
        }

        await db.SaveChangesAsync();
    }

    private static async Task EnsureDemoRoleHasProductCatalog(AppDbContext db, Tenant tenant)
    {
        var demoRoleId = await db.Roles
            .Where(role => role.Name == AppRoles.Demo && role.TenantId == tenant.Id)
            .Select(role => (Guid?)role.Id)
            .SingleOrDefaultAsync();

        if (demoRoleId is null)
            Assert.Inconclusive($"The '{tenant.Name}' tenant has no {AppRoles.Demo} role.");

        var feature = AppFeatures.AdminPanel.ProductCatalog_Manage;
        if (await db.RoleClaims.AnyAsync(claim => claim.RoleId == demoRoleId && claim.ClaimType == AppClaimTypes.FEATURES && claim.ClaimValue == feature))
            return;

        await db.RoleClaims.AddAsync(new RoleClaim
        {
            RoleId = demoRoleId.Value,
            ClaimType = AppClaimTypes.FEATURES,
            ClaimValue = feature
        });
        await db.SaveChangesAsync();
    }

    private static async Task DeleteUser(AppDbContext db, string email)
    {
        var normalized = email.ToUpperInvariant();
        var userId = await db.Users.IgnoreQueryFilters()
            .Where(user => user.NormalizedEmail == normalized)
            .Select(user => user.Id)
            .SingleOrDefaultAsync(CancellationToken.None);

        if (userId == Guid.Empty)
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
