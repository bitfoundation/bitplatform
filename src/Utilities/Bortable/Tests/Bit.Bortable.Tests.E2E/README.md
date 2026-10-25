# Bit.Bortable end-to-end tests

The component tests in `Bit.Bortable.Tests` run `Bortable<TItem>` without a browser: the sorting engine, the pull
and put rules, the clone, swap and multi-drag commits, the events and the render tree are all checked in .NET, with
drag events raised by hand and a JS interop double that hands back made-up drop positions. That covers the C# side
well and leaves out everything that only exists in a real browser:

- `bit-bortable.ts` never executes, so nothing proves that the module even loads, that the drop position it measures
  against the items' boxes (the SortableJS swap thresholds, the inverted zones, the gaps between items, a grid's rows)
  is the one a user gets, that a drag handle really decides on `pointerdown` whether the browser may start a drag, or
  that an item is refused by a list nested inside it;
- there is no native drag: no `dragstart` that the browser only fires for a `draggable` element after the pointer has
  moved, no stream of `dragover` events racing the .NET handlers that measure each of them, no `drop` or `dragend`
  deciding how a drag ends, and no Blazor re-render moving an item's DOM node while the browser is still dragging it;
- there is no layout, so the FLIP animation never slides anything, no stylesheet hides an item previewed in another
  list, and the swap and selection states are never drawn;
- the demo pages, which compose the lists with real application code (bound readouts, an event log, a recursive tree),
  never run.

This suite boots the demo (`Bit.Bortable.Demo.Server`, which prerenders the pages and runs them on WebAssembly) and
drives it with Playwright, with real mouse drags - in Chromium by default, and in Firefox or WebKit on request
(`BORTABLE_E2E_BROWSER`, below). It asserts **effects**: the order readouts the demo binds
to each list (what .NET holds), the items of each list in DOM order (what the browser shows), the placeholder and the
classes the drag states put on the items mid-drag, and the lines of the event log.

## What it proves

