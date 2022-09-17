//-:cnd:noEmit
using TodoTemplate.Shared.Dtos.Account;

namespace TodoTemplate.Client.Components;

public partial class NavMenu
{
    private bool isMenuOpen;

    public List<BitNavLinkItem> NavLinks { get; set; }

    public UserDto? User { get; set; } = new();

    public string? ProfileImageUrl { get; set; }

    public bool IsSignOutModalOpen { get; set; }

    [CascadingParameter]
    public Task<AuthenticationState> AuthenticationStateTask { get; set; } = default!;

    [Parameter]
    public bool IsMenuOpen
    {
        get => isMenuOpen;
        set
        {
            if (value == isMenuOpen) return;
            isMenuOpen = value;
            _ = IsMenuOpenChanged.InvokeAsync(value);
        }
    }

    [Parameter] public EventCallback<bool> IsMenuOpenChanged { get; set; }

    private void CloseMenu()
    {
        IsMenuOpen = false;
    }

    protected override async Task OnInitAsync()
    {
        NavLinks = new()
        {
            new BitNavLinkItem
            {
                Name = Localizer[nameof(AppStrings.Home)],
                Url = "/",
                IconName = BitIconName.Home,
                Key = "Home"
            },
            new BitNavLinkItem
            {
                Name = Localizer[nameof(AppStrings.TodoTitle)],
                Url = "/todo",
                IconName = BitIconName.ToDoLogoOutline,
                Key = "Todo"
            },
            new BitNavLinkItem
            {
                Name = Localizer[nameof(AppStrings.SignOut)],
                OnClick = (item) =>
                {
                    IsSignOutModalOpen = true;
                    StateHasChanged();
                },
                IconName = BitIconName.SignOut,
                Key = "SignOut"
            }
        };

        User = await StateService.GetValue($"{nameof(NavMenu)}-{nameof(User)}", async () =>
            await HttpClient.GetFromJsonAsync("User/GetCurrentUser", AppJsonContext.Default.UserDto));

        var access_token = await StateService.GetValue($"{nameof(NavMenu)}-access_token", async () =>
            await AuthTokenProvider.GetAcccessToken());

        ProfileImageUrl = $"{GetBaseUrl()}Attachment/GetProfileImage?access_token={access_token}&file={User!.ProfileImageName}";

        await base.OnInitAsync();
    }

    string GetBaseUrl()
    {
#if BlazorWebAssembly
        return "/api/";
#else
        return Configuration.GetValue<string>("ApiServerAddress");
#endif
    }
}
