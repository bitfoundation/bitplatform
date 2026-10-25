# Bit.Bmotion improvement plan

A living document for the Bmotion improvement initiative: what the library does today, what it is missing, what
is being changed and why, and what has been verified. It is updated at the end of every review round.

## 1. Baseline (round 1, 2026-10-25)

### Build and tests before any change

| Check | Result |
|---|---|
| `dotnet build Bit.Bmotion.slnx` (Debug, net8/9/10) | succeeded, 0 warnings |
| `Bit.Bmotion.Tests` (net10.0) | 792 / 792 passed |
| `Bit.Bmotion.Tests.Mcp` (net10.0) | 371 / 371 passed |
| `bit-bmotion-js` (vitest) | 38 / 38 passed |
| `Bit.Bmotion.Tests.E2E`, host `wasm` (Chrome 156) | 140 / 140 passed |
| `Bit.Bmotion.Tests.E2E`, host `server` | see round 1 results below |

Only the .NET 10 runtime is installed on this machine; the net8.0/net9.0 test builds compile but are not run here
(CI runs them).

### Architecture as found

* **Public API**: `<Bmotion>` (wraps one authored element and rewrites its render frames to inject id + initial
  style), presence components (`BmotionAnimatePresence`, `BmotionPresenceSwitch`, `BmotionPresenceGroup`),
  `BmotionReorderGroup`, `BmotionSplitText`, `BmotionLayoutGroup`, `BmotionConfig`, `BmotionInspector`; the `Bm`
  facade (`Bm.To`, `Bm.Spring`, `Bm.Tween`, `Bm.Inertia`, `Bm.Stagger`, `Bm.Value`, ...); `BmotionAnimateService`
  (selector / `ElementReference` / motion value / number / `BmSequence`), `BmotionScrollTracker`,
  `BmotionViewTransition`, `BmotionCapabilities`.
* **Engine**: all animation maths in C# (`BmotionAnimationEngine` + one `BmotionElementAnimationState` per element,
  drivers for tween / spring / inertia / keyframes / colour / arc). Two execution paths:
  * *frame loop* (WebAssembly only): the bridge's `requestAnimationFrame` calls `ComputeFrame` synchronously each
    frame and applies the returned styles;
  * *compositor offload*: tweens and zero-velocity springs on transform components and opacity are pre-sampled in C#
    (`linear()` easing) and played by `element.animate()`; the engine keeps a mirrored plan so it can interrupt
    without reading the DOM. This is the only animated path on Blazor Server.
* **Bridge** (`Scripts/bit-bmotion.ts`, ~1400 lines): rAF loop, style application, pointer/focus/keyboard listeners,
  drag maths, IntersectionObserver pooling, FLIP playback, WAAPI playback, scroll timelines (native + scrub
  fallback), scroll tracking, view transitions.
* **Prerendering**: `BmPrerenderMode` (`Initial` / `Final` / `Animate` = pure-CSS enter animation), with persisted
  state so the interactive runtime takes over at rest.

## 2. Feature inventory

Status legend: **V** implemented and verified (unit + browser), **T** implemented, insufficiently tested,
**P** partial, **M** missing, **D** inadequately documented, **R** needs redesign, **N/A** not applicable.

| Area | Capability | Status | Notes |
|---|---|---|---|
| Core | transform components, opacity | V | |
| Core | colours (hex, rgb(), hsl()) | V | sRGB and OKLab spaces; OKLab goes to the browser only where it interpolates in OKLab (round 4: Firefox parses `oklab()` but mixes in sRGB, so it gets the frame loop) |
| Core | named colours, `rgb(r g b / a)`, `oklch()`/`oklab()`, `lab()`/`lch()`/`hwb()`/`color()` | V (round 1) | was: parser returned null, so these snapped although `BmProps` documented named colours |
| Core | same-unit dimensions, complex strings (filters, shadows) | V | browser-played on both runtimes (round 2); to/from `none` and stylesheet shadows in the page's own form mix (round 3) |
| Core | implicit starting values (no `Initial`) for non-transform properties | V (round 2) | was: unknown colours started from `rgba(0,0,0,0)`, unknown dimensions snapped. Now read off the computed style (AD-2) |
| Core | `height`/`width: "auto"` (accordion) | V (round 3) | was: snaps. Measured, played to, lands on `auto`; from `auto` starts at the size on screen |
| Core | lengths across units (`px` ↔ `%` / `rem` / `vw` …) | V (round 4) | was: snapped. Plays through `calc()` of both ends - CSS's own interpolation, exact, nothing measured; lands on the authored value |
| Core | keyframes, `times`, per-segment `eases`, `Bm.Current` wildcard | V | |
| Core | easing presets, cubic-bezier, steps | V | |
| Core | spring (physics + bounce/duration), rest thresholds | V | |
| Core | inertia, `modifyTarget`, min/max | V (round 3) | was: coast pre-clamped (braked from the release to stop on the bound). Now coasts at its own speed and bounces off the bound (`BounceStiffness`/`BounceDamping`) |
| Core | velocity inheritance when a spring is retargeted | V (round 2) | was: both paths restarted from the authored velocity (0) |
| Core | pause / resume / speed / stop / complete | V | programmatic controls + `<Bmotion>` instance methods |
| Core | `Time` / `Duration` / `Progress` / `State` / seek on controls | V (round 4) | in-place seek for browser plans, replay from captured starts otherwise, re-armed after the end; sequences replay (overlapping same-property segments approximated) |
| Core | reverse playback (negative speed) | M | |
| Hosting | Blazor Server: transform/opacity tweens and springs | V | browser timeline |
| Hosting | Blazor Server: gesture overlays (hover/tap/focus/in-view/drag) | V (round 2) | was: snapped on Server (the README claimed they played) |
| Hosting | Blazor Server: colours, dimensions, filters, path drawing, keyframes on them, inertia, arcs | V (round 2) | was: snapped (only transform + opacity offloaded). Only per-frame callbacks and `d`/`points` morphs still need the frame loop |
| Hosting | Blazor Server: reduced-motion fades | V (round 2) | was: even the retained opacity fade snapped |
| Hosting | Blazor Server: drag | V (round 4) | was: one round trip behind the pointer. The bridge shows the drag with CSS `translate` until .NET's transform catches up; cleared once .NET has the final position (backlog 17) |
| Hosting | Blazor Server: layout animations (FLIP) | V (round 4) | was: the element showed at its new place for the round trip before the FLIP (Firefox and WebKit: one frame, 340 px away). The bridge holds it at the FLIP's first frame from the moment the render lands |
| Declarative | Initial / Animate / Exit / While* / variants / State | V | |
| Declarative | presets / reusable effects | V (round 3) | was: none. `BmPresets` (23), `Preset` parameter, `+` composition |
| Orchestration | variants, stagger (from, grid), delayChildren, when | V | |
| Orchestration | sequences (`at`, labels), one playhead | V | |
| Presence | Sync / Wait / PopLayout, keyed switch, keyed list, re-entry | V | |
| Presence | exiting content inert (not focusable / clickable) | V (round 3) | `BitBmotionOptions.InertWhileExiting`, default on |
| Presence | custom components taking part in presence (`usePresence`) | V (round 3) | was: only `<Bmotion>` children registered. `BmotionPresenceContext.Register()` → `BmPresenceRegistration.SafeToRemove()` |
| Layout | FLIP (position / size), anchor, scroll, root, dependency, LayoutId | V | |
| Layout | LayoutId crossfade while both holders are mounted | V (round 4) | was: the new holder flipped from the old one's last recorded rect, the old one just exited. Now the pair crossfade (`filter: opacity()`, multiplying with their own opacity) and the old one travels onto the new box; `LayoutCrossfade` |
| Gestures | hover, tap (+ keyboard), focus, pan, drag (axis, constraints, elastic, momentum, snap, handles, controls) | V | |
| Gestures | reorder group by keyboard (pick up, move, drop, cancel; announced) | V (round 3) | was: pointer only |
| Gestures | reorder grids (`Axis = Both`), keyboard rows | V (round 4) | was: one-dimensional slots. Nearest cell by centre; ↑/↓ move a row |
| Gestures | reorder / drag auto-scroll, drag scroll compensation | V (round 4) | was: a scroll under a drag left the element behind, and nothing scrolled. `DragAutoScroll` (reorder default on), capped at the extent the drag started with |
| Gestures | drag release velocity | V (round 4) | was: the last two moves' velocity - a spike when moves came back to back (WebKit flung 619 px past a bound), and kept after the pointer stopped. Now the average of the last 100 ms (motion.dev) |
| Gestures | hover ignores touch pointers | V (round 1) | was: touch taps fired hover start/end |
| Gestures | keyboard tap on native `<button>` | V (round 1) | was: Space was `preventDefault`ed on every tappable element, so Space on a `<button>` inside `WhileTap` did not click it |
| Gestures | `WhileFocus` on keyboard focus only (`:focus-visible`, motion.dev) | V (round 3) | was: any focus, a mouse click included |
| Scroll | native scroll timelines + scrub fallback | V | |
| Scroll | smoothed scroll-linked animation (`useSpring(scrollYProgress)`) | V (round 4) | `BmScrollTimeline.Smoothed()`: a spring in the bridge between the scroll and the scrub - both runtimes, no per-frame interop |
| Scroll | `BmotionScrollTracker` | V (round 3) | was: one interop call per scroll event, queueing behind a Server round trip. Now one call in flight, latest wins; a slow callback no longer holds the motion values back |
| Gestures | pan | V (round 3) | was: one interop call per pointermove. Now one in flight, the moves meanwhile folded (deltas summed); the last one always arrives before the end |
| SVG | path drawing, `d` morph (same commands), motion path, arc | V | browser-played on both runtimes except the `d`/`points` morph (an attribute), which needs the frame loop |
| Values | `BmValue`, transform, velocity, spring follower, template | V | |
| View transitions | `BmotionViewTransition.StartAsync` | V (round 3) | was: ignores reduced motion, no types. Now types + reduced-motion policy; no ready/finished split yet |
| A11y | reduced motion policy | V | default kept at `IgnoreUnlessConfigured` (AD-3); what is kept under reduction widened (round 3) |
| Diagnostics | inspector, server-degradation warning, CSS safe mode | V | |
| Browsers | Chromium, Firefox, WebKit | V (round 4) | the full E2E suite on all three (`BMOTION_E2E_BROWSER`); found and fixed: Firefox's sRGB `oklab()` mixing, WebKit's `commitStyles()` that writes nothing |

