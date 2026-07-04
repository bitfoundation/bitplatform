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

## Quick start

```razor
<Brouter NotFound="404">
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
- **Async data `Loader`** exposed via the typed cascading `BrouterRouteData` wrapper (`Get<T>` / `TryGet<T>` / `GetOrDefault<T>`) - sequential root → leaf by default, with opt-in **`ParallelLoaders`** for independent loaders
- Redirects with `RedirectTo`
- Component or `Content` (typed render fragment) rendering
- `NotFound` URL or inline `NotFoundContent`
- **Type-safe `BrouterRouteParameters`** with `TryGet<T>` / `Get<T>` / `GetOrDefault<T>`
- **Auto-binding** to component properties via `[Parameter, BrouterParameter]`
- **`<BrouterLink>`** component with active-class and `aria-current` (NavLink-style)
- **Programmatic navigation** via `IBrouter`: `Navigate`, `Back`, `NavigateToName`, `ResolveUrl`
- **Relative navigation**: `./edit` and `../sibling` resolve against the current location (segment math, React Router style) in `Navigate`, guard redirects and `<BrouterLink>`
- **Global hooks**: `OnNavigating`, `OnNavigated`, `OnError` (Vue Router style)
- **Navigation type** on `BrouterNavigationContext.NavigationType`: distinguishes `Push` / `Replace` / `Pop` (Back/Forward) for scroll-restoration and analytics logic
- **Preventive guards** (via `RegisterLocationChangingHandler`): a cancel/redirect stops the URL from ever changing - no address-bar flicker, no corrupted history/back button, and real "unsaved changes" prompts are possible
- In-flight loader cancellation when navigation is superseded
- **Attribute-route / `@page` discovery**: scan `AppAssembly` / `AdditionalAssemblies` for `[Route]`-annotated components so routes live colocated with their pages (Razor class libraries and lazy-loaded assemblies included)
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

```razor
<Broute Path="/profile/{username?}" Component="@typeof(ProfilePage)" />
```

```razor
@code {
    [Parameter, BrouterParameter] public string? Username { get; set; }
    [Parameter, BrouterParameter(Name = "id")] public int UserId { get; set; }
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

If the chain's loaders are independent (the common case), opt into running them concurrently —
like React Router — with `ParallelLoaders`:

```razor
<Brouter ParallelLoaders="true">
    ...
</Brouter>
```

Results are still committed and errors still surfaced in root → leaf order, so render and failure
behavior are unchanged; only the awaiting overlaps, making the wait as long as the slowest loader
instead of all of them combined.

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

### Relative navigation

Paths starting with `./` or `../` resolve against the **current location** using segment math
(React Router style, not URL directory semantics): from `/users/42`, `Navigate("./edit")` goes to
`/users/42/edit` and `Navigate("../7")` to `/users/7`. Extra `..` clamp at the root, and any query
or hash on the relative URL is preserved.

The same resolution applies in guard redirects — `ctx.Redirect("../login")` resolves against the
path being navigated **to**, so a guard on `/admin/secret` lands on `/admin/login` — and in
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

Detection relies on navigation going through Brouter's own primitives (links and `IBrouter`). A raw
`NavigationManager.NavigateTo` that bypasses `IBrouter` is indistinguishable from a history traversal
and is reported as `Pop`; route programmatic navigations through `IBrouter` to classify them correctly.

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
    <ChildContent>
        <Broute Path="/stats" Component="@typeof(StatsPage)" />
    </ChildContent>
</Broute>
```

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

Discovered routes bind their `[Parameter]` properties by name (Blazor-style) - route segments to plain
`[Parameter]` properties and query values to `[SupplyParameterFromQuery]` (or `[BrouterQuery]`). To get the
same by-name binding on a hand-declared route, set `BindComponentParametersByName="true"` on the `<Broute>`.

> Discovery reflects over the given assemblies, so - like the built-in Blazor `Router` - keep your routable
> components preserved when trimming.

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
