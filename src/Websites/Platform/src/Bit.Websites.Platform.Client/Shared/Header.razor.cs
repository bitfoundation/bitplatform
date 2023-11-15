using Microsoft.AspNetCore.Components.Routing;

namespace Bit.Websites.Platform.Client.Shared;

public partial class Header : IDisposable
{
    private string _currentUrl = string.Empty;
    private bool _isHeaderMenuOpen;

    [AutoInject] public NavManuService _navManuService { get; set; } = default!;
    [AutoInject] public BitThemeManager _bitThemeManager { get; set; } = default!;

    protected override async Task OnInitAsync()
    {
        _currentUrl = NavigationManager.Uri.Replace(NavigationManager.BaseUri, "/", StringComparison.Ordinal);
        NavigationManager.LocationChanged += OnLocationChanged;

        await base.OnInitAsync();
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs args)
    {
        _currentUrl = NavigationManager.Uri.Replace(NavigationManager.BaseUri, "/", StringComparison.Ordinal);
        StateHasChanged();
    }

    private void ToggleMenu()
    {
        _navManuService.ToggleMenu();
    }

    private string GetActiveRouteName()
    {
        if (_currentUrl.Contains("templates"))
        {
            return "Products & Services";
        }
        else return _currentUrl switch
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
        return (_currentUrl.Contains("templates") ||
           _currentUrl == Urls.Components ||
           _currentUrl == Urls.CloudHostingSolutins ||
           _currentUrl == Urls.Support ||
           _currentUrl == Urls.Academy);
    }

    private async Task ToggleHeaderMenu()
    {
        _isHeaderMenuOpen = !_isHeaderMenuOpen;
        await JSRuntime.ToggleBodyOverflow(_isHeaderMenuOpen);
        StateHasChanged();
    }

    private async Task ToggleTheme()
    {
        var newTheme = await _bitThemeManager.ToggleDarkLightAsync();
        var isDark = newTheme.Contains("dark");
    }



    public void Dispose()
    {
        NavigationManager.LocationChanged -= OnLocationChanged;
    }
}
