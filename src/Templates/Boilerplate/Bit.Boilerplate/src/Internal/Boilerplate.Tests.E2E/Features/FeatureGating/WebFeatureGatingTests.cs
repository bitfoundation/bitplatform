using Microsoft.AspNetCore.Identity;
using Boilerplate.Server.Api.Features.Tenants;
using Boilerplate.Tests.E2E.Infrastructure.Services;

namespace Boilerplate.Tests.E2E.Features.FeatureGating;

/// <summary>
/// A feature granted while its user is signed in reaches her on the next visit to the page it gates, with no sign-out,
/// sign-in or reload: NotAuthorizedPage refreshes the token before deciding, and a refresh re-reads the user's roles.
/// </summary>
[TestClass, TestCategory(TestCategories.Web), Retry(2)]
public partial class WebFeatureGatingTests : AppTestBase
{
    private const string password = "123456";

    protected override IAppOpener AppOpener => new WebAppOpener();

    /// <summary>
    /// A tenant and a member of it are made straight in the database - nothing shared is touched, and everything is
    /// deleted afterwards. The member has no feature, so the dashboard refuses her; granting Dashboard_View through a
    /// user-group of that tenant opens it once she comes back to it.
    /// </summary>
    [TestMethod]
    [DataRow(App.AdminPanel, DisplayName = "AdminPanel (bit Brouter)")]
    [DataRow(App.AdminPanelWasmStandalone, DisplayName = "AdminPanelWasmStandalone (Blazor Router)")]
    public async Task Dashboard_Should_Open_OnceTheFeatureIsGranted_WithoutSigningInAgain(App app)
    {
        await SkipWithoutGlobalAdminCredentials();

        var globalApiClient = await DeployedApiClientProvider.GetGlobalApiClient(TestContext.CancellationToken);
        await using var dbContext = await globalApiClient.DbContextFactory!.CreateDbContextAsync(TestContext.CancellationToken);

        var marker = Guid.NewGuid().ToString("N")[..12];
        var email = $"e2e-{marker}@bitplatform.dev";

        var tenant = new Tenant { Id = Guid.NewGuid(), Name = $"e2e-{marker}", Title = $"E2E {marker}" };
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = $"e2e-{marker}",
            NormalizedUserName = $"E2E-{marker}".ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            CreatedOn = DateTimeOffset.UtcNow
        };
        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, password);

        // Registered before the writes, so a half-made setup is undone too.
        RegisterForCleanup(() => DeleteTenantAndUser(tenant.Id, user.Id));

        await dbContext.Tenants.AddAsync(tenant, TestContext.CancellationToken);
        await dbContext.Users.AddAsync(user, TestContext.CancellationToken);
        // Accepted, so signing in selects this tenant (See IdentityController.GetTenantId).
        await dbContext.TenantUsers.AddAsync(new TenantUser { Id = Guid.NewGuid(), TenantId = tenant.Id, UserId = user.Id, AcceptedOn = DateTimeOffset.UtcNow }, TestContext.CancellationToken);
        await dbContext.SaveChangesAsync(TestContext.CancellationToken);

        var page = await OpenApp(app);
        await WaitUntilInteractive(page);

        await SignIn(page, email, password);
        var sessionId = await GetSessionId(page);

        await page.GoToInApp(PageUrls.Dashboard);
        await Expect(page).ToHaveTitleAsync(AppStrings.NotAuthorizedPageTitle);

        await GrantDashboard(dbContext, tenant.Id, user.Id);

        // Another page first: the dashboard has to be a new visit, not the NotAuthorizedPage already on screen.
        await page.GoToInApp(PageUrls.Settings);
        await page.GoToInApp(PageUrls.Dashboard);
        await Expect(page).ToHaveTitleAsync(AppStrings.DashboardPageTitle);

        Assert.AreEqual(sessionId, await GetSessionId(page), "The dashboard opened on a new session, so it took a sign in rather than a token refresh.");
    }

    /// <summary>The way a tenant admin would: a user-group of the tenant carrying the feature, with her in it.</summary>
    private async Task GrantDashboard(AppDbContext dbContext, Guid tenantId, Guid userId)
    {
        var roleName = $"e2e-{Guid.NewGuid():N}";
        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = roleName,
            NormalizedName = roleName.ToUpperInvariant(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            TenantId = tenantId
        };

        await dbContext.Roles.AddAsync(role, TestContext.CancellationToken);
        await dbContext.RoleClaims.AddAsync(new RoleClaim { RoleId = role.Id, ClaimType = AppClaimTypes.FEATURES, ClaimValue = AppFeatures.AdminPanel.Dashboard_View }, TestContext.CancellationToken);
        await dbContext.UserRoles.AddAsync(new UserRole { UserId = userId, RoleId = role.Id, TenantId = tenantId }, TestContext.CancellationToken);
        await dbContext.SaveChangesAsync(TestContext.CancellationToken);
    }

    /// <summary>Not on the test's token: a canceled or timed out test still owes the deployment its cleanup.</summary>
    private static async Task DeleteTenantAndUser(Guid tenantId, Guid userId)
    {
        var globalApiClient = await DeployedApiClientProvider.GetGlobalApiClient(CancellationToken.None);
        await using var db = await globalApiClient.DbContextFactory!.CreateDbContextAsync(CancellationToken.None);

        var roleIds = await db.Roles.IgnoreQueryFilters().Where(role => role.TenantId == tenantId).Select(role => role.Id).ToListAsync(CancellationToken.None);

        await db.UserRoles.IgnoreQueryFilters().Where(ur => ur.UserId == userId || roleIds.Contains(ur.RoleId)).ExecuteDeleteAsync(CancellationToken.None);
        await db.RoleClaims.IgnoreQueryFilters().Where(rc => roleIds.Contains(rc.RoleId)).ExecuteDeleteAsync(CancellationToken.None);
        await db.Roles.IgnoreQueryFilters().Where(role => role.TenantId == tenantId).ExecuteDeleteAsync(CancellationToken.None);
        await db.UserSessions.IgnoreQueryFilters().Where(session => session.UserId == userId).ExecuteDeleteAsync(CancellationToken.None);
        await db.TenantUsers.IgnoreQueryFilters().Where(tu => tu.UserId == userId || tu.TenantId == tenantId).ExecuteDeleteAsync(CancellationToken.None);
        await db.Users.IgnoreQueryFilters().Where(u => u.Id == userId).ExecuteDeleteAsync(CancellationToken.None);
        await db.Tenants.IgnoreQueryFilters().Where(t => t.Id == tenantId).ExecuteDeleteAsync(CancellationToken.None);
    }
}
