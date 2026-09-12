using Boilerplate.Server.Api.Features.Tenants;
using Boilerplate.Tests.E2E.Infrastructure.Services;

namespace Boilerplate.Tests.E2E.Features.ErrorPages;

/// <summary>
/// NotFoundPage and NotAuthorizedPage as a visitor meets them, across the ways a deployment can reach them: prerendered
/// (Todo, AdminPanel, Sales - the server's UseStatusCodePages redirects), not prerendered (the WebAssembly standalone
/// apps - the client router alone), Blazor's Router (Todo, Sales, the standalone apps) and bit Brouter (AdminPanel).
/// </summary>
[TestClass, TestCategory(TestCategories.Web), Retry(2)]
public partial class WebErrorPageTests : AppTestBase
{
    /// <summary>Several segments: no route template matches it, so a prerendering server answers 404 itself.</summary>
    private const string unknownDeepPath = "/e2e/route/that-does-not-exist";

    /// <summary>
    /// One segment: a client router captures it as HomePage's <c>{culture}</c> and AppPageBase turns the unknown culture
    /// into NotFoundPage; a prerendering server's culture redirect makes it <c>/en-US/...</c> first, a deep path again.
    /// </summary>
    private const string unknownShallowPath = "/e2e-route-that-does-not-exist";

    protected override IAppOpener AppOpener => new WebAppOpener();

    [TestMethod]
    [DataRow(App.Todo, unknownDeepPath, DisplayName = "Todo (prerendered, Blazor Router), deep path")]
    [DataRow(App.Todo, unknownShallowPath, DisplayName = "Todo (prerendered, Blazor Router), one segment")]
    [DataRow(App.AdminPanel, unknownDeepPath, DisplayName = "AdminPanel (prerendered, bit Brouter), deep path")]
    [DataRow(App.AdminPanel, unknownShallowPath, DisplayName = "AdminPanel (prerendered, bit Brouter), one segment")]
    [DataRow(App.Sales, unknownDeepPath, DisplayName = "Sales (prerendered, integrated API), deep path")]
    [DataRow(App.Sales, unknownShallowPath, DisplayName = "Sales (prerendered, integrated API), one segment")]
    [DataRow(App.AdminPanelWasmStandalone, unknownDeepPath, DisplayName = "AdminPanelWasmStandalone (not prerendered), deep path")]
    [DataRow(App.AdminPanelWasmStandalone, unknownShallowPath, DisplayName = "AdminPanelWasmStandalone (not prerendered), one segment")]
    [DataRow(App.TodoAot, unknownDeepPath, DisplayName = "TodoAot (not prerendered), deep path")]
    [DataRow(App.TodoAot, unknownShallowPath, DisplayName = "TodoAot (not prerendered), one segment")]
    public async Task UnknownRoute_Should_ShowTheNotFoundPage(App app, string path)
    {
        // A first visit, like following a broken link - not OpenApp, whose home page would install bswup's service
        // worker, which then answers the unknown url from the client and hides what the server does.
        await Page.GotoAsync(AddressIn(app, path));
        var page = Page;

        // The page's own text, not just a status code: a 404 with an empty body is exactly the failure this is after.
        await Expect(page.GetByText(AppStrings.NotFoundText, new() { Exact = true }))
            .ToBeVisibleAsync(new() { Timeout = (float)TimeSpan.FromMinutes(2).TotalMilliseconds });

        await WaitUntilInteractive(page);
    }

    [TestMethod]
    [DataRow(App.Todo, DisplayName = "Todo (prerendered, Blazor Router)")]
    [DataRow(App.AdminPanel, DisplayName = "AdminPanel (prerendered, bit Brouter)")]
    [DataRow(App.Sales, DisplayName = "Sales (prerendered, integrated API)")]
    [DataRow(App.AdminPanelWasmStandalone, DisplayName = "AdminPanelWasmStandalone (not prerendered)")]
    [DataRow(App.TodoAot, DisplayName = "TodoAot (not prerendered)")]
    public async Task SignedInOnlyPage_Should_ShowTheNotAuthorizedPage_ToASignedOutVisitor(App app)
    {
        var page = await OpenApp(app);

        await page.GotoAsync(AddressIn(app, PageUrls.Settings));

        // NotAuthorizedPage renders a loader until its first interactive render has tried a token refresh, so only a
        // booted app shows this.
        await Expect(page.GetByText(AppStrings.YouAreNotAuthorized, new() { Exact = true }))
            .ToBeVisibleAsync(new() { Timeout = (float)TimeSpan.FromMinutes(2).TotalMilliseconds });

        // Not exact: the button's accessible name carries its icon glyph too. The header has sign in links of its own.
        await Expect(page.Locator("main section").GetByRole(AriaRole.Button, new() { Name = AppStrings.SignIn })).ToBeVisibleAsync();
    }

