//+:cnd:noEmit
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
//#if (multitenant == true)
using Boilerplate.Shared.Features.Tenants.Dtos;
//#endif

namespace Boilerplate.Workbench.Components.Pages.AppPages;

/// <summary>The Workbench copy of the app's not-authorized page; its sign-in button explains why no sign-in page opens here.</summary>
public partial class NotAuthorizedPage
{
    private bool lacksValidPrivilege;
    //#if (multitenant == true)
    private bool isGlobalAdmin;
    private bool lacksSelectedTenant;
    private List<TenantDto> tenants = [];
    //#endif

    [Parameter] public Type? PageType { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        var user = (await AuthenticationStateTask).User;

        var failedPolicies = await GetFailedPolicies(user);

        lacksValidPrivilege = failedPolicies.Contains(AuthPolicies.PRIVILEGED_ACCESS);
        //#if (multitenant == true)
        lacksSelectedTenant = failedPolicies.Contains(AuthPolicies.TENANT_SELECTED);

        if (lacksSelectedTenant)
        {
            isGlobalAdmin = await AuthorizationService.IsAuthorized(user, AppFeatures.Management.Tenants_Manage_Global);
            tenants = GetSampleTenants();
        }
        //#endif
    }

    //#if (multitenant == true)
    /// <summary>
    /// The sample tenants in place of what the app's UserController.GetTenants returns: the view's user belongs to the first
    /// and has been invited to the others.
    /// </summary>
    private List<TenantDto> GetSampleTenants()
    {
        return [.. WorkbenchDemoUsers.TenantKeys.Select((key, index) => new TenantDto
        {
            Id = new(index + 1, 0, 0, new byte[8]),
            Name = key.ToLowerInvariant(),
            Title = Localizer[key],
            CurrentUserMembershipStatus = index is 0 ? TenantMembershipStatus.Member : TenantMembershipStatus.Invited
        })];
    }
    //#endif

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

    /// <summary>Signs the view in as the next demo user, the way picking another account in the app's sign-in would.</summary>
    private Task SignInAsDifferentUser() => ReportScenario(Scenario with { User = (Scenario.User + 1) % WorkbenchDemoUsers.All.Count }, isApplied: false);
}
