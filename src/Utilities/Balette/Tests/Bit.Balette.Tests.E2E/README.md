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
drives it with Playwright in Chromium, Firefox or WebKit. It asserts **effects**, not markup: where the focus is, what `:modal` and
`elementFromPoint` say, what `getComputedStyle` and `getBoundingClientRect` return, what the page around the palette
shows after a command ran.

## What it proves

| Suite | What it covers |
|---|---|
| `OpenAndCloseTests` | `Ctrl+K` and `Ctrl+Shift+P` open a modal dialog in the top layer with the focus in the input; inside it `Ctrl+Shift+P` pins instead of closing; `Esc` closes it and gives the focus back to the field, the trigger or the page's button that opened it - in WebKit too, where a click does not focus the button; a click on the backdrop closes it and one inside does not; the page behind it can be neither focused nor hit; the scroll lock on `<html>` is what keeps the page from scrolling, and it goes with the palette. |
| `SearchAndNavigationTests` | Keys typed faster than the palette renders are all kept (on Blazor Server a render answering an earlier key once wrote its value over them); typing filters and ranks (`gs` puts Settings first); the `<mark>`s are on the matched characters, and the segments of a title touch, with no gap between them (they share one inline box); the live region announces the result count; `↑` `↓` `Ctrl+N` `Ctrl+P` `Home` `End` move the active row, with `aria-activedescendant`, `aria-selected` and the focus following; the active row is scrolled into view in a long list; `Tab` stays in the dialog and opens the action panel; `Backspace` deletes text when there is some and goes back a page when there is none, without eating the query it restores. |
| `FlowTests` | A nested page with breadcrumbs whose choice sets the page's theme, remembered across a reload; the lazy page's loading state, then its items; the rename prompt, pre-filled, renaming the project; a multi-select page toggled with `Enter` and `Space` and applied with `Ctrl+Enter`; a destructive command that needs a second `Enter`, disarmed by `Esc`; Pin to top from the action panel, listed under Pinned when reopened; a recent that survives a reload; toasts, a disabled row explaining itself, and the toast of a handler that closes the palette shown before it closes; choosing a page navigates there by keyboard and by mouse. |
| `ModesAndSourcesTests` | A typed prefix selects its mode and shows the chip; the palette bar's mode buttons open the palette with the prefix kept and the caret after it, so typing goes on from there; `#105` finds that issue; `@ada` answers only after the debounce and the source's latency, with the detail pane showing the person; a burst of keystrokes reaches the source far fewer times than it was typed; the documentation source answers next to the registry. |
| `GlobalShortcutTests` | `G` then `S` navigates with the palette closed; plain keys typed in a page field are typing while chords still fire from it; a single-key shortcut declared in markup; `F2` runs Rename project… and opens the palette on its prompt; `Mod+/` lands on the keyboard page; `Alt+Z` and a page's own `Ctrl+Up` run with the palette closed; with Vim bindings on, `Ctrl+K` inside the palette moves up instead of closing. |
| `ThemingAndLayoutTests` | The palette and site theme buttons change the colours the dialog is painted with - the site's `bit-theme`, which demo.css points the palette's custom properties at; the header's own theme toggle is followed by the palette and shown by the Theme command, and `auto` drops the pinned choice again; the accent token recolours the mode chip and the highlights; the detail pane follows the page's checkbox and the palette's own toggle, and the dialog's width with it; on a 390 px phone the dialog fills the screen and nothing overflows sideways. |
| `TwoPalettesTests` | The site has two palettes - Balette on `Ctrl+K` and the header's docs search on `/` - and a key never reaches both: each opens the one it belongs to; while the docs search is open Balette's toggle, chords and page shortcuts stay quiet and the search keeps typing, its arrows and `Enter`; while Balette is open a `/` is typed into it, and from one of its buttons does not open the search behind it. |
| `KeyboardLayoutTests` | Key presses as other keyboards produce them, dispatched with their `key` and `code`: `Ctrl+K` on a Russian layout opens the palette, `G S` typed on a Persian one runs, `Ctrl+N`/`Ctrl+P` move on a Russian one; a held `Ctrl+K` opens the palette once; keys that belong to an input method's composition (`isComposing`, `keyCode` 229) run nothing, globally and inside the palette. |
| `AccessibilityTests` | axe-core (WCAG 2.0-2.2 A and AA rules, run inside the page) over the root list, search results, a nested page, a multi-select page and a prompt, in light and dark, with the demo's theme and with the library's defaults; forced colours outline the active row; in right-to-left text the chevrons are mirrored and `→` goes back; clicking a row leaves the focus in the input. An automated audit is not a claim of conformance. |
| `SearchFeatureTests` | `francais` finds Français on the Language page from the root, with its path and highlights; `settigns` finds Settings and says the match is close; choosing Persian lays the page and the palette out right to left; the rename prompt validates and keeps a refused name for a correction; the directory page searches as you type. |
| `AccessibilityTreeTests` | What a screen reader is told, read from Chromium's own accessibility tree through the DevTools protocol: the search box is a named, expanded combobox whose active option is named by its title, subtitle and shortcut in words and sits in a group named by its heading; a toggle's state is in its name; a multi-select page reports checked, not selected; a prompt is a textbox named by its question with no listbox behind it; the dialog is a named modal with polite and assertive live regions. Closer to a screen reader than a rule engine, and still not one. |
| `InlinePaletteTests` | The inline palette in the settings section of /commands: searched and driven in place, its switches following the page; `Ctrl+K` from its box opens only the modal palette and a global shortcut runs once; axe-core finds nothing. |
| `DemoPagesTests` | Every demo page loads, turns interactive and logs no error, and every button on it - the site header's included - runs without one; an unknown address gets the not-found page (404), where the palette still works. |

