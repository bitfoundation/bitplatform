# Bit.Bmotion

A Blazor-native animation library inspired by [Motion](https://motion.dev) (Framer Motion). Springs, gestures, keyframes, variants, drag, layout (FLIP) animations, shared-element transitions and exit animations - **no manual JavaScript wiring required**. All animation math runs in C#; the slim browser bridge is auto-loaded for you.

**Hybrid engine:** compositor-eligible animations (tweens and zero-velocity springs on transform/opacity - i.e. most enter/exit/hover/variant animations) are pre-sampled in C# and handed to the browser's **Web Animations API**, so they play off the main thread with zero per-frame interop. Everything else runs on the C# rAF engine.

> Targets **.NET 8, 9, and 10** · Full support on **Blazor WebAssembly**. On **Blazor Server**, compositor-eligible animations play normally (they need only async interop); features that require the per-frame loop - inertia, color/dimension interpolation, keyframe arrays, drag, motion values - degrade to instant state changes.

---

## Table of Contents

- [Installation](#installation)
- [Quick Start](#quick-start)
- [The `Bm` facade](#the-bm-facade)
- [Components](#components)
  - [Bmotion](#bmotion)
  - [BmotionAnimatePresence](#bmotionanimatepresence)
  - [BmotionPresenceSwitch](#bmotionpresenceswitch)
  - [BmotionPresenceGroup](#bmotionpresencegroup)
  - [BmotionReorderGroup](#bmotionreordergroup)
  - [BmotionConfig](#bmotionconfig)
- [Transitions](#transitions)
- [Keyframes](#keyframes)
- [Variants](#variants)
- [Drag](#drag)
- [Layout & shared elements](#layout--shared-elements)
- [Programmatic API](#programmatic-api)
- [Motion values](#motion-values)
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

<Bmotion Initial="Bm.To(opacity: 0, y: 20)"
         Animate="Bm.To(opacity: 1, y: 0)">
    <div>Hello, Bmotion!</div>
</Bmotion>
```

That's it - the element fades in and slides up on first render.

---

## The `Bm` facade

`Bm` is the terse entry point for the whole hot path - it reads like motion.dev inside Razor:

```csharp
Bm.To(opacity: 1, x: 100, scale: 1.2)            // an animation target
Bm.To(scale: [1, 1.4, 0.8, 1])                   // keyframes are just another value shape
Bm.Spring(stiffness: 200, damping: 20)           // physics spring
Bm.Spring(bounce: 0.4, duration: 0.6)            // intuitive duration-based spring
Bm.Tween(0.4, BmEase.InOut, repeat: BmRepeat.Mirror())
Bm.Inertia(velocity: 500)
Bm.Stagger(0.08, from: BmStaggerFrom.Center)     // delay generator for multi-element animations
Bm.Current                                        // wildcard keyframe: "the element's current value"
```

`Bm.To(...)` returns a `BmProps`; every parameter is optional. Available
properties: `x, y, z, scale, scaleX, scaleY, rotate, rotateX, rotateY, rotateZ, skewX, skewY,
perspective, originX, originY, opacity, backgroundColor, color, borderColor, outlineColor,
fill, stroke, width, height, borderRadius, boxShadow, filter, pathLength, pathOffset, pathSpacing,
cssVars, transition`.

> **Security:** string-valued properties are written verbatim into the element's inline style.
> They are intended for developer-authored values; binding untrusted end-user input risks CSS
> injection.

---

## Components

### Bmotion

`<Bmotion>` is the core component. It wraps the element you author and adds animation
superpowers: you write the animated element as plain markup inside `<Bmotion>`, and at render
time Bmotion injects the engine id, the initial inline style, and `pathLength` into the
**first root HTML element** of the child content - the Blazor equivalent of React's
`cloneElement`. No context, no attribute splatting.

```razor
<Bmotion Initial="Bm.To(opacity: 0, scale: 0.9)"
         Animate="Bm.To(opacity: 1, scale: 1)"
         Exit="Bm.To(opacity: 0, scale: 0.9)"
         WhileHover="Bm.To(scale: 1.05)"
         WhileTap="Bm.To(scale: 0.97)"
         Transition="Bm.Spring(stiffness: 200, damping: 20)">
    <section class="my-card">
        <p>Content</p>
    </section>
</Bmotion>
```

Additional root nodes render unchanged; the root that receives the animation must be a plain
HTML element, not a component.

Plain expressions need no `@()` in non-string attributes. When the expression embeds string
literals, single-quote the attribute: `WhileHover='Bm.To(backgroundColor: "#8a66ff")'`.

#### Parameters

| Parameter | Type | Description |
|---|---|---|
| `ChildContent` | `RenderFragment` | The animated element as plain markup; Bmotion automatically injects the engine id, initial inline style, and `pathLength` into the first root HTML element |
| `Id` | `string?` | Stable element id used as the element's identity; takes precedence over an `id` authored on the element |
| `Initial` | `BmTarget?` | Starting state (props or `false` to disable the enter animation) |
| `Animate` | `BmTarget?` | Target state; animates on mount and on every change |
| `Exit` | `BmTarget?` | State to animate to before unmounting (requires a presence component) |
| `WhileHover` / `WhileTap` / `WhileFocus` / `WhileDrag` / `WhileInView` | `BmTarget?` | Gesture overlays; automatically revert when the gesture ends |
| `Transition` | `BmTransition?` | Timing/physics for all of this element's transitions |
| `Variants` | `BmVariants?` | Named animation states |
| `State` / `InitialState` | `string?` | Active / initial variant name (razor-literal friendly) |
| `Custom` | `object?` | Data passed to dynamic variants |
| `Values` | `Dictionary<string, BmValue<double>>?` | Motion-value bindings (`style={{ x }}` equivalent) |
| `StringValues` | `Dictionary<string, BmValue<string>>?` | String motion-value bindings for any CSS property (`useMotionTemplate` equivalent, see [Motion values](#motion-values)) |
| `Drag`, `DragConstraints`, `DragElastic`, `DragMomentum`, `DragSnapToOrigin`, `DragDirectionLock`, `DragTransition`, `DragHandle`, `DragControls`, `DragListener` | | See [Drag](#drag) |
| `Layout` | `BmLayout` | Automatic FLIP layout animations (`true` or `BmLayout.Position`) |
| `LayoutId` | `string?` | Shared-element transitions (see [Layout & shared elements](#layout--shared-elements)) |
| `Once` / `Viewport` | `bool` / `BmViewport?` | Viewport tracking for `WhileInView` |
| `OnUpdate` | `Action<IReadOnlyDictionary<string, string>>?` | Per-frame callback with the CSS flushed this frame (no re-render) |

Plain HTML attributes (`class`, `role`, `data-*`, …) go directly on the element you author
inside the child content. Your own inline `style` just works: the engine's initial style is
merged **before** your declarations, so anything you write wins conflicts. If you author an
`id`, it is honored and adopted as the engine id (the `Id` parameter takes precedence over
both).

```razor
<Bmotion ...>
    <div style="border:1px solid #ccc;" />
</Bmotion>
```

#### Event callbacks

```text
OnHoverStart / OnHoverEnd
OnTapStart / OnTap / OnTapCancel
OnFocusStart / OnFocusEnd
OnPanStart / OnPan / OnPanEnd                  (BmPanInfo)
OnDragStart / OnDrag / OnDragEnd
OnAnimationStart / OnAnimationComplete         (BmProps? - the resolved target)
OnViewportEnter / OnViewportLeave
```

#### Instance methods (via `@ref`)

```razor
<Bmotion @ref="_box" ...>
    <div />
</Bmotion>

@code {
    private Bmotion _box = default!;

    Task Pulse() => _box.AnimateAsync(Bm.To(scale: 1.2), Bm.Spring(bounce: 0.5)).AsTask();
    void Freeze() => _box.Pause();          // also: Resume(), SetPlaybackRate(2), Stop(), Set(...), SetAsync(...)
}
```

---

### BmotionAnimatePresence

Wraps conditional content to enable exit animations. Children remain in the DOM while their
exit animation plays, then are removed.

```razor
<BmotionAnimatePresence IsPresent="@_show" Mode="BmPresenceMode.Wait">
    <Bmotion Initial="Bm.To(opacity: 0)"
             Animate="Bm.To(opacity: 1)"
             Exit="Bm.To(opacity: 0)">
        <div>I animate in and out!</div>
    </Bmotion>
</BmotionAnimatePresence>
```

| Parameter | Type | Description |
|---|---|---|
| `IsPresent` | `bool` | Controls whether the child content is present |
| `Mode` | `BmPresenceMode` | `Sync` (default), `Wait` (exit finishes before re-enter) or `PopLayout` (exiting content pops out of the layout flow so siblings reflow immediately) |
| `OnExitComplete` | `EventCallback` | Fires when all exit animations finish |

### BmotionPresenceSwitch

Animates **between** items - the paging / toast idiom covered by motion.dev's keyed
`AnimatePresence`. When `Item` changes, the outgoing subtree plays its `Exit` before the new
item enters (it keeps rendering the *old* item while exiting, because the content is a
template of the item):

```razor
<BmotionPresenceSwitch Item="_page" Context="pageNumber">
    <Bmotion Initial="Bm.To(opacity: 0, x: 40)"
             Animate="Bm.To(opacity: 1, x: 0)"
             Exit="Bm.To(opacity: 0, x: -40)">
        <div>Page @pageNumber</div>
    </Bmotion>
</BmotionPresenceSwitch>
```

`Mode` defaults to `Wait`; `Sync` overlaps exit and enter. `OnExitComplete` fires per exit.

### BmotionPresenceGroup

Keyed **list** presence - motion.dev's `AnimatePresence` around a collection. Render one
template per item; removed items play their `Exit` before leaving the DOM, added items play
their enter, and an item re-added mid-exit cancels the exit. Just mutate the list:

```razor
<BmotionPresenceGroup Items="_messages" ItemKey="m => m.Id" Context="message">
    <Bmotion Initial="Bm.To(opacity: 0, x: 40)"
             Animate="Bm.To(opacity: 1, x: 0)"
             Exit="Bm.To(opacity: 0, scale: 0.9)">
        <div class="toast">@message.Text</div>
    </Bmotion>
</BmotionPresenceGroup>

@code {
    private List<Message> _messages = [];   // Add/Remove and the animations follow
}
```

| Parameter | Type | Description |
|---|---|---|
| `Items` | `IEnumerable<TItem>` | The current items, in render order |
| `ItemKey` | `Func<TItem, object>?` | Stable identity across renders (defaults to the item itself); keys must be unique |
| `Mode` | `BmPresenceMode` | `Sync` (default) or `PopLayout` - exiting items pop to `position: absolute` at their spot so siblings reflow immediately (give the container `position: relative`) |
| `OnExitComplete` | `EventCallback` | Fires each time a removed item finishes exiting |

### BmotionReorderGroup

Drag-to-reorder lists - motion.dev's `Reorder.Group`/`Reorder.Item` in one component. Every
item is draggable along the list axis, siblings spring out of the way during the drag
(transform-based preview, no re-renders), and the new order is committed to the bound list on
release:

```razor
<BmotionReorderGroup @bind-Items="_tracks" ItemKey="t => t.Id" Context="track">
    <div class="row">@track.Title</div>
</BmotionReorderGroup>
```

| Parameter | Type | Description |
|---|---|---|
| `Items` / `ItemsChanged` | `List<TItem>` | The list being reordered (`@bind-Items` supported) |
| `ItemKey` | `Func<TItem, object>?` | Stable identity across renders; keys must be unique |
| `Axis` | `BmDragAxis` | `Y` (vertical, default) or `X` (horizontal); wrapping grids are not supported |
| `WhileDrag` | `BmTarget?` | Overlay while dragging (default: slight scale-up) |
| `HandleSelector` | `string?` | CSS selector of a drag grip inside each item; the rest of the row stays clickable |
| `Transition` | `BmTransition?` | Spring for sibling displacement and the release settle |
| `OnReorder` | `EventCallback` | Fires after a reorder is committed |

### BmotionConfig

Provides global animation defaults to an entire subtree via cascading values.

```razor
<BmotionConfig Transition="Bm.Tween(0.2)" TransitionSpeed="2">
    <!-- all Bmotion elements inside inherit these defaults; run twice as fast -->
</BmotionConfig>
```

| Parameter | Type | Description |
|---|---|---|
| `Transition` | `BmTransition?` | Default transition for all descendants |
| `ReduceMotion` | `bool?` | `null` = respect OS preference, `true` = always reduce, `false` = always animate |
| `TransitionSpeed` | `double` | Playback rate: `2` = twice as fast, `0.5` = half speed, `0` = instant |

---

## Transitions

Three concrete types under the abstract `BmTransition`, each carrying only its own knobs:

```csharp
// Tween (duration + easing)
Bm.Tween(0.4, BmEase.InOut, delay: 0.1)

// Spring - physics parameters…
Bm.Spring(stiffness: 200, damping: 15, mass: 1)
// …or the intuitive duration-based form (visual seconds + bounciness 0-1)
Bm.Spring(bounce: 0.4, duration: 0.5)

// Inertia (momentum deceleration)
Bm.Inertia(velocity: 500, timeConstant: 700, min: 0, max: 1000)
```

Repeat via `BmRepeat` (no more `int.MaxValue` sentinel):

```csharp
Bm.Tween(1.2, repeat: BmRepeat.Forever)          // loop forever
Bm.Tween(1.2, repeat: BmRepeat.Mirror())         // ping-pong forever
Bm.Spring(repeat: BmRepeat.Loop(3, delay: 0.3))  // 3×, 300 ms apart
Bm.Tween(0.5, repeat: 2)                         // implicit int conversion
```

Per-property overrides and orchestration live on the base type:

```csharp
new BmTween
{
    Duration = 0.4,
    Properties = new() { ["opacity"] = Bm.Tween(0.1) },  // opacity snaps faster
}
```

A target can also **embed** its own transition, which wins over the component's `Transition`:

```csharp
Bm.To(x: 100, transition: Bm.Spring(bounce: 0.6))
```

## Keyframes

Every property accepts a single value or a keyframe sequence via collection expressions:

```razor
<Bmotion Animate="Bm.To(scale: [1, 1.3, 0.8, 1.1, 1], rotate: [0, 15, -10, 5, 0])"
         Transition="Bm.Tween(1.2, BmEase.InOut, repeat: BmRepeat.Mirror())">
    <div />
</Bmotion>

<Bmotion Animate='Bm.To(backgroundColor: ["#6c47ff", "#ff4785", "#6c47ff"])'
         Transition="Bm.Tween(3, BmEase.Linear, repeat: BmRepeat.Forever)">
    <div />
</Bmotion>
```

- `times: [0, 0.2, 0.5, 1]` on `Bm.Tween` sets custom keyframe offsets.
- `eases: [BmEase.CircIn, BmEase.CircOut, ...]` gives each keyframe **segment** its own curve
  (one entry per segment; the last entry repeats when the array is shorter):
  `Bm.Tween(2, eases: [BmEase.CircOut, BmEase.CircIn, BmEase.BackOut])`.
- `Bm.Current` inside a sequence is a wildcard for the element's current value:
  `x: [Bm.Current, 100]` continues seamlessly from wherever the element is now.
- **Complex strings interpolate**: between two values with the same shape, every number and
  embedded color animates - `filter: "blur(0px) brightness(1)"` → `"blur(8px) brightness(1.4)"`,
  multi-part `boxShadow`s, matching gradients, mixed-unit strings. Shapes that don't match
  snap to the target instead.

## Variants

Named states declared once, selected by name - with razor-literal-friendly `State` /
`InitialState` parameters. The active state propagates to descendants automatically:

```razor
<Bmotion Variants="_list" InitialState="hidden"
         State='@(_open ? "visible" : "hidden")'
         Transition="Bm.Tween(staggerChildren: 0.08, delayChildren: 0.2)">
    <div>
        <Bmotion Variants="_item">
            <div>Item 1</div>
        </Bmotion>
        <Bmotion Variants="_item">
            <div>Item 2</div>
        </Bmotion>
    </div>
</Bmotion>

@code {
    private readonly BmVariants _list = new()
    {
        ["hidden"]  = Bm.To(opacity: 0),
        ["visible"] = Bm.To(opacity: 1),
    };

    private readonly BmVariants _item = new()
    {
        ["hidden"]  = Bm.To(opacity: 0, x: -30),
        // a variant can embed its own transition
        ["visible"] = Bm.To(opacity: 1, x: 0, transition: Bm.Spring(stiffness: 300)),
    };
}
```

Dynamic variants receive the component's `Custom` parameter:

```csharp
_item.Add("visible", custom => Bm.To(x: 10 * (int)custom!));
```

```razor
<Bmotion Variants="_item" Custom="@i">
    <div />
</Bmotion>
```

## Drag

Motion-style flat parameters:

```razor
<Bmotion Drag="true" DragElastic="0.5">
    <div />
</Bmotion>

<Bmotion Drag="BmDrag.X"
         DragConstraints="BmDragConstraints.Horizontal(-200, 200)"
         DragMomentum="true"
         DragSnapToOrigin="false"
         DragDirectionLock="true"
         DragTransition="Bm.Spring(stiffness: 400, damping: 35)">
    <div />
</Bmotion>
```

Constraints can also be **element bounds** (motion.dev's `dragConstraints={ref}`): the
container is measured at each drag start, so responsive layout changes just work.

```razor
<Bmotion Drag="true" DragConstraints="BmDragConstraints.Parent()">
    <div />   @* stays inside its parent element *@
</Bmotion>

<Bmotion Drag="true" DragConstraints='BmDragConstraints.Within(".drop-zone")'>
    <div />   @* stays inside the first element matching the selector *@
</Bmotion>
```

**Handles and drag controls.** `DragHandle` restricts the drag to a grip inside the element;
`BmDragControls` (motion.dev's `useDragControls`) starts the drag from any other element -
pair it with `DragListener="false"` so the controls are the only trigger:

```razor
<Bmotion Drag="true" DragHandle=".grip">
    <div class="row"><span class="grip">⠿</span> Only the grip drags</div>
</Bmotion>

<div class="track" @onpointerdown="e => _controls.StartAsync(e)">
    <Bmotion Drag="BmDrag.X" DragControls="_controls" DragListener="false"
             DragConstraints="BmDragConstraints.Parent()">
        <div class="thumb" />   @* press anywhere on the track to grab the thumb *@
    </Bmotion>
</div>

@code {
    private readonly BmDragControls _controls = new();
}
```

**Per-edge elasticity.** `DragElastic` accepts a uniform value or per-edge values
(unspecified edges are rigid):

```razor
<Bmotion Drag="true" DragConstraints="BmDragConstraints.Parent()"
         DragElastic="BmDragElastic.Edges(right: 0.9, bottom: 0.9)">
    <div />
</Bmotion>
```

## Layout & shared elements

`Layout` plays a FLIP animation whenever a re-render moves or resizes the element:

```razor
@* animate position + size *@
<Bmotion Layout="true" ...>
    <div />
</Bmotion>

@* position only - no scale distortion on text *@
<Bmotion Layout="BmLayout.Position" ...>
    <div />
</Bmotion>
```

`LayoutId` connects elements across mounts: when one element unmounts and another mounts with
the same id, the new one FLIPs from where the old one was - the tab-underline / card-to-detail
idiom:

```razor
@if (tab == _activeTab)
{
    <Bmotion LayoutId="underline">
        <div style="position:absolute;bottom:0;left:0;right:0;height:3px;background:#6c47ff;" />
    </Bmotion>
}
```

Wrap independent groups in `<BmotionLayoutGroup Name="sidebar">` to namespace their ids.

---

## Programmatic API

`BmotionAnimateService` (inject as `Motion`) animates elements by CSS selector or
`ElementReference` - no `<Bmotion>` wrapper needed:

```razor
@inject BmotionAnimateService Motion

@code {
    async Task Animate()
    {
        var controls = await Motion.AnimateAsync("#target", Bm.To(x: 100, opacity: 0.5), Bm.Tween(0.6));
        controls.Pause();          // playback controls: Pause / Play / SetSpeed / Stop / Complete
        controls.SetSpeed(2);
        await controls;            // directly awaitable
    }

    // Stagger across all matched elements
    Task Cascade() => Motion.AnimateAsync(".item", Bm.To(opacity: 1, y: 0),
        Bm.Spring(), stagger: Bm.Stagger(0.08, from: BmStaggerFrom.Center)).AsTask();

    // Animate a raw number (counters, canvas, anything outside the DOM)
    Task CountUp() => Motion.AnimateAsync(0, 100,
        v => { _count = (int)v; StateHasChanged(); }, Bm.Tween(1.5));
}
```

### Sequences

Multi-step timelines with motion.dev-style `at` offsets:

```csharp
var seq = new BmSequence()
    .Add("#box", Bm.To(x: 100), Bm.Tween(0.5))
    .Add("#box", Bm.To(y: 50), Bm.Tween(0.3), at: "-0.1")   // overlap previous end by 0.1s
    .Label("burst")
    .Add(".dot", Bm.To(scale: [1, 1.4, 1]), at: "burst");   // at a named label

var controls = await Motion.RunAsync(seq);
```

`at` accepts: `"+0.5"` / `"-0.2"` (relative to previous end), `"<"` / `"<0.3"` (previous
start), `"1.5"` (absolute), or a label name.

---

## Motion values

A reactive value graph, fully composable and bindable to elements:

```csharp
var x = Bm.Value(0.0);
x.Subscribe(v => Console.WriteLine(v));
x.SetSync(100);
x.GetVelocity();                                  // units/sec
x.Jump(0);                                        // set without feeding physics

var angle = x.Transform([0, 200], [0, 360]);      // range mapping
var label = x.Transform(v => $"{v:0}px");         // arbitrary derivation (useTransform)
var smooth = Motion.Spring(x, Bm.Spring(stiffness: 120));  // spring follower (useSpring)

await Motion.AnimateAsync(x, 200, Bm.Spring());   // animate the value itself
```

Bind values straight to element properties - changes flush to the DOM each frame **without
re-rendering** (the `style={{ x }}` equivalent):

```razor
<Bmotion Values='new() { ["x"] = _x, ["rotate"] = _angle }'>
    <div />
</Bmotion>
```

`Bm.Template` composes motion values into a CSS string (motion.dev's `useMotionTemplate`),
and `StringValues` binds string values to any CSS property:

```csharp
var blur = Bm.Value(0.0);
var filter = Bm.Template(() => $"blur({blur.Value}px)", blur);
```

```razor
<Bmotion StringValues='new() { ["filter"] = _filter }'>
    <div />
</Bmotion>
```

`BmotionScrollTracker` exposes scroll positions as motion values, and can track a target
element's journey through the viewport between configurable offsets (motion.dev's
`useScroll({ target, offset })`):

```csharp
await Scroll.ObserveAsync(new BmScrollOptions
{
    TargetId = "hero",
    Offset = ["start end", "end start"],   // 0 when hero enters at the bottom, 1 when it leaves at the top
}, _ => Task.CompletedTask);

// then compose: Scroll.TargetProgressValue.Transform([0, 1], [0, -120])
```

Scroll-linked animations compose end-to-end:

```razor
@inject BmotionScrollTracker Scroll

<Bmotion Values="_bar">
    <div style="transform-origin:0 50%;" />
</Bmotion>

@code {
    private Dictionary<string, BmValue<double>> _bar = default!;

    protected override void OnInitialized()
        => _bar = new() { ["scaleX"] = Scroll.ProgressYValue };

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender) await Scroll.ObserveAsync(_ => Task.CompletedTask);
    }

    // BmotionScrollTracker is transient and owned by this component:
    public ValueTask DisposeAsync() => Scroll.DisposeAsync();
}
```

---

## Examples

See the `Demos` samples app for runnable examples of basic animations, gestures, springs,
drag, variants & stagger, keyframes, enter/exit transitions, presence switching, layout
(FLIP) animations, scroll-linked motion values and programmatic control.

---

## Accessibility

Tap gestures are keyboard-accessible out of the box: when a tappable element has focus,
<kbd>Enter</kbd> and <kbd>Space</kbd> press and release it exactly like a pointer tap
(`WhileTap` plays, `OnTapStart`/`OnTap` fire; losing focus mid-press cancels). Give the
element `tabindex="0"` if it isn't natively focusable.

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
