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
| Core | colours (hex, rgb(), hsl()) | V | sRGB and OKLab spaces |
| Core | named colours, `rgb(r g b / a)`, `oklch()`/`oklab()`, `lab()`/`lch()`/`hwb()`/`color()` | V (round 1) | was: parser returned null, so these snapped although `BmProps` documented named colours |
| Core | same-unit dimensions, complex strings (filters, shadows) | V | browser-played on both runtimes (round 2); to/from `none` and stylesheet shadows in the page's own form mix (round 3) |
| Core | implicit starting values (no `Initial`) for non-transform properties | V (round 2) | was: unknown colours started from `rgba(0,0,0,0)`, unknown dimensions snapped. Now read off the computed style (AD-2) |
| Core | `height`/`width: "auto"` (accordion) | V (round 3) | was: snaps. Measured, played to, lands on `auto`; from `auto` starts at the size on screen |
| Core | keyframes, `times`, per-segment `eases`, `Bm.Current` wildcard | V | |
| Core | easing presets, cubic-bezier, steps | V | |
| Core | spring (physics + bounce/duration), rest thresholds | V | |
| Core | inertia, `modifyTarget`, min/max | V (round 3) | was: coast pre-clamped (braked from the release to stop on the bound). Now coasts at its own speed and bounces off the bound (`BounceStiffness`/`BounceDamping`) |
| Core | velocity inheritance when a spring is retargeted | V (round 2) | was: both paths restarted from the authored velocity (0) |
| Core | pause / resume / speed / stop / complete | V | programmatic controls + `<Bmotion>` instance methods |
| Core | seek / progress inspection / reverse | M | no `Time`/`Progress` on controls |
| Hosting | Blazor Server: transform/opacity tweens and springs | V | browser timeline |
| Hosting | Blazor Server: gesture overlays (hover/tap/focus/in-view/drag) | V (round 2) | was: snapped on Server (the README claimed they played) |
| Hosting | Blazor Server: colours, dimensions, filters, path drawing, keyframes on them, inertia, arcs | V (round 2) | was: snapped (only transform + opacity offloaded). Only per-frame callbacks and `d`/`points` morphs still need the frame loop |
| Hosting | Blazor Server: reduced-motion fades | V (round 2) | was: even the retained opacity fade snapped |
| Hosting | Blazor Server: drag | P | follows the pointer one round trip behind (one position per frame); the throw plays in the browser (backlog 17) |
| Declarative | Initial / Animate / Exit / While* / variants / State | V | |
| Declarative | presets / reusable effects | V (round 3) | was: none. `BmPresets` (23), `Preset` parameter, `+` composition |
| Orchestration | variants, stagger (from, grid), delayChildren, when | V | |
| Orchestration | sequences (`at`, labels), one playhead | V | |
| Presence | Sync / Wait / PopLayout, keyed switch, keyed list, re-entry | V | |
| Presence | exiting content inert (not focusable / clickable) | V (round 3) | `BitBmotionOptions.InertWhileExiting`, default on |
| Presence | custom components taking part in presence (`usePresence`) | V (round 3) | was: only `<Bmotion>` children registered. `BmotionPresenceContext.Register()` → `BmPresenceRegistration.SafeToRemove()` |
| Layout | FLIP (position / size), anchor, scroll, root, dependency, LayoutId | V | |
| Gestures | hover, tap (+ keyboard), focus, pan, drag (axis, constraints, elastic, momentum, snap, handles, controls) | V | |
| Gestures | reorder group by keyboard (pick up, move, drop, cancel; announced) | V (round 3) | was: pointer only |
| Gestures | hover ignores touch pointers | V (round 1) | was: touch taps fired hover start/end |
| Gestures | keyboard tap on native `<button>` | V (round 1) | was: Space was `preventDefault`ed on every tappable element, so Space on a `<button>` inside `WhileTap` did not click it |
| Gestures | `WhileFocus` on keyboard focus only (`:focus-visible`, motion.dev) | V (round 3) | was: any focus, a mouse click included |
| Scroll | native scroll timelines + scrub fallback | V | |
| Scroll | `BmotionScrollTracker` | V (round 3) | was: one interop call per scroll event, queueing behind a Server round trip. Now one call in flight, latest wins; a slow callback no longer holds the motion values back |
| Gestures | pan | V (round 3) | was: one interop call per pointermove. Now one in flight, the moves meanwhile folded (deltas summed); the last one always arrives before the end |
| SVG | path drawing, `d` morph (same commands), motion path, arc | V | browser-played on both runtimes except the `d`/`points` morph (an attribute), which needs the frame loop |
| Values | `BmValue`, transform, velocity, spring follower, template | V | |
| View transitions | `BmotionViewTransition.StartAsync` | V (round 3) | was: ignores reduced motion, no types. Now types + reduced-motion policy; no ready/finished split yet |
| A11y | reduced motion policy | V | default kept at `IgnoreUnlessConfigured` (AD-3); what is kept under reduction widened (round 3) |
| Diagnostics | inspector, server-degradation warning, CSS safe mode | V | |

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
| 15 | P2 | Controls: progress / time / seek | unit | open |
| 16 | P2 | Reduced-motion default → `User` (prerelease-only package, so a documented default change) | unit + docs | decided against (AD-3) |
| 17 | P3 | Drag on Server applied locally in the browser | E2E server | open |
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
| Controls: `time` / seek / `duration` / `state` / reverse speed | yes | `seek`, `progress`, `reverse` | `currentTime` | **no** |
| Colours: modern syntaxes, oklch, wide gamut | yes (12.37) | yes | yes | yes (round 1) |
| Animating unknown starting values (read computed) | yes | yes | implicit keyframes | yes (round 2) |
| `height: auto` | yes (measures) | `height: "auto"` | `interpolate-size` (Chrome only) | yes (round 3, measures; both runtimes) |
| Presence: sync/wait/popLayout, `initial={false}`, `custom`, `usePresence` | yes | - | `@starting-style` (CSS only) | yes (custom components via `Register()`, round 3), and exiting content is `inert` (round 3; Motion leaves it interactive) |
| Layout / shared layout (`layoutId`), crossfade, scale correction | yes | Flip plugin | View Transitions | yes (no crossfade while both mounted) |
| Gestures: hover ignores touch, press keyboard, focus-visible | hover ignores touch; press = Enter; whileFocus needs `:focus-visible` | - | - | hover/press fixed (round 1); whileFocus follows `:focus-visible` (round 3) |
| Drag: constraints, elastic, momentum, snap, controls, direction lock | yes | Draggable | - | yes (form fields no longer start a drag, round 3) |
| Reorder: grid axis, keyboard reorder, auto-scroll | yes (14.6 keyboard) | - | - | keyboard reorder with announcements (round 3); **vertical/horizontal only, no auto-scroll** |
| Scroll: scroll-linked (native timelines), in-view | yes | ScrollTrigger (pin, scrub, snap) | ScrollTimeline/ViewTimeline (FF 159+) | yes; **no pinning/scrub smoothing** |
| Motion values, transform, spring, velocity, template | yes | `quickTo` | - | yes |
| Sequences with `at`, labels | yes | timelines, position param | - | yes |
| View transitions | `animateView` | Flip | `startViewTransition` | wrapper with types and the reduced-motion policy (round 3); **no `animateView`-style element API** |
| Reduced motion | `MotionConfig reducedMotion` (default `never`) | `matchMedia` (opt-in) | media query | policy option (default `IgnoreUnlessConfigured`) |
| Text splitting | Motion+ `splitText` | SplitText (free) | - | `BmotionSplitText` |
| Number animation | Motion+ `AnimateNumber` | - | `@property` counters | programmatic only |
| Presets / reusable effects | - (user land) | - | - | `BmPresets`, 23 composable presets (round 3) |
| Blazor Server parity | n/a | n/a | n/a | everything except per-frame callbacks and `d` morphs (round 2) |

