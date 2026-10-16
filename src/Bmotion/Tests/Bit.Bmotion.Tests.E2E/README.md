# Bit.Bmotion end-to-end tests

MSTest + Microsoft.Playwright suite that drives the harness pages of `Bit.Bmotion.Tests.Harness.Web` in a real
Chromium and measures what the browser actually renders. The same tests run against every render mode a Blazor app
can host Bit.Bmotion in; the mode is picked per run with `BMOTION_E2E_HOST`. Uses the **Microsoft.Testing.Platform**
runner (mandated by the repo `global.json`) via `EnableMSTestRunner`.

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
dotnet build Bmotion\Tests\Bit.Bmotion.Tests.E2E\Bit.Bmotion.Tests.E2E.csproj

# First time only: the Playwright-managed Chromium.
pwsh Bmotion\Tests\Bit.Bmotion.Tests.E2E\bin\Debug\net10.0\playwright.ps1 install chromium

# The default host (WebAssembly). The harness host is built before the first test.
dotnet test --project Bmotion\Tests\Bit.Bmotion.Tests.E2E\Bit.Bmotion.Tests.E2E.csproj

# Another render mode.
$env:BMOTION_E2E_HOST = "server"
dotnet test --project Bmotion\Tests\Bit.Bmotion.Tests.E2E\Bit.Bmotion.Tests.E2E.csproj
```

Run the harness by hand with one of its launch profiles (`ssr`, `server`, `wasm`, `auto`, `wasm-strict`) and open
any page listed on `/`; `?mode=<mode>` overrides the render mode of one request.

| Variable | Effect |
| --- | --- |
| `BMOTION_E2E_HOST` | The render mode of the run - see above. A name that is not one of them fails the run. |
| `BMOTION_E2E_FRAMEWORK` | Target framework of the harness host: `net10.0` (default), `net9.0`, `net8.0`. |
| `BMOTION_E2E_CONFIGURATION` | Build configuration of the harness host; defaults to the test assembly's. |
| `BMOTION_E2E_SKIP_BUILD` | `1`: the harness host was built beforehand (as CI does); do not rebuild it. |
| `BMOTION_E2E_PUBLISHED_HOST` | A `dotnet publish` output of the harness host to run instead of the build output. |
| `BMOTION_E2E_CHANNEL` | An installed browser channel instead of the bundled Chromium - e.g. `chrome`, `msedge`. |
| `BMOTION_E2E_EXECUTABLE` | Full path to a chromium-family executable. |
| `BMOTION_E2E_HEADED` | `1` to watch the run in a visible window. |

## Layout

* `Infrastructure/` - the host processes (`HarnessTarget`, `WebHarnessHost`, `HarnessProfile`), the build, the
  browser, and `BmotionPageTest`, the base of every fixture.
* One fixture per harness page: `Basics`, `Gestures`, `Drag`, `InView`, `Variants`, `Presence`, `Layout`, `Scroll`,
  `Programmatic`, `Config`, `SplitText`, `Reorder`, `Prerender` (static, without the stylesheet, reduced motion,
  takeover, global options, CSS safe mode) and `Misc` (motion values, view transitions, the inspector, and the
  interop contract against the bundle the host serves).
