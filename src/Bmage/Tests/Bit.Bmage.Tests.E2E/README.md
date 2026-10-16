# Bit.Bmage end-to-end tests

MSTest + Microsoft.Playwright suite that drives the Bmage demo site (`src/Bmage/Bit.Bmage.Demo`) in a real
Chromium. It covers what the unit tests in `Bit.Bmage.Tests` cannot: the `bit-bmage.js` browser bridge, the
browser's own codecs, canvas rendering, pointer and keyboard input on the editor, and the demo pages as a reader
uses them. Uses the **Microsoft.Testing.Platform** runner (mandated by the repo `global.json`) via
`EnableMSTestRunner`.

## What runs

| Class | What it checks |
| --- | --- |
| `CapabilityTests` | The capability probe reports real formats, the WebAssembly runtime and a realistic canvas limit. |
| `ProcessingTests` | The avatar page decodes, processes and encodes through the browser, re-runs on a slider change, and reports a non-image clearly. |
| `EditorTests` | The editor paints the image, renders its toolbar and panel, annotates, redacts the real pixels, undoes, adjusts, rotates, is labelled for assistive technology, and draws from the pointer. |
| `DemoPageTests` | Every page loads without a console error or an error banner, and fits a 320px wide phone without horizontal scrolling. |
| `JsModuleContractTests` | The JS module's own contracts: aspect-preserving decode limits, natural-size decode, no silent PNG substitution, no resource or heap growth. |
| `EditorLifecycleTests` | Repeated navigation, named and reachable controls, roving tab focus, keyboard focus, and rapid input. |
| `DemoRegressionTests` | Defects found by driving the demo: overlay alignment, crop handles, pan, selection, undo of a move, history, paste, failing batches, toggle state, the headless examples. |

## First-time setup

```powershell
dotnet build .\Bit.Bmage.Tests.E2E.csproj

# Install the Playwright-managed Chromium (skip if you will use a system browser, see below).
pwsh .\bin\Debug\net10.0\playwright.ps1 install chromium
```

Without `pwsh`, `node .\bin\Debug\net10.0\.playwright\package\cli.js install chromium` does the same.

## Running

Run from inside `src` (so `src/global.json` selects the test runner):

```powershell
# Builds Bit.Bmage.Demo.Server, starts it on a free 127.0.0.1 port, runs every test, stops it.
dotnet test --project Bmage\Tests\Bit.Bmage.Tests.E2E\Bit.Bmage.Tests.E2E.csproj

# One class, or one test.
dotnet test --project Bmage\Tests\Bit.Bmage.Tests.E2E\Bit.Bmage.Tests.E2E.csproj --filter "FullyQualifiedName~DemoRegressionTests"

# Or run the built test executable directly.
.\Bmage\Tests\Bit.Bmage.Tests.E2E\bin\Debug\net10.0\Bit.Bmage.Tests.E2E.exe --filter "FullyQualifiedName~EditorTests"
```

To drive a demo that is already running - `dotnet run` in another terminal, a published (AOT) build, the live site -
point the suite at it; nothing is built or started then:

```powershell
$env:BMAGE_E2E_HOST_URL = "http://127.0.0.1:5090"
dotnet test --project Bmage\Tests\Bit.Bmage.Tests.E2E\Bit.Bmage.Tests.E2E.csproj
```

The tests run one at a time: a Debug build runs Bmage's pixel loops on the WebAssembly interpreter, and several
tests measure after a fixed settling delay or drag to exact pixel positions, so running them side by side would trade
speed for flakiness. A run against a Release publish (`dotnet publish -c Release` of the server compiles the client
ahead of time) is several times faster.

### Environment variables

| Variable | Effect |
| --- | --- |
| `BMAGE_E2E_HOST_URL` | An already-running demo to drive instead of building and booting one. When unset, the suite starts `Bit.Bmage.Demo.Server` on a free port on 127.0.0.1 - never on the launch profile's port, which other checkouts may hold. |
| `BMAGE_E2E_CONFIGURATION` | Build configuration of the demo host the suite boots; defaults to the test assembly's. |
| `BMAGE_E2E_SKIP_BUILD` | `1`: the demo host was built beforehand (as CI does); do not rebuild it. |
| `BMAGE_E2E_CHANNEL` | Launch an installed browser channel instead - e.g. `chrome`, `msedge`. When unset the suite tries Playwright's bundled Chromium, then Chrome, then Edge. |
| `BMAGE_E2E_EXECUTABLE` | Full path to a chromium-family executable. |
| `BMAGE_E2E_HEADED` | `1`: watch the run in a visible window. |

When no browser can be launched at all, every test reports **Inconclusive** with the reason rather than failing, so a
checkout without browsers still runs the rest of the repository's suites.

## Layout

* `AssemblySetup.cs` - builds the demo host (unless told not to) and starts it once for the whole run; stops it and
  the browser at the end.
* `Infrastructure/DemoHost.cs` - the demo every test drives: an external URL, or the built server started with
  `ASPNETCORE_ENVIRONMENT=Development` (a build output finds its static web assets - the client's `_framework`,
  `bit-bmage.js`, the editor's stylesheet - through the development manifest).
* `Infrastructure/DemoTest.cs` - the base class: a browser context per test, closed afterwards; `OpenAsync`, which
  waits for the layout's `data-interactive="true"` before handing the page over (the site is prerendered, so its
  HTML is on screen well before a single handler is attached); the sample upload; and loading it into an editor,
  which scrolls the editor to just under the sticky header so pointer-driven tests can reach every part of the
  canvas.
* `Infrastructure/SampleImage.cs` - the deterministic 800 x 600 gradient PNG every upload uses, built with the
  library's own encoder.
* `Infrastructure/PlaywrightSession.cs`, `ChildProcess.cs`, `RepoLayout.cs`, `E2EEnvironment.cs`, `DemoBuild.cs` -
  one browser for the run, process management, locating the demo, configuration, and the pre-run build.