Other Blazor libraries (NuGet): BlazorAnimate (AOS), animate.css wrappers, Toolbelt.Blazor.ViewTransition,
BlazorTransitionableRoute, LumexUI.Motion (wraps Motion JS, incomplete), Navius.Motion (preview). None matches
Bmotion's scope of springs, gestures, drag, FLIP layout and presence in C#.

## 7. Final report (end of round 3)

### 7.1 What changed for users

* **The same animations on Blazor Server as on WebAssembly.** Before, Server played transform/opacity tweens and
  snapped everything else, gesture overlays included. Now the browser plays whatever it can reproduce exactly (AD-1):
  colours, sizes, filters, shadows, keyframes, springs, inertia, arcs, hovers and exits. Only per-frame callbacks and
  SVG `d`/`points` morphs still need the frame loop.
* **Fidelity to what is on screen.** Starting values the engine never set are read off the page, and values
  borrowed from the stylesheet are handed back after a gesture (AD-2). Every CSS colour syntax is understood and
  mixed premultiplied, and an element ends on the colour that was written. Retargeted springs keep their velocity.
  `width`/`height` animate to and from `auto`. Filters and shadows animate from and to `none`, and shadows in the
  page's own form mix with authored ones.
* **Declarative presets**: `BmPresets` (23 of them) through `Preset`, composable with `+`.
* **Accessibility**: Exiting content is `inert`. `WhileFocus` follows keyboard focus. Reorder lists work by keyboard,
  with announcements. Hover ignores touch, and Space/Enter on native controls is left to the control. Reduced motion
  still fades filters and shadows, and view transitions honour it.
