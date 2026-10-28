# Bit.Bonboarding end-to-end tests

The engine and component tests in `Bit.Bonboarding.Tests` run the whole engine against a scripted stand-in for the
browser layer (`FakeInterop`): every transition, hook, wait and persistence rule is checked in .NET. That covers the
engine well and leaves out everything that only exists in a real browser:

- `bit-bonboarding.ts` never executes, so nothing proves that a target is found, measured and observed, that a
  hidden or moved target is noticed, or that keys and focus behave;
- nothing is laid out, so the spotlight and the popover are never actually placed anywhere;
- the stylesheet is never applied, so the themes, reduced motion and the cascade with an application's own styles are
  unchecked;
- the demo pages, which compose tours with real application code, never run.

This suite boots the demo (`Bit.Bonboarding.Demo.Server`, which prerenders the pages and runs them interactively)
and drives it with Playwright, in Chromium by default and in Firefox or WebKit when asked
(`BONBOARDING_E2E_BROWSER`). It asserts **what a person meets**: where the spotlight and the popover are on screen
relative to the element a step names, which step is showing, where focus is, what a screen reader is told, what
axe-core finds.

## What it proves

| Suite | What it covers |
|---|---|
| `TourTests` | The spotlight frames each target with the configured padding and the popover sits on the side asked for; a step with no target is a centred card; the arrow keys move the tour and Escape ends it and returns focus to the control that started it; focus moves into each step and Tab cannot leave a modal one; each step is announced in a polite live region; a finished tour leaves nothing that intercepts the page. |
| `ResilienceTests` | A step waits for a panel the user reveals while it waits (nothing blocks the page and the keyboard is not trapped); the waiting card can be closed; a target that exists but is hidden is waited for until it shows; a target hidden mid-step is waited for and found again; a target that moves with nothing resized is followed; a selector whose first match is hidden points at the visible one; an interactive step moves on when the real control is used, and Tab goes round its card and that control and nowhere else. |
| `ThemeTests` | The system theme is dark on a dark system and light on a light one; an application's palette declared on `.bit-bonboarding-root` still wins on a dark system; focus stays visible in forced colors; reduced motion takes the transitions out. |
| `PlacementTests` | The spotlight covers only the part of a target its scrolling pane shows, and a target scrolled out of its pane is pointed at on the pane's edge; a tour run again over a pane left scrolled brings its target back into the pane; a logical side (`InlineEnd`) is on the right in a left-to-right page; a host inside a transformed, scrolled ancestor still frames its target. |
| `FlowTests` | A conditional step shows, and counts in the progress, only for the role and feature flag it asks for; Back retraces the branch that was taken; a step holds until its field is valid and says why in an alert. |
| `GuidanceTests` | A beacon sits on its target's corner and follows it, hides with it and while a tour runs; a read hint takes its beacon away for good (across a reload) and focus goes to the target; the checklist ticks itself off by signal, condition and tour, and remembers; the tour list starts what it lists; a held advance says why. |
| `LifecycleTests` | A session driven from the page's own buttons (pause, resume, jump, Back through the history, complete, start at a step); queued tours start highest priority first and a `Skip` start does not start; "remind me later" holds an announcement back from `StartOnceAsync` but not from `StartAsync`; hooks run, and an answer written to the session's state unlocks a later step; a broken definition is listed by `Validate()` and refused by `StartAsync`. |
| `SampleTests` | The demo samples the suites above do not drive: a target found by anchor, selector and element reference; one spotlight around several elements; a step advancing on a change event, a filtered click and an application signal; a replaced target followed and a removed one waited for; `CanStart` and `Force`; a checklist item listed only for administrators and ticked off from another page; a beacon running a handler; a recomposed step, and a page-drawn card that no other host draws too. |
| `CrossTabTests` | Two tabs of the site: a tour dismissed in one stops in the other without overwriting the decision; a checklist item ticked in one is ticked in the other. |
| `HostingTests` | A tour on both renderers InteractiveAuto picks: the server on a first visit, WebAssembly once its runtime is cached. |
| `AccessibilityTests` | axe-core's WCAG 2.2 A and AA rules on every demo page, on a spotlight step and a beacon's hint in light and dark, and on the waiting card. A failure lists each rule broken and the elements that broke it. |

