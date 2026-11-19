//+:cnd:noEmit
using Bunit;
using Boilerplate.Workbench.Components.Preview;
using Boilerplate.Workbench.Components.Pages.AppPages;
using Microsoft.AspNetCore.Components.Authorization;

namespace Boilerplate.WorkbenchTests.Features.AppPages;

/// <summary>The not-authorized page's own entry: each case its switch picks, signed in or not.</summary>
[TestClass, TestCategory("UITest")]
public class NotAuthorizedCasesTests
{
    private static readonly TimeSpan timeout = TimeSpan.FromSeconds(10);

    private static readonly WorkbenchAuthState[] signIns = [WorkbenchAuthState.SignedIn, WorkbenchAuthState.SignedOut];

    //#if (multitenant == true)
    [TestMethod]
    public async Task NoTenant_Should_ListTheTenantsAndTheInvitation()
    {
        foreach (var auth in signIns)
        {
            var text = await Render(NotAuthorizedPage.NoTenantCase, new() { Auth = auth });

            Assert.Contains(AppStrings.SelectTenantMessage, text);
            Assert.Contains(AppStrings.PendingInvitations, text);
            Assert.Contains(WorkbenchStrings.DemoTenantHeadOffice, text);
            Assert.Contains(AppStrings.AcceptInvitation, text);
            Assert.Contains(AppStrings.SwitchTenant, text);
            Assert.Contains(WorkbenchStrings.DemoTenantStore, text);
        }
    }

    [TestMethod]
    public async Task NoTenants_Should_ShowTheMessageAlone()
    {
        foreach (var auth in signIns)
        {
            var text = await Render(NotAuthorizedPage.NoTenantsCase, new() { Auth = auth });

            Assert.Contains(AppStrings.SelectTenantMessage, text);
            Assert.Contains(AppStrings.ManageMyTenants, text);
            Assert.DoesNotContain(AppStrings.PendingInvitations, text);
            Assert.DoesNotContain(AppStrings.SwitchTenant, text);
        }
    }
    //#endif

    [TestMethod]
    public async Task Privilege_Should_OfferRemovingOtherSessions()
    {
        foreach (var auth in signIns)
        {
            var text = await Render(NotAuthorizedPage.PrivilegeCase, new() { Auth = auth });

            Assert.Contains(AppStrings.TryRemovingOtherSessions, text);
            //#if (multitenant == true)
            Assert.DoesNotContain(AppStrings.SelectTenantMessage, text);
            //#endif
        }
    }

    [TestMethod]
    public async Task Forbidden_Should_SayWhoIsSignedInAndNothingElse()
    {
        foreach (var auth in signIns)
        {
            var text = await Render(NotAuthorizedPage.ForbiddenCase, new() { Auth = auth });

            Assert.Contains(AppStrings.ForbiddenException, text);
            Assert.Contains(WorkbenchDemoUsers.All[0].Email, text);
            Assert.DoesNotContain(AppStrings.TryRemovingOtherSessions, text);
            //#if (multitenant == true)
            Assert.DoesNotContain(AppStrings.SelectTenantMessage, text);
            //#endif
        }
    }

    [TestMethod]
    public async Task SignedOut_Should_ShowThe401EvenToASignedInView()
    {
        foreach (var auth in signIns)
        {
            var text = await Render(NotAuthorizedPage.SignedOutCase, new() { Auth = auth });

            Assert.Contains(AppStrings.YouAreNotAuthorized, text);
            Assert.DoesNotContain(AppStrings.ForbiddenException, text);
        }
    }

    [TestMethod]
    public async Task Loading_Should_ShowTheAppsLoadingIndicator()
    {
        await using var ctx = WorkbenchBunit.Create();

        var cut = RenderView(ctx, new WorkbenchScenario { Data = WorkbenchDataState.Loading }.WithSwitch(NotAuthorizedHost.CaseSwitch, NotAuthorizedPage.ForbiddenCase));

        cut.WaitForAssertion(() => Assert.IsNotEmpty(cut.FindAll(".bit-lds-grid")), timeout);
        Assert.DoesNotContain(AppStrings.ForbiddenException, cut.Markup);
    }

    private static async Task<string> Render(string pageCase, WorkbenchScenario scenario)
    {
        await using var ctx = WorkbenchBunit.Create();

        var cut = RenderView(ctx, scenario.WithSwitch(NotAuthorizedHost.CaseSwitch, pageCase));

        var text = "";
        cut.WaitForAssertion(() => Assert.IsNotEmpty(text = string.Concat(cut.Nodes.Select(node => node.TextContent)).Trim()), timeout);

        return text;
    }

    private static IRenderedComponent<BlexProvider> RenderView(BunitContext ctx, WorkbenchScenario scenario)
    {
        ctx.Services.GetRequiredService<WorkbenchScenarioState>().Set(scenario);

        return ctx.Render<BlexProvider>(p => p.AddChildContent<CascadingAuthenticationState>(auth => auth
            .AddChildContent<PreviewPageView>(view => view.Add(x => x.Page, WorkbenchPageCatalog.Find(NotAuthorizedHost.PageKey)!))));
    }
}
