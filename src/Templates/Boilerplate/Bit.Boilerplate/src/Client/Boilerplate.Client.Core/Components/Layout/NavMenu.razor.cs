//+:cnd:noEmit
using Boilerplate.Shared.Dtos.Identity;

namespace Boilerplate.Client.Core.Components.Layout;

public partial class NavMenu
{
    private bool disposed;
    private bool isMenuOpen;
    private bool isMenuToggled;
    private UserDto user = new();
    private bool isSignOutModalOpen;
    private string? profileImageUrl;
    private List<BitNavItem> navItems = [];
    private Action unsubOpenNavMenu = default!;
    private Action unsubUserDataChange = default!;

    [AutoInject] private NavigationManager navManager = default!;



    protected override async Task OnInitAsync()
    {
        CreateNavItems();

        unsubOpenNavMenu = PubSubService.Subscribe(PubSubMessages.OPEN_NAV_MENU, async _ => {
            isMenuOpen = true;
            StateHasChanged();
        });

        unsubUserDataChange = PubSubService.Subscribe(PubSubMessages.USER_DATA_UPDATED, async payload =>
        {
            if (payload is null) return;
            user = (UserDto)payload;
            StateHasChanged();
        });

        user = (await PrerenderStateService.GetValue(() => HttpClient.GetFromJsonAsync("api/User/GetCurrentUser", AppJsonContext.Default.UserDto, CurrentCancellationToken)))!;

        var serverAddress = Configuration.GetServerAddress();
        var access_token = await PrerenderStateService.GetValue(() => AuthTokenProvider.GetAccessTokenAsync());
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
        navManager.NavigateTo(Urls.ProfilePage);
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

    private async Task ToggleNavMenu()
    {
        isMenuToggled = !isMenuToggled;
    }


    protected override async ValueTask DisposeAsync(bool disposing)
    {
        await base.DisposeAsync(disposing);

        if (disposed || disposing is false) return;

        unsubOpenNavMenu?.Invoke();
        unsubUserDataChange?.Invoke();

        disposed = true;
    }
}
