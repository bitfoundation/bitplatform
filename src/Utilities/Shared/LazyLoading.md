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

## How it is checked end to end

Those unit tests read the assembly; none of them publishes an app. Only a publish trims the WebAssembly client,
fingerprints `Bit.<Lib>.wasm` and writes the lazy mark into the boot configuration, so every library that documents
lazy loading also has a lazy loading sample and a browser suite that drives its published copy:

- `Samples/Lazy/Bit.<Lib>.Samples.Lazy` (and its `.Client`): a Blazor Web App set up exactly as the library's guide
  says. `Routes` runs on WebAssembly, the client marks the assembly `BlazorWebAssemblyLazyLoad`, registers none of its
  services and loads it from `OnNavigateAsync` for one route; the server prerenders, registers whatever the library
  needs as any host does (Bink, BbScanner and Blow need nothing) and registers `LazyAssemblyLoader`. The start page
  names none of the library's types. The mark and the router's switch are left out when `RunAOTCompilation` is true,
  as the guides say an AOT build must. BevTools' sample has no lazy route: its guide loads the assembly from
  `BevToolsLoader.OpenAsync`, so the "lazy route" is its Open dev tools button.
- `Tests/E2E/LazyLoadingSample.cs`, linked into each library's end-to-end project: it boots the sample from its
  publish folder, counts the requests for the fingerprinted assemblies (`LazyAssemblyRequests`), and asserts that the
  start page downloaded none of them and that the lazy route downloaded each exactly once - or, for a sample
  published with AOT (`<PREFIX>_E2E_LAZY_AOT=1`), that nothing was downloaded on demand.
- `LazyLoadingTests` in each library's end-to-end project: the start page, an in-app navigation to the lazy route and
  back, a deep link the server prerenders, and one real interaction there that goes through the library's trimmed JS
  interop and its no-registration entry point (`GetBdndInterop()`, `GetBaletteService()`,
  `GetBcrdtCollaborationService(...)`, ...), with no console error, unhandled exception or failed request. Its E2E
  workflow publishes the sample trimmed next to the rest of the suite, and AOT-compiled in a leg of its own.

Bortable's sample and suite came first and keep their own copy of the harness.

## The copies

- Blow and Bink: the README's "Lazy loading on WebAssembly" and "Blazor Web App", the Getting Started page's lazy
  loading card, the setup guide's render modes, the debug prompt and its completions, and the unit test built on
  `LazyLoadingAssert`. Blow's tests also check that its Getting Started page shows the README's code.
- Bdnd and Bcrdt: the README's section, the Getting Started page, the setup guide and the debug prompt. Bcrdt's also
  covers configuring its service without registering it.
- Balette and BbScanner: the README's section and the Getting Started page.
- Bmage: `docs/getting-started.md` and the demo's Hosting page.
- BlazeDb: the README's section, the demo's Hosting page, the setup guide, the debug prompt and its completions.
- Bmotion, Bonboarding and Bortable: the README's section, the Getting Started page, the setup guide and the debug
  prompt (Bonboarding's and Bortable's completions too).
- BevTools: the README's "Opening it on demand, and lazy loading" and the Getting Started page. It lazy loads from
  `BevToolsLoader.OpenAsync` rather than from the router.
- Bsqlite: `docs/getting-started.md`'s "Lazy loading on WebAssembly" and the README's summary of it, the Getting
  Started page's lazy loading card, the setup guide's hosting scenarios, the debug prompt and its completions. Bsqlite
  has services to register, so `GetBitBsqliteServices` makes that registration once the assembly has loaded, and its
  unit tests check that instead of using `LazyLoadingAssert`. Its guide also lists the EF Core assemblies to lazy load with
  it, checked on .NET 10 and 11 (rc.3) in a published app.
