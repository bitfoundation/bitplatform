# Lazy loading a utility on WebAssembly

Several utilities document how a WebAssembly app can download them only when a page that uses them opens. Each one
ships that guidance in its own README, because the README is the NuGet package's readme and the source its MCP server
answers `Get...GuideSection` from, so the text cannot move out of it. What the libraries share is how Blazor's router
and the WebAssembly runtime behave, and that is written down here once. Correct it here first, then in every copy
below.

## What the runtime does

Observed on .NET 11 (rc.3) WebAssembly, non-AOT, in a standalone app and in a Blazor Web App, with Bit.Blow marked
`BlazorWebAssemblyLazyLoad` and loaded from the router's `OnNavigateAsync`:

- The router reads `ExportedTypes` - public types only - of `AppAssembly` and `AdditionalAssemblies`, once, when it
  first builds its route table. That happens after the first navigation's `OnNavigateAsync` has finished.
- A public type whose base type, interface or generic constraint needs the library throws a
  `ReflectionTypeLoadException` from that read when the library is not loaded yet. So the app works when it is opened
  at a page the library is loaded for, and fails on its first page when it is opened anywhere else. The constraint
  case reads `Could not load generic parameter constrains due to Could not load file or assembly ...`.
- An `internal` type that derives from one of the library's loads fine: the router never reads it, and it loads when
  first used, after the library.
- Fields of the library's types on a public page - reference types and structs alike - and public methods that take
  or return them do not stop the router's read. The fields are resolved when the page is created, which the router
  does after loading the library.
- With per-page render modes the router runs statically on the server, so `OnNavigateAsync` never runs in the
  browser. A page with `@rendermode InteractiveWebAssembly` that uses the library fails as it becomes interactive:
  `Could not load type of field '<Page>:<field>' ... Could not load file or assembly ...`. A page that names none of
  the library's types, awaits `LoadAssembliesAsync` in `OnInitializedAsync` and only then renders a component that
  holds the library's component works.
- `@inject LazyAssemblyLoader` is resolved when the component is created, so every host that renders the router's
  file needs `LazyAssemblyLoader` registered: a standalone WebAssembly host registers it by itself; a Web App's server
  project and a Hybrid host do not.

Not part of that check, but relied on by every guide (and by Brouter's FAQ): once WebAssembly has failed to resolve an
assembly, the code that asked cannot use it even after it is loaded, which is why the method that loads a library may
not name its types.

## What a library does

A library that says it lazy loads with nothing to register links `Tests/LazyLoadingAssert.cs` into its unit tests and
checks its assembly with it: its components take from the container only what every host registers, through
`[Inject]` or their constructors, and it has no public static method that takes what an app's startup hands it.

## The copies

- Blow and Bink: the README's "Lazy loading on WebAssembly" and "Blazor Web App", the Getting Started page's lazy
  loading card, the setup guide's render modes, the debug prompt and its completions, and the unit test built on
  `LazyLoadingAssert`. Blow's tests also check that its Getting Started page shows the README's code.
- Bdnd and Bcrdt: the README's section, the Getting Started page, the setup guide and the debug prompt. Bcrdt's also
  covers configuring its service without registering it.
- Balette and BbScanner: the README's section and the Getting Started page.
- Bmage: `docs/getting-started.md` and the demo's Hosting page.
