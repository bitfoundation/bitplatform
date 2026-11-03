# Bit.Bink end-to-end tests

The engine and component tests in `Bit.Bink.Tests` run `BinkCanvas` without a browser: the document model,
the tools and the render commands are all checked in .NET, and JS interop is a test double. That covers the
engine well and leaves out everything that only exists in a real browser:

- `bit-bink.ts` never executes, so nothing proves that pointer events reach the engine, that the command
  buffer is painted onto a canvas, or that the backing store follows the device pixel ratio;
- there is no real event order: pointer capture, focus moving to a toolbar button before its click, the
  pointer events of a pen and a palm interleaving, a cancelled contact in the middle of a stroke;
- there is no WebAssembly runtime, so the zero-copy `[JSImport]` render path is never taken;
- the demo pages, which compose the canvas with real application code (diagrams, collaboration, document
  review), never run.

This suite boots the demo (`Bit.Bink.Demo.Server`, which prerenders the pages and runs them on WebAssembly)
and drives it with Playwright, in Chromium by default and in Firefox or WebKit when asked
(`BINK_E2E_BROWSER`). It asserts **pixels and effects**, not markup: ink is counted by reading the canvas back,
and positions are measured in backing-store pixels. One suite drives the published lazy loading sample
(`Samples/Lazy`) instead, an app that downloads `Bit.Bink.wasm` only when a page that draws opens.

## What it proves

