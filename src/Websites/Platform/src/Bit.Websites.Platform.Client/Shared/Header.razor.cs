using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Websites.Platform.Client.Shared;

public partial class Header : IDisposable
{
    private bool isDocsRoute;
    private bool isLcncDocRoute;
    private bool isProductsRoute;
    private bool isBesqlDocRoute;
    private bool isHeaderMenuOpen;
    private bool isTemplateDocRoute;
    private bool isProductsMenuOpen;
    private bool isMobileProductsOpen;
    private bool isProductsMenuForceClosed;
    private string currentUrl = string.Empty;


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
        currentUrl = $"/{NavigationManager.ToBaseRelativePath(NavigationManager.Uri)}";
        var fragmentIndex = currentUrl.IndexOfAny(['?', '#']);
        if (fragmentIndex >= 0)
        {
            currentUrl = currentUrl[..fragmentIndex];
        }

        isBesqlDocRoute = currentUrl.Contains("besql");
        isLcncDocRoute = currentUrl.Contains("lowcode-nocode");
        isTemplateDocRoute = currentUrl.Contains("templates") || currentUrl.Contains("boilerplate") ||
                             currentUrl.Contains("admin-panel") || currentUrl.Contains("todo-template");

        // Docs routes get the docs nav menu (hamburger); Lcnc has no docs, so it is excluded there,
        // but it still counts as a product route for highlighting the Products menu trigger.
        isDocsRoute = isTemplateDocRoute || isBesqlDocRoute;
        isProductsRoute = isDocsRoute || isLcncDocRoute;
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
            // The popup is also held open by the :hover / :focus-within CSS rules, and after a tap or
            // an Enter press the trigger keeps focus; dropping it is what actually dismisses the popup.
            await JSRuntime.BlurActiveElement();
        }
    }

    private async Task HandleProductsMenuKeyDown(KeyboardEventArgs e)
    {
        if (e.Key is not "Escape") return;

        isProductsMenuOpen = false;
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
        // The :hover CSS would hold the popup open while the cursor is still parked on the clicked
        // item; force-closed suppresses it (see the scss) and is lifted once the pointer leaves.
        isProductsMenuForceClosed = true;
        await JSRuntime.BlurActiveElement();
    }

    private void HandleProductsMenuMouseLeave()
    {
        isProductsMenuForceClosed = false;
    }

    private bool IsMenuItemActive(ProductMenuItem item)
    {
        if (item.Url == Urls.LowCodeNoCode) return isLcncDocRoute;
        if (item.Url == Urls.Templates) return isTemplateDocRoute;
        if (item.Url == Urls.Besql) return isBesqlDocRoute;

        return false;
    }

    private string GetActiveRouteName()
    {
        var routeName = currentUrl switch
        {
            Urls.Home => "Home",
            Urls.Demos => "Demos",
            Urls.Pricing => "Pricing",
            Urls.AboutUs => "About us",
            Urls.ContactUs => "Contact us",
            _ => null,
        };

        if (routeName is not null) return routeName;

        if (currentUrl.StartsWith(Urls.NotFound)) return "404";

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
