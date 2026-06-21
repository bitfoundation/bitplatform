# Bit.Bmotion

A Blazor-native animation library inspired by [Framer Motion](https://www.framer.com/motion/). Springs, gestures, layout animations, variants, and keyframes - **no manual JavaScript wiring required**. All animation math runs in C# via WebAssembly; the slim browser bridge is auto-loaded for you.

> Targets **.NET 8, 9, and 10**

---

## Table of Contents

- [Installation](#installation)
- [Quick Start](#quick-start)
- [Components](#components)
  - [Bmotion](#bmotion)
  - [BmotionAnimatePresence](#bmotionanimatepresence)
  - [BmotionConfig](#bmotionconfig)
- [Animation Models](#animation-models)
  - [BmotionAnimationProps](#bmotionanimationprops)
  - [BmotionTransitionConfig](#bmotiontransitionconfig)
  - [BmotionMotionVariants](#bmotionmotionvariants)
  - [BmotionDragOptions](#bmotiondragoptions)
  - [BmotionViewportOptions](#bmotionviewportoptions)
- [Services](#services)
  - [BmotionAnimationController](#bmotionanimationcontroller)
  - [BmotionAnimateService](#bmotionanimateservice)
  - [BmotionValue](#bmotionvalue)
- [Examples](#examples)
- [Accessibility](#accessibility)

---

## Installation

```bash
dotnet add package Bit.Bmotion
```

Register the services in `Program.cs`:

```csharp
using Bit.Bmotion;

builder.Services.AddBitBmotionServices();
```

The browser bridge (`bit-bmotion.js`) ships as a static web asset of the package and is
imported automatically the first time an animation runs, so no manual `<script>` tag is
required.

---

## Quick Start

```razor
@using Bit.Bmotion

<Bmotion Animate='new BmotionAnimationProps { Opacity = 1, Y = 0 }'
         Initial='new BmotionAnimationProps { Opacity = 0, Y = 20 }'>
    Hello, Bmotion!
</Bmotion>
```

That's it - the element fades in and slides up on first render.

---

## Components

### Bmotion

`<Bmotion>` is the core component. It replaces any HTML element and adds animation superpowers.

```razor
<Bmotion Tag="section"
         Class="my-card"
         Initial='new BmotionAnimationProps { Opacity = 0, Scale = 0.9 }'
         Animate='new BmotionAnimationProps { Opacity = 1, Scale = 1 }'
         Exit='new BmotionAnimationProps { Opacity = 0, Scale = 0.9 }'
         WhileHover='new BmotionAnimationProps { Scale = 1.05 }'
         WhileTap='new BmotionAnimationProps { Scale = 0.97 }'
         Transition='new BmotionTransitionConfig { Type = BmotionTransitionType.Spring, Stiffness = 200, Damping = 20 }'>
    <p>Content</p>
</Bmotion>
```

#### Parameters

| Parameter | Type | Description |
|---|---|---|
| `Tag` | `string` | HTML element tag (default: `"div"`) |
| `Class` | `string?` | CSS class attribute |
| `Style` | `string?` | Inline style attribute |
| `ChildContent` | `RenderFragment?` | Child content |
| `Initial` | `BmotionAnimationTarget?` | Starting state (props, variant name, or `false`) |
| `Animate` | `BmotionAnimationTarget?` | Target state |
| `Exit` | `BmotionAnimationTarget?` | State to animate to before unmounting (requires `<BmotionAnimatePresence>`) |
| `WhileHover` | `BmotionAnimationTarget?` | Overlay applied while hovered |
| `WhileTap` | `BmotionAnimationTarget?` | Overlay applied while tapped/pressed |
| `WhileFocus` | `BmotionAnimationTarget?` | Overlay applied while focused |
| `WhileDrag` | `BmotionAnimationTarget?` | Overlay applied while dragging |
| `WhileInView` | `BmotionAnimationTarget?` | Overlay applied while in viewport |
| `Transition` | `BmotionTransitionConfig?` | Controls timing/physics of all transitions |
| `Variants` | `BmotionMotionVariants?` | Named animation states |
| `Drag` | `bool` | Enable drag gesture |
| `DragOptions` | `BmotionDragOptions?` | Drag axis, constraints, elasticity |
| `Layout` | `bool` | Enable automatic FLIP layout animations |
| `Once` | `bool` | `WhileInView` fires once and never reverses |
| `Viewport` | `BmotionViewportOptions?` | Advanced viewport tracking options |
| `AdditionalAttributes` | `Dictionary<string, object>?` | Extra HTML attributes (passed through) |

#### Event Callbacks

```text
OnHoverStart / OnHoverEnd
OnTapStart / OnTap / OnTapCancel
OnFocusStart / OnFocusEnd
OnPanStart / OnPan / OnPanEnd         (BmotionPanInfo)
OnDragStart / OnDrag / OnDragEnd
OnAnimationStart / OnAnimationComplete
OnViewportEnter / OnViewportLeave
```

---

### BmotionAnimatePresence

Wraps conditional content to enable exit animations. Children remain in the DOM while their exit animation plays, then are removed.

```razor
<BmotionAnimatePresence IsPresent="@_show">
    <Bmotion Initial='new BmotionAnimationProps { Opacity = 0 }'
             Animate='new BmotionAnimationProps { Opacity = 1 }'
             Exit='new BmotionAnimationProps { Opacity = 0 }'>
        I animate in and out!
    </Bmotion>
</BmotionAnimatePresence>

<button @onclick="() => _show = !_show">Toggle</button>

@code {
    bool _show = true;
}
```

| Parameter | Type | Description |
|---|---|---|
| `IsPresent` | `bool` | Controls whether the child content is present (default: `true`) |
| `ExitBeforeEnter` | `bool` | Wait for exit animation to finish before entering new content |
| `ChildContent` | `RenderFragment?` | Content to animate |

---

### BmotionConfig

Provides global animation defaults to an entire subtree via cascading values.

```razor
<BmotionConfig Transition='new BmotionTransitionConfig { Duration = 0.2 }'
               TransitionSpeed="1.5">
    <!-- all Bmotion elements inside inherit these defaults -->
</BmotionConfig>
```

| Parameter | Type | Description |
|---|---|---|
| `Transition` | `BmotionTransitionConfig?` | Global default transition for all descendant `<Bmotion>` elements |
| `ReduceMotion` | `bool?` | Reduced-motion for this subtree: `null` = respect OS preference, `true` = always reduce, `false` = always animate |
| `TransitionSpeed` | `double` | Scale factor for all animation durations (default: `1.0`) |

---

## Animation Models

### BmotionAnimationProps

Describes the animatable state - the *what* of an animation.

```csharp
new BmotionAnimationProps
{
    // Transform
    X = 100, Y = -20, Z = 0,
    Scale = 1.2, ScaleX = 1, ScaleY = 1,
    Rotate = 45, RotateX = 0, RotateY = 0, RotateZ = 0,
    SkewX = 10, SkewY = 0,
    Perspective = 800,

    // Visual
    Opacity = 1,
    BackgroundColor = "#ff0000",
    Color = "rgba(0,0,0,0.8)",
    BorderColor = "#ccc",
    Width = "200px", Height = "200px",
    BorderRadius = "8px",
    BoxShadow = "0 4px 20px rgba(0,0,0,0.2)",

    // SVG
    Fill = "#0000ff",
    Stroke = "#ff0000",
    PathLength = 1,        // 0–1, drives stroke-dashoffset drawing

    // CSS custom properties
    CssVars = new() { ["--accent"] = "#ff6b6b" },

    // Keyframe arrays (multi-step)
    Keyframes = new() { ["scale"] = new double[] { 1, 1.4, 0.8, 1 } }
}
```

### BmotionTransitionConfig

Controls *how* a value moves between states.

```csharp
// Tween (duration-based, default)
new BmotionTransitionConfig
{
    Type = BmotionTransitionType.Tween,
    Duration = 0.4,
    Delay = 0.1,
    Ease = BmotionEasing.EaseInOut
}

// Spring (physics-based)
new BmotionTransitionConfig
{
    Type = BmotionTransitionType.Spring,
    Stiffness = 200,
    Damping = 15,
    Mass = 1,
    Bounce = 0.4,
    VisualDuration = 0.5
}

// Inertia (momentum deceleration)
new BmotionTransitionConfig
{
    Type = BmotionTransitionType.Inertia,
    InertiaVelocity = 500,
    TimeConstant = 700,
    Power = 0.8,
    InertiaMin = 0, InertiaMax = 1000
}
```

Shorthand: `BmotionTransitionConfig.Spring(stiffness: 150, damping: 12)`

Repeat: `new BmotionTransitionConfig { Repeat = int.MaxValue, RepeatType = BmotionRepeatType.Mirror }`

### BmotionMotionVariants

```csharp
var variants = BmotionMotionVariants.Create(
    ("hidden",  new BmotionAnimationProps { Opacity = 0, Y = 20 }),
    ("visible", new BmotionAnimationProps { Opacity = 1, Y = 0  })
);
```

```razor
<Bmotion Variants="variants"
         Initial="hidden"
         Animate="visible"
         Transition='new BmotionTransitionConfig { StaggerChildren = 0.1 }'>
    <Bmotion>Item 1</Bmotion>
    <Bmotion>Item 2</Bmotion>
    <Bmotion>Item 3</Bmotion>
</Bmotion>
```

### BmotionDragOptions

```csharp
new BmotionDragOptions
{
    Axis = BmotionDragAxis.X,
    Constraints = BmotionDragConstraints.Horizontal(-200, 200),
    Elastic = 0.2,
    Momentum = true,
    SnapToOrigin = false,
    DirectionLock = true
}
```

### BmotionViewportOptions

```csharp
new BmotionViewportOptions
{
    Once = true,
    Margin = "-100px",
    Amount = "some"   // "some", "all", or 0–1 threshold
}
```

---

## Services

### BmotionAnimationController

Programmatic control bound to a specific element by ID.

```razor
@inject BmotionAnimationController Controller
@implements IDisposable

<Bmotion id="my-box" ... />

@code {
    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender) Controller.BindTo("my-box");
    }

    async Task Pulse() => await Controller.AnimateAsync(
        new BmotionAnimationProps { Scale = 1.2 },
        new BmotionTransitionConfig { Type = BmotionTransitionType.Spring, Bounce = 0.5 });

    // Dispose the controller so the bound element is unregistered from the engine on teardown.
    public void Dispose() => Controller.Dispose();
}
```

### BmotionAnimateService

Animate elements by CSS selector or `ElementReference` without wrapping them in `<Bmotion>`.

```razor
@inject BmotionAnimateService Motion

<div id="target">Animate me</div>

@code {
    async Task AnimateIt()
    {
        var controls = await Motion.AnimateAsync(
            "#target",
            new BmotionAnimationProps { X = 100, Opacity = 0.5 },
            new BmotionTransitionConfig { Duration = 0.6 });

        await controls.WhenCompleteAsync();
    }
}
```

### BmotionValue

A reactive numeric value you can subscribe to and transform.

```csharp
var mv = BmotionValueFactory.Create(0.0);
mv.Subscribe(v => Console.WriteLine($"value: {v}"));
await mv.SetAsync(100);

BmotionValue<double> mapped = mv.Transform(
    inputRange:  new[] { 0.0, 1.0 },
    outputRange: new[] { 0.0, 360.0 });
```

---

## Examples

See the `Demos` samples app for runnable examples of basic animations, gestures,
springs, drag, variants & stagger, keyframes, enter/exit transitions, layout (FLIP)
animations, scroll/viewport effects and programmatic control.

---

## Accessibility

Bmotion can honour the user's **prefers-reduced-motion** preference, collapsing animations to
instant state changes. To keep it from ever disabling animations an app didn't opt into, this
is **scoped to `<BmotionConfig>`**: an element only consults the preference when it sits inside
one. Elements with no surrounding `<BmotionConfig>` always animate.

```razor
<BmotionConfig ReduceMotion="null">   @* respect the OS prefers-reduced-motion setting *@
    ...
</BmotionConfig>
```

---

## License

[MIT](https://github.com/bitfoundation/bitplatform/blob/develop/LICENSE)
