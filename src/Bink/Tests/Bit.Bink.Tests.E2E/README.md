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
and drives it in Chromium with Playwright. It asserts **pixels and effects**, not markup: ink is counted by
reading the canvas back, and positions are measured in backing-store pixels.

## What it proves

| Suite | What it covers |
|---|---|
| `DrawingTests` | Mouse and pen strokes paint ink; pen pressure and tilt reach the engine and pressure changes the stroke width; undo/redo from the toolbar and from the keyboard; the text tool places a real, focused textarea and commits what is typed; the canvas describes its contents to assistive technology; pointer input does not re-render the Blazor component; WebAssembly takes the zero-copy render path. |
| `KeyboardAndTouchTests` | Arrow keys and Tab are left to the page while nothing is selected, and taken (nudge, delete) once something is; the canvas is never a keyboard trap; typing in child content is not hijacked; palm rejection while a pen is down; a second finger turns a stroke into a pan. |
| `DisplayAndDemoTests` | A device pixel ratio of 2 doubles the backing store and keeps ink under the pointer; resizing keeps the drawing in place; a cancelled unrelated pointer does not discard the stroke in progress; drawing at a zoomed viewport; the image cache never evicts an image still in use; navigating away and back leaves no surfaces behind; every demo page loads and every button on it runs without a console error; the whiteboard still draws after every toolbar control was used. |
| `DemoRegressionTests` | Defects found by driving the demo pages: an image of known size is drawn, text typed before a toolbar click is kept, a highlight and its description are one undo step, deleting a diagram node takes its connectors and label in one step, collaborators converge after one was offline. |

Every page is prerendered, so the canvas markup is on screen before anything listens to it. Each test waits
until the page runs in the browser (`data-interactive="true"` on the layout) and the browser layer has sized
every canvas, which is what says it is attached.

## Running locally

From `src/Bink` (inside `src`, so `src/global.json` selects the Microsoft.Testing.Platform runner):

```bash
# The suite runs the demo server's build output: build it first, in the configuration the tests run in.
dotnet build Bit.Bink.Demo/Server/Bit.Bink.Demo.Server.csproj
dotnet build Tests/Bit.Bink.Tests.E2E/Bit.Bink.Tests.E2E.csproj

# Once per machine (and after a Playwright update): the browser Playwright drives.
pwsh Tests/Bit.Bink.Tests.E2E/bin/Debug/net10.0/playwright.ps1 install chromium

cd Tests/Bit.Bink.Tests.E2E && dotnet test

# One test class, or one test
dotnet test --filter "FullyQualifiedName~.KeyboardAndTouchTests."
dotnet test --filter "FullyQualifiedName~Drawing_with_the_mouse"
```

CI does the same in Release (`.github/workflows/bit.ci.Bink.e2e.yml`):

```bash
dotnet build src/Bink/Bit.Bink.Demo/Server/Bit.Bink.Demo.Server.csproj -c Release
dotnet build src/Bink/Tests/Bit.Bink.Tests.E2E/Bit.Bink.Tests.E2E.csproj -c Release
pwsh src/Bink/Tests/Bit.Bink.Tests.E2E/bin/Release/net10.0/playwright.ps1 install --with-deps chromium
cd src/Bink/Tests/Bit.Bink.Tests.E2E && dotnet test -c Release --no-build --report-trx --report-trx-filename bink-e2e.trx
```

That workflow only runs when started by hand (Actions > "bit platform CI - Bink E2E" > Run workflow). The pull-request pipeline, `bit.ci.Bink.yml`, runs the build, engine, component and MCP tests only.

Without PowerShell 7, the browser can be installed with the Node.js runtime Playwright ships:
`bin/Debug/net10.0/.playwright/node/<platform>/node bin/Debug/net10.0/.playwright/package/cli.js install chromium`.

The suite starts the demo server itself (`dotnet Bit.Bink.Demo.Server.dll --urls http://127.0.0.1:<free port>`,
in the Development environment so the build output finds its static web assets), waits until it answers,
shares it between all tests and stops it when the run ends. Each test gets a browser context of its own.
The tests run one at a time: most of them measure pixels a moment after a gesture, and a second page booting
WebAssembly next to them is exactly the load that turns those waits into flakes.

When no browser can be launched or the demo does not start, every test reports **Inconclusive** with the
reason. On a build server (`CI=true`) the same tests **fail** instead, so a suite that never ran cannot pass.

### Environment variables

| Variable | Effect |
|---|---|
| `BINK_E2E_CONFIGURATION` | Build configuration of the demo server to boot; defaults to the test assembly's own. |
| `BINK_E2E_BASE_URL` | Drive an already running demo (for example one started from the IDE) instead of booting one. |
| `BINK_E2E_CHANNEL` / `BINK_E2E_EXECUTABLE` | Use an installed Chrome/Edge (`chrome`, `msedge`) or a specific Chromium binary. Without either, the Chromium Playwright installed is used, falling back to an installed Chrome, then Edge. |
| `BINK_E2E_HEADED=1` | Show the browser. |
