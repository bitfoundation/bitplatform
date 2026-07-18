# bit Brouter
A modern, declarative, nestable router for Blazor with async guards, data loaders,
named routes, programmatic navigation, query string binding, and global navigation hooks.

API design notes informed by React Router v6, Vue Router 4, Angular Router,
SvelteKit, TanStack Router and ASP.NET Core's route matcher.

---

## Install

```bash
dotnet add package Bit.Brouter
```

```csharp
using Bit.Brouter;

builder.Services.AddBitBrouterServices(o =>
{
    o.CaseSensitive = false;              // default
    o.IgnoreTrailingSlash = true;         // default
    o.ScrollBehavior = BrouterScrollMode.ToTop;
    o.ScrollToFragment = true;            // default: /docs#install scrolls #install into view
    o.FocusOnNavigateSelector = "h1";     // move focus after navigation (accessibility)
});
```

A runnable tour of every feature below lives in [`InteralDemos`](InteralDemos/) - run the
[Server](InteralDemos/Server/) project (`dotnet run`) and click through the home page cards; the
same shared demo pages also run under [WASM](InteralDemos/Wasm/) and [Auto](InteralDemos/Auto/)
render modes.

## Quick start

```razor
<Brouter NotFoundUrl="404">
    <Broute Path="/" RedirectTo="/home" />

    <Broute Name="home" Path="/home">
        <Content><HomePage /></Content>
    </Broute>

    <Broute Name="user" Path="/users/{id:int}">
        <Content><UserPage /></Content>
    </Broute>

    <Broute Path="/files/{**path}" Component="@typeof(FilesPage)" />

    <Broute Path="404">
        <Content>
            <h1 class="text-danger">404</h1>
            <p>Sorry, there's nothing at this address.</p>
        </Content>
    </Broute>
</Brouter>
```

## Features

- Declarative routes with literal segments, parameter segments, constraints and wildcards
- Built-in constraints: `int`, `bool`, `guid`, `long`, `float`, `double`, `decimal`, `datetime`
- Multiple constraints per parameter: `{id:int:long}`
- Wildcards: `*` (single segment), `**` (catch-all)
- **Optional parameters**: `{id?}` - must be trailing
- **Catch-all parameter binding**: `{**path}` exposes the remainder
- Custom constraints, scoped per DI container via `o.Constraints.Register("slug", new MyConstraint())`
- Specificity-based matching (literals beat constrained beat unconstrained beat wildcards)
- **Ambiguous templates are rejected**: registering two routes that match exactly the same URLs (e.g. a duplicated `@page`, or `/users/{id}` next to `/users/{userId}`) throws instead of silently picking one, mirroring the built-in router's `AmbiguousMatchException`. A hand-declared route may still shadow a discovered `@page` with the same template (see [`@page` discovery](#attribute-route--page-discovery))
- Nested routes via `Broute` children or `BrouterOutlet`
- Async `Guard` with cancel/redirect via `BrouterNavigationContext`
- **Per-route `LeaveGuard`** (Angular `CanDeactivate` / Vue `beforeRouteLeave` style): runs preventively, leaf → root, only for routes the navigation actually deactivates - real per-route "unsaved changes" prompts
- **Component-level navigation lock**: the routed component itself vetoes navigation from `OnDeactivating` / `OnRenavigating` (React Router `useBlocker` / Vue `onBeforeRouteLeave`+`beforeRouteUpdate` style) - awaited, cancellable lock callbacks on the route lifecycle that can hold the navigation open for a **custom confirmation dialog** and cover the case `LeaveGuard` can't: parameter changes on the same route
- **External-navigation confirmation**: `o.ConfirmExternalNavigation` (always-on) or `brouter.SetConfirmExternalNavigationAsync(...)` (runtime toggle) arms the browser's `beforeunload` dialog for tab-close/reload/external links
- **Per-route error boundaries**: `ErrorContent` on a `Broute` (nearest boundary wins, bubbling leaf → root) or on the `Brouter` (root fallback), with typed `BrouterErrorContext` carrying the exception and a `RetryAsync()`
- **Awaitable navigation**: `NavigateAsync` resolves with how the navigation actually ended - `Succeeded` / `Cancelled` / `Redirected` / `NotFound` / `Failed` / `Superseded` (Vue Router navigation-failures style)
- **History entry state**: attach a state string to a navigation (`Navigate(url, historyState: ...)`, `<BrouterLink HistoryState>`), read it back on `BrouterLocation.HistoryState` - survives Back/Forward
- **View Transitions API**: `o.ViewTransitions = true` wraps each navigation's re-render in `document.startViewTransition` with **beautiful direction-aware default animations** out of the box (push glides forward, Back mirrors it, replace fades; overridable via plain CSS thanks to `@layer`, opt-out via `o.ViewTransitionDefaultAnimations`); `view-transition-name` morphs just work; inert on unsupported browsers
- **.NET 10 `NotFound()` interop**: `NavigationManager.NotFound()` routes through Brouter's not-found handling, and an unmatched URL during static SSR sets a real HTTP 404
- **Revalidation**: `brouter.RevalidateAsync()` re-runs the matched chain's loaders after a mutation - no guards, no URL change, current content stays while fresh data loads
- **Loader caching (stale-while-revalidate)**: per-route `StaleTime` (or a global default) caches loader results per URL - fresh hits skip the loader (instant Back/Forward), stale hits render immediately and refresh in the background (TanStack Router style); `GcTime`, entry cap, `Blocking` mode and `ClearLoaderCache()` included
- **Link preloading**: `<BrouterLink Preload="Intent">` (hover/touch/focus with debounce), `Viewport` (IntersectionObserver) or `Render` warm the loader cache before the click; programmatic `brouter.PreloadAsync(url)`; guards never run on preloads (`ctx.IsPreload`)
- **Deferred (streamed) data**: return unawaited `Task<T>`s inside the loader result and render them with `<BrouterAwait>` (`Pending`/`Resolved`/`Error`) - critical data blocks navigation, slow data streams in (React Router `<Await>` style)
- **AOT-safe prerender persistence**: plug a source-generated `JsonSerializerContext` into `o.LoaderStateTypeInfoResolver` to make `PersistLoaderState` trimming/AOT-safe
- **Pathless group routes**: `<Broute Group>` attaches a shared guard/loader/layout/error boundary to its children without adding URL segments (SvelteKit `(group)` / TanStack pathless-layout style)
- **Lazy route loading**: `Brouter OnNavigateAsync` loads route assemblies on demand (e.g. `LazyAssemblyLoader` on WASM) and the *same* navigation matches the freshly-loaded page
- **Functional query updates**: `brouter.NavigateWithQuery(q => q.Set("page", 2))` updates one parameter and preserves the rest (typed values, multi-value support, replace-by-default)
- **Source-generated typed routes** (`Bit.Brouter.Generators`): compile-time-safe URL builders generated from your `@page` directives and `<Broute>` declarations - `BrouterRoutes.Counter(1234)` instead of `"/counter/1234"`, with constraint-typed parameters and a `Names` class for named routes
- **Named outlets**: `<BrouterOutlet Name="sidebar">` + `<BrouterView Name="sidebar">` let one route drive multiple regions of its parent layout (Vue named views / Angular secondary outlets style)
- **Keep-alive routes**: `<Broute KeepAlive>` keeps the rendered component mounted (hidden) when navigated away, so returning restores its exact state instead of recreating it (Vue `KeepAlive` / Angular `RouteReuseStrategy` style); `KeepAliveMax="N"` upgrades a parameterized route to per-parameter caching (`/item/1` and `/item/2` each resume their own state, LRU-evicted over the budget), and `brouter.ClearKeepAlive()` evicts retained pages on demand
- **Route lifecycle**: every routed component (keep-alive or not, at any depth) can receive `OnActivated` / `OnDeactivated` / `OnRenavigated` callbacks - implement `IBrouterRoute` on a page (auto-discovered) or derive from `BrouterRouteBase` (the Ionic `ionViewWillEnter` / Vue `onActivated` idea, with async support and a Disposing-vs-Hidden reason) - plus the pre-commit `OnDeactivating` / `OnRenavigating` lock callbacks above
- **Async data `Loader`** exposed via the typed cascading `BrouterRouteData` wrapper (`Get<T>` / `TryGet<T>` / `GetOrDefault<T>`) - sequential root → leaf by default, with opt-in **`ParallelLoaders`** for independent loaders
- Redirects with `RedirectTo`
- Component or `Content` (typed render fragment) rendering
- **Pending navigation UI**: a `Navigating` fragment shown while a navigation awaits slow loaders - revealed lazily so loader-less (or cache-hit) navigations never flash it (mirrors the built-in `Router.Navigating`)
- Router-level hooks: `OnMatch` (a route matched) and `OnNotFound` (nothing matched), alongside the global `IBrouter` events
- `NotFoundUrl` redirect or inline `NotFound` content
- **Type-safe `BrouterRouteParameters`** with `TryGet<T>` / `Get<T>` / `GetOrDefault<T>`
- **Auto-binding** to plain `[Parameter]` properties by name (Blazor-style) and `[SupplyParameterFromQuery]` for query values, plus two opt-in Brouter attributes that extend the built-in tools: `[BrouterParameter(Name = ...)]` remaps a route parameter to a differently-named property, and `[BrouterQuery]` binds query values of types the framework supplier can't parse (e.g. enums)
- **`<BrouterLink>`** component with active-class and `aria-current` (NavLink-style)
- **Programmatic navigation** via `IBrouter`: `Navigate`, `Back`, `NavigateToName`, `ResolveUrl`
- **Relative navigation**: `./edit` and `../sibling` resolve against the current location (segment math, React Router style) in `Navigate`, guard redirects and `<BrouterLink>`
- **Global hooks**: `OnNavigating`, `OnNavigated`, `OnError` (Vue Router style)
- **Navigation type** on `BrouterNavigationContext.NavigationType`: distinguishes `Push` / `Replace` / `Pop` (Back/Forward) for scroll-restoration and analytics logic
- **Preventive guards** (via `RegisterLocationChangingHandler`): a cancel/redirect stops the URL from ever changing - no address-bar flicker, no corrupted history/back button, and real "unsaved changes" prompts are possible
- In-flight loader cancellation when navigation is superseded
- **Attribute-route / `@page` discovery**: scan `AppAssembly` / `AdditionalAssemblies` for `[Route]`-annotated components so routes live colocated with their pages (Razor class libraries and lazy-loaded assemblies included)
- **Drop-in built-in-Router migration via `Found`**: a `RenderFragment<RouteData>` that receives a real framework `RouteData` for every matched page, so the built-in `RouteView`, `AuthorizeRouteView` (`[Authorize]`, `Authorizing`/`NotAuthorized` UI), per-page `@layout` resolution and `FocusOnNavigate` all keep working unchanged - the built-in Router's `<Found Context="routeData">` block ports over as-is
- **Zero-template authorization**: set `NotAuthorized` / `Authorizing` / `DefaultLayout` / `Resource` directly on `<Brouter>` and `[Authorize]` pages just work - Brouter composes the framework's own `AuthorizeRouteView`/`RouteView` internally, so authorization correctness stays Microsoft's code; native rendering fails closed on `[Authorize]` components it can't enforce
- **Cascaded `RouteData` observer channel**: the committed navigation's framework `RouteData` (or null on not-found) is cascaded unnamed-by-type, so publishers/breadcrumbs/telemetry anywhere under the router can take `[CascadingParameter] RouteData?` with no template plumbing
- **Prerender state bridging**: loader results captured during prerender are restored on the interactive pass via `PersistentComponentState`, so loaders don't double-fetch (opt-in)
- Query string and hash exposed via `BrouterLocation`
- Configurable case sensitivity and trailing-slash handling
- **Scroll management**: optional scroll-to-top, fragment scrolling (`/docs#install` lands on `#install`), and scroll-position restoration on Back/Forward
- **Focus management** for accessibility: move focus to a selector after navigation so screen readers announce the new page (mirrors Blazor's `FocusOnNavigate`)
- Multi-target: net8.0, net9.0, net10.0

## Type-safe parameters

```razor
<Broute Path="/users/{id:int}">
    <Content Context="p">
        <p>User: @p.Get<int>("id")</p>
    </Content>
</Broute>
```

```razor
@code {
    [CascadingParameter(Name = "RouteParameters")] BrouterRouteParameters? Params { get; set; }

    protected override void OnInitialized()
    {
        if (Params!.TryGet<int>("id", out var id)) { /* ... */ }
    }
}
```

## Auto-bound parameters

Route parameters bind to `[Parameter]` properties by name, exactly like the built-in Blazor
`Router` - no extra attribute needed:

```razor
<Broute Path="/profile/{username?}" Component="@typeof(ProfilePage)" />
```

```razor
@code {
    [Parameter] public string? Username { get; set; }
}
```

When the property name and the route parameter name differ, remap with
`[BrouterParameter(Name = ...)]` (a feature the built-in Router doesn't have). It also serves as
the escape hatch when an unrelated `[Parameter]` property collides with a route parameter name:

```razor
<Broute Path="/users/{id:int}" Component="@typeof(UserPage)" />
```

```razor
@code {
    [Parameter, BrouterParameter(Name = "id")] public int UserId { get; set; }
}
```

Only `[Parameter]` properties whose names match a parameter in the route's template (or that carry
`[BrouterParameter]`) are driven by the router; other component parameters are left untouched.

Query values bind the standard Blazor way, via `[Parameter, SupplyParameterFromQuery]`. When the
property's type falls outside what the framework's query supplier can parse - enums, for example -
switch that property to Brouter's opt-in `[BrouterQuery]`: the framework supplier ignores it (it only
reacts to its own attribute), and Brouter converts the value itself (any `Convert.ChangeType`-compatible
scalar, `Guid`, enums, nullables, and `string[]` for multi-value keys):

