using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Websites.Platform.Client.Shared;

public partial class Header : IDisposable
{
    private bool isHeaderMenuOpen;
    private bool isProductsMenuOpen;
    private bool isMobileProductsOpen;
    private bool isProductsMenuForceClosed;
    private ProductSubMenu? openSubMenu;
    private ProductSubMenu? openMobileSubMenu;


    private sealed record ProductSubMenu(string Id, string Title, string Description, ProductLink[] Items);

    // The entries themselves live in ProductCatalog, shared with the products page. The utilities, the
    // toolings and bit Fabric sit in submenus, so the root of the Products menu stays focused on the main products.
    private static readonly ProductLink[] mainProductMenuItems = ProductCatalog.Main;

    // Closes the root list, so the products page is reachable from every page, not only the home page.
    private static readonly ProductLink allProductsMenuItem = new("All products", "Every product, utility and tooling", Urls.Products);

    private static readonly ProductSubMenu[] productSubMenus =
    [
        new("utilities", "Utilities", $"{ProductCatalog.Utilities.Length} focused Blazor libraries", ProductCatalog.Utilities),
        new("toolings", "Toolings", $"{ProductCatalog.Toolings.Length} tools to build apps with", ProductCatalog.Toolings),
        new("fabric", "bit Fabric", "Full apps on these libraries", ProductCatalog.Fabric),
    ];


    [AutoInject] public NavMenuService navMenuService = default!;
    [AutoInject] public BitThemeManager bitThemeManager = default!;

    protected override async Task OnInitAsync()
    {
        HandleActiveRoutes();

        NavigationManager.LocationChanged += OnLocationChanged;

        await base.OnInitAsync();
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs args)
    {
        HandleActiveRoutes();
        CloseMenus();

        StateHasChanged();
    }

    private void HandleActiveRoutes()
    {
        navMenuService.UpdateRouteFlags($"/{NavigationManager.ToBaseRelativePath(NavigationManager.Uri)}");
    }

    private void CloseMenus()
    {
        isProductsMenuOpen = false;
        openSubMenu = null;
        isMobileProductsOpen = false;
        openMobileSubMenu = null;

        if (isHeaderMenuOpen)
        {
            isHeaderMenuOpen = false;
            _ = JSRuntime.ToggleBodyOverflow(false);
        }
    }

    private async Task ToggleMenu()
    {
        await navMenuService.ToggleMenu();
    }

    private async Task ToggleProductsMenu()
    {
        isProductsMenuOpen = !isProductsMenuOpen;
        openSubMenu = null;

        if (isProductsMenuOpen)
        {
            isProductsMenuForceClosed = false;
        }
        else
        {
            await SuppressProductsMenu();
        }
    }

    private async Task HandleProductsMenuKeyDown(KeyboardEventArgs e)
    {
        if (e.Key is not "Escape") return;

        isProductsMenuOpen = false;
        openSubMenu = null;
        await SuppressProductsMenu();
    }

    // Only the click/tap/Enter path goes through here; a mouse hover opens the submenu with pure CSS.
    private void ToggleSubMenu(ProductSubMenu subMenu)
    {
        openSubMenu = openSubMenu == subMenu ? null : subMenu;
    }

    // The popup is also held open by the :hover / :focus-within CSS rules, so closing takes both:
    // force-closed suppresses them (a sticky :hover after a tap on a touch device, or the cursor
    // still parked on the trigger; lifted on the next mouseenter), and the blur drops the focus a
    // tap or an Enter press left on the trigger.
    private async Task SuppressProductsMenu()
    {
        isProductsMenuForceClosed = true;
        await JSRuntime.BlurActiveElement();
    }

    private async Task HandleHeaderNavKeyDown(KeyboardEventArgs e)
    {
        if (e.Key is not "Escape") return;

        await ToggleHeaderMenu();
    }

    private void ToggleMobileProducts()
    {
        isMobileProductsOpen = !isMobileProductsOpen;

        if (isMobileProductsOpen is false)
        {
            openMobileSubMenu = null;
        }
    }

    private void ToggleMobileSubMenu(ProductSubMenu subMenu)
    {
        openMobileSubMenu = openMobileSubMenu == subMenu ? null : subMenu;
    }

    private async Task HandleProductMenuItemClick()
    {
        isProductsMenuOpen = false;
        openSubMenu = null;
        await SuppressProductsMenu();
    }

    private void HandleProductsMenuMouseEnter()
    {
        isProductsMenuForceClosed = false;
    }

    // Attached only while force-closed. It has to be a default EventCallback otherwise, which renders
    // no handler at all: `cond ? handler : null` still builds a callback bound to this component with
    // a null delegate, which Blazor wires up anyway and then throws a NullReferenceException on in
    // DispatchEventAsync on every hover.
    private EventCallback<MouseEventArgs> ProductsMenuMouseEnterCallback =>
        isProductsMenuForceClosed ? EventCallback.Factory.Create<MouseEventArgs>(this, WrapHandled(HandleProductsMenuMouseEnter)) : default;

    private bool IsMenuItemActive(ProductLink item)
    {
        return item.External is false && navMenuService.IsCurrentUrl(item.Url);
    }

    private bool IsSubMenuActive(ProductSubMenu subMenu) => subMenu.Items.Any(IsMenuItemActive);

    private string GetActiveRouteName()
    {
        if (navMenuService.IsCurrentUrl(Urls.Home)) return "Home";
        if (navMenuService.IsCurrentUrl(Urls.Demos)) return "Demos";
        if (navMenuService.IsCurrentUrl(Urls.AboutUs)) return "About us";
        if (navMenuService.IsCurrentUrl(Urls.ContactUs)) return "Contact us";

        if (navMenuService.CurrentUrl.StartsWith(Urls.NotFound, StringComparison.OrdinalIgnoreCase)) return "404";

        return "Products";
    }

    private async Task ToggleHeaderMenu()
    {
        isHeaderMenuOpen = !isHeaderMenuOpen;

        if (isHeaderMenuOpen is false)
        {
            isMobileProductsOpen = false;
            openMobileSubMenu = null;
        }

        await JSRuntime.ToggleBodyOverflow(isHeaderMenuOpen);
        StateHasChanged();
    }

    private async Task ToggleTheme()
    {
        await bitThemeManager.ToggleDarkLightAsync();
    }


    public void Dispose()
    {
        NavigationManager.LocationChanged -= OnLocationChanged;
    }
}
