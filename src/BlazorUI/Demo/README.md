# Bit.BlazorUI.Demo

https://blazorui.bitplatform.dev/

## Debugging Blazor Server vs. WebAssembly

Debug builds render in Blazor Server mode; the WASM client is excluded by default
(see `IncludeWasm` in the csproj). To run/debug the Blazor WebAssembly client,
build with the WASM client included, e.g.:

    dotnet run --project Bit.BlazorUI.Demo.Server -p:IncludeWasm=true

That flag alone both bundles the WASM client and boots the app as Blazor
WebAssembly in Debug (it defines `INCLUDE_WASM`, which `AppRenderMode.WasmEnabled`
reads).

## Build speed

Two things keep the Debug inner loop small, and both are off in Release so that
packing and CI still see the full picture:

- **One target framework.** `Bit.BlazorUI`, `.Assets`, `.Extras`, `.Icons`,
  `.Legacy` and `Bit.BlazorUI.Tests` multi-target `net10.0;net9.0;net8.0`, but the
  demo and the tests only ever run on `net10.0`, so Debug compiles that one alone.
  Pass `-p:BuildAllTfms=true` to compile all three locally; CI already does.
- **No analyzers during build.** `Bit.CodeAnalyzers` runs over every demo page and
  its findings are warnings only, so `RunAnalyzersDuringBuild` is off in Debug (see
  `Directory.Build.props`). The IDE still reports them live, and Release builds
  still run them.

Beyond that:

- Open `Bit.BlazorUI.Web.slnf`, not `Bit.BlazorUI.slnx`. The full solution pulls in
  the MAUI and Windows clients, which add four platform target frameworks nothing
  in the web demo needs.
- Prefer `dotnet watch --project Bit.BlazorUI.Demo.Server` over rebuilding: most
  `.razor` and `.cs` edits apply through hot reload with no build at all, and scss
  edits never need one (the in-app compiler handles them - see `ScssCompilerService`).