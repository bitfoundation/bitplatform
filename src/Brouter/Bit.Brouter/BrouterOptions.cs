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
    /// Whether the scroll position of each page is remembered and restored when the user navigates
    /// <em>Back</em> or <em>Forward</em> (a history pop), mirroring what native browsers and real SPA
    /// routers (React Router's <c>ScrollRestoration</c>, Vue Router's <c>scrollBehavior</c>) do: returning
    /// to a page lands the user where they left it instead of at the top.
    /// <para>
    /// This composes with the other scroll options rather than replacing them. A <em>new</em>
    /// (push/replace) navigation still uses <see cref="ScrollBehavior"/> (e.g. scroll to top); only a
    /// Back/Forward navigation to a previously-visited URL restores its saved position. Precedence per
    /// navigation: a resolved URL fragment (see <see cref="ScrollToFragment"/>) wins; then, on a
    /// Back/Forward with a remembered position, that position is restored; otherwise
    /// <see cref="ScrollBehavior"/> applies.
    /// </para>
    /// <para>
    /// Positions are keyed by absolute URL. By default they are kept in memory for the lifetime of the
    /// page (they do not survive a full reload); set <see cref="ScrollPositionStorage"/> to persist them
    /// in <c>sessionStorage</c>/<c>localStorage</c> so they survive reloads. Enabling this sets
    /// <c>history.scrollRestoration = "manual"</c> so the browser's own restoration doesn't fight the
    /// router's; it is left untouched when disabled. Defaults to <c>false</c>.
    /// </para>
    /// </summary>
    public bool RestoreScrollPosition { get; set; } = false;

    /// <summary>
    /// Where saved scroll positions are stored when <see cref="RestoreScrollPosition"/> is enabled.
    /// Defaults to <see cref="BrouterScrollPositionStorage.Memory"/> (in-memory only, lost on reload).
    /// Use <see cref="BrouterScrollPositionStorage.SessionStorage"/> (recommended) or
    /// <see cref="BrouterScrollPositionStorage.LocalStorage"/> to persist positions so a reload returns
    /// the user to where they left off. Has no effect unless <see cref="RestoreScrollPosition"/> is
    /// enabled. If the chosen web storage is unavailable (private mode, disabled, quota exceeded),
    /// restoration degrades gracefully to in-memory for the session.
    /// </summary>
    public BrouterScrollPositionStorage ScrollPositionStorage { get; set; } = BrouterScrollPositionStorage.Memory;

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