Every page is prerendered, so its buttons are on screen before anything listens to them. Each test waits until the
page runs in the browser (`data-interactive="true"` on the layout) before it touches anything, and waits for a step
by its title and for the popover to stop moving before it measures.

## Running locally

From `src/Utilities/Bonboarding` (inside `src`, so `src/global.json` selects the Microsoft.Testing.Platform runner):

```bash
# The suite runs the demo server's build output: build it first, in the configuration the tests run in.
dotnet build Demo/Server/Bit.Bonboarding.Demo.Server.csproj
dotnet build Tests/Bit.Bonboarding.Tests.E2E/Bit.Bonboarding.Tests.E2E.csproj

# Once per machine (and after a Playwright update): the browser Playwright drives. Without it the suite falls
# back to an installed Chrome, then Edge.
pwsh Tests/Bit.Bonboarding.Tests.E2E/bin/Debug/net10.0/playwright.ps1 install chromium

cd Tests/Bit.Bonboarding.Tests.E2E && dotnet test

# The same suite in another engine, after installing it (playwright.ps1 install firefox webkit)
BONBOARDING_E2E_BROWSER=firefox dotnet test
BONBOARDING_E2E_BROWSER=webkit dotnet test

# One test class, or one test
dotnet test --filter "FullyQualifiedName~.ResilienceTests."
dotnet test --filter "FullyQualifiedName~waiting_card"
```

| Variable | Meaning |
|---|---|
| `BONBOARDING_E2E_BASE_URL` | Drive an already running demo instead of booting one. |
| `BONBOARDING_E2E_BROWSER` | `chromium` (default), `firefox` or `webkit`. |
| `BONBOARDING_E2E_CHANNEL` | An installed Chromium-based browser to use, e.g. `chrome` or `msedge`. |
| `BONBOARDING_E2E_EXECUTABLE` | A browser executable of the chosen engine. |
| `BONBOARDING_E2E_CONFIGURATION` | The demo build to boot; defaults to the configuration of the test build. |
| `BONBOARDING_E2E_HEADED` | `1` shows the browser. |

A missing browser or a demo that does not start makes every test **Inconclusive** on a developer machine, naming the
reason, and **fails** it on a build server (`CI=true`), so a suite that never ran is never a green build. An engine that
cannot emulate something a test needs (forced colors, say) reports that test Inconclusive with the reason, never as
passed. Every test has a three-minute timeout (`testconfig.json`), so a hang is reported as a failure rather than
holding up the run.

CI does the same in Release (`.github/workflows/bit.ci.Bonboarding.e2e.yml`), against the demo published the way
`bonboarding.demo.cd.yml` publishes it rather than a build output: only a publish trims the WebAssembly client, so an
interop payload the trimmer breaks fails there and nowhere else. That workflow runs only when started by hand, once
per engine. To run the suite the same way, from the repository root:

```bash
dotnet publish src/Utilities/Bonboarding/Demo/Server/Bit.Bonboarding.Demo.Server.csproj -c Release -o demo -p:InvariantGlobalization=true
dotnet build src/Utilities/Bonboarding/Tests/Bit.Bonboarding.Tests.E2E/Bit.Bonboarding.Tests.E2E.csproj -c Release
(cd demo && dotnet Bit.Bonboarding.Demo.Server.dll --urls http://127.0.0.1:5000 &)
cd src/Utilities/Bonboarding/Tests/Bit.Bonboarding.Tests.E2E && BONBOARDING_E2E_BASE_URL=http://127.0.0.1:5000 dotnet test -c Release --no-build
```

The pull-request pipeline, `bit.ci.Bonboarding.yml`, runs the build, engine, component and MCP tests on .NET 10
and 11.
