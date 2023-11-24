using Microsoft.AspNetCore.Components.Routing;

namespace Bit.Websites.Platform.Client.Shared;

public partial class Header : IDisposable
{
    private string currentUrl = string.Empty;
    private bool isHeaderMenuOpen;

    [AutoInject] public NavManuService navManuService = default!;
    [AutoInject] public BitThemeManager bitThemeManager  = default!;

    protected override async Task OnInitAsync()
    {
        currentUrl = NavigationManager.Uri.Replace(NavigationManager.BaseUri, "/", StringComparison.Ordinal);
        NavigationManager.LocationChanged += OnLocationChanged;

        await base.OnInitAsync();
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs args)
    {
        currentUrl = NavigationManager.Uri.Replace(NavigationManager.BaseUri, "/", StringComparison.Ordinal);
        StateHasChanged();
    }

    private void ToggleMenu()
    {
        navManuService.ToggleMenu();
    }

    private string GetActiveRouteName()
    {
        if (currentUrl.Contains("templates"))
        {
            return "Products & Services";
        }
        else return currentUrl switch
        {
            Urls.HomePage => "Home",
            Urls.Components => "Products & Services",
            Urls.CloudHostingSolutins => "Products & Services",
            Urls.Support => "Products & Services",
            Urls.Academy => "Products & Services",
            Urls.Pricing => "Pricing",
            Urls.AboutUs => "About us",
            Urls.ContactUs => "Contact us",
            Urls.Blogs => "Blogs",
            Urls.Videos => "Videos",
            _ => "Products & Services",
        };
    }

    private bool IsProductsServicesActive()
    {
        return (currentUrl.Contains("templates") ||
           currentUrl == Urls.Components ||
           currentUrl == Urls.CloudHostingSolutins ||
           currentUrl == Urls.Support ||
           currentUrl == Urls.Academy);
    }

    private async Task ToggleHeaderMenu()
    {
        isHeaderMenuOpen = !isHeaderMenuOpen;
        await JSRuntime.ToggleBodyOverflow(isHeaderMenuOpen);
        StateHasChanged();
    }

    private async Task ToggleTheme()
    {
        var newTheme = await bitThemeManager.ToggleDarkLightAsync();
        var isDark = newTheme.Contains("dark");
    }



    public void Dispose()
    {
        NavigationManager.LocationChanged -= OnLocationChanged;
    }
}
