namespace Microsoft.AspNetCore.Components;

public static partial class NavigationManagerExtensions
{
    extension(NavigationManager navigationManager)
    {
        public string GetUriWithoutQueryParameter(string key)
        {
            return new Uri(navigationManager.Uri).GetUrlWithoutQueryParameter(key);
        }

        public string GetUriPath()
        {
            return new Uri(navigationManager.Uri).GetPath();
        }

        public string GetRelativePath()
        {
            return navigationManager.ToBaseRelativePath(navigationManager.Uri);
        }
    }
}
