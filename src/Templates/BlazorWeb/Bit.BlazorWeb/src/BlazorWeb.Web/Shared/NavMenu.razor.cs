//-:cnd:noEmit
using BlazorWeb.Shared.Dtos.Account;

namespace BlazorWeb.Web;

public partial class NavMenu : IDisposable
{
    private bool _disposed;
    private bool _isSignOutModalOpen;
    private string? _profileImageUrl;
    private string? _profileImageUrlBase;
    private UserDto _user = new();
    private List<BitNavItem> _navItems = new();

    private Action _unsubscribe = default!;

    [Parameter] public bool IsMenuOpen { get; set; }

    [Parameter] public EventCallback<bool> IsMenuOpenChanged { get; set; }

    protected override async Task OnInitAsync()
    {
        _navItems = new()
        {
            new BitNavItem
            {
                Text = Localizer[nameof(AppStrings.Home)],
                IconName = BitIconName.Home,
                Url = "/",
            },
            new BitNavItem
            {
                Text = Localizer[nameof(AppStrings.TodoTitle)],
                IconName = BitIconName.ToDoLogoOutline,
                Url = "/todo",
            },
            new BitNavItem
            {
                Text = Localizer[nameof(AppStrings.SignOut)],
                IconName = BitIconName.SignOut,
            }
        };

        _unsubscribe = PubSubService.Sub(PubSubMessages.PROFILE_UPDATED, payload =>
        {
            if (payload is null) return;

            _user = (UserDto)payload;

            SetProfileImageUrl();

            StateHasChanged();
        });

        _user = await StateService.GetValue($"{nameof(NavMenu)}-{nameof(_user)}", async () =>
            await HttpClient.GetFromJsonAsync("User/GetCurrentUser", AppJsonContext.Default.UserDto)) ?? new();

        var access_token = await StateService.GetValue($"{nameof(NavMenu)}-access_token", AuthTokenProvider.GetAcccessTokenAsync);
        _profileImageUrlBase = $"{GetBaseUrl()}Attachment/GetProfileImage?access_token={access_token}&file=";

        SetProfileImageUrl();
    }

    private void SetProfileImageUrl()
    {
        _profileImageUrl = _user.ProfileImageName is not null ? _profileImageUrlBase + _user.ProfileImageName : null;
    }

    private string GetBaseUrl()
    {
#if BlazorWebAssembly
        return "/api/";
#else
        return Configuration.GetValue<string>("ApiServerAddress") ?? string.Empty;
#endif
    }

    private async Task HandleOnItemClick(BitNavItem item)
    {
        if (item.Text == Localizer[nameof(AppStrings.SignOut)])
        {
            _isSignOutModalOpen = true;
        }

        await CloseMenu();
    }

    private async Task CloseMenu()
    {
        IsMenuOpen = false;
        await IsMenuOpenChanged.InvokeAsync(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed || disposing is false) return;

        _unsubscribe?.Invoke();

        _disposed = true;
    }
}
