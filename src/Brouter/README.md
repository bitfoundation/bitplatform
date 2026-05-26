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
    o.CaseSensitive = false;        // default
    o.IgnoreTrailingSlash = true;   // default
    o.ScrollBehavior = BrouterScrollMode.ToTop;
});
```

## Quick start

```razor
<Brouter NotFound="404">
    <BrouterRoute Path="/" RedirectTo="/home" />

    <BrouterRoute Name="home" Path="/home">
        <Content><HomePage /></Content>
    </BrouterRoute>

    <BrouterRoute Name="user" Path="/users/{id:int}">
        <Content><UserPage /></Content>
    </BrouterRoute>

    <BrouterRoute Path="/files/{**path}" Component="@typeof(FilesPage)" />

    <BrouterRoute Path="404">
        <Content>
            <h1 class="text-danger">404</h1>
            <p>Sorry, there's nothing at this address.</p>
        </Content>
    </BrouterRoute>
</Brouter>
```

## Features

- Declarative routes with literal segments, parameter segments, constraints and wildcards
- Built-in constraints: `int`, `bool`, `guid`, `long`, `float`, `double`, `decimal`, `datetime`
- Multiple constraints per parameter: `{id:int:long}`
- Wildcards: `*` (single segment), `**` (catch-all)
- **Optional parameters**: `{id?}` - must be trailing
- **Catch-all parameter binding**: `{**path}` exposes the remainder
- Custom constraints via `BrouterConstraints.Register("slug", new MyConstraint())`
- Specificity-based matching (literals beat constrained beat unconstrained beat wildcards)
- Nested routes via `BrouterRoute` children or `BrouterOutlet`
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
- Cancel-then-restore URL semantics (no broken back button after a guard cancels)
- In-flight loader cancellation when navigation is superseded
- Query string and hash exposed via `BrouterLocation`
- Configurable case sensitivity and trailing-slash handling
- Optional scroll-to-top on navigation
- Multi-target: net8.0, net9.0, net10.0

## Type-safe parameters

```razor
<BrouterRoute Path="/users/{id:int}">
    <Content Context="p">
        <p>User: @p.Get<int>("id")</p>
    </Content>
</BrouterRoute>
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
<BrouterRoute Path="/profile/{username?}" Component="@typeof(ProfilePage)" />
```

```razor
@code {
    [Parameter, BrouterParameter] public string? Username { get; set; }
    [Parameter, BrouterParameter(Name = "id")] public int UserId { get; set; }
}
```

## Async guards

```razor
<BrouterRoute Path="/admin" Guard="@CheckAdmin">
    <Content><AdminPage /></Content>
</BrouterRoute>

@code {
    [Inject] AuthService Auth { get; set; } = default!;

    private async ValueTask CheckAdmin(BrouterNavigationContext ctx)
    {
        if (await Auth.IsAdminAsync(ctx.CancellationToken) is false)
            ctx.Redirect("/login?return=" + Uri.EscapeDataString(ctx.To.Path));
    }
}
```

## Data loader

```razor
<BrouterRoute Path="/users/{id:int}" Loader="@LoadUser">
    <Content Context="p">
        <UserDetails />  @* reads cascading RouteData *@
    </Content>
</BrouterRoute>

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
<BrouterRoute Path="/users">
    <BrouterRoute Path="/{id:int}" Component="@typeof(UserPage)" />
    <BrouterRoute Path="/{id:int}/edit">
        <Content Context="p">Edit user [@p["id"]]</Content>
    </BrouterRoute>
</BrouterRoute>
```

```razor
<BrouterRoute Path="/dashboard">
    <Content>
        <h1>Dashboard</h1>
        <BrouterOutlet />
    </Content>
    <ChildContent>
        <BrouterRoute Path="/stats" Component="@typeof(StatsPage)" />
    </ChildContent>
</BrouterRoute>
```

## Custom constraints

```csharp
BrouterConstraints.Register("slug",
    new BrouterTypeRouteConstraint<string>((string s, out string r) =>
    {
        r = s;
        return s.Length >= 3 && s.All(c => char.IsLetterOrDigit(c) || c == '-');
    }));
```

```razor
<BrouterRoute Path="/posts/{slug:slug}" Component="@typeof(PostPage)" />
```
