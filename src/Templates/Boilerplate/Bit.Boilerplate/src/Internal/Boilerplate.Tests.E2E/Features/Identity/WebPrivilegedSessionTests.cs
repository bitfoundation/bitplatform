using Microsoft.AspNetCore.Identity;
using Boilerplate.Server.Api.Features.Tenants;
using Boilerplate.Tests.E2E.Infrastructure.Services;

namespace Boilerplate.Tests.E2E.Features.Identity;

/// <summary>
/// PRIVILEGED_ACCESS on the live AdminPanel: a user may hold <c>Identity:MaxPrivilegedSessionsCount</c> (3 on the
/// deployment) privileged sessions at once, so the fourth device gets a session without it and the dashboard refuses
/// it - until one of the other three signs out, and a refresh earns the privilege back (See
/// IdentityController.UpdateUserSessionPrivilegeStatus, which re-counts on every token refresh).
/// </summary>
[TestClass, TestCategory(TestCategories.Web), Retry(2)]
public partial class WebPrivilegedSessionTests : AppTestBase
{
    private const string password = "123456";

    /// <summary>The deployment's own <c>Identity:MaxPrivilegedSessionsCount</c>.</summary>
    private const int maxPrivilegedSessions = 3;

    protected override IAppOpener AppOpener => new WebAppOpener();

    /// <summary>
    /// A tenant and a user of their own, made with the DbContext: the privileged count is per user, and a brand-new
    /// one starts at zero - any seeded user carries whatever the rest of the suite left signed in.
    /// </summary>
    [TestMethod]
    public async Task TheFourthDevice_Should_BeRefusedTheDashboard_UntilAnotherSignsOut()
    {
        await SkipWithoutGlobalAdminCredentials();

        var globalApiClient = await DeployedApiClientProvider.GetGlobalApiClient(TestContext.CancellationToken);
        await using var dbContext = await globalApiClient.DbContextFactory!.CreateDbContextAsync(TestContext.CancellationToken);

        var marker = Guid.NewGuid().ToString("N")[..10];
        var email = $"e2e-{marker}@bitplatform.dev";
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = $"e2e-{marker}", Title = $"E2E {marker}" };

        // Registered before the writes, so a half-made setup is undone too.
        RegisterForCleanup(() => DeleteTenant(tenant.Id));

        await CreateTenantMember(dbContext, tenant, email);

        // ---- Three devices: each session is privileged, each opens the dashboard ----
        var devices = new List<IPage> { Page };
        for (var i = 1; i <= maxPrivilegedSessions; i++)
        {
            devices.Add(await (await NewDevice()).NewPageAsync());
        }

        for (var i = 0; i < maxPrivilegedSessions; i++)
        {
            await SignInOn(devices[i], email);
            await ExpectDashboard(devices[i], accessible: true);
        }

        // ---- The fourth: signed in, but its session is not privileged ----
        var fourth = devices[maxPrivilegedSessions];
        await SignInOn(fourth, email);
        await ExpectDashboard(fourth, accessible: false);

        // NotAuthorizedPage knows why, and says where to go about it.
        await Expect(fourth.GetByRole(AriaRole.Link, new() { Name = AppStrings.TryRemovingOtherSessions })
            .Or(fourth.GetByRole(AriaRole.Button, new() { Name = AppStrings.TryRemovingOtherSessions }))).ToBeVisibleAsync();

