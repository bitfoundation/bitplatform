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
`_content/Bit.Bink/bit-bink.css` automatically.

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
- Selection, move, resize and rotate through transforms; layers with visibility, lock and opacity.
- Shapes, arrows, highlights, callouts, text with a real HTML editor, and images.
- Undo and redo built on inverse operations, never snapshots.
- A versioned, compact JSON format with binary stroke points, hardened against untrusted input.
- SVG and PNG export that match the screen, including a pure .NET PNG path for servers and tests.
- Real-time collaboration over any transport, with presence and offline queueing.
- Palm rejection, pinch zoom, coalesced samples and high-DPI, low-latency canvases.
- A labelled, keyboard-operable canvas with a live text summary for screen readers.
- Blazor WebAssembly, Server, Web App (including prerendering) and Hybrid on .NET 8, 9 and 10.

## For AI agents

The demo site hosts an MCP server at `https://bink.bitplatform.dev/mcp` (also served through
`https://bitplatform.dev/mcp`) that answers from the shipped library: its API, its guide and the demo's working
pages.

## Documentation

The full guide - architecture, brushes and pressure, erasing, selection and layers, annotations, serialization,
export, hosting models, mobile and stylus, accessibility, performance, collaboration and extending the library - is
in the [Bink README](https://github.com/bitfoundation/bitplatform/tree/develop/src/Bink). See it running at
[bink.bitplatform.dev](https://bink.bitplatform.dev).
