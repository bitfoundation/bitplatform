# Bit.Bmotion end-to-end tests

MSTest + Microsoft.Playwright suite that drives the harness pages of `Bit.Bmotion.Tests.Harness.Web` in a real
Chromium and measures what the browser actually renders. The same tests run against every render mode a Blazor app
can host Bit.Bmotion in; the mode is picked per run with `BMOTION_E2E_HOST`. Uses the **Microsoft.Testing.Platform**
runner (mandated by the repo `global.json`) via `EnableMSTestRunner`. One fixture, `LazyLoadingTests`, drives the
published lazy loading sample (`Samples/Lazy`) instead, an app that downloads `Bit.Bmotion.wasm` only when its page
that animates opens - see [Lazy loading](#lazy-loading).

## What is measured, and how

Every assertion is about the element as the browser computes it (`getComputedStyle` + `getBoundingClientRect`, see
`Infrastructure/MotionState.cs`), so a Web Animation playing on the compositor is measured exactly like an inline
style the C# frame loop wrote. Three tools cover almost every test:

* `WaitForStateAsync(selector, "s => ...")` - polls every animation frame until the element satisfies a predicate
  (`s.x`, `s.y`, `s.scaleX`, `s.rotate`, `s.opacity`, `s.left`, `s.width`, `s.backgroundColor`, `s.style`, ...) and
  fails with the last state it saw.
* `SampleAsync(selector, ms)` - one probe per animation frame, each stamped with its time: intermediate values,
  overshoot, stagger gaps, "never went back" checks.
* `ExpectEventAsync(name, count)` - every Bmotion callback a page wires up is logged to `#log` as
  `<li data-event="...">`; a failure quotes the whole log.

The harness pages are written so the tests can be deterministic:

* `HarnessPage` handles events without re-rendering: a callback only writes to the log (which re-renders itself).
  A page re-renders only through a control that asks for it, so a test exercises exactly the re-render it names.
* `#status` reads `ready` only after the first interactive render, and `#caps` reports whether the runtime has
  Bit.Bmotion's per-frame loop (`data-frame-loop`). Blazor Server has none - drag, colour and dimension
  interpolation, keyframe arrays on non-compositor properties and the like land at once there - so tests that assert
  intermediate frames of those check `FrameLoop`, and assert the end state everywhere.
* Elements that must be listening before a test touches them announce `armed:<name>` from `OnAnimationStart`,
  which the library raises only once the element's listeners are attached.
* Every uncaught page error, every Blazor fatal error (an unhandled exception in a circuit or the WebAssembly
  renderer) and every bridge error (`bmotion: ...`) fails the test that was running, quoting the host's output.

## Hosts

| `BMOTION_E2E_HOST` | What it is |
| --- | --- |
| `wasm` (default) | InteractiveWebAssembly, prerendered - every feature, with a prerender to take over |
| `wasm-noprerender` | InteractiveWebAssembly without prerendering |
| `server`, `server-noprerender` | InteractiveServer: every call a SignalR round trip, no per-frame loop |
| `auto`, `auto-noprerender` | InteractiveAuto: a visit may land on either runtime |

Static SSR has no interactivity of its own; the prerender fixtures ask the same host for it per page with
`?mode=ssr` (and drive it with scripting disabled, which is exactly what the served HTML shows on its own).

Global library options are process-wide, so fixtures needing other options get a host process of their own
(`Infrastructure/HarnessProfile.cs`): `Strict` (prerender `Animate`, reduced motion `User`, CSS safe mode `Throw`)
and `CssWarn`.

## Running

From inside `src` (so `src/global.json` selects the test runner):

```powershell
dotnet build Utilities\Bmotion\Tests\Bit.Bmotion.Tests.E2E\Bit.Bmotion.Tests.E2E.csproj

# LazyLoadingTests runs the lazy loading sample's publish output: publish it in the configuration the tests run in.
dotnet publish Utilities\Bmotion\Samples\Lazy\Bit.Bmotion.Samples.Lazy\Bit.Bmotion.Samples.Lazy.csproj -c Debug

# First time only: the Playwright-managed Chromium.
pwsh Utilities\Bmotion\Tests\Bit.Bmotion.Tests.E2E\bin\Debug\net10.0\playwright.ps1 install chromium

# The default host (WebAssembly). The harness host is built before the first test.
dotnet test --project Utilities\Bmotion\Tests\Bit.Bmotion.Tests.E2E\Bit.Bmotion.Tests.E2E.csproj

# Another render mode.
$env:BMOTION_E2E_HOST = "server"
dotnet test --project Utilities\Bmotion\Tests\Bit.Bmotion.Tests.E2E\Bit.Bmotion.Tests.E2E.csproj
```

Run the harness by hand with one of its launch profiles (`ssr`, `server`, `wasm`, `auto`, `wasm-strict`) and open
any page listed on `/`; `?mode=<mode>` overrides the render mode of one request.

| Variable | Effect |
| --- | --- |
| `BMOTION_E2E_HOST` | The render mode of the run - see above. A name that is not one of them fails the run. |
| `BMOTION_E2E_FRAMEWORK` | Target framework of the harness host: `net11.0` (default), `net10.0`. |
| `BMOTION_E2E_CONFIGURATION` | Build configuration of the harness host; defaults to the test assembly's. |
| `BMOTION_E2E_SKIP_BUILD` | `1`: the harness host was built beforehand (as CI does); do not rebuild it. |
| `BMOTION_E2E_PUBLISHED_HOST` | A `dotnet publish` output of the harness host to run instead of the build output. |
| `BMOTION_E2E_BROWSER` | `chromium` (default), `firefox` or `webkit`: the Playwright browser engine to run in. |
| `BMOTION_E2E_CHANNEL` | An installed browser channel instead of the bundled Chromium - e.g. `chrome`, `msedge`. Chromium only. |
| `BMOTION_E2E_EXECUTABLE` | Full path to a chromium-family executable. |
| `BMOTION_E2E_HEADED` | `1` to watch the run in a visible window. |
| `BMOTION_E2E_LAZY_BASE_URL` | Drive an already running lazy loading sample instead of booting its publish output. |
| `BMOTION_E2E_LAZY_AOT` | `1`: the lazy loading sample was published with `-p:RunAOTCompilation=true`, which leaves its lazy mark out: `LazyLoadingTests` then checks that Bit.Bmotion comes with the app and nothing is downloaded on demand. |

**Firefox and WebKit.** Install the engine once (`playwright.ps1 install firefox webkit`) and run one test class at
a time: headless Firefox throttles the timers and frames of windows that aren't in front, so tests running in
parallel windows starve each other. `serial.testconfig.json` runs them one after another:

```powershell
$env:BMOTION_E2E_BROWSER = "firefox"
dotnet test --project Utilities\Bmotion\Tests\Bit.Bmotion.Tests.E2E\Bit.Bmotion.Tests.E2E.csproj -- --config-file Utilities\Bmotion\Tests\Bit.Bmotion.Tests.E2E\serial.testconfig.json
```

Some browser quirks shape the tests. Under automation Firefox reports a pointer dragged out of the viewport in the
wrong place and drops its `pointerup`, so drags stay inside the viewport. WebKit passes every Playwright pointer move
on the moment it arrives instead of once a frame, so `DragByAsync` paces them a frame apart there.

## Lazy loading

`LazyLoadingTests` drives the published lazy loading sample (`Samples/Lazy`) - trimmed, `Bit.Bmotion.wasm` marked
`BlazorWebAssemblyLazyLoad` and loaded by the router's `OnNavigateAsync`, no `AddBitBmotionServices` in the browser -
as the library README's "Lazy loading on WebAssembly" section sets it up. Its home page never downloads
`Bit.Bmotion.wasm`; navigating to the showcase downloads it once (and not again on the way back), and its card enters
through the frames between rather than snapping to rest. A deep link to the showcase is prerendered by the server (which
registers Bmotion's services with the options the showcase's layout gives `ConfigureBmotionServices`, and the router's
`LazyAssemblyLoader`) with the card's enter in CSS, the client's `Program.cs` downloads the assembly before the app
starts, and the runtime takes the card over without playing its enter again. Either way the card grows under the
pointer, and the page's buttons animate from code through `GetBmotionService<BmotionAnimateService>()`: a Web Animation
on an element handed over by reference, and a number counted up on the per-frame loop. No console error, unhandled
exception or failed request. Published with AOT (`BMOTION_E2E_LAZY_AOT=1`), the sample leaves the lazy mark out as the
README says an AOT build must, and the same tests check that it loads Bit.Bmotion with the app and nothing on demand.

The sample is an app of its own, so the fixture does not derive from `BmotionPageTest`; it opens the sample in the
run's browser and measures it with the same probe. It runs on WebAssembly and .NET 11 whatever `BMOTION_E2E_HOST` and
`BMOTION_E2E_FRAMEWORK` say, so only the run on their defaults (`wasm`, `net11.0`) tests it, and every other run reports
it inconclusive. It is started from its publish folder (`bin/<configuration>/net11.0/publish`) in the Production
environment by the first test that drives it (`src/Utilities/Shared/Tests/E2E/LazyLoadingSample.cs`, the harness every
lazy-loadable library shares); `BMOTION_E2E_LAZY_BASE_URL` points it at a copy that is already running. A sample that
has not been published makes those tests inconclusive - or failed, on a build server (`CI=true`) - and leaves the other
fixtures alone.

In `ci.bmotion.e2e.yml` the `wasm` / `net11.0` leg publishes the sample next to the harness build, and the
`lazy-loading-aot` job publishes it with AOT (needs the wasm-tools workload) and runs `LazyLoadingTests` alone. The
suite starts the harness host before any test runs, so that job builds it too, though none of its tests drive it:

```bash
dotnet build src/Utilities/Bmotion/Tests/Bit.Bmotion.Tests.Harness.Web/Bit.Bmotion.Tests.Harness.Web.csproj -c Release -f net11.0
dotnet publish src/Utilities/Bmotion/Samples/Lazy/Bit.Bmotion.Samples.Lazy/Bit.Bmotion.Samples.Lazy.csproj -c Release -p:RunAOTCompilation=true
dotnet build src/Utilities/Bmotion/Tests/Bit.Bmotion.Tests.E2E/Bit.Bmotion.Tests.E2E.csproj -c Release
cd src/Utilities/Bmotion/Tests/Bit.Bmotion.Tests.E2E && BMOTION_E2E_SKIP_BUILD=1 BMOTION_E2E_LAZY_AOT=1 dotnet test -c Release --no-build --filter "FullyQualifiedName~LazyLoadingTests"
```

## Layout

* `Infrastructure/` - the host processes (`HarnessTarget`, `WebHarnessHost`, `HarnessProfile`), the build, the
  browser, and `BmotionPageTest`, the base of every fixture.
* One fixture per harness page: `Basics`, `Gestures`, `Drag`, `InView`, `Variants`, `Presence`, `Layout`, `Scroll`,
  `Programmatic`, `Config`, `SplitText`, `Reorder`, `Prerender` (static, without the stylesheet, reduced motion,
  takeover, global options, CSS safe mode) and `Misc` (motion values, view transitions, the inspector, and the
  interop contract against the bundle the host serves).
* `LazyLoadingTests` - the published lazy loading sample rather than a harness page (see [Lazy loading](#lazy-loading)).
