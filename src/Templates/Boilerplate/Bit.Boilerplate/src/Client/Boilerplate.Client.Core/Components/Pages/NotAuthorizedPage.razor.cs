//+:cnd:noEmit
//#if (multitenant == true)
using Boilerplate.Shared.Features.Tenants.Dtos;
//#endif

namespace Boilerplate.Client.Core.Components.Pages;

public partial class NotAuthorizedPage
{
    private bool lacksValidPrivilege;
    //#if (multitenant == true)
    private bool isGlobalAdmin;
    private bool lacksSelectedTenant;
    private List<TenantDto> tenants = [];
    //#endif
    private bool isUpdatingAuthState = true;


    [SupplyParameterFromQuery(Name = "return-url"), Parameter]
    public string? ReturnUrl { get; set; }

    [SupplyParameterFromQuery(Name = "try_refreshing_token"), Parameter]
    public bool? TryRefreshingToken { get; set; }

    [Parameter] public Type? PageType { get; set; }

    private string GetSafeReturnUrl() => Uri.IsAppRelativeUrl(ReturnUrl, requireLeadingSlash: false) ? ReturnUrl : PageUrls.Home;

    [AutoInject] private SignInModalService signInModalService = default!;
    //#if (multitenant == true)
    [AutoInject] private IUserController userController = default!;
    //#endif


    protected override async Task OnAfterFirstRenderAsync()
    {
        await base.OnAfterFirstRenderAsync();

        try
        {
            var refreshToken = await StorageService.GetItem("refresh_token");

            // Let's update the access token by refreshing it when a refresh token is available.
            // Following this procedure, the newly acquired access token may now include the necessary roles or claims.
            // TryRefreshingToken is checked FIRST: arriving here having already been refreshed once for this
            // destination means the refresh cannot help, so a second round trip is pure cost.
            if (TryRefreshingToken is not false && string.IsNullOrWhiteSpace(refreshToken) is false)
            {
                var accessToken = await AuthManager.RefreshToken(requestedBy: nameof(NotAuthorizedPage));
                if (string.IsNullOrWhiteSpace(accessToken) is false && ReturnUrl is not null)
                {
                    var returnUrl = GetSafeReturnUrl();
                    var @char = returnUrl.Contains('?') ? '&' : '?'; // The RedirectUrl may already include a query string.
                    NavigationManager.NavigateTo($"{returnUrl}{@char}try_refreshing_token=false", replace: true);
                }
            }

            var user = (await AuthenticationStateTask).User;

            var failedPolicies = await GetFailedPolicies(user);

            lacksValidPrivilege = failedPolicies.Contains(AuthPolicies.PRIVILEGED_ACCESS);
            //#if (multitenant == true)
            lacksSelectedTenant = failedPolicies.Contains(AuthPolicies.TENANT_SELECTED);

            if (lacksSelectedTenant)
            {
                await LoadTenants(user);
            }
            //#endif
        }
        finally
        {
            isUpdatingAuthState = false;
            StateHasChanged();
        }
    }

    //#if (multitenant == true)
    private async Task LoadTenants(ClaimsPrincipal user)
    {
        isGlobalAdmin = await AuthorizationService.IsAuthorized(user, AppFeatures.Management.Tenants_Manage_Global);
        tenants = await userController.GetTenants(CurrentCancellationToken);
    }

    private async Task SwitchTenant(TenantDto tenant)
    {
        if (await AuthManager.SwitchTenant(tenant.Id, CurrentCancellationToken))
        {
            // As in AppMenu: the refused page renders again, now with a tenant.
            PubSubService.Publish(ClientAppMessages.SOFT_RESTART);
        }
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

    private async Task SignIn()
    {
        var returnUrl = ReturnUrl is null ? NavigationManager.GetRelativePath() : GetSafeReturnUrl();

        if (await signInModalService.SignIn(returnUrl) && ReturnUrl is not null)
        {
            NavigationManager.NavigateTo(returnUrl, replace: true);
        }

        // Alternatively, you can redirect the user to the sign-in page.
        // NavigationManager.NavigateTo($"{PageUrls.SignIn}?return-url={Uri.EscapeDataString(returnUrl)}");
    }
}
