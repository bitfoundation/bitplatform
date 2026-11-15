# Bit.Bmotion assessment

The gap analysis that opens the "world-class animation framework" plan: what Bmotion does today, measured against
every section of that plan, what this round (round 5) changed, and what stays open and why. The history of rounds 1-4
- the architecture decisions (AD-1 to AD-3), the defects they fixed and how each was validated - is in
[IMPROVEMENT_PLAN.md](IMPROVEMENT_PLAN.md); this file does not repeat it. Round 5's own report is
[Bmotion-Completion-Report.md](Bmotion-Completion-Report.md).

## Starting point

Bmotion is not a prototype to rewrite. Four improvement rounds already gave it:

- **One start path** for every animation (`PlayAsync`, AD-1): starting values the engine has never seen are read off
  the page, overlapping browser animations are folded back with their velocity, and the rest goes to the browser as a
  two-keyframe Web Animation, as frames recorded off the engine's own drivers, or to the C# frame loop - so Blazor
  Server plays nearly everything WebAssembly does.
- A browser suite that runs in **Chromium, Firefox and WebKit** on WebAssembly, Server and Auto (and an AOT, trimmed
  WebAssembly build), a unit suite on both target frameworks, JS unit tests of the bridge, and an MCP server whose API
  reference is reflected from the assembly and whose transition and playback analyses run the real engine.

Baseline for this round (Windows 11, .NET SDK 11.0.100, net10.0 + net11.0):

