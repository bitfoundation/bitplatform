# Bit.Bheadless end-to-end tests

The component, keyboard and ARIA tests in `Bit.Bheadless.Tests` render the components with bUnit, without a
browser: `bit-bheadless.js` never runs, there is no layout, no real focus, no `inert`, no scroll and no
WebAssembly runtime. This suite boots the demo site (`Bit.Bheadless.Demo.Server`, which prerenders every page
and runs it on WebAssembly) and drives it with Playwright in Chromium.

## What it proves

| Suite | What it covers |
|---|---|
| `AccessibilityTests` | Every page of the demo's catalogue (`DemoCatalog` in `Demo/Client/.../Shared/DemoSection.cs`, compiled into this project), in both styling sections - `/<slug>` (plain CSS) and `/tw/<slug>` (Tailwind) - passes axe-core over the page's content - `.bb-docs-content`, the docs layout's content column, without the site's navigation, breadcrumb, pager and rail (tags `wcag2a`, `wcag2aa`, `wcag21a`, `wcag21aa`, `wcag22aa`, `best-practice`) in its initial, interactive state, and reports no script error. One test per page per section. |
| `KeyboardTests` | The flows only a browser can verify, with axe-core run again on the open state of every overlay: the dialog traps the focus, makes the page inert, locks the scroll and gives the focus back; the menu opens with `ArrowDown`, moves its active item and returns the focus on `Escape`; the listbox and the combobox move their active option and select with `Enter`; tabs move the selection without scrolling the page; the popover and the alert dialog close on `Escape` and restore the focus, the alert dialog ignoring outside clicks; the tooltip shows on hover and hides on `Escape`; radio group arrows move the focus and check; a checkbox toggles with `Space` and not `Enter`; tree arrows move the focus and `ArrowRight` expands; the command palette opens with `Ctrl+K`, filters and restores the focus; slider arrows, `Home` and `End` set the value; sortable keyboard reordering is announced in the live region; toasts are announced and `F8` moves the focus to them. |
| `PointerAndScrollTests` | What the runtime decides from layout, pointer presses and the browser's own focus handling: `End` scrolls the last option of a virtualized list fully into view, and so do `End`, `Home` and `Page Down` in a list of options of different heights, whose view holds still as the options are measured; a query typed into a scrolled combobox shows the option it highlights; a press on a checkable menu item keeps the keys on the menu (no page scroll); typeahead reaches a checked menu item (its `aria-hidden` mark is not its text); a submenu with no room on either side stays on a 390px screen; after a re-sort the grid's arrow keys follow the new row order; a press on a range slider's track focuses the thumb that moved; and, with presses made to behave as in Safari (which focuses no button it clicks), a dialog gives the focus back to the pressed button and a second press on a popover's trigger closes it. |
| `SortableTests` | Items moved between the lists of a sortable group, on the sortable page's kanban example: a pointer drag leaves its column, marks the columns that take the card (`data-drop-target`) and the one under the pointer (`data-drag-over`) and lands where the column made room - an empty column too, and the column the pointer already rests over when .NET names the targets, let go without another move - while a column that refuses the card is no target; with the keyboard, the arrows across a column move a picked-up card to the next one, each step announced in the live region with the column and the position and the focus following the card, `Escape` putting it back and axe-core finding nothing mid-move. |
| `MenuHoverTests` | Menus that open on hover (`OpenOnHover`): the hover opens the menu after its delay without moving the focus, and the first key press - with nothing focused - moves the focus in and acts there; the pointer crossing the gap to the menu diagonally keeps it open, and leaving closes it after its delay without pulling the focus to the trigger; a click on the trigger keeps the menu open and gives it the focus; once a menu of a bar is open, hovering the next trigger switches without moving the focus; a hover leaves the focus - and the keys - in the field being typed in. axe-core runs on the open state. |
| `StrictCspTests` | `/strict-csp`, served under a Content-Security-Policy whose `style-src` is `'self'` (no `'unsafe-inline'`) and rendering its components with `BitBhOptions.StrictCsp`: the prerendered markup has no `style` attribute; the `<head>` script applies the components' `data-bit-bh-style` before the app starts (`blazor.web.js` is held back until that is checked); their styles - a visually hidden skip link's clipping, a splitter's flex layout, a scroll area's viewport, a slider thumb's position, `--bit-bh-progress`, a tree level, a modal dialog's positioning - take effect through the CSSOM and follow the state; and the policy reports no violation. No axe-core run there: axe adds a `<style>` of its own. |

It is a port of the Playwright (JavaScript) suite the library was written with: the same pages, rules and flows,
with the names the library took here (`BitBh*` components, `data-bit-bh-*` hooks, the `[data-bit-bh-live-region]`
announcer). An automated audit finds a part of the problems; it is not a claim of WCAG conformance.

Every test fails on an uncaught page error and on any console error (favicon and 404 resource noise aside), and on a
page that does not answer with a success status - a page whose prerender threw is a 500.

### When a page is ready

Every page is prerendered, so its markup is on screen long before anything handles a key, and neither the load event
nor an idle network says when that changes. A test starts once both of these hold:

- the site shell's `.page` says `data-interactive="true"`. `MainLayout.razor` renders it from
  `RendererInfo.IsInteractive`, which is false in a prerender, so it turns true with the first interactive render -
  on WebAssembly once the runtime has loaded and rendered the whole page over its prerendered markup, on Blazor
  Server once the circuit has - in the same batch that attaches the page's event handlers.
