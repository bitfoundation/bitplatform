# Bit.Bdnd end-to-end tests

The component tests in `Bit.Bdnd.Tests` run the draggables, drop targets and drag layers without a browser: the
manager's state machine, the collected states, the callbacks and their order are all checked in .NET, and the browser
module is a test double the tests call into by hand. That covers the C# side well and leaves out everything that only
exists in a real browser:

- `bit-bdnd.ts` never executes, so nothing proves that the module even loads, that it tags a `dragstart` with the
  source's id (`application/x-bit-bdnd-<id>`) while the browser still lets it write to the `DataTransfer`, that only
  the innermost of nested sources claims it, or that a drag handle is the only place a drag may start;
- there is no native event order: `dragenter` and `dragleave` for every element the pointer crosses, `dragover` only
  for the moves that stay on one, a `drop` that the browser raises only where the last answer was "accepted", and a
  `dragend` raised on the source even when a drop has re-rendered it out of the document;
- the drop-effect feedback (`preventDefault` and `dropEffect` on every `dragover`) and the hover tracking that has
  to survive Blazor replacing the element under the pointer are never exercised;
- a native drag - a link from the page, files or text from the operating system - is never read from a live
  `DataTransfer`;
- the demo pages, which compose the components with real application code, never run.

This suite boots the demo (`Bit.Bdnd.Demo.Server`, which prerenders the pages and runs them on WebAssembly) and
drives it in Chromium with Playwright, whose mouse Chromium turns into real HTML5 drags. It asserts **effects**, not
markup: what a page shows and the colours the browser computed while a drag hovers and after it drops, where a dropped
box landed on screen, and the drag events as the browser raised them and the module answered them.

## What it proves

