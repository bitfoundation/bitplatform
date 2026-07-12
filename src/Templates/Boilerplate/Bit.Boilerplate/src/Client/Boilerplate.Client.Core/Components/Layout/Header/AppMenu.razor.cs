//+:cnd:noEmit
using Boilerplate.Shared.Features.Identity;
using Boilerplate.Shared.Features.Identity.Dtos;
using Microsoft.AspNetCore.Components.Routing;

namespace Boilerplate.Client.Core.Components.Layout.Header;

public partial class AppMenu
{
    [CascadingParameter] public BitDir? CurrentDir { get; set; }

    [CascadingParameter] public UserDto? CurrentUser { get; set; }

    [CascadingParameter] public AppThemeType? CurrentTheme { get; set; }


    [AutoInject] private Cookie cookie = default!;
    [AutoInject] private ThemeService themeService = default!;
    [AutoInject] private CultureService cultureService = default!;
    [AutoInject] private IUserController userController = default!;
    [AutoInject] private CultureInfoManager cultureInfoManager = default!;
    [AutoInject] private SignInModalService signInModalService = default!;


    private bool isOpen;
    private bool showCultures;
    private bool isSignOutConfirmOpen;
    private BitChoiceGroupItem<string>[] cultures = default!;
    //#if (multitenant == true)
    private bool showTenants;
    private string? currentTenantId;
    private BitChoiceGroupItem<string>[] tenants = [];
    //#endif

    private bool ShowMainMenu =>
        showCultures is false
        //#if (multitenant == true)
        && showTenants is false
        //#endif
        ;


    private string? ProfileImageUrl => CurrentUser?.GetProfileImageUrl(AbsoluteServerAddress);


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

        tenants = [.. (await userController.GetTenants(CurrentCancellationToken))
                        .Select(t => new BitChoiceGroupItem<string> { Value = t.Id.ToString(), Text = t.Title ?? t.Name })];
    }

    private async Task OnTenantChanged(string? tenantId)
    {
        if (Guid.TryParse(tenantId, out var newTenantId) is false || tenantId == currentTenantId)
            return;

        isOpen = false;

        // Switching calls the refresh token api that stores the new tenant id in the token's claims (See IdentityController.Refresh).
        if (await AuthManager.SwitchTenant(newTenantId, CurrentCancellationToken))
        {
            NavigationManager.RefreshCurrentPage(); // Re-renders the current page so it reflects the new tenant's data.
            // If you've changed the layout to show tenant information somewwhere, you may need to use PubSub to notify the layout to re-render.
        }
    }
    //#endif

    private async Task ToggleTheme()
    {
        await themeService.ToggleTheme();
    }

    private async Task GoToProfile()
    {
        NavigationManager.NavigateTo(PageUrls.Settings);
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
        showTenants = false; // This would help refreshing the list of tenants, so they would get loaded again the next time user opens the tenant menu.
        StateHasChanged();
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
        isOpen = false;
        await signInModalService.SignIn();
    }
}
