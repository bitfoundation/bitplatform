//+:cnd:noEmit
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Boilerplate.Workbench.Components.Pages.AppPages;

/// <summary>The Workbench copy of the app's not-authorized page; its sign-in buttons change the view's signed-in state.</summary>
public partial class NotAuthorizedPage
{
    private bool lacksValidPrivilege;
    //#if (multitenant == true)
    private bool lacksSelectedTenant;
    //#endif

    [Parameter] public Type? PageType { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        var user = (await AuthenticationStateTask).User;

        var failedPolicies = await GetFailedPolicies(user);

        lacksValidPrivilege = failedPolicies.Contains(AuthPolicies.PRIVILEGED_ACCESS);
        //#if (multitenant == true)
        lacksSelectedTenant = failedPolicies.Contains(AuthPolicies.TENANT_SELECTED);
        //#endif
    }

    private async Task<List<string>> GetFailedPolicies(ClaimsPrincipal user)
    {
        List<string> failedPolicies = [];

        if (PageType is null) return failedPolicies;

        var policies = PageType.GetCustomAttributes(inherit: true)
            .OfType<IAuthorizeData>()
            .Select(authorizeData => authorizeData.Policy)
            .OfType<string>()
            .Distinct();

        foreach (var policy in policies)
        {
            if (await AuthorizationService.IsAuthorized(user, policy) is false)
            {
                failedPolicies.Add(policy);
            }
        }

        return failedPolicies;
    }

    /// <summary>Signs the view in as the first demo user.</summary>
    private Task SignIn() => ReportScenario(Scenario with { Auth = WorkbenchAuthState.SignedIn, User = 0 });

    /// <summary>Signs the view in as the next demo user, the way picking another account in the app's sign-in would.</summary>
    private Task SignInAsDifferentUser() => ReportScenario(Scenario with { User = (Scenario.User + 1) % WorkbenchDemoUsers.All.Count });
}
