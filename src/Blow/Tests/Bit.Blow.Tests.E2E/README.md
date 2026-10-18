# Bit.Blow end-to-end tests

The component tests in `Bit.Blow.Tests` run `BlowCanvas` without a browser: the graph model, the gestures, the
geometry and the render tree are all checked in .NET, and JS interop is a test double that hands back made-up
measurements. That covers the C# side well and leaves out everything that only exists in a real browser:

- `bit-blow.ts` never executes, so nothing proves that the module even loads, that nodes and handles are measured
  in flow units at whatever scale the viewport is painted at, that the handle under a point is found, or that a
  `ResizeObserver` reports the size changes nothing in C# can see;
- there is no real event order: pointer capture holding a drag that leaves the pane, focus moving to a node on
  press, a wheel that must be cancelled while it is still being dispatched so the page does not scroll, the
  `nodrag` / `nopan` / `nowheel` guards stopping an event before Blazor's dispatcher sees it;
- there is no layout, so edges are never drawn to measured handles and no stylesheet decides a colour mode;
- the demo pages, which compose the canvas with real application code, never run.

This suite boots the demo (`Bit.Blow.Demo.Server`, which prerenders the pages and runs them on WebAssembly) and
drives it in Chromium with Playwright. It asserts **effects**, not markup: where nodes landed on screen, the points
an edge's path really runs through, the transform the viewport is painted with, the colours the browser computed.

## What it proves

| Suite | What it covers |
|---|---|
| `RenderingTests` | Edges of the basic flow start and end on the centres of the measured handles, and the initial fit keeps every node in view; the browser layer measures nodes and handle offsets in flow units at a zoom it is not told about; it finds the handle under a point and nothing in a node's body or outside the pane; a container resized behind Blazor's back and a node whose content grows by itself are both picked up by the `ResizeObserver`, and the edge follows the grown node; dark and system colour modes apply the dark palette; two flows on one page are measured and steered independently. |
| `GestureTests` | Dragging a node moves it with the pointer and its edges follow; a drag keeps following the pointer outside the pane (pointer capture) and ends on release; dragging from a source handle to a target handle draws a connection line, marks valid, invalid and originating handles, and creates an edge between the two handles; a drop on empty space is abandoned; the wheel zooms about the cursor; dragging the background pans; a double click zooms in about the point; Shift-drag box-selects and Delete removes the nodes and their edges; clicking an edge selects it and Delete removes only it; the controls zoom in, zoom out and fit the view; the minimap mirrors the nodes and the visible area and a click on it brings a node to the middle; the node resizer resizes a selected node (its edge following the moved handle) and can keep the aspect ratio. |
| `KeyboardTests` | Tab reaches a node, Enter selects it, the arrow keys move it by the fine and the coarse step and announce it, without scrolling the page; Escape clears the selection; Escape abandons a drag and puts the node back; Backspace in a field inside a node edits the text instead of deleting the node, while Delete with the node itself focused does delete it. |
| `GuardTests` | A drag in a `nodrag` area works the control and leaves the node alone, while its title still drags it; a wheel over a `nowheel` element scrolls it instead of zooming the flow; dragging a `nopan` overlay does not pan; a wheel the flow uses never scrolls the page, while one over the controls does. |
| `DemoPagesTests` | Every demo page (the routes are read from the `@page` directives under `Demo/Client/Pages`, so a new page is swept without being listed) answers, becomes interactive, loads `bit-blow.js` when it has a flow, and runs every button on it without a console error, an unhandled exception, a failed request or the error banner; the examples gallery links to every example page; navigating through the demos in the app leaves a working canvas behind; "View code" shows the page's source. |