* **Feel**: inertia bounces off its bounds instead of braking from the release. Heavily damped springs stay stable.
* **Extensibility**: components of the app's own take part in exits (`BmotionPresenceContext.Register()`).
  View transitions take types.
* **Server efficiency**: one scroll or pan message in flight at a time.

### 7.2 Feature coverage

Section 2 (inventory) and section 6 (competitive matrix) are the coverage matrix, kept current; every row says
whether it is verified, partial, or missing and why.

### 7.3 Architecture

* **AD-1, one start path; the browser plays what it can reproduce exactly.** Every start goes through `PlayAsync`:
  read what is unknown, interrupt the overlapping browser animations (folding their values and velocity back in),
  land the zero-length keys, and hand the rest to the browser. It goes as a two-keyframe timeline when exact, or
  else as a recording of the frame loop's own drivers on a forked state. The engine mirrors every browser animation
  so it can interrupt without reading the DOM.
  *Trade-off*: a recording costs a few KB of interop and a few ms of main thread per start (measured in round 2).
  The timeline shape is preferred whenever it is exact.
* **AD-2, borrowed values are handed back.** Values read from the stylesheet are re-read when a gesture lets go, and
  their inline declaration is removed afterwards, so a later stylesheet change still applies.
* **AD-3, the reduced-motion default stays.** This matches Motion's and doesn't silently change existing apps. What
  "reduced" keeps was widened instead.
* Round 3 added no new subsystems. Measured sizes ride the same read, `auto`/`none` are handled where the endpoints
  are resolved, keyboard reordering reuses the drag session's slot maths and commit, and the interop additions all
  have no-op defaults.

### 7.4 Files

