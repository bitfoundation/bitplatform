# Bit.Bmotion

A Blazor-native animation library inspired by [Motion](https://motion.dev) (Framer Motion):
springs, gestures, keyframes, variants, drag, layout (FLIP) animations, shared-element
transitions and exit animations — no manual JavaScript wiring required. All animation math
(spring, tween, inertia, keyframes, easing, color interpolation, gesture state, transform
composition) runs in C#; JavaScript is used only as a thin bridge to browser-native APIs.

## Quick start

```csharp
// Program.cs
builder.Services.AddBitBmotionServices();
```

```razor
@using Bit.Bmotion

<Bmotion Initial="Bm.To(opacity: 0, y: 20)"
         Animate="Bm.To(opacity: 1, y: 0)"
         Transition="Bm.Spring(bounce: 0.4, duration: 0.6)">
    <div>Hello, Bmotion!</div>
</Bmotion>
```

The `Bm` facade is the entry point for the whole hot path: `Bm.To(...)` builds animation
targets (single values or keyframe sequences), `Bm.Spring` / `Bm.Tween` / `Bm.Inertia` build
transitions, `Bm.Stagger` builds multi-element delay generators, `Bm.Value` creates reactive
motion values, and `Bm.Template` composes them into CSS strings. Components:
`<Bmotion>`, `<BmotionAnimatePresence>`, `<BmotionPresenceSwitch>`, `<BmotionPresenceGroup>`
(keyed list presence, with a popLayout mode), `<BmotionReorderGroup>` (drag-to-reorder lists),
`<BmotionLayoutGroup>` and `<BmotionConfig>`. Programmatic
animation is available through `BmotionAnimateService` (selector / `ElementReference` based,
supports sequences and staggers) and `BmotionScrollTracker` (scroll-linked motion values).

See the repository README for the full guide, and the XML documentation on `Bmotion`, `Bm`,
and `BmotionAnimateService` for the complete API surface.

## Platform support

- ✅ **Blazor WebAssembly** — fully supported. Compositor-eligible animations (tweens and
  zero-velocity springs on transform/opacity) are pre-sampled in C# and run on the browser's
  Web Animations API off the main thread; everything else runs on the C# rAF engine over
  synchronous interop.
- ⚠️ **Blazor Server** — the compositor path works (it needs only async interop), so
  enter/exit/hover/tap/variant animations on transform + opacity play normally. Features that
  require the per-frame loop — inertia, color/dimension interpolation, keyframe arrays, drag,
  motion values — degrade to instant state changes.
- ⏸️ **Server-side prerendering** — components render their initial styles; animations start
  once the circuit/runtime becomes interactive.

## Accessibility: reduced motion is opt-in

`prefers-reduced-motion` is **only** honoured for elements inside a `<BmotionConfig>`. Elements
with no surrounding config always animate, even when the OS requests reduced motion. This is a
deliberate choice (so the OS setting never silently disables animations an app didn't opt into),
but it is the opposite of the web-platform default — if you care about reduced-motion
accessibility, wrap your app (or the relevant subtree) in a config:

```razor
<BmotionConfig>           @* ReduceMotion defaults to null = follow the OS setting *@
    ...
</BmotionConfig>
```

Set `ReduceMotion="true"`/`"false"` to force it on or off regardless of the OS preference.

## Security note

String-valued animation properties (`backgroundColor`, `width`, `boxShadow`, `color`, custom
`cssVars`, …) are written verbatim into the element's inline style. They are meant for
developer-authored values. Do **not** bind untrusted end-user input to them, or you risk CSS
injection into the element's `style`.

## Threading

The animation engine is intentionally lock-free and assumes a single (the WebAssembly UI) thread.
Do **not** enable WebAssembly multithreading (`<WasmEnableThreads>`) with this library.

## Disposing the programmatic helpers

`BmotionScrollTracker` is registered `Transient` and is owned by the consuming component. When
obtained via `@inject`, Blazor does **not** dispose it per component — the consuming component
must dispose it itself (call its `DisposeAsync` from the component's own `DisposeAsync`),
otherwise its JS subscription leaks until the app shuts down.
