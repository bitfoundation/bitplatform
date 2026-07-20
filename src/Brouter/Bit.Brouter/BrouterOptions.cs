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

    /// <summary>
    /// Default freshness window for loader results when a <see cref="Broute"/> doesn't set its own
    /// <see cref="Broute.StaleTime"/>. Null (the default) means loaders don't cache at all - every
    /// navigation re-runs them, exactly the pre-caching behavior. See <see cref="Broute.StaleTime"/>
    /// for the stale-while-revalidate semantics.
    /// </summary>
    public TimeSpan? DefaultLoaderStaleTime { get; set; }

    /// <summary>
    /// How long a cached loader result may live at all. Entries older than this are dropped on
    /// lookup regardless of staleness handling, bounding how outdated a stale-while-revalidate
    /// render can ever be. Defaults to 30 minutes (TanStack Router's <c>gcTime</c> default).
    /// </summary>
    public TimeSpan LoaderCacheGcTime { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Upper bound on cached loader results; the oldest-written entries are evicted first.
    /// Defaults to 50 (mirrors the scroll-position store's cap).
    /// </summary>
    public int MaxLoaderCacheEntries { get; set; } = 50;

    /// <summary>
    /// How a stale (but not yet garbage-collected) cached loader result is served.
    /// <see cref="BrouterStaleReloadMode.Background"/> (default) renders the cached data immediately
    /// and refreshes it in the background - classic stale-while-revalidate;
    /// <see cref="BrouterStaleReloadMode.Blocking"/> treats stale as a miss and waits for the loader.
    /// </summary>
    public BrouterStaleReloadMode StaleReloadMode { get; set; } = BrouterStaleReloadMode.Background;

    /// <summary>
    /// Default preload behavior for every <see cref="BrouterLink"/> that doesn't set its own
    /// <see cref="BrouterLink.Preload"/>. Defaults to <see cref="BrouterLinkPreload.None"/>.
    /// </summary>
    public BrouterLinkPreload DefaultLinkPreload { get; set; } = BrouterLinkPreload.None;

    /// <summary>
    /// Default retained-instance budget for <see cref="Broute.KeepAlive"/> routes that don't set their
    /// own <see cref="Broute.KeepAliveMax"/>. At the default of 1 a keep-alive route keeps a single
    /// live instance that re-binds when its parameter values change; a value above 1 keeps up to that
    /// many instances per route, keyed by the route's matched parameter values and evicted
    /// least-recently-used - so <c>/item/1</c> and <c>/item/2</c> each resume their own exact state.
    /// Values below 1 are treated as 1. See <see cref="Broute.KeepAliveMax"/> for the full semantics.
    /// </summary>
    public int DefaultKeepAliveMax { get; set; } = 1;

    /// <summary>
    /// Debounce for <see cref="BrouterLinkPreload.Intent"/> preloading: the pointer must rest on the
    /// link this long before the preload fires, so merely brushing past links doesn't fetch.
    /// Defaults to 50 ms (TanStack Router's <c>defaultPreloadDelay</c>).
    /// </summary>
    public TimeSpan PreloadDelay { get; set; } = TimeSpan.FromMilliseconds(50);

    /// <summary>
    /// Freshness window for cache entries produced by link preloading (<see cref="BrouterLink.Preload"/> /
    /// <see cref="IBrouter.PreloadAsync"/>) on routes that don't otherwise cache (no
    /// <see cref="Broute.StaleTime"/>). A preloaded result younger than this is used instead of
    /// re-running the loader when the user actually navigates. Defaults to 30 seconds (TanStack
    /// Router's <c>preloadStaleTime</c> default).
    /// </summary>
    public TimeSpan PreloadStaleTime { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Optional <see cref="System.Text.Json.Serialization.Metadata.IJsonTypeInfoResolver"/> used to
    /// serialize loader results for <see cref="PersistLoaderState"/>. Supply a source-generated
    /// <c>JsonSerializerContext</c> covering your loader data types to make the prerender state
    /// bridge fully trimming/AOT-safe; when <c>null</c> (the default) reflection-based
    /// <c>System.Text.Json</c> is used. Types the resolver can't handle degrade gracefully: their
    /// results simply aren't persisted, so the loader re-runs on the interactive pass.
    /// </summary>
    public System.Text.Json.Serialization.Metadata.IJsonTypeInfoResolver? LoaderStateTypeInfoResolver { get; set; }

    /// <summary>
    /// When <c>true</c>, each successful navigation's re-render is wrapped in the browser's View
    /// Transitions API (<c>document.startViewTransition</c>), giving an animated cross-fade between
    /// the outgoing and incoming pages by default and enabling per-element morph animations via the
    /// standard <c>view-transition-name</c> CSS property - no Blazor-specific animation code needed.
    /// Mirrors Angular's <c>withViewTransitions</c> and React Router's <c>viewTransition</c>.
    /// Gracefully inert on browsers without the API, during prerender, and in non-browser hosts.
    /// Defaults to <c>false</c>. See also <see cref="ViewTransitionDefaultAnimations"/>.
    /// </summary>
    public bool ViewTransitions { get; set; } = false;

    /// <summary>
    /// When <c>true</c> (the default) and <see cref="ViewTransitions"/> is enabled, Brouter injects a
    /// small stylesheet of polished, direction-aware default animations so navigations look good out
    /// of the box: a forward navigation (push) glides the new page in, Back/Forward (pop) mirrors the
    /// motion so going back feels like going back, a replace does a quick in-place fade, and
    /// shared-element morphs (<c>view-transition-name</c>) get a springy glide.
    /// <c>prefers-reduced-motion</c> (which OS accessibility settings propagate to the browser, e.g.
    /// Windows "Animation effects" off) swaps the slides for gentle opacity-only crossfades and
    /// disables morph motion, so navigation keeps visual feedback without movement.
    /// <para>
    /// The injected rules live in the CSS layer <c>bit-brouter</c>, so any unlayered
    /// <c>::view-transition-*</c> rules in application CSS override them automatically - customize
    /// freely without fighting specificity, or set this to <c>false</c> to opt out entirely (the
    /// browser's plain cross-fade / your own CSS only). The current navigation's direction is exposed
    /// as <c>data-brouter-nav="push|replace|pop"</c> on the root element for custom CSS to key off.
    /// </para>
    /// </summary>
    public bool ViewTransitionDefaultAnimations { get; set; } = true;

    /// <summary>
    /// Whether the built-in default animations honor the user's <c>prefers-reduced-motion</c>
    /// preference (the default, <c>true</c>): motion is replaced by gentle opacity-only crossfades
    /// and shared-element morphs are stilled. Set to <c>false</c> to run the full animations
    /// regardless of the preference.
    /// </summary>
    /// <remarks>
    /// Think before disabling: <c>reduce</c> is a genuine accessibility signal for motion-sensitive
    /// users. The legitimate reason to bypass it is that operating systems also report it for
    /// non-accessibility reasons - e.g. Windows "Animation effects" is commonly switched off on
    /// VMs, remote-desktop sessions and performance-tuned machines, making every browser there
    /// report <c>reduce</c> even though no user asked for less motion. Only affects Brouter's
    /// injected defaults (<see cref="ViewTransitionDefaultAnimations"/>); your own
    /// <c>::view-transition-*</c> CSS is never touched.
    /// </remarks>
    public bool ViewTransitionRespectReducedMotion { get; set; } = true;

    /// <summary>
    /// When <c>true</c>, leaving the SPA entirely - closing the tab, a full page reload, or following
    /// a link to another document - triggers the browser's generic "leave site?" confirmation dialog
    /// (a <c>beforeunload</c> handler), armed once the router becomes interactive. Complements
    /// <see cref="Broute.LeaveGuard"/>, which covers in-SPA navigations with full custom logic.
    /// Browser rules apply: the dialog only appears after the user has interacted with the page, and
    /// its text cannot be customized. For dynamic control (e.g. only while a form is dirty), leave
    /// this <c>false</c> and toggle <see cref="IBrouter.SetConfirmExternalNavigationAsync"/> at
    /// runtime instead. Defaults to <c>false</c>.
    /// </summary>
    public bool ConfirmExternalNavigation { get; set; } = false;

    /// <summary>
    /// Custom route parameter constraints scoped to this DI container. Register at startup so templates
    /// can use them, e.g. <c>AddBitBrouterServices(o =&gt; o.Constraints.Register("slug", new SlugConstraint()))</c>,
    /// then <c>{post:slug}</c> in a route. Constraints registered here are visible only to the app/service
    /// provider that owns these options, so separate apps in one process (and parallel test classes) stay
    /// isolated. Built-in constraints (<c>int</c>, <c>bool</c>, <c>guid</c>, <c>long</c>, <c>float</c>,
    /// <c>double</c>, <c>decimal</c>, <c>datetime</c>) are always available and need no registration.
    /// </summary>
    public BrouterConstraintRegistry Constraints { get; } = new();
}
