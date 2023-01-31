//-:cnd:noEmit

namespace AdminPanel.Client.Shared;

public partial class NavMenu
{
    private bool isMenuOpen;

    private List<BitNavItem> _navItems = default!;

    [CascadingParameter] public Task<AuthenticationState> AuthenticationStateTask { get; set; } = default!;

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
                Text = Localizer[nameof(AppStrings.ProductCategory)],
                IconName = BitIconName.Tag,
                Items = new List<BitNavItem>
                {
                    new BitNavItem
                    {
                        Text = Localizer[nameof(AppStrings.Products)],
                        Url = "/products",
                    },
                    new BitNavItem
                    {
                        Text = Localizer[nameof(AppStrings.Categories)],
                        Url = "/categories",
                    },
                }
            }
        };

        await base.OnInitAsync();
    }

    private async Task HandleOnItemClick(BitNavItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Url)) return;

        await CloseNavMenu();
    }

    private async Task CloseNavMenu()
    {
        IsMenuOpen = false;

        await JsRuntime.SetBodyOverflow(false);
    }
}
