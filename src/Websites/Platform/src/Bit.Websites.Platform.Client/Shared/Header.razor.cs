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


    private sealed record ProductMenuItem(string Title, string Description, string Url, bool External = false, bool Disabled = false);

    private sealed record ProductSubMenu(string Id, string Title, string Description, ProductMenuItem[] Items);

    private static readonly ProductMenuItem[] mainProductMenuItems =
    [
        new("Boilerplate", "Feature-rich project template", Urls.Templates),
        new("BlazorUI", "Native Blazor UI components", Urls.BlazorUI, External: true),
        new("Butil", "Blazor utils for browser APIs", Urls.Butil, External: true),
        new("Bswup", "Blazor PWA on steroids", Urls.Bswup, External: true),
        new("Brouter", "Modern declarative Blazor router", Urls.Brouter, External: true),
    ];

    // Listed under the Utilities submenu (the projects under src/Utilities), so the root of the
    // Products menu stays focused on the main products.
    private static readonly ProductMenuItem[] utilityMenuItems =
    [
        new("Bmotion", "Blazor-native animation library", Urls.Bmotion, External: true),
        new("Bmage", "Blazor-native image processing", Urls.Bmage, External: true),
        new("Bsqlite", "SQLite and EF Core in the browser", Urls.Bsqlite, External: true),
        new("Bink", "Blazor-native drawing and ink", Urls.Bink, External: true),
        new("Balette", "Blazor-native command palette", Urls.Balette, External: true),
        new("BbScanner", "Blazor-native barcode scanner", Urls.BbScanner, External: true),
        new("Bdnd", "Declarative drag and drop for Blazor", Urls.Bdnd, External: true),
        new("BlazeDb", "Memory-first database for Blazor", Urls.BlazeDb, External: true),
        new("Blow", "Blazor-native node-based editors", Urls.Blow, External: true),
        new("Borm", "Schema-driven forms for Blazor", Urls.Borm, External: true),
        new("Bcrdt", "Real-time collaborative editing for Blazor", Urls.Bcrdt, External: true),
        new("Bonboarding", "Blazor-native product tours", Urls.Bonboarding, External: true),
        new("Bortable", "Blazor-native sortable lists", Urls.Bortable, External: true),
        new("Blex", "Reactive state management for Blazor and MAUI", Urls.Blex, External: true),
        new("Belemetry", "OpenTelemetry for every Blazor app", Urls.Belemetry, External: true),
    ];

    private static readonly ProductMenuItem[] toolingMenuItems =
    [
        new("bit CLI", "Ready-to-run projects in one command", Urls.Cli),
        new("Minifier", "Smaller Blazor WebAssembly downloads", Urls.Minifier),
        new("BlazorES2019", "Blazor apps on older browsers", Urls.BlazorES2019),
        new("Code Analyzers", "Best practices checked as you type", Urls.CodeAnalyzers),
        new("Source Generators", "Less code to write and review", Urls.SourceGenerators),
    ];

    private static readonly ProductSubMenu[] productSubMenus =
    [
        new("utilities", "Utilities", $"{utilityMenuItems.Length} focused Blazor libraries", utilityMenuItems),
        new("toolings", "Toolings", $"{toolingMenuItems.Length} tools to build apps with", toolingMenuItems),
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

    private bool IsMenuItemActive(ProductMenuItem item)
    {
        if (item.Url == Urls.Templates) return navMenuService.IsTemplateDocRoute;

        return item.External is false && navMenuService.CurrentUrl == item.Url;
    }

    private bool IsSubMenuActive(ProductSubMenu subMenu) => subMenu.Items.Any(IsMenuItemActive);

    private string GetActiveRouteName()
    {
        var routeName = navMenuService.CurrentUrl switch
        {
            Urls.Home => "Home",
            Urls.Demos => "Demos",
            Urls.Pricing => "Pricing",
            Urls.AboutUs => "About us",
            Urls.ContactUs => "Contact us",
            _ => null,
        };

        if (routeName is not null) return routeName;

        if (navMenuService.CurrentUrl.StartsWith(Urls.NotFound)) return "404";

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
