# Bonboarding improvement programme: plan and progress record

This file tracks the audit, the requirements matrix, the backlog and the verification evidence of the effort to
make Bonboarding a complete onboarding and in-app guidance framework for Blazor. It is a working record for
maintainers, not user documentation: the README is the user guide.

## Baseline (start of round 1)

- Branch `develop` at `692e428154`, clean working tree.
- `dotnet build Bit.Bonboarding.slnx`: succeeds, one pre-existing nullable warning
  (`Bit.Bonboarding.Tests.Mcp/McpSurfaceTests.cs:55`).
- `dotnet test --solution Bit.Bonboarding.slnx -f net10.0`: 276 / 276 passing (226 engine and component tests,
  50 MCP tests). The net8.0 and net9.0 test runs cannot start on this machine because only the .NET 10 runtime is
  installed. That is an environment limit, not a code failure: CI (`bit.ci.Bonboarding.yml`) runs all three.
- No browser-level tests exist. `bit-bonboarding.ts` never executes in any automated check.
- Library: ~6.4k lines (C# engine, three components, 1.0k lines of TypeScript, 0.6k lines of CSS), net8/9/10,
  trimmable, no runtime dependency beyond `Microsoft.AspNetCore.Components.Web`.

## Architecture as found

| Layer | Types | Notes |
| --- | --- | --- |
| Model | `BonboardingTourDefinition`, `BonboardingStepDefinition`, `BonboardingStepOptions` (three-level inheritance: step, tour, global, then `BonboardingDefaults`) | Plain mutable classes; hooks are `Func<..., ValueTask>` |
| Engine | `BonboardingTourSession` (state machine), `BonboardingService` (registry, start/stop, event fan-out) | One transition lock per session, re-entrant hook scopes, generation counter against stale async work |
| Browser | `IBonboardingInterop` / `BonboardingInterop`, `bit-bonboarding.ts` | Measures, observes (Resize/Intersection/Mutation observers, no polling), scrolls, keyboard, focus trap, storage |
| Layout | `BonboardingPlacementEngine` | Pure C#, shift before flip, arrow clamping, RTL alignment mirroring |
| UI | `BonboardingRoot` (host), `BonboardingPopoverContent`, `BonboardingProgress`, declarative `BonboardingTour` / `BonboardingStep` | One host renders the active session; hosts can claim sessions |
| Persistence | `IBonboardingStore`, `BonboardingBrowserStore` (localStorage + in-memory mirror), `BonboardingInMemoryStore` | Versioned `BonboardingRecord` |
| Extensibility | `IBonboardingLocalizer`, `IBonboardingObserver`, templates, `Configure` escape hatches | |

The architecture is sound and is preserved. The engine is careful about races and lifecycle, which the existing
226 tests prove in detail. The gaps are concentrated in the presentation primitives beyond tours (beacons,
checklists, launcher), orchestration between competing tours, a handful of genuine defects at the edges, and
the complete absence of browser-level verification.

## Confirmed defects (found by reading the source, each to be proven by a failing test first)

| # | Defect | Evidence |
| --- | --- | --- |
| D1 | `BonboardingTheme.System`, the default, never follows the OS dark preference | `bit-bonboarding.css` has only a `[data-bonboarding-theme="dark"]` rule; no `prefers-color-scheme` anywhere |
| D2 | While a session waits for a route, the page is dimmed and blocked. A user who navigates away, or a step whose route is a wildcard constraint or has `AutoNavigateToStepRoute = false`, cannot use the app to get to the page | `BonboardingRoot` renders overlay + blocker for every active session, `Waiting` included. README "Navigating away mid-tour" promises the overlay is removed |
| D3 | The default waiting card offers no way out except Escape; with an infinite `WaitTimeout` a pointer user is stuck behind a blocker | `BonboardingRoot.razor` waiting branch renders a spinner and text only |
| D4 | An `AdvanceOn` trigger detaches after its first event, so when `CanAdvance` rejects that advance the trigger never fires again | `attachAdvanceTrigger` listener calls `detachAdvanceTrigger` before invoking .NET |
| D5 | Replaying a completed tour writes `Completed = false`, contradicting `IsCompletedAsync` ("run to the end at some point") | `PersistAsync` / `PersistEndAsync` build a fresh record and only carry `Data` over |
| D6 | `RefreshAsync` and same-page navigation ignore a target that has gone; nothing reports the loss unless an observer happens to fire | `RefreshCoreAsync` applies a `Found = false` frame as plain geometry |
| D7 | A hidden target (`display:none`, zero size, `visibility:hidden`) counts as found: the step is not waited for and the spotlight silently disappears; `querySelector` also picks a hidden responsive duplicate over the visible one | `resolveOne` / `unionRect` in `bit-bonboarding.ts` |
| D8 | An invalid CSS selector is swallowed and waited on for the whole timeout | `resolveOne` catches the `SyntaxError` and returns null |
| D9 | A footer action whose handler is slow runs twice on a double click (consequential actions included) | `InvokeActionAsync` queues the second click behind the first and runs the handler again |
| D10 | The close button's negative margin is physical, so it is misaligned in RTL | `.bit-bonboarding-close { margin: -0.25rem -0.25rem 0 0 }` |
| D11 | While a step waits for its target the page is blocked and Tab is trapped, so a step that waits for the user to reveal its element cannot be completed - the demo's own "waiting tour" included | Found while writing the browser tests; `BonboardingRoot` renders the blocker while waiting |
| D12 | A target that moves or is hidden with nothing resized (a sibling growing in a fixed-height pane, `visibility: hidden`) is not noticed | Proven by a browser test; no observer reports such a change |
| D13 | The default fix for D1 would have overridden an application's own light palette on a dark system, and forced `color-scheme` over the page's | Caught in review: higher specificity than the documented `.bit-bonboarding-root` overrides; the system rule now uses `:where()` and inherits `color-scheme` |
| A1-A4 | Demo accessibility: link and active-link contrast, the search button's and the account button's accessible names not containing their visible text, prose links told apart by colour only, a scroll container the keyboard cannot reach | axe-core, WCAG 2.2 AA, in the new browser suite. The first three were already fixed in the Bink demo's copy of the shared site kit and are ported from it |

## Backlog (ordered)

Round 1: defects D1-D10, each with a regression test.

Round 2: authoring safety and feedback.
- Tour definition validation (`Validate()`; errors fail `StartAsync` with a precise message, warnings are logged):
  duplicate or blank step ids, `GoTo` actions and routes that point nowhere, steps that can never be left.
- Validation feedback when `CanAdvance` holds the user: a message that is shown and announced, and an
  `AdvanceBlocked` event.
- Declarative parity: the `BonboardingTour` parameters that today need the `Configure` escape hatch.

Round 3: orchestration and eligibility.
- Competing tours: an automatic start no longer cancels a running tour; it queues, ordered by priority.
- Snooze ("remind me later") with a persisted `SnoozedUntil`, honoured by `StartOnceAsync` and auto-start.
- Application signals: a step can wait for a named signal raised by application code (`SignalAsync`).
- Progress migration when a tour's version is bumped, instead of always discarding.

Round 4: presentation primitives beyond tours.
- Beacons (hotspots) positioned in the browser with no per-frame interop.
- Onboarding checklists with persisted task state, linked to tours or completed by the application.
- A tour launcher (resource centre entry point) listing what can be started or replayed.

Round 5: positioning and platform.
- Visual-viewport and safe-area aware placement (virtual keyboard, pinch zoom, notches).
- Correct placement when the host sits inside a transformed ancestor (containing block offset).
- Spotlight clipped to the scroll containers that clip the target.

Round 6: persistence.
- Cross-tab consistency (`storage` events invalidate the mirror; a tour finished in another tab stops here).
- Per-user storage scope and a session-storage option.

Round 7: performance.
- Geometry-only frames stop re-rendering the popover content.

Round 8: browser verification.
- `Bit.Bonboarding.Tests.E2E` (Playwright, axe-core) against the demo, plus a manually triggered CI workflow.

Round 9: documentation, demo pages and MCP surface for everything above.

Round 10: final adversarial audit.

## Requirements matrix

Status: **Done** (implemented and verified by tests), **Done (round n)** when this programme added or fixed it,
**Partial**, **Deferred** (with the reason), **N/A**. Priority: P0 correctness/safety, P1 essential, P2 advanced,
P3 specialised. "E2E" means the Playwright suite (Chromium, Firefox, WebKit).

### A. Tours and walkthroughs

| Requirement | P | Status | Evidence / notes |
| --- | --- | --- | --- |
| Sequential, conditional (`When`), branching (`ResolveNext`, `GoTo` actions), Back through the visit history | P1 | Done | TourSessionTests |
| Start/pause/resume/restart/stop, start at a step, resume saved position | P1 | Done | TourSessionTests, PersistenceTests |
| Validation before advancing, and saying why | P1 | Done (round 2) | `ValidationMessage`, `AdvanceBlocked`; AuthoringTests, E2E `A_held_advance_says_why` |
| Dynamic steps, live edits of a running tour | P1 | Done | declarative tours re-project; ComponentTests |
| Fluent and declarative authoring, typed events | P1 | Done | object initialisers and `BonboardingTour`/`BonboardingStep`; a separate fluent builder adds nothing over C# initialisers and was not built |
| Definition validation | P1 | Done (round 2) | `Validate()`, 13 rules, enforced by `StartAsync`; AuthoringTests |
| Nested guidance | P3 | Partial | several sessions with `StopOthers = false` and explicit hosts; no parent/child lifecycle - no demonstrated need |

### B. Presentation

| Requirement | P | Status | Evidence / notes |
| --- | --- | --- | --- |
| Popover with spotlight, centred modal, templates, actions, progress | P1 | Done | ComponentTests, E2E TourTests |
| Hints and beacons | P1 | Done (round 4) | `BonboardingBeacon`; GuidanceComponentTests, E2E GuidanceTests |
| Checklists | P1 | Done (round 4) | `BonboardingChecklist`; GuidanceComponentTests, E2E |
| Resource centre / launcher | P2 | Done (round 4) | `BonboardingTourList` (the launcher's content; the drawer around it is the application's) |
| Banners, NPS/surveys | P3 | N/A | outside an onboarding library; an announcement is a one-step centred tour |

### C. Targets

| Requirement | P | Status | Evidence / notes |
| --- | --- | --- | --- |
| Selector, anchor, element reference, dynamic resolver, several elements | P1 | Done | TourSessionTests |
| Missing, late, replaced targets; wait/skip/centre/fail | P0 | Done | TourSessionTests, E2E |
| Hidden targets, responsive duplicates | P0 | Done (round 1) | E2E ResilienceTests |
| Target moved or hidden with nothing resized | P1 | Done (round 1) | E2E `The_spotlight_follows_a_target_that_moves_with_nothing_resized` |
| Invalid selector | P1 | Done (round 1) | TourSessionTests |
| Shadow DOM, iframes | P3 | Deferred | not reliably observable; README "Known limitations" |

### D. Positioning and visuals

| Requirement | P | Status | Evidence / notes |
| --- | --- | --- | --- |
| 12 placements + auto + centre, shift before flip, arrow clamping | P1 | Done | PlacementEngineTests |
| Logical sides for RTL | P2 | Done (round 5) | PlacementEngineTests, E2E PlacementTests |
| Visual viewport, safe areas | P1 | Done (round 5) | engine test with a non-zero viewport origin; README |
| Transformed ancestor of the host | P1 | Done (round 5) | E2E `A_host_inside_a_transformed_ancestor_still_frames_its_target` |
| Spotlight clipped to scroll panes | P2 | Done (round 5) | E2E |
| Reduced motion, forced colors, dark mode | P1 | Done (rounds 1, 10) | E2E ThemeTests |

### E. Interactive onboarding

| Requirement | P | Status | Evidence / notes |
| --- | --- | --- | --- |
| Advance on a DOM event, re-armed when held | P1 | Done (round 1) | TourSessionTests, E2E |
| Advance on an application signal | P1 | Done (round 2) | AuthoringTests |
| Hooks before/after, async, cancellation | P1 | Done | TourSessionTests |
| Consequential actions run once per request | P0 | Done (round 1) | TourSessionTests, ComponentTests |
| Cross-route workflows | P1 | Done | RoutingTests; route waits no longer block the page (round 1) |

### F-H. Lifecycle, persistence, eligibility

| Requirement | P | Status | Evidence / notes |
| --- | --- | --- | --- |
| Competing tours: queue, priority, skip | P1 | Done (round 3) | OrchestrationTests |
| Snooze | P1 | Done (round 3) | OrchestrationTests |
| Show once, role/flag gates (`CanStart`, `When`), route scoping | P1 | Done | existing tests |
| Versioned progress, migration | P1 | Done (round 3) | OrchestrationTests |
| Sticky completion | P0 | Done (round 1) | PersistenceTests |
| Per-user scope, session storage | P1 | Done (round 6) | StorageTests |
| Cross-tab consistency | P1 | Done (round 6) | StorageTests, E2E CrossTabTests |
| Server-backed stores | P1 | Done | `IBonboardingStore`, `IBonboardingStoreEvents` |
| Scheduling and expiry windows | P3 | Partial | `CanStart` covers it; no dedicated members (documented) |
| Persisted global throttle | P3 | Deferred | see round 2-3 notes |

### I-L. Developer experience, accessibility, theming, localisation

| Requirement | P | Status | Evidence / notes |
| --- | --- | --- | --- |
| Declarative parity with the model | P1 | Done (round 2) | `BonboardingTour`/`BonboardingStep` parameters |
| Dialog semantics, focus management, live region, keyboard | P0 | Done | ComponentTests, E2E TourTests |
| WCAG 2.2 AA (automated) on every demo page and the tour UI | P1 | Done (rounds 1, 4) | E2E AccessibilityTests (axe-core) |
| Focus visible in forced colors | P1 | Done (round 10) | E2E ThemeTests |
| Theme tokens for every component, light/dark/system | P1 | Done (rounds 1, 4) | MCP oracle tests, E2E |
| Localisable labels for every built-in string | P1 | Done | `BonboardingLabels` (12 new) |

### M-P. Hosting, performance, reliability, diagnostics

| Requirement | P | Status | Evidence / notes |
| --- | --- | --- | --- |
| Server, WebAssembly, prerendering | P1 | Done | E2E HostingTests (both renderers), existing prerender tests |
| MAUI Blazor Hybrid | P2 | Deferred | not verified on a device in this programme; no browser-only API is used beyond `localStorage`, which the WebView provides |
| No polling, one measurement per frame, no interop when idle | P0 | Done | design; round 7 removed per-frame re-renders of the card |
| Errors in hooks, stores, browser; disposal; races | P0 | Done | ResilienceTests, TourSessionTests |
| Structured logging of invalid definitions, invalid selectors, store failures | P1 | Done | `ILogger`; no telemetry is collected |

## Iteration log

| Round | Scope | Outcome |
| --- | --- | --- |
| 1 | Defects D1-D13, demo accessibility, browser test suite (moved forward from round 8, since every later round changes the browser layer too) | All fixed. +14 engine/component tests (240), +1 MCP test (51), new `Bit.Bonboarding.Tests.E2E` with 31 tests passing in Chromium (installed Chrome 156), Firefox and WebKit, `bit.ci.Bonboarding.e2e.yml` (manual, three engines). New public API: `BonboardingWaitReason`, `IBonboardingTourSession.WaitReason` (default interface implementation, so existing implementations still compile), `BonboardingStepGeometry.InvalidSelector`, `BonboardingStepRenderContext.IsRunning(action)`. Behaviour changes, each a fix of documented intent: the system theme follows a dark OS; nothing blocks the page while a step waits for its target or route; completion is sticky per version; hidden targets count as missing |

| 2 | Authoring safety and feedback | `BonboardingTourDefinition.Validate()` with 13 rules (errors refuse `StartAsync` with every problem listed, warnings are logged); validation feedback (`ValidationMessage` on the step and on `BonboardingStepContext`, shown with `role="alert"`, `AdvanceBlocked` event); application signals (`AdvanceOnSignal`, `IBonboardingService.SignalAsync`, brought forward from round 3); declarative parity (16 more `BonboardingStep` and 20 more `BonboardingTour` parameters, including `CanStart`). A declarative `AutoStart` of an invalid tour logs instead of throwing out of a lifecycle method. +14 tests |
| 3 | Orchestration and eligibility | Competing tours: `BonboardingStartOptions.WhenBusy` (`Replace` default, `Queue`, `Skip`) and `BonboardingTourDefinition.Priority`; queued starts re-check eligibility when their turn comes; declarative `AutoStart` queues (fixing a page's auto tour cancelling the multi-page tour the user was taking) and withdraws its queued start with its page. Snooze: `SnoozeAsync`, `BonboardingTourStatus/EndReason.Snoozed`, `TourSnoozed`, `BonboardingRecord.SnoozedUntil`, `BonboardingStepAction.RemindMeLater`, honoured by automatic starts only. `MigrateRecord` for version bumps. `TimeProvider` registered and used for every record time. +17 tests (271 total) |

| 4 | Guidance on the page | `BonboardingBeacon` (positioned by the browser layer with shared observers and no per-frame interop; hidden with its target, while clipped by a scroll pane and while a tour runs; opens a non-modal hint, a tour or a handler; persisted by `Id`; focus moves to its target when it goes for good), `BonboardingChecklist` (items done by tour, condition, signal or `SetChecklistItemAsync`; stored as `checklist:{id}` records in the existing store; dismissible; progress bar and spoken item states), `BonboardingTourList` (new / in progress / completed with Start / Resume / Replay, accessible names including the tour title). Theme tokens moved to `.bit-bonboarding-root, .bit-bonboarding-themed` so the on-page components theme with the tours. New demo page `/guidance`. +17 component tests, +8 browser tests |
| 5 | Positioning and platform | Placement inside the visual viewport and the safe-area insets (virtual keyboard, pinch zoom, notches); positions relative to the host's containing block (a host inside a transformed ancestor); spotlight clipped to the scroll panes that clip the target; logical sides `InlineStart`/`InlineEnd` (+Top/Bottom). +8 engine tests, +3 browser tests. 44/44 browser tests in Chromium, Firefox and WebKit |
| 6 | Persistence | `IBonboardingStoreEvents` (optional; the browser store implements it with the `storage` event): another tab's change drops the mirrored copy, refreshes checklists and tour lists, and stops a tour that tab ended since this one started, without writing over it. `BonboardingOptions.StorageScope` (per-user keys `@{owner}/`, per-owner mirror, scoped clear), `StorageLocation` (`Session`). +11 storage tests, +2 browser tests |
| 7 | Rendering cost | Geometry-only frames keep the render context (no allocation, no localizer lookups) and the built-in card skips re-rendering: in 20 frames of a moving target, 20 card renders and 60 localizer lookups before, 0 and 0 after (measured by `A_Moving_Target_Repositions_Without_Rebuilding_The_Card`). Templates still re-render every frame, since one may read the geometry |

| 8 | Browser verification | Done in round 1 and extended every round since: 48 browser tests in 8 classes, a per-test timeout (`testconfig.json`) so a hang is reported rather than blocking the run, and `HostingTests` proving a tour on both renderers InteractiveAuto picks (the demo exposes which one runs) |
| 9 | Documentation, demo, MCP | README sections for every addition, an "Upgrading" table of every behaviour change, "Known limitations", the API reference brought up to date (components, services, options, enums, labels, browser layer); demo pages `/guidance` and the validating tour; MCP server instructions; package README features |
| 10 | Final adversarial audit | Found and fixed: the "no way forward" warning ignored a step's own `Body`; checklist items and tour lists ignored a version bump of a tour that was not registered; a beacon hint forced `Persist = true` over a global `false`; focus rings (drawn with `box-shadow`) were invisible in forced colors, a pre-existing WCAG 2.4.7 failure; element ids built from user ids broke `aria-labelledby` when an id held a space; the one pre-existing build warning. +3 unit tests, +1 browser test |
| 11 | Demo review in three engines | Every demo page driven by hand-written Playwright scripts in Chromium (installed Chrome 156), Firefox and WebKit, at 1280 and 390 pixels wide: every step's geometry (in the viewport, framing its target, not covering it, the target actually shown by its panes), focus, announcements and console. Found and fixed: a target hidden in a pane left scrolled was never scrolled to; a target scrolled wholly out of its pane was spotlit at its own box, over the content beside the pane; Tab escaped the trap of a `TargetOnly` step instead of reaching its target; completing a tour with Next reported `StepLeft` twice; the Flows page's feature flag could never be switched on (a `bool` bound to a `<select>`); the Workspace page told the reader to use the docs navigation that the step's blocker covers; the usage beacon on `/guidance` could not be brought back; the welcome tour invited a shortcut that opens the site's palette beneath its blocker. The accessibility tests now let a fade-in finish before axe reads colours (the waiting card failed contrast intermittently in Firefox, measured mid-fade). +1 engine test, +6 browser tests (new `FlowTests`) |

### Final verification (end of round 10)

- `dotnet build Bit.Bonboarding.slnx`: succeeds with **no warnings** (the baseline had one).
- `Bit.Bonboarding.Tests` (net10.0): **311 / 311** (baseline 226). net8.0 and net9.0 cannot run on this machine (only the
  .NET 10 runtime is installed); CI runs them.
- `Bit.Bonboarding.Tests.Mcp`: **51 / 51** (baseline 50).
- `Bit.Bonboarding.Tests.E2E`: **48 / 48** in Chromium (installed Chrome 156, `BONBOARDING_E2E_CHANNEL=chrome`, because
  this machine has no download of the Chromium build Playwright 1.64 pins), **48 / 48 in Firefox and 48 / 48 in
  WebKit**, none skipped or inconclusive. Two
  earlier runs had browser processes crash ("Process exited", "Page crashed") while the library was being rebuilt
  underneath them; clean re-runs passed.

### Follow-up backlog (prioritised)

1. Run the new `bit.ci.Bonboarding.e2e.yml` workflow once on GitHub (it has only run locally) and the unit tests on
   net8.0/net9.0 there.
2. Verify on MAUI Blazor Hybrid (WebView) on a device; nothing browser-only is used beyond `localStorage`.
3. A persisted global throttle for automatic tours, if users ask for one (deliberately not built; see round 2-3).
4. A screen-reader pass by hand (NVDA, VoiceOver) over a tour, a hint and the checklist; automated rules cover only a
   part of it.
5. Shadow DOM targets, if a component library built on web components needs them (needs an observation strategy
   that sees into shadow roots, or it would not be reliable).

### Round 11 review notes

- Verification: `dotnet build Bit.Bonboarding.slnx` with no warnings; `Bit.Bonboarding.Tests` 329 / 329 and
  `Bit.Bonboarding.Tests.Mcp` 51 / 51 on net10.0; `Bit.Bonboarding.Tests.E2E` 54 / 54 in Chromium (installed Chrome
  156), Firefox and WebKit.

- The browser suite passed 48 / 48 in all three engines before this round, yet the scripts found eight defects: the
  suite checks the bounding boxes of the spotlight and the target, and a target hidden by its pane still has a box in
  the middle of the screen. The scripts also checked what each pane shows of the target, and every page in every
  engine at a phone width as well as a desktop one.
- A target scrolled wholly out of its pane is now pointed at as a one-pixel line on the pane's edge, the limit of
  the partly clipped case, rather than collapsing the step into a centred card: .NET reads a rectangle with no area
  as no target at all, and a card jumping to the middle of the screen while the user scrolls is worse than one that
  stays by the pane. `TargetVisible` is still reported false for it, and still read by nothing in .NET.
- The scroll check skips ancestors that clip without scrolling: scrolling cannot show more of a target there, and
  `scrollIntoView` would scroll the page for nothing.
- The focus trap moves focus by hand only when Tab leaves a scope (the popover, or a target); within one, the
  browser's own order stands, so a modal step behaves exactly as before.
- Left as they are, on purpose: in WebKit a mouse click does not focus a button, so a tour started that way gives
  focus back to whatever had it (`<main>` here), as Safari does everywhere; a dismissed or completed tour starts
  again from the top, and a replay is not resumed after a reload (completion is sticky); `Circle` draws a pill
  around a target that is not square; the progress bar is empty on the first step and full on the last.
- Not fixed, needing a decision: dismissing a checklist drops focus to the body, and the library cannot know where
  it should go (an `OnDismissed` callback would let the application say). The site kit's `/` and Ctrl+K open the
  palette beneath a modal tour's blocker, where Escape then ends the tour rather than closing the palette;
  `site.js` is the same file in sixteen demos, so a fix (ignore the shortcut while an `aria-modal` dialog is open)
  belongs in all of them at once. The welcome tour no longer invites the shortcut mid-tour.

### Round 4-7 review notes

- Beacons were first written to hide whenever another session was active, and their own hint's `TourStarted` arrived
  before the start returned, so the beacon hid under its own hint and lost the focus it should get back. Fixed by
  recognising its own sessions by tour id; caught by the component tests.
- A session is added to `Sessions` before it starts, so `SessionsChanged` alone does not say when a tour begins;
  the on-page components also follow `TourStarted` and `TourPaused` in the event stream.
- The containing-block compensation uses the host's own box, so it costs one `getBoundingClientRect` per frame and
  needs no knowledge of which ancestor is transformed. A scaled ancestor (`zoom`, `scale()`) is not compensated;
  documented as a limitation.
- Deliberately not built: shadow DOM piercing selectors. Waiting for an element inside a shadow root would not be
  reliable (mutation observers do not see into shadow roots), and a half-working feature is worse than a documented
  limitation. Same for iframes.
- Per-frame cost after round 7: one measurement in the browser (target, popover, viewport, host, clipping panes),
  one interop call only when something moved, one placement computation, one render of the host whose diff touches
  only the positioned styles.

### Round 2-3 review notes

- New members of public enums are appended, so persisted or analytics values keep their meaning. Code that switches
  exhaustively over `BonboardingTourStatus`, `BonboardingTourEndReason`, `BonboardingEventKind` or
  `BonboardingStepActionEffect` gets new cases to consider; that is the price of the new states and is noted in the
  migration notes below.
- New `IBonboardingTourSession` members (`WaitReason`, `ValidationMessage`, `SnoozeAsync`) have default
  implementations, so an application's own implementation or test double still compiles. `IBonboardingService`'s
  new members (`SignalAsync` and the checklist members) do not: their only possible default does nothing, and a
  decorator that inherited it would silently drop signals and checklist progress. The guidance components reach
  what they need beyond the interface through the internal `BonboardingGuidanceRegistry`, never by downcasting the
  service, so they keep working behind a decorator.
- A queued session is `Idle`, sits in `Sessions` and is found by `FindSession`; `Active` stays on the running tour
  until the queued one's turn comes. `StopAllAsync` cancels queued starts before running ones, so none slips in.
- Deliberately not built: a persisted global throttle ("at most one automatic tour a day"). The queue already keeps
  automatic tours from stacking up in a session, `CanStart` can implement any throttle an application wants, and a
  persisted cross-tour rule would need a store record that is not about any one tour. Revisit if asked for.

### Round 1 review notes

- Every fix was proven by a test that exercises the defect: engine/component tests for D2, D3, D5, D6, D8, D9, D11;
  browser tests for D4 (trigger re-armed), D7, D11, D12 and the cascade of D1/D13.
- Accessibility of the busy state: a running action is `aria-busy` and `aria-disabled`, not `disabled`, because a
  disabled button drops the focus of the keyboard user who pressed it.
- An `AdvanceOn` click that hides its own target (a menu button that closes itself) is not reported as a lost target
  while the advance is pending; once it has been decided, the target is measured again.

## Competitive research (October 2026)

Products reviewed: Driver.js 1.10, Shepherd 15.3, Intro.js 8.5, React Joyride 3.2, Reactour, NextStep/Onborda,
Appcues, Pendo, Userpilot, Userflow, Chameleon, WalkMe, UserGuiding, Product Fruits, and the Blazor options
(BootstrapBlazor.DriverJs, Blazor.DriverJs, GuideFlow, PSC.Blazor.Components.Tours, STGTour, Blazor.FlowGuide).

- Table stakes Bonboarding already had: popover and modal steps, adjustable cutout, interaction blocking, overlay
  click and Escape policy, arrow-key navigation, focus trap with dialog semantics, smooth scrolling, collision-aware
  placement, progress, localisable labels, templates, async hooks, waiting for and skipping missing targets,
  advance on a DOM event, runtime step changes, start at step N, an analytics event stream, cross-route tours (only
  NextStep/Onborda among the JS libraries have those, and Onborda with a hard-coded 500 ms delay).
- Table stakes it lacked: beacons/hints (Driver.js, Intro.js, Joyride and every commercial platform), checklists
  with a launcher (Driver.js 1.10 and every platform), a resource centre / launcher listing guidance, frequency and
  show-once rules beyond "once", snooze, a global throttle and priority between competing flows.
- Worth adopting: Driver.js separates closing a hint (beacon stays) from dismissing it (gone), hides beacons while a
  tour runs, and keeps them still under reduced motion; checklist items complete by predicate or start a tour; Pendo's
  `guideTimeout`-style reporting of a step that failed to show; WalkMe-style field validation guidance (our
  validation message); Userpilot's "remind me later" (our snooze).
- Recurring pain points elsewhere, and where Bonboarding stands: stacking contexts (our cutout is drawn above the page
  and never lifts the target, so it does not have intro.js's z-index problem), scroll containers and fixed headers
  (handled by `scrollIntoView` per ancestor and capture-phase scroll listeners; clipping is round 5), re-rendered
  targets (handled; extended to hidden and moved targets in round 1), framework double mounts (the transition lock and
  host registry), host modals and the native `<dialog>` top layer (a known limitation, documented in round 9),
  mobile keyboards and pinch zoom (round 5), shadow DOM and iframes (evaluated in round 5).
- No Blazor option has async per-step hooks with cancellation, re-resolution after re-renders, waiting, action-driven
  advance, hints, checklists, targeting rules or RTL.
