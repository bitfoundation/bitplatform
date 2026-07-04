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
- Nested routes via `Broute` children or `BrouterOutlet`
- Async `Guard` with cancel/redirect via `BrouterNavigationContext`
- **Async data `Loader`** exposed via cascading `RouteData`
- Redirects with `RedirectTo`
- Component or `Content` (typed render fragment) rendering
- `NotFound` URL or inline `NotFoundContent`
- **Type-safe `BrouterRouteParameters`** with `TryGet<T>` / `Get<T>` / `GetOrDefault<T>`
- **Auto-binding** to component properties via `[Parameter, BrouterParameter]`
- **`<BrouterLink>`** component with active-class and `aria-current` (NavLink-style)
- **Programmatic navigation** via `IBrouter`: `Navigate`, `Back`, `NavigateToName`, `ResolveUrl`
- **Global hooks**: `OnNavigating`, `OnNavigated`, `OnError` (Vue Router style)
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
        <UserDetails />  @* reads cascading RouteData *@
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

Precedence when several apply: a resolved fragment target scrolls (and takes focus) and wins over
everything; otherwise, on a Back/Forward with a remembered position, that position is restored; otherwise
scroll-to-top runs. `FocusOnNavigateSelector` (if set) then receives focus.

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

Discovered routes bind their `[Parameter]` properties by name (Blazor-style) - route segments to plain
`[Parameter]` properties and query values to `[SupplyParameterFromQuery]` (or `[BrouterQuery]`). To get the
same by-name binding on a hand-declared route, set `BindComponentParametersByName="true"` on the `<Broute>`.

> Discovery reflects over the given assemblies, so - like the built-in Blazor `Router` - keep your routable
> components preserved when trimming.

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