```razor
@code {
    [Parameter, SupplyParameterFromQuery] public string? Tab { get; set; }   // ?tab=..., framework-supported type
    [Parameter, BrouterQuery] public DayOfWeek? Day { get; set; }            // ?day=tuesday - enums need [BrouterQuery]
}
```

## Async guards

```razor
<Broute Path="/admin" Guard="@CheckAdmin">
    <Content><AdminPage /></Content>
</Broute>

@code {
    [Inject] AuthService Auth { get; set; } = default!;

    private async ValueTask CheckAdmin(BrouterNavigationContext ctx)
    {
        if (await Auth.IsAdminAsync(ctx.CancellationToken) is false)
            ctx.Redirect("/login?return=" + Uri.EscapeDataString(ctx.To.Path));
    }
}
```

Guards (and `OnNavigating`) run inside a `RegisterLocationChangingHandler`, so `ctx.Cancel()` /
`ctx.Redirect(...)` are **preventive**: the target URL is never committed to history when the
navigation is blocked. There is no address-bar flicker and no torn back/forward stack, and you can
implement a genuine "you have unsaved changes" prompt by cancelling from a guard or `OnNavigating`.

A redirect to the URL the navigation is already heading to is treated as "continue", so guards like
"always send anonymous users to `/login`" can't create redirect loops.

## Leave guards (unsaved changes)

`LeaveGuard` is the per-route counterpart for *leaving*: it runs - preventively, before
`OnNavigating` and any enter guards - when a navigation would deactivate the route (it is part of
the currently rendered chain but not the new one), leaf → root. A navigation that keeps the route
matched (a parameter change, or moving between its children) does not fire it.

```razor
<Broute Path="/editor" LeaveGuard="@ConfirmLeave">
    <Content><EditorPage /></Content>
</Broute>

@code {
    private ValueTask ConfirmLeave(BrouterNavigationContext ctx)
    {
        if (_isDirty) ctx.Cancel();   // URL never changes; no flicker, no broken Back
        return ValueTask.CompletedTask;
    }
}
```

Leaving the SPA entirely (tab close, reload, external link) can't run C# - for that, arm the
browser's generic confirmation dialog: `o.ConfirmExternalNavigation = true` at startup, or toggle it
at runtime with `brouter.SetConfirmExternalNavigationAsync(isDirty)` from a dirty-form tracker.
(Browser rules: the dialog needs prior user interaction and its text is not customizable.)

