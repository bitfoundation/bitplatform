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

Three things keep the Debug inner loop small, and all three come back in Release so
that packing and CI still see the full picture:

- **One target framework.** `Bit.BlazorUI`, `.Assets`, `.Extras`, `.Icons`,
  `.Legacy` and the test projects multi-target `net10.0;net9.0;net8.0`, but the
  demo and the tests only ever run on `net10.0`, so Debug compiles that one alone.
  Pass `-p:BuildAllTfms=true` to compile all three locally; CI already does.
- **No analyzers during build.** `Bit.CodeAnalyzers` runs over every demo page and
  its findings are warnings only, so `RunAnalyzersDuringBuild` is off in Debug (see
  `Directory.Build.props`). The IDE still reports them live, and Release builds
  still run them.
- **No trim/AOT analysis during build.** `IsAotCompatible` and `IsTrimmable` turn on
  ILLink's Roslyn analyzers, and their `DynamicallyAccessedMembersAnalyzer` walks
  every method body: ~26s of wall clock per library project, so a one-line edit in
  `Bit.BlazorUI` used to cost 58s before the demo client came back. `Bit.Build.props`
  turns all three switches (`EnableTrimAnalyzer`, `EnableAotAnalyzer`,
  `EnableSingleFileAnalyzer` - they only pay off together) off for local Debug builds.
  Release keeps them, and so does CI, which builds Debug but sets `CI=true`. Pass
  `-p:RunTrimAnalyzers=true` to check trim warnings locally before opening a PR.

Beyond that:

- Open `Bit.BlazorUI.Web.slnf`, not `Bit.BlazorUI.slnx`. The full solution pulls in
  the MAUI and Windows clients, which add four platform target frameworks nothing
  in the web demo needs.
- Prefer `dotnet watch --project Bit.BlazorUI.Demo.Server` over rebuilding: most
  `.razor` and `.cs` edits apply through hot reload with no build at all, and scss
  edits never need one (the in-app compiler handles them - see `ScssCompilerService`).