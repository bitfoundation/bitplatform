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
}