Library: `Engine/BmotionAnimationEngine.cs`, `Engine/BmotionElementAnimationState.cs`, `Engine/BmotionFrameRecorder.cs`
(new), `Engine/BmotionColorInterpolator.cs`, `Engine/BmotionStringMixer.cs`, `Engine/BmotionInertiaDriver.cs`,
`Engine/BmotionSpringDriver.cs`, `Components/Bmotion.cs`, `Components/BmotionReorderGroup.cs`,
`Context/BmotionPresenceContext.cs`, `Models/BmPreset.cs`, `Models/BmPresets.cs`, `Models/BmViewTransitionOptions.cs`,
`Models/BmReorderAnnouncement.cs` (new), `Models/BmTransition.cs`, `Models/BmotionTransitionConfig.cs`, `Models/BmProps.cs`,
`Services/BmotionViewTransition.cs`, `Services/BmotionScrollTracker.cs`, `Interop/*`, `Scripts/bit-bmotion.ts`,
`BitBmotionOptions.cs`, `BmotionCapabilities.cs`.
Tests: unit (`AutoSizeTests`, `NeutralNoneTests`, `BrowserPlaybackTests`, `ColorParsingTests`, `PresetTests`,
`PresenceParticipationTests`, `InertiaDriverTests`, `SpringDriverTests`, `ReorderGroupTests`, …), JS
(`exit-inert`, `keyboard-press`, `latest-only`), E2E (`FidelityTests`, `PresenceTests`, `GesturesTests`, `DragTests`,
`ReorderTests`) and the harness pages behind them. MCP server and tests (playback analysis, catalog, review rules,
guides). Docs: `README.md`, this plan, demo pages (`Presets` new; `Accessibility`, `DragPage`, `GettingStarted`,
`Home`, `Gestures`, `ReorderPage` corrected). `git status` has the complete list.

### 7.4b Iteration summary

Section 5 records each round: what its review found, what it changed and how that was validated. Round 1 covered
the baseline, research, nine defects confirmed by failing browser tests, the bridge input fixes and the colour parser.
Round 2 covered the core engine (AD-1, AD-2), Server parity, velocity and the recorder. Round 3 covered presets,
`auto`, string fidelity, view transitions, presence participation, inert exits, inertia bounce, `:focus-visible`,
keyboard reorder, Server interop backpressure, docs and the MCP server.

### 7.5 Validation (final runs)

