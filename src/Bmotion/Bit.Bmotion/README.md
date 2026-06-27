# Bit.Bmotion

A Framer-Motion-style animation library for Blazor. All animation math (spring, tween,
inertia, keyframes, easing, color interpolation, gesture state, transform composition) runs
in C#; JavaScript is used only as a thin bridge to browser-native APIs.

## ⚠️ Platform support: Blazor WebAssembly only

Bit.Bmotion drives its animation loop over **synchronous** JS↔.NET interop. That is only
available on **Blazor WebAssembly**, so:

- ✅ **Blazor WebAssembly** — fully supported.
- ❌ **Blazor Server** — not supported (synchronous interop is unavailable). Starting the
  animation loop throws `PlatformNotSupportedException`.
- ⏸️ **Server-side prerendering** — components render their initial styles, but animations
  do not start until the WebAssembly runtime becomes interactive.

## Getting started

```csharp
// Program.cs (WebAssembly host)
builder.Services.AddBitBmotionServices();
```

```razor
<Bmotion Tag="div"
         Initial="@(new BmotionAnimationProps { Opacity = 0 })"
         Animate="@(new BmotionAnimationProps { Opacity = 1 })" />
```

See the XML documentation on `Bmotion`, `BmotionAnimateService`, and `BmotionTransitionConfig`
for the full API surface.

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

String-valued animation properties (`BackgroundColor`, `Width`, `BoxShadow`, `Color`, custom
`CssVars`, …) are written verbatim into the element's inline style. They are meant for
developer-authored values. Do **not** bind untrusted end-user input to them, or you risk CSS
injection into the element's `style`.

## Threading

The animation engine is intentionally lock-free and assumes a single (the WebAssembly UI) thread.
Do **not** enable WebAssembly multithreading (`<WasmEnableThreads>`) with this library.

## Disposing the programmatic helpers

`BmotionAnimationController` and `BmotionScrollTracker` are registered `Transient`. When obtained
via `@inject`, Blazor does **not** dispose them per component — the consuming component must
dispose them itself (from its own `Dispose`/`DisposeAsync`), otherwise their engine registration /
JS subscription leaks until the app shuts down.
