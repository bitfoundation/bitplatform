namespace Bit.Websites.Platform.Client.Services;

/// <summary>
/// Owns the docs nav menu toggle and the URL-derived route flags, so Header (which renders the docs
/// hamburger trigger) and MainLayout (which mounts the NavMenu it toggles) can never disagree about
/// which routes count as docs routes.
/// </summary>
public class NavMenuService
{
    public event Func<Task>? OnToggleMenu;

    /// <summary>
    /// No-ops when NavMenu is not mounted (Header renders the trigger on docs routes before
    /// NavMenu subscribes), and awaits the handler so its exceptions surface instead of being lost.
    /// </summary>
    public Task ToggleMenu()
    {
        return OnToggleMenu?.Invoke() ?? Task.CompletedTask;
    }


    /// <summary>Current base-relative path, without query, fragment or trailing slash, with a leading slash.</summary>
    public string CurrentUrl { get; private set; } = "/";

    public bool IsTemplateDocRoute { get; private set; }
    public bool IsToolingsDocRoute { get; private set; }

    /// <summary>
    /// Docs routes get the docs nav menu (hamburger).
    /// </summary>
    public bool IsDocsRoute => IsTemplateDocRoute || IsToolingsDocRoute;

    public bool IsProductsRoute => IsDocsRoute || IsCurrentUrl(Urls.Products);

    /// <summary>
    /// Whether <paramref name="url"/> is the current page. Case-insensitive, like the Blazor router,
    /// which serves /Products the same page as /products.
    /// </summary>
    public bool IsCurrentUrl(string url) => string.Equals(CurrentUrl, url, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Idempotent; called by every component that reacts to LocationChanged, whichever fires first.
    /// </summary>
    public void UpdateRouteFlags(string currentUrl)
    {
        var fragmentIndex = currentUrl.IndexOfAny(['?', '#']);
        if (fragmentIndex >= 0)
        {
            currentUrl = currentUrl[..fragmentIndex];
        }

        // The router ignores a trailing slash, so /products/ renders the /products page and has to match it too.
        currentUrl = currentUrl.TrimEnd('/');

        CurrentUrl = currentUrl.Length == 0 ? "/" : currentUrl;

        // Case-insensitive like IsCurrentUrl, since the router serves /Templates the same page as /templates.
        IsTemplateDocRoute = currentUrl.Contains("/templates", StringComparison.OrdinalIgnoreCase) || currentUrl.Contains("/boilerplate", StringComparison.OrdinalIgnoreCase);
        IsToolingsDocRoute = currentUrl.Contains("/toolings", StringComparison.OrdinalIgnoreCase);
    }
}
