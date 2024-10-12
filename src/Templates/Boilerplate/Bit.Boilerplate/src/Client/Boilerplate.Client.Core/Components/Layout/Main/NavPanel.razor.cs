using Boilerplate.Shared.Dtos.Identity;

namespace Boilerplate.Client.Core.Components.Layout.Main;

public partial class NavPanel
{
    private bool disposed;
    private bool isMenuOpen;
    private bool isMenuToggled;
    private UserDto user = new();
    private bool isSignOutModalOpen;
    private string? profileImageUrl;
    private List<BitNavItem> navItems = [];
    private Action unsubOpenNavPanel = default!;
    private Action unsubUserDataChange = default!;

    [AutoInject] private NavigationManager navManager = default!;



    protected override async Task OnInitAsync()
    {
        CreateNavItems();

        unsubOpenNavPanel = PubSubService.Subscribe(PubSubMessages.OPEN_NAV_PANEL, async _ => {
            isMenuOpen = true;
            StateHasChanged();
        });

        unsubUserDataChange = PubSubService.Subscribe(PubSubMessages.PROFILE_UPDATED, async payload =>
        {
            if (payload is null) return;
            user = (UserDto)payload;
            StateHasChanged();
        });

        user = (await PrerenderStateService.GetValue(() => HttpClient.GetFromJsonAsync("api/User/GetCurrentUser", AppJsonContext.Default.UserDto, CurrentCancellationToken)))!;

        var serverAddress = Configuration.GetServerAddress();
        var access_token = await PrerenderStateService.GetValue(() => AuthTokenProvider.GetAccessToken());
        profileImageUrl = $"{serverAddress}/api/Attachment/GetProfileImage?access_token={access_token}";
    }


    private async Task DoSignOut()
    {
        isSignOutModalOpen = true;
        await CloseMenu();
    }

    private async Task GoToProfile()
    {
        await CloseMenu();
        navManager.NavigateTo(Urls.SettingsPage);
    }

    private async Task HandleNavItemClick(BitNavItem item)
    {
        if (string.IsNullOrEmpty(item.Url)) return;

        await CloseMenu();
    }

    private async Task CloseMenu()
    {
        isMenuOpen = false;
    }

    private async Task ToggleNavPanel()
    {
        isMenuToggled = !isMenuToggled;
    }


    protected override async ValueTask DisposeAsync(bool disposing)
    {
        await base.DisposeAsync(disposing);

        if (disposed || disposing is false) return;

        unsubOpenNavPanel?.Invoke();
        unsubUserDataChange?.Invoke();

        disposed = true;
    }
}