`LeaveGuard` lives on the route declaration; when the veto belongs to the *component* - it knows
whether its form is dirty - use the [component-level navigation lock](#navigation-lock-blocking-from-the-component-itself)
instead, which also covers parameter changes on the same route (a case `LeaveGuard` deliberately
never fires for).

## Error boundaries

When a commit-phase failure happens (typically a `Loader` throwing), the **nearest `ErrorContent`**
- walking from the failed route up through its ancestors, then to the `Brouter` itself - renders in
place of the routed content. Layouts above the boundary keep rendering; the global `OnError` hook
still fires either way.

```razor
<Brouter>
    <Routes>
        <Broute Path="/users/{id:int}" Loader="@LoadUser">
            <Content Context="p"><UserDetails /></Content>
            <ErrorContent Context="err">
                <p>Couldn't load this user: @err.Exception.Message</p>
                <button @onclick="@(() => err.RetryAsync())">Try again</button>
            </ErrorContent>
        </Broute>
    </Routes>
    <ErrorContent Context="err">
        <h1>Something went wrong</h1>
        <button @onclick="@(() => err.RetryAsync())">Retry</button>
    </ErrorContent>
</Brouter>
```

`RetryAsync()` re-runs the full navigation (guards included) for the current URL; success replaces
the error UI with the routed content. With no boundary declared anywhere, behavior is unchanged:
the previous page stays visible and `OnError` observes the failure.

## Data loader

```razor
<Broute Path="/users/{id:int}" Loader="@LoadUser">
    <Content Context="p">
        <UserDetails />  @* reads the cascading BrouterRouteData *@
    </Content>
</Broute>

@code {
    [Inject] HttpClient Http { get; set; } = default!;

    private async ValueTask<object?> LoadUser(BrouterNavigationContext ctx)
        => await Http.GetFromJsonAsync<User>(
               $"/api/users/{ctx.Parameters["id"]}",
               ctx.CancellationToken);
}
```

The loader result is cascaded as a typed `BrouterRouteData` wrapper (route `Meta` likewise as
`BrouterRouteMeta`), so consumers get compile-time-safe access instead of casting an `object?`:

```razor
@* UserDetails.razor *@
<h1>@(Data?.Get<User>().Name)</h1>

@code {
    // The cascade is unnamed and matched by the unique wrapper type - no Name string involved.
    [CascadingParameter] public BrouterRouteData? Data { get; set; }
}
```

`Get<T>()` throws a descriptive exception when the value is absent or of another type;
`TryGet<T>(out var value)` and `GetOrDefault<T>()` are the non-throwing variants, and the raw
payload stays available via `Data.Value`.

### Loader ordering in nested routes

When a matched route has ancestors with their own loaders, the loaders run **sequentially,
root → leaf** by default: a parent's loader completes before its child's starts, mirroring guard
order. That lets a child loader depend on work its parent's loader already did (e.g. state stashed
in a scoped service), but it means the total wait is the *sum* of the chain's loader times.

If the chain's loaders are independent (the common case), opt into running them concurrently -
like React Router - with `ParallelLoaders`:

```razor
<Brouter ParallelLoaders="true">
    ...
</Brouter>
```

Results are still committed and errors still surfaced in root → leaf order, so render and failure
behavior are unchanged; only the awaiting overlaps, making the wait as long as the slowest loader
instead of all of them combined.

### Pending navigation UI

```razor
<Brouter>
    <Routes>
        <Broute Path="/reports" Loader="@LoadReports">...</Broute>
    </Routes>
    <Navigating>
        <div class="spinner">Loading…</div>
    </Navigating>
</Brouter>
```

While a navigation is awaiting its route loaders, the `Navigating` fragment renders in place of the
routed content - the counterpart of the built-in `Router.Navigating`. It is revealed *lazily*, only
once a loader is actually about to run: navigations with no loaders, cache hits, and
prerender-restored loads never flash it. Left unset, the previous page simply stays visible until
the new route is ready.

### Revalidation (refresh after a mutation)

```csharp
await Http.PostAsJsonAsync("/api/todos", newTodo);
await brouter.RevalidateAsync();   // re-runs the matched chain's loaders, re-renders fresh data
```

Not a navigation: the URL stays, guards and `OnNavigating`/`OnNavigated` don't run, and the current
content remains visible while loaders work. Loaders can branch on `ctx.IsRevalidation`. For data on
*other* pages, `brouter.ClearLoaderCache()` drops every cached loader result instead.

### Loader caching (stale-while-revalidate)

```razor
<Broute Path="/feed" Loader="@LoadFeed" StaleTime="@TimeSpan.FromMinutes(1)">...</Broute>
```

With a `StaleTime` (per-route, or `o.DefaultLoaderStaleTime` globally), loader results cache per
URL (path + query):

- **fresh** hit (younger than `StaleTime`) → the loader is skipped entirely - Back/Forward becomes instant;
- **stale** hit → by default (`o.StaleReloadMode = Background`) the cached data renders immediately
  and a background revalidation refreshes it (classic SWR); `Blocking` treats stale as a miss;
- entries die after `o.LoaderCacheGcTime` (30 min default) and the store is capped at
  `o.MaxLoaderCacheEntries` (50), oldest evicted first.

No `StaleTime` anywhere → no *loader* caching, exactly the previous behavior. (Preloading is the
one exception: a preloaded result stays reusable for `o.PreloadStaleTime` even on routes without a
`StaleTime` - see below.)

### Preloading

```razor
<BrouterLink Href="/users/42" Preload="BrouterLinkPreload.Intent">Saleh</BrouterLink>
```

`Intent` runs the destination's loaders into the cache on hover/touch/focus (debounced by
`o.PreloadDelay`, 50 ms); `Viewport` fires once when the link scrolls into view; `Render` fires
immediately; `o.DefaultLinkPreload` sets an app-wide default. Programmatic:
`await brouter.PreloadAsync("/users/42")`. Preloads run **loaders only** - no guards, no rendering -
and a preloaded result younger than `o.PreloadStaleTime` (30 s) is used by the real navigation even
on routes with no `StaleTime`. Keep preloaded loaders side-effect-free (`ctx.IsPreload` is set).

### Deferred (streamed) data

Let the critical part block navigation and stream the slow part in afterwards:

```csharp
private async ValueTask<object?> LoadPost(BrouterNavigationContext ctx)
{
    var post = await Http.GetFromJsonAsync<Post>($"/api/posts/{ctx.Parameters["id"]}", ctx.CancellationToken);
    var comments = Http.GetFromJsonAsync<Comment[]>($"/api/posts/{ctx.Parameters["id"]}/comments"); // NOT awaited
    return new PostData(post!, comments!);
}
```

```razor
<h1>@(Data!.Get<PostData>().Post.Title)</h1>

<BrouterAwait Task="@(Data!.Get<PostData>().Comments)">
    <Pending><p>Loading comments…</p></Pending>
    <Resolved Context="comments">@foreach (var c in comments) { <p>@c.Text</p> }</Resolved>
    <Error Context="ex"><p>Comments unavailable: @ex.Message</p></Error>
</BrouterAwait>
```

Loader results containing live tasks are skipped by `PersistLoaderState` (tasks aren't
serializable), so such loaders simply re-run on the interactive pass.

## Programmatic navigation

```razor
@inject IBrouter brouter

<button @onclick="GoHome">Home</button>
<button @onclick="GoToUser">User 42</button>
<button @onclick="brouter.Back">Back</button>

@code {
    void GoHome() => brouter.Navigate("/");

    void GoToUser() => brouter.NavigateToName(
        "user",
        new Dictionary<string, object?> { ["id"] = 42 });

    string UserUrl() => brouter.ResolveUrl(
        "user",
        new Dictionary<string, object?> { ["id"] = 42 });
}
```

### Awaitable navigation

`NavigateAsync` resolves with how the navigation actually concluded, mirroring Vue Router's
navigation failures - no more assuming a `Navigate` call landed:

