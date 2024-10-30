namespace AdminPanel.Client.Core.Services;

public partial class DefaultExternalNavigationService : IExternalNavigationService
{
    [AutoInject] private readonly NavigationManager navigationManager = default!;

    public async Task NavigateToAsync(string url)
    {
        navigationManager.NavigateTo(url, forceLoad: true, replace: true);
    }
}
