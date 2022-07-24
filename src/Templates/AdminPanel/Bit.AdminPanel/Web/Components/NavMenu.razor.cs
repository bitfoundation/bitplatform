//-:cnd:noEmit
using AdminPanel.Shared.Dtos.Account;

namespace AdminPanel.App.Components;

public partial class NavMenu
{
    [AutoInject] private HttpClient httpClient = default!;

    [AutoInject] private IStateService stateService = default!;

    [AutoInject] private IAuthTokenProvider authTokenProvider = default!;

#if BlazorServer || BlazorHybrid
    [AutoInject] private IConfiguration configuration = default!;
#endif

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
                Name = "Dashboard",
                Url = "/",
                IconName = BitIconName.ViewDashboard,
                Key = "Dashboard",
                Links=new List<BitNavLinkItem>
                        {
                            new BitNavLinkItem
                            {
                                Name = "Analytics",
                                Url = "/analytics",
                                IconName = BitIconName.AnalyticsReport,
                                Key = "Analytics"
                            },
                        }
            },
            new BitNavLinkItem
            {
                Name = "Product catologue",
                Url = "/",
                IconName = BitIconName.ProductCatalog,
                Key = "ProductCatologue",
                Links=new List<BitNavLinkItem>
                        {
                            new BitNavLinkItem
                            {
                                Name = "Products",
                                Url = "/products",
                                IconName = BitIconName.Product,
                                Key = "Products"
                            },
                            new BitNavLinkItem
                            {
                                Name = "Categories",
                                Url = "/categories",
                                IconName = BitIconName.ProductCatalog,
                                Key = "Categories"
                            },
                        }
            },
            new BitNavLinkItem
            {
                Name = "Home",
                Url = "/",
                IconName = BitIconName.Home,
                Key = "Home"
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

    private void CloseMenu()
    {
        IsMenuOpen = false;
    }

    protected override async Task OnInitAsync()
    {
        User = await stateService.GetValue($"{nameof(NavMenu)}-{nameof(User)}", async () =>
            await httpClient.GetFromJsonAsync("User/GetCurrentUser", AppJsonContext.Default.UserDto));

        var access_token = await stateService.GetValue($"{nameof(NavMenu)}-access_token", async () =>
            await authTokenProvider.GetAcccessToken());

        ProfileImageUrl = $"{GetBaseUrl()}Attachment/GetProfileImage?access_token={access_token}&file={User!.ProfileImageName}";

        await base.OnInitAsync();
    }

    string GetBaseUrl()
    {
#if BlazorWebAssembly
        return "/api/";
#else
        return configuration.GetValue<string>("ApiServerAddress");
#endif
    }
}
