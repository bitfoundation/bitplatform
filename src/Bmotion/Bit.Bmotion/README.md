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
