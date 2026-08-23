//+:cnd:noEmit

namespace Boilerplate.Client.Core.Components.Layout.Header;

public partial class AppMenu
{
    [CascadingParameter] public BitDir? CurrentDir { get; set; }

    [CascadingParameter] public UserDto? CurrentUser { get; set; }

    [CascadingParameter] public AppThemeType? CurrentTheme { get; set; }


    [AutoInject] private ThemeService themeService = default!;
    [AutoInject] private CultureService cultureService = default!;
    [AutoInject] private IUserController userController = default!;
    [AutoInject] private SignInModalService signInModalService = default!;
    //#if (brouter == true && multitenant == true)
    [AutoInject] private IBrouter brouter = default!;
    //#endif


    private bool isOpen;
    private bool showCultures;
    private bool isSignOutConfirmOpen;
    private BitChoiceGroupItem<string>[] cultures = default!;
    //#if (multitenant == true)
    private bool showTenants;
    private string? currentTenantId;
    private BitChoiceGroupItem<string>[] tenants = [];
    private HashSet<string> pendingInvitationTenantIds = [];
    //#endif

    private bool ShowMainMenu =>
        showCultures is false
        //#if (multitenant == true)
        && showTenants is false
        //#endif
        ;


    private string? ProfileImageUrl => CurrentUser?.GetProfileImageUrl(AbsoluteServerAddress);

    private string AccountMenuLabel => CurrentUser?.DisplayName ?? Localizer[nameof(AppStrings.AccountMenuTitle)].Value;

    private string ThemeToggleLabel => CurrentTheme == AppThemeType.Light
        ? Localizer[nameof(AppStrings.SwitchToDarkThemeTitle)]
        : Localizer[nameof(AppStrings.SwitchToLightThemeTitle)];


    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();

        NavigationManager.LocationChanged += NavigationManager_LocationChanged;
        //#if (multitenant == true)
        AuthManager.AuthenticationStateChanged += AuthManager_AuthenticationStateChanged;
        //#endif

        if (CultureInfoManager.InvariantGlobalization is false)
        {
            cultures = CultureInfoManager.SupportedCultures
                              .Select(sc => new BitChoiceGroupItem<string> { Value = sc.Culture.Name, Text = sc.DisplayName })
                              .ToArray();
        }
    }

    private void NavigationManager_LocationChanged(object? sender, LocationChangedEventArgs e)
    {
        // The sign-in and sign-up buttons href are bound to NavigationManager.GetRelativePath().
        // To ensure the bound values update with each route change, it's necessary to call StateHasChanged on location changes.
        StateHasChanged();
    }

    private async Task OnCultureChanged(string? cultureName)
    {
        await cultureService.ChangeCulture(cultureName);
    }

    //#if (multitenant == true)
    private async Task ShowTenants()
    {
        showTenants = true;

        var user = (await AuthenticationStateTask).User;
        currentTenantId = user.GetTenantId()?.ToString();

        // GetTenants also returns tenants the user has only been INVITED to, and switching into one of those is what
        // accepts the invitation (See IdentityController.Refresh) - so they have to be told apart in the list rather
        // than offered as if they were memberships. A pending invitation is only relevant for regular users; a global
        // admin is listed every active tenant and always just switches (See ManageMyTenantsPage.IsPendingInvitation).
        var isGlobalAdmin = await AuthorizationService.IsAuthorized(user, AppFeatures.Management.Tenants_Manage_Global);

        var userTenants = await userController.GetTenants(CurrentCancellationToken);

        pendingInvitationTenantIds = isGlobalAdmin
            ? []
            : [.. userTenants.Where(t => t.CurrentUserHasAcceptedThisTenantInvitation is false).Select(t => t.Id.ToString())];

        tenants = [.. userTenants.Select(t => new BitChoiceGroupItem<string> { Value = t.Id.ToString(), Text = t.Title ?? t.Name })];
    }

    private bool IsPendingInvitation(string? tenantId) => tenantId is not null && pendingInvitationTenantIds.Contains(tenantId);

    private async Task OnTenantChanged(string? tenantId)
    {
        if (Guid.TryParse(tenantId, out var newTenantId) is false || tenantId == currentTenantId)
            return;

        CloseMenu();

        // Switching calls the refresh token api that stores the new tenant id in the token's claims (See IdentityController.Refresh).
        if (await AuthManager.SwitchTenant(newTenantId, CurrentCancellationToken))
        {
            //#if (brouter == true)
            brouter.ClearKeepAlive();
            //#endif

            NavigationManager.RefreshCurrentPage(); // Re-renders the current page so it reflects the new tenant's data.
            // The layout's tenant display (next to the app version) updates on its own: switching changes the tenant claim, which
            // triggers the authentication-state change that MainLayout re-resolves the current tenant from (See MainLayout.SetCurrentTenantIfNeeded).
        }
    }
    //#endif

    private async Task ToggleTheme()
    {
        await themeService.ToggleTheme();
    }

    private async Task GoToProfile()
    {
        CloseMenu();
        NavigationManager.NavigateTo($"{PageUrls.Settings}/{PageUrls.SettingsSections.Profile}");
    }

    /// <summary>
    /// Closes the menu and resets its sub panels, so it reopens on the main menu rather than on whichever
    /// sub panel was open when it was closed. BitDropMenu only raises OnDismiss when it closes itself
    /// (a click on the overlay or on the trigger); assigning the bound IsOpen from code does not.
    /// </summary>
    private void CloseMenu()
    {
        isOpen = false;
        OnDropMenuDismiss();
    }

    private void OnDropMenuDismiss()
    {
        showCultures = false;
        //#if (multitenant == true)
        showTenants = false;
        //#endif
    }

    //#if (multitenant == true)
    private void AuthManager_AuthenticationStateChanged(Task<AuthenticationState> task)
    {
        _ = InvokeAsync(() =>
        {
            showTenants = false; // This would help refreshing the list of tenants, so they would get loaded again the next time user opens the tenant menu.
            StateHasChanged();
        });
    }
    //#endif


    protected override async ValueTask DisposeAsync(bool disposing)
    {
        await base.DisposeAsync(disposing);

        NavigationManager.LocationChanged -= NavigationManager_LocationChanged;
        //#if (multitenant == true)
        AuthManager.AuthenticationStateChanged -= AuthManager_AuthenticationStateChanged;
        //#endif
    }

    private async Task ModalSignIn()
    {
        CloseMenu();
        await signInModalService.SignIn();
    }
}
