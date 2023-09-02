//-:cnd:noEmit
using TodoTemplate.Shared.Dtos.Identity;

namespace TodoTemplate.Client.Core.Shared;

public partial class NavMenu : IDisposable
{
    private bool _disposed;
    private bool _isSignOutModalOpen;
    private string? _profileImageUrl;
    private string? _profileImageUrlBase;
    private UserDto _user = new();
    private List<BitNavItem> _navItems = new();
    private Action _unsubscribe = default!;

    [AutoInject] private NavigationManager _navManager { get; set; } = default!;

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
                Text = Localizer[nameof(AppStrings.EditProfileTitle)],
                IconName = BitIconName.EditContact,
                Url = "/edit-profile",
            },
            new BitNavItem
            {
                Text = Localizer[nameof(AppStrings.TermsTitle)],
                IconName = BitIconName.EntityExtraction,
                Url = "/terms",
            }
        };

        _unsubscribe = PubSubService.Subscribe(PubSubMessages.PROFILE_UPDATED, payload =>
        {
            if (payload is null) return;

            _user = (UserDto)payload;

            SetProfileImageUrl();

            StateHasChanged();
        });

        _user = await PrerenderStateService.GetValue($"{nameof(NavMenu)}-{nameof(_user)}", async () =>
            await HttpClient.GetFromJsonAsync("User/GetCurrentUser", AppJsonContext.Default.UserDto)) ?? new();

        var access_token = await PrerenderStateService.GetValue($"{nameof(NavMenu)}-access_token", AuthTokenProvider.GetAccessTokenAsync);
        _profileImageUrlBase = $"{Configuration.GetApiServerAddress()}Attachment/GetProfileImage?access_token={access_token}&file=";

        SetProfileImageUrl();
    }

    private void SetProfileImageUrl()
    {
        _profileImageUrl = _user.ProfileImageName is not null ? _profileImageUrlBase + _user.ProfileImageName : null;
    }

    private async Task DoSignOut()
    {
        _isSignOutModalOpen = true;

        await CloseMenu();
    }

    private async Task GoToEditProfile()
    {
        await CloseMenu();
        _navManager.NavigateTo("edit-profile");
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