```csharp
var outcome = await brouter.NavigateAsync("/admin");
switch (outcome.Status)
{
    case BrouterNavigationStatus.Succeeded:  /* committed + rendered */          break;
    case BrouterNavigationStatus.Cancelled:  /* a guard said no */               break;
    case BrouterNavigationStatus.Redirected: /* see outcome.RedirectedTo */      break;
    case BrouterNavigationStatus.NotFound:   /* no route matched */              break;
    case BrouterNavigationStatus.Failed:     /* see outcome.Exception */         break;
    case BrouterNavigationStatus.Superseded: /* a newer navigation overtook it */ break;
}
```

### History entry state

Attach application state to the destination's history entry and read it back after the navigation -
including when the user returns to the entry via Back/Forward (`history.state` semantics):

```csharp
brouter.Navigate("/results", historyState: "search=blazor;page=3");
// later, e.g. in a loader or OnNavigated:
var state = brouter.Location.HistoryState; // also on ctx.To.HistoryState
```

`<BrouterLink Href="/results" HistoryState="...">` does the same for link clicks (the link
intercepts unmodified left-clicks the way `Replace` links do, since an href-driven navigation
can't carry state). Serialize structured payloads (e.g. JSON) yourself.

### Relative navigation

Paths starting with `./` or `../` resolve against the **current location** using segment math
(React Router style, not URL directory semantics): from `/users/42`, `Navigate("./edit")` goes to
`/users/42/edit` and `Navigate("../7")` to `/users/7`. Extra `..` clamp at the root, and any query
or hash on the relative URL is preserved.

The same resolution applies in guard redirects - `ctx.Redirect("../login")` resolves against the
path being navigated **to**, so a guard on `/admin/secret` lands on `/admin/login` - and in
`<BrouterLink Href="../sibling">`, whose rendered `href` is the resolved absolute path and
re-resolves after every (matched) navigation.

Bare paths without a leading `.` (e.g. `Navigate("sibling")`) are untouched and keep their usual
base-relative meaning through `NavigationManager`.

## Navigation type (push / replace / pop)

`BrouterNavigationContext.NavigationType` tells guards, loaders and hooks how the current navigation
was initiated, so logic that treats a Back/Forward differently from a fresh navigation (scroll
restoration, analytics, "leave animation" direction) can branch on it. It is populated before guards
run and is available for the whole navigation.

```csharp
private ValueTask<object?> LoadFeed(BrouterNavigationContext ctx)
{
    if (ctx.NavigationType == BrouterNavigationType.Pop)
        return ValueTask.FromResult<object?>(_cachedFeed); // Back/Forward: reuse, don't refetch
    ...
}
```

- `Push` - a new history entry: an intercepted link click, `brouter.Navigate(...)` /
  `brouter.NavigateToName(...)` without `replace`, an internal redirect, and the initial page load.
- `Replace` - the current entry was replaced: `brouter.Navigate(url, replace: true)`, a
  `<BrouterLink Replace>` click, or the address-bar restore after a cancelled navigation.
- `Pop` - a history traversal: browser Back/Forward, or `brouter.Back()` / `brouter.Forward()`.

Detection relies on navigation going through Brouter's own primitives (links and `IBrouter`) -
`brouter.Back()` / `Forward()` stamp the traversal explicitly, so they always report `Pop`. Two
framework-level caveats: a raw `NavigationManager.NavigateTo` that bypasses `IBrouter` cannot be
classified reliably, and interactive Blazor reports **browser-button** Back/Forward as intercepted
navigations, so those may surface as `Push` to guards/hooks. The built-in view-transition
animations are unaffected - their direction comes from the browser's own `popstate` signal, so the
back-button motion mirrors correctly regardless.

## Active links

```razor
<BrouterLink Href="/" Match="BrouterLinkMatch.All">Home</BrouterLink>
<BrouterLink Href="/users" Class="nav-item">Users</BrouterLink>
```

## Scroll & focus management

After each successful navigation Brouter runs a few DOM effects, all configured on `BrouterOptions`
and applied once the matched route is committed to the DOM (so `#fragment` and focus selectors resolve
against the new page). During static prerender these are skipped - there is no DOM/JS to act on.

```csharp
builder.Services.AddBitBrouterServices(o =>
{
    // Scroll the window to the top on navigation. Default: BrouterScrollMode.None.
    o.ScrollBehavior = BrouterScrollMode.ToTop;

    // Scroll a URL fragment into view: navigating to /docs#install lands on the #install
    // element (and moves focus to it). A found fragment target wins over ScrollBehavior.
    // Only acts when the URL carries a fragment. Default: true.
    o.ScrollToFragment = true;

    // Remember each page's scroll position and restore it on Back/Forward, like native browsers
    // and real SPA routers. A NEW navigation still uses ScrollBehavior (e.g. ToTop); only a
    // Back/Forward to a previously-visited URL restores where the user left off. Enabling this
    // takes over the browser's own restoration (history.scrollRestoration = "manual"). Default: false.
    o.RestoreScrollPosition = true;

    // Where restored positions are stored. Default Memory (lost on a full reload). Use SessionStorage
    // (recommended: per-tab, auto-cleared on tab close) or LocalStorage (survives restarts, shared
    // across tabs) to make positions survive a reload. No effect unless RestoreScrollPosition is on;
    // falls back to in-memory if the store is unavailable (private mode, quota).
    o.ScrollPositionStorage = BrouterScrollPositionStorage.SessionStorage;

    // Move focus to this selector after navigation so assistive technologies announce the new
    // page instead of leaving focus on the activated link - a WCAG-relevant concern for an SPA
    // router, mirroring Blazor's <FocusOnNavigate>. A non-focusable target gets tabindex="-1"
    // so it can receive programmatic focus without joining the Tab order. Default: null (off).
    o.FocusOnNavigateSelector = "h1";
});
```

Precedence when several apply: if a fragment target resolves, it scrolls into view and takes focus, and
no further scroll or focus handling runs (so `FocusOnNavigateSelector` is not applied on that navigation).
Otherwise, on a Back/Forward with a remembered position that position is restored, else scroll-to-top runs;
and only in these non-fragment cases does `FocusOnNavigateSelector` (if set) then receive focus.

## View transitions

Enable the browser's View Transitions API to animate between pages:

```csharp
builder.Services.AddBitBrouterServices(o =>
{
    o.ViewTransitions = true;
});
```

**Beautiful by default.** With `ViewTransitions` on, Brouter ships polished, direction-aware
animations out of the box (`o.ViewTransitionDefaultAnimations`, enabled by default):

- a forward navigation (push) glides the new page in;
- Back/Forward (pop) **mirrors the motion**, so going back *feels* like going back;
- a replace does a quick in-place fade;
- shared-element morphs get a springy glide;
- `prefers-reduced-motion` swaps the slides for gentle opacity-only crossfades (and stills the
  morphs) - navigation keeps visual feedback without movement. Note that OS accessibility settings
  feed this media query: on Windows, turning off Settings > Accessibility > Visual effects >
  **Animation effects** makes every browser on the machine report `reduce`. Because that setting is
  often off for *performance* reasons (VMs, remote desktops) rather than user preference, you can
  bypass it with `o.ViewTransitionRespectReducedMotion = false` - think twice, though: for
  motion-sensitive users `reduce` is a genuine request.

The defaults live in the CSS layer `bit-brouter`, so **any unlayered `::view-transition-*` rule in
your own CSS overrides them automatically** - customize without specificity fights, or set
`o.ViewTransitionDefaultAnimations = false` to opt out entirely. The current direction is exposed as
`data-brouter-nav="push|replace|pop"` on `<html>` for your CSS to key off.

Per-element morphs are standard CSS - the same `view-transition-name` on both pages makes the
element morph between them (see the demo's `/gallery` page for a tile-to-hero showcase):

```css
.post-title { view-transition-name: post-title; }
```

The same mechanism **excludes persistent chrome** from the page animation. Anything without a
`view-transition-name` is captured into the `root` snapshot, so a layout header/sidebar would glide
along with the page even though it lives outside the router. Give it its own name and it gets its
own transition group - old and new snapshots are identical, so it stays visually pinned while only
the page content animates:

```css
.app-header { view-transition-name: app-header; }
```

One follow-up gotcha: if some pages scroll and others don't, classic scrollbars (Windows) change
the viewport width between snapshots, so even a named header animates a ~17px resize. Reserve the
gutter to keep the layout width constant:

```css
html { scrollbar-gutter: stable; }
```

Brouter splits the transition around Blazor's async render: the outgoing page is snapshotted (and
the snapshot is awaited - critical for correct morphs) right before the new route renders, and the
transition completes once the new DOM (including scroll/focus effects) has landed. On browsers
without `document.startViewTransition`, during prerender, and in non-browser hosts the whole thing
is inert - navigation behaves exactly as with the option off.

> **Troubleshooting: animations (and other JS features) suddenly stop working in dev.** When
> Bit.Brouter's `bit-brouter.js` changes (package update, or a local rebuild of the library), the
> host app's cached compressed static-web-asset manifest can go stale, and the browser receives the
> module as an empty 200 response - transitions, scroll management and link preloading all silently
> stop while navigation keeps working. Fix: **Rebuild the host project once** (`dotnet build
> -t:Rebuild`, or Build > Rebuild in the IDE). `curl` shows the file fine, which makes this
> maddening to diagnose - check the response with an `Accept-Encoding: gzip` header instead.

## Not found handling (.NET 10)

On net10.0, Brouter participates in the framework's not-found contract:

- Application code calling `NavigationManager.NotFound()` (e.g. a page whose entity lookup failed)
  flows through Brouter: the `OnNotFound` hook fires, then the `NotFoundUrl` redirect or inline
  `NotFound` content renders - the URL stays put, mirroring the built-in router.
- When Brouter itself matches nothing during **static SSR**, it calls `NavigationManager.NotFound()`
  so the response carries a real **HTTP 404** (and drives `UseStatusCodePagesWithReExecute` when
  configured) instead of a 200 with fallback HTML.

## Global hooks

```razor
@inject IBrouter brouter
@implements IDisposable

@code {
    private Func<BrouterNavigationContext, ValueTask>? _onNavigating;
    private Func<BrouterNavigationContext, ValueTask>? _onNavigated;
    private Func<BrouterNavigationContext, Exception?, ValueTask>? _onError;

    protected override void OnInitialized()
    {
        _onNavigating = ctx =>
        {
            // Telemetry, analytics, page title, scroll restoration, ...
            return ValueTask.CompletedTask;
        };
        _onNavigated = ctx => ValueTask.CompletedTask;
        _onError = (ctx, ex) => ValueTask.CompletedTask;

        brouter.OnNavigating += _onNavigating;
        brouter.OnNavigated += _onNavigated;
        brouter.OnError += _onError;
    }

    public void Dispose()
    {
        // Always unsubscribe to avoid handler leaks when the component is removed.
        if (_onNavigating is not null) brouter.OnNavigating -= _onNavigating;
        if (_onNavigated is not null) brouter.OnNavigated -= _onNavigated;
        if (_onError is not null) brouter.OnError -= _onError;
    }
}
```

Besides the `IBrouter` events, the `Brouter` component itself takes two async hooks:
`OnMatch` (fired with the winning `Broute` whenever a route matches) and `OnNotFound` (fired with
the `BrouterLocation` when nothing matches, before the `NotFoundUrl` redirect / `NotFound` fallback applies).

## Nested routes

```razor
<Broute Path="/users">
    <Broute Path="/{id:int}" Component="@typeof(UserPage)" />
    <Broute Path="/{id:int}/edit">
        <Content Context="p">Edit user [@p["id"]]</Content>
    </Broute>
</Broute>
```

```razor
<Broute Path="/dashboard">
    <Content>
        <h1>Dashboard</h1>
        <BrouterOutlet />
    </Content>
    <Routes>
        <Broute Path="/stats" Component="@typeof(StatsPage)" />
    </Routes>
</Broute>
```

When another template (`Content`, `Found`, `NotFound`, `ErrorContent`, ...) is present, Razor
requires the child routes to be wrapped in an explicit fragment. `Routes` is an alias for
`ChildContent` on both `Brouter` and `Broute` so that wrapper can carry a self-describing name -
either spelling works (setting both throws).

### Pathless group routes

Share behavior across routes without inventing a URL segment:

```razor
<Broute Group Path="" Guard="@RequireAdmin" Loader="@LoadAdminShell">
    <Content>
        <AdminShell><BrouterOutlet /></AdminShell>
    </Content>
    <Routes>
        <Broute Path="/dashboard" Component="@typeof(DashboardPage)" />
        <Broute Path="/audit" Component="@typeof(AuditPage)" />
    </Routes>
</Broute>
```

`/dashboard` and `/audit` match exactly as written - the group is invisible in the URL, in
specificity and in depth tiebreaks - but its guard, loader, layout and `ErrorContent` apply to both
children. Sibling groups coexist freely (they never register as matchable templates).

### Lazy route loading

```razor
<Brouter OnNavigateAsync="@LoadRouteAssemblies" Navigating="@LoadingUi">...</Brouter>

@code {
    [Inject] LazyAssemblyLoader Lazy { get; set; } = default!;

    private async ValueTask<IEnumerable<Assembly>?> LoadRouteAssemblies(BrouterNavigationContext ctx)
    {
        if (ctx.To.Path.StartsWith("/reports"))
            return await Lazy.LoadAssembliesAsync(["Reports.wasm"]);
        return null;
    }
}
```

The hook runs before matching on every navigation (initial deep links included). Returned
assemblies are scanned for `@page`/`[Route]` components and registered *within the same
navigation*, so the URL that triggered the load lands on the freshly-loaded page - no
grow-a-list-and-re-render dance.

### Functional query updates

```csharp
// From /q?filter=red&sort=name&page=1:
brouter.NavigateWithQuery(q => q.Set("page", 2));          // -> /q?filter=red&sort=name&page=2
brouter.NavigateWithQuery(q => q.Remove("filter"));         // untouched params always survive
brouter.NavigateWithQuery(q => q.SetAll("tag", ["a", "b"])); // -> ?tag=a&tag=b
```

Values are formatted invariantly (same rules as `ResolveUrl`), null removes a parameter, and the
navigation replaces the history entry by default (query-as-UI-state); pass `replace: false` to push.

## Typed routes (source generator)

Add the `Bit.Brouter.Generators` package and every route declared in your `.razor` files - `@page`
directives, `@attribute [Route(...)]`, and literal (nested) `<Broute Path="...">` trees - gets a
compile-time-safe URL builder on a generated `BrouterRoutes` class in your root namespace:

```razor
@* declared somewhere: <Broute Name="counter" Path="/counter/{init:int}" />
                       <Broute Path="/profile/{username?}" />
                       @page "/files/{**path}" *@

<BrouterLink Href="@BrouterRoutes.Counter(1234)">Counter</BrouterLink>

@code {
    void Go() => brouter.Navigate(BrouterRoutes.ProfileByUsername("saleh", query: "tab=posts"));
    string FileUrl() => BrouterRoutes.Files("docs/readme.md");
    void ByName() => brouter.NavigateToName(BrouterRoutes.Names.Counter,
                         new Dictionary<string, object?> { ["init"] = 5 });
}
```

- Constraints become parameter types (`{init:int}` → `int init`, `{id:guid}` → `Guid id`; the last
  constraint wins, matching the matcher), optionals become optional arguments, catch-alls become
  path strings split and escaped per segment.
- Methods are named from the route's `Name` when present (named routes always own their identifier)
  or from the template's literals + `By{Param}` suffixes; every method takes a trailing
  `string? query = null`.
- Values are escaped and formatted with the router's exact invariant rules, so a generated URL
  always round-trips through its own template.
- Skipped by design: dynamic paths (`Path="@expr"`) and their subtrees, `RedirectTo` routes,
  literal-wildcard templates (`/*/x`), and `Group` routes (their children generate normally).

## Named outlets

One route can fill several regions of its parent's layout. The parent declares outlets; each child
route provides its main content plus optional named `BrouterView` fragments:

```razor
<Broute Path="/dashboard">
    <Content>
        <main><BrouterOutlet /></main>                @* primary: the child's Content/Component *@
        <aside><BrouterOutlet Name="sidebar" /></aside> @* named: the child's matching BrouterView *@
    </Content>
    <Routes>
        <Broute Path="/stats">
            <Content><StatsPage /></Content>
            <Routes>
                <BrouterView Name="sidebar" Context="p"><StatsFilters /></BrouterView>
            </Routes>
        </Broute>
        <Broute Path="/settings" Component="@typeof(SettingsPage)" />  @* no view -> sidebar renders empty *@
    </Routes>
</Broute>
```

Named views receive the route's parameters (the `Context`) and see its data/meta cascades. Unlike
Angular's secondary outlets there is no URL serialization - the named regions always follow the
primary match, which is the common layout case.

## Keep-alive routes

```razor
<Broute Path="/search" KeepAlive="true">
    <Content><SearchPage /></Content>  @* filters, scroll, half-typed input survive navigation *@
</Broute>
```

When the user navigates away, the rendered component stays mounted inside a hidden wrapper instead
of being disposed; navigating back flips it visible again with all its state intact - including
through a parent's `BrouterOutlet` when switching between sibling routes. Opt-in per route; combine
with `StaleTime` for instant, fully-warm Back navigation.

### Route lifecycle: activate / deactivate / renavigate

Every routed component - keep-alive or not - can receive discrete lifecycle callbacks, the
component-level hooks Angular's `RouteReuseStrategy` never delivered and Ionic's
`ionViewWillEnter`/`ionViewDidLeave` proved right:

- **`OnActivated`** - the content just became the visible route content: on the first show (with
  `activation.IsFirstActivation` set, Vue-style superset-of-mount semantics) and again every time a
  kept-alive instance is revealed. Runs after the commit render, so the DOM is available.
- **`OnDeactivated`** - it just stopped being visible, with a reason: `Hidden` (kept mounted,
  keep-alive) or `Disposing` (about to be torn down - the synchronous part runs *before* `Dispose`,
  and unlike `Dispose` it carries the destination location). `KeepAlive` only changes which reason
  you get; pages written against the lifecycle keep working when the route's retention changes.
- **`OnRenavigated`** - a navigation re-committed this route while the *same instance* stayed
  visible (`/item/1 → /item/2` on a singleton route, or a query-only change): the "user arrived
  here again" moment that `OnInitialized` misses on instance reuse. On a per-parameter keep-alive
  route (`KeepAliveMax` > 1) a parameter change mounts a separate instance instead, so it surfaces
  as an activate/deactivate pair rather than a renavigation.

All callbacks have async variants; returned tasks are observed for errors (surfaced via
`IBrouter.OnError`) but never delay the navigation. They are not invoked during static prerendering.
`OnActivated` and `OnDeactivated` are always **paired**: content that never received its activation
(skipped under static prerender, or a commit superseded before its activation render) is torn down
through `Dispose` alone and gets no `OnDeactivated`, so acquire-in-`OnActivated` /
release-in-`OnDeactivated` handlers can never release something they never acquired.
(The lifecycle also carries two *pre-commit* callbacks that deliberately CAN delay a navigation -
the navigation lock below.)

The easiest consumption is `BrouterRouteBase` - it works for any component under the routed content
(any depth, any render path) and repaints automatically after activation/renavigation:

```razor
@inherits BrouterRouteBase
@code {
    private Timer? _poll;

    protected override void OnActivated(BrouterRouteActivation activation)
    {
        // First show AND every reveal from keep-alive retention: resume/refresh here.
        _poll ??= new Timer(_ => Refresh(), null, 0, 5000);
        _poll.Change(0, 5000);
    }

    protected override void OnDeactivated(BrouterRouteDeactivation deactivation)
    {
        // Hidden (kept) or Disposing (leaving for real) - pause background work either way.
        _poll?.Change(Timeout.Infinite, Timeout.Infinite);
    }

    protected override void Dispose(bool disposing) => _poll?.Dispose();
}
```

A page component instantiated by the router itself (a `Component=` route or a discovered `@page`
route) can skip the base class and just implement the interface - the router discovers it
automatically:

```razor
@implements IBrouterRoute
@code {
    public ValueTask OnRenavigatedAsync(BrouterRouteRenavigation renavigation)
        => RefreshAsync(); // e.g. /item/1 -> /item/2 reused this instance; OnInitialized won't re-run
}
```

(All `IBrouterRoute` members have no-op defaults - implement only what you need.) For full manual
control - or for content the router doesn't instantiate (`Content` fragments, `Found`-template
pages) when not using the base class - take the cascaded context and register any handler:

