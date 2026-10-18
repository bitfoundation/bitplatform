# Bit.Balette end-to-end tests

The model, search and component tests in `Bit.Balette.Tests` run the palette without a browser: matching, ranking,
paging, modes, history and every result a handler can return are checked in .NET with bUnit, and the browser layer
is a stubbed JS runtime. That covers the logic well and leaves out everything that only exists in a real browser:

- `bit-balette.ts` never executes, so nothing proves that `Ctrl+K` reaches the palette from anywhere on the page,
  that a `G` then `S` sequence is matched while typing in a text box is not, or that the navigation keys are claimed
  before the browser runs their default action - `Tab` leaving the dialog, `Backspace` deleting text;
- there is no native `<dialog>`: no top layer, no inert page behind it, no focus moved in and given back, no scroll
  lock;
- there is no layout, so the highlighted segments of a title, the theme's computed colours and the full-screen dialog
  on a phone are never measured;
- there is no real clock under the debounce of an async source, and no WebAssembly runtime under the demo pages, which
  compose the palette with real application code.

This suite boots the demo (`Bit.Balette.Demo.Server`, which prerenders the pages and runs them on WebAssembly) and
drives it in Chromium with Playwright. It asserts **effects**, not markup: where the focus is, what `:modal` and
`elementFromPoint` say, what `getComputedStyle` and `getBoundingClientRect` return, what the page around the palette
shows after a command ran.

## What it proves

| Suite | What it covers |
|---|---|
| `OpenAndCloseTests` | `Ctrl+K` and `Ctrl+Shift+P` open a modal dialog in the top layer with the focus in the input; inside it `Ctrl+Shift+P` pins instead of closing; `Esc` closes it and gives the focus back to the field or the trigger that had it; a click on the backdrop closes it and one inside does not; the page behind it can be neither focused nor hit; the scroll lock on `<html>` is what keeps the page from scrolling, and it goes with the palette. |
| `SearchAndNavigationTests` | Typing filters and ranks (`gs` puts Settings first); the `<mark>`s are on the matched characters, and the segments of a title touch, with no gap between them (they share one inline box); the live region announces the result count; `↑` `↓` `Ctrl+N` `Ctrl+P` `Home` `End` move the active row, with `aria-activedescendant`, `aria-selected` and the focus following; the active row is scrolled into view in a long list; `Tab` stays in the dialog and opens the action panel; `Backspace` deletes text when there is some and goes back a page when there is none, without eating the query it restores. |
| `FlowTests` | A nested page with breadcrumbs whose choice sets the page's theme; the lazy page's loading state, then its items; the rename prompt, pre-filled, renaming the project; a multi-select page toggled with `Enter` and `Space` and applied with `Ctrl+Enter`; a destructive command that needs a second `Enter`, disarmed by `Esc`; Pin to top from the action panel, listed under Pinned when reopened; a recent that survives a reload; toasts, a disabled row explaining itself, and the toast of a handler that closes the palette shown before it closes; choosing a page navigates there by keyboard and by mouse. |
| `ModesAndSourcesTests` | A typed prefix selects its mode and shows the chip; the top bar's mode buttons open the palette with the prefix kept and the caret after it, so typing goes on from there; `#105` finds that issue; `@ada` answers only after the debounce and the source's latency, with the detail pane showing the person; a burst of keystrokes reaches the source far fewer times than it was typed; the documentation source answers next to the registry. |
| `GlobalShortcutTests` | `G` then `S` navigates with the palette closed; plain keys typed in a page field are typing while chords still fire from it; a single-key shortcut declared in markup; `F2` runs Rename project… and opens the palette on its prompt; `Mod+/` lands on the keyboard page; `Alt+Z` and a page's own `Ctrl+Up` run with the palette closed; with Vim bindings on, `Ctrl+K` inside the palette moves up instead of closing. |
| `ThemingAndLayoutTests` | The palette and site theme buttons change the colours the dialog is painted with; the accent token recolours the mode chip and the highlights; the detail pane follows the page's checkbox and the palette's own toggle, and the dialog's width with it; on a 390 px phone the dialog fills the screen and nothing overflows sideways. |
| `DemoPagesTests` | Every demo page loads, turns interactive and logs no error, and every button on it runs without one; an unknown address gets the not-found page (404), where the palette still works. |