One test is `[Ignore]`d for a known library limitation, with the reason in its attribute:
`Clicking_the_minimap_centres_the_view_exactly_on_the_clicked_point` (the minimap maps a pointer by its nominal
size, which a host's `box-sizing: border-box` shrinks by its border).

Every page is prerendered, so the nodes and edges are on screen before anything listens to them, and before a
single node has been measured. Each test waits until the page runs in the browser (`data-interactive="true"` on
the layout) and the flows have settled (the measure and fit passes have rendered); tests that check anchoring wait
until every edge starts and ends on a handle, which only holds once the browser layer's measurements reached C#.

## Running locally

From `src/Blow` (inside `src`, so `src/global.json` selects the Microsoft.Testing.Platform runner):

```bash
# The suite runs the demo server's build output: build it first, in the configuration the tests run in.
dotnet build Demo/Server/Bit.Blow.Demo.Server.csproj
dotnet build Tests/Bit.Blow.Tests.E2E/Bit.Blow.Tests.E2E.csproj

# Once per machine (and after a Playwright update): the browser Playwright drives.
pwsh Tests/Bit.Blow.Tests.E2E/bin/Debug/net10.0/playwright.ps1 install chromium

cd Tests/Bit.Blow.Tests.E2E && dotnet test

# One test class, or one test
dotnet test --filter "FullyQualifiedName~.GestureTests."
dotnet test --filter "FullyQualifiedName~Dragging_a_node"
# Everything but the page sweep, which is most of the run time
dotnet test --filter "FullyQualifiedName!~Every_demo_page"
```

The workflow `.github/workflows/bit.ci.Blow.e2e.yml` does the same in Release. It is started by hand
(`workflow_dispatch`) only, since the suite takes far longer than the build-and-unit pipeline in
`bit.ci.Blow.yml`:

```bash
dotnet build src/Blow/Demo/Server/Bit.Blow.Demo.Server.csproj -c Release
dotnet build src/Blow/Tests/Bit.Blow.Tests.E2E/Bit.Blow.Tests.E2E.csproj -c Release
pwsh src/Blow/Tests/Bit.Blow.Tests.E2E/bin/Release/net10.0/playwright.ps1 install --with-deps chromium
cd src/Blow/Tests/Bit.Blow.Tests.E2E && dotnet test -c Release --no-build --report-trx --report-trx-filename blow-e2e.trx
```

Without PowerShell 7, the browser can be installed with Windows PowerShell
(`powershell -ExecutionPolicy Bypass -File bin/Debug/net10.0/playwright.ps1 install chromium`) or with the Node.js
runtime Playwright ships:
`bin/Debug/net10.0/.playwright/node/<platform>/node bin/Debug/net10.0/.playwright/package/cli.js install chromium`.

The suite starts the demo server itself (`dotnet Bit.Blow.Demo.Server.dll --urls http://127.0.0.1:<free port>`,
in the Development environment so the build output finds its static web assets), waits until it answers, shares
it between all tests and stops it when the run ends. Each test gets a browser context of its own, in which every
request that leaves the demo's origin (its web font) is answered with an empty response, so a machine without
internet access runs the suite just the same. The tests run one at a time: most of them measure where something
landed a moment after a gesture, and a second page booting WebAssembly next to them is exactly the load that turns
those waits into flakes.

When no browser can be launched or the demo does not start, every test reports **Inconclusive** with the reason.
On a build server (`CI=true`) the same tests **fail** instead, so a suite that never ran cannot pass.

### Environment variables

| Variable | Effect |
|---|---|
| `BLOW_E2E_CONFIGURATION` | Build configuration of the demo server to boot; defaults to the test assembly's own. |
| `BLOW_E2E_BASE_URL` | Drive an already running demo (for example one started from the IDE) instead of booting one. |
| `BLOW_E2E_CHANNEL` / `BLOW_E2E_EXECUTABLE` | Use an installed Chrome/Edge (`chrome`, `msedge`) or a specific Chromium binary. Without either, the Chromium Playwright installed is used, falling back to an installed Chrome, then Edge. |
| `BLOW_E2E_HEADED=1` | Show the browser. |