```razor
@code {
    [CascadingParameter] BrouterRouteContext? RouteContext { get; set; }
    // RouteContext.Register(handler) / Unregister(handler); RouteContext.IsActive for current state.
}
```

Why it matters for keep-alive in particular: a kept component keeps *running* while hidden - timers,
polling and live subscriptions all keep firing, and any `StateHasChanged` re-renders it off-screen
(on Blazor Server that is a diff over the wire for a page nobody is looking at). Pause that work in
`OnDeactivated` and resume (or refresh) in `OnActivated`.

### Navigation lock: blocking from the component itself

The lifecycle's two *pre-commit* callbacks let the routed component - the code that actually knows
whether its form is dirty or its save is still in flight - veto a pending navigation (React Router's
`useBlocker`, Vue's `onBeforeRouteLeave` + `beforeRouteUpdate`, Angular's `CanDeactivate`, delivered
component-side):

- **`OnDeactivating`** - a pending navigation would deactivate this content. Carries the pending
  target (`context.To`), the `NavigationType`, and the retention that would follow
  (`context.Reason`: `Hidden` for keep-alive - state survives, so you may skip the prompt
  entirely - or `Disposing`).
- **`OnRenavigating`** - a pending navigation keeps this route matched (a route/query parameter
  change, or moving between its descendants). This is the case `LeaveGuard` deliberately never
  fires for - without it, a dirty edit form on `/item/1` couldn't veto going to `/item/2`.

