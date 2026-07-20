# Bit.Bmotion

A Blazor-native animation library inspired by [Motion](https://motion.dev) (Framer Motion):
springs, gestures, keyframes, variants, drag, layout (FLIP) animations, shared-element
transitions and exit animations - no manual JavaScript wiring required. All animation math
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

- ✅ **Blazor WebAssembly** - fully supported. Compositor-eligible animations (tweens and
  zero-velocity springs on transform/opacity) are pre-sampled in C# and run on the browser's
  Web Animations API off the main thread; everything else runs on the C# rAF engine over
  synchronous interop.
- ⚠️ **Blazor Server** - the compositor path works (it needs only async interop), so
  enter/exit/hover/tap/variant animations on transform + opacity play normally. Features that
  require the per-frame loop - inertia, color/dimension interpolation, keyframe arrays, drag,
  motion values - degrade to instant state changes.
- ⏸️ **Server-side prerendering** - components render their initial styles; animations start
  once the circuit/runtime becomes interactive.

Inject **`BmotionCapabilities`** to detect the current mode instead of guessing:
`Caps.SupportsFrameLoop` is `false` on Server (drag/inertia/interpolation degrade to instant),
`Caps.SupportsCompositor` is always `true`. On Server, the first animation that collapses to an
instant change also logs a one-time warning so the degradation is diagnosable.

## Accessibility: reduced motion

Choose how the library honours the OS `prefers-reduced-motion` setting **globally** at registration:

```csharp
builder.Services.AddBitBmotionServices(o => o.ReducedMotion = BmReducedMotionMode.User);
```

| `BmReducedMotionMode`      | Behaviour |
|----------------------------|-----------|
| `IgnoreUnlessConfigured`   | **Default (back-compat).** OS preference respected only inside a `<BmotionConfig>`. |
| `User`                     | **Recommended.** Respect the OS preference everywhere - the web-platform default. |
| `Always`                   | Always reduce, regardless of the OS. |
| `Never`                    | Never reduce, regardless of the OS. |

When motion is reduced, Bit.Bmotion follows Motion's `"user"` semantics: **transforms, layout and
dimension changes snap to their target instantly, while opacity and colour still animate** - a
softer, more correct reduction than collapsing every property to instant.

A local `<BmotionConfig ReduceMotion="true|false">` always overrides the global mode for its subtree
(`null` = follow the mode):

```razor
<BmotionConfig ReduceMotion="true">   @* force-reduce this subtree *@
    ...
</BmotionConfig>
```

> **Migration note.** The default remains `IgnoreUnlessConfigured` so existing apps are unaffected.
> New apps should set `ReducedMotion = BmReducedMotionMode.User` to match the platform default; this
> is planned to become the default in a future major version.

## Security note

String-valued animation properties (`backgroundColor`, `width`, `boxShadow`, `color`, custom
`cssVars`, …) are written verbatim into the element's inline style. They are meant for
developer-authored values. Do **not** bind untrusted end-user input to them, or you risk CSS
injection into the element's `style`.

If you must bind untrusted input, enable **CSS safe mode** to validate those values against a
conservative injection check (rejecting `;`, `}`, `<`, `javascript:`, `expression(`, comments, …):

```csharp
builder.Services.AddBitBmotionServices(o => o.CssSafeMode = BmCssSafeMode.Throw); // or .Warn
```

It is `Off` by default (for performance/parity); `Warn` logs and still applies, `Throw` rejects.

## Threading

The animation engine is intentionally lock-free and assumes a single (the WebAssembly UI) thread.
Do **not** enable WebAssembly multithreading (`<WasmEnableThreads>`) with this library.

## Disposing the programmatic helpers

`BmotionScrollTracker` is registered `Transient` and is owned by the consuming component. When
obtained via `@inject`, Blazor does **not** dispose it per component - the consuming component
must dispose it itself (call its `DisposeAsync` from the component's own `DisposeAsync`),
otherwise its JS subscription leaks until the app shuts down.
