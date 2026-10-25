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
| `DustbinTests` | A box dropped on the dustbin is recorded, and the bin shows "Release to drop" in its hover colour while the box is over it; the hover survives the bin replacing the element under the pointer; leaving the bin clears the hover and a drop elsewhere records nothing (no `drop` event at all); a box lights up (dashed outline) only the bins that accept its type, dims the others, and drops into an accepting one; a bin that does not accept the type answers every `dragover` with the `none` effect, so the browser raises no drop there, while over no target at all the browser's default applies; the "move" box leaves the palette once a drop took it (`OnEnd` with `DidDrop`) while the "copy" box stays however often it is dropped, and both stay after drops outside the bin, and the bin grows with the list of what it took; the same demo works inside an iframe. |
| `TutorialTests` | A knight in the air lights exactly its legal squares green, the hovered square yellow when legal and red when not; a drop on an illegal square moves nothing (no `drop` event) and ends the drag; a drop on a legal square moves the knight, and ends the drag although the drop re-rendered the dragged knight out of the document first (the browser raised `dragend` on the detached element, where only the module's own listener hears it); the knight on its new square drags again, with its new legal squares. |
| `NestingTests` | Grabbing the inner, middle or outer of three nested sources drags that one only (one `dragstart`, one library tag on it); hovering the innermost of four nested targets paints it as the shallow one and its ancestors as "also over", backing out to an outer level un-hovers the inner ones, and leaving the nest clears them all; a drop runs every level's `OnDrop` innermost first, the outermost result reaching `OnEnd` - or, with greedy targets (`DidDrop()`), the innermost; a source that may not drag (`CanDrag`) hands the press to the one around it. |
| `DragAroundTests` | A dropped box moves by exactly the distance the pointer travelled since the `dragstart`; with a custom drag layer, the preview follows the pointer wherever it goes (drawn at the source's corner plus the travelled distance), disappears on drop, and the box lands where the preview was; snapped to the grid (`SnapTo`) the preview and the box move by whole cells, kept to an axis (`KeepTo`) the box keeps its height. |
| `CustomizeTests` | A box without a handle drags from anywhere on it; a box with a handle starts no drag at all when pressed on its text, and drags as a whole from its grip; a press in a text field inside a draggable selects the field's text and starts no drag - also the first press after a drag, which WebKit reports as a `pointermove` rather than a `pointerdown` - and text selected there drags as the field's own text, not as the box; a drag is tagged at `dragstart` with its source's id as both the type's suffix and its data, different for every source, and carries the source's `effectAllowed` (`copy`, `move`), which is the drop effect the browser shows over a target that takes it; the module keeps every drop from the browser. |
| `SortableTests` | Dragging a card past the middle of the next ones reorders the list while the card hovers, and a drop keeps the order; a card dropped outside the list goes back where it started (`OnEnd` with `DidDrop` false), while one dropped in a gap between cards (on the list itself) keeps its new place. |
| `NativeDragTests` | Files dragged in from the system light up the target and are listed with name, type and size on drop, with the browser kept from opening them; a file drag that leaves without dropping clears the hover; a link dragged from the page itself (a real browser drag) is read as HTML, text and URL, without the page navigating or opening a tab; dropped text is matched by its type, and a drag of a type the target does not list neither lights it up nor drops (the `none` effect); text dropped on a field inside a target is the field's, around it the target's. |
| `KeyboardTests` | Space picks a focused box up, the arrows move it onto the bin and Space (or Enter) drops it, with the live region announcing each step and the focus staying on the box; Escape, Tab and a pointer press cancel, and a mouse drag works right after; a hover-reordering sortable list moves one place per key with the focus following the card; a kanban card moves to another column, the new card element takes the focus over, a full column is skipped; the move buttons are a non-drag alternative; the provider's `Announcements` are what the live region says, and its `AllowKeyboard` takes the cards out of the tab order; a `Disabled` column is skipped. |
| `PointerTests` | A touch that rests on a box drags it onto the bin and a quick swipe drags nothing (touches synthesized through the DevTools protocol - Chromium only); a pen rests and drags the same way; the mouse drags a `PointerDrag="All"` box with a preview in the top layer and no native drag; Escape, a window losing the focus and a hidden page cancel, and the release clicks nothing - while a release the page never saw leaves the next click alone; a press in a no-drag region drags nothing; the tray scrolls near its edge and the drop lands on a slot scrolled into view; a click is not a drag. |
| `IndicatorTests` | A card over another reports the closest edge (`data-bit-bdnd-edge`), which follows the pointer, and only the target under the pointer shows one (Firefox raises no `dragleave` for an element whose line just appeared); drops land before or after, within a list and across lists, or at a list's end; horizontal labels likewise; the keyboard steps through the places between cards, skips the item's own place and stops at the ends; right to left, the left edge of a label is after it (`reversed`). |
| `TreeTests` | A folder node goes before a node, into a node (below its children), never into its own subtree; the keyboard moves a node into another. |
| `GridTests` | Tiles of a wrapping grid land left or right of a tile in another row; with the keyboard, Down and Up go a row and keep the column, the gap at a row break is the start of the next row, and Down on the last row goes nowhere. |
| `VirtualizedTests` | In a 1,000-row `Virtualize`: a pointer drag scrolls the list, keeps its copy (on the vertical axis) after the dragged row's component is gone, and drops on a row rendered on the way; a keyboard drag walks past the first rendered rows. |
| `MultiSelectTests` | A drag of a ticked card carries the whole selection (every ticked card shows as dragged), an unticked card drags alone, and the keyboard drags the selection; a drag layer says how many cards a drag carries, and the archive gives them back. |
| `PageBuilderTests` | Palette blocks are copied onto the page where the line shows (the palette keeps them) or at its end; page blocks move within the page and into the bin; the bin takes no palette block; the keyboard places a new block; a full page stops the palette from dragging (`CanDragPredicate`) until a block goes. |
| `FolderDropTests` | A dropped folder is walked into its files with their paths and contents (nested folders, an empty one, readEntries in batches), an unreadable entry is skipped and marks the list incomplete, and the walk stops at the target's file limit - a folder of exactly that many files is listed complete. A scripted drag carries no folders, so the page's `webkitGetAsEntry` answers with a tree of entries for placeholder files. |
| `DragEventsTests` | A drag's `OnDragStart`, `OnEnter`, `OnLeave`, `OnDrop` and `OnDragEnd` are logged with the input and the drop result, which undo reverses; a closed (`Disabled`) tray takes nothing; the drag state is read outside the provider through its `Manager`. |
| `ContextsTests` | A note lights up and drops on the bins of its own provider only - side by side and nested - and two providers sharing one manager are one context. |
| `ScreenReaderTests` | A keyboard source keeps its role and is described by the instructions; a handle is a focusable button described by them; the live region is an assertive, atomic status the moves are written to; the pointer copy adds nothing to the accessibility tree - on every engine, and in Chromium in the browser's own accessibility tree (`Accessibility.getPartialAXTree`). |
| `HybridHostingTests` | In a WinForms BlazorWebView (`Tests/Bit.Bdnd.Tests.Harness.Hybrid`), driven through WebView2's DevTools port: the page runs on the WebView renderer, and native mouse, keyboard, mouse pointer and touch drags drop; a drag layer follows the pointer; insertion edges reorder; a dropped file's contents reach .NET. Windows only, with the Chromium pass. |
| `ServerHostingTests` | On the page served over a Blazor Server circuit: native mouse, keyboard, pointer and touch drags drop, a pointer drag right after a native one too; a drag sends a handful of circuit messages however long it moves (fewer than 30 for 120 moves); a drag layer follows the pointer; insertion edges reorder; a dropped file's contents stream to the server; moving between that page and the WebAssembly ones loads the page in the other render mode. |
| `PerfTests` | Measurements only (category `Perf`): frames, the longest frame gap, long tasks and DOM mutations of a scripted drag across three stress pages, written to the test output. |
| `DemoPagesTests` | Every demo page (the routes are read from the `@page` directives under `Demo/Client/Pages`, so a new page is swept without being listed) answers, becomes interactive, loads `bit-bdnd.js` and registers every draggable with it when it has draggables, shows its own source under "View code", and runs every button on it without a console error, an unhandled exception, a failed request or the error banner; "View code" opens and closes; navigating through every page in the app leaves a page that still drags. |

No test is `[Ignore]`d. Every test, not only the page sweep, fails when its page logs a console error, throws an
unhandled exception or has a request fail.

Run against a published demo ([the publish gate](#publish-gate-trimming--aot)), the same tests prove all of it on the
WebAssembly client as it is deployed (trimmed), and AOT-compiled. Only there does an interop type the trimmer
stripped show up. System.Text.Json reads and writes the registrations' options and the callbacks' payloads through
reflection, so a constructor or property the trimmer removed makes every draggable and drop target throw as it
registers, and nothing drags at all. A build output is never trimmed, so it cannot show this. The roots that prevent
it are checked on every pull request by `BdndTrimmingTests` in `Bit.Bdnd.Tests`, which finds every type that crosses
the boundary in the library's compiled code.

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

The workflow `.github/workflows/bit.ci.Bdnd.e2e.yml` does the same in Release, and runs the
[publish gate](#publish-gate-trimming--aot) next to it. It is started by hand (`workflow_dispatch`) only, since the
suite takes far longer than the build-and-unit pipeline in `bit.ci.Bdnd.yml`:

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
in the Development environment so the build output finds its static web assets - or, with
`BDND_E2E_PUBLISHED_HOST`, from the publish output in Production, as it is deployed), waits until it answers, shares
it between all tests and stops it when the run ends. Each test gets a browser context of its own, in which every
request that leaves the demo's origin (its web font) is answered with an empty response, so a machine without
internet access runs the suite just the same. The tests run one at a time: a drag is a sequence of native events
whose handlers call into .NET between them, and a second page booting WebAssembly next to it is exactly the load
that turns those waits into flakes.

When no browser can be launched or the demo does not start, every test reports **Inconclusive** with the reason.
On a build server (`CI=true`) the same tests **fail** instead, so a suite that never ran cannot pass.

If the browser goes away in the middle of a run (WebKit on Windows has crashed under the suite's load), the next
test launches it again, so the crash fails the one test it happened in.

### Blazor Hybrid

`HybridHostingTests` starts `Tests/Bit.Bdnd.Tests.Harness.Hybrid` - a WinForms app with a `BlazorWebView`, built on
first use when its executable is missing (build it again after changing the library or the harness) - with
`--remote-debugging-port` and a user data folder of its own, which the app hands to WebView2 through its API (since
WebView2 Runtime 150 an elevated process ignores the port in `WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS`). Playwright
attaches with `ConnectOverCDPAsync`, adds the suite's probe to the page and reloads it before each test. It needs
Windows and the WebView2 Runtime (Windows 11 has it; the `e2e-hybrid` job installs it on its runner), and runs with
the Chromium pass only, since the WebView is Chromium whatever the run drives. It needs no demo and no downloaded
browser:

```bash
dotnet build Tests/Bit.Bdnd.Tests.Harness.Hybrid/Bit.Bdnd.Tests.Harness.Hybrid.csproj
dotnet test --project Tests/Bit.Bdnd.Tests.E2E/Bit.Bdnd.Tests.E2E.csproj --filter "FullyQualifiedName~HybridHostingTests"
```

## Checks a person makes before a release

The suite drives three engines, a Blazor Server circuit, a BlazorWebView and touch input through pointer events -
but not a screen reader's speech, nor a real finger. Before a release, someone runs these by hand and records the
outcome in `BDND-IMPLEMENTATION-LOG.md`.

**Screen readers** - NVDA with Firefox and with Chrome, JAWS with Chrome (Windows), VoiceOver with Safari (macOS),
VoiceOver with Safari (iOS), TalkBack with Chrome (Android), on the demo's
[keyboard and screen readers](https://bdnd.bitplatform.dev/accessibility/keyboard) page:

1. Tab to a card. Expected: its text is read, then the instructions ("To pick up, press Space or Enter...").
2. Press Space. Expected: "Picked up <card>. N drop targets are available." - read at once, not after other speech.
3. Press the arrows. Expected: each move is read ("<card> is over Doing."); at the end, "There is nothing further in
   that direction...".
4. Press Space over a column. Expected: "<card> was dropped on Doing.", and the focus is on the moved card (its text
   is read again on the next Tab cycle without a jump to the page's top).
5. Pick up again, press Escape. Expected: "Dragging <card> was cancelled.", the focus on the card.
6. Use the move buttons of a card. Expected: they are reachable by Tab and named ("Move <card> to Doing").
7. With the screen reader's own navigation (browse mode, rotor), no copy of a dragged card appears anywhere.

**Touch devices** - an iPhone and an iPad (Safari), an Android phone (Chrome), a Windows touch screen (Edge), and a
pen on the iPad or the Windows device, on the [touch and pointer drags](https://bdnd.bitplatform.dev/customize/pointer)
page and the [single target](https://bdnd.bitplatform.dev/dustbin/single) dustbin:

1. Swipe across the boxes. Expected: the page scrolls, nothing drags, no text is selected, no callout appears.
2. Rest a finger on a box, then move. Expected: a copy follows the finger, the bin lights up under it, a release on
   the bin drops; no context menu, no system drag of the element.
3. Hold a box near the tray's bottom edge. Expected: the tray scrolls, faster nearer the edge.
4. Start a drag and lift the finger off any target. Expected: nothing dropped, the copy disappears.
5. Rest on the "About" button inside a box and lift. Expected: the button clicks, nothing drags.
6. Repeat 2 with a pen. Expected: the same as a finger.

## Running locally

### Environment variables

| Variable | Effect |
|---|---|
| `BDND_E2E_CONFIGURATION` | Build configuration of the demo server to boot; defaults to the test assembly's own. |
| `BDND_E2E_PUBLISHED_HOST` | Boot a `dotnet publish` output of the demo server (see `publish-gate.sh`) instead of the build output. A full path: `dotnet test` runs the suite in its output folder, so a relative one is refused. Not together with `BDND_E2E_BASE_URL`. |
| `BDND_E2E_BASE_URL` | Drive an already running demo (for example one started from the IDE) instead of booting one. Not together with `BDND_E2E_PUBLISHED_HOST`. |
| `BDND_E2E_CHANNEL` / `BDND_E2E_EXECUTABLE` | Use a specific browser: an installed Chrome/Edge (`chrome`, `msedge`), Playwright's headless shell (`chromium-headless-shell`), or a Chromium binary. Without either, the full Chromium Playwright installed is used, then its headless shell, falling back to an installed Chrome, then Edge. |
| `BDND_E2E_BROWSER` | `chromium` (default), `firefox` or `webkit`: the engine to drive, from the browsers Playwright installed (`playwright.ps1 install firefox webkit`). The tests that synthesize touches or a pen through Chromium's DevTools protocol, read its accessibility tree or rely on its drag controller are inconclusive elsewhere; touch through pointer events runs on every engine. |
| `BDND_E2E_HEADED=1` | Show the browser. |

### Publish gate (trimming / AOT)

`publish-gate.sh` publishes the demo the way `bdnd.demo.cd.yml` deploys it - its WebAssembly client trimmed, and,
with the wasm-tools workload installed as the deployment has it, its runtime relinked - or, with
`-p:RunAOTCompilation=true`, AOT-compiled, which the deployed demo is not but an app using the library may be. It
fails on any trim/AOT analysis warning raised inside Bit.Bdnd: the trimmer's, and the trim and AOT analyzers' while
Bit.Bdnd compiles - the only place an AOT warning (IL3050) shows up, since the WebAssembly AOT compiler runs no
analysis of its own. It ignores the framework assemblies' own warnings and the demo's (`Bit.Bdnd.Demo.*`). The
publishing and the matching are `src/publish-gate.sh`'s, which every library's publish gate shares. The suite then
runs against that output. The analysis alone is not enough: JS interop takes its arguments as `object`, so the
trimmer raises no warning for a type it strips from under System.Text.Json, and only the browser run shows it.

A warning is reported only by the step that raises it, and a publish skips the steps whose output is up to date: CI
publishes from a clean checkout, and locally, after a Release build or an earlier gate run, clean first
(`dotnet clean Demo/Server/Bit.Bdnd.Demo.Server.csproj -c Release`, which cleans the client and Bit.Bdnd too).

```bash
# From src/Utilities/Bdnd (inside src, so src/global.json picks the SDK the wasm-tools workload is installed for):
bash Tests/Bit.Bdnd.Tests.E2E/publish-gate.sh ../../artifacts/bdnd-demo-trimmed
# AOT leg (needs the wasm-tools workload):
bash Tests/Bit.Bdnd.Tests.E2E/publish-gate.sh ../../artifacts/bdnd-demo-aot -p:RunAOTCompilation=true

dotnet build Tests/Bit.Bdnd.Tests.E2E/Bit.Bdnd.Tests.E2E.csproj
BDND_E2E_PUBLISHED_HOST=$PWD/../../artifacts/bdnd-demo-trimmed dotnet test --project Tests/Bit.Bdnd.Tests.E2E/Bit.Bdnd.Tests.E2E.csproj --no-build
```

The `publish-gate` job of `bit.ci.Bdnd.e2e.yml` runs both legs.
