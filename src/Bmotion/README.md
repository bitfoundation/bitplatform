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
  - [BmotionSplitText](#bmotionsplittext)
  - [BmotionConfig](#bmotionconfig)
- [Transitions](#transitions)
- [Keyframes](#keyframes)
- [Variants](#variants)
- [Drag](#drag)
- [Layout & shared elements](#layout--shared-elements)
- [Scroll timelines](#scroll-timelines)
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

Bm.Value(0.0)                                     // a reactive motion value
Bm.Velocity(x)                                    // a motion value carrying x's units/sec (useVelocity)
Bm.Clamp(0, 10, v) · Bm.Wrap(0, 10, v) · Bm.Mix(a, b, t) · Bm.MapRange(v, [0, 100], [0, 1])
```

Every transition is reusable: `transition.WithDelay(seconds)` returns a copy with a new delay, so
one configured spring can drive a whole staggered set without them fighting over its `Delay`.

`Bm.To(...)` returns a `BmProps`; every parameter is optional:

| Group | Properties |
|---|---|
| Transforms | `x, y, z, scale, scaleX, scaleY, rotate, rotateX, rotateY, rotateZ, skewX, skewY, perspective, originX, originY` |
| Visual | `opacity, backgroundColor, color, borderColor, outlineColor, fill, stroke, width, height, borderRadius, boxShadow, filter` |
| Layout / box-model | `top, left, right, bottom, margin, padding, gap` |
| Typography | `letterSpacing, lineHeight, fontSize` |
| Misc CSS | `clipPath, backgroundPosition, backgroundSize` |
| Motion path | `offsetPath, offsetDistance` |
| SVG | `d` (shape morph), `pathLength, pathOffset, pathSpacing` (stroke drawing) |
| Escape hatches | `cssVars` (custom properties), `css` (any other CSS property), `transition` |

Anything without a typed parameter goes through `css`, which takes dash-case or camelCase keys:
`Bm.To(css: new() { ["mix-blend-mode"] = "screen" })`.

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
| `Inherit` | `bool` | Default `true`. `false` cuts this element out of an ancestor's variant cascade (it still cascades its own variants downward) |
| `TransformTemplate` | `BmTransformTemplate?` | Rewrites the composed `transform` string - reorder the components, or keep one of your own in front |
| `GesturePropagation` | `bool` | Default `true`. `false` stops this element's tap/pan from also reaching gesture-enabled ancestors |
| `Values` | `Dictionary<string, BmValue<double>>?` | Motion-value bindings (`style={{ x }}` equivalent) |
| `StringValues` | `Dictionary<string, BmValue<string>>?` | String motion-value bindings for any CSS property (`useMotionTemplate` equivalent, see [Motion values](#motion-values)) |
| `Drag`, `DragConstraints`, `DragElastic`, `DragMomentum`, `DragSnapToOrigin`, `DragDirectionLock`, `DragTransition`, `DragHandle`, `DragControls`, `DragListener` | | See [Drag](#drag) |
| `Timeline` | `BmScrollTimeline?` | Drives `Animate` from scroll position on the browser's native scroll timelines (see [Scroll timelines](#scroll-timelines)) |
| `Layout` | `BmLayout` | Automatic FLIP layout animations (`true`, `BmLayout.Position` or `BmLayout.Size`) |
| `LayoutId` | `string?` | Shared-element transitions (see [Layout & shared elements](#layout--shared-elements)) |
| `Once` / `Viewport` | `bool` / `BmViewport?` | Viewport tracking for `WhileInView` (`Viewport.Root` measures against a scroll container instead of the page) |
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

#### Controlling the transform string

`TransformTemplate` rewrites the `transform` Bmotion composes - to reorder the components, or to
keep one of your own in front of the animated ones:

```razor
@* stays centred on its own anchor whatever the animation does *@
<Bmotion Animate="Bm.To(scale: 1.2)"
         TransformTemplate="(_, generated) => $&quot;translate(-50%, -50%) {generated}&quot;">
    <div class="pin" />
</Bmotion>
```

It receives the element's live transform components (px and degrees) and the string Bmotion would
have written, and applies on every path that writes a transform - the frame loop, the
pre-first-paint inline style, instant `Set` calls and the keyframes handed to the compositor - so
the element never flickers between a templated and an untemplated transform.

#### Nested gestures

Pointer gestures bubble, so pressing a tappable child also presses its tappable parent.
`GesturePropagation="false"` stops that for tap and pan, exactly as `DragPropagation` does for drag
(hover is unaffected - `pointerenter`/`pointerleave` don't bubble in the first place):

```razor
<Bmotion WhileTap="Bm.To(scale: 0.98)">          @* the card *@
    <div class="card">
        <Bmotion WhileTap="Bm.To(scale: 0.9)" GesturePropagation="false">
            <button>Only this presses</button>
        </Bmotion>
    </div>
</Bmotion>
```

#### Event callbacks

```text
OnHoverStart / OnHoverEnd
OnTapStart / OnTap / OnTapCancel
OnFocusStart / OnFocusEnd
OnPanStart / OnPan / OnPanEnd                  (BmPanInfo)
OnDragStart / OnDrag / OnDragEnd
OnDirectionLock                                (BmDragAxis - the axis DragDirectionLock resolved)
OnLayoutAnimationStart / OnLayoutAnimationComplete
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

### BmotionSplitText

Staggered text animation - motion.dev's `splitText` and GSAP's `SplitText`, with no DOM surgery.
The text is split in **C#** and every unit is rendered as its own `<Bmotion>` element, so there is
nothing to re-split on re-render and nothing for a script to undo:

```razor
<BmotionSplitText Text="Every character"
                  By="BmSplitBy.Chars"
                  Initial="Bm.To(opacity: 0, y: 24, rotate: -12)"
                  Animate="Bm.To(opacity: 1, y: 0, rotate: 0)"
                  Stagger="Bm.Stagger(0.03)"
                  Transition="Bm.Spring(bounce: 0.35, duration: 0.7)" />
```

| Parameter | Type | Description |
|---|---|---|
| `Text` | `string?` | The text to split and animate |
| `By` | `BmSplitBy` | `Chars` (default), `Words`, or `Lines` (splits on authored newlines) |
| `Initial` / `Animate` / `Exit` | `BmTarget?` | Forwarded to every unit |
| `WhileHover` / `WhileTap` / `WhileInView` | `BmTarget?` | Per-unit gestures - the hover lands on the one character the pointer is over |
| `Once` / `Viewport` | `bool` / `BmViewport?` | Viewport options for `WhileInView` |
| `Transition` | `BmTransition?` | Timing for every unit; each unit's stagger offset is added to its delay |
| `Stagger` | `BmStagger?` | The cascade across units. Default `Bm.Stagger(0.03)` |
| `As` / `Class` / `Style` / `UnitClass` | `string?` | The container element/styling and the per-unit class |
| `Accessible` | `bool` | Default `true`: `aria-label` on the container, `aria-hidden` on the units |
| `OnComplete` | `EventCallback` | Fires once the last unit finishes |

The split is **not** a bag of one-character spans: whitespace is rendered as whitespace rather than
as units, and in `Chars` mode each word is wrapped in its own inline-block. So the text wraps
between words (never mid-word), a selection copies the original string back, and a screen reader
reads the sentence once instead of spelling it out.

> Every unit is a real animated element, so `Chars` on a paragraph means hundreds of them. Split
> headlines by character; split body copy by `Words` or `Lines`.

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
Bm.Inertia(velocity: 500, modifyTarget: Bm.SnapTo(100))   // coast, then settle on a 100px grid
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

### Arcs

`path: Bm.Arc(...)` bends the straight line between two points into a curve. There is no path data
to author - the curve is generated from wherever the element is to wherever it is going, so it
keeps working when either end moves:

```razor
<Bmotion Animate="Bm.To(x: 220, y: 90)"
         Transition="Bm.Tween(0.8, path: Bm.Arc(strength: 0.8, rotate: 1))">
    <div class="card" />
</Bmotion>
```

| Option | Default | Meaning |
|---|---|---|
| `strength` | `0.5` | How far it bends, as a fraction of the distance travelled. `1` peaks a full travel-distance off the line |
| `peak` | `0.5` | Where the curve crests - `0` towards the start, `1` towards the end |
| `direction` | `Auto` | Which side it bulges. `Auto` arcs upward, which reads as "thrown" |
| `rotate` | `0` | How much the element turns to follow the curve: `1` points it along the tangent |

The timing still comes from the transition, so a spring on a path arcs *and* overshoots. An arc
needs both `x` and `y` as single values in the same target - a keyframe sequence on either axis is
already describing its own path, so it is left alone. Because the two axes have to move together,
an arc runs on the C# frame loop rather than the compositor (so, like drag, it is a
**Blazor WebAssembly** feature).

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

### Orchestration

A variant container's transition carries the orchestration knobs for the subtree beneath it:

```csharp
Bm.Tween(0.3, staggerChildren: 0.08, delayChildren: 0.2)

// "the panel opens, then its contents cascade in": children wait out the container's own
// animation, so the offset stays correct when you change the container's duration
Bm.Tween(0.4, staggerChildren: 0.06, when: BmWhen.BeforeChildren)
```

```csharp
// the mirror image: the contents leave, then the panel closes behind them
Bm.Tween(0.3, staggerChildren: 0.06, when: BmWhen.AfterChildren)

// the cascade doesn't have to run first-to-last - it can radiate from anywhere
Bm.Tween(0.3, childStagger: Bm.Stagger(0.06, from: BmStaggerFrom.Center))
Bm.Tween(0.3, childStagger: Bm.Stagger(0.04, grid: (cols: 6, rows: 4)))
```

`childStagger` takes the same `BmStagger` the programmatic API uses, so `from` origins, grids and
fully custom `(index, total) => delay` generators all work. It supersedes the flat
`staggerChildren` interval; `delayChildren` still adds on top of either.

A spring has no true end, so `BeforeChildren` uses `Bm.Spring(duration:)` when set and estimates
from the physics otherwise. `AfterChildren` waits for the real cascade: the latest child's stagger
slot plus that child's own resolved transition.

`Inherit="false"` cuts one element out of the cascade entirely - it stops reacting to the label
coming down while everything below it still inherits the variants *it* defines:

```razor
<Bmotion Variants="_list" State="visible" Transition="Bm.Tween(staggerChildren: 0.08)">
    <div>
        <Bmotion Variants="_item"><div>In the cascade</div></Bmotion>
        <Bmotion Inherit="false" Animate="Bm.To(opacity: 1)">
            <div>Runs to its own timing</div>
        </Bmotion>
    </div>
</Bmotion>
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

**Snapping on release.** A `DragTransition` authored as an *inertia* configures the momentum
itself - including `modifyTarget`, which decides where the coast comes to rest. That is how a
carousel pages and a slider clicks onto a grid: the fling projects a resting position, and
`modifyTarget` rounds it. `Bm.SnapTo` builds the usual two shapes for you:

```razor
@* release anywhere on the track; settle on the nearest 120px stop *@
<Bmotion Drag="BmDrag.X" DragConstraints="BmDragConstraints.Parent()"
         DragTransition="Bm.Inertia(modifyTarget: Bm.SnapTo(120))">
    <div />
</Bmotion>

@* a three-page carousel: snap to whichever page the fling was heading for *@
<Bmotion Drag="BmDrag.X"
         DragTransition="Bm.Inertia(modifyTarget: Bm.SnapTo([0, -320, -640]))">
    <div />
</Bmotion>
```

With a `modifyTarget` set, even a slow release coasts to the nearest stop instead of resting
where the pointer left it. Snapping happens before constraint clamping, so a snapped target can
never land outside the bounds.

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

@* size only - the element snaps to its new spot and only the box grows/shrinks *@
<Bmotion Layout="BmLayout.Size" ...>
    <div />
</Bmotion>
```

Four options control how the projection is measured:

| Parameter | Use it when |
|---|---|
| `LayoutAnchor` | The wrong part of the box appears to stay still. Default `TopLeft`; `BmLayoutAnchor.Center` makes a resizing box grow evenly around its middle |
| `LayoutScroll` | The element lives in a **scrolling container**. Without it, scrolling that container between the two measurements reads as the element having moved, and it visibly jumps |
| `LayoutRoot` | The element is `position: fixed`. It stays put while the page scrolls, so it is measured in viewport rather than document coordinates |
| `LayoutDependency` | Measuring on every re-render is costing you a forced reflow. Point it at the state the layout depends on and unrelated renders stop measuring |

```razor
@* an accordion panel inside a scrolling list, measured only when it actually opens/closes *@
<Bmotion Layout="BmLayout.Size"
         LayoutAnchor="BmLayoutAnchor.Center"
         LayoutScroll="true"
         LayoutDependency="_isOpen"
         OnLayoutAnimationComplete="@(() => _settled = true)">
    <div class="panel" />
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

## Scroll timelines

`Timeline` drives `Animate` from **scroll position instead of time**, on the browser's native
`ScrollTimeline` / `ViewTimeline`. The keyframes are pre-sampled in C# once and handed over; after
that there is no scroll handler, no frame loop and no interop - the animation runs on the
compositor.

```razor
@* a reading-progress bar: no scroll handler, no disposal, no state *@
<Bmotion Timeline="BmScrollTimeline.Page()" Animate="Bm.To(scaleX: [0, 1])">
    <div class="progress-bar" style="transform-origin:0 50%;" />
</Bmotion>

@* the element's own journey through the viewport: 0 as it enters, 1 as it leaves *@
<Bmotion Timeline="BmScrollTimeline.View()"
         Animate="Bm.To(opacity: [0, 1, 1, 0], y: [40, 0, 0, -40])">
    <div class="card" />
</Bmotion>
```

| Source | Progress measured over |
|---|---|
| `BmScrollTimeline.Page(axis)` | the whole document's scroll |
| `BmScrollTimeline.Container(selector, axis)` | one scroll container's scroll |
| `BmScrollTimeline.View(axis, range)` | the animated element's journey through the scrollport |
| `BmScrollTimeline.ViewOf(selector, axis, range)` | another element's journey, driving this one |

`range` takes CSS `animation-range` syntax (`"entry 0% cover 50%"`) and needs native support.

- Only **transform components and `opacity`** can be scroll-driven - they are what the browser can
  interpolate for us. A target touching anything else falls back to the ordinary time-based path.
- `Transition` does not apply while a timeline is attached: scroll position *is* the progress.
- A timeline **owns the properties it animates**, so don't also aim a gesture or a second animation
  at them.
- Without native scroll timelines the bridge scrubs the same Web Animation from one passive scroll
  listener - still no per-frame interop, still the browser interpolating. Custom `range` strings
  need the native API and the fallback covers the full journey instead.

Use `BmotionScrollTracker` (see [Motion values](#motion-values)) instead when you need the scroll
progress *as a number* - to drive C# state, compose it with other values, or animate a property
the browser can't own.

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

The whole timeline runs on **one playhead**, driven by the animation clock rather than by
wall-clock timers, so the returned controls govern the gaps between steps as well as the steps
themselves: `controls.Pause()` genuinely holds the sequence instead of letting its later steps
start on time, and `controls.SetSpeed(3)` compresses the silences by the same factor as the
movement. (On Blazor Server, where there is no frame loop, the gaps fall back to wall-clock
timers along with everything else that needs the loop.)

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
var raw   = x.Transform([0, 200], [0, 360], clamp: false);  // extrapolate past the ends
var label = x.Transform(v => $"{v:0}px");         // arbitrary derivation (useTransform)
var speed = Bm.Velocity(x);                       // units/sec as its own value (useVelocity)
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

See the `Demo` samples app for runnable examples of basic animations, gestures, springs,
drag, variants & stagger, keyframes, enter/exit transitions, presence switching, layout
(FLIP) animations, scroll-linked motion values and programmatic control.

---

## Accessibility

Tap gestures are keyboard-accessible out of the box: when a tappable element has focus,
<kbd>Enter</kbd> and <kbd>Space</kbd> press and release it exactly like a pointer tap
(`WhileTap` plays, `OnTapStart`/`OnTap` fire; losing focus mid-press cancels). Give the
element `tabindex="0"` if it isn't natively focusable.

Bmotion honours the user's **prefers-reduced-motion** preference. Choose how, globally, at
registration:

```csharp
builder.Services.AddBitBmotionServices(o => o.ReducedMotion = BmReducedMotionMode.User);
```

| `BmReducedMotionMode` | Behaviour |
|---|---|
| `IgnoreUnlessConfigured` | **Default (back-compat).** OS preference respected only inside a `<BmotionConfig>`. |
| `User` | **Recommended.** Respect the OS preference everywhere - the web-platform default. |
| `Always` | Always reduce, regardless of the OS. |
| `Never` | Never reduce, regardless of the OS. |

When motion is reduced, Bmotion follows Motion's `"user"` semantics: **transforms, layout and
dimension changes snap to their target instantly, while opacity and colour still animate** - a
softer, more correct reduction than collapsing every property to instant. Stagger delays are
dropped too, so a reduced list appears at once instead of trickling in.

A local `<BmotionConfig ReduceMotion="true|false">` always overrides the global mode for its
subtree (`null` = follow the mode):

```razor
<BmotionConfig ReduceMotion="true">   @* force-reduce this subtree *@
    ...
</BmotionConfig>
```

> **Migration note.** The default remains `IgnoreUnlessConfigured` so existing apps are unaffected.
> New apps should set `ReducedMotion = BmReducedMotionMode.User` to match the platform default;
> this is planned to become the default in a future major version.

---

## License

[MIT](https://github.com/bitfoundation/bitplatform/blob/develop/LICENSE)