| Suite | What it covers |
|---|---|
| `DustbinTests` | A box dropped on the dustbin is recorded, and the bin shows "Release to drop" in its hover colour while the box is over it; the hover survives the bin replacing the element under the pointer; leaving the bin clears the hover and a drop elsewhere records nothing (no `drop` event at all); a box lights up (dashed outline) only the bins that accept its type, dims the others, and drops into an accepting one; a bin that does not accept the type answers every `dragover` with the `none` effect, so the browser raises no drop there, while over no target at all the browser's default applies; the "move" box leaves the palette once a drop took it (`OnEnd` with `DidDrop`) while the "copy" box stays however often it is dropped, and both stay after drops outside the bin; the same demo works inside an iframe. |
| `TutorialTests` | A knight in the air lights exactly its legal squares green, the hovered square yellow when legal and red when not; a drop on an illegal square moves nothing (no `drop` event) and ends the drag; a drop on a legal square moves the knight, and ends the drag although the drop re-rendered the dragged knight out of the document first (the browser raised `dragend` on the detached element, where only the module's own listener hears it); the knight on its new square drags again, with its new legal squares. |
| `NestingTests` | Grabbing the inner, middle or outer of three nested sources drags that one only (one `dragstart`, one library tag on it); hovering the innermost of four nested targets paints it as the shallow one and its ancestors as "also over", backing out to an outer level un-hovers the inner ones, and leaving the nest clears them all. |
| `DragAroundTests` | A dropped box moves by exactly the distance the pointer travelled since the `dragstart`; with a custom drag layer, the preview follows the pointer wherever it goes (drawn at the source's corner plus the travelled distance), disappears on drop, and the box lands where the preview was. |
| `CustomizeTests` | A box without a handle drags from anywhere on it; a box with a handle starts no drag at all when pressed on its text, and drags as a whole from its grip; a drag is tagged at `dragstart` with its source's id as both the type's suffix and its data, different for every source, and carries the source's `effectAllowed` (`copy`, `move`), which is the drop effect the browser shows over a target that takes it; the module keeps every drop from the browser. |
| `SortableTests` | Dragging a card past the middle of the next ones reorders the list while the card hovers, and a drop keeps the order; a card dropped outside the list goes back where it started (`OnEnd` with `DidDrop` false), while one dropped in a gap between cards (on the list itself) keeps its new place. |
| `NativeDragTests` | Files dragged in from the system light up the target and are listed with name, type and size on drop, with the browser kept from opening them; a file drag that leaves without dropping clears the hover; a link dragged from the page itself (a real browser drag) is read as HTML, text and URL, without the page navigating or opening a tab; dropped text is matched by its type, and a drag of a type the target does not list neither lights it up nor drops (the `none` effect). |
| `DemoPagesTests` | Every demo page (the routes are read from the `@page` directives under `Demo/Client/Pages`, so a new page is swept without being listed) answers, becomes interactive, loads `bit-bdnd.js` and registers every draggable with it when it has draggables, shows its own source under "View code", and runs every button on it without a console error, an unhandled exception, a failed request or the error banner; "View code" opens and closes; navigating through every page in the app leaves a page that still drags. |

No test is `[Ignore]`d. Every test, not only the page sweep, fails when its page logs a console error, throws an
unhandled exception or has a request fail.

Drags from outside the browser cannot be started with a mouse, so the file and text drops raise `dragenter`,
`dragover`, `drop` and `dragleave` themselves, on the target, with a `DataTransfer` created in the page - the events
then run through the module and Blazor exactly as the operating system's would. A link, by contrast, is dragged for
real.

Every page is prerendered, so its boxes and bins are on screen before anything listens to them, and a draggable only
starts working once the module has registered it, asynchronously after the render that created it. Each test waits
until the page runs in the browser (`data-interactive="true"` on the layout) and, before every drag, until every
draggable on the page is registered and registrations have settled (`BdndTest.WaitForBdndAsync`, which counts the
element-level listeners the module adds). Where a test checks how the module answered a `dragover`, it waits for the
event the browser raised at the very point the pointer was moved to (`BdndTest.DragOverAtAsync`).

## Running locally

From `src/Utilities/Bdnd` (inside `src`, so `src/global.json` selects the Microsoft.Testing.Platform runner):

```bash
# The suite runs the demo server's build output: build it first, in the configuration the tests run in.
dotnet build Demo/Server/Bit.Bdnd.Demo.Server.csproj
dotnet build Tests/Bit.Bdnd.Tests.E2E/Bit.Bdnd.Tests.E2E.csproj

# Once per machine (and after a Playwright update): the browser Playwright drives.
pwsh Tests/Bit.Bdnd.Tests.E2E/bin/Debug/net10.0/playwright.ps1 install chromium

cd Tests/Bit.Bdnd.Tests.E2E && dotnet test

# One test class, or one test
dotnet test --filter "FullyQualifiedName~.SortableTests."
dotnet test --filter "FullyQualifiedName~Dropping_the_knight"
# Everything but the page sweep
dotnet test --filter "FullyQualifiedName!~Every_demo_page"
```

The workflow `.github/workflows/bit.ci.Bdnd.e2e.yml` does the same in Release. It is started by hand
(`workflow_dispatch`) only, since the suite takes far longer than the build-and-unit pipeline in
`bit.ci.Bdnd.yml`:

```bash
dotnet build src/Utilities/Bdnd/Demo/Server/Bit.Bdnd.Demo.Server.csproj -c Release
dotnet build src/Utilities/Bdnd/Tests/Bit.Bdnd.Tests.E2E/Bit.Bdnd.Tests.E2E.csproj -c Release
pwsh src/Utilities/Bdnd/Tests/Bit.Bdnd.Tests.E2E/bin/Release/net10.0/playwright.ps1 install --with-deps chromium
cd src/Utilities/Bdnd/Tests/Bit.Bdnd.Tests.E2E && dotnet test -c Release --no-build --report-trx --report-trx-filename bdnd-e2e.trx
```

Without PowerShell 7, the browser can be installed with Windows PowerShell
(`powershell -ExecutionPolicy Bypass -File bin/Debug/net10.0/playwright.ps1 install chromium`) or with the Node.js
runtime Playwright ships:
`bin/Debug/net10.0/.playwright/node/<platform>/node bin/Debug/net10.0/.playwright/package/cli.js install chromium`.

`install chromium` downloads both the full Chromium build and Playwright's headless shell. The suite prefers the
full build, in its new headless mode (the `chromium` channel), because that is the code a person's Chrome runs; it
falls back to the headless shell, where it passes as well.

The suite starts the demo server itself (`dotnet Bit.Bdnd.Demo.Server.dll --urls http://127.0.0.1:<free port>`,
in the Development environment so the build output finds its static web assets), waits until it answers, shares
it between all tests and stops it when the run ends. Each test gets a browser context of its own, in which every
request that leaves the demo's origin (its web font) is answered with an empty response, so a machine without
internet access runs the suite just the same. The tests run one at a time: a drag is a sequence of native events
whose handlers call into .NET between them, and a second page booting WebAssembly next to it is exactly the load
that turns those waits into flakes.

When no browser can be launched or the demo does not start, every test reports **Inconclusive** with the reason.
On a build server (`CI=true`) the same tests **fail** instead, so a suite that never ran cannot pass.

### Environment variables

| Variable | Effect |
|---|---|
| `BDND_E2E_CONFIGURATION` | Build configuration of the demo server to boot; defaults to the test assembly's own. |
| `BDND_E2E_BASE_URL` | Drive an already running demo (for example one started from the IDE) instead of booting one. |
| `BDND_E2E_CHANNEL` / `BDND_E2E_EXECUTABLE` | Use a specific browser: an installed Chrome/Edge (`chrome`, `msedge`), Playwright's headless shell (`chromium-headless-shell`), or a Chromium binary. Without either, the full Chromium Playwright installed is used, then its headless shell, falling back to an installed Chrome, then Edge. |
| `BDND_E2E_HEADED=1` | Show the browser. |