| Suite | What it covers |
|---|---|
| `DrawingTests` | Mouse and pen strokes paint ink; pen pressure and tilt reach the engine and pressure changes the stroke width; undo/redo from the toolbar and from the keyboard; the text tool places a real, focused textarea and commits what is typed; the canvas describes its contents to assistive technology; pointer input does not re-render the Blazor component; WebAssembly takes the zero-copy render path. |
| `KeyboardAndTouchTests` | Arrow keys and Tab are left to the page while nothing is selected, and taken (nudge, delete) once something is; the canvas is never a keyboard trap; typing in child content is not hijacked; palm rejection while a pen is down; a second finger turns a stroke into a pan. |
| `DisplayAndDemoTests` | A device pixel ratio of 2 doubles the backing store and keeps ink under the pointer; resizing keeps the drawing in place; a cancelled unrelated pointer does not discard the stroke in progress; drawing at a zoomed viewport; the image cache never evicts an image still in use; navigating away and back leaves no surfaces behind; every demo page loads and every button on it runs without a console error; the whiteboard still draws after every toolbar control was used. |
| `DemoRegressionTests` | Defects found by driving the demo pages: an image of known size is drawn, text typed before a toolbar click is kept, a highlight and its description are one undo step, deleting a diagram node takes its connectors and label in one step, collaborators converge after one was offline, the first canvas shows its status line before anything is drawn, the status line follows a zoom from the keyboard, the whiteboard's align list applies the same alignment twice, undoing an opened picture names the picture that came back, a file that is not a picture is refused, a phone shows the whole picture to annotate, an emptied comment shows the text it keeps, drawing on a locked layer is refused and the page says why, the landing page's canvas does not ask for a desynchronized context (black inside its rounded frame on Windows Chromium), its ink follows the theme without a filter, including ink that undo puts back. |
| `DemoFeatureTests` | What the demo pages say they show, through their own controls: an imported SVG becomes editable elements and one undo takes it away, the picture to annotate hides and crops without losing its pixels, a collaborator who reloads joins again from a snapshot, a picture shown twice is saved once in a version 2 file, an application without the extensions keeps a custom element and brush and hands them back, a vote on a custom element is an undoable edit, a shortcut left to the page no longer reaches the canvas. |
| `EditingTests` | Selection, duplication, the lasso, wheel and pinch navigation; the system clipboard in both directions, including the picture a copy puts beside the drawing; pasted and dropped images; PNG export; the whiteboard kept in IndexedDB and offered back after a reload. |
| `AccessibilityTests` | axe-core's WCAG 2.1 A and AA rules on every demo page and on the whiteboard with the inline text editor open. A failure lists each rule broken and the elements that broke it. |
| `LazyLoadingTests` | The published lazy loading sample - trimmed, `Bit.Bink.wasm` marked `BlazorWebAssemblyLazyLoad` and loaded by the router's `OnNavigateAsync`, nothing of Bink's registered anywhere - as the README's "Lazy loading on WebAssembly" section sets it up: its home page never downloads `Bit.Bink.wasm`, yet Bink's stylesheet is already on it; navigating to the whiteboard downloads it once (and not again on the way back), and a deep link to the whiteboard is prerendered by the server (which resolves the router's `LazyAssemblyLoader`) and then downloads it; either way the canvas attaches, takes the zero-copy render path, draws a mouse stroke the page counts, and clears it from the page's own button - with no console error, unhandled exception or failed request. Published with AOT (`BINK_E2E_LAZY_AOT=1`), the sample leaves the lazy mark out as the README says an AOT build must, and the same tests check that it loads Bit.Bink with the app and nothing on demand. |

Every page is prerendered, so the canvas markup is on screen before anything listens to it. Each test waits
until the page runs in the browser (`data-interactive="true"` on the layout) and the browser layer has sized
every canvas, which is what says it is attached.

## Running locally

From `src/Utilities/Bink` (inside `src`, so `src/global.json` selects the Microsoft.Testing.Platform runner):

```bash
# The suite runs the demo server's build output and the lazy loading sample's publish output: build and publish them
# first, in the configuration the tests run in.
dotnet build Demo/Server/Bit.Bink.Demo.Server.csproj
dotnet publish Samples/Lazy/Bit.Bink.Samples.Lazy/Bit.Bink.Samples.Lazy.csproj -c Debug
dotnet build Tests/Bit.Bink.Tests.E2E/Bit.Bink.Tests.E2E.csproj

# Once per machine (and after a Playwright update): the browser Playwright drives.
pwsh Tests/Bit.Bink.Tests.E2E/bin/Debug/net10.0/playwright.ps1 install chromium

cd Tests/Bit.Bink.Tests.E2E && dotnet test

# The same suite in another engine, after installing it (playwright.ps1 install firefox webkit)
BINK_E2E_BROWSER=firefox dotnet test
BINK_E2E_BROWSER=webkit dotnet test

# One test class, or one test
dotnet test --filter "FullyQualifiedName~.KeyboardAndTouchTests."
dotnet test --filter "FullyQualifiedName~Drawing_with_the_mouse"
```

CI does the same in Release (`.github/workflows/bit.ci.Bink.e2e.yml`), against the demo published the way
`bink.demo.cd.yml` publishes it rather than a build output: only a publish trims the WebAssembly client, so an interop payload the trimmer breaks (an anonymous type whose
constructor lost its parameter names, a getter nothing but System.Text.Json reads) fails there and nowhere else:

```bash
dotnet publish src/Utilities/Bink/Demo/Server/Bit.Bink.Demo.Server.csproj -c Release -o demo -p:InvariantGlobalization=true
dotnet publish src/Utilities/Bink/Samples/Lazy/Bit.Bink.Samples.Lazy/Bit.Bink.Samples.Lazy.csproj -c Release
dotnet build src/Utilities/Bink/Tests/Bit.Bink.Tests.E2E/Bit.Bink.Tests.E2E.csproj -c Release
pwsh src/Utilities/Bink/Tests/Bit.Bink.Tests.E2E/bin/Release/net10.0/playwright.ps1 install --with-deps chromium
(cd demo && dotnet Bit.Bink.Demo.Server.dll --urls http://127.0.0.1:5000 &)
cd src/Utilities/Bink/Tests/Bit.Bink.Tests.E2E && BINK_E2E_BASE_URL=http://127.0.0.1:5000 dotnet test -c Release --no-build --report-trx --report-trx-filename bink-e2e.trx
```

That workflow only runs when started by hand (Actions > "bit platform CI - Bink E2E" > Run workflow), once per
engine: Chromium, Firefox and WebKit run as separate jobs, each with its own results. A fourth job,
`lazy-loading-aot`, publishes the lazy loading sample with `-p:RunAOTCompilation=true`, which leaves its lazy mark
out as the README says an AOT build must, and runs `LazyLoadingTests` against it in Chromium to check that Bit.Bink
then loads with the app and works all the same. To run that leg locally (it needs the wasm-tools workload):

```bash
dotnet publish src/Utilities/Bink/Samples/Lazy/Bit.Bink.Samples.Lazy/Bit.Bink.Samples.Lazy.csproj -c Release -p:RunAOTCompilation=true
cd src/Utilities/Bink/Tests/Bit.Bink.Tests.E2E && BINK_E2E_LAZY_AOT=1 dotnet test -c Release --no-build --filter "FullyQualifiedName~LazyLoadingTests"
```

The pull-request pipeline, `bit.ci.Bink.yml`, runs the build, engine, component and MCP tests, on .NET 10 and 11.

### What another engine cannot run

A few tests need something Playwright offers in Chromium alone. Each detects that from the browser itself, not
from its name, and reports **Inconclusive** with the reason, so a run in another engine never counts one as passed:

| Test | Firefox | WebKit |
|---|---|---|
| The three system clipboard tests | The clipboard permissions cannot be granted (`Unknown permission`). | The same, refused when the page opens. |
| `A_pasted_image_is_added_to_the_drawing` | A constructed paste event arrives with an empty copy of its data, so the file cannot be simulated. A real paste carries it. | Runs. |

Without PowerShell 7, the browser can be installed with the Node.js runtime Playwright ships:
`bin/Debug/net10.0/.playwright/node/<platform>/node bin/Debug/net10.0/.playwright/package/cli.js install chromium`.

The suite starts the demo server itself (`dotnet Bit.Bink.Demo.Server.dll --urls http://127.0.0.1:<free port>`,
in the Development environment so the build output finds its static web assets), waits until it answers,
shares it between all tests and stops it when the run ends. Each test gets a browser context of its own.
The tests run one at a time: most of them measure pixels a moment after a gesture, and a second page booting
WebAssembly next to them is exactly the load that turns those waits into flakes.

The lazy loading sample is started the same way, from its publish folder (`bin/<configuration>/net11.0/publish`) in
the Production environment, by the first test that drives it (`src/Utilities/Shared/Tests/E2E/LazyLoadingSample.cs`,
the harness every lazy-loadable library shares); `BINK_E2E_LAZY_BASE_URL` points those tests at a copy that is already
running. A sample that has not been published makes them inconclusive - or failed, on a build server - and leaves the
other suites alone. They still need the demo, which the session every test shares starts first.

When no browser can be launched or the demo does not start, every test reports **Inconclusive** with the
reason. On a build server (`CI=true`) the same tests **fail** instead, so a suite that never ran cannot pass.

### Environment variables

| Variable | Effect |
|---|---|
| `BINK_E2E_CONFIGURATION` | Build configuration of the demo server to boot; defaults to the test assembly's own. |
| `BINK_E2E_BASE_URL` | Drive an already running demo (for example one started from the IDE) instead of booting one. |
| `BINK_E2E_BROWSER` | The engine: `chromium` (the default), `firefox` or `webkit`, each the build Playwright installs. |
| `BINK_E2E_CHANNEL` / `BINK_E2E_EXECUTABLE` | Use an installed Chrome/Edge (`chrome`, `msedge`) or a specific browser binary of the engine chosen. Without either, the browser Playwright installed is used; for Chromium, falling back to an installed Chrome, then Edge. Channels apply to Chromium only. |
| `BINK_E2E_LAZY_BASE_URL` | Drive an already running lazy loading sample instead of booting its publish output. |
| `BINK_E2E_LAZY_AOT=1` | The lazy loading sample was published with `-p:RunAOTCompilation=true`, which leaves its lazy mark out: `LazyLoadingTests` then checks that Bit.Bink comes with the app and nothing is downloaded on demand. |
| `BINK_E2E_HEADED=1` | Show the browser. |
