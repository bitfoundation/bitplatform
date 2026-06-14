# bit Bmotion

A Blazor-native animation library inspired by [Framer Motion](https://www.framer.com/motion/). Springs, gestures, layout animations, variants, and keyframes - **zero JavaScript dependencies**. All animation math runs in C# via WebAssembly.

> Targets **.NET 8, 9, and 10**

---

## Table of Contents

- [Installation](#installation)
- [Quick Start](#quick-start)
- [Components](#components)
  - [Motion](#motion)
  - [AnimatePresence](#animatepresence)
  - [MotionConfig](#motionconfig)
- [Animation Models](#animation-models)
  - [AnimationProps](#animationprops)
  - [TransitionConfig](#transitionconfig)
  - [MotionVariants](#motionvariants)
  - [DragOptions](#dragoptions)
  - [ViewportOptions](#viewportoptions)
- [Services](#services)
  - [AnimationController](#animationcontroller)
  - [MotionAnimateService](#motionanimateservice)
  - [MotionValue](#motionvalue)
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

The browser bridge (`BitBmotion.js`) ships as a static web asset of the package and is
imported automatically the first time an animation runs, so no manual `<script>` tag is
required.

---

## Quick Start

```razor
@using Bit.Bmotion.Components
@using Bit.Bmotion.Models

<Motion Animate='new AnimationProps { Opacity = 1, Y = 0 }'
        Initial='new AnimationProps { Opacity = 0, Y = 20 }'>
    Hello, Bmotion!
</Motion>
```

That's it - the element fades in and slides up on first render.

---

## Components

### Motion

`<Motion>` is the core component. It replaces any HTML element and adds animation superpowers.

```razor
<Motion Tag="section"
        Class="my-card"
        Initial='new AnimationProps { Opacity = 0, Scale = 0.9 }'
        Animate='new AnimationProps { Opacity = 1, Scale = 1 }'
        Exit='new AnimationProps { Opacity = 0, Scale = 0.9 }'
        WhileHover='new AnimationProps { Scale = 1.05 }'
        WhileTap='new AnimationProps { Scale = 0.97 }'
        Transition='new TransitionConfig { Type = TransitionType.Spring, Stiffness = 200, Damping = 20 }'>
    <p>Content</p>
</Motion>
```

#### Parameters

| Parameter | Type | Description |
|---|---|---|
| `Tag` | `string` | HTML element tag (default: `"div"`) |
| `Class` | `string?` | CSS class attribute |
| `Style` | `string?` | Inline style attribute |
| `ChildContent` | `RenderFragment?` | Child content |
| `Initial` | `AnimationTarget?` | Starting state (props, variant name, or `false`) |
| `Animate` | `AnimationTarget?` | Target state |
| `Exit` | `AnimationTarget?` | State to animate to before unmounting (requires `<AnimatePresence>`) |
| `WhileHover` | `AnimationTarget?` | Overlay applied while hovered |
| `WhileTap` | `AnimationTarget?` | Overlay applied while tapped/pressed |
| `WhileFocus` | `AnimationTarget?` | Overlay applied while focused |
| `WhileDrag` | `AnimationTarget?` | Overlay applied while dragging |
| `WhileInView` | `AnimationTarget?` | Overlay applied while in viewport |
| `Transition` | `TransitionConfig?` | Controls timing/physics of all transitions |
| `Variants` | `MotionVariants?` | Named animation states |
| `Drag` | `bool` | Enable drag gesture |
| `DragOptions` | `DragOptions?` | Drag axis, constraints, elasticity |
| `Layout` | `bool` | Enable automatic FLIP layout animations |
| `LayoutId` | `string?` | Shared-element transition ID |
| `Once` | `bool` | `WhileInView` fires once and never reverses |
| `Viewport` | `ViewportOptions?` | Advanced viewport tracking options |
| `AdditionalAttributes` | `Dictionary<string, object>?` | Extra HTML attributes (passed through) |

#### Event Callbacks

```text
OnHoverStart / OnHoverEnd
OnTapStart / OnTap / OnTapCancel
OnFocusStart / OnFocusEnd
OnPanStart / OnPan / OnPanEnd         (PanInfo)
OnDragStart / OnDrag / OnDragEnd
OnAnimationStart / OnAnimationComplete
OnViewportEnter / OnViewportLeave
```

---

### AnimatePresence

Wraps conditional content to enable exit animations. Children remain in the DOM while their exit animation plays, then are removed.

```razor
<AnimatePresence IsPresent="@_show">
    <Motion Initial='new AnimationProps { Opacity = 0 }'
            Animate='new AnimationProps { Opacity = 1 }'
            Exit='new AnimationProps { Opacity = 0 }'>
        I animate in and out!
    </Motion>
</AnimatePresence>

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

### MotionConfig

Provides global animation defaults to an entire subtree via cascading values.

```razor
<MotionConfig Transition='new TransitionConfig { Duration = 0.2 }'
              TransitionSpeed="1.5">
    <!-- all Motion elements inside inherit these defaults -->
</MotionConfig>
```

| Parameter | Type | Description |
|---|---|---|
| `Transition` | `TransitionConfig?` | Global default transition for all descendant `<Motion>` elements |
| `ReduceMotion` | `bool?` | Reduced-motion for this subtree: `null` = respect OS preference, `true` = always reduce, `false` = always animate |
| `TransitionSpeed` | `double` | Scale factor for all animation durations (default: `1.0`) |

---

## Animation Models

### AnimationProps

Describes the animatable state - the *what* of an animation.

```csharp
new AnimationProps
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

### TransitionConfig

Controls *how* a value moves between states.

```csharp
// Tween (duration-based, default)
new TransitionConfig
{
    Type = TransitionType.Tween,
    Duration = 0.4,
    Delay = 0.1,
    Ease = Easing.EaseInOut
}

// Spring (physics-based)
new TransitionConfig
{
    Type = TransitionType.Spring,
    Stiffness = 200,
    Damping = 15,
    Mass = 1,
    Bounce = 0.4,
    VisualDuration = 0.5
}

// Inertia (momentum deceleration)
new TransitionConfig
{
    Type = TransitionType.Inertia,
    InertiaVelocity = 500,
    TimeConstant = 700,
    Power = 0.8,
    InertiaMin = 0, InertiaMax = 1000
}
```

Shorthand: `TransitionConfig.Spring(stiffness: 150, damping: 12)`

Repeat: `new TransitionConfig { Repeat = int.MaxValue, RepeatType = RepeatType.Mirror }`

### MotionVariants

```csharp
var variants = MotionVariants.Create(
    ("hidden",  new AnimationProps { Opacity = 0, Y = 20 }),
    ("visible", new AnimationProps { Opacity = 1, Y = 0  })
);
```

```razor
<Motion Variants="variants"
        Initial='"hidden"'
        Animate='"visible"'
        Transition='new TransitionConfig { StaggerChildren = 0.1 }'>
    <Motion>Item 1</Motion>
    <Motion>Item 2</Motion>
    <Motion>Item 3</Motion>
</Motion>
```

### DragOptions

```csharp
new DragOptions
{
    Axis = DragAxis.X,
    Constraints = DragConstraints.Horizontal(-200, 200),
    Elastic = 0.2,
    Momentum = true,
    SnapToOrigin = false,
    DirectionLock = true
}
```

### ViewportOptions

```csharp
new ViewportOptions
{
    Once = true,
    Margin = "-100px",
    Amount = "some"   // "some", "all", or 0–1 threshold
}
```

---

## Services

### AnimationController

Programmatic control bound to a specific element by ID.

```razor
@inject AnimationController Controller

<Motion id="my-box" ... />

@code {
    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender) Controller.BindTo("my-box");
    }

    async Task Pulse() => await Controller.AnimateAsync(
        new AnimationProps { Scale = 1.2 },
        new TransitionConfig { Type = TransitionType.Spring, Bounce = 0.5 });
}
```

### MotionAnimateService

Animate elements by CSS selector or `ElementReference` without wrapping them in `<Motion>`.

```razor
@inject MotionAnimateService Motion

<div id="target">Animate me</div>

@code {
    async Task AnimateIt()
    {
        var controls = await Motion.AnimateAsync(
            "#target",
            new AnimationProps { X = 100, Opacity = 0.5 },
            new TransitionConfig { Duration = 0.6 });

        await controls.WhenCompleteAsync();
    }
}
```

### MotionValue

A reactive numeric value you can subscribe to and transform.

```csharp
var mv = MotionValueFactory.Create(0.0);
mv.Subscribe(v => Console.WriteLine($"value: {v}"));
await mv.SetAsync(100);

MotionValue<double> mapped = mv.Transform(
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
is **scoped to `<MotionConfig>`**: an element only consults the preference when it sits inside
one. Elements with no surrounding `<MotionConfig>` always animate.

```razor
<MotionConfig ReduceMotion="null">   @* respect the OS prefers-reduced-motion setting *@
    ...
</MotionConfig>
```

---

## License

[MIT](https://github.com/bitfoundation/bitplatform/blob/develop/LICENSE)