        // ---- One of the three signs out, through the app menu ----
        var first = devices[0];
        await ClickAppMenuItem(first, AppStrings.SignOut);
        // SignOutConfirmDialog's OK: the menu's own "Sign out" closed with the menu, and role lookups skip hidden elements.
        await first.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignOut, Exact = true }).ClickAsync();
        await Expect(first.GetByRole(AriaRole.Link, new() { Name = AppStrings.SignIn }).First).ToBeVisibleAsync();

        // ---- The fourth refreshes: its token refresh finds a free slot, so the dashboard opens ----
        await fourth.ReloadAsync();
        await WaitUntilInteractive(fourth);
        await ExpectDashboard(fourth, accessible: true);
    }

    private async Task<IBrowserContext> NewDevice()
    {
        var context = await NewBrowserContext(Browser);
        RegisterForCleanup(async () => await context.DisposeAsync());
        return context;
    }

    private async Task SignInOn(IPage page, string email)
    {
        await page.GotoAsync(DeployedApps.AdminPanel);
        await WaitUntilInteractive(page);
        await SignIn(page, email, password);
    }

    private async Task ExpectDashboard(IPage page, bool accessible)
    {
        await page.GoToInApp(PageUrls.Dashboard);

        await Expect(page).ToHaveTitleAsync(accessible ? AppStrings.DashboardPageTitle : AppStrings.NotAuthorizedPageTitle);
    }

    /// <summary>The tenant, an accepted member of it, and a user-group of that tenant with the dashboard in it.</summary>
    private async Task CreateTenantMember(AppDbContext dbContext, Tenant tenant, string email)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = email.Split('@')[0],
            NormalizedUserName = email.Split('@')[0].ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            CreatedOn = DateTimeOffset.UtcNow
        };
        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, password);

        var roleName = $"e2e-{Guid.NewGuid():N}";
        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = roleName,
            NormalizedName = roleName.ToUpperInvariant(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            TenantId = tenant.Id
        };

        await dbContext.Tenants.AddAsync(tenant, TestContext.CancellationToken);
        await dbContext.Users.AddAsync(user, TestContext.CancellationToken);
        // Accepted, so signing in selects this tenant (See IdentityController.GetTenantId).
        await dbContext.TenantUsers.AddAsync(new TenantUser { Id = Guid.NewGuid(), TenantId = tenant.Id, UserId = user.Id, AcceptedOn = DateTimeOffset.UtcNow }, TestContext.CancellationToken);
        await dbContext.Roles.AddAsync(role, TestContext.CancellationToken);
        // The dashboard is Dashboard_View's; ProductCatalog_Manage is the catalogue access the scenario names.
        await dbContext.RoleClaims.AddRangeAsync([
            new RoleClaim { RoleId = role.Id, ClaimType = AppClaimTypes.FEATURES, ClaimValue = AppFeatures.AdminPanel.Dashboard_View },
            new RoleClaim { RoleId = role.Id, ClaimType = AppClaimTypes.FEATURES, ClaimValue = AppFeatures.AdminPanel.ProductCatalog_Manage }
        ], TestContext.CancellationToken);
        await dbContext.UserRoles.AddAsync(new UserRole { UserId = user.Id, RoleId = role.Id, TenantId = tenant.Id }, TestContext.CancellationToken);

        await dbContext.SaveChangesAsync(TestContext.CancellationToken);
    }

    /// <summary>Not on the test's token: a canceled or timed out test still owes the deployment its cleanup.</summary>
    private static async Task DeleteTenant(Guid tenantId)
    {
        var globalApiClient = await DeployedApiClientProvider.GetGlobalApiClient(CancellationToken.None);
        await using var db = await globalApiClient.DbContextFactory!.CreateDbContextAsync(CancellationToken.None);

        var userIds = await db.TenantUsers.IgnoreQueryFilters().Where(tu => tu.TenantId == tenantId).Select(tu => tu.UserId).ToListAsync(CancellationToken.None);
        var roleIds = await db.Roles.IgnoreQueryFilters().Where(role => role.TenantId == tenantId).Select(role => role.Id).ToListAsync(CancellationToken.None);

        await db.UserRoles.IgnoreQueryFilters().Where(ur => userIds.Contains(ur.UserId) || roleIds.Contains(ur.RoleId)).ExecuteDeleteAsync(CancellationToken.None);
        await db.RoleClaims.IgnoreQueryFilters().Where(rc => roleIds.Contains(rc.RoleId)).ExecuteDeleteAsync(CancellationToken.None);
        await db.Roles.IgnoreQueryFilters().Where(role => role.TenantId == tenantId).ExecuteDeleteAsync(CancellationToken.None);
        await db.UserSessions.IgnoreQueryFilters().Where(session => userIds.Contains(session.UserId)).ExecuteDeleteAsync(CancellationToken.None);
        await db.TenantUsers.IgnoreQueryFilters().Where(tu => tu.TenantId == tenantId).ExecuteDeleteAsync(CancellationToken.None);
        await db.Users.IgnoreQueryFilters().Where(u => userIds.Contains(u.Id)).ExecuteDeleteAsync(CancellationToken.None);
        await db.Tenants.IgnoreQueryFilters().Where(t => t.Id == tenantId).ExecuteDeleteAsync(CancellationToken.None);
    }
}