| Suite | What it covers |
|---|---|
| `SortingTests` | Dragging an item down or up reorders the list live under the pointer (the dragged item wearing the ghost and chosen classes) and the bound list takes the new order only when the drag ends; successive drags keep sorting; a CSS grid reorders across rows in both directions; a drag that runs down the whole list and back starts every slide while the previous ones are still running, and the items still end in the bound order, each in its slot, with no animation style left behind; with a small `SwapThreshold` the edge of an item is a dead zone while its middle swaps, and with the full threshold the same edge swaps; `InvertSwap` makes the middle of an item dead and its far edge sort the dragged item past it. |
| `SharedListTests` | Dragging into another list of the group previews a placeholder (the item's own template, with the ghost class) where it would land while the original is hidden at home and both bound lists are untouched, and the drop moves it there; an item moved to the other list keeps its own `ItemClassSelector` class; taking the item back home before the drop removes the preview and cancels the move; releasing outside every list after a preview keeps the move (the SortableJS behaviour); items go back and forth, to the top and to the end of a list. |
| `CloningTests` | With `Pull="Clone"` the target gains a copy while the original stays visible at home (even mid-drag) and the source never changes; the same item can be cloned again, into another place. |
| `DisableSortTests` | A list with `Sort="false"` does not reorder while one of its items is dragged over it, yet the item can still be dragged out; a list with `Put="false"` shows no placeholder for an item from the other list and refuses it. |
| `HandleAndFilterTests` | With a `Handle`, dragging a row by its body starts no drag at all, dragging it by its handle sorts the list, and the row is undraggable again afterwards; an item excluded by the `Filter` cannot be dragged, while the other items sort past it in both directions. |
| `NestedTests` | An item dropped into the empty list of another item becomes its child; an item moves between branches to a given place among the children, and a whole branch moves with its children (the list it holds follows it); an item dragged into a list inside itself is refused while the drag runs (no placeholder anywhere) and the tree is unchanged. |
| `EventsTests` | A drag within a list logs `OnStart` when it begins and `OnUpdate`, `OnSort` and `OnEnd`, with the old and new index, when it is dropped; a drag across lists logs `OnStart`, then `OnRemove`, `OnAdd`, `OnSort` on both lists and `OnEnd` with the source and destination; a drag taken back home logs only `OnStart` and `OnEnd`; `OnChange` is logged on request, ending at the position the item is dropped in, and the log clears; a keyboard drag cancelled with Escape logs `OnStart` and an `OnEnd` that says `keyboard` and `canceled`. |
| `PluginTests` | MultiDrag: a click toggles an item's selection; dragging a selected item moves the whole selection as a block, in its original order, within a list and into another list, and spends the selection; dragging an unselected item drops the selection and moves only that item. Swap: the item under the pointer is highlighted (and only that one, following the pointer) while nothing moves, the drop trades the two items' places within a list and across lists, and a drag released with no target highlighted changes nothing. |
| `BrowserLayerTests` | `bit-bortable.js` called directly, on lists built for the test: a list with a single item sorts along a flex row, down a flex column, and along the row an inline or floated item makes; right-to-left rows and reversed flex directions are measured in their own order; only a drag of the lists' own, in progress, is allowed to drop - and only on a list that takes it - while dragged text keeps its default; a drag started inside an item that is not draggable itself reaches the app's listeners unmarked; an invalid handle selector starts no drag and throws nothing. |
| `KeyboardTests` | With the real keyboard: a list is one tab stop and the arrows move the focus; Space picks an item up, the arrows move it live, Space drops it, with the focus following the item and every step read out by the live region; keys pressed faster than the steps come back (and while the last one still slides) each move the item one place; Escape puts it back; an arrow past the list carries the item into the list beside it, and a cancel brings it home; keys pressed on a control inside an item are left to it. |
| `PointerTests` | Drags without the browser's drag and drop: real touch events (CDP) - a long press picks an item up and a finger drags it, within a list and into another, while a swipe drags nothing; a handle with no touch delay drags at once, the row outside it does not; a pen (CDP pen events) drags without the long press; the mouse with `ForceFallback` drags a preview and starts no native drag; Escape puts a pointer drag back. |
| `RulesTests` | The rules page: a column `CanDrop` refuses is marked refused and a release there puts the card back (`RevertOnSpill`); a full column (`MaxItems`) does not take the drag; a drop the server refuses (`ConfirmDrop`) slides back and an approved one stays. The accessibility page's buttons move items without dragging and are announced. |
| `LargeListTests` | Three hundred rows in a scrolling panel: a pointer drag held at the panel's edge scrolls it and carries the row along; a drag of 60 pointer moves sends one position per row crossed to .NET, with and without `IsolateItems` (the counts and round-trip times go to the test output). |
| `DemoPagesTests` | Every demo page (the routes are read from the `@page` directives under `Demo/Client/Pages`, so a new page is swept without being listed) answers, becomes interactive, loads `bit-bortable.js` and applies `bit-bortable.css` when it has lists, shows its own source (every file of it) in the "View code" panel and closes it again, without a console error, an unhandled exception, a failed request or the error banner; an unknown address answers 404 with the not-found page; navigating through every demo in the app leaves lists behind that still sort, the drag handle included. |

Every page is prerendered, so its lists are on screen before anything listens to them. Each test waits until the
page runs in the browser (`data-interactive="true"` on the layout) and the lists have imported `bit-bortable.js`,
without which every `dragover` is answered with "keep the current order".

### How a drag is driven

`BortableTest` drives each drag the way a hand does: press on the item, move a few pixels (Chromium starts a native
drag, and fires `dragstart`, only once the pointer has moved more than 4 pixels with the button held), move to the
target in steps, release. Two things make the outcome deterministic:

- **Settling.** A hand's browser repeats `dragover` every 50 ms while the pointer rests, but under Playwright Chromium
  fires it only when the pointer moves, and the hit test ignores an item still sliding into place. The browser layer
  keeps the latest position while a call to .NET is in flight and sends it once the call returns, but a position
  measured mid-slide is not acted on. So after every move the pointer is nudged a pixel off and back, each time once
  the page has gone quiet (the 150 ms slides included), until a nudge changes nothing: every list has then acted on
  where the pointer is. No test sleeps for a fixed time, except to hold a long press.
- **Aiming at slots.** The demo's items are all the same size, so wherever the pointer comes to rest, the dragged item
  (or its placeholder in another list) ends up in the slot under it: a drag that ends on B3 lands where B3 was, and
  pushes B3 down. The nested tree is the exception, as its items are not the same size and every list in it lies
  inside the items of the lists around it; its tests move the pointer to the target in one jump.

## Running locally

From `src/Utilities/Bortable` (inside `src`, so `src/global.json` selects the Microsoft.Testing.Platform runner):

```bash
# The suite runs the demo server's build output: build it first, in the configuration the tests run in.
dotnet build Demo/Server/Bit.Bortable.Demo.Server.csproj
dotnet build Tests/Bit.Bortable.Tests.E2E/Bit.Bortable.Tests.E2E.csproj

# Once per machine (and after a Playwright update): the browser Playwright drives.
pwsh Tests/Bit.Bortable.Tests.E2E/bin/Debug/net10.0/playwright.ps1 install chromium

cd Tests/Bit.Bortable.Tests.E2E && dotnet test

# One test class, or one test
dotnet test --filter "FullyQualifiedName~.SharedListTests."
dotnet test --filter "FullyQualifiedName~Dragging_from_A_into_B"
# Everything but the page sweep
dotnet test --filter "FullyQualifiedName!~Every_demo_page"
# In Firefox, or WebKit (after 'playwright.ps1 install firefox webkit')
BORTABLE_E2E_BROWSER=firefox dotnet test
```

The built test assembly is also an executable test host, which takes the same options:
`dotnet bin/Debug/net10.0/Bit.Bortable.Tests.E2E.dll --filter "FullyQualifiedName~.PluginTests."`.

The workflow `.github/workflows/bit.ci.Bortable.e2e.yml` does the same in Release. It is started by hand
(`workflow_dispatch`) only, since the suite takes far longer than the build-and-unit pipeline in
`bit.ci.Bortable.yml`:

```bash
dotnet build src/Utilities/Bortable/Demo/Server/Bit.Bortable.Demo.Server.csproj -c Release
dotnet build src/Utilities/Bortable/Tests/Bit.Bortable.Tests.E2E/Bit.Bortable.Tests.E2E.csproj -c Release
pwsh src/Utilities/Bortable/Tests/Bit.Bortable.Tests.E2E/bin/Release/net10.0/playwright.ps1 install --with-deps chromium
cd src/Utilities/Bortable/Tests/Bit.Bortable.Tests.E2E && dotnet test -c Release --no-build --report-trx --report-trx-filename bortable-e2e.trx
```

Without PowerShell 7, the browser can be installed with Windows PowerShell
(`powershell -ExecutionPolicy Bypass -File bin/Debug/net10.0/playwright.ps1 install chromium`) or with the Node.js
runtime Playwright ships:
`bin/Debug/net10.0/.playwright/node/<platform>/node bin/Debug/net10.0/.playwright/package/cli.js install chromium`.

The suite starts the demo server itself (`dotnet Bit.Bortable.Demo.Server.dll --urls http://127.0.0.1:<free port>`,
in the Development environment so the build output finds its static web assets), waits until it answers, shares
it between all tests and stops it when the run ends. To drive a demo that is already running instead - one started
from the IDE, or a published copy (`dotnet publish` output, started with `dotnet Bit.Bortable.Demo.Server.dll --urls
http://127.0.0.1:5563`) - point `BORTABLE_E2E_BASE_URL` at it; nothing is then built or booted for the run.

Each test gets a browser context of its own, 1280 x 1400 so that every demo page fits without scrolling (a drag
cannot be aimed below the fold), in which every request that leaves the demo's origin (its web font) is answered with
an empty response, so a machine without internet access runs the suite just the same. The tests run one at a time:
each of them races .NET handlers against the browser's drag events, and a second page booting WebAssembly next to
them is exactly the load that turns those races into flakes.

When no browser can be launched or the demo does not start, every test reports **Inconclusive** with the reason.
On a build server (`CI=true`) the same tests **fail** instead, so a suite that never ran cannot pass.

### Environment variables

| Variable | Effect |
|---|---|
| `BORTABLE_E2E_CONFIGURATION` | Build configuration of the demo server to boot; defaults to the test assembly's own. |
| `BORTABLE_E2E_BASE_URL` | Drive an already running demo (for example one started from the IDE, or a published copy) instead of booting one. |
| `BORTABLE_E2E_BROWSER` | The engine: `chromium` (the default), `firefox` or `webkit`, each the one Playwright installed (`playwright.ps1 install firefox webkit`). The touch and pen tests send their input through the Chrome DevTools Protocol, so they report **Inconclusive** outside Chromium. |
| `BORTABLE_E2E_CHANNEL` / `BORTABLE_E2E_EXECUTABLE` | Use an installed Chrome/Edge (`chrome`, `msedge`) or a specific browser binary (of the `BORTABLE_E2E_BROWSER` engine). Without either, the Chromium Playwright installed is used, falling back to an installed Chrome, then Edge. |
| `BORTABLE_E2E_HEADED=1` | Show the browser. |
