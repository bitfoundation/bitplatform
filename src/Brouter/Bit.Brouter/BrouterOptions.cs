namespace Bit.Brouter;

/// <summary>
/// Global options for Bit.Brouter. Register via <c>builder.Services.AddBitBrouterServices(o =&gt; ...)</c>.
/// </summary>
public sealed class BrouterOptions
{
    /// <summary>
    /// Whether literal segment matching is case sensitive. Defaults to <c>false</c>
    /// to match React Router and Vue Router conventions (URLs are case-insensitive).
    /// </summary>
    public bool CaseSensitive { get; set; } = false;

    /// <summary>
    /// Whether <c>/users</c> and <c>/users/</c> are treated as the same path.
    /// Defaults to <c>true</c>; trailing slashes are ignored.
    /// </summary>
    public bool IgnoreTrailingSlash { get; set; } = true;

    /// <summary>
    /// Whether to scroll to the top of the page after a successful navigation.
    /// Defaults to <see cref="BrouterScrollMode.None"/>.
    /// </summary>
    public BrouterScrollMode ScrollBehavior { get; set; } = BrouterScrollMode.None;

    /// <summary>
    /// Whether a URL fragment scrolls its target element into view after a successful navigation
    /// (e.g. navigating to <c>/docs#install</c> scrolls the <c>#install</c> element into view and
    /// moves focus to it). When a fragment target is found it takes precedence over
    /// <see cref="ScrollBehavior"/>. Only acts when the destination URL carries a fragment.
    /// Defaults to <c>true</c>.
    /// </summary>
    public bool ScrollToFragment { get; set; } = true;

    /// <summary>
    /// A CSS selector for the element to move focus to after each successful navigation, mirroring
    /// Blazor's <c>FocusOnNavigate</c>. Moving focus lets assistive technologies announce the new page
    /// instead of leaving focus on the activated link, which is a WCAG-relevant concern for an SPA
    /// router. A fragment target (see <see cref="ScrollToFragment"/>) takes precedence when present.
    /// If the selector matches an element that isn't natively focusable, a <c>tabindex="-1"</c> is
    /// added so it can receive programmatic focus without entering the sequential Tab order.
    /// Defaults to <c>null</c> (no focus change). Common values are <c>"h1"</c> or a main-content
    /// landmark selector such as <c>"main"</c>.
    /// </summary>
    public string? FocusOnNavigateSelector { get; set; }

    /// <summary>
    /// Whether route <c>Loader</c> results are persisted across the SSR/prerender -&gt; interactive
    /// transition using <see cref="Microsoft.AspNetCore.Components.PersistentComponentState"/>, so a
    /// loader that ran during prerender is not run again (double-fetched) when the component becomes
    /// interactive. Defaults to <c>false</c>.
    /// </summary>
    /// <remarks>
    /// Enabling this serializes loader results with reflection-based <c>System.Text.Json</c>, which is
    /// not trimming/AOT-safe for arbitrary types. Only enable it when your loader data types are
    /// JSON-serializable and preserved under trimming. Restoration degrades gracefully: if a value can't
    /// be rehydrated the loader simply runs again, so a serialization mismatch never breaks navigation.
    /// </remarks>
    public bool PersistLoaderState { get; set; } = false;
}
