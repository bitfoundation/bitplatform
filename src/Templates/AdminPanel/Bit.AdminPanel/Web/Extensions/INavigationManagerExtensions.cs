namespace AdminPanel.App.Extensions;

public static class INavigationManagerExtensions
{
    public static void ForceReload(this NavigationManager navigationManager)
    {
        navigationManager.NavigateTo(navigationManager.ToBaseRelativePath(navigationManager.Uri), forceLoad: true);
    }
}
