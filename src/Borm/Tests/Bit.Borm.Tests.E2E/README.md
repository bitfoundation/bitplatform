# Bit.Borm end-to-end tests

The tests in `Bit.Borm.Tests` run the engine and its components without a browser: schema generation, the builder,
JSON Schema import and export, validation, conditions, computed values and the state API are checked in .NET, and the
components are rendered with bUnit. That covers the C# side well and leaves out everything that only exists in a real
browser:

- there is no real focus: `FocusAsync` is a call a test double records, so nothing proves that the caret lands in the
  first invalid field, in a repeater row that was just added, on the error summary, or on the step body of a wizard -
  after the render that put the control on screen, and without something else taking it away again;
- there is no real event order: `change` raised only when a control loses the focus or Enter is pressed, the blur a
  mouse press causes before the click it starts, the implicit submission Enter triggers (which a form only gets with a
  submit button or a single text box), a tag box that belongs to no form, a debounced field written while the caret is
  still in it;
- there is no stylesheet, so no theme tokens, no dark palette following the operating system, no grid that collapses
  on a narrow screen and no error colours, and no `<datalist>` or `<input type="file">` doing their part;
- the demo pages, prerendered on the server and then run on WebAssembly with real application code, never run.

This suite boots the demo (`Bit.Borm.Demo.Server`, which prerenders the pages and runs them on WebAssembly) and drives
it in Chromium with Playwright. It asserts **effects**, not markup: where the focus is, what a control holds and what
the page around the form shows after a submit, the attributes a screen reader reads, and what `getComputedStyle` and
`getBoundingClientRect` return.

## What it proves

| Suite | What it covers |
|---|---|
| `ValidationTests` | A failed submit marks every required field (`aria-invalid`, the message the control is described by, the error colours) and takes the focus to the first one, and fixing a value clears its complaint; with the error summary on, the summary takes the focus and its links focus each field without leaving the page; the summary takes the focus after a failed submit, never while the user is editing; a `[Compare]` rule and a hand-written `RevalidateOn` rule are settled by correcting the other field; an attribute rule without a message of its own reads as a sentence; a debounced async rule reports a taken username after its lookup; Reset restores the values and clears the errors; `Trim` and `Normalize` tidy values, and a date outside its range is reported; a FluentValidation validator and an `IValidatableObject` rule report on the fields they name. |
| `ConditionalTests` | The company fields appear only for a business account and are only then required; a condition on a repeater row switches that row alone, the summary numbers the rows, and a hidden answer is cleared; unticking the hire car hides and clears its days and the computed estimate drops the car again; a conditionally required field blocks the submit only while it shows. |
| `RepeaterTests` | Adding a line puts the caret in it and announces the count, by mouse and by keyboard; clicking Add line again straight away adds a second line; removing a line moves the caret to the line that took its place, and removing the last one to the add button; the line totals and the invoice total follow the quantities and prices; moving a line carries its values and its errors, and the focus stays on the button that moved it; locking the invoice disables every control and button in it. |
| `WizardTests` | Next judges only the current step, focuses its first error, and moves the focus to the new step and announces it; Back keeps the answers; a business account adds the company step, which gates on its own field; every step already reached is clickable in the stepper in both directions, a step never reached is not, and the last step finishes the form; Enter in a text box advances instead of submitting. |
| `ChoiceTests` | A label typed into the combobox, in any casing, stores the value behind it, and an answer on no list is kept and reported; choosing a country loads its cities and changing it clears the old city and reloads them; a plan fills in the seats, which stay editable; Enter or a comma adds a tag without submitting the form, a repeat is ignored, and Backspace on an empty box removes the last; a select's cities reload with the country and a city that no longer exists is dropped; an options service that fails says so and the form recovers once it is back. |
| `FieldTests` | A field written on every keystroke drives the live preview and its counter while the caret is still in it, and the counter turns red instead of truncating; the clear button empties the field and leaves the caret in it; the reveal button shows the password and says so with `aria-pressed`; chosen files are listed and judged by type and size, clearing them puts the focus back on the field and the same file can be chosen again; a custom renderer takes part in validation like a built-in control. |
| `JsonSchemaTests` | An edited schema regenerates the form, rules included; an invalid one is reported and the last good form stays; the conditional example brings `x-visibleWhen`, `dependentRequired`, `$ref` and its two-column layout through; the wizard example survives the round trip out to JSON and back, and Enter in its one-field step advances. |
| `ThemingAndLayoutTests` | A dark colour-scheme preference switches the form's tokens and the controls with them, and switches back on the spot; a `data-theme` on an ancestor wins over the preference; two columns side by side collapse into one on a narrow screen, and a field spanning both is as wide as the two; reduced motion turns the transitions off. |
| `KeyboardTests` | The signup form can be filled in and sent from the keyboard alone (Tab, the arrow keys in a radio group, Space, Enter); a field marked to autofocus has the caret when the page opens; a read-only answer stays in the tab order but cannot be changed. |
| `DemoPagesTests` | Every demo page (the routes are read from the `@page` directives under `Demo/Client/Pages`, so a new page is swept without being listed) answers, is prerendered with its forms in the first response, turns interactive with `bit-borm.css` loaded and styling its forms, and runs every button on it without a console error, an unhandled exception, a failed request or the error banner; an unknown address gets the not-found page inside the app; navigating through every demo in the app leaves a working form behind; "View source" shows the page's code. |

