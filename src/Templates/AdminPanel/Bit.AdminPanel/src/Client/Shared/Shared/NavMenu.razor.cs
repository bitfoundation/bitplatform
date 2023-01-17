//-:cnd:noEmit

namespace AdminPanel.Client.Shared;

public partial class NavMenu
{
    private bool isMenuOpen;

    private List<BitNavLinkItem> _navLinks = default!;

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
        _navLinks = new()
        {
            new BitNavLinkItem
            {
                Name = Localizer[nameof(AppStrings.Home)],
                Key = "Home",
                IconName = BitIconName.Home,
                Url = "/",
            },
            new BitNavLinkItem
            {
                Name = Localizer[nameof(AppStrings.ProductCategory)],
                Key = "Product catolog",
                IconName = BitIconName.Tag,
                Links = new List<BitNavLinkItem>
                {
                    new BitNavLinkItem
                    {
                        Name = Localizer[nameof(AppStrings.Products)],
                        Url = "/products",
                        Key = "Products"
                    },
                    new BitNavLinkItem
                    {
                        Name = Localizer[nameof(AppStrings.Categories)],
                        Url = "/categories",
                        Key = "Categories"
                    },
                }
            }
        };

        await base.OnInitAsync();
    }

    private async Task HandleLinkClick(BitNavLinkItem item)
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
