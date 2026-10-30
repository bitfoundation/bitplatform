# Bit.Bink

A native drawing and ink engine for Blazor: signature fields, handwriting, whiteboards, image markup and document
review. The stroke model, smoothing, pressure response, hit testing, undo, serialization and export are written in
C#. JavaScript only collects pointer and keyboard input and replays a command buffer onto a canvas - there is no
JavaScript drawing library underneath.

## Quick start

```bash
dotnet add package Bit.Bink
```

```razor
@* _Imports.razor *@
@using Bit.Bink
```

No services to register and no stylesheet to reference: the package's Blazor initializer injects
`_content/Bit.Bink/bit-bink.css` automatically. Having nothing to register also lets a WebAssembly app lazy load
`Bit.Bink.wasm`; see
[lazy loading](https://github.com/bitfoundation/bitplatform/tree/develop/src/Utilities/Bink#lazy-loading-on-webassembly).

```razor
<BinkCanvas @ref="_canvas" Height="180px" AriaLabel="Sign here"
            DocumentChanged="() => StateHasChanged()" />

<button @onclick="() => _canvas!.Clear()">Clear</button>
<button @onclick="SaveAsync">Save</button>

@code {
    private BinkCanvas? _canvas;

    private async Task SaveAsync()
    {
        string svg = await _canvas!.ExportSvgAsync(new BinkSvgExportOptions { Region = BinkExportRegion.Content });
        string json = BinkSerializer.Serialize(_canvas.CurrentDocument);
        // store either one; BinkSerializer.Deserialize + LoadDocument brings the drawing back
    }
}
```

Pick what pointer input does with `Tool` (`BinkDrawTool`, `BinkEraserTool`, `BinkSelectTool`, `BinkShapeTool`,
`BinkTextTool`, `BinkPanTool`) and how ink looks with `Brush` (`BinkPenBrush`, `BinkPencilBrush`,
`BinkMarkerBrush`, `BinkHighlighterBrush`).

## Features

- Pressure-aware, variable-width ink with smoothing, simulated pressure, tilt and twist capture.
- Stroke, segment and pixel erasers.
- Selection, lasso, move, resize and rotate through transforms, snapping to a grid and to other elements; layers
  with visibility, lock and opacity.
- Copy, cut and paste through the system clipboard (images and text included), duplicate, Alt-drag copies and
  bring-to-front, each with its usual shortcut and one undo step.
- Shapes, arrows, highlights, callouts, text with a real HTML editor, and images.
- Undo and redo built on inverse operations, never snapshots, with long drags compacted to one step per element.
- A versioned, compact JSON format with binary stroke points, hardened against untrusted input.
- SVG, PNG, JPEG and WebP export that match the screen, including a pure .NET PNG path for servers and tests.
- Ink measurements (length, stroke count, duration) for validating signatures.
- Real-time collaboration over any transport, with presence and offline queueing.
- Palm rejection, pinch zoom, mouse wheel and trackpad pan and zoom, coalesced samples, high-DPI canvases and an
  opt-in low-latency canvas.
- Retained rendering: the browser keeps every path it has drawn, so redrawing a large page costs a few numbers per
  element, over WebAssembly and over a Blazor Server circuit alike.
- A labelled, keyboard-operable canvas with a live text summary for screen readers.
- Blazor WebAssembly, Server, Web App (including prerendering) and Hybrid on .NET 10 and 11; lazy loadable on
  WebAssembly.

## For AI agents

The demo site hosts an MCP server at `https://bink.bitplatform.dev/mcp`
that answers from the shipped library: its API, its guide and the demo's working
pages.

## Documentation

The full guide - architecture, brushes and pressure, erasing, selection and layers, annotations, serialization,
export, hosting models, mobile and stylus, accessibility, performance, collaboration and extending the library - is
in the [Bink README](https://github.com/bitfoundation/bitplatform/tree/develop/src/Utilities/Bink). See it running at
[bink.bitplatform.dev](https://bink.bitplatform.dev).
