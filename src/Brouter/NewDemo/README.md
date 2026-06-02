# Bit.Brouter NewDemo

A Blazor Web App (interactive server render mode) that showcases **server pre-rendering** with
Bit.Brouter.

## Why this demo exists

The original demo heads (`Demo/Bit.Brouter.Demo.Web` and `...Maui`) run Brouter in a client-only
runtime (WebAssembly / BlazorWebView), where pre-rendering doesn't apply. This project hosts the
exact same routes - it references `Demo/Bit.Brouter.Demo.Core` - inside an ASP.NET Core server with
the `InteractiveServer` render mode, which pre-renders on the server by default.

## What it proves

`Brouter` runs its initial route match in `OnInitializedAsync` (not `OnAfterRenderAsync`). Because
the Blazor server awaits `OnInitializedAsync` during the pre-render pass, the matched route's markup
is part of the initial HTML response - before the SignalR circuit connects and before any JS runs.

You can confirm this without a browser:

```pwsh
# Run the server
dotnet run --project NewDemo/Bit.Brouter.NewDemo.Server

# In another shell, request a deep link and inspect the RAW html (no JS executed).
# The matched page's content appears INSIDE the <!--Blazor:...prerenderId...--> markers.
curl http://localhost:5180/counter/1234
curl http://localhost:5180/profile/saleh
```

For `/counter/1234` the pre-rendered HTML already contains the counter heading, the bound
`init=1234` route parameter, and the `Counter` nav link marked `aria-current="page"`.

## How it's wired

- `Components/App.razor` - the host document. Loads the shared `app.css` from the Core project and
  renders `BlazorRoutes` + `HeadOutlet` with `@rendermode="InteractiveServer"`.
- `Components/BlazorRoutes.razor` - the standard Blazor `Router`. Its only job is endpoint routing:
  it funnels every request to the catch-all host page. (Named `BlazorRoutes` to avoid colliding with
  `Bit.Brouter.Demo.Core.Routes`.)
- `Components/Pages/Host.razor` - a catch-all (`/{*path}`) page that renders the Core project's
  `AppRouter` (which contains the `<Brouter>`). Brouter does the real route matching from here.
- `Program.cs` - standard Blazor Web App startup; `AddCoreServices()` registers `IBrouter`.

## Run

```pwsh
dotnet run --project NewDemo/Bit.Brouter.NewDemo.Server
```

Then browse to <http://localhost:5180> (or the HTTPS URL <https://localhost:7180> from `launchSettings.json`).
