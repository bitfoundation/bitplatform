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


    /// <summary>Current base-relative path, without query or fragment, with a leading slash.</summary>
    public string CurrentUrl { get; private set; } = "/";

    public bool IsTemplateDocRoute { get; private set; }
    public bool IsBesqlDocRoute { get; private set; }
    public bool IsLcncDocRoute { get; private set; }

    /// <summary>
    /// Docs routes get the docs nav menu (hamburger); Lcnc has no docs yet, so it is excluded here,
    /// but it still counts as a product route for highlighting the Products menu trigger.
    /// </summary>
    public bool IsDocsRoute => IsTemplateDocRoute || IsBesqlDocRoute;

    public bool IsProductsRoute => IsDocsRoute || IsLcncDocRoute;

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

        CurrentUrl = currentUrl;

        IsTemplateDocRoute = currentUrl.Contains("/templates") || currentUrl.Contains("/boilerplate");
        IsBesqlDocRoute = currentUrl.Contains("/besql");
        IsLcncDocRoute = currentUrl.Contains("/lowcode-nocode");
    }
}