Every page is prerendered, so its markup - the trigger and the mode buttons included - is on screen before anything
handles a click, and the browser layer that hears `Ctrl+K` starts a moment after the page turns interactive. Each test
waits for both: `data-interactive="true"` and `data-palette-ready="true"` on the layout's `.app` element.

Every test also fails on any uncaught page error and on any console error in which Blazor reports an unhandled
exception, whatever the test was looking at: an exception that escapes a handler can leave every later keystroke
unanswered, and the test would otherwise time out pointing at the wrong thing.

## Running locally

From `src/Balette` (inside `src`, so `src/global.json` selects the Microsoft.Testing.Platform runner):

```bash
# The suite runs the demo server's build output: build it first, in the configuration the tests run in.
dotnet build Bit.Balette.Demo/Server/Bit.Balette.Demo.Server.csproj
dotnet build Tests/Bit.Balette.Tests.E2E/Bit.Balette.Tests.E2E.csproj

# Once per machine (and after a Playwright update): the browser Playwright drives.
pwsh Tests/Bit.Balette.Tests.E2E/bin/Debug/net10.0/playwright.ps1 install chromium

cd Tests/Bit.Balette.Tests.E2E && dotnet test

# One test class, or one test
dotnet test --filter "FullyQualifiedName~.FlowTests."
dotnet test --filter "FullyQualifiedName~Backspace_on_an_empty_query"
```

CI does the same in Release (`.github/workflows/bit.ci.Balette.e2e.yml`):

```bash
dotnet build src/Balette/Bit.Balette.Demo/Server/Bit.Balette.Demo.Server.csproj -c Release
dotnet build src/Balette/Tests/Bit.Balette.Tests.E2E/Bit.Balette.Tests.E2E.csproj -c Release
pwsh src/Balette/Tests/Bit.Balette.Tests.E2E/bin/Release/net10.0/playwright.ps1 install --with-deps chromium
cd src/Balette/Tests/Bit.Balette.Tests.E2E && dotnet test -c Release --no-build --report-trx --report-trx-filename balette-e2e.trx
```

That workflow only runs when started by hand (Actions > "bit platform CI - Balette E2E" > Run workflow). The pull-request pipeline, `bit.ci.Balette.yml`, runs the build, model, search, component and MCP tests only.

Without PowerShell 7, the browser can be installed with Windows PowerShell
(`powershell -ExecutionPolicy Bypass -File bin/Debug/net10.0/playwright.ps1 install chromium`), or with the Node.js
runtime Playwright ships: `bin/Debug/net10.0/.playwright/node/<platform>/node bin/Debug/net10.0/.playwright/package/cli.js install chromium`.

The suite starts the demo server itself (`dotnet Bit.Balette.Demo.Server.dll --urls http://127.0.0.1:<free port>`,
in the Development environment so the build output finds its static web assets), waits until it answers, shares it
between all tests and stops it when the run ends. Rebuilding the demo while a run is in progress changes the
fingerprinted assets under the running server and fails the pages that boot afterwards, so rebuild between runs.
Each test gets a browser context of its own - its own `localStorage`, so recents and pins never leak between tests -
with a light colour scheme and reduced motion, so the palette's `auto` theme is the same on every machine and a box
measured right after the dialog opened is its resting box rather than a frame of the pop-in animation. The tests run
one at a time: many of them race a keystroke against a render or a debounce against a clock, and a second page booting
WebAssembly next to them is exactly the load that turns those waits into flakes.

When no browser can be launched or the demo does not start, every test reports **Inconclusive** with the reason. On a
build server (`CI=true`) the same tests **fail** instead, so a suite that never ran cannot pass.

### Environment variables

| Variable | Effect |
|---|---|
| `BALETTE_E2E_CONFIGURATION` | Build configuration of the demo server to boot; defaults to the test assembly's own. |
| `BALETTE_E2E_BASE_URL` | Drive an already running demo (for example one started from the IDE) instead of booting one. |
| `BALETTE_E2E_CHANNEL` / `BALETTE_E2E_EXECUTABLE` | Use an installed Chrome/Edge (`chrome`, `msedge`) or a specific Chromium binary. Without either, the Chromium Playwright installed is used, falling back to an installed Chrome, then Edge. |
| `BALETTE_E2E_HEADED=1` | Show the browser. |
