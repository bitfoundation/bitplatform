# The editor component

```razor
<div style="height: 70vh">
    <BmageEditor @ref="_editor" />
</div>
```

The editor fills its container, so give it a height. Everything else has a working default.

## Parameters

| Parameter | Purpose |
| --- | --- |
| `Image` | The image to edit. Setting it replaces the session and its history. The editor works on a copy, so your buffer stays yours. |
| `Strings` | Every user-visible label. See [Localisation](#localisation). |
| `Direction` | `"ltr"` or `"rtl"`. The layout uses logical CSS properties, so it mirrors correctly. |
| `Class`, `Style` | Applied to the root element. Use `bmg-theme-dark` or `bmg-theme-light` to force a theme. |
| `ShowToolbar`, `ShowPanel` | Hide the built-in chrome without replacing it. |
| `Toolbar`, `Panel`, `Footer` | Replace the chrome entirely; each receives a `BmageEditorContext`. |
| `EmptyState` | Replace the "drop an image here" view. |
| `MaxProxyDimension` | Longest edge used for interactive editing. Falls back to `BitBmageOptions.MaxInteractiveDimension`, then 2048. Set 0 to always edit at full resolution. |
| `MaxHistoryDepth` | Undo steps kept. Default 100. |
| `ExportOptions` | Settings used by the built-in save action. |
| `OnDocumentChanged`, `OnExported`, `OnImageLoaded`, `OnError` | Callbacks. |

`OnImageLoaded` matters more than it looks: the editor can load an image through its own drop zone, and your `Image`
parameter will still be null when it does. If your page has buttons that depend on an image being present, handle this
callback so the page re-renders.

## Driving it from code

```csharp
@code {
    private BmageEditor? _editor;

    async Task Crop16x9()
    {
        _editor!.SetTool(EditorToolKind.Crop);
        _editor.SetCropAspect(AspectRatio.Ratio16x9);
    }

    async Task Save() => await _editor!.DownloadAsync("edited",
        new ImageExportOptions { Format = ImageFormat.WebP, Quality = 0.9 });

    async Task AddCallout() => await _editor!.AddAnnotationAsync(new ArrowAnnotation
    {
        Start = new Vector2(100, 400),
        End   = new Vector2(300, 200),
        Style = ShapeStyle.Default with { Stroke = new StrokeStyle { Width = 6 } },
    });
}
```

The same surface is available to custom chrome through `BmageEditorContext`, so anything the built-in toolbar can do,
yours can too.

## Replacing the chrome

```razor
<BmageEditor ExportOptions="_options">
    <Toolbar Context="editor">
        <div class="my-toolbar">
            <button class="@(editor.Tool == EditorToolKind.Crop ? "on" : null)"
                    @onclick="() => editor.SetTool(EditorToolKind.Crop)">
                Crop
            </button>
            <button disabled="@(!editor.CanUndo)" @onclick="editor.Undo">Undo</button>
            <button disabled="@(!editor.CanRedo)" @onclick="editor.Redo">Redo</button>
            <span>@editor.OutputSize.Width × @editor.OutputSize.Height</span>
            <button @onclick="() => editor.DownloadAsync(&quot;image&quot;)">Save</button>
        </div>
    </Toolbar>
</BmageEditor>
```

`BmageEditorContext` exposes the current tool and settings, the document, the annotations, the history and its position,
the detected browser capabilities, the output size, and every action: tool selection, crop, rotate, flip, adjustments,
annotations, undo, redo, reset, zoom, load, paste, export, download and clipboard copy.

For adjustments driven by a slider, pass `transient: true` while dragging and commit once when the interaction ends, so
the history gets one entry rather than hundreds:

```csharp
async Task OnBrightnessInput(double value)
    => await context.SetAdjustmentAsync(new BrightnessAdjustment((float)value), "Brightness", transient: true);

async Task OnBrightnessCommit()
    => await context.SetAdjustmentAsync(context.FindAdjustment<BrightnessAdjustment>(), "Brightness");
```

Note that `onchange` on a range input is not reliable in Blazor when `oninput` is also handled. Debouncing `oninput`
is more robust and gives a live preview; the demo application includes a small `Debouncer` that does this.

## Theming

Every colour and metric is a CSS custom property on `.bmg-editor`. Override the ones you care about:

```css
.my-editor {
    --bmg-accent: #7c3aed;
    --bmg-accent-text: #ffffff;
    --bmg-bg: #0f0f12;
    --bmg-bg-subtle: #17171c;
    --bmg-canvas-bg: #202027;
    --bmg-border: #2a2a33;
    --bmg-text: #eeeef2;
    --bmg-radius: 4px;
    --bmg-panel-width: 320px;
}
```

```razor
<BmageEditor Class="my-editor" />
```

The full set is documented in `bit-bmage-editor.css`. Dark mode follows `prefers-color-scheme` unless you force it
with `bmg-theme-light` or `bmg-theme-dark`. High contrast mode and `prefers-reduced-motion` are honoured.

## Localisation

`BmageEditorStrings` is a record with a property for every label, so a translation is an object rather than a resource
file, and you can override only the words you care about:

```csharp
var german = BmageEditorStrings.Default with
{
    Crop = "Zuschneiden",
    Undo = "Rückgängig",
    Redo = "Wiederholen",
    Download = "Herunterladen",
    DropImageHere = "Bild hier ablegen oder klicken zum Auswählen",
};
```

```razor
<BmageEditor Strings="german" />
```

Number and size formatting are also replaceable, because they are functions on the same record:

```csharp
var strings = BmageEditorStrings.Default with
{
    FormatFileSize = bytes => $"{bytes / 1024.0:0.#} Ko",
    FormatDimensions = (w, h) => $"{w}×{h} px",
};
```

For right-to-left languages set `Direction="rtl"`. The layout uses logical properties throughout, so the panel, the crop
handles and the toolbar all mirror.

## Tools

| Tool | Behaviour |
| --- | --- |
| `Select` | Click to select an annotation, drag to move it, arrow keys to nudge, Delete to remove |
| `Pan` | Drag to move the view |
| `Crop` | Drag a rectangle, adjust with eight handles, apply from the toolbar |
| `Draw` | Freehand, smoothed so fast strokes do not look polygonal |
| `Line`, `Arrow` | Drag from start to end; hold Shift to snap to 15° increments |
| `Rectangle`, `Ellipse` | Drag to size; hold Shift to constrain to a square or circle |
| `Highlight` | Translucent block, like a marker pen |
| `Redact` | Solid, pixelate or blur; replaces the pixels underneath |
| `Text` | Places a label you then edit |
| `Step` | Auto-numbered markers for tutorials and bug reports |

## Keyboard and accessibility

| Keys | Action |
| --- | --- |
| Ctrl/Cmd + Z | Undo |
| Ctrl/Cmd + Shift + Z, Ctrl + Y | Redo |
| Delete, Backspace | Delete the selected annotation |
| Arrow keys | Nudge the selection by 1px, or 10px with Shift |
| Escape | Clear the selection, or cancel a pending crop |
| Plus, Minus | Zoom |
| Ctrl/Cmd + 0 | Fit to window |

The canvas is a focusable `application` region with a described keyboard hint. The toolbar is a `toolbar` with labelled
buttons carrying `aria-pressed`. The panel is a `tablist` with arrow-key navigation. Crop handles are focusable buttons
with 24px hit targets. Status messages use live regions. Focus is visible throughout.

## Touch

Touch works out of the box. The canvas registers a non-passive `touchmove` listener through the JavaScript bridge, which
Blazor's own event handling cannot do, so drawing on the editor does not scroll the page. Pointer capture keeps a drag
tracking when the finger leaves the element. Controls use touch-sized targets on narrow screens, and the panel moves
below the canvas.

## Large images

By default the editor edits through a proxy no larger than 2048px on its longest edge, and replays the edit at full
resolution when you export. `ImageEditSession.UsesProxy` and `ProxyScale` tell you when this is happening, and the demo
surfaces it. Set `MaxProxyDimension="0"` to always edit at full resolution, which is only sensible for small images. To change
the default for every editor in the application, set `MaxInteractiveDimension` when registering the services.

## The document

`OnDocumentChanged` hands you the whole edit as a value:

```csharp
private void OnDocumentChanged(ImageDocument document)
{
    // Pixel operations, in order.
    foreach (var operation in document.Pipeline) Console.WriteLine(operation);

    // Annotation objects, still editable.
    foreach (var annotation in document.Annotations)
        Console.WriteLine($"{annotation.Kind} at {annotation.GetBounds()}");

    Console.WriteLine($"Exports as {document.OutputSize.Width} × {document.OutputSize.Height}");
}
```

That document can be stored, sent to a server, or replayed against the original file later. It is the edit, not a
rendering of it.
