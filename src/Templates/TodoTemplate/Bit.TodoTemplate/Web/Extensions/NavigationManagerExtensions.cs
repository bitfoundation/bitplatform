namespace Microsoft.AspNetCore.Components;

public static class INavigationManagerExtensions
{
    public static void Reload(this NavigationManager navigationManager)
    {
        navigationManager.NavigateTo(navigationManager.ToBaseRelativePath(navigationManager.Uri), forceLoad: true);
    }
}