Every page is prerendered, so its form - inputs, buttons and all - is on screen before anything handles a keystroke:
a value typed then is overwritten when WebAssembly takes the page over, and a click is lost. Each test waits until the
page runs in the browser (`data-interactive="true"` on the layout's `.app-shell`) and the page has stopped changing,
and from then on waits for conditions (a message, a focused control, a value), never for a fixed time.

Every test also fails on any uncaught page error and on any console error in which Blazor reports an unhandled
exception, whatever the test was looking at: an exception that escapes a handler can leave every later keystroke
unanswered, and the test would otherwise time out pointing at the wrong thing.

## Running locally

From `src/Borm` (inside `src`, so `src/global.json` selects the Microsoft.Testing.Platform runner):

```bash
# The suite runs the demo server's build output: build it first, in the configuration the tests run in.
dotnet build Demo/Server/Bit.Borm.Demo.Server.csproj
dotnet build Tests/Bit.Borm.Tests.E2E/Bit.Borm.Tests.E2E.csproj

# Once per machine (and after a Playwright update): the browser Playwright drives.
pwsh Tests/Bit.Borm.Tests.E2E/bin/Debug/net10.0/playwright.ps1 install chromium

cd Tests/Bit.Borm.Tests.E2E && dotnet test

# One test class, or one test
dotnet test --filter "FullyQualifiedName~.WizardTests."
dotnet test --filter "FullyQualifiedName~Removing_the_last_line"
# Everything but the page sweep
dotnet test --filter "FullyQualifiedName!~Every_demo_page"
```

The workflow `.github/workflows/bit.ci.Borm.e2e.yml` does the same in Release:

```bash
dotnet build src/Borm/Demo/Server/Bit.Borm.Demo.Server.csproj -c Release
dotnet build src/Borm/Tests/Bit.Borm.Tests.E2E/Bit.Borm.Tests.E2E.csproj -c Release
pwsh src/Borm/Tests/Bit.Borm.Tests.E2E/bin/Release/net10.0/playwright.ps1 install --with-deps chromium
cd src/Borm/Tests/Bit.Borm.Tests.E2E && dotnet test -c Release --no-build --report-trx --report-trx-filename borm-e2e.trx
```

Without PowerShell 7, the browser can be installed with Windows PowerShell
(`powershell -ExecutionPolicy Bypass -File bin/Debug/net10.0/playwright.ps1 install chromium`) or with the Node.js
runtime Playwright ships:
`bin/Debug/net10.0/.playwright/node/<platform>/node bin/Debug/net10.0/.playwright/package/cli.js install chromium`.

The suite starts the demo server itself (`dotnet Bit.Borm.Demo.Server.dll --urls http://127.0.0.1:<free port>`, in
the Development environment so the build output finds its static web assets), waits until it answers, shares it
between all tests and stops it when the run ends. Rebuilding the demo while a run is in progress changes the
fingerprinted assets under the running server and fails the pages that boot afterwards, so rebuild between runs.
Each test gets a browser context of its own - its own WebAssembly instance, so no form state leaks between tests -
with a light colour scheme, so the stylesheet's `prefers-color-scheme` theme is the same on every machine, and reduced
motion, which the stylesheet answers by dropping its transitions, so a colour read right after a field turned invalid
is its resting colour. Every request that leaves the demo's origin is answered with an empty response, so a machine
without internet access runs the suite just the same. The tests run one at a time: many of them race a keystroke
against the render it causes, or an options lookup against its simulated latency, and a second page booting
WebAssembly next to them is exactly the load that turns those waits into flakes.

When no browser can be launched or the demo does not start, every test reports **Inconclusive** with the reason.
On a build server (`CI=true`) the same tests **fail** instead, so a suite that never ran cannot pass.

### Environment variables

| Variable | Effect |
|---|---|
| `BORM_E2E_CONFIGURATION` | Build configuration of the demo server to boot; defaults to the test assembly's own. |
| `BORM_E2E_BASE_URL` | Drive an already running demo (for example one started from the IDE) instead of booting one. |
| `BORM_E2E_CHANNEL` / `BORM_E2E_EXECUTABLE` | Use an installed Chrome/Edge (`chrome`, `msedge`) or a specific Chromium binary. Without either, the Chromium Playwright installed is used, falling back to an installed Chrome, then Edge. |
| `BORM_E2E_HEADED=1` | Show the browser. |
