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
    //#if (notification == true)
    [AutoInject] private IPushNotificationService pushNotificationService = default!;
    //#endif
    //#if (brouter == true)
    [AutoInject] private IBrouter brouter = default!;
    //#endif


    private bool isOpen;
    private bool showCultures;
    private bool isSignOutConfirmOpen;
    private BitChoiceGroupItem<string>[] cultures = default!;
    private bool showTimeZones;
    private string? currentTimeZoneId;
    private string? timeZoneSearchText;
    private BitChoiceGroupItem<string>[] timeZones = [];
    //#if (notification == true)
    private bool pushNotificationsEnabled = true;
    //#endif
    //#if (multitenant == true)
    private bool showTenants;
    private string? currentTenantId;
    private BitChoiceGroupItem<string>[] tenants = [];
    private HashSet<string> pendingInvitationTenantIds = [];
    //#endif

    private bool ShowMainMenu =>
        showCultures is false
        && showTimeZones is false
        //#if (multitenant == true)
        && showTenants is false
        //#endif
        ;

    private BitChoiceGroupItem<string>[] FilteredTimeZones =>
        string.IsNullOrWhiteSpace(timeZoneSearchText)
            ? timeZones
            : [.. timeZones.Where(tz => tz.Text!.Contains(timeZoneSearchText, StringComparison.OrdinalIgnoreCase))];


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

    //#if (notification == true)
    protected override async Task OnAfterFirstRenderAsync()
    {
        await base.OnAfterFirstRenderAsync();

        pushNotificationsEnabled = await pushNotificationService.IsEnabled();
        StateHasChanged();
    }

    private string PushNotificationsToggleLabel => pushNotificationsEnabled
        ? Localizer[nameof(AppStrings.TurnPushNotificationsOff)].Value
        : Localizer[nameof(AppStrings.TurnPushNotificationsOn)].Value;

    private async Task TogglePushNotifications()
    {
        var enable = pushNotificationsEnabled is false;

        if (enable)
        {
            await pushNotificationService.RequestPermission(CurrentCancellationToken);

            if (await pushNotificationService.IsAvailable(CurrentCancellationToken) is false)
            {
                SnackBarService.Error(Localizer[nameof(AppStrings.PushNotificationsBlockedMessage)]);
                return; // The switch stays off; reporting success for a device that can never receive a push would be a lie.
            }
        }

        pushNotificationsEnabled = enable;
        await pushNotificationService.SetEnabled(enable, CurrentCancellationToken);
    }
    //#endif

    private async Task ShowTimeZones()
    {
        showTimeZones = true;
        timeZoneSearchText = null;
        currentTimeZoneId = (await TimeZoneService.GetCurrentTimeZone()).Id;

        // Rebuilt on every open rather than cached, because the current zone leads the list and changes with it.
        timeZones = [.. TimeZoneInfo.GetSystemTimeZones()
            .OrderByDescending(tz => string.Equals(tz.Id, currentTimeZoneId, StringComparison.OrdinalIgnoreCase))
            .ThenBy(tz => tz.BaseUtcOffset)
            .ThenBy(tz => tz.Id, StringComparer.OrdinalIgnoreCase)
            .Select(tz => new BitChoiceGroupItem<string> { Value = tz.Id, Text = GetTimeZoneDisplayText(tz) })];
    }

    private static string GetTimeZoneDisplayText(TimeZoneInfo timeZone)
    {
        // Windows display names already carry the "(UTC+01:00) ..." prefix; elsewhere (browser, Android, iOS) the
        // display name is the bare IANA id, so the offset is stitched on to keep the list searchable both by place
        // name and by offset.
        if (timeZone.DisplayName.StartsWith('(')) return timeZone.DisplayName;

        var offset = timeZone.BaseUtcOffset;
        return $"(UTC{(offset < TimeSpan.Zero ? '-' : '+')}{offset:hh\\:mm}) {timeZone.DisplayName}";
    }

    private async Task OnTimeZoneChanged(string? timeZoneId)
    {
        if (string.IsNullOrEmpty(timeZoneId) || timeZoneId == currentTimeZoneId)
            return;

        currentTimeZoneId = timeZoneId;

        await TimeZoneService.ChangeTimeZone(timeZoneId);

        showTimeZones = false; // Back to the main menu panel, the way the tenant panel hands control back after a switch.

        // Re-renders the current page so every displayed date/time reflects the new time zone.
        //#if (brouter == true)
        brouter.ClearKeepAlive();
        await brouter.ReloadAsync();
        //#else
        //#if (IsInsideProjectTemplate == true)
        /*
        //#endif
        NavigationManager.RefreshCurrentPage();
        //#if (IsInsideProjectTemplate == true)
        */
        //#endif
        //#endif
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
            // Re-renders the current page so it reflects the new tenant's data.
            // The layout's tenant display (next to the app version) updates on its own: switching changes the tenant claim, which
            // triggers the authentication-state change that MainLayout re-resolves the current tenant from (See MainLayout.SetCurrentTenantIfNeeded).
            //#if (brouter == true)
            brouter.ClearKeepAlive();
            await brouter.ReloadAsync();
            //#else
            //#if (IsInsideProjectTemplate == true)
            /*
            //#endif
            NavigationManager.RefreshCurrentPage();
            //#if (IsInsideProjectTemplate == true)
            */
            //#endif
            //#endif
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
        showTimeZones = false;
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
