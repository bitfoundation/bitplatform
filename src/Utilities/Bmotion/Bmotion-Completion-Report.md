# Bit.Bmotion completion report - round 5

What this round of the "world-class animation framework" plan changed, how it was validated, and what is left. The
gap analysis it worked from is [Bmotion-Assessment.md](Bmotion-Assessment.md); the earlier rounds are in
[IMPROVEMENT_PLAN.md](IMPROVEMENT_PLAN.md).

## What changed for users

**Why an animation plays where it does, at run time.** `BmotionAnimationEngine.AnimationEvent` reports every part of
an element animation as it starts and ends: its `BmPlaybackStrategy` (`Instant`, `BrowserTimeline`,
`BrowserRecorded`, `FrameLoop`, `InstantWithoutFrameLoop`), its properties, how it ended (`Finished` or
`Interrupted`) and how long it ran - and, for the frame loop or its absence, the reason: a per-frame callback, a
playback rate other than 1, an SVG `d`/`points` morph, a drag owning the transform, or a curve the browser can't
reproduce exactly. The same goes to the BCL meter `Bit.Bmotion` (`bmotion.animation.started`, `.ended`,
`.duration`), which Bit.Belemetry and OpenTelemetry read with no dependency either way. `<BmotionInspector>` shows
each element's latest parts and counts what landed without a frame loop. Property names only - never values or page
content - and nothing is tracked while nobody listens.

**Springs by name and by measurement.** `BmSprings.Smooth/Snappy/Bouncy/Gentle/Stiff/Heavy/Responsive()` say how
motion should feel. `spring.Analyze(from, to)` returns its settling time, overshoot, peak time and velocity, target
crossings, damping ratio and kind, oscillation period and a 60 Hz trace, by stepping the engine's own driver as the
frame loop does. The MCP server's spring simulations now include the damping ratio, kind and period.

**Reduced motion per element.** `<Bmotion ReducedMotion="...">` (or `BmPreset.ReducedMotion`) says how an element
answers reduced motion: `Reduce` (the default, unchanged), `Instant` (fades land too) or `Full` (calm, informative
motion keeps playing). Prerendered CSS follows it.

**Motion values.** `Bm.Combine(() => ..., a, b)` derives a numeric value from several; `BmValue<T>.Previous` is the
value before the latest change. Velocity tracking no longer boxes on every numeric set.

**Presets.** `BmPresets.Expand()` - an accordion panel to and from `height: auto`.

**Fixes.**

- A sequence segment placed after a physical spring started 0.8 s in, whatever the spring - 0.6 s late after a stiff
  spring, 3.2 s early after a loose one. It now uses the settling estimate the engine already schedules children with
  (the default spring's placement is unchanged: 0.8 s).
- The MCP search ranked `BmSprings` above an exact `BmSpring` (the plural rule made them equal); an exact name now wins.

## Validation

Run on Windows 11 with .NET SDK 11.0.100, Playwright's Chromium (build 1248).

| Check | Before | After |
|---|---|---|
| Build, every project except the MAUI sample (its `maui-android` workload isn't installed here) | succeeded | succeeded; the library and its tests with 0 warnings (the harness client's one BL0016 warning is older) |
| Unit tests, net10.0 + net11.0 | 2168 | **2266** (1133 per framework, 49 of them new) |
| MCP tests | - | **430**, 4 new |
| E2E, Chromium, `wasm` host | - | **167/167** |
| E2E, Chromium, `server` host | - | **167/167** |

The lazy-loading E2E fixture needs a publish of the lazy sample and wasn't run; nothing in the round touches lazy
loading. The bridge (`bit-bmotion.ts`) is unchanged, so its JS tests weren't re-run, nor were Firefox and WebKit.

What the new tests pin:

- `AnimationEventTests` (12): the strategy of each shape - timeline, recording, frame loop with its reason, an
  attribute morph, a Server degradation, an instant set; a part-browser part-loop animation reported in two parts;
  interruption by a newer animation and by a stop; a throwing handler isolated; the meter's tags; nothing changes
  with nobody listening.
- `SpringAnalysisTests` (16): an underdamped spring against theory (ζ, ω₀, period exactly; overshoot and peak time
  within a 60 Hz frame); settling time against the driver run frame by frame; distance-independence; velocity;
  equal ends; an undamped spring reported as unsettled; extreme damping finite; the named springs' overshoot and
  speed order.
- `TheLibrarysSpringAnalysis_AgreesWithTheEngineRunOffScreen` (MCP, 3 springs): `Analyze` and the whole engine agree
  on settling time within two frames and on overshoot within a percentage point.
- `ReducedMotionBehaviorTests` (6) and `PresetCompositionTests` (2): each behaviour under reduced motion, no effect
  when motion isn't reduced, preset precedence, composition, and a reflection guard that `BmPreset +` keeps every
  property.
- `MotionValueCombineTests` (8), `SequenceSpringPlacementTests` (3), and an inspector test on Server.

## Breaking changes and migration

No public member was removed or renamed; `BmotionAnimationEngine` became `partial`, which callers can't see.

| Change | Migration |
|---|---|
| A sequence segment after a physical spring (no `duration`/`bounce`) starts when the spring's physics settles, not at 0.8 s; after a `bounce`-only spring, at its 0.5 s visual duration, not 0.8 s | Give the segment an explicit `at` to keep the old timing |

## What is left

Reverse playback, sequence callbacks and nesting, benchmarks and the integration adapters lead the open list; each is
in [Bmotion-Assessment.md](Bmotion-Assessment.md) with why it waits and what the next step is.
