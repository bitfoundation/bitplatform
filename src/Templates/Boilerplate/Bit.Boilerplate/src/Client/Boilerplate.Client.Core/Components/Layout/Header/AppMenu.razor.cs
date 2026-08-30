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


    private bool isOpen;
    private bool showCultures;
    private bool isSignOutConfirmOpen;
    private BitChoiceGroupItem<string>[] cultures = default!;
    private bool showTimeZones;
    private string? currentTimeZoneId;
    private string? timeZoneSearchText;
    private TimeZoneOption[] timeZones = [];
    //#if (notification == true)
    private bool pushNotificationsEnabled;
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

    private sealed record TimeZoneOption(string Id, string Text);

    private TimeZoneOption[] FilteredTimeZones =>
        string.IsNullOrWhiteSpace(timeZoneSearchText)
            ? timeZones
            : [.. timeZones.Where(tz => tz.Text.Contains(timeZoneSearchText, StringComparison.OrdinalIgnoreCase))];

    private bool IsCurrentTimeZone(TimeZoneOption timeZone) =>
        string.Equals(timeZone.Id, currentTimeZoneId, StringComparison.OrdinalIgnoreCase);


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

        // Warms the switch so the first open shows the real state; OnDropMenuOpen keeps it fresh from then on.
        await RefreshPushNotificationsState();
    }

    private string PushNotificationsToggleLabel => pushNotificationsEnabled
        ? Localizer[nameof(AppStrings.TurnPushNotificationsOff)].Value
        : Localizer[nameof(AppStrings.TurnPushNotificationsOn)].Value;

    /// <summary>
    /// What the switch shows: the preference stored on this device AND whether the platform will actually deliver a
    /// push. The preference alone defaults to enabled for a device that was never asked, which read as on in a
    /// browser whose notification permission was denied.
    /// </summary>
    private async Task RefreshPushNotificationsState()
    {
        pushNotificationsEnabled = await pushNotificationService.IsEnabled()
                                   && await pushNotificationService.IsAvailable(CurrentCancellationToken);

        StateHasChanged();
    }

    private async Task TogglePushNotifications()
    {
        var enable = pushNotificationsEnabled is false;

        if (enable)
        {
            // Asked first, so the prompt is still tied to the click that got us here.
            await pushNotificationService.RequestPermission(CurrentCancellationToken);
        }

        // Stored either way: the permission decides whether the device can receive a push, not whether the user
        // asked for one. Bailing out before SetEnabled - as this used to, on a check taken right after the prompt -
        // left anyone who had opted out stuck that way, and short circuited the automatic re-subscribe on the next
        // auth-state change too (See PushNotificationServiceBase).
        await pushNotificationService.SetEnabled(enable, CurrentCancellationToken);

        await RefreshPushNotificationsState();

        // Reported from the outcome: after an enable, the switch is only off when the platform refused.
        if (enable && pushNotificationsEnabled is false)
        {
            SnackBarService.Error(Localizer[nameof(AppStrings.PushNotificationsBlockedMessage)]);
        }
    }
    //#endif

    private async Task ShowTimeZones()
    {
        showTimeZones = true;
        timeZoneSearchText = null;
        currentTimeZoneId = (await TimeZoneService.GetCurrentTimeZone()).Id;

        // Rebuilt on every open rather than cached, because the current zone leads the list and changes with it.
        // Android's tzdata carries the IANA links as ids of their own ("Iran" beside "Asia/Tehran"), and both render
        // the same text, so rows that read alike are dropped - after the ordering, which keeps the current zone's one.
        timeZones = [.. TimeZoneInfo.GetSystemTimeZones()
            .OrderByDescending(tz => string.Equals(tz.Id, currentTimeZoneId, StringComparison.OrdinalIgnoreCase))
            .ThenBy(tz => tz.BaseUtcOffset)
            .ThenBy(tz => tz.Id, StringComparer.OrdinalIgnoreCase)
            .Select(tz => new TimeZoneOption(tz.Id, GetTimeZoneDisplayText(tz)))
            .DistinctBy(tz => tz.Text, StringComparer.OrdinalIgnoreCase)];
    }

    /// <summary>
    /// Normalizes the three wordings the runtimes produce - "(UTC+03:30) Tehran" on Windows, "(UTC+03:30) Asia/Tehran"
    /// in the browser, the bare "Asia/Tehran" on Android/iOS - to the first, keeping the offset so the list stays
    /// searchable by it. The IANA area is dropped and underscores become spaces ("Asia/Tehran" reads as "Tehran"),
    /// but only when the name IS the zone's own id, so a real display name containing a slash is never cut in half.
    /// </summary>
    private static string GetTimeZoneDisplayText(TimeZoneInfo timeZone)
    {
        var displayName = timeZone.DisplayName;

        var offsetEndIndex = displayName.StartsWith('(') ? displayName.IndexOf(')', StringComparison.Ordinal) : -1;

        var offset = timeZone.BaseUtcOffset;
        var offsetText = offsetEndIndex is -1
            ? $"(UTC{(offset < TimeSpan.Zero ? '-' : '+')}{offset:hh\\:mm})"
            : displayName[..(offsetEndIndex + 1)];

        var place = offsetEndIndex is -1 ? displayName : displayName[(offsetEndIndex + 1)..].TrimStart();

        if (place == timeZone.Id && place.IndexOf('/', StringComparison.Ordinal) is int areaSeparatorIndex and not -1)
        {
            place = place[(areaSeparatorIndex + 1)..].Replace('_', ' ');
        }

        return $"{offsetText} {place}";
    }

    private async Task OnTimeZoneChanged(string timeZoneId)
    {
        if (timeZoneId == currentTimeZoneId) return;

        currentTimeZoneId = timeZoneId;

        await TimeZoneService.ChangeTimeZone(timeZoneId);
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

        CloseMenu(); // A switch that fails leaves the menu closed rather than sitting on the tenant panel behind the error.

        // Switching calls the refresh token api that stores the new tenant id in the token's claims (See IdentityController.Refresh).
        if (await AuthManager.SwitchTenant(newTenantId, CurrentCancellationToken))
        {
            // Rebuilds everything the previous tenant's data reached: the current page, the tenant shown in the
            // nav panel, this menu itself.
            PubSubService.Publish(ClientAppMessages.SOFT_RESTART);
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
    /// Re-reads whatever the menu shows that can go stale while it is closed. The notification permission is the one
    /// that matters: it changes in the browser's or the OS's own settings, and nothing tells the app when.
    /// </summary>
    private async Task OnDropMenuOpen()
    {
        //#if (notification == true)
        await RefreshPushNotificationsState();
        //#endif
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