## 3. Prioritised backlog

P0 correctness / accessibility · P1 essential · P2 advanced · P3 extended. Each item lists its acceptance check.

| # | P | Item | Acceptance | Status |
|---|---|---|---|---|
| 1 | P0 | Read implicit starting values from the computed style for properties the engine has not seen; restore authored CSS when a gesture lets go of a property it took from the stylesheet | E2E: a CSS-blue element hovered to red animates blue→red and back to blue, with no inline background left afterwards | done (round 1-2) |
| 2 | P0 | Gesture layers and drag release use the same offload-or-frame-loop path as `Animate` | E2E on `server`: intermediate hover frames observed | done (round 1-2) |
| 3 | P0 | Hover ignores touch pointers | E2E with touch: a tap raises tap, not hover | done (round 1-2) |
| 4 | P0 | Keyboard activation of native buttons inside `WhileTap` | E2E: Space and Enter on a `<button>` raise its `@onclick` | done (round 1-2) |
| 5 | P0 | Reduced-motion fades play on Server; view transitions honour the reduced-motion policy | unit + E2E | done (round 2-3) |
| 6 | P1 | Spring velocity inheritance on interruption (frame loop and compositor) | unit: retargeted spring starts with the old velocity; E2E: no velocity kink | done (round 2-3) |
| 7 | P1 | Browser-played animations for every interpolable property (colours, lengths, complex strings, path drawing, keyframes, per-property transitions, inertia) with exact C# mirrors | unit + E2E on `server`: colour/width tweens show intermediate frames | done (round 2-3) |
| 8 | P1 | Colour parser: named colours, modern syntax, `oklab()`/`oklch()`, `transparent` | unit | done (round 1-2) |
| 9 | P1 | Scroll tracker and pan coalesce to one interop call per frame | JS unit + E2E | done (round 3: one call in flight - scroll latest wins, pan deltas summed) |
| 10 | P1 | `height: "auto"` / `width: "auto"` animations | E2E accordion | done (round 3) |
| 11 | P1 | Presets: composable fade / slide / scale / pop / ... with a `Preset` parameter | unit + demo | done (round 3) |
| 12 | P2 | Exiting content is `inert` | E2E: exiting button not focusable | done (round 3) |
| 13 | P2 | Public presence participation for custom components | unit | done (round 3) |
| 14 | P2 | Inertia boundary bounce | unit | done (round 3) |
| 15 | P2 | Controls: progress / time / seek | unit + E2E | done (round 4) |
| 16 | P2 | Reduced-motion default → `User` (prerelease-only package, so a documented default change) | unit + docs | decided against (AD-3) |
| 17 | P3 | Drag on Server applied locally in the browser | E2E server | done (round 4) |
| 19 | P3 | Reorder grids | unit + E2E | done (round 4) |
| 20 | P3 | Drag scroll compensation and auto-scroll (reorder default) | JS unit + E2E | done (round 4) |
| 21 | P3 | LayoutId crossfade while both holders are mounted | unit + E2E | done (round 4) |
| 22 | P3 | Smoothed scroll timelines | JS unit + unit + E2E | done (round 4) |
| 23 | P3 | Lengths across units | unit + E2E | done (round 4) |
| 24 | P1 | Firefox and WebKit: run the suite, fix what they find | E2E in both | done (round 4) |
| 18 | P3 | Docs / demo pages for the new features, hosting guide, troubleshooting | review | done (round 3): README, a Presets demo page, the keyboard reorder section, every Server claim in the demo; no separate troubleshooting guide (the MCP server's debugging prompt and setup guide cover it) |

## 4. Architectural decisions

### AD-1: one start path, and the browser plays whatever it can reproduce exactly

Every animation start - `Animate`/variants, gesture overlays and their revert, drag release, exits, programmatic
calls - goes through one engine method (`PlayAsync`) that, in order:

1. reads the starting values the engine has never seen from the computed style (one batched interop call, cached
   until the value is handed back to the stylesheet), so nothing starts from a guessed identity value;
2. interrupts the browser animations it overlaps, folding their values *and velocities* back into the state;
3. applies zero-length keys at once;
4. hands the rest to the browser as a Web Animation when it can be reproduced exactly, in one of two shapes:
   * **timeline** - two keyframes and the curve as the timing easing (`linear()` samples or an exact keyword):
     every key shares one tween or zero-velocity spring. Transform components, opacity, path drawing, colours
     (normalised to `rgba()`, or `oklab()` for an OKLab transition), same-unit lengths, and complex strings the
     browser interpolates the same way the C# mixer does (filters, shadows, clip paths, ...);
   * **sampled** - anything else the frame loop could play (per-property transitions, keyframe sequences with
     per-segment easing, springs carrying velocity, inertia, arcs, gradients, custom properties): the animation is
     *recorded* by running the very same drivers on a forked state at 60 Hz, and the browser plays the frames with
     a linear timing. Recording is what guarantees the browser and the frame loop never disagree;
5. runs the remainder on the frame loop (WebAssembly) - per-frame callbacks, a playback rate other than 1, SVG
   geometry attributes (`d`, `points`), transforms while a drag owns them - or lands it at once (Server).

The engine keeps a mirror of every browser animation (the same samples) so it can interrupt without reading the
DOM. On Blazor Server the C# clock and the browser's are a network hop apart, so there a browser animation starts
from the *implicit* current value (no offset-0 keyframe) and the hop never shows as a jump. The one exception (round
3) is a size the page still shows as `auto`: `auto` doesn't interpolate, so the first keyframe carries the measured
start for those keys alone.

Trade-off: a sampled animation carries ~60 keyframes per second of animation over the interop (a few KB), against a
handful for a timeline one; the timeline shape is preferred whenever it is exact.

### AD-2: values borrowed from the stylesheet are handed back

A property the engine read from the computed style (rather than one a target set) is *borrowed*. When the last
gesture that took it lets go, it animates back to the value the stylesheet gives it *now* and then its inline
declaration is removed, so later stylesheet changes (a theme switch, a `:hover` rule, a class) apply again.

### AD-3: the reduced-motion default stays `IgnoreUnlessConfigured` (backlog 16, decided not to change)

Changing the default to `User` was considered and rejected for this release. Motion's own default (`reducedMotion:
"never"`) also ignores the OS preference unless configured, so changing it would make Bmotion differ from the library
its users port from. It would also silently change the look of every existing app on upgrade, and that is not a
correctness fix: the option exists, is documented as the recommended setting, and `BmotionConfig` overrides it locally.
The README keeps recommending `User` for new apps and flags the default as planned to change in a future major
version. What *was* changed is what "reduced" keeps: opacity, colours, filters and shadows still fade (round 3), view
transitions are skipped, and fades play on Server too (round 2).

## 5. Round log

### Round 1 - baseline and architecture

* Baseline recorded (section 1): build clean, 792 unit / 371 MCP / 38 JS / 140 + 140 E2E (wasm, server) green.
* Competitive research (Motion v14.6, GSAP 3.15, browser support as of Chrome 156 / Firefox 158 / Safari 27) summarised
  in section 6.
* Wrote `Fidelity.razor` + `FidelityTests` asserting the *correct* behaviour of nine suspected defects; on the unchanged
  library 7/9 failed on WebAssembly and 9/9 on Blazor Server, confirming every one of them (inventory rows marked **R**).
* Fixed in the bridge: hover ignores touch pointers and pointers carrying a drag (motion.dev semantics); keyboard press
  only applies to the element itself (keys typed in a field inside a tappable card belong to the field) and never
  `preventDefault`s Space on native controls - **Space on a `<button>` inside `WhileTap` did not click it**.
* Colour parser rewritten: named colours, `transparent`, modern `rgb()/hsl()` syntax with `/ alpha` and percentages,
  hue units, `hwb()`, `lab()/lch()`, `oklab()/oklch()`, `color(srgb | srgb-linear | display-p3)`; out-of-gamut colours
  clamp. The string mixer tokenises the same syntaxes (named colours inside shadows). Colour drivers end on the
  *authored* string. Interpolation is premultiplied (CSS semantics; the old mix darkened fades from `transparent`).

### Round 2 - core engine (AD-1, AD-2)

* One start path (`PlayAsync`) for `Animate`, gesture layers and their release, drag release, exits and programmatic
  calls. Gesture layers are registered synchronously and played through it, so they animate on Blazor Server, and an
  activation overtaken by its own deactivation (an enter waiting on a read when the pointer leaves) no longer plays.
* Unseen starting values are read off the computed style; values borrowed from the stylesheet go back to it after a
  gesture (re-read at release, inline declaration removed afterwards).
* Browser playback generalised (AD-1): timeline shape for every interpolable value, recorded shape for everything else
  the frame loop can play; the remaining frame-loop-only cases are per-frame callbacks, a playback rate other than 1
  (WebAssembly), `d`/`points` morphs, transforms under a drag, endless reversing repeats and recordings over 10 s.
  Recordings are simplified (frames linear interpolation reproduces within 0.1 px / 0.002 opacity are dropped) and
  composed only for kept frames.
* Velocity: every key's last two frame-loop samples and every interrupted browser animation hand their velocity to a
  spring that replaces them (unless it authors one or repeats). A browser animation that continues an interrupted one
  starts at the instant the bridge cancelled that one, so building it can't show as a stall.
* Semantic fixes found on the way: the compositor path eased a keyframe sequence once across the whole sequence while
  the frame loop and the prerendered CSS ease each segment (motion.dev) - all paths now ease per segment; a mirrored
  repeat played its way back time-reversed (eased in) in the browser but eased out on the frame loop - both now match
  motion.dev's mirror; colour springs had degraded to a 0.3 s tween; a reduced-motion fade snapped on Server.
* Diagnostics and the inspector report what the browser is animating (`BrowserProperties`, "N in browser").

**Measurements** (Debug WebAssembly, headless Chrome 156): a click that retargets a soft 3 s spring blocked the main
thread 27 ms with a 190-keyframe recording, 26 ms after simplification (70 keyframes), 18 ms once only kept frames are
composed; a click starting a two-keyframe timeline costs 5 ms. Release/AOT builds are several times faster.

**Validation**: unit 851/851 (net10.0), JS 41/41, `FidelityTests` 10/10 on wasm and server; the velocity test was
checked against a build with inheritance disabled (fails 6/6) and passes 9/9 with it.

### Round 3 - declarative API, sizes, fidelity of strings, accessibility

* **Presets** (backlog 11): `BmPreset` (Initial / Animate / Exit / While* / Viewport / Transition) and 23 `BmPresets`
  factories - enter/exit (Fade, Slide, Scale, Pop, Blur, Rotate, Flip), overlays (Modal, Drawer, Popover, Tooltip,
  Toast), gestures (HoverLift, HoverScale, Press), attention (Pulse, Shake, Wiggle, Jump, Heartbeat), Reveal, Spin,
  Shimmer. `<Bmotion Preset>` supplies only what the element leaves unset; presets compose with `+` target by target
  (`BmProps` gained `With` / `+`, with a reflection guard that `Merge` covers every property). Every built-in renders
  and plays on WebAssembly and Server (unit).
* **`width`/`height` to and from `"auto"`** (backlog 10): the natural size is measured (inline size set to `auto` for
  an instant, in the same read as the starting values), played to, and the element then lands on `auto` itself so it
  follows its content; an animation from `auto` starts at the size on screen. Interrupted, it stays where it was
  taken; an instant transition is simply `auto`; an element that isn't laid out snaps as before. The accordion E2E
  found a Server-only bug in the plan of AD-1: the implicit first keyframe started the browser from `auto`, which
  doesn't interpolate, so the collapse flipped at the halfway point - such sizes now get their measured start
  (those keys only), see AD-1.
* **Strings that start from what the page reports** (found by the MCP agent's measurements, confirmed in the
  browser): a filter or shadow whose computed value is `none` jumped instead of animating, and a stylesheet shadow
  never mixed with an authored one (the page reports `rgba(…) 0px 4px 12px 0px`, colour first with the spread).
  `none` ends now play through the other end's neutral values (CSS semantics: `blur(0px)`, `brightness(1)` /
  `100%`, transparent zero-size shadows), shadows are aligned to one form and padded to the same count, and the
  element ends on the authored value (`none` included). Both E2E tests fail with the alignment disabled.
* **View transitions**: `BmViewTransitionOptions.Types` (typed transitions, untyped fallback) and the reduced-motion
  policy (skipped when motion is reduced; `ReduceMotion` overrides per call).
* **Reduced motion** keeps `filter`, `backdropFilter`, `boxShadow` and `textShadow` fades, like opacity and colour.
* **Drag** no longer starts from a press on a form field inside the draggable element.
* **Exiting content is `inert`** (backlog 12) while its exit plays, set by the bridge (rendering it would re-create
  static child content mid-exit); a cancelled exit makes it interactive again; an app's own `inert` is kept. Opt out
  with `BitBmotionOptions.InertWhileExiting = false`. *Behaviour change*: exiting content used to stay interactive.
* **Inertia bounces off its bounds** (backlog 14, motion.dev semantics): the coast heads for the (snapped) projection
  at the speed it was thrown with, and a spring (`BmInertia.BounceStiffness`/`BounceDamping`, default 500/10) takes
  over with its velocity where it crosses `Min`/`Max`; released outside them it springs straight back. Before, the
  projection was clamped first, so a throw at a nearby bound braked from the release onwards - in the E2E a flick
  released at 100px towards a bound at 150px crept to 143px in 1.5 s, where it now reaches the bound moving, bounces
  ~10px past it and settles. A drag's momentum bounces with the drag's own snap-back spring (400/35) unless its
  `DragTransition` says otherwise.
* **Heavily damped springs stayed finite only below damping ≈ 1800**: the integrator's 1 ms sub-step floor let
  explicit damping flip and amplify the velocity each step (non-finite values, a spring that never rests -
  `Bm.Spring(damping: 5000)` or a hard inertia wall). Damping is applied implicitly where explicit damping would be
  unstable; ordinary springs keep their exact trajectories (every existing test unchanged).
* **Presence participation** (backlog 13, motion.dev's `usePresence`): `BmotionPresenceContext` is public, and a
  component of the app's own registers with it (`Register()` → `BmPresenceRegistration`: `IsPresent`,
  `SafeToRemove()`, `Dispose()`), so an exit that isn't a `<Bmotion>` (a CSS transition, a canvas) keeps the content
  mounted - in every presence component and mode; a re-entry cancels it, a late `SafeToRemove` completes nothing.
* **`WhileFocus` follows keyboard focus** (`:focus-visible`, motion.dev semantics): a button clicked with the mouse
  no longer plays the focus overlay or raises `OnFocusStart`. *Behaviour change*. The E2E test fails with the check
  disabled.
* **Scroll tracking and pan on Server** (backlog 9): one `OnScroll` / `OnPanMove` call in flight at a time - the
  latest scroll position going next, pan moves folded into one (deltas summed, the last one flushed before
  `OnPanEnd`) - so nothing queues behind a round trip. The scroll callback is started rather than awaited, so a slow
  handler can't hold the scroll-linked motion values back. (Scroll events are frame-aligned already; per-frame
  coalescing alone would have saved nothing.)
* **Keyboard reordering** (`BmotionReorderGroup.Keyboard`, default on): Space/Enter picks the focused item up (it lifts
  with `WhileDrag`), the arrow keys along the axis move it while the others make way exactly as for a drag,
  Space/Enter drops it (focus stays on it after the re-render), Escape puts it back, focus leaving drops it. Every step
  goes to a polite live region (`IBmotionInterop.AnnounceAsync`; `Announce` words or translates it). Items are made
  focusable; keys in a field or button inside an item stay theirs. Alt+arrows was rejected: Alt+←/→ is the browser's
  Back/Forward on Windows and Linux.
* Bugs found on the way: (1) the reorder group enumerated its entries across awaits, so a parameter update arriving
  mid-loop threw "collection was modified" on WebAssembly - every awaiting loop now walks a snapshot; (2) an animation
  the browser rejected was folded back into the state at its wall-clock age before the frame loop replayed it, so the
  replay started a few pixels in (a flaky culture test exposed it: 11 ms of JIT time on the first run) - a rejection
  inside the never-started window now restores the plan's start.
* **Docs**: README (engine and hosting claims, presets, sizes, colour syntax, filters/shadows, view transitions,
  inert, presence participation, inertia bounce, keyboard reorder, keyboard, focus and touch semantics), a new demo
  page for presets, the demo's reorder keyboard section, demo pages that still claimed colours/inertia/drag snap on Server,
  `BmotionCapabilities`, `IBmotionInterop.IsInProcess` and the Server warning text.
* **MCP server** (subagent): the playback analysis runs the real engine as WebAssembly and as Server and reports the
  browser shape (timeline / recorded / frame loop / instant) with reasons and remedies; the property catalog is
  measured rather than hand-written; review rules and guides follow the new rule.
* Test robustness: the velocity test detects the retarget at 80% of peak speed (the half-peak detection lost the
  coast at 30 fps under suite load) - with inheritance disabled it fails 3/3 (0 px of coast), with it it passes 3/3;
  the accordion samples every frame in the page.

**Validation**: unit 934/934 (net10.0; net9.0 934/934 and net8.0 933/933 on the .NET 10 runtime), MCP 409/409, JS
49/49. Mid-round full E2E runs: 154/154 on wasm and on server (before keyboard reorder and the scroll/pan/focus
changes); every group touched after that re-run on both hosts (fidelity 12/12, presence 11/11, gestures + scroll +
drag 29/29, reorder 6/6). Final full runs: section 7.5. Each new browser test was checked against a build without its
fix (velocity, shadows/filters, inert, bounce, focus-visible, rejected replay): every one fails without it.

**Review questions (round 3)**

1. *Unsupported requirements*: seek/time on controls; drag applied locally on Server; reorder grids and auto-scroll;
   a LayoutId crossfade while both elements are mounted. See section 7.6.
2. *Gaps against Motion / GSAP / native*: Motion's `animation.time`/`duration`/`state`, GSAP's ScrollTrigger pinning
   and scrub smoothing, `animateView`-style per-element view transitions.
3. *New gaps found while testing*: Server's implicit start from `auto`, computed `none` and computed shadows, a
   reorder enumeration race, the rejected-animation replay and the spring integrator's instability under heavy
   damping. All five are fixed.
4. *API consistency*: presets compose with `+`, as `BmProps` now does. `BmotionPresenceContext` was made public
   rather than adding a parallel type, and the bounce settings are nullable so that "unset" can mean the context's
   own default (a drag's snap spring). No API was renamed.
5. *Browser-native alternatives*: `inert` and `:focus-visible` are native. `interpolate-size` (for `auto`) was
   rejected because Chrome alone ships it and the frame loop can't mirror it. View-transition types are native.
6. *Waste*: scroll and pan interop is now bounded to one call in flight. Measuring an `auto` size rides the
   existing start read (no extra round trip), and `inert` is set without awaiting.
7. *Interruptibility*: an interrupted `auto` animation doesn't land on `auto`; a re-entry cancels a custom exit and an
   inert; a pointer drag cancels a keyboard hold; keyboard steps are serialised.
8. *Lifecycle and leaks*: registrations remove themselves on dispose, the reorder group disposes its .NET reference,
   the live region is one shared node, and key listeners go with their elements (WeakSet).
9. *Reduced motion*: filters and shadows still fade. Presets and view transitions follow the policy.
10. *Input and screen readers*: exiting content is inert, focus overlays follow keyboard focus, reorder works by
    keyboard with announcements, and Space and Enter on native controls are left alone.
11. *Hosting modes*: every feature of the round was run on wasm and server; the final run adds auto (section 7.5).
12. *Implemented, not just declared*: every new parameter has a unit test, and the user-visible ones a browser test
    with a negative control.
13. *Boundaries and failure recovery*: added tests for an unmeasurable `auto` size, a rejected animation, a failed
    send in the coalescer, a disposed registration, an overtaken exit and heavy damping.
14. *Docs and demos*: the README is aligned with the code and a Presets demo page was added. No troubleshooting page
    was added beyond the MCP server's.
15. *Complexity*: one new option (`InertWhileExiting`), and the new interop methods all have no-op defaults. No new
    dependencies.
16. *Next highest-value item*: playback controls that report and set time (section 7.6).

### Round 4 - the open backlog, and Firefox and WebKit

* **Playback time** (backlog 15): controls report `Duration`, `Time`, `Progress` and `State`
  (`Running`/`Paused`/`Finished`/`Stopped`), and `Seek`/`Time =` move the playhead. A browser-played animation seeks in
  place (WAAPI `currentTime` and the mirror together - scrubbing costs nothing); anything else replays from the values
  it started from with a negative delay, the frame-loop drivers fast-forwarding frame by frame so a spring lands exactly
  where it would have been. A seek after the end takes hold again; `Play()` after the end plays from the start.
  Sequences always replay, segment by segment (a segment already over lands, one under way starts part-way, later ones
  are rescheduled); overlapping segments on one property are an approximation (documented). Found on the way: a
  negative synthetic timestamp broke the drivers' `< 0` "not started" sentinels (now NaN), and a rejected browser
  animation's replay applied the offset twice.
* **Firefox and WebKit** (backlog 24): `BMOTION_E2E_BROWSER` runs the suite in either; `serial.testconfig.json` runs
  it one class at a time (headless Firefox throttles windows that aren't in front, so parallel windows starve). Firefox
  passed 156/157 and WebKit 156/159 on the first full runs. What they found:
  * Firefox parses `oklab()` but interpolates the keyframes in sRGB (the OKLab test went through sRGB's path). The
    support check now plays red to blue on a probe and looks for green at the midpoint, so Firefox gets the C# OKLab
    frame loop. Probed by hand: Chromium 156 and WebKit 26 interpolate in OKLab, Firefox 141 doesn't.
  * WebKit (Playwright build 2370) returns from `commitStyles()` without writing anything - on one letter of a split
    text, every time, in plain HTML too, and calling it again doesn't help. The letter was left at its initial
    `opacity: 0`. Every commit is now checked: when the inline style is unchanged afterwards, the animated properties
    are written from the keyframe the animation ended on (or from what it shows, for one stopped part-way).
  * WebKit passes every Playwright pointer move on at once instead of once a frame: a three-move flick crossed the
    screen in a millisecond and flung 619 px past its bound. The release velocity is now the average over the last
    100 ms (motion.dev's window, over at least a frame) instead of the last two moves, so moves in a burst don't spike
    it - and a pointer that stood still before letting go throws nothing (it used to keep the last move's velocity: a
    held release still flew to the bound, 150 instead of 60). The tests' drags pace WebKit's moves a frame apart, as
    Chromium and Firefox deliver them.
  * The environment, not the library: Firefox under automation reports a pointer dragged out of the viewport in the
    wrong place and drops its `pointerup`, so the drags stay inside it.
* **Drag on Blazor Server shows locally** (backlog 17): while .NET is a round trip behind, the bridge shows the element
  where the drag has it with the CSS `translate` property - the difference between the drag's position and the
  transform .NET last wrote, re-based on every transform write. It goes once the transform has caught up, or, once
  .NET has acknowledged the final position (the completion of the last `SetDragPosition` orders it before every
  transform written after), at the next transform it writes, wherever the release takes the element. The E2E test
  found two bugs in the first version: a `translate: 0px` left behind when .NET had caught up before the release, and
  element-bound constraints measured with the local offset still in the rect (a parent-constrained drag stopped at
  320 × 37.5 instead of 370 × 50).
* **Scroll during a drag, and auto-scroll** (backlog 20): a scroll under a drag - of the page or of any container -
  moves the element's layout box away from a still pointer; the drag adds the shift back, measured off the box itself
  (so whatever scrolled, and an element a scroll doesn't move, come out right). `DragAutoScroll` scrolls the nearest
  scrollable container, then the page, while the pointer is near its edge (48 px, a quarter of a small container, up
  to 20 px a frame), and `BmotionReorderGroup.AutoScroll` (default on) turns it on for reorder items. Found while
  testing: the dragged row's transform counts towards its scroller's overflow, so a row carried past the end made room
  for itself and the list scrolled forever - auto-scroll is capped at the extent the drag started with; and a
  compensation on the scroll event trailed a step (18-20 px) behind - it now runs with each step, on WebAssembly in a
  new before-frame hook of the tick, ahead of `ComputeFrame`, so a step and the position it leads to share a frame.
* **Reorder grids** (backlog 19): `Axis = BmDragAxis.Both` takes the cell nearest the dragged item's centre; ↑/↓ move a
  held item a row from the keyboard.
* **LayoutId crossfade** (backlog 21): when the old holder is still mounted on its way out (presence), the new one grows
  out of the old one's live box and fades in while the old one travels onto the new box and fades out. The fade is a
  `filter: opacity()`, so it multiplies with the elements' own opacity instead of overriding their enter and exit
  fades; a re-entry takes the old one back off it; `LayoutCrossfade = false` opts out. The registry tracks the holder
  now, so a leaving holder that re-renders no longer overwrites the new holder's rect.
* **Smoothed scroll timelines** (backlog 22): `BmScrollTimeline.Smoothed(spring)` - motion.dev's
  `useSpring(scrollYProgress)`, default stiffness 100 / damping 30. A native timeline can't be smoothed, so a smoothed
  one plays through the scrub with a spring stepped in the bridge - on both runtimes, with no per-frame interop.
* **Lengths across units** (backlog 23): `"100px"` → `"50%"` used to snap. It now plays through `calc()` of both ends,
  which is how CSS itself interpolates a length-percentage pair: exact in every unit, nothing measured. (Round 3's plan
  to measure the target in px is dropped - it would have lost the `%` at the end.) The browser path hands over the two
  ends; the frame loop mixes the same `calc()`.
* **MCP server**: the playback probe gave the engine's hand-off two `Task.Yield()`s before reading it. Under load that
  read nothing, and the property catalog - cached for the life of the process - reported `top`, `width` and others as
  frame-loop only (4 MCP tests failed once). It now waits, bounded, until the engine has handed the animation to the
  browser or a driver; 3/3 runs under CPU load pass.
* **What the second Firefox and WebKit pass found** (the full matrix after the features above):
  * *Blazor Server layout animations flashed at the destination*, in every browser: the render that moves the element
    paints before its FLIP, which is a round trip behind. Chromium on localhost happened to fit both in one frame;
    Firefox and WebKit showed the element at its new place (340 px away) for a frame and then took it back. The
    snapshot before a render now arms a **layout hold** in the bridge: when the render lands (a mutation observer,
    before paint) and the box has moved, a paused animation holds the element at the FLIP's own first frame until the
    FLIP takes over, .NET lets go (no FLIP) or two seconds pass; measurements look through it.
  * *A `WhileDrag` overlay hid the drag for its duration* on WebAssembly: the element only counted as dragged from its
    first move, so the overlay started at the press went to the browser, whose transform keyframes held the element at
    the press point for 300 ms (WebKit, whose moves arrive faster, showed it). The drag now owns the transform from the
    press (`BeginDrag`); on Server nothing changes (the local `translate` shows the drag over a browser overlay).
  * *Firefox's scroll anchoring followed the dragged row* (the default `overflow-anchor: auto`): it scrolled after the
    row, the drag answered, and a frame showed the row 15 px off. Draggable elements are `overflow-anchor: none` while
    attached.
  * *WebKit's own autoscroll under a held mouse* kept scrolling into the room the dragged row's transform made, which the
    drag then carried further - a list that never ended. A drag now holds every container it is in at the extent it
    had when the drag started (in the scroll handler, before paint), and the auto-scroll clamp never scrolls backwards.
  * *A crossfading LayoutId holder showed at full strength* for the round trips before its crossfade on Server and Auto:
    it now renders hidden (`filter: opacity(0)`), which the crossfade or its cancellation takes over.
* **Docs**: README (time and seek, drag on Server, scrolling during drags, release velocity, reorder grids and long
  lists, crossfade, smoothed timelines, lengths across units), the package README's Server section, the E2E README
  (browsers, serial runs, the browser quirks the tests work around), and the demo pages and MCP guides that still said
  drag on Server trails the pointer.

**Validation**: section 7.5.

**Review questions (round 4)**

1. *Unsupported requirements*: reverse playback (negative speed); GSAP-style pinning (`position: sticky` covers most of
   it natively); an `animateView`-style element API for view transitions; real .NET 8/9 runtimes (not installed here;
   CI covers them).
2. *Gaps against Motion / GSAP / native*: Motion's reorder auto-scroll and grids, `layoutCrossfade`, `useSpring` on
   scroll and `animation.time` are now covered. Remaining: reverse speed, `animateView`, ScrollTrigger pinning.
3. *New gaps found while testing*: Firefox's sRGB OKLab mixing, WebKit's silent `commitStyles`, the velocity spike and
   the stale throw, the leftover `translate`, the skewed parent bounds, endless auto-scroll, the trailing compensation,
   the MCP probe race, a leaving holder overwriting the layout registry - and in the second pass the Server layout
   flash, the overlay hiding the drag, Firefox's anchoring, WebKit's autoscroll and the crossfade's first frames. All
   fixed, each with a test that failed first.
4. *API consistency*: `Smoothed()` is a fluent copy like the timeline factories; `DragAutoScroll`/`AutoScroll` and
   `LayoutCrossfade` follow the `Drag*`/`Layout*` naming; controls follow Motion's names (`time`, `duration`,
   `state`). The interop additions all have defaults that keep the old behaviour; no signature changed.
5. *Browser-native alternatives*: `calc()` interpolation instead of measuring, `filter: opacity()` instead of
   overriding opacity, WAAPI `currentTime` for seeks, the CSS `translate` property for the local drag.
6. *Waste*: no new per-frame interop - the local drag, auto-scroll, smoothing and crossfade run in the bridge. A seek
   of a browser plan is one call. The MCP probe waits only as long as the hand-off takes.
7. *Interruptibility*: a seek mid-flight, a drag picked up again before .NET caught up (it starts from what shows), a
   re-entry during a crossfade, a new drag over a settling one - all tested.
8. *Lifecycle and leaks*: scroll listeners and auto-scroll go with the drag and on unregistration; crossfade animations
   are keyed weakly by element; layout holders are released on dispose.
9. *Reduced motion*: the crossfade and smoothing follow the transition they are given, so an instant one lands at once.
10. *Input and screen readers*: the release velocity no longer depends on how the browser batches input; grids reorder
    by keyboard; announcements are unchanged.
11. *Hosting modes*: wasm, server and auto on Chromium; wasm on Firefox and WebKit; an AOT-published host (7.5).
12. *Implemented, not just declared*: every new parameter and behaviour has a unit or JS test and a browser test with a
    negative control (7.5).
13. *Boundaries and failure recovery*: seeks past the end and to non-finite values, an empty drag, a container with no
    room left to scroll, a leaving holder that comes back, a commit that writes nothing, an animation stopped part-way.
14. *Docs and demos*: the README and package README match the code; the demo pages and MCP guides no longer claim the
    old Server drag.
15. *Complexity*: one hook in the tick (`_beforeFrame`), one mutation observer that exists only while a Server layout
    hold is armed, four interop methods (all with defaults that keep the old behaviour), four parameters; no dependencies.
16. *Next highest-value item*: reverse playback, then measuring the bridge's and engine's size in a trimmed build.

## 6. Competitive matrix

Sources: motion.dev docs and source (v14.6, 2026-10-20), gsap.com docs (v3.15), MDN browser-compat-data. "Bmotion"
is the state after the latest round; **bold** marks a gap still open.

| Capability | Motion | GSAP | Native | Bmotion |
|---|---|---|---|---|
| Tween / spring (physics + bounce/duration) / inertia | yes | tween, inertia plugin | WAAPI, `linear()` springs | yes |
| Inertia boundary bounce (`bounceStiffness`/`bounceDamping`) | yes | `resistance` | - | yes (round 3) |
| Velocity inheritance on interruption | yes (`velocity` defaults to current) | `overwrite` + InertiaPlugin `track` | - | yes (round 2) |
| Keyframes, `times`, per-segment ease | yes | `keyframes` | yes | yes |
| Repeat loop / reverse / mirror, repeat delay | yes | `repeat`, `yoyo`, `repeatDelay` | `direction` | yes |
| Controls: play/pause/stop/complete/speed | yes | yes | yes | yes |
| Controls: `time` / seek / `duration` / `state` | yes | `seek`, `progress` | `currentTime` | yes (round 4) |
| Controls: reverse speed | yes | `reverse` | negative `playbackRate` | **no** |
| Colours: modern syntaxes, oklch, wide gamut | yes (12.37) | yes | yes | yes (round 1) |
| Animating unknown starting values (read computed) | yes | yes | implicit keyframes | yes (round 2) |
| `height: auto` | yes (measures) | `height: "auto"` | `interpolate-size` (Chrome only) | yes (round 3, measures; both runtimes) |
| Lengths across units (`px` → `%`) | yes (measures, converts) | yes (converts) | `calc()` interpolation | yes (round 4, through `calc()`, exact, keeps the unit) |
| Presence: sync/wait/popLayout, `initial={false}`, `custom`, `usePresence` | yes | - | `@starting-style` (CSS only) | yes (custom components via `Register()`, round 3), and exiting content is `inert` (round 3; Motion leaves it interactive) |
| Layout / shared layout (`layoutId`), crossfade, scale correction | yes | Flip plugin | View Transitions | yes (crossfade while both are mounted, round 4) |
| Gestures: hover ignores touch, press keyboard, focus-visible | hover ignores touch; press = Enter; whileFocus needs `:focus-visible` | - | - | hover/press fixed (round 1); whileFocus follows `:focus-visible` (round 3) |
| Drag: constraints, elastic, momentum, snap, controls, direction lock | yes | Draggable | - | yes (form fields no longer start a drag, round 3; stays under the pointer through scrolls, 100 ms release velocity, round 4) |
| Reorder: grid axis, keyboard reorder, auto-scroll | yes (14.6 keyboard) | - | - | yes: keyboard with announcements (round 3), grids and auto-scroll (round 4) |
| Scroll: scroll-linked (native timelines), in-view, smoothing | yes (`useSpring`) | ScrollTrigger (pin, scrub, snap) | ScrollTimeline/ViewTimeline (FF 159+) | yes, smoothing in the bridge (round 4); pinning is `position: sticky` (README recipe), **no spacer-based pin** |
| Motion values, transform, spring, velocity, template | yes | `quickTo` | - | yes |
| Sequences with `at`, labels | yes | timelines, position param | - | yes |
| View transitions | `animateView` | Flip | `startViewTransition` | wrapper with types and the reduced-motion policy (round 3); **no `animateView`-style element API** |
| Reduced motion | `MotionConfig reducedMotion` (default `never`) | `matchMedia` (opt-in) | media query | policy option (default `IgnoreUnlessConfigured`) |
| Text splitting | Motion+ `splitText` | SplitText (free) | - | `BmotionSplitText` |
| Number animation | Motion+ `AnimateNumber` | - | `@property` counters | programmatic only |
| Presets / reusable effects | - (user land) | - | - | `BmPresets`, 23 composable presets (round 3) |
| Blazor Server parity | n/a | n/a | n/a | everything except per-frame callbacks and `d` morphs (round 2); drag shown locally (round 4) |
| Browsers verified by the suite | all evergreen | all evergreen | - | Chromium, Firefox, WebKit (round 4) |

Other Blazor libraries (NuGet): BlazorAnimate (AOS), animate.css wrappers, Toolbelt.Blazor.ViewTransition,
BlazorTransitionableRoute, LumexUI.Motion (wraps Motion JS, incomplete), Navius.Motion (preview). None matches
Bmotion's scope of springs, gestures, drag, FLIP layout and presence in C#.

## 7. Final report (end of round 4)

### 7.1 What changed for users

Round 4 (uncommitted on top of the branch):

* **Playback time and seeking.** Controls report `Duration`, `Time`, `Progress` and `State`, and `Seek` / `Time =`
  move the playhead - a range input can scrub an animation. Browser-played animations seek in place; the rest replay
  from where they started, frame-loop springs landing exactly where they would have been.
* **Drag on Blazor Server stays under the pointer.** The browser shows the drag until .NET's transform catches up.
* **Layout animations on Blazor Server no longer flash** at the element's new place for the round trip before the
  FLIP - in every browser; Chromium on localhost had only hidden it.
* **A `WhileDrag` overlay no longer hides the drag** for its first 300 ms on WebAssembly.
* **Drags survive scrolling**, and long lists scroll themselves: an element stays under the pointer whatever scrolls
  under it, `DragAutoScroll` scrolls the container near its edge, and reorder lists do it by default.
* **The throw is the pointer's.** Momentum starts from the average velocity of the last 100 ms, so a burst of moves
  doesn't fling the element and a pointer held still before letting go throws nothing.
* **Reorder grids** (`Axis = Both`) by pointer and keyboard.
* **LayoutId crossfade** while the old holder is still leaving.
* **Smoothed scroll-linked animations** (`BmScrollTimeline.Smoothed()`), on both runtimes, without per-frame interop.
* **Lengths across units** (`100px` → `50%`) animate through `calc()` instead of snapping, and keep the unit given.
* **Firefox and WebKit** run the whole suite. Firefox now gets correct OKLab colour mixing, and WebKit no longer
  leaves an element at its starting style when its `commitStyles()` writes nothing.

Rounds 1-3 (committed): the same animations on Blazor Server as on WebAssembly (AD-1); fidelity to what is on screen
- unknown starting values read off the page and handed back to the stylesheet (AD-2), every CSS colour syntax,
retargeted springs keeping their velocity, `width`/`height` to and from `auto`, filters and shadows to and from
`none`; 23 composable presets; accessibility (inert exits, keyboard-only `WhileFocus`, keyboard reorder with
announcements, touch-proof hover, native controls left alone, reduced motion that still fades filters); inertia that
bounces off its bounds; stable heavily damped springs; custom components in exits; typed view transitions; one scroll
or pan message in flight on Server.

### 7.2 Feature coverage

Section 2 (inventory) and section 6 (competitive matrix) are the coverage matrix, kept current; every row says
whether it is verified, partial, or missing and why.

### 7.3 Architecture

* **AD-1, one start path; the browser plays what it can reproduce exactly.** Every start goes through `PlayAsync`:
  read what is unknown, interrupt the overlapping browser animations (folding their values and velocity back in),
  land the zero-length keys, and hand the rest to the browser - as a two-keyframe timeline when exact, or else as a
  recording of the frame loop's own drivers on a forked state. The engine mirrors every browser animation, so it can
  interrupt and now seek without reading the DOM. *Trade-off*: a recording costs a few KB of interop and a few ms of
  main thread per start (measured in round 2); the timeline shape is preferred whenever it is exact.
* **AD-2, borrowed values are handed back.** Values read from the stylesheet are re-read when a gesture lets go, and
  their inline declaration is removed afterwards.
* **AD-3, the reduced-motion default stays** (Motion's default; no silent change for existing apps).
* Round 4 kept to that structure and added three small mechanisms to the bridge, none with per-frame interop:
  * *A playhead on the controls.* A run generation and an offset: seeks in place go to WAAPI `currentTime` and the
    mirror; others replay from captured start values with a negative delay (drivers fast-forward frame by frame).
  * *Bridge-side presentation*, layered on what .NET writes rather than replacing it: the CSS `translate` property for
    the local Server drag, `filter: opacity()` for the crossfade, `calc()` for mixed units, a spring between a
    scroll scrub and its animation.
  * *A before-frame hook in the WebAssembly tick*, so bridge work that changes what .NET must draw (an auto-scroll
    step and the drag position after it) lands in the same frame's `ComputeFrame`.
  * *A layout hold on Blazor Server*: the snapshot before a render arms it, a mutation observer (alive only while one
    is armed) applies the FLIP's first frame before paint, and the FLIP takes it over - the one place the bridge acts
    on the DOM before .NET has seen it.
  *Trade-off*: the local drag owns the element's inline `translate` while it lasts (documented); the crossfade owns
  its `filter` for the duration of the hand-over.

### 7.4 Files

Round 4 (`git status` has the exact list, 56 files):

* Library: `Components/Bmotion.cs`, `Components/BmotionReorderGroup.cs`, `Engine/BmotionAnimationEngine.cs`,
  `Engine/BmotionElementAnimationState.cs`, the seven drivers (`Tween`, `Spring`, `Inertia`, `NumericKeyframes`,
  `ColorTween`, `ColorKeyframes`, `WaapiContinuation`), `Interop/IBmotionInterop.cs`, `Interop/BmotionInterop.cs`,
  `Models/BmPlaybackState.cs` (new), `Models/BmScrollTimeline.cs`, `Models/BmotionTransitionConfig.cs`,
  `Services/BmAnimationControls.cs`, `Services/BmotionAnimateService.cs`, `Services/BmotionLayoutRegistry.cs`,
  `Scripts/bit-bmotion.ts`, `README.md`.
* Tests: unit `ControlsTimeTests`, `LayoutCrossfadeTests`, `MixedUnitLengthTests` (new), `LayoutModeTests` (holds),
  `GestureLifecycleTests` (an overlay at the press), `ReorderGroupTests`,
  `ScrollTimelineTests`, `FakeBmotionInterop`; JS `pointer-velocity`, `commit-styles`, `auto-scroll`, `smooth-step`
  (new); E2E `BasicsTests`, `DragTests`, `LayoutTests`, `ProgrammaticTests`, `ReorderTests`, `ScrollTests`, the
  infrastructure (`BrowserLaunch`, `PlaywrightSession`, `BmotionPageTest`), `serial.testconfig.json` (new), the E2E
  README; harness pages `Basics`, `Layout`, `Programmatic`, `Reorder`, `Scroll` and `harness.css`.
* Demo and MCP server: `BmotionMotionLab.cs` (the probe race), `BmotionRecipeCatalog.cs`, `BmotionSetupGuide.cs`; demo
  pages `ReorderPage` (grids, long lists), `LayoutPage` (crossfade), `ScrollAnimations` (smoothing), `Programmatic`
  (a scrubber), and `Accessibility`, `DragPage`, `GettingStarted` corrected; `demo.css`.
* Docs: `README.md`, this plan.

### 7.4b Iteration summary

Section 5 records each round: what its review found, what it changed, and how that was validated. Round 1: baseline,
research, nine defects confirmed by failing browser tests, bridge input fixes, the colour parser. Round 2: the core
engine (AD-1, AD-2), Server parity, velocity, the recorder. Round 3: presets, `auto`, string fidelity, view
transitions, presence participation, inert exits, inertia bounce, `:focus-visible`, keyboard reorder, Server interop
backpressure, docs and the MCP server. Round 4: the whole open backlog (time and seek, local Server drag, reorder
grids and auto-scroll, crossfade, smoothing, mixed units) and the first Firefox and WebKit runs, with the defects
they found.

### 7.5 Validation (final runs)

Everything below was run on this machine: Windows 10, .NET SDK 10.0.402, Chrome 156 through Playwright's `chrome`
channel (Playwright's own Chromium couldn't be downloaded here), Playwright's Firefox and WebKit (builds 1555 and
2370), and the .NET 8.0.32 and 9.0.21 runtimes installed for this round. The baseline is section 1.

| Check | Baseline | End of round 3 | Final |
|---|---|---|---|
| `dotnet build Bit.Bmotion.slnx --no-incremental` (net8/9/10) | 0 warnings | 0 warnings | **0 warnings, 0 errors** |
| Unit tests, net10.0 | 792 | 934 | **968/968** |
| Unit tests, net9.0 / net8.0 | not run | 934 / 933, on the .NET 10 runtime | **968/968 on .NET 9.0.21, 967/967 on .NET 8.0.32** (the real runtimes) |
| MCP server tests | 371 | 409 | **409/409**, and 3/3 runs under CPU load |
| JS tests (vitest) | 38 | 49 | **71/71** |
| E2E Chromium `wasm` / `server` / `auto` | 140 / 140 / - | 156 each | **164/164** / **164/164** / **164/164** |
| E2E, AOT-compiled and trimmed `wasm` host (`dotnet publish -p:RunAOTCompilation=true`, 0 warnings) | not run | not run | **164/164** |
| E2E Firefox `wasm` / `server` | not run | not run | **164/164** / **164/164** (one class at a time) |
| E2E WebKit `wasm` / `server` | not run | not run | **164/164** / **164/164** (one class at a time) |

Negative controls - each new browser test was run against a build with its fix disabled, and failed:
* OKLab on Firefox and the WebKit commit check - their failures before the fix (Firefox: the OKLab midpoint had no
  green; WebKit: a letter at `opacity: 0` after the animation completed, in two full runs and a probe).
* Under the pointer on Server without the local drag: 12.6 px behind on every frame.
* Release velocity read at the last move instead of the release: a held release flew to the bound (150, not 60).
* The leftover `translate` and the parent bounds before their fixes: `translate` never cleared; stopped at 320 × 37.5.
* Auto-scroll without the cap: scrolled forever. Compensation on the scroll event only: the row trailed by 18-20 px.
* Smoothing sent to the native timeline: "the smoothed bar jumped: 0.5".
* Crossfade off: no frame with both cards part-way faded.
* Mixed units with the `calc()` mix disabled: jumped straight to 200 px.
* Second pass, each failing before its fix: the Server layout flash (a 340 px jump on Firefox and WebKit server), the
  overlay hiding the drag (the WebKit row 126-318 px behind the pointer), Firefox's anchoring (a frame 15 px off),
  WebKit's autoscroll (the list stopped at 381 and 350 instead of 288), the crossfade's full-strength first frames
  (Chromium `auto`).

Unit and JS controls are in the tests themselves (the old instantaneous velocity would give 1056 px a frame, not
100; a commit that writes nothing leaves the inline style unchanged).

Performance: nothing in round 4 adds per-frame interop. The local drag, auto-scroll, smoothing and crossfade run in
the bridge, and a seek of a browser plan is one call. Round 2's measurements stand (a recording start 18 ms, a
timeline start 5 ms, Debug WebAssembly). The AOT build was run, not measured.

Intermittent failures seen and not fixed, both in the tests rather than the library:
`ALeaveThatOvertakesItsEnter_LeavesNoOverlayBehind` failed once in eight full unit runs (it drives a Server-mode engine
with no synchronization context, so continuations race - the same harness limitation as an earlier `SeekingAFinished`
flake), and `Momentum_ThrownAtABound_BouncesOffIt_AndSettlesOnIt` failed once on Chromium server during a class run under
load (3/3 alone, and in the final run). One that *was* fixed: Firefox server failed
`CallbacksReRenderAnOrdinaryComponent` 3 times in 4 - the harness's event log grew as events arrived and moved the
button out from under Playwright's click; the log now has a fixed height.

Accessibility checks: keyboard paths are browser-tested (tap on native buttons, `:focus-visible`, keyboard reorder in
lists and grids with focus kept, `inert` exits). Screen-reader output is checked as live-region text in the DOM, not
with a real screen reader. Touch uses Playwright's emulated touch, not devices.

Not verified here: a real screen reader, touch devices, Safari itself (WebKit is Playwright's build of it), Firefox's
and WebKit's `auto` host and Release runs.

### 7.6 Remaining backlog

| P | Gap | Why it is open | Next step |
|---|---|---|---|
| P2 | Reverse playback (negative speed) | The frame-loop drivers integrate forwards only; a spring can't run backwards without re-simulating from its start | Replay to `Time - dt` per frame for frame-loop runs, negative `playbackRate` for browser plans |
| P3 | `animateView`-style per-element view transitions | The wrapper covers the document-level API only | Name elements with `view-transition-name` from `LayoutId` |
| P3 | Overlapping same-property segments in a seeked sequence | A later segment replays from the earlier one's end, not from mid-flight | Sample the earlier segment at the later one's start time |
| P3 | Size of the bridge and engine in a trimmed build | Not measured | Compare the AOT publish with and without Bmotion |
| P3 | Pinning beyond `position: sticky` | Native CSS covers it (README recipe with a `contain` view range); GSAP's spacer-based pin isn't planned | - |

### 7.7 Breaking changes and migration

No public member was removed or renamed. The new interface members have default implementations, and every new
parameter defaults to the old behaviour unless the table says otherwise.

| Change | Round | Migration |
|---|---|---|
| A LayoutId handed over while the old holder is still leaving crossfades (`LayoutCrossfade` default `true`) | 4 | `LayoutCrossfade="false"` |
| Reorder lists auto-scroll near their scroller's edge (`AutoScroll` default `true`) | 4 | `AutoScroll="false"` |
| A drag stays under the pointer when something scrolls under it | 4 | None needed (bug fix) |
| The release velocity is the last 100 ms average; a held release throws nothing | 4 | None needed (bug fix) |
| On Blazor Server a dragged element's inline `translate` is the bridge's while the drag lasts | 4 | Use `x`/`y`, not a `translate` of your own, on draggable elements |
| Lengths across units animate (they used to snap) | 4 | None needed |
| On Firefox, OKLab colour transitions run on the frame loop (they played through sRGB in the browser) | 4 | None needed (bug fix) |
| Draggable elements are `overflow-anchor: none` while their drag is attached | 4 | None needed; scroll anchoring picks another element |
| During a drag, the containers around it don't scroll past the extent they had when it started | 4 | None needed (only the dragged element's own overflow lies beyond) |
| On Blazor Server an element is held where it was through the render that moves it, until its FLIP | 4 | None needed (bug fix) |
| On WebAssembly a `WhileDrag` overlay plays on the frame loop, composed with the drag | 4 | None needed (bug fix) |
| Exiting content is `inert` while its exit plays | 3 | `BitBmotionOptions.InertWhileExiting = false` |
| `WhileFocus` / `OnFocusStart` / `OnFocusEnd` follow keyboard focus (`:focus-visible`) | 3 | For "any focus", handle `@onfocusin` |
| Inertia with `Min`/`Max` coasts at its own speed and bounces | 3 | `BounceDamping` ≈ `2·√BounceStiffness` for no bounce |
| Reorder items become focusable and take Space/Enter/arrows | 3 | `Keyboard="false"` |
| Hover ignores touch; Space on native controls is no longer prevented; keyboard tap needs the key on the element | 1-3 | None needed (bug fixes) |
| Values borrowed from the stylesheet are handed back after a gesture | 2 | None needed (bug fix) |
| `BmotionScrollTracker` doesn't await the scroll callback before taking the next position | 3 | Guard a callback that must not overlap itself |
| Server plays (rather than snaps) colours, sizes, filters, keyframes, springs, inertia | 2 | None needed |

### 7.8 Assessment

**Production-ready for Blazor WebAssembly, Server and Auto on Chromium, Firefox and WebKit, within the limits in 7.6.**

The evidence: the whole browser suite runs on all three engines - on Chromium in every hosting mode and as an
AOT-compiled, trimmed build - and the unit suite on the real .NET 8, 9 and 10 runtimes. Every fix of this initiative
was shown to fail without it. The engine still has one start path with one rule for what the browser plays (AD-1).
Round 4's additions in the bridge layer on top of what .NET writes rather than replacing it, so the frame loop and the
browser can't disagree about where an element is.

Running the suite in Firefox and WebKit was the most valuable thing in this round. Four of the problems they
surfaced are browser behaviours the library now works around: Firefox's sRGB `oklab()` mixing, WebKit's silent
`commitStyles`, Firefox's scroll anchoring on a dragged element, and WebKit's own autoscroll. Four more were
Bmotion's own bugs that Chromium's timing had hidden: the Server layout flash, the `WhileDrag` overlay hiding the
drag, the velocity spike and the stale throw.

What keeps the verdict short of unconditional:

* Not run here: Safari itself (the suite's WebKit is Playwright's Windows build of it), a real screen reader, touch
  devices.
* Reverse playback is missing (7.6).
* On Blazor Server a drag owns the element's inline `translate` while it lasts, and a layout animation holds the element
  through its render. Both are documented, and both only change what the old behaviour got visibly wrong.
* Behaviour changes (7.7) are improvements, but apps that relied on interactive exits, on focus overlays from mouse
  clicks, or on a LayoutId hand-over without a crossfade need the documented opt-outs.
* Two intermittent failures were seen and not fixed, both in the test harness rather than the library (7.5).

Recommended before a stable release: add Firefox and WebKit to CI (`BMOTION_E2E_BROWSER`, serial), and run the
suite once on a Mac's Safari. Then do reverse playback.
