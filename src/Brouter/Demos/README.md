# Bit.Brouter Demos

Three Blazor Web App hosts that exercise the same routes and pages from `Demo/Bit.Brouter.Demo.Core` in every interactive render mode, with **server prerendering** enabled so Brouter matches the URL during `OnInitializedAsync` and the matched markup is in the initial HTML.

| Folder | Render mode | Run |
|--------|-------------|-----|
| `Server/` | `InteractiveServer` | `dotnet run --project Demos/Server/Bit.Brouter.Demos.Server` |
| `Wasm/` | `InteractiveWebAssembly` | `dotnet run --project Demos/Wasm/Bit.Brouter.Demos.Wasm` |
| `Auto/` | `InteractiveAuto` | `dotnet run --project Demos/Auto/Bit.Brouter.Demos.Auto` |

## Verify prerendering

```pwsh
dotnet run --project Demos/Server/Bit.Brouter.Demos.Server
# In another shell (no JS):
curl http://localhost:5181/counter/1234
curl http://localhost:5181/profile/saleh
```

The response HTML should already contain the matched page content inside the Blazor prerender markers.

## Wiring (all hosts)

- `Components/App.razor` — host document; shared `app.css` from Core.
- `Components/BlazorRoutes.razor` — ASP.NET Core `Router` funneling every URL to the catch-all host page.
- `Components/Pages/Host.razor` — catch-all page rendering Core `AppRouter` (`<Brouter>`).
- `Program.cs` — `AddCoreServices()` registers Brouter; `MapStaticAssets()` keeps `/_framework/*` out of the catch-all.

`Wasm` and `Auto` include a `.Client` project so WebAssembly interactivity can load the same Core assembly in the browser. `BlazorRoutes` and the catch-all `Host` page live in the **Client** project (not the host), because components with `InteractiveWebAssembly` / `InteractiveAuto` must be defined in the WASM assembly.