- `[data-bit-bh-live-region]` exists: the library's JS initializer has installed its document-level behaviours.

Tests find their elements inside `.bb-docs-content` (`DemoTest.Content`) rather than `main`: the docs layout puts its
navigation, its "Browse docs" toggle, the pager and the rail inside `main` as well, and a selector such as
`main button[aria-expanded]` would find the toggle before the page's own popover.

axe-core then waits until the page is at rest: no element is marked `[data-transition]` (a `BitBhTransition` sets it
from its first render until its phase ends) and every finite CSS animation or transition has finished. Audited
mid-fade, text has the contrast of a frame - the transition page's appear sample starts fading in on the first
interactive render.

## Running locally

From `src/Utilities/Bheadless` (inside `src`, so `src/global.json` selects the SDK and the Microsoft.Testing.Platform runner):

```bash
dotnet build Tests/Bit.Bheadless.Tests.E2E/Bit.Bheadless.Tests.E2E.csproj

# Once per machine (and after a Playwright update): the browser Playwright drives.
pwsh Tests/Bit.Bheadless.Tests.E2E/bin/Debug/net11.0/playwright.ps1 install chromium

cd Tests/Bit.Bheadless.Tests.E2E && dotnet test

# One class, or one test
dotnet test --filter "FullyQualifiedName~.KeyboardTests."
dotnet test --filter "FullyQualifiedName~Dialog_traps_focus"
```

The suite publishes the demo itself before the first test, the way `bheadless.demo.cd.yml` does
(`dotnet publish ... -c Release -p:AspNetCoreHostingModel=OutOfProcess -p:InvariantGlobalization=true`), into
`bin/<configuration>/net11.0/demo/<render mode>`, starts it on a free port in the Production environment, shares it
between all tests and stops it when the run ends. A publish rather than a build output: only a publish trims the
WebAssembly client, so an interop payload the trimmer breaks fails here and nowhere else. The publish needs the
`wasm-tools` workload (`dotnet workload install wasm-tools`) and Node.js (the demo builds its Tailwind stylesheet). To
skip it, publish once and point `BHEADLESS_E2E_DEMO_DIR` at the output:

```bash
dotnet publish Demo/Server/Bit.Bheadless.Demo.Server.csproj -c Release -p:AspNetCoreHostingModel=OutOfProcess -p:InvariantGlobalization=true -o ../../../demo
BHEADLESS_E2E_DEMO_DIR=../../../demo dotnet test
```

On Windows, the WebAssembly publish relinks `dotnet.native.wasm` under the client's `obj` folder, and Emscripten's
tools fail on a path longer than 260 characters - a clone in a deeply nested folder hits that; a shorter one does not.

Each test gets a browser context of its own - its own `localStorage` and WebAssembly instance - with Playwright's
Desktop Chrome viewport and a light colour scheme, so the demo's theme and axe's contrast checks are the same on every
machine. The tests run four at a time (`AssemblySetup.cs`). Every test is traced; the trace of a failed one is kept as
`TestResults/traces/<test>.zip` next to the binaries (open it with `playwright.ps1 show-trace <zip>` or at
trace.playwright.dev) and attached to the TRX.

When no browser can be launched or the demo does not publish or start, every test reports **Inconclusive** with the
reason. On a build server (`CI=true`) the same tests **fail** instead, so a suite that never ran cannot pass.

Without PowerShell 7, the browser can be installed with the Node.js runtime Playwright ships:
`bin/Debug/net11.0/.playwright/node/<platform>/node bin/Debug/net11.0/.playwright/package/cli.js install chromium`.

### WebAssembly and Blazor Server

The demo runs on WebAssembly, as it is published. `BHEADLESS_E2E_RENDER_MODE=Server` publishes it with
`-p:IncludeWasm=false` instead, which leaves the WebAssembly client out, and the same pages then run on a Blazor Server
circuit - every key the components handle crosses the connection. A demo handed in from outside
(`BHEADLESS_E2E_DEMO_DIR`, `BHEADLESS_E2E_BASE_URL`) has to be in the mode asked for; the suite checks
the render-mode markers of its home page and refuses to run against the other one.

### Environment variables

| Variable | Effect |
|---|---|
| `BHEADLESS_E2E_RENDER_MODE` | `WebAssembly` (default) or `Server`. |
| `BHEADLESS_E2E_DEMO_DIR` | A folder holding a published demo server: start that instead of publishing. |
| `BHEADLESS_E2E_BASE_URL` | Drive an already running demo (for example one started from the IDE); nothing is published or started. |
| `BHEADLESS_E2E_CHANNEL` / `BHEADLESS_E2E_EXECUTABLE` | Use an installed Chrome/Edge (`chrome`, `msedge`) or a specific Chromium binary. Without either, the Chromium Playwright installed is used, falling back to an installed Chrome, then Edge. |
| `BHEADLESS_E2E_HEADED=1` | Show the browser. |

## CI

`.github/workflows/bit.ci.Bheadless.e2e.yml` publishes the demo the way `bheadless.demo.cd.yml` does,
builds this suite in Release, installs Chromium and runs it from this folder with `--report-trx`, once per render mode,
uploading the TRX and the traces of failed tests. It runs only when started by hand (Actions > "CI Bheadless E2E" > Run
workflow) or called from another workflow; the pull-request pipeline,
`bit.ci.Bheadless.yml`, runs the build, the component tests and the MCP tests only.
