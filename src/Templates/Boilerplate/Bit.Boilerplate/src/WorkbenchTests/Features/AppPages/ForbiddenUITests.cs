//+:cnd:noEmit
//#if (module == "Admin")
using Boilerplate.WorkbenchTests.Features.Chrome;

namespace Boilerplate.WorkbenchTests.Features.AppPages;

/// <summary>The not-authorized page as a signed-in user without permission gets it, on a Workbench that refuses the demo user the dashboard, as no demo user is refused anything otherwise.</summary>
[TestClass, TestCategory("UITest")]
public class ForbiddenUITests : AppViewPageTest
{
    private static readonly Lazy<Task<WorkbenchTestServer>> withAgent = new(() => StartServer(isDevMode: true));
    private static readonly Lazy<Task<WorkbenchTestServer>> withoutAgent = new(() => StartServer(isDevMode: false));

    private ILocator Forbidden => View().GetByRole(AriaRole.Heading, new() { Name = AppStrings.ForbiddenException, Exact = true });

    private ILocator Figures => View().GetByText(AppStrings.TotalProducts, new() { Exact = true });

    private protected override Task<WorkbenchTestServer> ServerToTest => HasAgent ? withAgent.Value : withoutAgent.Value;

    private static Task<WorkbenchTestServer> StartServer(bool isDevMode)
    {
        return new WorkbenchTestServer().Start(services => services.AddAuthorizationCore(options =>
        {
            options.AddPolicy(AuthPolicies.PRIVILEGED_ACCESS, policy => policy.RequireAssertion(context => context.User.IsInRole(AppRoles.Demo) is false));
            //#if (multitenant == true)
            options.AddPolicy(AuthPolicies.TENANT_SELECTED, policy => policy.RequireAssertion(context => context.User.IsInRole(AppRoles.Demo) is false));
            //#endif
        }), isDevMode);
    }

    [ClassCleanup]
    public static async Task StopServers()
    {
        foreach (var server in new[] { withAgent, withoutAgent }.Where(server => server.IsValueCreated))
        {
            await (await server.Value).DisposeAsync();
        }
    }

    [TestMethod, EverySurface]
    public async Task AUserWithoutPermission_Should_SeeWhoTheyAreAndNotThePage(WorkbenchSurface surface)
    {
        await OpenWorkbench("dashboard", "?user=2");

        await Expect(Forbidden).ToBeVisibleAsync();
        await Expect(View().GetByText(AppStrings.YouAreSignedInAs)).ToContainTextAsync(WorkbenchDemoUsers.All[2].Email);
        await Expect(Figures).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task SignInAsDifferentUser_Should_MoveToTheNextUserAndOpenThePage(WorkbenchSurface surface)
    {
        await OpenWorkbench("dashboard", "?user=2");

        await Press(View().GetByRole(AriaRole.Button, new() { Name = AppStrings.SignInAsDifferentUser, Exact = true }));

        await Expect(AccountButton(WorkbenchStrings.DemoUserYas)).ToBeVisibleAsync();
        await Expect(Page).Not.ToHaveURLAsync(new Regex("user="));
        await Expect(Figures).ToBeVisibleAsync();
        await Expect(Forbidden).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task AMissingPrivilege_Should_OfferRemovingOtherSessions(WorkbenchSurface surface)
    {
        await OpenWorkbench("dashboard", "?user=2");

        await Expect(Forbidden).ToBeVisibleAsync();
        await Expect(View().GetByRole(AriaRole.Button, new() { Name = AppStrings.TryRemovingOtherSessions, Exact = true })).ToBeVisibleAsync();
    }

    //#if (multitenant == true)
    [TestMethod, EverySurface]
    public async Task AMissingTenant_Should_OfferManagingTenants(WorkbenchSurface surface)
    {
        await OpenWorkbench("dashboard", "?user=2");

        await Expect(View().GetByText(AppStrings.SelectTenantMessage)).ToBeVisibleAsync();
        await Expect(View().GetByRole(AriaRole.Button, new() { Name = AppStrings.ManageMyTenants, Exact = true })).ToBeVisibleAsync();
    }
    //#endif

    [TestMethod, EverySurface]
    public async Task AUserWithPermission_Should_NotSeeIt(WorkbenchSurface surface)
    {
        await OpenWorkbench("dashboard", "?user=1");

        await Expect(Figures).ToBeVisibleAsync();
        await Expect(Forbidden).ToHaveCountAsync(0);
    }
}
//#endif
