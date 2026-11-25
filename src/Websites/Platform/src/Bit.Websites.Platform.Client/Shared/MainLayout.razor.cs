using Microsoft.AspNetCore.Components.Routing;

namespace Bit.Websites.Platform.Client.Shared;

public partial class MainLayout : IDisposable
{
    [AutoInject] private NavMenuService navMenuService = default!;
    [AutoInject] private NavigationManager navigationManager = default!;

    private List<BitNavItem> navItems = [];



    private readonly List<BitNavItem> toolingsNavItems =
    [
        new BitNavItem { Text = "Minifier", Url = Urls.Minifier },
        new BitNavItem { Text = "BlazorES2019", Url = Urls.BlazorES2019 },
        new BitNavItem { Text = "Code Analyzers", Url = Urls.CodeAnalyzers },
        new BitNavItem { Text = "Source Generators", Url = Urls.SourceGenerators },
    ];

    protected override Task OnInitializedAsync()
    {
        SetNavItems();

        navigationManager.LocationChanged += OnLocationChanged;

        return base.OnInitializedAsync();
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs args)
    {
        SetNavItems();

        StateHasChanged();
    }

    private void SetNavItems()
    {
        // The flags live in NavMenuService so this component and Header (which renders the docs
        // hamburger that toggles the NavMenu mounted here) always agree on what a docs route is.
        navMenuService.UpdateRouteFlags($"/{navigationManager.ToBaseRelativePath(navigationManager.Uri)}");

        navItems = navMenuService.IsDocsRoute ? toolingsNavItems : [];
    }

    public void Dispose()
    {
        navigationManager.LocationChanged -= OnLocationChanged;
    }
}
