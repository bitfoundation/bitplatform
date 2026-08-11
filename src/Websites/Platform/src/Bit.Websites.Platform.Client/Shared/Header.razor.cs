using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Websites.Platform.Client.Shared;

public partial class Header : IDisposable
{
    private bool isHeaderMenuOpen;
    private bool isProductsMenuOpen;
    private bool isMobileProductsOpen;
    private bool isProductsMenuForceClosed;


    private sealed record ProductMenuItem(string Title, string Description, string Url, bool External = false, bool Disabled = false);

    private static readonly ProductMenuItem[][] productMenuColumns =
    [
        [
            new("""Low-code/<span style="opacity:0.5">No-code</span>""", "(Private alpha)", Urls.LowCodeNoCode),
            new("Boilerplate", "Feature-rich .NET project template", Urls.Templates),
            new("Butil", "Blazor utils for browser APIs", Urls.Butil, External: true),
            new("Bswup", "Blazor PWA on steroids", Urls.Bswup, External: true),
            new("Besql", "Blazor Entity Framework SQLite", Urls.Besql),
        ],
        [
            new("Brouter", "Modern declarative Blazor router", Urls.Brouter, External: true),
            new("Bmotion", "Blazor-native animation library", Urls.Bmotion, External: true),
            new("BlazorUI", "Native Blazor UI components", Urls.BlazorUI, External: true),
            new("bit Academy", "Coming soon", string.Empty, Disabled: true),
        ]
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
        isMobileProductsOpen = false;

        if (isHeaderMenuOpen)
        {
            isHeaderMenuOpen = false;
            _ = JSRuntime.ToggleBodyOverflow(false);
        }
    }

    private void ToggleMenu()
    {
        navMenuService.ToggleMenu();
    }

    private async Task ToggleProductsMenu()
    {
        isProductsMenuOpen = !isProductsMenuOpen;

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
        await SuppressProductsMenu();
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
    }

    private async Task HandleProductMenuItemClick()
    {
        isProductsMenuOpen = false;
        await SuppressProductsMenu();
    }

    private void HandleProductsMenuMouseEnter()
    {
        isProductsMenuForceClosed = false;
    }

    private bool IsMenuItemActive(ProductMenuItem item)
    {
        if (item.Url == Urls.LowCodeNoCode) return navMenuService.IsLcncDocRoute;
        if (item.Url == Urls.Templates) return navMenuService.IsTemplateDocRoute;
        if (item.Url == Urls.Besql) return navMenuService.IsBesqlDocRoute;

        return false;
    }

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
