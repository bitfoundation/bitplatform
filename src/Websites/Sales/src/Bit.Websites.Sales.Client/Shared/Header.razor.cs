namespace Bit.Websites.Sales.Client.Shared;

public partial class Header
{
    private string? currentUrl;
    private bool isMenuOpen;

    [AutoInject] private NavigationManager navigationManager = default!;
    [AutoInject] private IJSRuntime js = default!;

    protected override async Task OnInitAsync()
    {
        navigationManager.LocationChanged += OnLocationChanged;

        SetCurrentUrl();

        await base.OnInitAsync();
    }

    private void OnLocationChanged(object? sender, Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs e)
    {
        SetCurrentUrl();

        _ = CloseMenu();
    }

    private void SetCurrentUrl()
    {
        currentUrl = navigationManager.Uri.Replace(navigationManager.BaseUri, "/", StringComparison.Ordinal);
        var hashIndex = currentUrl.IndexOf('#');
        if (hashIndex > 0)
        {
            currentUrl = currentUrl.Substring(0, hashIndex);
        }
    }

    private async Task OpenMenu()
    {
        isMenuOpen = true;
        await js.InvokeVoidAsync("App.setBodyStyle", "overflow:hidden;");
        StateHasChanged();
    }

    private async Task CloseMenu()
    {
        isMenuOpen = false;
        await js.InvokeVoidAsync("App.setBodyStyle", "overflow:auto;");
        StateHasChanged();
    }
}