    /// <summary>
    /// A signed-in member without Users_Manage is turned away from the users page; once a global admin grants it, the
    /// same session opens the page on its next visit. NotAuthorizedPage refreshes the token before deciding, and a
    /// refresh re-reads the user's roles, so no sign-out / sign-in is needed.
    /// <para>
    /// The grant is a user-group of its own, written through the global admin's database access and deleted at
    /// cleanup: store-user's demo user-group is shared by every deployment, and the role api would need the shared
    /// global admin session to switch tenant and elevate first.
    /// </para>
    /// </summary>
    [TestMethod]
    [DataRow(App.Todo, DisplayName = "Todo (Blazor Router, not multitenant)")]
    [DataRow(App.AdminPanel, DisplayName = "AdminPanel (bit Brouter)")]
    [DataRow(App.Sales, DisplayName = "Sales (Blazor Router, integrated API)")]
    public async Task ForbiddenPage_Should_Open_OnceTheGlobalAdminGrantsAccess_WithoutSigningInAgain(App app)
    {
        await SkipWithoutGlobalAdminCredentials();

        var page = await OpenApp(app);
        await WaitUntilInteractive(page);

        await SignIn(page, StoreUser.Email, StoreUser.Password);
        var sessionId = await GetSessionId(page);

        await page.GoToInApp(PageUrls.Users);

        // The Authorized half of NotAuthorizedPage: signed in, just not allowed.
        await Expect(page.GetByText(AppStrings.ForbiddenException, new() { Exact = true })).ToBeVisibleAsync();
        await Expect(page.GetByPlaceholder(AppStrings.SearchUsersPlaceholder)).ToHaveCountAsync(0);

        await GrantUsersManageToStoreUser();

        await page.GoToInApp(PageUrls.Home);
        await page.GoToInApp(PageUrls.Users);

        await Expect(page.GetByPlaceholder(AppStrings.SearchUsersPlaceholder)).ToBeVisibleAsync();
        await Expect(page.GetByText(AppStrings.ForbiddenException, new() { Exact = true })).ToHaveCountAsync(0);

        Assert.AreEqual(sessionId, await GetSessionId(page), "The page opened on a new session, so it took a sign in rather than a token refresh.");
    }

    private static string AddressIn(App app, string path) => new Uri(new Uri(DeployedApps.AddressOf(app)), path).ToString();

    /// <summary>
    /// Users_Manage, plus unlimited privileged sessions: the users page also needs PRIVILEGED_ACCESS, which store-user
    /// - three privileged sessions at most, and signed in by other tests too - cannot count on otherwise.
    /// </summary>
    private async Task GrantUsersManageToStoreUser()
    {
        var globalApiClient = await DeployedApiClientProvider.GetGlobalApiClient(TestContext.CancellationToken);
        await using var dbContext = await globalApiClient.DbContextFactory!.CreateDbContextAsync(TestContext.CancellationToken);

        var storeUserId = await dbContext.Users.IgnoreQueryFilters()
            .Where(user => user.NormalizedEmail == StoreUser.Email.ToUpperInvariant())
            .Select(user => user.Id)
            .SingleAsync(TestContext.CancellationToken);

        var roleName = $"e2e-{Guid.NewGuid():N}";
        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = roleName,
            NormalizedName = roleName.ToUpperInvariant(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            // store-user's tenant, which AdminPanel and Sales scope her roles to; Todo is not multitenant and reads all.
            TenantId = TenantConfiguration.FallbackTenantId
        };

        // Registered before the writes, so a half-done grant is undone too.
        RegisterForCleanup(() => DeleteRole(role.Id));

        await dbContext.Roles.AddAsync(role, TestContext.CancellationToken);
        await dbContext.RoleClaims.AddRangeAsync([
            new RoleClaim { RoleId = role.Id, ClaimType = AppClaimTypes.FEATURES, ClaimValue = AppFeatures.Management.Users_Manage },
            new RoleClaim { RoleId = role.Id, ClaimType = AppClaimTypes.MAX_PRIVILEGED_SESSIONS, ClaimValue = AppClaimTypes.UNLIMITED_PRIVILEGED_SESSIONS.ToString(CultureInfo.InvariantCulture) }
        ], TestContext.CancellationToken);
        await dbContext.UserRoles.AddAsync(new UserRole { UserId = storeUserId, RoleId = role.Id, TenantId = role.TenantId }, TestContext.CancellationToken);

        await dbContext.SaveChangesAsync(TestContext.CancellationToken);
    }

    /// <summary>Not on the test's token: a canceled or timed out test still owes the deployment its cleanup.</summary>
    private static async Task DeleteRole(Guid roleId)
    {
        var globalApiClient = await DeployedApiClientProvider.GetGlobalApiClient(CancellationToken.None);
        await using var dbContext = await globalApiClient.DbContextFactory!.CreateDbContextAsync(CancellationToken.None);

        await dbContext.UserRoles.IgnoreQueryFilters().Where(userRole => userRole.RoleId == roleId).ExecuteDeleteAsync(CancellationToken.None);
        await dbContext.RoleClaims.IgnoreQueryFilters().Where(roleClaim => roleClaim.RoleId == roleId).ExecuteDeleteAsync(CancellationToken.None);
        await dbContext.Roles.IgnoreQueryFilters().Where(role => role.Id == roleId).ExecuteDeleteAsync(CancellationToken.None);
    }
}