Both are **awaited** by the pipeline (unlike the notify-only lifecycle callbacks) and run inside
the preventive phase, so `context.Cancel()` / `context.Redirect(...)` stop the URL from ever
changing - no address-bar flicker, no corrupted Back stack. Because they're awaited, a lock can
hold the navigation open and show a **custom confirmation dialog** instead of `window.confirm`:

```razor
@inherits BrouterRouteBase

@if (_prompt is not null)
{
    <div class="modal">
        Unsaved changes - leave for @_target anyway?
        <button @onclick="@(() => _prompt.TrySetResult(true))">Stay</button>
        <button @onclick="@(() => _prompt.TrySetResult(false))">Leave</button>
    </div>
}

@code {
    private TaskCompletionSource<bool>? _prompt;
    private string? _target;

    protected override async Task OnDeactivatingAsync(BrouterRouteDeactivatingContext context)
    {
        if (_isDirty is false) return;

        _prompt = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _target = context.To.Path;
        StateHasChanged();                       // show the dialog; the navigation stays parked

        // A superseding navigation cancels the token: dismiss the prompt, the answer no longer matters.
        await using var dismiss = context.CancellationToken.Register(() => _prompt?.TrySetResult(false));
        var stay = await _prompt.Task;

        _prompt = null;
        StateHasChanged();
        if (stay) context.Cancel();              // preventive: the URL never changed
    }
}
```

Semantics worth knowing:

- **Ordering.** Locks run in the leave phase, leaf → root, and per route the content's own locks run
  *before* the route-declared `LeaveGuard` (innermost first - the code closest to the state at risk
  gets the first veto), all before `OnNavigating` and any enter guards.
- **First decision wins.** Handlers on the same content run in registration order; the first
  `Cancel()`/`Redirect()` anywhere settles the phase and skips every remaining lock and leave
  guard - no stacked prompts.
- **Only visible content votes.** Hidden kept-alive instances aren't being deactivated by the
  navigation and are not consulted.
- **Fail closed.** A lock that throws blocks the navigation (like a guard) and surfaces via
  `IBrouter.OnError`.
- **Supersession.** A newer navigation cancels `context.CancellationToken`; a parked lock should
  observe it (as above) so a stale prompt can't decide a dead navigation - the pipeline ignores its
  outcome either way.
