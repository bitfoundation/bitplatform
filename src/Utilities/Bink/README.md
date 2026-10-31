# Bit.Bink

A native drawing and ink engine for Blazor.

The stroke model, smoothing, pressure response, hit testing, undo, serialization and export are written in C#.
JavaScript does only what the browser alone can do: collect pointer and keyboard input, and replay a command
buffer onto a canvas. There is no JavaScript drawing library underneath. The one drawing JavaScript does is a
provisional trail under Blazor Server, described in [hosting models](#hosting-models), which exists to hide network
latency and is replaced by the engine's own geometry.

```razor
<BinkCanvas @ref="canvas" Height="200px" AriaLabel="Signature" />

@code {
    private BinkCanvas? canvas;

    private void Clear() => canvas!.Clear();
    private Task<string> SaveAsync() => canvas!.ExportSvgAsync();
}
```

## What it is for

Signature fields, handwriting input, whiteboards, image markup, document review, diagram surfaces, and anything
else that needs ink. The engine is a vector document model, so a drawing can be saved, versioned, transmitted,
re-rendered at any size and edited later, rather than being a picture of a drawing.

## Highlights

- **Pressure-aware ink** with spacing filters, streamline smoothing, simulated pressure for devices without it,
  and variable-width outlines that stay solid through tight loops and sharp corners.
- **Six built-in brushes** (pen, pencil, marker, highlighter, broad-nib calligraphy, pixel eraser) and a brush model
  you can extend.
- **Three erasers** that are genuinely different: whole stroke, segment and pixel.
- **Selection, transforms and layers** that are part of the document, so they survive saving, take part in undo
  and export exactly as drawn, with lasso selection and snapping to a grid and to other elements.
- **Editing commands** a whiteboard needs: copy, cut and paste through the system clipboard (images and text
  included), duplicate, Alt-drag copies, bring-to-front, nested groups, locking, align, distribute and flip, each
  with its usual shortcut and one undo step.
- **Annotations**: shapes, arrows, highlights, callouts, text with a real HTML editor, and images.
- **Undo and redo** that store inverse operations, never snapshots.
- **A versioned, compact JSON format** with binary stroke points, validated against hostile input.
- **SVG, PNG, JPEG and WebP export** that match the screen, including a pure .NET PNG path for servers and tests.
- **Real-time collaboration**: transport-agnostic operation sync, presence and offline queueing.
- **Touch, stylus, mouse and trackpad**: palm rejection, pinch zoom, wheel and trackpad pan and zoom, coalesced
  samples, high-DPI canvases and an opt-in low-latency canvas.
- **Retained rendering**: the browser keeps every path it has drawn, so a redraw of a large page costs a few
  numbers per element rather than every coordinate, which is what keeps Blazor Server fast too.
- **Accessibility**: a labelled, focusable, keyboard-operable canvas, a live text summary of the drawing and
  polite announcements.
- Works unchanged on **Blazor WebAssembly, Blazor Server, Blazor Web App and Blazor Hybrid**, including while
  prerendering, and can be [lazy loaded](#lazy-loading-on-webassembly) on WebAssembly.

## Quick start

```bash
dotnet add package Bit.Bink
```

Add the namespace to `_Imports.razor`:

```razor
@using Bit.Bink
```

Put a canvas on a page:

```razor
<BinkCanvas Height="400px" />
```

There are no services to register and no stylesheet to reference: the stylesheet the component needs is injected
automatically by the package's Blazor initializer. The full walkthrough is in [getting started](#getting-started).

## Requirements

- .NET 10 or .NET 11
- Blazor WebAssembly, Blazor Server, Blazor Web App or Blazor Hybrid

### Browser support

Drawing, editing, saving and export work in every current browser. A few capabilities depend on what the browser
offers; where one is missing the canvas does without it rather than failing.

| Capability | Chromium (Chrome, Edge) | Firefox | Safari | Without it |
| --- | --- | --- | --- | --- |
| Coalesced pen samples | Yes | Yes (none on Android) | Yes | Fewer samples on very fast strokes |
| Predicted samples (`PredictInk`) | Yes | 89+ | 18.2+ | About a frame more latency |
| Low-latency canvas (`LowLatency`, off by default) | Windows, ChromeOS | No | 15+ | Normal latency |
| WebP export | Yes | Yes | No | `NotSupportedException`; use PNG or JPEG |
| A picture on the clipboard with a copy | Yes | 127+ | Yes | The copy pastes into canvases only |
| Eyedropper (`PickColorAsync`) | Desktop | No | No | Returns null |
| IndexedDB (`BinkBrowserDocumentStore`) | Yes | Yes | Yes | Not in some private windows |
| Notice of a canvas the browser discarded | Yes | 125+ | No | Repainted at the next change or resize |

Pen pressure, tilt and the eraser end depend on the device and its driver as much as on the browser; read
`Capabilities.ObservedPressure` and the other observed flags rather than assuming them.

## The demo

The demo site, [bink.bitplatform.dev](https://bink.bitplatform.dev), covers a signature field, a handwriting
surface, a full whiteboard, a diagram surface with connectors that follow the nodes they join, image annotation,
document review, two-way collaboration, touch, keyboard and screen reader use, the serialization format, a
performance page that measures the engine in your browser, and a custom brush, element type and render target.
Every page explains what it shows, and its source is one click away.

[`Samples`](Samples/) is the hosting harness rather than the docs: one shared [Core](Samples/Core/) project of
pages (signature, whiteboard, images, export, collaboration, autosave and canvas lifecycle), hosted unchanged under
[Server](Samples/Server/), [WASM](Samples/Wasm/), [Auto](Samples/Auto/) and [MAUI](Samples/MAUI/) (Blazor Hybrid on
Android, iOS, macOS and Windows), so the same drawing code can be exercised in every hosting model.

## How it fits together

```text
Pointer events (browser)
  -> batched once per animation frame
  -> BinkStrokeBuilder      spacing filter, streamline smoothing, pressure
  -> BinkStroke + BinkBrush immutable vector model
  -> BinkDrawingOperation   the unit of undo, diffing and collaboration
  -> BinkDocument           layers of elements
  -> IBinkRenderContext     canvas commands, SVG, or the built-in rasterizer
```

Everything ships in one package, `Bit.Bink`, under one namespace, `Bit.Bink`:

| Part | Contents |
| --- | --- |
| The engine | Document model, strokes, brushes, geometry, tools, undo, serialization, SVG and PNG export. No UI and no JavaScript; it runs on a server, in a console application or in a unit test. |
| The component | `BinkCanvas` and the browser layer. |
| Collaboration | Transport-agnostic operation sync, presence and offline queueing. Optional: nothing in the engine uses it unless you create a session. |

A signature pad needs only `BinkCanvas`.

Every renderer draws the same elements through the same `IBinkRenderContext` interface, so an exported file
matches what was drawn. Pointer input never triggers a Blazor render: samples arrive once per frame, the active
tool processes them, and only the affected canvas layer is redrawn.

## MCP server

The demo hosts a [Model Context Protocol](https://modelcontextprotocol.io) server at
`https://bink.bitplatform.dev/mcp`, so an AI agent writing drawing code reads the library itself - its API, this
guide and the demo's working pages - instead of guessing at an API it half remembers.

```json
{
  "mcpServers": {
    "bink": { "type": "http", "url": "https://bink.bitplatform.dev/mcp" }
  }
}
```

Start with `SearchBink`, which searches the guide, every public type and member, the built-in brushes, the setup
guides, the stylesheet and the demo's pages at once, and points each hit at the exact follow-up call.
`GetBinkSetupGuide` gives the wiring for one render mode, `GetBinkGuideSection` one section of this guide,
`GetBinkApi` one public type with its members, real defaults and documentation, `GetBinkBrushes` the built-in
brushes with their defaults and serialized form, and `GetBinkSourceFile` one of the demo's working pages; each
returns its index when called without a key. `InspectBinkDocument` runs a saved document through the library's own
serializer and exporters and reports what it contains, what a round trip and a PNG or SVG export do with it, and
what to fix - or, called with no document, returns a sample one. Every tool is read-only.

## Documentation

- [Getting started](#getting-started)
- [Architecture](#architecture)
- [Drawing, brushes and pressure](#drawing-brushes-and-pressure)
- [Erasing](#erasing)
- [Selection, transforms and layers](#selection-transforms-and-layers)
- [Annotations, text and images](#annotations-text-and-images)
- [Serialization](#serialization)
- [SVG and PNG export](#svg-and-png-export)
- [Hosting models](#hosting-models)
- [Mobile, touch and stylus](#mobile-touch-and-stylus)
- [Accessibility](#accessibility)
- [Performance](#performance)
- [Collaboration](#collaboration)
- [Extending Bink](#extending-bink)

## Getting started

### Install

```bash
dotnet add package Bit.Bink
```

Add the namespace to `_Imports.razor`. Points and sizes in the API are `System.Numerics.Vector2`, so most code that
builds documents needs that namespace too:

```razor
@using Bit.Bink
@using System.Numerics
```

There are no services to register. The component's stylesheet, `_content/Bit.Bink/bit-bink.css`, is injected
automatically when the app starts by the package's Blazor initializer (`Bit.Bink.lib.module.js`). If you prefer to
control it, add the link yourself and the automatic injection stands down:

```html
<link rel="stylesheet" href="_content/Bit.Bink/bit-bink.css" />
```

Having nothing to register is also what lets a WebAssembly app download Bink only when a page first needs it; see
[lazy loading on WebAssembly](#lazy-loading-on-webassembly).

### A drawing surface

```razor
<BinkCanvas Height="400px" />
```

That is a working canvas: pointer and stylus input, and touch as soon as you set `EnableTouchGestures`,
pressure-aware ink, undo and redo on the keyboard.

### A signature field

```razor
<BinkCanvas @ref="_canvas" Height="180px" AriaLabel="Sign here"
            DocumentChanged="() => StateHasChanged()" />

<button @onclick="Clear" disabled="@IsEmpty">Clear</button>
<button @onclick="AcceptAsync" disabled="@IsEmpty">Accept</button>

@code {
    private BinkCanvas? _canvas;

    private bool IsEmpty => _canvas?.CurrentDocument.IsEmpty ?? true;

    private void Clear() => _canvas!.Clear();

    private async Task AcceptAsync()
    {
        string svg = await _canvas!.ExportSvgAsync(new BinkSvgExportOptions
        {
            Region = BinkExportRegion.Content,   // crop to the ink
            Background = BinkColor.Transparent,
        });
        await SaveToServerAsync(svg);
    }
}
```

`DocumentChanged` is what keeps your own UI in step. A child component cannot re-render its parent on its own, so
bind this event whenever your markup reads canvas state such as `CurrentDocument.IsEmpty` or `History.CanUndo`.
Drawing itself never re-renders anything.

### Choosing a brush

```razor
<BinkCanvas Brush="_brush" Height="400px" />

@code {
    private BinkBrush _brush = new BinkPenBrush
    {
        Color = BinkColor.Parse("#1d4ed8"),
        Width = 5f,
    };
}
```

Brushes are immutable records, so derive variants with `with`:

```csharp
_brush = _brush with { Color = BinkColor.Parse("#dc2626") };
```

The built-in brushes are `BinkPenBrush`, `BinkPencilBrush`, `BinkMarkerBrush`, `BinkHighlighterBrush`,
`BinkCalligraphyBrush` and
`BinkPixelEraserBrush`.

For a colour picker's "pick from screen" button, `PickColorAsync` opens the browser's eyedropper. Only Chromium
browsers on the desktop have one, so ask `SupportsEyeDropperAsync` before offering it, and call it straight from the
click, which is what lets the browser open it:

```csharp
if (await _canvas.PickColorAsync() is { } picked) _brush = _brush with { Color = picked };
```

### Choosing a tool

The `Tool` parameter decides what pointer input does.

```razor
<BinkCanvas Tool="_tool" Brush="_brush" Height="400px" />

@code {
    private IBinkTool _tool = new BinkDrawTool();

    private void UseEraser() => _tool = new BinkEraserTool { Mode = BinkEraserMode.Stroke, Radius = 12f };
    private void UseSelect() => _tool = new BinkSelectTool();
    private void UseArrow()  => _tool = new BinkShapeTool(BinkShapeKind.Arrow);
}
```

### Undo, redo and clearing

```csharp
_canvas.Undo();
_canvas.Redo();
_canvas.Clear();
_canvas.DeleteSelection();
```

`History` exposes `CanUndo`, `CanRedo`, `UndoLabel` and the stacks themselves if you want to show a history list.

Keyboard shortcuts work when the canvas has focus, which happens automatically when it is drawn on: Ctrl+Z,
Ctrl+Shift+Z or Ctrl+Y, Ctrl+A, Delete, Escape, Ctrl+D to duplicate, Ctrl+] and Ctrl+[ to change the stacking
order, and Ctrl with `0`, `9`, `+` or `-` for the viewport. Copy, cut and paste (Ctrl+C, Ctrl+X, Ctrl+V) join
them when you turn the clipboard on; see [clipboard, duplicating and arranging](#clipboard-duplicating-and-arranging).

### Validating a signature

Whether anything was drawn is the wrong question for a signature field: a dot is not a signature. Measure the ink
instead:

```csharp
BinkInkMetrics ink = BinkDocumentQuery.MeasureInk(_canvas.CurrentDocument);
bool acceptable = ink.Length > 120f && ink.StrokeCount >= 1;
// also ink.Duration (how long the pen was down) and ink.Bounds (how much of the field it covers)
```

Leave the clipboard off on a signature field, as it is by default, so nobody can paste a signature in.

### Saving and loading

```csharp
string json = BinkSerializer.Serialize(_canvas.CurrentDocument);
// ... store it anywhere: a database column, a file, a blob ...

BinkDocument document = BinkSerializer.Deserialize(json);
_canvas.LoadDocument(document);
```

For untrusted input use `TryDeserialize`, which reports a message instead of throwing.

`CurrentDocument.IsDirty` says whether anything changed since you last called `MarkSaved()`, for an "unsaved
changes" prompt. To keep a drawing safe across a reload or a crash, see [autosave and recovery](#autosave-and-recovery).

### Exporting

```csharp
string svg = await _canvas.ExportSvgAsync();
byte[] png = await _canvas.ExportPngAsync(new BinkPngExportOptions { Scale = 3f });
byte[] jpeg = await _canvas.ExportImageAsync(BinkImageFormat.Jpeg, quality: 0.85);

await _canvas.SaveSvgAsync("drawing.svg");
await _canvas.SavePngAsync("drawing.png");
```

### Working with the document directly

The document is a plain object. You can build, inspect and edit a drawing with no UI at all, which is what makes
the engine testable and usable on a server.

```csharp
using System.Numerics;
using Bit.Bink;

var document = new BinkDocument(new BinkDocumentSettings { Width = 400, Height = 300 });

var points = BinkStrokePoints.FromPositions([new Vector2(10, 10), new Vector2(200, 120)]);
document.AddElement(new BinkStroke(points, new BinkPenBrush { Width = 6f }));

Console.WriteLine(document.ContentBounds);
File.WriteAllText("drawing.svg", BinkSvgExporter.Export(document));
```

### Where to go next

- [Architecture](#architecture) explains how the pieces fit together.
- [Drawing, brushes and pressure](#drawing-brushes-and-pressure) covers stroke quality and stylus support.
- [Hosting models](#hosting-models) covers Blazor Server and Hybrid.

## Architecture

### The shape of the system

```text
Input               browser pointer events, batched once per animation frame
  |
Stroke recognition  BinkStrokeBuilder: spacing filter, streamline smoothing, pressure
  |
Stroke model        BinkStroke + BinkBrush, immutable, structure-of-arrays points
  |
Commands            BinkDrawingOperation, the unit of undo, diffing and collaboration
  |
Document            BinkDocument: layers of elements, settings, metadata
  |
Renderer            IBinkRenderContext: canvas commands, SVG, or the built-in rasterizer
```

Every layer is independent of the ones above it. Everything except `BinkCanvas` and the browser bridge is plain
.NET code that needs no browser and no Blazor renderer, and runs on a server, in a console application or in a unit
test.

### The document model

```text
BinkDocument
 |- Settings          page size and background
 |- Metadata          application key/value data, preserved by serialization
 |- Layers            ordered, each with a name, visibility, lock and opacity
      |- Elements     strokes, shapes, text, images, highlights, callouts
```

Elements are immutable records identified by a `Guid`. Editing one means creating a modified copy with `with` and
replacing it in the document. Point data and cached geometry are shared between copies, so a `with` on a
10,000-point stroke copies a handful of references, not the points.

This is what makes the rest of the system simple:

- **Undo** stores the inverse operation, never a copy of the document.
- **Collaboration** transmits the changed element, never the document.
- **Diffing** is a comparison of element ids and versions.

### Operations

Every mutation goes through `BinkDocument.Apply`, which returns the inverse:

```csharp
BinkDrawingOperation inverse = document.Apply(new BinkAddElementOperation(layerId, stroke));
```

The operation types are `BinkAddElementOperation`, `BinkRemoveElementOperation`, `BinkReplaceElementOperation`,
`BinkReorderElementOperation`, `BinkAddLayerOperation`, `BinkRemoveLayerOperation`, `BinkUpdateLayerOperation`,
`BinkReorderLayerOperation`, `BinkUpdateSettingsOperation`, `BinkSetMetadataOperation` and `BinkBatchOperation`.
A batch applies atomically: if one part fails, the already-applied parts are rolled back, so a document is never
left half-changed.

`BeginBatch` groups a burst of changes into one event and one undo step:

```csharp
using (document.BeginBatch("Import"))
{
    foreach (BinkStroke stroke in strokes) document.AddElement(stroke);
}
```

A scope collects changes that have already been applied, so an exception inside it leaves the successful ones in
place. Apply a `BinkBatchOperation` directly when the change must be all or nothing.

The operation set is deliberately closed. Custom behaviour composes the existing operations rather than adding
new ones, which keeps undo, serialization and synchronization correct for every editor built on the library.

### Undo and redo

`BinkHistoryManager` subscribes to the document and records the inverse of every local change as it happens. No
snapshots are taken. Memory is proportional to what changed, and the elements referenced by history share their
point data with the document.

Changes made with origin `BinkChangeOrigin.Remote` or `BinkChangeOrigin.Load` are not recorded: a collaborator's
edit belongs to their history, not yours. Undo and redo are re-broadcast to collaborators as ordinary edits,
because that is what they are.

`BeginGroup` merges a gesture's many operations, such as every frame of a drag, into a single undo step. Every
tool uses it for pointer gestures and for keyboard edits alike.

History keeps 200 steps by default (`Limit`, or `HistoryLimit` on the canvas). A step holds what it would put back,
so deleting a large embedded photo keeps the photo alive for undo; `MemoryBudget` (`HistoryMemoryBudget`) bounds the
estimated bytes all steps hold, dropping the oldest first and never the most recent. `EstimatedMemory` reports the
current estimate. It counts every element a step refers to, and the points, text or image each carries once however
many steps share it, as the steps of a drag share a stroke's points; data the document still holds is counted too,
so it errs high. The estimate is kept only while a budget is set, so history costs nothing extra without one.

#### Changes caused by other changes

An application often keeps derived geometry in sync: a diagram re-routes a connector when the node it is attached
to moves. It does that by handling `Changed` and applying more operations, and those operations must undo with
the change that caused them, not before it.

The document reports that relationship. A change applied while another change is being reported is nested inside
it, and its `BinkDocumentChangedEventArgs.Depth` is greater than zero. History holds those consequential entries
back until the whole notification has finished and then records them after their cause, so the result does not
depend on whether the application subscribed to `Changed` before or after the history manager did. The cause and
its consequences become one undo step, whether the cause came from a tool's gesture or a direct API call; only a
consequence of a change history does not record, such as a remote edit, is a step of its own.

The demo's diagram page is built on exactly this: moving a node and re-routing its four connectors is a single
undo, and it is covered by tests at both the model and browser level.

### Rendering

`IBinkRenderContext` is the only drawing surface the engine knows. Three implementations exist:

| Implementation | Used for |
| --- | --- |
| `BinkCanvasRenderContext` | Records a flat command buffer that the browser replays onto a 2D canvas. |
| `BinkSvgRenderContext` | Writes SVG markup. Paths stay paths. |
| `BinkRasterRenderContext` | Draws into a pixel buffer with the built-in anti-aliased rasterizer. |

`BinkDocumentRenderer.Render` walks the document once and calls the context. The canvas and the rasterizer both go
through it, so what is on screen and what a PNG contains cannot drift apart. SVG export walks the document
itself, because it has to turn eraser strokes into masks rather than replaying them as sequential compositing;
it draws every element through the same `IBinkRenderContext`, so element geometry is identical.

Layers that contain an eraser stroke, or that have opacity below 1, are drawn inside an isolated group so erasing
cannot reach the layers below. On canvas that is a scratch bitmap; in SVG it is a group with `isolation: isolate`
whose erasers become masks; in the rasterizer it is a scratch surface shared by every isolated layer, composited
and cleared only where the layer drew. A layer that is not isolated is not isolated in any of the three, so a
multiply highlight blends with the layers below it everywhere.

### The browser bridge

`bit-bink.js` contains no stroke model, no smoothing and no geometry. It does five things:

1. Collects pointer samples, including coalesced ones, and hands them to .NET once per animation frame.
2. Forwards keyboard and wheel input on the same path, so neither ever triggers a Blazor render.
3. Replays a command buffer of numbers onto a 2D canvas, keeping the paths and long strings .NET asks it to keep.
4. Moves clipboard content between the system clipboard and .NET.
5. Reports capabilities, sizes and encoded image data.

It draws exactly one thing itself: the provisional trail described in [hosting models](#hosting-models), a
constant-width polyline used under Blazor Server to hide the round trip. It carries no stroke model and is
replaced by the engine's geometry as soon as .NET responds.

In WebAssembly the command buffer reaches JavaScript through `[JSImport]` as a view over managed memory, so a
redraw is one call with no serialization. Everywhere else the same buffer travels through `IJSRuntime` as a byte
array of 32-bit floats, which Blazor sends as binary.

The document canvas's buffer refers to paths the browser already holds. A path is defined once, kept as a
`Path2D` under a slot number .NET chose, and later frames name the slot; .NET decides when a slot is released and
tells the browser in the same ordered stream, so the two sides cannot disagree about what exists. Should the
browser ever meet a slot it does not hold, it says so and the next frame defines everything again.

### Two canvases

The component owns a **document canvas** holding committed elements and an **overlay canvas** holding the live
stroke, selection handles and tool previews.

While a stroke is being drawn, only the overlay is redrawn. The thousands of strokes already in the document are
not touched until the pen lifts. This is the single most important reason the canvas stays responsive on a large
document, and it is why pointer input never triggers a Blazor render.

### Coordinate spaces

| Space | Meaning |
| --- | --- |
| Document | The units elements are stored in. Never changes when the user pans or zooms. |
| View | CSS pixels relative to the canvas element. `view = document * Zoom + Offset`. |
| Device | Physical canvas pixels, `view * devicePixelRatio`. Handled by the renderer. |

`BinkViewport` is an immutable value; panning and zooming produce a new one, so vector data is never rewritten by a
change of view.

### Extension points

- `BinkBrush` is an abstract record. Derive from it, implement `BuildGeometry`, register the kind with
  `BinkBrushRegistry`.
- `IBinkTool` decides what pointer input does. Implement it for a custom interaction.
- `IBinkRenderContext` lets you render a document to any surface.
- `IBinkTextRasterizer` supplies a font engine for server-side PNG export.
- `IBinkCollaborationTransport` moves operations between participants over any transport.
- `BinkElement` is an abstract record; `BinkElementRegistry` saves, pastes and synchronizes a custom element type.

See [Extending Bink](#extending-bink).

## Drawing, brushes and pressure

### How a stroke is made

A pointer event does not become a point directly. Between the two sits `BinkStrokeBuilder`, which does four
things, all without allocating:

1. **Spacing filter.** A sample that would land closer than `Brush.Spacing * Brush.Width` to the previous stored
   point, with a small floor so a zero spacing still filters duplicates, is merged into it and their pressure
   averaged. This removes sensor jitter and keeps documents small: a 240 Hz pen produces far more samples than the
   shape needs.
2. **Streamline smoothing.** Each stored position is pulled toward the previous one by `Brush.Smoothing`. This is
   an exponential filter, so it removes hand tremor without adding lag on straight sections.
3. **Pressure.** Real pen pressure is lightly smoothed. When the device has none, and the brush asks for it,
   pressure is derived from drawing speed so fast movements thin out like a real pen.
4. **Timestamps** are recorded relative to the start of the stroke, which is what makes velocity available later.

When the pointer lifts, the last stored point is moved to the exact lift position, so a stroke ends where the pen
did rather than at the last filtered sample.

### Turning points into geometry

`BinkStrokeOutliner` builds a closed, variable-width outline polygon around the points:

- The radius at each point is `Width/2 * PressureCurve(pressure) * taper`.
- Left and right offsets are placed along the perpendicular of the averaged neighbouring directions, with a
  bounded miter correction so the visual width stays constant through gentle corners.
- At sharp corners, where the direction changes by 90 degrees or more, an arc of points is inserted so the
  outline does not pinch.
- Round or flat caps close both ends.
- The polygon is emitted as a smooth quadratic path through segment midpoints.

The result is filled with the non-zero rule, so a tight loop that folds over itself stays solid instead of
punching a hole.

Constant-width brushes such as the marker and highlighter skip all of that and emit a smooth centerline
(Catmull-Rom converted to cubic Béziers) to be stroked at a fixed width. That produces far less geometry.

Geometry is cached per (points, brush) pair on the stroke, so it is built once and reused by every redraw and
every export.

### The built-in brushes

| Brush | Behaviour |
| --- | --- |
| `BinkPenBrush` | Pressure-sensitive variable width, round caps, slight taper at the end. The default. |
| `BinkPencilBrush` | Thin, slightly translucent, strong pressure response, tighter point spacing. |
| `BinkMarkerBrush` | Constant width, round caps, opaque. Rendered as a centerline. |
| `BinkHighlighterBrush` | Wide, translucent, flat caps, multiply blending so text underneath stays legible. |
| `BinkCalligraphyBrush` | A broad nib at a fixed angle (`NibAngle`, `NibThickness`): thick across the nib, thin along it, like an italic pen or a chisel marker. |
| `BinkPixelEraserBrush` | Erases what is beneath it within its layer, as a real element that can be undone. |

### Configuring a brush

```csharp
var pen = new BinkPenBrush
{
    Color = BinkColor.Parse("#1d4ed8"),
    Width = 6f,                  // maximum width, reached at full pressure
    Opacity = 1f,                // applied to the stroke as one unit, so self-overlaps do not darken
    Smoothing = 0.5f,            // 0 keeps every wobble, 0.9 is very smooth and slightly rounds fast corners
    Spacing = 0.25f,             // minimum point spacing as a fraction of Width
    SimulatePressure = true,     // derive width from speed when the device has no pressure
    TaperStart = 0f,
    TaperEnd = 6f,
    PressureCurve = BinkPressureCurve.Pen,
};
```

For handwriting, prefer less smoothing and closer spacing so small letters keep their shape:

```csharp
var writing = new BinkPenBrush { Width = 3f, Smoothing = 0.35f, Spacing = 0.12f, TaperEnd = 5f };
```

### Pressure curves

A `BinkPressureCurve` maps input pressure (0 to 1) to a width factor between `MinFactor` and `MaxFactor`.

```csharp
BinkPressureCurve.Pen        // S-curve from 20% to 100%: the default pen response
BinkPressureCurve.Linear     // linear from 20% to 100%
BinkPressureCurve.Constant   // ignores pressure entirely

new BinkPressureCurve(BinkPressureCurveKind.EaseIn, minFactor: 0.1f, maxFactor: 1f)
BinkPressureCurve.FromSamples([0f, 0.1f, 0.45f, 0.8f, 1f])   // a custom response curve
```

`BinkPressureCurve.Pen` puts a normal touch (pressure 0.5) at about 60% of the nominal width, so `Width` behaves
the way a user expects while still leaving room to press harder.

### Devices without pressure

A mouse reports a constant 0.5 while a button is down; most touch screens report the same. When
`SimulatePressure` is on, the builder derives pressure from drawing speed instead: slower means heavier, up to a
capped maximum. The result is a line that thins out on quick flicks, which reads as natural even without
hardware support.

`BinkStroke.Input` records which device drew the stroke and whether the pressure was real, so an application can
tell the two apart after the fact.

### Reading device capabilities

```razor
<BinkCanvas @ref="_canvas" Ready="OnReady" />

@code {
    private void OnReady(BinkInputCapabilities capabilities)
    {
        // capabilities.CoalescedEvents, .Tilt, .Twist, .MaxTouchPoints, .DevicePixelRatio, .LowLatencyCanvas
    }
}
```

The canvas's `Capabilities` also gains `ObservedPen`, `ObservedTouch`, `ObservedPressure` and `ObservedTilt` as
input arrives, which is the only reliable way to know whether a real stylus is in use: feature detection tells you
the browser supports tilt, not that the device produces it.

### Tilt and twist

Tilt and barrel rotation are captured into the stroke when the device reports them, and preserved by
serialization. The built-in brushes do not shape ink from tilt, because a convincing chisel nib needs a brush
model of its own. The data is there for a custom brush to use; see [Extending Bink](#extending-bink).

### Simplification

When a stroke is committed, `BinkDrawTool` simplifies it:

```csharp
var tool = new BinkDrawTool { SimplifyTolerance = 0.12f };   // document units; 0 disables it
```

Simplification runs on position with the Ramer-Douglas-Peucker algorithm and, separately, on the pressure
channel (`SimplifyPressureTolerance`). Position-only simplification would collapse a straight line drawn with
changing pressure into two points and throw the width variation away, so pressure is treated as its own channel
with its own tolerance.

### Quality checklist

The stroke pipeline is covered by tests that assert behaviour rather than pixels:

- A constant-pressure stroke has a constant measured width.
- Pressure changes the measured width.
- A sharp corner does not produce a spike beyond the stroke radius.
- Smoothing reduces jitter without distorting the overall path.
- Fast movement produces lighter ink than slow movement when pressure is simulated.
- A single tap leaves a visible dot.
- Duplicate and degenerate points never produce non-finite geometry.

## Erasing

Three erasing strategies exist because they are genuinely different, not three names for one behaviour. Pick the
one that matches the application.

```csharp
var eraser = new BinkEraserTool
{
    Mode = BinkEraserMode.Stroke,   // Stroke, Segment or Pixel
    Radius = 12f,                   // document units
};
```

### Stroke eraser

`BinkEraserMode.Stroke` deletes any stroke the eraser touches, whole.

This is what note-taking applications do, and what most users expect: a quick swipe removes a letter. It is cheap,
exactly reversible, and never increases the element count. It cannot remove part of a long stroke, which is
occasionally surprising if someone drew a whole line without lifting the pen.

### Segment eraser

`BinkEraserMode.Segment` removes only the touched portion, splitting the stroke into fragments.

This feels like a real eraser and keeps the result fully vector. The first fragment keeps the original element id,
so undo and collaboration see an edit rather than a delete followed by two inserts. The cost is that erasing
through the middle of a stroke increases the element count.

The cut is computed where the eraser's circle meets the ink, in document space, so it lands at the eraser's edge
however far apart the stroke's points are (a simplified straight line may be two points hundreds of units apart)
and however the stroke has been stretched. A fragment keeps the original points it spans plus one new point at
each cut, and a crumb shorter than the ink is wide is dropped rather than left behind as a dot.

```csharp
var fragments = new List<BinkStroke>();
bool changed = BinkStrokeEditor.EraseSegments(stroke, eraserPath, radius, fragments);
```

`BinkStrokeEditor` is public, so an application can implement its own erasing behaviour on the same geometry.

### Pixel eraser

`BinkEraserMode.Pixel` draws an eraser stroke that masks out everything below it within the same layer.

Nothing is deleted. The erase is itself an object in the document: it can be undone, re-ordered, or removed later
to bring the ink back. It is the only mode that can erase part of an image, and it is the only mode that produces
the soft, partial erasure people expect from a raster tool. It exports as an SVG mask, so the result is preserved
in a vector file.

The trade-off is that an erased document keeps the erased ink, which matters if the point of erasing was to
remove information rather than to hide it.

### Which to choose

| Situation | Mode |
| --- | --- |
| Note taking, handwriting | `Stroke` |
| Drawing and sketching | `Segment` |
| Marking up an image or a photo | `Pixel` |
| The erased content must actually be gone | `Stroke` or `Segment` |

### The pen's eraser end

A stylus with an inverted eraser end reports pointer button 32. `BinkDrawTool` honours it by default, switching to
a pixel eraser for the duration of that stroke:

```csharp
var tool = new BinkDrawTool { HonorPenEraserButton = false };   // to opt out
```

### Erasing and undo

A whole erase gesture is one undo step, however many strokes it touched. In stroke and segment mode the tool
opens a history group on pointer down and closes it on pointer up, and erasing nothing creates no entry at all.
Pixel mode needs no group: the gesture produces a single eraser stroke, which is already one step.

### Erasing and layers

Erasing only affects unlocked, visible layers. A pixel eraser cannot reach past its own layer, which is why the
background and any locked image layer are safe from it.

## Selection, transforms and layers

### The selection tool

```csharp
var tool = new BinkSelectTool();
```

Click to select, drag inside the selection to move it, drag a corner or edge handle to resize, drag the circle
above it to rotate (Shift turns in 15 degree steps). Dragging on empty space draws a marquee. Shift-click,
Ctrl-click or right-click adds to the selection. Alt-dragging an element moves a copy of the selection and leaves
the original where it was; the copy and the move are one undo step, and Escape takes the copy away again.

Keyboard, when the canvas has focus:

| Key | Action |
| --- | --- |
| Arrow keys | Nudge by one document unit |
| Shift + arrows | Nudge by ten |
| Delete or Backspace | Delete the selection |
| Ctrl+A | Select everything on unlocked, visible layers |
| Ctrl+D | Duplicate the selection |
| Ctrl+] / Ctrl+[ | Bring forward / send backward |
| Ctrl+Shift+] / Ctrl+Shift+[ | Bring to front / send to back |
| Ctrl+G / Ctrl+Shift+G | Group / ungroup the selection |
| Ctrl+Shift+L | Lock the selection |
| Shift+H / Shift+V | Flip horizontally / vertically |
| Escape | Cancel the drag, then leave the group being worked inside, then clear the selection |

### Lasso selection

A rectangle around one handwritten word also catches the lines above and below it. A lasso does not:

```csharp
var tool = new BinkSelectTool { MarqueeShape = BinkMarqueeShape.Lasso, LassoCoverage = 0.5f };
```

Holding Alt when the drag starts uses the other shape, whichever is the default. A stroke is selected when at least
`LassoCoverage` of its points lie inside the loop, so a loose loop picks up the word without the strokes it
merely brushes; other elements are judged by their corners and centre the same way. The query is public:

```csharp
List<BinkElement> enclosed = BinkDocumentQuery.FindInPolygon(document, lassoPoints, coverage: 0.5f);
```

### Snapping

```razor
<BinkCanvas Snapping="new BinkSnapOptions { GridSize = 20f }" />
```

With snapping on, moving and resizing a selection and drawing a shape line edges and centres up with other
elements and with the page, showing a guide while they are lined up, and fall back to the grid when nothing is
within `Threshold` screen pixels. `ToObjects`, `ToPage` and `GridSize` turn each source on or off; `null`, the
default, snaps to nothing. Lining up with an element wins over the grid when both are in reach.

To show the grid it snaps to:

```razor
<BinkCanvas Grid="BinkGrid.DottedDefault" Snapping="new BinkSnapOptions { GridSize = 20f }" />
<BinkCanvas Grid="new BinkGrid { Style = BinkGridStyle.Lines, Spacing = 10f, MajorEvery = 10 }" />
```

The grid is part of the view, not of the document: it is never exported, saved or sent to collaborators. Zoomed
out until its cells would be a few pixels across, it draws coarser multiples instead, and a dotted grid costs one
dashed line a row rather than a shape a dot, so it stays cheap at any size.

The snap targets are collected once when a gesture starts, from what is visible, and sorted, so a frame of
dragging is a couple of binary searches however large the document is. `BinkSnapper` is public for a custom tool
that wants the same behaviour, and a custom tool reads the canvas's options from `IBinkToolContext.Snapping`.

### Clipboard, duplicating and arranging

```razor
<BinkCanvas EnableClipboard="true" />
```

```csharp
await canvas.CopySelectionAsync();
await canvas.CutSelectionAsync();
await canvas.PasteAsync();                  // for a toolbar button; Ctrl+V reaches the canvas by itself
canvas.DuplicateSelection();
canvas.ArrangeSelection(BinkArrangement.BringToFront);   // BringForward, SendBackward, SendToBack
```

Copied elements go to the system clipboard as a small Bink document, so they paste into another canvas, another
tab or another application built on Bink, and a saved drawing pasted as text pastes its elements. A PNG picture of
the selection goes beside it, on the document's background at twice its size, so the same copy pastes into a chat,
an email or a document; pasted back into a canvas, the drawing wins over its picture. Pasted elements
get new ids, land on the active layer, step away from the original when the same copy is pasted twice, land in
the middle of the view when they would otherwise be off screen, and are selected. A pasted image becomes an image
element. Plain text becomes a text element, unless you handle `TextPasted` yourself.

The clipboard is off by default. A signature field that accepted a pasted image, or pasted ink, would no longer
prove that anyone signed. The methods above work regardless, so an application can offer clipboard buttons
without turning on the shortcuts.

How it works in the browser, for the curious: writing the clipboard is only allowed while handling the user's key
press, and the selection is serialized in .NET, which on Blazor Server is a round trip away. The browser layer
therefore creates the clipboard item inside the key event with a promise for its content, and .NET fulfils it.
Pasting reads the browser's paste event, which needs no permission prompt; large content travels to .NET as a
stream, because a Blazor Server circuit refuses any single message above 32 KB. If the browser refuses the write,
the canvas keeps its own copy and the next paste uses it.

A copied group pastes as a group of its own, so pasting twice gives two independent groups.

Files dragged onto the canvas, from the desktop or another page, are added where they land once you turn it on:
images (raster types only, as for a paste), and saved drawings (`.bink` or `.json`), whose elements are pasted
centred on the drop point. Like the clipboard it is off by default, so a signature field cannot be signed by
dropping a picture on it. Every file travels to .NET as a stream, so its size is no problem on Blazor Server.

```razor
<BinkCanvas EnableClipboard="true" EnableDrop="true" />
```

Duplicating and arranging are document operations too, for a headless editor:

```csharp
IReadOnlyList<BinkElement> copies = document.DuplicateElements(ids, offset: new Vector2(16, 16));
document.Arrange(ids, BinkArrangement.SendBackward);
string payload = BinkClipboard.Serialize(elements);
BinkClipboard.TryDeserialize(payload, out IReadOnlyList<BinkElement> pasted, out string? error);
```

Arranging moves elements within their own layer and keeps their order relative to each other; bringing one stroke
to the front of a thousand is one operation, not a thousand.

The complete keyboard map, including resizing, rotating and stepping through elements, is in
[accessibility](#keyboard).

### Grouping

```csharp
Guid? group = canvas.GroupSelection();      // Ctrl+G
canvas.UngroupSelection();                  // Ctrl+Shift+G

Guid? id = document.GroupElements(ids);     // the same, headless
document.UngroupElements(ids);
```

Clicking any member of a group selects the whole group, and so does a marquee or lasso that catches part of it;
moving, resizing, rotating, copying and deleting then act on all of it. Double-click a member to work inside the
group: the next level down, a nested group or a single element, can then be picked on its own. Escape, or a click
outside the group, leaves it again. Tab steps through a group as one stop.

A group is not an element. Each element lists the groups it belongs to, innermost first, in `GroupIds`, so grouping
and ungrouping are ordinary edits that undo, save, paste and synchronize like any other. Members keep their layers;
within each layer they are brought together in the stacking order, just below the top-most of them.

Groups nest. Grouping elements that already make up whole groups puts those groups inside the new one, and
grouping part of a group while working inside it nests the new group there. Ungrouping takes a group apart one
level, leaving the groups inside it intact. `BinkDocumentQuery.ExpandToGroups` and `GetSelectionGroup` apply the
same rules for a custom tool.

### Locking

```csharp
canvas.LockSelection();                       // Ctrl+Shift+L
canvas.UnlockAll();
document.SetLocked(ids, locked: false);       // for particular elements
```

A locked element is drawn and exported as usual, but no tool can select, move or erase it, so a background image
or a template stays put while everything else on its layer is edited. Locks protect against tools, not against
code: the document API still edits locked elements, which is how an application unlocks them.
`BinkDocumentQuery.HitTest(document, point, includeLocked: true)` finds one under the pointer, for a context menu
that offers to unlock it. The pixel eraser masks what is below it, locked or not, exactly as it does on its layer.

### Aligning, distributing and flipping

```csharp
canvas.AlignSelection(BinkAlignment.Left);                                     // to the selection's own bounds
canvas.AlignSelection(BinkAlignment.HorizontalCenter, document.Settings.PageRect);  // to the page
canvas.DistributeSelection(BinkDistribution.Horizontal);
canvas.FlipSelection(BinkFlipDirection.Horizontal);                            // Shift+H; Shift+V for vertical
```

Groups take part as a whole. Aligning without a reference lines the selection up on its own bounds and needs at
least two separate things; with one, such as the page, a single element is aligned to it. Distributing keeps the
two outermost in place and spaces the rest between them with equal gaps, in the order of their centres; it needs
three. Flipping mirrors the selection about its centre through each element's transform, so ink is never
rewritten and flipping twice restores it exactly. Text moves to its mirrored place without its letters being
reversed, and a callout's box and pointer swap sides. Each command is one undo step, and the same commands are on
`BinkDocument` for a headless editor.

### Why manipulation is cheap

Moving, resizing and rotating change only each element's `Transform`. Stroke points, text and image pixels are
never rewritten. That has three consequences worth knowing:

- A drag of 10,000 selected strokes costs one matrix per stroke, not a rewrite of every point.
- Undo restores the exact original transform, with no accumulated floating-point drift.
- Scaling a selection scales the ink with it, because the transform is applied when the element is drawn. The
  stored `Brush.Width` is left alone until you bake the transform in with `BinkStroke.Flatten()`, which folds the
  scale into the width and resets the transform.

The whole gesture is a single undo step.

### Working with the selection in code

```csharp
BinkSelection selection = canvas.Selection;

selection.Set(elementId);
selection.Add(otherId);
selection.Toggle(thirdId);
selection.SelectAll();
selection.Clear();

BinkRect bounds = selection.Bounds;
foreach (BinkElement element in selection.Elements) { }
```

The selection drops elements automatically when they are removed from the document, including by a collaborator.

### Querying the document

```csharp
BinkElement? hit = BinkDocumentQuery.HitTest(document, point, tolerance: 4f);
List<BinkElement> all = BinkDocumentQuery.HitTestAll(document, point);
List<BinkElement> inside = BinkDocumentQuery.FindInRect(document, rect, BinkRectSelectionMode.Contain);
List<BinkElement> lassoed = BinkDocumentQuery.FindInPolygon(document, lassoPoints, coverage: 0.5f);
BinkInkMetrics ink = BinkDocumentQuery.MeasureInk(document);
```

Hit testing is geometric for strokes and shapes: a diagonal stroke is not selected by a marquee that only
overlaps its bounding box, and clicking inside the empty middle of an unfilled rectangle does not select it. It is
measured in document units even when an element was stretched by an edge handle: a stroke stretched sideways is
fatter across than along, and the hit tolerance is added to the ink as it is drawn rather than converted by an
average scale.
Text, images and highlights are rectangular by nature and are tested against their bounds.
`BinkRectSelectionMode.Contain` compares bounding boxes, because "entirely inside" is a question about extent.

### Layers

Layers are part of the document model, not separate HTML canvases. That means they survive saving, take part in
undo, and are honoured identically by the screen, SVG export and PNG export.

```csharp
BinkLayer annotations = document.AddLayer("Annotations");

document.UpdateLayer(annotations.Id, p => p with { IsVisible = false });
document.UpdateLayer(annotations.Id, p => p with { IsLocked = true });
document.UpdateLayer(annotations.Id, p => p with { Opacity = 0.5f });
document.UpdateLayer(annotations.Id, p => p with { Name = "Review notes" });

document.UpdateLayer(annotations.Id, p => p with { BlendMode = BinkBlendMode.Multiply });

document.ReorderLayer(annotations.Id, index: 0);   // 0 is the bottom
BinkLayer copy = document.DuplicateLayer(annotations.Id);   // directly above, with copies of its elements
document.RemoveLayer(annotations.Id);              // a document always keeps at least one layer
```

A layer's blend mode decides how the finished layer combines with everything below it. `Multiply` darkens like
ink on paper, which is what a layer of markup over a scanned page wants: the page shows through the strokes rather
than behind them. `Screen` lightens. The layer is composited as one unit, so its own strokes overlap normally
among themselves, and the result is the same on screen, in PNG export and in SVG, where it is `mix-blend-mode`.

Tell the canvas where new elements go:

```razor
<BinkCanvas ActiveLayerId="_annotationLayer" />
```

A locked or hidden layer rejects drawing, erasing and selection. That is how the image annotation demo keeps a
picture from being moved by accident: the image sits on its own locked layer. Locking or hiding a layer also drops
its elements from the selection, so the next Delete or arrow key cannot reach them.

While the layer named in `ActiveLayerId` is locked or hidden, nothing new goes on it: strokes, shapes, text, pasted,
dropped and imported content and `AddImageAsync` are all refused, and the canvas announces why. They are never put
on another layer instead, which would leave them somewhere nobody chose, and on a hidden layer they would be
invisible. A layer locked or hidden while a stroke or a shape is being drawn refuses it when it ends. If your UI lets
people lock or hide the layer they draw on, say so beside it, as the whiteboard demo does. Without `ActiveLayerId`,
new elements go on the top-most layer that is neither locked nor hidden.

### A typical structure

```text
Foreground     stickers, watermarks
Annotations    arrows, highlights, callouts
Drawing        freehand ink
Images         the photo or screenshot being marked up  (locked)
Background     paper colour, grid
```

## Annotations, text and images

### Shapes

```csharp
document.AddElement(new BinkRectangleShape(new BinkRect(20, 20, 200, 120))
{
    Style = new BinkShapeStyle { StrokeColor = BinkColor.Parse("#dc2626"), StrokeWidth = 3f },
    CornerRadius = 6f,
});

document.AddElement(new BinkEllipseShape(new BinkRect(60, 60, 120, 120)));

document.AddElement(new BinkLineShape(new Vector2(20, 20), new Vector2(200, 90))
{
    EndArrow = true,
    Style = new BinkShapeStyle { StrokeColor = BinkColor.Parse("#2563eb"), StrokeWidth = 2f },
});
```

`BinkShapeStyle` carries stroke colour and width, an optional fill, opacity, a dash pattern, a blend mode, and the
line caps and joins (round by default; `LineJoin = BinkLineJoin.Miter` keeps corners sharp).

Interactively, `BinkShapeTool` draws them:

```csharp
var tool = new BinkShapeTool(BinkShapeKind.Arrow);
// Rectangle, Ellipse, Line, Arrow, Highlight, Diamond, Triangle, Polygon (PolygonSides) and Star (StarPoints)
```

Holding Shift while dragging constrains to a square, a circle or a 15-degree angle. The modifier travels with the
pointer samples, so it works with a pen and a touch keyboard as well as a mouse.

### Polygons, polylines and paths

`BinkPolygonShape` is any outline through a list of points: closed and fillable, or an open polyline, straight or
smoothed into a curve through its points, with arrow heads at the ends of an open one. That last is an elbow
connector, the thing a flowchart joins its boxes with.

```csharp
document.AddElement(BinkPolygonShape.Diamond(new BinkRect(100, 100, 120, 80)));   // also Triangle, Regular, Star
document.AddElement(new BinkPolygonShape([new(0, 0), new(80, 0), new(80, 60)], isClosed: false) { EndArrow = true });
```

`BinkPolylineTool` draws one point by point. Each click adds a point while the next segment follows the pointer (a
finger or pen is dragged to the spot and lifted instead); a double click or Enter finishes, clicking the first
point again closes the outline, Backspace takes the last point back, Escape abandons it, and Shift keeps segments
to 15-degree steps. It is the one built-in tool that asks for hover movement, through `IBinkTool.WantsHover`.

`BinkPathElement` holds any vector path: lines, quadratic and cubic curves in any number of sub-paths, with a
shape style and a fill rule. It is what an icon, a sticker or a logo becomes. It reads SVG path data:

```csharp
var heart = BinkPathElement.FromSvgPathData("M50 30 C50 0 0 0 0 30 C0 60 50 80 50 100 C50 80 100 60 100 30 C100 0 50 0 50 30 Z")
{
    Style = new BinkShapeStyle { FillColor = BinkColor.Parse("#e11d48"), StrokeWidth = 0f },
};
```

`BinkSvgPathData` is the parser on its own. It understands every command, absolute and relative, turns arcs into
cubic curves, and bounds what it reads, since path data is often untrusted. Both elements save, paste, export and
synchronize like every other; a path is stored as SVG path data.

### Highlights

A highlight is one or more rectangles drawn with multiply blending, so whatever is underneath stays legible. It is
the right element for marking text, because a text line is a rectangle.

```csharp
document.AddElement(new BinkHighlightElement(lineRects)
{
    Color = BinkColor.Parse("#fde047"),
    Opacity = 0.5f,
    Description = "Highlighted: payment terms",
});
```

### Callouts

A callout is a text box with a pointer to an anchor. This is the shape a review comment takes.

```csharp
document.AddElement(new BinkCalloutElement(
    box: new BinkRect(320, 100, 200, 70),
    anchor: new Vector2(280, 130),
    text: "Please confirm this clause.")
{
    Style = new BinkShapeStyle { FillColor = BinkColor.Parse("#fff9c4"), StrokeColor = BinkColor.Parse("#785a00") },
});
```

The pointer starts at whichever edge of the box faces the anchor, so it stays sensible when the callout is moved.

### Text

Text is laid out by the browser, so a text element records an explicit size. `BinkTextTool` creates one and asks
the canvas to place a real HTML `textarea` over it, because an accessible, IME-capable text editor cannot be built
on a canvas. The canvas does that on its own:

```razor
<BinkCanvas Tool="_textTool" />

@code {
    private BinkTextTool _textTool = new()
    {
        Style = BinkTextStyle.Default with { FontSize = 20f },
    };
}
```

Enter commits the text (Shift+Enter starts a new line), Escape cancels, and the browser measures the committed
text so the element's bounds are exact. Committing empty text removes the element, so an abandoned text box never
leaves an invisible object behind. The edit is applied to the element as it is when it is committed, so a
collaborator who moved or restyled it meanwhile keeps their change, and committing without a measured size keeps
the size the element already had. `EditRequested` and `EditFinished` are public events on the tool, for an
application that wants to react when editing starts and ends.

Created directly:

```csharp
document.AddElement(new BinkTextElement(new Vector2(40, 40), "Approved")
{
    Style = BinkTextStyle.Default with { FontSize = 20f, Bold = true, Color = BinkColor.Parse("#15803d") },
    Background = BinkColor.Parse("#dcfce7"),
    Padding = 6f,
});
```

When no measured size is supplied, the element estimates one from the font size. The estimate is only used for
bounds; the browser still lays the text out for real when drawing.

### Images

```csharp
var source = BinkImageSource.FromBytes(pngBytes, "image/png", width: 640, height: 400);
BinkImageElement? image = await canvas.AddImageAsync(source, position: null, maxWidth: 800f);
// null when the canvas is read-only; it is already in the document otherwise
```

`AddImageAsync` asks the browser for the natural size when it is unknown, scales the image to `maxWidth`, and
centres it in the view when no position is given. To place an image inside a history group or batch, decode it
first with `LoadImageAsync(source)`, which returns the source with its natural size without adding anything, and
add the element synchronously inside the group: a group left open across the browser round trip disables undo
until it closes.

Directly, without a canvas:

```csharp
document.AddElement(new BinkImageElement(source, new BinkRect(0, 0, 640, 400))
{
    Crop = new BinkRect(100, 50, 400, 300),   // in source pixels
    Opacity = 0.9f,
    Description = "Screenshot of the checkout page",
});
```

Position, size, rotation (through `Transform`) and cropping are all vector properties. Moving or resizing an image
never re-encodes pixels, and cropping is non-destructive: it can be changed or removed later.

#### A pattern that works

Put the image on its own layer and lock it:

```csharp
Guid imageLayer = document.Layers[0].Id;
document.AddElement(new BinkImageElement(source, new BinkRect(0, 0, 640, 400)), imageLayer);
document.UpdateLayer(imageLayer, p => p with { IsLocked = true, Name = "Image" });

Guid annotations = document.AddLayer("Annotations").Id;
```

Annotating then cannot move the picture by accident, and hiding the image layer shows the annotations alone.

#### Sources and safety

`BinkImageSource` accepts `data:`, `https:`, `http:` and `blob:` URIs. Data URIs must carry a raster media type;
SVG is rejected unless explicitly allowed, because an SVG image is a document that can reference other resources.

```csharp
if (!BinkImageSource.TryCreate(uri, width, height, allowSvg: false, out BinkImageSource? source))
{
    // refuse it
}
```

Embedded images are stored in the document as data URIs, which keeps it self-contained. For large assets, keep the
bytes elsewhere and reference them by URL, or save with `IncludeEmbeddedImages = false`. An image saved that way
loads with `BinkImageSource.Omitted` as its source: it keeps its rectangle, crop, opacity and metadata but draws
nothing, and the application re-attaches the payload by replacing the element.

```csharp
foreach (BinkImageElement image in document.AllElements.OfType<BinkImageElement>())
{
    if (image.Source.IsOmitted) document.ReplaceElement(image with { Source = LoadStoredImage(image.Id) });
}
```

### What Bink deliberately is not

There is no filter stack, no colour adjustment, no compositing pipeline for pixels. Images are placed, cropped and
annotated. Turning the library into an image editor would cost every consumer size and complexity for something
few of them want.

## Serialization

### The API

```csharp
string json = BinkSerializer.Serialize(document);
BinkDocument document = BinkSerializer.Deserialize(json);

byte[] utf8 = BinkSerializer.SerializeToUtf8Bytes(document);
await BinkSerializer.SerializeAsync(document, stream);
BinkDocument loaded = await BinkSerializer.DeserializeAsync(stream);

if (!BinkSerializer.TryDeserialize(json, out BinkDocument? d, out string? error))
{
    // report `error` to the user instead of throwing
}
```

### The format

Versioned JSON with compact binary stroke points:

```json
{
  "format": "bink",
  "version": 1,
  "id": "0b3f...",
  "settings": { "width": 800, "height": 600, "background": "#ffffff" },
  "layers": [
    {
      "id": "9c21...",
      "name": "Drawing",
      "elements": [
        {
          "type": "stroke",
          "id": "4ad7...",
          "brush": { "kind": "pen", "color": "#111827", "width": 4 },
          "input": { "device": "pen", "pressure": true },
          "points": { "count": 142, "encoding": "b64", "data": "AQBAjgE..." }
        }
      ]
    }
  ]
}
```

It is a stable contract, not a projection of the library's classes. Every element carries a `type` discriminator
and only documented fields. Besides its own fields, any element may carry `transform` (six numbers), `description`,
`metadata`, `locked: true` and `groups`, its group ids innermost first. Each of these is left out when it has its
default value, and a reader that predates one ignores it, so an older build opens a grouped drawing ungrouped
rather than refusing it.

### Images shown more than once

An embedded picture shown by several elements (duplicated, pasted twice, a stamp used across a page) is written
once, in an `assets` table keyed by a hash of its content, and each of those elements refers to it:

```json
{
  "format": "bink",
  "version": 2,
  "assets": { "3f9a0c...": "data:image/png;base64,iVBORw0..." },
  "layers": [ { "elements": [
    { "type": "image", "rect": [0, 0, 640, 480], "asset": "3f9a0c..." },
    { "type": "image", "rect": [700, 0, 640, 480], "asset": "3f9a0c..." }
  ] } ]
}
```

On load the elements share one image object again, so a picture shown ten times is held in memory once. A file
with an asset table is version 2, which earlier versions of Bink refuse to open with a "newer version" message
rather than opening it without its pictures; a file whose pictures are each shown once has no table and is still
version 1, readable by every version. `BinkSerializerOptions.ShareImageData = false` always writes version 1, for
documents that must open in an older build. The table is read as untrusted input like the rest: an entry that is
not an embedded raster picture is left out, and the elements that refer to it are dropped, as an element with an
unacceptable `src` is.

### How points are stored

`BinkPointCodec` quantizes coordinates to a fixed grid (64 steps per document unit by default) and delta-encodes
them as zigzag varints. Pressure is one byte, time is a varint delta in milliseconds, and tilt and twist are only
present when the device produced them.

A handwriting point costs about 3 to 6 bytes depending on how fast the pen was moving, against 30 or more for a
JSON number array. The encoding is deterministic: the same points always produce the same bytes, so documents can
be compared and diffed.

Times are kept within 1,000,000,000 ms either side of the stroke start and tilt within 90 degrees, on write and on
read, so a point that loads can always be saved and sent again. A stroke too large for the requested
`Quantization` is written on the finest coarser grid that fits; the grid is stored with the stroke.

For a readable file, ask for JSON points:

```csharp
string readable = BinkSerializer.Serialize(document, BinkSerializerOptions.Readable);
```

### Compatibility

- **Version checks.** A document written by a newer schema version is refused with a message naming the problem,
  rather than being partially read.
- **Unknown elements survive.** An element whose `type` this build does not recognise is preserved verbatim
  through a load and save, so an older build never silently destroys work made by a newer one. Set
  `PreserveUnknownElements = false` to drop them instead.
- **Unknown brushes survive.** A brush kind that is not registered draws like a pen with the stored colour, width,
  opacity and pressure curve, and keeps its kind, so saving the document or editing the stroke writes it back
  unchanged rather than turning it into a pen for everyone downstream.
- **Brush fields.** A brush field left out of a file means `BinkBrush`'s own default, not the brush kind's. Fields
  whose kind default differs from it are always written, so older readers, which assumed the kind's default, read
  the same brush.
- **Unknown fields are ignored** on read, so adding a field is not a breaking change.

### Untrusted input

Loading is a parser attack surface. Everything is validated:

| Check | Limit |
| --- | --- |
| Input size | `MaxInputBytes`, 256 MB by default, checked before parsing |
| Layers, elements | `BinkDocumentLimits.MaxLayers`, `MaxElements` |
| Points per stroke, total points | `MaxPointsPerStroke` (which also bounds a path's segments), `MaxTotalPoints` |
| Coordinates | `MaxCoordinate`, checked against every element's bounds and rejected rather than clamped |
| Point time, tilt, twist | clamped to 10^9 ms, 90 degrees and 0 to 360 degrees |
| Brush width | `MaxStrokeWidth` |
| Image URIs | scheme and media type allowlist; SVG images are opt-in (`AllowSvgImages`) and remote images can be refused (`AllowRemoteImages = false`) |
| Font families | restricted character set, because the value reaches CSS and SVG |
| Transforms | singular or absurd matrices are replaced with the identity |
| Duplicate ids | repaired by assigning a new id rather than failing the load |

```csharp
var options = new BinkDeserializerOptions
{
    Limits = new BinkDocumentLimits { MaxElements = 5_000, MaxPointsPerStroke = 20_000 },
    AllowRemoteImages = false,
    AllowSvgImages = false,
};
BinkDocument document = BinkSerializer.Deserialize(json, options);
```

Malformed input throws `BinkFormatException` with a message describing what is wrong. It never allocates
unboundedly on a hostile size field.

### Size in practice

Measured on a synthetic handwriting document of 10,000 strokes with 60 points each (600,000 points):

| Encoding | Size |
| --- | --- |
| Compact, base64 binary points | about 8 bytes per point including base64 expansion and all JSON overhead |
| Readable, JSON number arrays | several times larger |

A collaboration test asserts the compact figure: a stroke costs under 14 bytes per point on the wire, including
the operation wrapper.

### Custom data

Attach your own information to a document or an element; both survive a round trip.

```csharp
document.SetMetadata("case-id", "2026-114");

document.ReplaceElement(stroke with
{
    Description = "Signature of Jane Doe",
    Metadata = new Dictionary<string, string> { ["signer"] = "jane" },
});
```

`Description` is what the canvas reports to assistive technology, and what SVG export writes as a `title` element
when `IncludeElementDescriptions` is set. Elements without one fall back to a structural summary.

### Unsaved changes

```csharp
if (document.IsDirty) { /* ask before leaving */ }

long revision = document.Revision;
await SaveToServerAsync(BinkSerializer.SerializeToUtf8Bytes(document));
document.MarkSaved(revision);   // what changed while the bytes were on their way is still unsaved
```

Any change counts, an undo included, so undoing back to the saved state still reads as changed.

### Autosave and recovery

`BinkAutosave` saves a document shortly after it changes, to any `IBinkDocumentStore`, so a reload, a closed tab or
a crash costs at most a few seconds of work:

```csharp
@inject IJSRuntime JS

@code {
    private BinkBrowserDocumentStore? _store;
    private BinkAutosave? _autosave;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;
        _store = new BinkBrowserDocumentStore(JS);
        if (await BinkAutosave.RestoreAsync(_store, "board") is { } saved) { /* offer it back */ }
        _autosave = new BinkAutosave(_document, _store, "board");
    }

    public async ValueTask DisposeAsync()
    {
        if (_autosave is not null) await _autosave.DisposeAsync();   // saves what is still waiting
    }
}
```

It saves two seconds after editing pauses, and at least every thirty seconds while it never does (`Delay`,
`MaxDelay`), one write at a time. It serializes on the thread that created it, because a document is not
thread-safe, so create it in the component. A store that fails is reported through `SaveFailed` and `LastError` and
tried again. It is a recovery copy, separate from the application's own saving: it leaves `IsDirty` alone, and
`DiscardAsync` removes the copy once the application has saved for real.

`BinkBrowserDocumentStore` keeps documents in the browser's IndexedDB, which survives reloads and crashes, holds far
more than `localStorage`, and never leaves the device. Reading one back streams it, so a large drawing is no problem
on Blazor Server. It works in a Safari private window too, which refuses to store a `Blob` in IndexedDB, because
the document is stored as plain bytes. The browser may still clear it under storage pressure or when a private
window closes, so treat it as a safety net. `BinkMemoryDocumentStore` is the in-memory one; implement `IBinkDocumentStore`, three methods over bytes,
to keep drawings in a database or behind an API. The whiteboard demo autosaves this way and offers the drawing back
when the page is opened again.

## SVG and PNG export

PNG export walks the same `BinkDocumentRenderer` path as the screen, and SVG export walks the document itself, but
both draw every element through the same `IBinkRenderContext`, so an exported file matches what was drawn.

### SVG

```csharp
string svg = BinkSvgExporter.Export(document);
byte[] bytes = BinkSvgExporter.ExportToUtf8Bytes(document);   // for writing a .svg file
BinkSvgExporter.Export(document, stream);                     // streaming, for large drawings
```

From the component:

```csharp
string svg = await canvas.ExportSvgAsync();
await canvas.SaveSvgAsync("drawing.svg");
```

The output is genuinely vector. Strokes become filled outline paths, shapes become native SVG geometry, text
becomes `text` elements, and images become `image` elements. Nothing is rasterized.

Eraser strokes become SVG masks applied to exactly the content they covered, so an erased drawing exports the way
it looks on screen. Consecutive eraser strokes share one mask. A layer that alternates drawing and erasing more
than 240 times puts every later eraser into one mask, which can also hide ink drawn between those later erasures
but never brings erased ink back; XML parsers refuse documents nested much deeper. Layer opacity becomes group
opacity, highlighter strokes carry `mix-blend-mode: multiply`, and images are referenced with both `href` and
`xlink:href` for SVG 1.1 consumers.

#### SVG options

```csharp
var options = new BinkSvgExportOptions
{
    Region = BinkExportRegion.Content,    // Page, Content, Selection or Custom
    Padding = 12f,
    Background = BinkColor.Transparent,   // null uses the document background
    Decimals = 2,                         // fewer decimals gives smaller files
    IncludeXmlDeclaration = true,         // for a standalone file
    Title = "Signature",                  // written as a title element, for accessibility
    IncludeElementDescriptions = true,    // per-element titles, navigable by screen readers
    SelectedIds = selection.Ids.ToArray(),
};
```

#### Safety

The writer XML-escapes everything that reaches the output, including text content, descriptions and font family
names. It never emits script, event handler attributes or foreign objects, and colours are emitted as hex, which
cannot carry a payload. The export tests assert this against hostile input.

Images are the one external reference the output can contain: a `BinkImageElement` whose source is an `https:` URL
is written as an `image` element pointing at it. Embed images as data URIs, or refuse remote sources on load with
`AllowRemoteImages = false`, if the exported file must be self-contained.

An embedded image is written in full wherever it is drawn, so a document that shows one picture many times can
export to far more text than it holds. A server that exports documents it did not write can cap the output with
`MaxLength`, in characters: past it the export throws `InvalidOperationException` instead of writing more. There is
no cap by default.

Injecting arbitrary SVG into a page is a scripting risk regardless of this library. To bring SVG from elsewhere
into a drawing, import it instead, which turns it into Bink elements and never puts its markup in the page.

#### Importing SVG

```csharp
BinkSvgImportResult result = BinkSvgImporter.Import(svgText);   // or TryImport, for untrusted input
foreach (BinkElement element in result.Elements) document.AddElement(element);
// result.Warnings lists what had no Bink equivalent

canvas.ImportSvg(svgText);   // onto the canvas: centred, grouped, selected, one undo step
```

Paths, rectangles (with rounded corners), circles, ellipses, lines, polylines and polygons become editable vector
elements with their fill, stroke, width, dashes, caps, joins, fill rule, opacity and transforms, including the root's
`viewBox`. Text keeps its content, size, colour, weight and anchor; images are imported only when the picture is
embedded as a raster `data:` URI. Groups pass their paint and transform down, and the `style` attribute overrides
presentation attributes as CSS does. With `EnableDrop`, an `.svg` file dropped on the canvas is imported this way.

SVG is treated as untrusted: a DOCTYPE is skipped without being read, so entities cannot expand; nothing is fetched,
neither external entities, stylesheets, fonts nor images by URL; scripts, foreign objects and event handlers are
ignored; and the length, element count and path segments are bounded (`BinkSvgImportOptions`). What has no
equivalent is left out and named in `Warnings` rather than approximated quietly: gradients and patterns (the shape is
filled grey, or with the fallback colour SVG allows after the reference), filters, masks, clip paths, `use`
references, CSS stylesheets and a nested viewport's view box. A path longer than a path element holds (100,000
segments) is left out with a warning, and an element whose transform collapses it (`scale(0)`) is not imported, as
SVG does not draw it. A group's opacity is applied to each element in it, which differs only where they overlap, and
the browser lays imported text out with the fonts it has.

#### Large drawings

`Export(document)` returns a string, which means the whole file exists twice in memory while it is built. For a
large document prefer the streaming overloads, which write straight to a stream:

```csharp
using var file = File.Create("drawing.svg");
BinkSvgExporter.Export(document, file);
```

### PNG

Two implementations exist, deliberately.

#### In the browser

```csharp
byte[] png = await canvas.ExportPngAsync(new BinkPngExportOptions { Scale = 3f });
await canvas.SavePngAsync("drawing.png");
```

Rendering happens on an offscreen canvas, so real fonts and every image format the browser can decode are
included. This is the right choice for anything a user downloads.

The browser encodes JPEG and WebP too, which are far smaller for a drawing over a photo:

```csharp
byte[] jpeg = await canvas.ExportImageAsync(BinkImageFormat.Jpeg, options, quality: 0.85);
await canvas.SaveImageAsync("markup.webp", BinkImageFormat.Webp);
```

JPEG has no transparency, so a transparent background is exported as white. Safari has no WebP encoder; rather than
handing back a PNG under a WebP name, the export throws `NotSupportedException` there.

#### On a server, or in tests

```csharp
byte[] png = BinkPngExporter.Export(document, new BinkPngExportOptions { Scale = 2f });
```

`BinkPngExporter` uses the built-in anti-aliased rasterizer and PNG encoder. It has no native dependencies, runs
anywhere .NET runs, and produces byte-identical output for identical input, which is what makes visual regression
testing possible.

It ships no font engine and decodes only PNG images. Rather than silently producing an incomplete picture, the
result reports what it could not draw:

```csharp
BinkPngExportResult result = BinkPngExporter.ExportWithDetails(document);
if (!result.IsComplete)
{
    // result.SkippedTextElements, result.SkippedImages
}
```

To render text on a server, supply a font engine:

```csharp
var options = new BinkPngExportOptions { TextRasterizer = new MySkiaTextRasterizer() };
```

`IBinkTextRasterizer` has two methods: draw a string into a surface, and measure one. See
[a text engine for server-side rendering](#a-text-engine-for-server-side-rendering).

#### PNG options

```csharp
var options = new BinkPngExportOptions
{
    Region = BinkExportRegion.Content,
    Padding = 8f,
    Scale = 3f,                       // output pixels per document unit
    Background = BinkColor.Transparent,
    PixelsPerMeter = 11811,           // about 300 DPI, written into the file
    MaxPixels = 80_000_000,           // export fails rather than allocating more; also bounds embedded images
    Compression = CompressionLevel.SmallestSize,
};
```

Embedded images are decoded within the same limit: each image, and all images together, may decode to at most
`MaxPixels`, an image larger than 4096 by 4096 is only decoded where it is drawn at no more than eight source
pixels per device pixel along each axis, and the decoder grows its buffers as data actually arrives, so a small
file that claims a huge image cannot allocate for it. Anything refused is counted in `SkippedImages`.

Rasterizing takes time in proportion to the geometry: every edge of a shape is tested on each sub-row of every
pixel row it spans, and every element is drawn across its bounds. A document built to be expensive, with thousands
of edges crossing every row or thousands of elements that each cover the page, can keep one export busy for
minutes. A server that exports documents it did not write can cap that work with `MaxRasterWork`, counted in edge
tests, sorted crossings and pixels touched: past it the export throws `InvalidOperationException`. There is no cap
by default; a page of handwriting at screen size needs well under a hundred million.

`Scale` is independent of the display the drawing was made on. A signature captured on a phone and one captured
on a desktop export at exactly the same resolution.

### Cropping

| Region | Meaning |
| --- | --- |
| `Page` | The document's page rectangle. |
| `Content` | The bounding box of the visible ink, plus `Padding`. |
| `Selection` | The bounding box of `SelectedIds`, plus `Padding`. |
| `Custom` | `CustomRegion`. |

Setting `SelectedIds` limits which elements are drawn whatever the region, so a selection can be exported in place
on the full page.

## Hosting models

The same component works in every Blazor hosting model. What differs is where the engine runs, and therefore what
happens between a pen moving and ink appearing.

### Blazor WebAssembly

The engine runs in the browser, in the same process as the UI. This is the model the library is designed for.

Pointer samples arrive from JavaScript once per animation frame. The active tool processes them, the live stroke
is drawn on the overlay canvas, and nothing leaves the machine. Latency is the browser's own input-to-paint
latency and nothing more.

Rendering uses a zero-copy path: the command buffer reaches JavaScript through `[JSImport]` as a view over managed
memory, so a redraw is one call with no serialization. `BinkCanvas.UsesFastRenderPath` reports whether it is
active.

No configuration is needed.

### Lazy loading on WebAssembly

A WebAssembly app can download Bink only when someone first opens a page that draws. Nothing has to be taken out of
`Program.cs`, because nothing of Bink's is registered there: the canvas takes only the framework's `IJSRuntime` and
`NavigationManager` from the app's container, and imports its browser module itself when it attaches. Mark the
assembly as lazy loaded in the project that runs in the browser, and set a runtime switch next to the mark so the
router knows the assembly is there to load:

```xml
<!-- AOT builds are left out: see below. -->
<ItemGroup Condition=" '$(RunAOTCompilation)' != 'true' ">
  <BlazorWebAssemblyLazyLoad Include="Bit.Bink.wasm" />
  <RuntimeHostConfigurationOption Include="MyApp.LazyLoadsBink" Value="true" Trim="true" />
</ItemGroup>
```

Then load it before the first page that uses Bink renders, from the router's `OnNavigateAsync`:

```razor
@* App.razor - in a Blazor Web App, the client project's Routes.razor: the router waits for OnNavigateAsync
   before it renders the page. typeof(Program) is the project the router's file is in, in either kind of app. *@
@using Microsoft.AspNetCore.Components.WebAssembly.Services
@inject LazyAssemblyLoader LazyAssemblyLoader

<Router AppAssembly="typeof(Program).Assembly" OnNavigateAsync="OnNavigateAsync">
    ...
</Router>

@code {
    // Set only where Bit.Bink.wasm is marked: loading an assembly that is not marked throws.
    private static readonly bool LazyLoadsBink = AppContext.TryGetSwitch("MyApp.LazyLoadsBink", out var on) && on;

    private async Task OnNavigateAsync(NavigationContext context)
    {
        // The pages that draw: fetch Bink before they render. A second call loads nothing again.
        if (LazyLoadsBink && IsDrawingPage(context.Path))
            await LazyAssemblyLoader.LoadAssembliesAsync(["Bit.Bink.wasm"]);
    }

    // /whiteboard and the routes beneath it, such as /Whiteboard/2 (routes match regardless of case), but not
    // /whiteboard-settings. Add every other route that renders Bink. The path keeps any query and fragment.
    private static bool IsDrawingPage(string path)
    {
        path = path.Split('?', '#')[0];
        return path.Equals("whiteboard", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("whiteboard/", StringComparison.OrdinalIgnoreCase);
    }
}
```

`Program.cs` stays as it is. The stylesheet and the initializer that injects it are static web assets rather than part
of the assembly, so they still load when the app starts, both small, and a canvas is laid out correctly from its first
render. The zero-copy render path works from the lazily loaded assembly as well. The rest of the app has to keep to
these rules:

- Only what renders after the assembly has loaded may use Bink's types - the pages it is loaded for and a layout only
  they use, not `App.razor`, `Routes.razor`, a layout every page shares or `Program.cs`. A page in the app's own
  assembly can hold a `<BinkCanvas>`, a `BinkDocument` field and fields of Bink's other types, structs included,
  since the router loads Bink before it creates the page. The method that calls `LoadAssembliesAsync` cannot use them
  either: WebAssembly resolves what a method refers to when the method first runs, which is before the load, and does
  not retry what it failed to resolve.
- No public type in an assembly the router reads in the browser - the app's own assembly first of all, the client
  project's in a Blazor Web App - may derive from or implement one of Bink's, close a generic type over one of them
  or constrain a type parameter to one: no public `BinkBrush` or `BinkElement` of your own, no public
  `BinkElementConverter<T>`, `IBinkTool` or `BinkToolBase`, no public `IBinkRenderContext`, `IBinkTextRasterizer`,
  `IBinkCollaborationTransport` or `IBinkDocumentStore`, no component that `@inherits` one of Bink's (a `.razor`
  component is public), no public class that derives from `List<BinkStroke>` or implements `IComparer<BinkElement>`,
  and no component with `@typeparam TElement where TElement : BinkElement`. The router reads the public types of those
  assemblies once, when it first builds its route table, which is after the first navigation's `OnNavigateAsync`; on
  WebAssembly a type whose base type, interface or constraint needs an assembly that is not loaded yet throws a
  `ReflectionTypeLoadException` there. So such a type goes unnoticed when the app is opened at a page Bink is loaded
  for, and fails the app on its first page when it is opened anywhere else: try the app from a page that does not
  draw. Only a type's declaration counts: its fields and properties can be of Bink's types, structs included, and its
  methods can take and return them. The router reads only public types, so an `internal` class - the default for a
  class with no modifier - or a `private` one nested in a page is fine: it loads the first time it is used, which is
  after Bink. A type that has to be public belongs in a library of your own, referenced from the client project and
  lazy loaded together with Bit.Bink: mark it in the same `ItemGroup`
  (`<BlazorWebAssemblyLazyLoad Include="MyApp.Drawing.wasm" />`) and load both
  (`LoadAssembliesAsync(["Bit.Bink.wasm", "MyApp.Drawing.wasm"])`).
- Register custom brushes and element types from code that runs after the load, such as a method of that library
  called from the drawing page, and register them once. `BinkBrushRegistry.Default.Register` and
  `BinkElementRegistry.Default.Register` replace an earlier registration of the same kind, and the brush registry
  drops the default brush it has cached for that kind, so registering on every page open rebuilds that brush each
  time. A static constructor runs once, however often the page opens and on however many threads:

  ```csharp
  // MyApp.Drawing, lazy loaded with Bit.Bink
  public static class DrawingExtensions
  {
      static DrawingExtensions()
      {
          BinkBrushRegistry.Default.Register<DashedBrush>();
          BinkElementRegistry.Default.Register(new StickyNoteConverter());
      }

      // The first call runs the static constructor; later ones do nothing.
      public static void Register() { }
  }
  ```

  ```razor
  @* Whiteboard.razor *@
  @code {
      protected override void OnInitialized() => DrawingExtensions.Register();
  }
  ```

- In a Blazor Web App only the browser lazy loads, so give `Routes` a render mode in `App.razor` that brings the
  router - and its `OnNavigateAsync` - to the browser. Under `InteractiveWebAssembly` it runs there once the app has
  started. Under `InteractiveAuto` it runs over a server circuit on the first visit, while the WebAssembly runtime
  downloads, and in the browser on later visits: a first visit draws on the server, and the browser downloads Bink on
  a later one.
- With per-page render modes the router renders statically on the server and never runs `OnNavigateAsync` in the
  browser, so a page that draws fails there as it becomes interactive, with `Could not load file or assembly
  'Bit.Bink'` - or `Could not load type of field`, naming one of the page's fields. Such a page loads Bink itself. It
  names none of Bink's types, loads the assembly in `OnInitializedAsync`, and only then renders a component of yours
  that holds the canvas:

  ```razor
  @* Pages/Whiteboard.razor, in the client project. WhiteboardEditor is a component of yours without a route, in the
     same project, that holds the canvas: the page creates it only once Bink has loaded. *@
  @page "/whiteboard"
  @rendermode InteractiveWebAssembly
  @using Microsoft.AspNetCore.Components.WebAssembly.Services
  @inject LazyAssemblyLoader LazyAssemblyLoader

  @if (_loaded)
  {
      <WhiteboardEditor />
  }

  @code {
      // The switch set next to the mark: unset on the server and in an AOT build, where the editor renders at once.
      private static readonly bool LazyLoadsBink = AppContext.TryGetSwitch("MyApp.LazyLoadsBink", out var on) && on;

      private bool _loaded;

      protected override async Task OnInitializedAsync()
      {
          if (LazyLoadsBink)
              await LazyAssemblyLoader.LoadAssembliesAsync(["Bit.Bink.wasm"]);

          _loaded = true;
      }
  }
  ```

  While the assembly downloads, the canvas the server prerendered gives way to nothing; render a placeholder in an
  `else` branch if that matters.
- Every host other than standalone WebAssembly registers `LazyAssemblyLoader` itself if a component it renders
  injects it - the server project of a Blazor Web App, where `Routes.razor` runs while it is prerendered and over the
  circuit under `InteractiveAuto`, and a Hybrid (MAUI, WPF, Windows Forms) host that shares the router's file:
  `builder.Services.AddScoped<LazyAssemblyLoader>()`. A component's services are resolved when it is created, whether
  or not the switch lets it load anything, and only a WebAssembly host registers this one by itself. There is still
  nothing of Bink's to register: those hosts load Bink with the rest of the app. Outside the browser
  `LazyAssemblyLoader` downloads nothing: it loads each assembly the call names from the ones the app already
  references, and throws if one is not among them. A library lazy loaded with Bit.Bink reaches the server through the
  client project's reference to it.
- The condition leaves AOT builds out. AOT compiles every assembly into `dotnet.native.wasm`, so there is nothing left
  to download later, and lazy loading an AOT-compiled assembly can fail at startup. The switch is set only where the
  mark is, so an AOT build skips `LoadAssembliesAsync` as well - loading an assembly that is not marked throws.
- `Could not load file or assembly 'Bit.Bink'` (or `Could not load type of field`) means something used Bink's types
  before the assembly loaded: load it in `OnNavigateAsync` for every route that renders them - from the page itself,
  for a page with its own render mode - and keep them out of `Program.cs`, `App.razor`, the layouts every page shares
  and the method that loads it. If it comes in a `ReflectionTypeLoadException` from the router on the first page -
  and only when the app is opened away from a page that draws - a public type in an assembly the router reads derives
  from, implements or is constrained to one of Bink's: make it internal, or move it into the library lazy loaded with
  Bit.Bink. `... must be marked with 'BlazorWebAssemblyLazyLoad' item group` names an assembly the call loads that the
  project file does not mark.

### Blazor Server

The engine runs on the server. Pointer samples travel over the SignalR circuit in batches of one animation frame,
and the resulting canvas commands travel back.

That round trip is real. On a local network it is a few milliseconds; over the internet it can be 50 to 150 ms,
which is enough to feel disconnected from the pen. Two things address it:

**Wet ink.** While a batch is in flight, the browser draws a provisional trail: a plain constant-width polyline in
the brush colour. It carries no stroke model and makes no decisions; it exists so the pen feels attached to the
hand. As soon as the engine responds, the real geometry replaces it. This is on by default under interactive
server rendering. It is only drawn for a tool that draws ink, so a selection drag or the eraser leaves no trail, and
never on a read-only canvas, under a pen's eraser end or during a two-finger gesture.

```razor
<BinkCanvas WetInk="true" />    <!-- default outside WebAssembly -->
<BinkCanvas WetInk="false" />   <!-- to turn it off -->
```

**Batching.** Input is already one message per frame rather than one per event, and the message is a flat array of
numbers rather than an object graph. A 240 Hz pen still costs 60 messages per second, not 240.

**Retained paths.** The other direction is small too. The browser keeps every path it has drawn, so committing a
stroke on a page of five thousand sends that stroke and a few numbers for each of the others, about 160 KB as
packed floats, rather than every coordinate of every stroke again, which was about 50 MB of JSON. An embedded image
crosses the circuit once, not with every redraw of the page it is on.

**Nothing from the browser outgrows a message.** A circuit refuses any single message from the browser above 32 KB
by default, and closes the connection when it gets one. Exported images, long pastes and long text from the inline
editor therefore travel as streams, and a burst of pointer samples queued behind a busy main thread is sent in
chunks, so none of them depends on raising `MaximumReceiveMessageSize`.

Be honest with yourself about the use case. A signature field or a form annotation over Blazor Server is fine.
Sustained handwriting over a high-latency connection is not, whatever the library does; that is what WebAssembly
is for. A Blazor Web App can render exactly the pages that need drawing with `InteractiveWebAssembly` and leave
the rest on the server.

### Blazor Web App

Mark the page or component interactive:

```razor
@rendermode InteractiveWebAssembly
```

Prerendering is handled: the canvas renders its markup on the server, discovers that no browser is available,
stays inert, and attaches when interactivity arrives. `AttachFailed` is null in that case, because nothing failed.

Put the component in the client project so its assembly is available to WebAssembly.

A page with its own render mode cannot use Bink from a [lazily loaded](#lazy-loading-on-webassembly) assembly as it
is: the router that loads it runs statically on the server, so the page fails once it is interactive. Give `Routes`
the render mode instead, or have each such page load the assembly itself, as that section shows.

### Blazor Hybrid

MAUI, WPF and Windows Forms hosts run the engine in-process and the canvas in the embedded WebView. Everything
works, over the standard `IJSRuntime` path; the zero-copy render path is WebAssembly-only, and it is not needed
because there is no serialization boundary of consequence.

### When the browser layer cannot start

If the JavaScript module cannot load, because a content security policy blocks it or static web assets are not
served, the canvas stays inert instead of throwing and taking the page down. `Ready` is not raised; `Unavailable`
is raised instead, with the reason, so an application can tell the user something useful. The same reason stays
available afterwards as `BinkCanvas.AttachFailed`, and the canvas reports it to screen readers itself.

```razor
<BinkCanvas Unavailable="reason => _unavailable = reason" />

@if (_unavailable is not null)
{
    <p role="alert">Drawing is unavailable: @_unavailable</p>
}

@code {
    private string? _unavailable;
}
```

### Content security policy

The component needs no `eval` and no inline script. Its browser layer and its Blazor initializer are modules
loaded from your own origin. A policy of `script-src 'self'` is enough for it, and because the initializer also
injects a stylesheet link from the same origin, so is `style-src 'self'`. If you serve static web assets from a
CDN, allow that origin.

## Mobile, touch and stylus

### Scrolling versus drawing

The single most important decision on a touch device is whether a finger drag draws or scrolls the page. There is
no setting that gets both, so the component asks you which one this canvas is.

```razor
<BinkCanvas EnableTouchGestures="false" />   <!-- default: a finger scrolls the page -->
<BinkCanvas EnableTouchGestures="true" />    <!-- a finger draws, and pinch zooms the canvas -->
```

With gestures off the canvas sets `touch-action: pan-x pan-y`, so a signature field embedded in a long form does
not trap the page. A stylus still draws, because a pen pointer is not a touch pointer.

With gestures on the canvas sets `touch-action: none` and owns every touch. Use this for a full-screen whiteboard.

### Palm rejection

While a pen is in contact, touch contacts are ignored. A hand usually lands before the nib does, so a touch
gesture that is already in progress when the pen arrives is cancelled rather than left to commit a mark:

```razor
<BinkCanvas RejectTouchWhilePen="true" />   <!-- default -->
```

This is what stops a hand resting on a tablet from drawing while you write. It is a heuristic, not a hardware
feature: the browser exposes pen and touch as separate pointer types, and the library uses that. Turn it off for a
finger-first surface where someone might legitimately use both at once.

### Pinch zoom and panning

With `EnableTouchGestures="true"` a second finger takes over the gesture whatever the active tool is: the
in-flight tool gesture is cancelled and both contacts drive pan and pinch zoom until the last finger lifts. One
finger still draws. `BinkPanTool` does the same thing when it is the active tool:

```csharp
var pan = new BinkPanTool { MinZoom = 0.1f, MaxZoom = 32f, EnablePinchZoom = true };
```

Panning and zooming change only the viewport, so they produce no operations, no history entries and no change to
the vector data.

### Mouse wheel and trackpad

```razor
<BinkCanvas WheelMode="BinkWheelMode.PanAndZoom" MinZoom="0.1f" MaxZoom="16f" />
```

| Mode | Wheel | Ctrl+wheel, trackpad pinch | Shift+wheel |
| --- | --- | --- | --- |
| `None` (default) | Scrolls the page | Browser zoom | Scrolls the page |
| `PanAndZoom` | Pans | Zooms around the pointer | Pans sideways |
| `Zoom` | Zooms around the pointer | Zooms around the pointer | Pans |

`PanAndZoom` is the convention of design tools and whiteboards, `Zoom` that of maps. The default leaves the wheel to
the page, which is right for a canvas embedded in a form. Wheel movement is accumulated and applied once per
animation frame, and a wheel turned while a stroke or a drag is in progress is ignored, because the gesture's points
are being mapped through the view it started with.

`MinZoom` and `MaxZoom` bound every way of zooming: the wheel, pinch gestures, the keyboard, `ZoomBy` and
`ZoomToFit`.

### High-DPI displays

The canvas backing store is sized to the device pixel ratio, so ink is sharp on a phone or a Retina display. The
ratio is watched at runtime: moving the window to a different display, or changing browser zoom, resizes the
backing store and redraws.

Nothing about a drawing depends on the display it was made on. A signature captured on a 3x phone and one captured
on a 1x monitor are the same vector document, and export at the same resolution.

### Pointer capture

The browser layer captures the pointer on contact, so a gesture continues when the finger or pen leaves the canvas
and completes properly when it is lifted outside. A cancelled gesture, for example when the system takes over,
arrives as a cancel and rolls the interaction back rather than leaving a half-drawn stroke.

### Coalesced samples

A stylus samples far faster than the display refreshes. The browser buffers those samples and hands them over with
the frame's pointer event; the library reads them all, so fast handwriting keeps its shape instead of turning into
straight lines between frame positions.

`Capabilities.CoalescedEvents` reports whether the browser supports this.

### Orientation and resizing

The canvas observes its own element. Rotating a phone or resizing a split view resizes the backing store and
redraws at the new size. The document is unaffected: it has its own coordinate space.

### Low-latency canvas

`LowLatency` asks the browser for the `desynchronized` hint, which shortens the path from drawing command to pixels
on browsers that support it. `Capabilities.LowLatencyCanvas` reports whether the browser honoured it.

```razor
<BinkCanvas LowLatency="true" />
```

It is off by default. Chromium (seen on Windows with GPU compositing) shows a desynchronized canvas that is not
fully opaque as solid black once it sits inside an element that clips to rounded corners (`overflow: hidden` with
a `border-radius`, an ordinary card), and the layer the live stroke is drawn on is never fully opaque. Turn it on
for a canvas with no such ancestor, such as a full-screen whiteboard or a note-taking page. It is read when the
canvas attaches.

### Pointer prediction

Between the digitizer reporting a position and the frame that shows it, the pen moves on. Browsers that offer
`getPredictedEvents` (Chromium-based ones) estimate where it is going, and the canvas draws the live stroke reaching
that far, which hides about a frame of latency:

```razor
<BinkCanvas PredictInk="true" />   <!-- the default in WebAssembly -->
```

Predicted points are drawn on the overlay only. The next measured sample replaces them, they are smoothed like real
ones and held to a few stroke widths so a wrong guess cannot draw a spike, and the committed stroke contains
measured points alone. The default is on in WebAssembly and off elsewhere, where the round trip to the server is far
longer than the frame a prediction covers. `BinkDrawTool.UsePrediction` turns it off for one tool.

### Practical advice

- A signature field: fixed height, `EnableTouchGestures="false"`, a clear button, and a validation rule on ink
  length rather than on whether anything was drawn at all.
- A note-taking surface: `EnableTouchGestures="true"`, palm rejection on, a pen brush with low smoothing and tight
  spacing, and a stroke or segment eraser.
- A form annotation over a document: gestures off so the page still scrolls, and a highlight tool rather than
  freehand for text.
- Test with a real stylus. Mouse input exercises none of the pressure, tilt or palm-rejection paths.

### Hit targets

Selection handles are drawn small, but what they respond to is not. A contact reported as touch grows the handle
and element hit areas by `BinkSelectTool.TouchTargetScale`, which is 2.8 by default, so a fingertip lands on a
target close to the 24 by 24 pixel minimum WCAG 2.5.8 asks for without the handles looking clumsy under a mouse.

## Accessibility

### What is honestly possible

A canvas is pixels. A screen reader cannot describe handwriting, and no amount of ARIA changes that. Claiming
otherwise would be worse than useless, because it would stop developers from doing the work that actually helps.

What the library does provide:

- A properly labelled, focusable, keyboard-operable canvas.
- A live text summary of what the drawing contains, built from the descriptions attached to elements.
- Announcements of changes through a polite live region.
- Real HTML controls for everything except the drawing itself.
- An export path that carries the same descriptions into the SVG.

What your application must still do: give elements meaningful descriptions, and provide a non-drawing route to
any information that only exists as a drawing.

### The canvas element

```razor
<BinkCanvas AriaLabel="Signature for the tenancy agreement" />
```

Rendered as:

```html
<div role="application" aria-roledescription="drawing canvas"
     aria-label="Signature for the tenancy agreement"
     aria-describedby="bit-bink-...-desc" tabindex="0">
  <canvas data-bink-layer="document" aria-hidden="true"></canvas>
  <canvas data-bink-layer="overlay" aria-hidden="true"></canvas>
  <div data-bink-layer="input" aria-hidden="true"></div>
  <div id="bit-bink-...-desc" class="bit-bink-visually-hidden">Drawing with 12 elements on 2 layers. Signature of Jane Doe.</div>
  <div class="bit-bink-visually-hidden" role="status" aria-live="polite">Erased 3 strokes.</div>
</div>
```

The canvases are hidden from assistive technology because their content is unreadable. The description element
beside them is not, and it updates as the document changes.

### Describing content

```csharp
document.ReplaceElement(stroke with { Description = "Signature of Jane Doe" });
document.AddElement(new BinkLineShape(a, b) { EndArrow = true, Description = "Arrow pointing at the total" });
```

Elements without a description fall back to something structural, such as "Stroke, 142 points, pen brush, color
#111827", and text elements report their text. Exported SVG carries the same strings as `title` elements when
`IncludeElementDescriptions` is set.

### Keyboard

The canvas takes focus when it is drawn on, so shortcuts are reachable for someone who just used a mouse or pen.
Stepping through elements and changing the selected ones - Enter to start, Tab, the arrow keys, R, Shift+H and
Shift+V - is the select tool's work, so those keys act while `BinkSelectTool` is the active tool; the others work
whatever the tool.

| Key | Action |
| --- | --- |
| Ctrl+Z | Undo |
| Ctrl+Shift+Z, Ctrl+Y | Redo |
| Ctrl+A | Select all |
| Enter | Select the first element, which is how you start navigating without a pointer |
| Tab, Shift+Tab | Step to the next or previous element, or group, announcing each one |
| Delete, Backspace | Delete the selection |
| Escape | Cancel the gesture, then clear the selection |
| Arrow keys | Nudge the selection; Shift for larger steps |
| Ctrl+arrow keys | Resize the selection; Shift for larger steps |
| R, Shift+R | Rotate the selection by 15 degrees |
| Shift+H, Shift+V | Flip the selection horizontally or vertically |
| Ctrl+D | Duplicate the selection |
| Ctrl+], Ctrl+[ | Bring the selection forward, send it backward; with Shift, to the front or the back |
| Ctrl+G, Ctrl+Shift+G | Group the selection, ungroup it |
| Ctrl+Shift+L | Lock the selection |
| Ctrl+C, Ctrl+X, Ctrl+V | Copy, cut and paste, when `EnableClipboard` is on |
| Enter, with the text tool active | Open a text box in the middle of the view |
| Enter, with a shape tool active | Add the shape in the middle of the view, to move and resize with the select tool |
| Ctrl+0 | Reset zoom |
| Ctrl+9 | Zoom to fit |
| Ctrl+plus, Ctrl+minus | Zoom |

Every element can therefore be reached, moved, resized, rotated, flipped, grouped and deleted without a pointer,
and each step announces what is now selected. Groups are one stop for Tab, as they are one click. Text is reachable
too: the text tool opens its real HTML editor from the keyboard, so dictation, an input method and a screen reader
all work on it. And shapes can be made without a drag: Enter with a shape tool adds one in the middle of the view.

The canvas is not a keyboard trap. Tab belongs to it only while an element is selected: pressing Escape clears
the selection, and the next Tab moves focus out of the canvas as usual. Delete, Backspace and the arrow keys are
likewise left to the page when nothing is selected, so a keyboard user can still scroll past a focused canvas, and
so are Ctrl+C, Ctrl+X, Ctrl+D and the bracket shortcuts, so copying text beside the canvas keeps working.
Keys pressed inside `ChildContent`, or in any input, button or link within the canvas, are never intercepted.

Turn them off with `EnableKeyboardShortcuts="false"` if the surrounding application owns those keys. That
genuinely releases the keys: the browser layer stops preventing their default behaviour as well.

To take back only some keys, list them in `DisabledShortcuts`, as chords of modifiers and a key joined with `+`:

```razor
<BinkCanvas DisabledShortcuts="@(new[] { "Ctrl+D", "Ctrl+G", "R" })" />
```

A disabled chord reaches neither the tool nor the canvas, and the browser layer leaves its default alone, so the
application's own handler gets it (or the browser's: Ctrl+D bookmarks the page). Every other shortcut keeps
working. Ctrl means Command on macOS, as it does for every shortcut here; `Cmd`, `Meta` and `Control` are read as
Ctrl and `Option` as Alt; letters can be written in either case; and a chord written with the unshifted key, such
as `Ctrl+Shift+]`, also matches the `}` the browser reports for that press. `BinkShortcut.Normalize` shows how a
chord is read. Enter and Escape in the inline text editor are how the editor works, not shortcuts, so they cannot
be disabled. To move a command to another key, disable its chord, handle the new key in the page and call the
canvas method behind it, such as `DuplicateSelection`, `GroupSelection` or `Undo`.

Keyboard input travels the same path as pointer input, so it does not depend on a framework event and does not
re-render the component per keystroke.

### Toolbars

Build them from real HTML. The demo's toolbar is buttons and inputs with `aria-pressed`, `aria-label` and
`role="toolbar"`, and it is fully keyboard operable without the canvas being involved at all. Never put controls
inside the canvas.

### Live announcements

```csharp
canvas.Announce("Signature captured.");
```

Tools already announce meaningful events: how many elements a marquee selected, how many strokes an erase
removed, that an image was added. Announcements are coalesced to at most one per frame.

### Colour, contrast and motion

- The library never relies on colour alone to convey state; selection is shown with an outline and handles.
- Under forced colours the container sets `forced-color-adjust: none`, because a drawing whose colours are
  replaced by the system palette is no longer the user's drawing. Style the surrounding UI to respect the
  forced-colours palette.
- There are no animations, so there is nothing to disable for reduced motion.

### What WCAG asks of a drawing surface

Freehand drawing is a path-based gesture, and 2.5.1 Pointer Gestures exempts input where the path itself is
essential: you cannot draw a signature with arrow keys. Everything *around* the drawing is not exempt, which is
why tools, colours, undo, layer controls and export must be ordinary controls. That is the split the library
takes: the ink is a gesture, everything else is a button.

Under 1.1.1 Non-text Content, a drawing needs a text alternative. `Description` is how you supply one; the canvas
surfaces it, and so does the exported SVG.

## Performance

Every number in this section was measured with the library's BenchmarkDotNet suite. Run it yourself from the
benchmark project:

```bash
dotnet run -c Release
dotnet run -c Release -- --filter "*Render*"
```

The demo application also has a performance page that measures the engine in your own browser, on your own
hardware, with a document you generate.

### The design decisions that matter

**Two canvases.** Committed elements live on a document canvas; the live stroke and tool previews live on an
overlay. While a stroke is being drawn only the overlay is redrawn, so the thousands of strokes already in the
document are not touched until the pen lifts.

**Input never re-renders Blazor.** Samples arrive from JavaScript once per animation frame and are processed
directly. A component render happens at most once per frame, and only when something bound to the document
actually changed.

**Geometry is cached.** A stroke's outline is built once per (points, brush) pair and reused by every redraw and
every export.

**Bounds never build geometry.** Asking an element for its bounds returns a conservative estimate from the brush
until the exact outline exists. Loading a document reads the bounds of every element, so building every outline up
front made loading far slower and allocated far more; the load figures below are what it costs now.

**Culling.** A redraw records only the elements whose bounds intersect the visible rectangle.

**Paths are sent once.** The browser keeps every path it has drawn as a `Path2D`, and a redraw refers to it by a
small slot number: about eight numbers per element instead of every coordinate of its outline. A moved element keeps
its path, because moving only changes its transform; an edited one gets a new path. Long strings, such as an
embedded photo's data URI, are kept the same way, so they cross the boundary once rather than with every stroke
drawn on top of them. Paths nobody has drawn for a while are released, least recently used first, once more than
twice what the last frame drew are held. The browser fills a retained path in one call, where it used to replay
hundreds of curve segments one call at a time.

**Packed floats off WebAssembly.** In WebAssembly the command buffer is handed to JavaScript as a view over managed
memory. Everywhere else it travels as a byte array of 32-bit floats, which Blazor transfers as binary: a quarter of
the size of the same numbers as JSON text, and nothing for the browser to parse.

**Constant-time element lookup.** Each layer indexes its elements by id. Dragging a selection only replaces elements
in place, so the index stays exact for the whole gesture; an insertion or removal only invalidates the positions
above it. Deleting many elements removes them from the top of each layer down, so no removal moves one still to go.

**Compact history.** A drag records every frame, and a group made only of replacements (moving, resizing, rotating,
the connector re-routing a diagram does in response) is folded into one replacement per element as it grows, and an
element the group added, such as an Alt-dragged copy, into its addition. Undoing a long drag replays one operation
per element instead of one per element per frame, and the gesture's memory stays bounded while it runs.

**Structure-of-arrays points.** Positions, pressure and time are parallel float arrays. Reading them allocates
nothing, and optional channels are only allocated when the device produces them.

### Measured results

Taken on .NET 10, x64, a desktop machine. Sizes are binary: MB means 2^20 bytes, as BenchmarkDotNet reports it.
Absolute numbers depend on hardware; the ratios and the allocation figures are the meaningful part.

#### Input, the per-frame hot path

| Samples per frame | Time | Allocated |
| --- | --- | --- |
| 4 | 81 ns | 0 B |
| 16 | 299 ns | 0 B |
| 64 | 1.24 us | 0 B |

Sixty-four samples is a 240 Hz pen with coalescing on a slow frame. Processing them costs about one hundredth of
a 16 ms frame budget and allocates nothing, so drawing produces no garbage collection pressure.

#### Stroke geometry

| Case | Time | Allocated |
| --- | --- | --- |
| Handwriting stroke, 60 points, variable width | 3.3 us | 2.2 KB |
| Handwriting stroke, 60 points, centerline | 0.7 us | 1.6 KB |
| Continuous stroke, 10,000 points, variable width | 534 us | 333 KB |

This runs once per stroke, not once per frame.

#### Redraw

Recording a redraw for a document of handwriting strokes spread over a 1600 by 1000 page. "In full" is what an
export, or a frame with nothing retained yet, records: every coordinate of every path. "Retained" is what the canvas
records for every frame after the first, referring to the paths the browser holds:

| Strokes | In full, whole page | In full, 900 by 600 view | Retained, whole page | Retained frame packed for the wire |
| --- | --- | --- | --- | --- |
| 500 | 436 us | 201 us | 27 us | 16 KB in 1.9 us |
| 5,000 | 5.1 ms | 2.0 ms | 0.35 ms | 160 KB in 42 us |

Allocation is 120 bytes per recorded frame, because the command buffer is reused between frames. A full frame of
5,000 strokes is about 3.6 million numbers, which over Blazor Server used to be about 50 MB of JSON; the retained
frame is 40,000 numbers. These two tables were measured together on the same machine.

A redraw happens when the document or the viewport changes, not while drawing. The browser then replays the
buffer, filling each retained path in one call.

#### Editing a large selection

Every stroke selected, dragged for one second at 60 frames a second:

| Strokes | One frame of the drag | Undo and redo of the whole drag |
| --- | --- | --- |
| 1,000 | 0.59 ms | 0.34 ms |
| 10,000 | 8.1 ms | 6.7 ms |

Before element lookups were indexed and drag history was compacted, the same frame cost about 400 ms at 10,000
strokes and undoing the drag about 3 seconds, on the same machine.

#### Saving, loading and export

Document of 10,000 strokes, 600,000 points:

| Operation | Time | Allocated |
| --- | --- | --- |
| Save | 28 ms | 22 MB |
| Load | 72 ms | 22 MB |
| Encode 10,000 points | 81 us | 101 KB |
| Decode 10,000 points | 55 us | 156 KB |
| Export SVG | 569 ms | 146 MB |

SVG export is the expensive one, and inherently so: 600,000 points become about 35 million characters of path
data. Most of that memory is the string itself, twice over, because `Export` returns one. Use the streaming
overload for documents this size and the second copy disappears.

#### Hit testing

A click with the selection tool over a 10,000-stroke document is a bounding-box rejection for almost every
element, followed by exact geometry for the few that overlap.

### Working with very large documents

- Keep the viewport meaningful. Culling is what makes a 10,000-stroke document interactive; a zoomed-out view of
  everything is the expensive case by definition.
- Use `BeginBatch` when adding many elements. One batch is one change event and one redraw.
- Prefer the streaming export overloads for large SVG files.
- Raise `BinkDocumentLimits` deliberately rather than by default. They exist to turn a runaway document into a
  clear error instead of an unresponsive tab.

### What is not optimized

- The pure .NET rasterizer is written for correctness and portability, not speed. Browser PNG export is faster and
  is what the component uses.
- Recording a full-document redraw is linear in the number of elements. There is no spatial index; culling uses a
  linear scan of bounding boxes. For the document sizes the library targets this is not the bottleneck (scanning
  ten thousand bounding boxes takes tens of microseconds), and a spatial index would add invalidation complexity
  that is not currently earning its place.
- The live stroke is re-sent in full on every frame it changes, because it is new geometry every frame. That is
  invisible in WebAssembly and costs bandwidth proportional to the stroke's length on Blazor Server.

## Collaboration

Collaboration is optional. Its types ship in the same package, but nothing in the core engine pays for them until
you create a `BinkCollaborationSession`.

### The design

Ink is append-mostly, and elements are identified by a `Guid`. That means the operations that matter commute
naturally: two people drawing at once produce two additions that can be applied in any order with the same result.
There is no conflict to resolve, so there is no CRDT.

The one genuine conflict is two people editing the *same* element, for example dragging the same shape. That is
settled last-writer-wins on the element's version, with the participant id breaking ties so every replica reaches
the same answer without coordination.

An application that needs stronger guarantees can layer a CRDT on top of these operations. Nobody pays for that
complexity by default.

### Wiring it up

```csharp
var session = new BinkCollaborationSession(document, transport, participantId: "alex");

session.PresenceChanged += presence => { /* redraw their cursor */ };
session.ParticipantLeft += id => { /* remove them */ };
session.RemoteOperationRejected += reason => { /* log it */ };

await session.RequestSnapshotAsync();   // when joining an existing session
```

Everything the local user does is transmitted automatically: the session subscribes to the document and forwards
local changes, including undo and redo, which to everyone else are ordinary edits.

### The transport

```csharp
public interface IBinkCollaborationTransport
{
    bool IsConnected { get; }
    Task SendAsync(BinkCollaborationMessage message, CancellationToken cancellationToken = default);
    event Func<BinkCollaborationMessage, Task>? MessageReceived;
    event Action<bool>? ConnectionChanged;
}
```

Implement it over SignalR, WebSockets, WebRTC or anything else. The transport must deliver messages and exclude
the sender from its own broadcasts. Each sender's messages must arrive in order, which SignalR and WebSockets both
give you; a message that arrives after a later one from the same sender is discarded. Duplicates are harmless, and
ordering between different senders does not matter: an operation on an element or layer that has not arrived yet,
which a mesh transport such as WebRTC can deliver first, waits up to 30 seconds for it, while an operation on
something this replica deleted is dropped, because the deletion is the newer intent. A remote batch is applied as
far as it still applies, so one stale part (an element someone else deleted meanwhile) does not discard the rest.
Sequence numbers start from the clock, so a participant that reloads the page or crashes and rejoins under the
same id is heard again without having sent a leave message.

A message with a `RecipientId` is meant for that participant alone. A transport that can route, such as a SignalR
hub sending to one connection, should deliver it only there; one that broadcasts everything is still correct,
because a session ignores messages addressed to someone else.

A SignalR implementation is about thirty lines: forward `SendAsync` to a hub method, raise `MessageReceived` from
a hub callback, and raise `ConnectionChanged` from the connection's reconnect events.

### What travels on the wire

Drawing a stroke sends that stroke once, as one operation. Not the document, not the other strokes. A typical
handwriting stroke is a few hundred bytes because the points use the same compact binary encoding as the file
format.

```csharp
string json = BinkOperationCodec.Serialize(operation);
BinkDrawingOperation received = BinkOperationCodec.Deserialize(json);
```

Incoming operations are validated exactly like a loaded file, against `BinkCollaborationOptions.Limits`. A
participant cannot make another client allocate unboundedly by sending a hostile operation.

An embedded picture crosses the wire once. Every change to an element carries the whole element, so moving a
photo used to send the photo again with every frame of the drag: a megabyte at sixty frames a second. Now the first
change that shows a picture carries it, and later ones (each frame of the drag, an undo that brings it back, three
copies pasted at once) refer to it by a hash of its content. A receiver resolves the reference from the same change,
from its own document, or from the last 32 million characters of pictures it has received; if it still lacks the
picture, because it joined after the picture was sent and the picture has since left the document, it holds the
change, asks for a snapshot, and applies the change on top of it. It asks at most once every ten seconds, so a peer
that refers to pictures that do not exist cannot make everyone send the document over and over; such changes are
reported through `RemoteOperationRejected` and dropped.

References are only used once every participant the session has heard from understands them. Each message carries
`Protocol`, which reads as 0 from a version that predates it; while anyone at 0 is in the session, every change
carries its pictures in full and snapshots are written without an asset table, so mixed versions keep working.

### Live ink and cursors

A finished stroke is an operation. A stroke *in progress* is presence: transient, throttled, and never recorded in
history or saved with the document.

```csharp
await session.UpdatePresenceAsync(new BinkPresenceState
{
    ParticipantId = "alex",
    Name = "Alex",
    Color = "#2563eb",
    Cursor = documentPoint,
    LiveStroke = pointsSoFar,
    LiveStrokeColor = "#2563eb",
    LiveStrokeWidth = 4f,
});
```

Presence is throttled to `PresenceInterval` (50 ms by default), and stale participants expire after
`PresenceTimeout`. Call `FlushPresenceAsync` on a timer so the last update is not held back by the throttle.
Presence is stored under the sender's id and timestamped on arrival, so expiry does not depend on the sender's
clock and nobody can speak for someone else; live strokes are capped at 10,000 points.

### Offline and reconnect

While the transport is disconnected, operations are queued, up to `MaxOfflineQueue`. Presence is not queued,
because stale presence is worse than none. When the transport reports a connection again, the queue is flushed in
order. Presence shares the sequence numbers that order a participant's messages, so while operations are still
queued, a presence update is held back and sent by the next `FlushPresenceAsync`; sending it first would make the
others discard the queued operations as late arrivals. A snapshot or a snapshot request made while the queue
drains waits behind it for the same reason.

The session queues what it sends, not what it should have received. Messages sent to a participant while it is
offline have to be kept by the server and delivered when it reconnects, or that participant asks for a snapshot
after reconnecting to catch up.

```csharp
if (!session.IsSynchronized)
{
    // session.PendingCount changes are waiting to be sent
}
```

### Joining

A new participant asks for a snapshot. Every participant that serves snapshots (`ServeSnapshots`, on by default)
answers with the serialized document, addressed to the one that asked. That participant applies the first answer
as a single batched change with origin `Remote`, so it does not land in their undo history, and ignores the rest:
a later, slower answer could be older than strokes already received since. Changes made or received while the
answer was on its way are applied again on top of it, because the answer may have been written before they reached
whoever sent it, so nothing drawn during the join is lost. Everyone else ignores answers meant for someone else, so
a participant joining never disturbs the ones already there.

`SendSnapshotAsync` is different: it sends the document to everyone, and every participant replaces their copy
with it. Use it to reset a session to one version, for example after restoring a saved document.

Both sides must be editing the same document, including its layer ids. In practice a client joins by loading the
snapshot, which gives it exactly that.

Every message names the document its sender edits. When one transport carries several documents, for example a
hub without per-document groups, set `IgnoreOtherDocuments` so a session drops the traffic of the others. That
requires every participant to construct its document with the shared id (`new BinkDocument { Id = sharedId }`)
before joining, because a snapshot replaces the content but not the id.

### Threading

A `BinkDocument` is not thread-safe: it is meant to be used from one thread, which in Blazor is the renderer's
dispatcher. A real transport does not respect that on its own. SignalR raises `MessageReceived` on a thread-pool
thread, so a message applied directly from that callback can run at the same moment the local user is drawing,
and the two writers will corrupt the document's element index.

Marshal the message inside your transport, before it raises `MessageReceived`. In a component,
`ComponentBase.InvokeAsync` puts the work on the renderer's dispatcher:

```csharp
// The transport is constructed with the component's InvokeAsync so it can hand messages to the right thread.
public sealed class SignalRTransport(HubConnection connection, Func<Func<Task>, Task> dispatch) : IBinkCollaborationTransport
{
    public event Func<BinkCollaborationMessage, Task>? MessageReceived;

    public void Start() =>
        connection.On<BinkCollaborationMessage>("Message", message =>
            dispatch(() => MessageReceived?.Invoke(message) ?? Task.CompletedTask));

    // ...
}
```

The same applies in the other direction. `UpdatePresenceAsync`, `FlushPresenceAsync` and `FlushAsync` read the
document and the session's queue, so call them from the same thread that owns the document. A loopback transport
that is already on the dispatcher, such as the one the demo uses, needs nothing extra.

### Limits worth knowing

- Two people editing the same element at the same moment: one edit wins, the other is dropped. Every replica
  agrees on which, but the losing edit is gone.
- Element and layer order is index-based and is not reconciled. Concurrent reorders (bring to front, send to back,
  moving between layers, reordering layers), or an undone delete re-inserting an element at its old index while
  someone else reorders, can leave participants with the same elements stacked in a different order. Nothing is
  lost, and `SendSnapshotAsync` brings everyone back to one order.
- There is no server-side authority in the box. If you need one, the same operations are what it would arbitrate.

## Extending Bink

Six extension points cover the things applications actually need to change. Each is a small interface or an
abstract record, and none of them requires touching the engine. An app that [lazy loads](#lazy-loading-on-webassembly)
Bink on WebAssembly keeps the implementations its browser code uses in a library lazy loaded together with it, never
in the client's own assembly. Ones only the server uses, such as a document store the server saves to or a text
engine for server-side PNG export, can stay in a Blazor Web App's server project, which loads Bink with the rest of
the app.

### A custom brush

Derive from `BinkBrush`, give it a unique `Kind`, and implement `BuildGeometry`.

```csharp
public sealed record DashedBrush : BinkBrush
{
    public const string KindName = "dashed";

    public override string Kind => KindName;

    /// <summary>Length of each dash in document units.</summary>
    public float DashLength { get; init; } = 6f;

    public override BinkStrokeGeometry BuildGeometry(BinkStrokePointsView points, BinkPathBuilder builder)
    {
        builder.Reset();
        // Emit whatever geometry you like: this walks the points and draws every other segment.
        for (int i = 1; i < points.Count; i++)
        {
            if (i % 2 == 0) continue;
            builder.MoveTo(points.PositionAt(i - 1));
            builder.LineTo(points.PositionAt(i));
        }
        return new BinkStrokeGeometry(
            builder.ToGeometry(),
            BinkStrokeGeometryMode.Centerline,
            Width,
            BinkLineCap.Round,
            BinkLineJoin.Round,
            points.Bounds.Inflate(Width));
    }
}
```

Register the kind so documents containing it can be loaded:

```csharp
BinkBrushRegistry.Default.Register<DashedBrush>();
```

The registry supplies a default instance, and the serialized colour, width, opacity, blend mode, smoothing,
spacing, taper and pressure curve are applied on top of it. A client without the registration still renders the
stroke, as a pen with the stored appearance, rather than losing it.

Settings of your own, such as `DashLength`, are saved with the stroke once the brush names them:

```csharp
public override IReadOnlyDictionary<string, float> GetParameters()
    => new Dictionary<string, float> { ["dashLength"] = DashLength };

public override BinkBrush WithParameters(IReadOnlyDictionary<string, float> parameters)
    => this with { DashLength = parameters.TryGetValue("dashLength", out float d) ? Math.Clamp(d, 1f, 100f) : DashLength };
```

They are written as `params` in the brush, read back through `WithParameters`, which has to clamp them because a
file or a collaborator can say anything, and kept unchanged by a client that does not know the brush, so passing a
document through it loses nothing. `BinkCalligraphyBrush` is built exactly this way.

`BinkStrokePointsView` gives you positions, pressure and timing. Tilt and twist are on the stroke's `Points` if you
need them for a brush that follows the pen's angle.

### A custom tool

Implement `IBinkTool`, or derive from `BinkToolBase` and override only what you need.

For a worked example larger than the snippet below, look at the demo's diagram surface. It builds a whole
node-and-connector diagram surface out of the public API alone: two tools, element metadata for the graph
structure, and a listener that re-routes connectors when a node moves. The library has no idea what a diagram is,
and nothing in it needed changing.

```csharp
public sealed class DotTool : BinkToolBase
{
    public override string Id => "dot";
    public override string DisplayName => "Dot";
    public override string Cursor => "crosshair";

    public override bool OnPointerDown(IBinkToolContext context, in BinkInputSample sample)
    {
        Vector2 point = context.Viewport.ToDocument(sample.Position);
        context.Document.AddElement(
            new BinkEllipseShape(BinkRect.FromCenter(point, 12f, 12f))
            {
                Style = new BinkShapeStyle { FillColor = context.ShapeStyle.StrokeColor, StrokeWidth = 0f },
            },
            context.ActiveLayerId);
        context.InvalidateDocument();
        return true;
    }
}
```

`IBinkToolContext` gives a tool the document, selection, history, viewport, active layer, current brush and shape
style, the canvas's snapping options, plus the ability to invalidate a canvas layer, set the cursor and announce a
message. `Snapping` has a default implementation, so a host that implements the interface itself keeps compiling;
build a `BinkSnapper` from it when a gesture starts to give a custom tool the same snapping as the built-in ones.

Tools mutate the document exclusively through operations, so anything a tool does is undoable and synchronizable
without extra work. Wrap a gesture in `context.History.BeginGroup()` to make it a single undo step.

Draw a live preview by overriding `DrawOverlay`; it is called on the overlay canvas, in document coordinates,
whenever you have called `InvalidateOverlay`.

### A custom render target

Implement `IBinkRenderContext` to draw a document anywhere: a PDF library, a printer, a different graphics API.

```csharp
public sealed class PdfRenderContext : IBinkRenderContext
{
    public void FillPath(BinkPathGeometry path, in BinkFillPaint paint) { /* walk path.Verbs and path.Coordinates */ }
    public void StrokePath(BinkPathGeometry path, in BinkStrokePaint paint) { }
    // Save, Restore, Transform, ClipRect, DrawImage, DrawText, PushGroup, PopGroup
}

BinkDocumentRenderer.Render(document, new PdfRenderContext());
```

Two rules matter. `PushGroup` must genuinely isolate, because content inside it may erase and that must not reach
what was drawn before. And fills must honour `BinkFillRule.NonZero`, because that is what keeps a self-overlapping
stroke outline solid.

### A text engine for server-side rendering

The built-in rasterizer has no font engine. Supply one to draw text in server-side PNG export.

```csharp
public sealed class SkiaTextRasterizer : IBinkTextRasterizer
{
    public void DrawText(BinkRasterSurface surface, string text, Vector2 origin, in BinkTextStyle style,
                         float scale, float opacity, BinkRect clip) { }

    public Vector2? Measure(string text, in BinkTextStyle style) => null;
}

BinkPngExporter.Export(document, new BinkPngExportOptions { TextRasterizer = new SkiaTextRasterizer() });
```

### A collaboration transport

Implement `IBinkCollaborationTransport` over SignalR, WebSockets or anything else. See
[collaboration](#collaboration).

### A custom element type

`BinkElement` is an abstract record you can derive from, and a custom element renders, hit tests, transforms,
groups and locks like any other. To save it, paste it and send it to collaborators, register a converter for it:

```csharp
public sealed class StickyNoteConverter : BinkElementConverter<StickyNote>
{
    public override string TypeName => "acme-sticky";

    public override void Write(Utf8JsonWriter writer, StickyNote note, BinkSerializerOptions options)
    {
        writer.WriteString("text", note.Text);
        writer.WriteNumber("votes", note.Votes);
        // ... the rectangle
    }

    public override StickyNote Read(JsonElement json, BinkDeserializerOptions options)
    {
        // Untrusted input: validate every value and bound every size, and throw FormatException when it is wrong.
    }
}

BinkElementRegistry.Default.Register(new StickyNoteConverter());
```

The serializer writes the `type` and every field all elements share (id, version, transform, description,
metadata, lock, groups) itself, plus the element's `bounds`; the converter writes only the type's own fields. The
bounds are for readers without the registration: another application, or an older build, loads the element as a
`BinkUnknownElement` that is not drawn but keeps its place, culls and selects by its bounds, and is written back
unchanged, so nothing is lost on the way through. Whatever a converter throws while reading is reported as a
malformed document, and the element is held to the document's coordinate limits like every other.

The default registry is shared. An application that wants its own passes it through
`BinkSerializerOptions.ElementRegistry` and `BinkDeserializerOptions.ElementRegistry`; a collaboration session
reads with the registries of its `SerializerOptions`, so whatever it can send it can also receive. A custom element
without a registration still cannot be written, deliberately: nothing could read it back.

### What is deliberately closed

The operation set. `BinkDrawingOperation` cannot be subclassed from outside the assembly, because undo,
serialization and synchronization all depend on knowing every operation's inverse and wire format. Compose the
existing operations, including `BinkBatchOperation`, instead.

## License

Bink is part of [bitplatform](https://github.com/bitfoundation/bitplatform) and is licensed under the
[MIT license](https://github.com/bitfoundation/bitplatform/blob/develop/LICENSE).