Everything below was run on this machine (Windows 10, .NET SDK 10.0.402, Chrome 156 through Playwright's `chrome`
channel, since Playwright's own Chromium could not be downloaded here). Baseline is section 1.

| Check | Baseline | Final |
|---|---|---|
| `dotnet build Bit.Bmotion.slnx --no-incremental` (net8/9/10) | 0 warnings | 0 warnings, 0 errors |
| Unit tests, net10.0 | 792 | **934/934**, three consecutive runs |
| Unit tests, net9.0 / net8.0 builds | not run (no runtimes) | 934/934 and 933/933 - on the .NET 10 runtime via `DOTNET_ROLL_FORWARD=Major`; the 8/9 runtimes themselves are CI-only |
| MCP server tests | 371 | **409/409** |
| JS tests (vitest) | 38 | **49/49** |
| E2E, `wasm` / `server` | 140 / 140 | **156/156** / **156/156** |
| E2E, `auto` | not run | **156/156** |
| E2E, `wasm-noprerender` / `server-noprerender` | not run | **151 passed, 5 skipped** each (the prerender takeover tests, which need prerendering) |
| E2E, `wasm` in Release | not run | **156/156** |

Negative controls - each browser test for a fix was run against a build with that fix disabled, and failed:
velocity inheritance (3/3, 0 px of coast), shadow alignment and filter `none` (both "jumped"), exiting `inert`
(timed out waiting for it), inertia bounce (crept to 143 px of 150 px in 1.5 s), `:focus-visible` ("a mouse click
raised the focus gesture"); unit: the rejected-animation replay (started 4.7 px in).

Performance (round 2, Debug WebAssembly): a retarget click that records a soft 3 s spring blocked the main thread
27 ms before the recorder was optimised and 18 ms after; a two-keyframe timeline start costs 5 ms. Release/AOT builds
were not measured. Server interop for scroll and pan is now bounded to one message in flight (by construction, JS
unit-tested; not measured on a real network).

Accessibility checks: keyboard paths are browser-tested (tap on native buttons, `:focus-visible`, keyboard
reorder with focus kept, `inert` exits); screen-reader output is verified as live-region text in the DOM, not with an
actual screen reader. Touch is tested with Playwright's emulated touch, not on devices.

Not verified here: Firefox and Safari (neither this run nor the repository's CI uses them - CI runs Chromium across
ten host/framework combinations), real .NET 8/9 runtimes (CI covers them), published trimmed/AOT builds.

### 7.6 Remaining backlog

| P | Gap | Why it is open | Next step |
|---|---|---|---|
| P2 | Controls: `Time` / `Duration` / `State` / seek | Seek is exact only for browser-played animations (WAAPI `currentTime` plus the mirror); a frame-loop spring can't run backwards without re-simulating | Read-only time/state first; seek for browser plans; document the frame-loop limit |
| P3 | Drag applied locally on Server | The bridge would have to own the transform during a drag while the engine owns its other components; every hand-over (overlay, release, momentum) has a visible-jump failure mode | CSS `translate` for the drag offset, cleared in the same flush that writes the final transform |
| P3 | Reorder grids, auto-scroll near edges | Slot maths is one-dimensional | 2-D slots from measured rects; scroll the container while held near an edge |
| P3 | LayoutId crossfade while both elements are mounted | Shared-layout handoff assumes one is leaving | Crossfade opacity across the pair |
| P3 | Scroll pinning / scrub smoothing (GSAP ScrollTrigger) | Out of Motion's scope too | `position: sticky` recipes; a smoothing follower for `BmotionScrollTracker` |
| P3 | Interpolating lengths across units from the page (`px` to `%`) | The C# mirror can't convert `%` without layout | Measure the target in px for the browser, as `auto` does |

### 7.7 Breaking changes and migration

No public member was removed or renamed. All new interface members have default implementations, and every new
parameter defaults to the old behaviour unless noted below. Behaviour changes:

| Change | Migration |
|---|---|
| Exiting content is `inert` while its exit plays | `BitBmotionOptions.InertWhileExiting = false` restores interactive exits |
| `WhileFocus` / `OnFocusStart` / `OnFocusEnd` follow keyboard focus (`:focus-visible`) | For "any focus", handle `@onfocusin` on the element |
| Inertia with `Min`/`Max` coasts at its own speed and bounces | `BounceDamping` ≈ `2·√BounceStiffness` for no bounce |
| Reorder items become focusable (`tabindex="0"` when they had none) and take Space/Enter/arrows | `Keyboard="false"` |
| Hover ignores touch; Space on native controls is no longer prevented; keyboard tap needs the key on the element | None needed (bug fixes) |
| Values borrowed from the stylesheet are handed back after a gesture (inline declaration removed) | None needed (bug fix) |
| `BmotionScrollTracker` doesn't await the scroll callback before taking the next position | A callback that must not overlap itself should guard against re-entry |
| Server plays (rather than snaps) colours, sizes, filters, keyframes, springs, inertia | None needed; animations that used to snap now animate |

### 7.8 Assessment

**Production-ready on Chromium-based browsers for Blazor WebAssembly, Server and Auto, within the limits listed above.
Firefox and Safari are not yet verified.**

The evidence: every documented capability has unit tests and the user-visible ones browser tests on all three
hosting modes. The fixes of this initiative were each shown to fail without the fix. The suite runs green in
Debug and Release with and without prerendering. The engine has one start path with one rule for what the browser
plays (AD-1), so a behaviour proved on one path holds on the others, and the frame loop and the browser can't
disagree because the browser plays the frame loop's own recording.

What keeps the verdict short of unconditional:

* Cross-browser coverage is Chromium only. The bridge uses features with good but not universal support
  (`linear()` easing, `oklab()`, `:focus-visible`, `inert`, typed view transitions). Each has a fallback or a
  feature check. The C# fallbacks for `linear()` and `oklab()` are unit-tested; none has been run in those
  browsers.
* The playback controls can't report or seek time yet (7.6). That is the most visible gap against Motion and
  GSAP for apps that scrub animations.
* Drag on Blazor Server follows the pointer a round trip behind. That is fine on a LAN and noticeable over a slow
  link.
* Behaviour changes (7.7) are improvements, but apps that relied on interactive exits or on focus overlays from
  mouse clicks need the documented opt-outs.

Recommended before a stable release: run the existing E2E suite under Firefox and WebKit, since Playwright supports
both and the harness is browser-agnostic. Then do the read-only half of controls time/state.