- **Per-parameter keep-alive nuance.** On a `KeepAliveMax > 1` route that stays matched across a
  parameter change, the active entry receives `OnRenavigating` (pre-commit, the new parameter key
  isn't known yet) even though the commit then surfaces as a `Hidden` deactivation + sibling
  activation.
- **Platform boundary.** Locks cover in-app navigations only. Tab close, reload and external links
  can't run C# - arm the browser's generic dialog via `o.ConfirmExternalNavigation` /
  `brouter.SetConfirmExternalNavigationAsync(...)` for those (see
  [Leave guards](#leave-guards-unsaved-changes)).

Any component under the routed content can hold a lock - derive from `BrouterRouteBase` (as above),
implement `IBrouterRoute` on a router-instantiated page (auto-discovered), or `Register` a handler
on the cascaded `BrouterRouteContext`. Simple non-interactive locks are one line:

```csharp
protected override void OnDeactivating(BrouterRouteDeactivatingContext context)
{
    if (_isDirty) context.Cancel();
}
```

### Per-parameter caching with `KeepAliveMax`

By default (`KeepAliveMax` unset, i.e. 1) retention is **per route**: a parameterized keep-alive
route (e.g. `/item/{id}`) keeps a *single* live instance that re-binds to each new value - state
carries *across* parameter changes. Set `KeepAliveMax` above 1 to cache **per parameter values**
instead:

```razor
<Broute Path="/item/{id:int}" KeepAlive="true" KeepAliveMax="5">
    <Content><ItemPage /></Content>
</Broute>
```

Now `/item/1 → /item/2 → /item/1` keeps two separate instances and returning to each resumes its
exact state (parameters and loader data stay frozen on hidden instances). When more than
`KeepAliveMax` parameter sets have been visited, the least-recently-used hidden instance is evicted
(disposed). The cache key is the route's template parameter values only - query-string variations
share one instance. `BrouterOptions.DefaultKeepAliveMax` sets the default for routes that don't
declare their own.

### What it keeps, and for how long

- **The loader still runs on return.** Keep-alive preserves component state, but a return navigation
  re-matches the route and re-runs its `Loader` unless a `StaleTime` cache hit covers it. Pair the
  two if you also want the data reused.
- **Lifetime is bounded by the hosting layout.** State survives sibling switches under a layout and
  navigations away and back at the *top* level, but not the hosting layout's own unmount, nor a full
  page reload. To extend a nested route's retention across leaving its parent, mark the parent
  `KeepAlive` too (at the cost of keeping the whole subtree hidden-mounted).

### Cost and eviction

Each kept page holds its C# state **and** its hidden DOM for as long as it is retained, so the cost
is memory + DOM node count per kept page (measured by the `Tests/Bit.Brouter.Benchmarks` project).
Retention is inherently bounded - one instance per `KeepAlive` route by default, up to
`KeepAliveMax` for per-parameter caching - but you can release it on demand:

```csharp
@inject IBrouter brouter
...
brouter.ClearKeepAlive();  // dispose every retained (hidden) page; the visible one stays
```

Call it on sign-out, under memory pressure, or after invalidating the state those pages hold; the
next visit to a dropped route recreates it fresh.

> Notes: turning on `KeepAlive` wraps the route's inline content in a `<div>` (the stable element
> that preserves the subtree), which can affect direct-child CSS selectors. Retention applies to a
> route's primary content; named-outlet (`BrouterView`) fragments are never independently retained,
> so they don't ride the keep-alive lifecycle cascade across hide/show - they dispose and recreate
> with their host. While a named fragment is rendered, though, it still holds its own active
> lifecycle context: `BrouterRouteBase` descendants inside it register for navigation locks and
> receive component-level lock dispatch like any routed content. Instances dropped by LRU eviction
> or `ClearKeepAlive()` were already deactivated (`Hidden`) when they were hidden, so plain component
> disposal is their final signal.

## Attribute-route / `@page` discovery

Routes don't have to be hand-declared in one tree. Point `Brouter` at your assemblies and it discovers
components annotated with `[Route]` (which is what `@page` compiles to), matching them alongside any
hand-declared `<Broute>` children. This keeps route templates colocated with their pages, supports Razor
class libraries, and works with lazily-loaded assemblies.

```razor
@* Counter.razor - the route lives next to the page *@
@page "/counter/{start:int}"

<h1>Count: @Start</h1>

@code {
    [Parameter] public int Start { get; set; }                       // bound from the {start:int} segment
    [Parameter, SupplyParameterFromQuery] public string? Tab { get; set; } // bound from ?tab=
}
```

```razor
<Brouter AppAssembly="@typeof(App).Assembly"
         AdditionalAssemblies="_lazyLoaded">
    @* Optional: hand-declared routes still work and win ties over discovered ones *@
    <Broute Path="/" RedirectTo="/home" />
</Brouter>

@code {
    // Grow this list (with a re-render) as assemblies load to register their routes at runtime.
    private readonly List<System.Reflection.Assembly> _lazyLoaded = new();
}
```

A hand-declared `<Broute>` with the exact template of a discovered `@page` shadows it (useful to attach a
`Guard`/`Loader` to an existing page) - this is the one duplicate-template pairing that isn't rejected as
ambiguous. Duplicating a template across two `@page` components, or across two hand-declared routes, throws.

Discovered and hand-declared routes bind component parameters identically: route segments to `[Parameter]`
properties by name (Blazor-style) and query values to `[Parameter, SupplyParameterFromQuery]` properties.
Brouter binds the query values itself, so this works even where the framework's own query supplier isn't
registered; where it is registered, the framework prefers the router-supplied value by design.
Two framework rules to keep in mind: a `[SupplyParameterFromQuery]` property *without* `[Parameter]` is
left to the framework's supplier (Blazor forbids setting it explicitly), so pair the two attributes on
routed components; and `[SupplyParameterFromQuery]` property types must stay within the set the framework
supplier can parse - string, numerics, bool, Guid, DateTime/DateOnly/TimeOnly, plus nullables and arrays
of those (notably *not* enums) - because the supplier evaluates every annotated property and throws for
anything else. For types outside that set, use Brouter's opt-in `[BrouterQuery]` instead (see
[Auto-bound parameters](#auto-bound-parameters)).

> Discovery reflects over the given assemblies, so - like the built-in Blazor `Router` - keep your routable
> components preserved when trimming.

## Migrating from the built-in Router

The built-in Router's companion components - `RouteView`, `AuthorizeRouteView`, `FocusOnNavigate` - are
router-independent: they consume a `RouteData` (the matched page type plus its route values), not the
`Router` itself. Brouter builds that `RouteData` for every matched page and hands it to those framework
components for you - so for the common case the whole `Found`/`AuthorizeRouteView` template disappears
into a few flat parameters, and for full control the built-in `<Found>` block still ports over verbatim.

### Zero-template authorization

For the common case - `[Authorize]` on pages, a layout, fallback UI - you don't need any routing
template at all. Set the fallback fragments (and optionally a `DefaultLayout`) directly on the router:

```razor
<Brouter AppAssembly="@typeof(App).Assembly" AdditionalAssemblies="_additional"
         DefaultLayout="@typeof(MainLayout)">
    <Authorizing>Checking credentials…</Authorizing>
    <NotAuthorized><RedirectToLogin /></NotAuthorized>
    <NotFound>Sorry, there's nothing here.</NotFound>
    <Navigating>Loading…</Navigating>
</Brouter>
```

Setting any of `NotAuthorized` / `Authorizing` / `Resource` routes every Component-rendering route
through the framework's own `AuthorizeRouteView`. That's deliberate: policy/role evaluation, the
`Authorizing`/`NotAuthorized` states, `DefaultLayout` plus per-page `@layout` resolution, and reaction
to live authentication-state changes are all Microsoft's implementation - Brouter never owns
authorization correctness. Pages without `[Authorize]` render normally through the same path, and
`Resource` is forwarded for resource-based policies. As with the built-in Router stack, the app still
provides `CascadingAuthenticationState` (or `AddCascadingAuthenticationState()`) and
`AddAuthorizationCore()`.

A lone `DefaultLayout` (no auth fragments) composes the framework's plain `RouteView` instead - which,
per its own contract, throws for `[Authorize]` pages rather than skipping the check. And when nothing
is configured at all, Brouter's native rendering **fails closed** too: instantiating a component that
declares `[Authorize]` throws with guidance, so a missing configuration can never silently bypass
authorization (use a route `Guard` and drop the attribute if you want guard-based auth instead). An
explicit `Found` parameter (below) always wins over these built-in compositions.

### Full control: the `Found` template

When you need arbitrary per-navigation markup around the page - telemetry publishers, custom
`AuthorizeRouteView` subclasses, `FocusOnNavigate` - the built-in Router's `<Found Context="routeData">`
block ports over verbatim:

```razor
<Brouter AppAssembly="@typeof(App).Assembly" AdditionalAssemblies="_additional">
    <Found Context="routeData">
        <AuthorizeRouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)">
            <Authorizing>Checking credentials…</Authorizing>
            <NotAuthorized><RedirectToLogin /></NotAuthorized>
        </AuthorizeRouteView>
        <FocusOnNavigate RouteData="@routeData" Selector="h1" />
    </Found>
    <NotFound>Sorry, there's nothing here.</NotFound>
    <Navigating>Loading…</Navigating>
</Brouter>
```

Everything the framework components already do keeps working with zero Brouter-specific code:

- **Authorization** - `[Authorize]` (policies/roles) on pages, the `Authorizing` and `NotAuthorized`
  fragments, and `CascadingAuthenticationState` integration, all via the built-in `AuthorizeRouteView`.
- **Layouts** - `DefaultLayout` plus per-page `@layout` directives (nested layout chains included), via
  the built-in `RouteView`/`AuthorizeRouteView`.
- **Focus** - `FocusOnNavigate` re-focuses its selector whenever the supplied `RouteData` changes.

### Observing the current `RouteData` from anywhere

Brouter also cascades the committed navigation's `RouteData` (unnamed, by type) to every descendant -
null when nothing matched or the matched route renders a `Content` fragment. Route-data publishers,
breadcrumbs and telemetry components can observe it without touching the routing template:

```razor
@* Works anywhere under the Brouter - no Found template required. *@
@code {
    [CascadingParameter] public RouteData? RouteData { get; set; }

    protected override void OnParametersSet()
    {
        // Fires on every committed navigation with the new RouteData (or null on not-found).
    }
}
```

The mapping from the built-in `Router`:

| Built-in `Router` stack | Brouter |
|---|---|
| `<Found>` + `<AuthorizeRouteView>` + `Authorizing`/`NotAuthorized` | flat `Authorizing`/`NotAuthorized` fragments on `<Brouter>` (no template), or an unchanged `<Found Context="routeData">` for full control |
| `RouteView`/`AuthorizeRouteView` `DefaultLayout` | `DefaultLayout` on `<Brouter>` |
| `AuthorizeRouteView` `Resource` | `Resource` on `<Brouter>` |
| `<NotFound>` | `<NotFound>` (unchanged; or a `NotFoundUrl` redirect URL) |
| `<Navigating>` | `<Navigating>` (unchanged) |
| `AppAssembly` / `AdditionalAssemblies` / `OnNavigateAsync` | same-named parameters |

`Found` applies to every matched route that renders a `Component` - attribute-discovered `@page` routes
and hand-declared `<Broute Component=...>` routes alike. When set, Brouter builds the `RouteData` and the
template renders the page; Brouter does not instantiate the component itself, so parameter binding is the
built-in `RouteView` binding (which is the point: identical behavior to the built-in stack). Guards,
loaders, keep-alive and error boundaries still run around the template as usual. Two kinds of routes
deliberately ignore `Found`: `Content`-fragment routes (no page type to expose) and child routes rendered
into a `BrouterOutlet` - nested-outlet layouts are Brouter's own layout model, and resolving per-page
`@layout`s inside an outlet-hosted layout would apply two layout systems at once. Migrate first, then
adopt guards/loaders/outlets incrementally where they earn their keep.

## Performance & scalability

Brouter is declarative: **every route is a live component instance**. Each hand-declared `<Broute>` - and
each attribute-discovered route, which Brouter emits as a synthetic `<Broute>` - is a `ComponentBase`
mounted in the render tree for the lifetime of the `Brouter`, carrying its own renderer, cached
template/parameter dictionaries and cascading-value subscriptions. This is what powers nested layouts,
per-route guards/loaders and hierarchical matching, but it differs from the built-in Blazor `Router`,
which keeps routes as a plain `RouteTable` (data, not components) and instantiates only the *matched*
component.

Two costs to keep separate:

- **Match cost** (per navigation) is handled: a first-segment index means matching does not do a full
  `O(routes)` scan on every navigation - only routes whose first template segment can match the URL's
  first segment (plus the usually-small set of parameter/wildcard/empty-template routes) are considered.
- **Instantiation cost** (steady state) is *not* reduced by that index. An app with several hundred pages
  keeps several hundred `Broute` instances alive. Unmatched routes render nothing (their `BuildRenderTree`
  short-circuits on the match flag), so this is a memory/instance-count cost, not a per-render one.

For typical apps (tens of routes) this is a non-issue. The `Tests/Bit.Brouter.Benchmarks` project
measures it directly (Brouter vs a RouteTable baseline that instantiates only the matched component).
Indicative numbers (.NET 10, Release): each live route costs on the order of **3-6 KB** of retained
managed heap, so **~500 routes** adds roughly **2.5 MB** of memory and **~4 ms** of startup over the
data-table approach, growing linearly (~5.6 MB / ~8 ms at 1000 routes). Material for a very large
all-`@page` app; negligible otherwise. Run `dotnet run -c Release` in that project for numbers on your
own hardware and route counts.

If you have **hundreds of pages** and care about startup/memory:

- **Benchmark at your real route count** (see `Tests/Bit.Brouter.Benchmarks`) before treating Brouter
  as a drop-in for a very large app.
- **Split routes across lazily-loaded assemblies** and add them to `AdditionalAssemblies` as they load,
  so routes for pages the user hasn't reached yet aren't mounted up front.

## Prerender state bridging

Under SSR/prerender, a route `Loader` runs on the server to produce the prerendered HTML, then the component
becomes interactive and its lifecycle runs again. By default the loader would run a second time (double-fetch).
Enable `PersistLoaderState` to capture each loader result during prerender (via `PersistentComponentState`) and
restore it on the interactive pass instead of re-fetching:

```csharp
builder.Services.AddBitBrouterServices(o =>
{
    o.PersistLoaderState = true;
});
```

Restoration degrades gracefully: if a value can't be rehydrated the loader simply runs again, so a mismatch
never breaks navigation.

> This serializes loader results with reflection-based `System.Text.Json`, which isn't trimming/AOT-safe for
> arbitrary types - enable it when your loader data types are JSON-serializable and preserved under trimming.
> For full trimming/AOT safety, supply a source-generated context:
>
> ```csharp
> [JsonSerializable(typeof(User))]
> [JsonSerializable(typeof(Post))]
> partial class AppJsonContext : JsonSerializerContext { }
>
> builder.Services.AddBitBrouterServices(o =>
> {
>     o.PersistLoaderState = true;
>     o.LoaderStateTypeInfoResolver = AppJsonContext.Default;
> });
> ```
>
> Types the resolver doesn't cover degrade gracefully: their results aren't persisted and the loader
> simply re-runs on the interactive pass.

## Custom constraints

Register custom constraints at startup on `BrouterOptions.Constraints`. They are scoped to the DI
container that owns the options, so separate apps in one process (and parallel test classes) stay
isolated.

```csharp
builder.Services.AddBitBrouterServices(o =>
{
    o.Constraints.Register("slug",
        new BrouterTypeRouteConstraint<string>((string s, out string r) =>
        {
            r = s;
            return s.Length >= 3 && s.All(c => char.IsLetterOrDigit(c) || c == '-');
        }));
});
```

```razor
<Broute Path="/posts/{slug:slug}" Component="@typeof(PostPage)" />
```

> Built-in constraints (`int`, `bool`, `guid`, `long`, `float`, `double`, `decimal`, `datetime`) are
> always available and need no registration.