Every page is prerendered, so its markup - the trigger and the mode buttons included - is on screen before anything
handles a click, and the browser layer that hears `Ctrl+K` starts a moment after the page turns interactive. Each test
waits for both: `data-interactive="true"` on the site shell's `.page` element and `data-ready="true"` on every palette -
the layout's dialog, and the inline palettes of /commands, /theming, /localization and /options. Not for the platform
being detected (`IBaletteService.PlatformDetected`): any palette reports it, an inline one often first, while the
dialog's key listener is still a few milliseconds away.

Every test also fails on any uncaught page error and on any console error in which Blazor reports an unhandled
exception, whatever the test was looking at: an exception that escapes a handler can leave every later keystroke
unanswered, and the test would otherwise time out pointing at the wrong thing.

## Chromium, Firefox and WebKit

The suite runs in Chromium unless `BALETTE_E2E_BROWSER` names another engine - `firefox` or `webkit`, the builds Playwright
installs. The palette's browser layer leans on what engines implement differently - which element a click focuses
(WebKit focuses the nearest focusable ancestor of a button, not the button), the native dialog, key events, forced
colours - so the workflow runs every engine. `AccessibilityTreeTests` reads Chromium's accessibility tree through the
DevTools protocol, which the other engines do not have: there it reports **Inconclusive**.

```bash
BALETTE_E2E_BROWSER=webkit dotnet test
```

A test that crashes the browser process (WebKit's, on Windows, now and then) fails on its own; the next test launches
the browser again.

## WebAssembly and Blazor Server

The demo runs on WebAssembly, as it is published. `BALETTE_E2E_RENDER_MODE=Server` starts it with `Demo:RenderMode=Server`
instead, which runs the same pages on a Blazor Server circuit - every key the palette handles then crosses the
connection - and the whole suite runs against that. An external demo (`BALETTE_E2E_BASE_URL`) has to have been started
in the mode asked for. The workflow runs both.

```bash
BALETTE_E2E_RENDER_MODE=Server dotnet test
```

## Running locally

From `src/Utilities/Balette` (inside `src`, so `src/global.json` selects the Microsoft.Testing.Platform runner):

```bash
# The suite runs the demo server's build output: build it first, in the configuration the tests run in.
dotnet build Demo/Server/Bit.Balette.Demo.Server.csproj
dotnet build Tests/Bit.Balette.Tests.E2E/Bit.Balette.Tests.E2E.csproj

# Once per machine (and after a Playwright update): the browser Playwright drives.
pwsh Tests/Bit.Balette.Tests.E2E/bin/Debug/net10.0/playwright.ps1 install chromium   # and firefox, webkit to run those

cd Tests/Bit.Balette.Tests.E2E && dotnet test

# One test class, or one test
dotnet test --filter "FullyQualifiedName~.FlowTests."
dotnet test --filter "FullyQualifiedName~Backspace_on_an_empty_query"
```

CI does the same in Release (`.github/workflows/bit.ci.Balette.e2e.yml`), against the demo published the way
`balette.demo.cd.yml` publishes it rather than a build output: only a publish trims the WebAssembly client, so an interop payload the trimmer breaks (an anonymous type whose
constructor lost its parameter names, a getter nothing but System.Text.Json reads) fails there and nowhere else:

```bash
dotnet publish src/Utilities/Balette/Demo/Server/Bit.Balette.Demo.Server.csproj -c Release -o demo -p:InvariantGlobalization=true
dotnet build src/Utilities/Balette/Tests/Bit.Balette.Tests.E2E/Bit.Balette.Tests.E2E.csproj -c Release
pwsh src/Utilities/Balette/Tests/Bit.Balette.Tests.E2E/bin/Release/net10.0/playwright.ps1 install --with-deps chromium
(cd demo && dotnet Bit.Balette.Demo.Server.dll --urls http://127.0.0.1:5000 &)
cd src/Utilities/Balette/Tests/Bit.Balette.Tests.E2E && BALETTE_E2E_BASE_URL=http://127.0.0.1:5000 dotnet test -c Release --no-build --report-trx --report-trx-filename balette-e2e.trx
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
| `BALETTE_E2E_BROWSER` | `chromium` (default), `firefox` or `webkit`: the engine to drive. |
| `BALETTE_E2E_CHANNEL` / `BALETTE_E2E_EXECUTABLE` | Use an installed Chrome/Edge (`chrome`, `msedge`) or a specific Chromium binary (`BALETTE_E2E_EXECUTABLE` also takes a Firefox or WebKit one with `BALETTE_E2E_BROWSER`). Without either, the Chromium Playwright installed is used, falling back to an installed Chrome, then Edge. |
| `BALETTE_E2E_HEADED=1` | Show the browser. |
