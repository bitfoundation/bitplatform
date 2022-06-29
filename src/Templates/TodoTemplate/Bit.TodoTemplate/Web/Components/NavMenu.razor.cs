﻿//-:cnd:noEmit
using TodoTemplate.Shared.Dtos.Account;

namespace TodoTemplate.App.Components;

public partial class NavMenu
{
    private bool IsMenuOpenHasBeenSet;
    private bool isMenuOpen;

    public List<BitNavLinkItem> NavLinks { get; set; }

    public UserDto? User { get; set; } = new();

    public string? ProfileImageUrl { get; set; }

    public bool IsSignOutModalOpen { get; set; }

    public NavMenu()
    {
        NavLinks = new()
        {
            new BitNavLinkItem
            {
                Name = "Home",
                Url = "/",
                IconName = BitIconName.Home,
                Key = "Home"
            },
            new BitNavLinkItem
            {
                Name = "Todo",
                Url = "/todo",
                IconName = BitIconName.ToDoLogoOutline,
                Key = "Todo"
            },
            new BitNavLinkItem
            {
                Name = "Sign out",
                OnClick = (item) =>
                {
                    IsSignOutModalOpen = true;
                    StateHasChanged();
                },
                IconName = BitIconName.SignOut,
                Key = "SignOut"
            }
        };
    }

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

    [AutoInject] private HttpClient HttpClient { get; set; } = default!;

    [AutoInject] private IStateService StateService { get; set; } = default!;

    [AutoInject] private IAuthTokenProvider AuthTokenProvider { get; set; } = default!;

#if BlazorServer || BlazorHybrid
    [AutoInject] private IConfiguration Configuration { get; set; } = default!;
#endif

    private void CloseMenu()
    {
        if (IsMenuOpenHasBeenSet && IsMenuOpenChanged.HasDelegate is false) return;

        IsMenuOpen = false;
    }

    protected override async Task OnInitAsync()
    {
        User = await StateService.GetValue($"{nameof(NavMenu)}-{nameof(User)}", async () =>
            await HttpClient.GetFromJsonAsync("User/GetCurrentUser", TodoTemplateJsonContext.Default.UserDto));

        var access_token = await StateService.GetValue($"{nameof(NavMenu)}-access_token", async () =>
            await AuthTokenProvider.GetAcccessToken());

        ProfileImageUrl = $"api/Attachment/GetProfileImage?access_token={access_token}";

#if BlazorServer || BlazorHybrid
        var serverUrl = Configuration.GetValue<string>("ApiServerAddress");
        ProfileImageUrl = $"{serverUrl}{ProfileImageUrl}";
#endif

        await base.OnInitAsync();
    }
}
