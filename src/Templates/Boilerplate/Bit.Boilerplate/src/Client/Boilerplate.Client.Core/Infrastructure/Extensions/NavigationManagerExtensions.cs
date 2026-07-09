namespace Microsoft.AspNetCore.Components;

public static partial class NavigationManagerExtensions
{
    public static string GetUriWithoutQueryParameter(this NavigationManager navigationManager, string key)
    {
        return new Uri(navigationManager.Uri).GetUrlWithoutQueryParameter(key);
    }

    public static string GetUriPath(this NavigationManager navigationManager)
    {
        return new Uri(navigationManager.Uri).GetPath();
    }

    public static string GetRelativePath(this NavigationManager navigationManager)
    {
        return navigationManager.ToBaseRelativePath(navigationManager.Uri);
    }

    /// <summary>
    /// This would re-render the current page.
    /// Note that <see cref="NavigationManager.Refresh(bool)"/> might either decide to do nothing at all of refresh the entire app dependeing on the situation
    /// </summary>
    public static void RefreshCurrentPage(this NavigationManager navigationManager)
    {
        navigationManager.NavigateTo(navigationManager.GetUriPath(), forceLoad: false, replace: true);
    }
}
