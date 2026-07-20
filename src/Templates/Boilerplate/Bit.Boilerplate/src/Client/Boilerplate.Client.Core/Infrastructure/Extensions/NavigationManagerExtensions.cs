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

        /// <summary>
        /// This would re-render the current page.
        /// Note that <see cref="NavigationManager.Refresh(bool)"/> might either decide to do nothing at all of refresh the entire app dependeing on the situation
        /// </summary>
        public void RefreshCurrentPage()
        {
            navigationManager.NavigateTo(navigationManager.GetUriPath(), forceLoad: true, replace: true);
        }
    }
}