| Check | Result |
|---|---|
| Build (every project but the MAUI sample, whose `maui-android` workload isn't installed here) | succeeded, 0 warnings in the library |
| Unit tests | 1084 per target framework, 2168 in all, passing |
| MCP tests | not run before the first change; 430 at the end of the round, 4 of them new |

## How to read the tables

The plan's six classes: **1** fully implemented and tested · **2** implemented but incomplete · **3** implemented
but unreliable · **4** implemented with performance limitations · **5** documented but missing · **6** missing.
"Before" is the state at the start of this round, "Now" after it. *Decision* marks a gap closed by deciding against
the feature, with the reason.

No capability was found in class **3** or **5**: nothing the README documents is missing, and the defects the browser
suite found in rounds 1-4 were fixed with negative controls. Class **4** applies only where noted.

## 2. Core engine and runtimes

| Capability | Before | Now | Notes |
|---|---|---|---|
| One execution architecture choosing browser timeline / recording / frame loop / instant | 1 | 1 | AD-1. |
| Diagnostics explaining *why* an animation plays where it does | 2 | **1** | Before: the MCP server's offline analysis, the inspector's "N in browser" and a one-time Server warning. Now `BmotionAnimationEngine.AnimationEvent` reports every part's `BmPlaybackStrategy` and, for the frame loop or its absence, the reason; the inspector shows it. |
| Pause / resume / speed / stop / complete / time / seek | 1 | 1 | |
| Reverse playback (negative speed) | 6 | 6 | Open, P2 - see "Not done". |
| Concurrent animations on one property | 1 | 1 | The newer replaces the older and inherits its velocity (Motion and GSAP's default). |
| Conflict policies Queue / Merge / Ignore / Continue | 6 | *Decision* | Not added. Replace-with-velocity is what every leading library does by default, a queue is `await controls`, and a per-element policy would multiply the states every gesture, variant and layout path must handle. |
| Error propagation | 1 | 1 | Faulted segments surface through completion tasks; handler exceptions are logged. |
| WebAssembly, AOT, Server, Auto, prerendering, hydration | 1 | 1 | E2E on all three engines (round 4). |
| .NET MAUI Blazor Hybrid | 2 | 2 | A sample exists; no automated run (the workload isn't on CI machines that run the suite). |

## 3. Properties and interpolation

| Capability | Before | Now | Notes |
|---|---|---|---|
| Transforms (individual, composed, origin, perspective, template) | 1 | 1 | `BmotionTransformComposer`, `TransformTemplate`. |
| Matrix interpolation | 6 | *Decision* | Components are composed, never decomposed, so nothing needs a matrix mix. |
| Opacity, colours, sizes, box model, typography, filters, shadows, clip-path | 1 | 1 | Implicit starting values from the computed style; `auto` sizes measured. |
| Lengths across units | 1 | 1 | Through `calc()`, exact. |
| Masks, grid/flex properties, arbitrary properties | 2 | 2 | Through the `css` dictionary: interpolated when both ends share a shape, recorded otherwise. |
| Colours: modern syntaxes, OKLab | 1 | 1 | |
| OKLCH (hue) interpolation | 6 | 6 | P3. OKLab covers the perceptual-midpoint need; hue interpolation adds a hue-direction choice nobody has asked for. |
| Gradients with several stops | 2 | 2 | Same-shape gradients mix; different stop counts aren't padded. P3. |
| Diagnostics for unsupported values | 1 | 1 | CSS safe mode, the Server warning, now the event's reason. |

## 4. Physics and transitions

| Capability | Before | Now | Notes |
|---|---|---|---|
| Springs: physics and bounce/duration, velocity, rest thresholds, stability at extremes | 1 | 1 | |
| Named springs (Gentle, Smooth, Snappy, Bouncy, Stiff, Heavy, Responsive) | 6 | **1** | `BmSprings`, each checked by its measured overshoot and speed order. |
| Spring prediction: settling time, overshoot, peak velocity, oscillation | 6 | **1** | `BmSpring.Analyze()` steps the engine's own driver at 60 Hz; an MCP test checks it against the whole engine run off-screen. |
| Inertia: bounds, bounce, snap to grid and points, drag hand-off | 1 | 1 | |
| Easings: all the plan lists, cubic-bezier, steps | 1 | 1 | |
| Custom transitions without touching the core | 6 | 6 | P3. `BmTransition` is closed on purpose: every transition must be recordable for the browser path (AD-1), which an arbitrary user driver isn't. Per-frame callbacks and keyframes cover the cases seen so far. |

## 5. Motion values

| Capability | Before | Now | Notes |
|---|---|---|---|
| Values, velocity, transform, range map, spring follower, template, velocity value | 1 | 1 | |
| Values derived from several inputs | 2 | **1** | `Bm.Combine(() => ..., a, b)` - only strings (`Bm.Template`) could before. |
| Previous value | 6 | **1** | `BmValue<T>.Previous`. |
| Arithmetic helpers (Add, Multiply, Clamp, ...) | 6 | *Decision* | `Transform(v => ...)` and `Combine` express all of them in one line; a dozen operator methods would only widen the surface. |
| Transactional batching | 6 | *Decision* | Bound values flush once per frame already; a batch API would only save recomputing a derived value, not renders. |
| No re-render per change; allocation-free numeric tracking | 4 | **1** | Velocity tracking boxed every numeric set (`Convert.ToDouble(object)`) and looked the type up in a set; now a static flag and a JIT-eliminated cast for `double`. |

## 6-7. Keyframes, timelines

| Capability | Before | Now | Notes |
|---|---|---|---|
| Keyframes, `times`, per-segment easing, wildcard, repeat/mirror/reverse, repeat delay | 1 | 1 | |
| Relative values (`"+=100"`) | 6 | 6 | P3. |
| Sequences: `at` (relative, absolute, labels), one playhead, pause/speed/seek | 1 | 1 | |
| Placement after a spring | 3 | **1** | A segment after a physical spring started 0.8 s in whatever the spring: 0.6 s late after a stiff one, 3 s early after a loose one. Now the engine's own settling estimate. |
| Nested timelines, callbacks in a timeline, motion values and numbers as sequence targets | 6 | 6 | P2 - see "Not done". |

## 8-10. Components, variants, presence

| Capability | Before | Now | Notes |
|---|---|---|---|
| The `<Bmotion>` parameter set the plan lists (Initial ... OnLayoutAnimationComplete) | 1 | 1 | Every one exists. |
| Variants, propagation, inheritance, stagger, `When`, orchestration | 1 | 1 | |
| Presets | 1 | 1 | 24 now: `BmPresets.Expand` (accordion, to and from `height: auto`) added. The plan's Success/Error/Skeleton/Attention are `Pop`/`Shake`/`Shimmer`/`Pulse`. |
| Presence: sync/wait/popLayout, nesting, re-entry, custom participants, inert exits | 1 | 1 | |

## 11-13. Gestures, drag, layout

| Capability | Before | Now | Notes |
|---|---|---|---|
| Hover, tap (keyboard), focus-visible, pan, drag with every option the plan lists | 1 | 1 | |
| Long press, double tap, pinch, rotate, swipe, wheel | 6 | 6 | P3. Motion ships none of them; pan with its velocity covers swipe. Better as a Bheadless/Butil concern than an animation one. |
| Keyboard alternative for free-form drag | 6 | 6 | Reorder has one; a free drag's equivalent is app-specific (arrow keys moving by what?). Documented pattern pending. |
| FLIP, LayoutId, crossfade, scroll containers, Server holds | 1 | 1 | |
| Transformed ancestors, ResizeObserver-driven layout | 2 | 2 | Not covered by the suite. |

## 14-17. Scroll, SVG, canvas, advanced

| Capability | Before | Now | Notes |
|---|---|---|---|
| Native scroll/view timelines, fallback, tracker, smoothing | 1 | 1 | |
| Path drawing, motion path, arcs | 1 | 1 | |
| Path morphing between different commands | 2 | 2 | Same-command morphs only; normalisation is P3. |
| Canvas / custom targets | 2 | 2 | Motion values animate independently of the DOM on WebAssembly; on Server a per-frame callback can't run. |
| Text splitting | 1 | 1 | |
| Animated counters | 2 | 2 | Programmatic only. P2: a counter needs per-frame text, which Server can't do through the engine. |

## 18-19. Design tokens, accessibility

| Capability | Before | Now | Notes |
|---|---|---|---|
| Shared motion configuration (default transition, speed, colour space, reduced motion) | 1 | 1 | `BmotionConfig`. |
| Spring and preset vocabulary | 2 | **1** | `BmSprings` + `BmPresets`. |
| Theme-aware tokens tied to Bit.BlazorUI | 6 | *Decision* | Not in the core: the plan itself forbids coupling Bmotion to BlazorUI. An adapter belongs to a separate package. |
| `prefers-reduced-motion`, live changes, cascading policy | 1 | 1 | |
| Per-element reduced-motion behaviour (reduce / instant / essential) | 6 | **1** | `Bmotion.ReducedMotion` and `BmPreset.ReducedMotion`: `Reduce` (default), `Instant`, `Full`. |
| Keyboard tap, keyboard reorder, inert exits, focus-visible | 1 | 1 | |

## 20-22. Performance, conflicts, testing

| Capability | Before | Now | Notes |
|---|---|---|---|
| Browser-native playback, compositor properties, batched reads, one interop call in flight for scroll/pan | 1 | 1 | |
| Benchmark project with recorded measurements | 6 | 6 | P2. Round 2 measured by hand (a recording start 18 ms, a timeline start 5 ms, Debug WebAssembly). |
| Unit, JS, E2E in three engines | 1 | 1 | |
| Visual regression tests | 6 | 6 | P3. The E2E suite measures computed styles every frame, which is stricter than screenshots for motion. |

## 24-25. Integration, packaging

| Utility | Before | Now | Notes |
|---|---|---|---|
| BevTools / Belemetry | 6 | **2** | The `Bit.Bmotion` meter works with Belemetry and OpenTelemetry today, with no dependency; an `AnimationEvent` stream is there for a BevTools panel. No adapter package yet. |
| Bit.BlazorUI, Bheadless, Brouter, Bonboarding, Blow, Bink, Bmage | 6 | 6 | P2-P3. Each wants an optional adapter package, with the other library's maintainers. |
| Bdnd / Bortable | 6 | 6 | Bmotion's own reorder group covers the animated list; coordinating with Bdnd's state machine is an adapter, P3. |
| Bcrdt, Blex, BlazeDb, Bsqlite | 6 | *Decision* | Nothing to persist or synchronise: motion values follow state, and animation frames must never be replicated. |
| One package, trimmable, AOT-compatible, no dependencies beyond ASP.NET Core | 1 | 1 | `System.Diagnostics.Metrics` is in the BCL. |

## 26-27. Documentation, MCP

| Capability | Before | Now | Notes |
|---|---|---|---|
| README, package README, demo pages per feature | 1 | 1 | New: named springs with measured traces, per-element reduced motion, disclosure panels, diagnostics and telemetry. |
| Spring / easing / timeline playgrounds | 2 | 2 | The Springs page now draws each named spring's measured trace; an editable playground is P3. |
| MCP: reflected API, real-engine simulation and playback analysis, review rules | 1 | 1 | Spring simulations now report damping ratio, kind and period from the library's own analysis. Found and fixed: a search for `BmSpring` ranked the new `BmSprings` first. |

## Not done, and why

| P | Gap | Why it waits | Next step |
|---|---|---|---|
| P2 | Reverse playback | The frame-loop drivers integrate forwards only, and a browser animation played backwards needs its mirror, its commit at the end and the "finished" handling rewritten for a run that ends at its start. Landing it unverified in all three engines would trade a missing feature for a broken one. | Browser plans: negative `playbackRate` plus a mirror that runs backwards and a finish that commits the start. Frame loop: replay to `time - dt` each frame (seek already exists). Then E2E on all hosts. |
| P2 | Sequence callbacks, nested sequences, motion-value targets | Wants the scheduler to own non-element entries. | `BmSequence.Call(action, at)`, `Add(BmSequence, at)`, `Add(BmValue<double>, to, at)`. |
| P2 | Benchmarks | Needs a harness that measures WebAssembly in a browser, not only .NET. | Follow `Bit.Bdnd.Benchmarks` for the C# half; Playwright traces for the browser half. |
| P2 | Adapters for BlazorUI / Brouter / Bheadless | Need the other libraries' extension points agreed. | Brouter route transitions first: the presence components already do the work. |
| P3 | Gestures beyond pan/drag; morph normalisation; OKLCH; relative keyframes; visual regression | Low demand against their cost. | As above. |

## Changes to the plan itself

The plan was followed in its order, with these deliberate departures:

- **No rewrite.** The audit found a sound architecture; the round adds to it.
- **Conflict policies, arithmetic motion-value helpers, batching, matrix interpolation, theme-coupled tokens and
  persistence adapters were declined**, each with its reason above - the plan's own last principle ("no arbitrary
  complexity without practical use cases").
- **Telemetry through the BCL meter**, not a Belemetry dependency: any pipeline reads it, and the core stays free of
  other Bit utilities.
- **Reverse playback was deferred** rather than shipped without browser verification.
- Documents: this assessment and the completion report are new; the round log stays in `IMPROVEMENT_PLAN.md`.
