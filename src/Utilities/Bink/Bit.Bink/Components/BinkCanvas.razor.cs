using System.Numerics;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Bink;

/// <summary>
/// A drawing surface: pointer, touch and stylus input, high-quality ink rendering, tools, undo/redo and export.
/// </summary>
/// <remarks>
/// <para>
/// Every await in this component resumes on the renderer's dispatcher (<c>ConfigureAwait(true)</c>). On Blazor
/// Server an interop call completes on a thread-pool thread, and continuing there raised <see cref="Ready"/> off
/// the dispatcher, which takes the circuit down the moment a handler calls <c>StateHasChanged</c>; it also let the
/// overlay be recorded while the dispatcher was processing pointer samples into the same tool, and let an image or
/// a text edit change the document from a second thread. WebAssembly has one thread, which is why it never showed.
/// </para>
/// <para>
/// The component is a thin shell around the engine in <c>Bit.Bink</c>. It owns two canvases: the document
/// surface holding committed elements, and an overlay holding the live stroke and tool previews. Pointer input
/// never triggers a Blazor render: samples arrive from the browser once per animation frame, the active tool
/// processes them, and only the affected canvas layer is redrawn.
/// </para>
/// <example>
/// A signature pad:
/// <code>
/// &lt;BinkCanvas @ref="canvas" Width="400px" Height="180px" AriaLabel="Signature" /&gt;
/// @code {
///     private BinkCanvas? canvas;
///     private void Clear() =&gt; canvas!.Clear();
///     private string Save() =&gt; canvas!.ExportSvg();
/// }
/// </code>
/// </example>
/// </remarks>
public sealed partial class BinkCanvas : ComponentBase, IAsyncDisposable, IBinkInteropClient
{
    // The document surface refers to paths and images the browser already holds, so a redraw costs a few numbers
    // per element rather than every coordinate again. The overlay is redrawn with geometry that is new every
    // frame (the live stroke, handles, previews), which a cache could only churn through, so it records in full.
    private readonly BinkCanvasResourceCache _resources = new();
    private readonly BinkCanvasRenderContext _documentContext;
    private readonly BinkCanvasRenderContext _overlayContext = new(2048);
    private readonly ToolContext _toolContext;
    private readonly string _elementId = "bit-bink-" + Guid.NewGuid().ToString("N")[..12];

    private ElementReference _container;
    private BinkInterop? _interop;
    private BinkDocument _document = null!;
    private BinkHistoryManager _history = null!;
    private BinkSelection _selection = null!;
    private IBinkTool _activeTool = null!;
    private BinkBrush _brush = new BinkPenBrush();
    private BinkShapeStyle _shapeStyle = BinkShapeStyle.Default;
    private BinkViewport _viewport = BinkViewport.Identity;
    private Guid _activeLayerId;
    private BinkInputCapabilities _capabilities = BinkInputCapabilities.None;

    private double _cssWidth;
    private double _cssHeight;
    private double _devicePixelRatio = 1;
    private bool _documentDirty = true;
    private bool _overlayDirty = true;
    private bool _attached;
    private bool _renderScheduled;
    private bool _uiDirty;
    private bool _disposed;
    private readonly BinkPanTool _touchGesture = new();
    private readonly Dictionary<int, Vector2> _touchPoints = [];
    private bool _gestureRouting;
    private bool _selectionActiveSent;
    private string _announcement = string.Empty;
    private string _cursor = "crosshair";
    private IBinkTool? _previousToolParameter;
    private BinkDocument? _previousDocumentParameter;
    private BinkViewport _previousViewportParameter = BinkViewport.Identity;
    private TextEditorState? _textEditor;
    private BinkStroke? _lastAddedStroke;
    private bool _watchForAddedStroke;
    private ElementReference _textEditorElement;
    private bool _focusTextEditor;
    private bool _firstRenderDone;
    private (BinkColor? Color, float Width, float Opacity)? _wetInkSent;
    private BinkInteropOptions? _optionsSent;

    /// <summary>Creates the component.</summary>
    public BinkCanvas()
    {
        _toolContext = new ToolContext(this);
        _documentContext = new BinkCanvasRenderContext(8192, _resources);
    }

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    // ---- Parameters -------------------------------------------------------------------------------------------

    /// <summary>
    /// The document to edit. When null the component creates and owns one, which is what a signature pad or a
    /// simple drawing field wants.
    /// </summary>
    [Parameter]
    public BinkDocument? Document { get; set; }

    /// <summary>The active tool. Defaults to freehand drawing.</summary>
    [Parameter]
    public IBinkTool? Tool { get; set; }

    /// <summary>The brush used by drawing tools.</summary>
    [Parameter]
    public BinkBrush? Brush { get; set; }

    /// <summary>The style applied to newly created shapes and annotations.</summary>
    [Parameter]
    public BinkShapeStyle? ShapeStyle { get; set; }

    /// <summary>The layer new elements are added to. Defaults to the top-most editable layer.</summary>
    [Parameter]
    public Guid? ActiveLayerId { get; set; }

    /// <summary>The viewport (pan and zoom). Supports two-way binding.</summary>
    [Parameter]
    public BinkViewport Viewport { get; set; } = BinkViewport.Identity;

    /// <summary>Raised when the viewport changes, for two-way binding.</summary>
    [Parameter]
    public EventCallback<BinkViewport> ViewportChanged { get; set; }

    /// <summary>When true, input is ignored and the drawing is display-only.</summary>
    [Parameter]
    public bool ReadOnly { get; set; }

    /// <summary>Maximum number of undo steps. Zero disables history.</summary>
    [Parameter]
    public int HistoryLimit { get; set; } = 200;

    /// <summary>CSS width of the canvas element, for example <c>"100%"</c> or <c>"400px"</c>.</summary>
    [Parameter]
    public string Width { get; set; } = "100%";

    /// <summary>CSS height of the canvas element.</summary>
    [Parameter]
    public string Height { get; set; } = "300px";

    /// <summary>Extra CSS classes for the container.</summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>Extra inline styles for the container.</summary>
    [Parameter]
    public string? Style { get; set; }

    /// <summary>Accessible name of the canvas.</summary>
    [Parameter]
    public string AriaLabel { get; set; } = "Drawing canvas";

    /// <summary>Whether the canvas can receive keyboard focus. Required for keyboard shortcuts.</summary>
    [Parameter]
    public bool Focusable { get; set; } = true;

    /// <summary>Whether the built-in keyboard shortcuts (undo, redo, delete, select all, escape) are handled.</summary>
    [Parameter]
    public bool EnableKeyboardShortcuts { get; set; } = true;

    /// <summary>
    /// Whether two-finger pan and pinch zoom are handled by the canvas. When false, touch gestures scroll the page
    /// instead, which is usually what a small signature field in a form wants.
    /// </summary>
    [Parameter]
    public bool EnableTouchGestures { get; set; }

    /// <summary>Whether a finger touch is ignored while a pen is in contact, so a resting palm does not draw.</summary>
    [Parameter]
    public bool RejectTouchWhilePen { get; set; } = true;

    /// <summary>
    /// Whether the live stroke is drawn reaching where the browser predicts the pen is going, which hides about a
    /// frame of latency on a stylus. Predictions are drawn, never stored. Defaults to on in WebAssembly and off
    /// elsewhere, where the round trip to the server is far longer than the frame a prediction covers.
    /// </summary>
    [Parameter]
    public bool? PredictInk { get; set; }

    /// <summary>
    /// What the mouse wheel and a trackpad do over the canvas. The default, <see cref="BinkWheelMode.None"/>,
    /// leaves the wheel to the page, which is right for a canvas embedded in a form that has to stay scrollable.
    /// </summary>
    [Parameter]
    public BinkWheelMode WheelMode { get; set; }

    /// <summary>
    /// How moving, resizing and drawing shapes snap to a grid and to other elements. Null, the default, snaps to
    /// nothing; <c>new BinkSnapOptions()</c> lines edges and centres up with other elements and the page.
    /// </summary>
    [Parameter]
    public BinkSnapOptions? Snapping { get; set; }

    /// <summary>The smallest zoom the wheel, pinch gestures, the keyboard and <see cref="ZoomBy"/> allow.</summary>
    [Parameter]
    public float MinZoom { get; set; } = 0.05f;

    /// <summary>The largest zoom the wheel, pinch gestures, the keyboard and <see cref="ZoomBy"/> allow.</summary>
    [Parameter]
    public float MaxZoom { get; set; } = 64f;

    /// <summary>
    /// Whether Ctrl+C, Ctrl+X and Ctrl+V copy, cut and paste through the system clipboard: selected elements, and
    /// pasted images. Off by default, because a signature field that accepts a pasted image or pasted ink no
    /// longer proves anyone signed. <see cref="CopySelectionAsync"/>, <see cref="PasteAsync()"/> and the other
    /// methods work regardless, so an application can offer clipboard buttons of its own.
    /// </summary>
    [Parameter]
    public bool EnableClipboard { get; set; }

    /// <summary>
    /// Raised when plain text, rather than copied Bink content or an image, is pasted onto the canvas. Without a
    /// handler the text becomes a text element in the middle of the view.
    /// </summary>
    [Parameter]
    public EventCallback<string> TextPasted { get; set; }

    /// <summary>
    /// Whether a provisional trail is drawn in the browser while a sample batch travels to the server. Ignored in
    /// WebAssembly, where the engine already runs locally. Defaults to on for interactive server rendering.
    /// </summary>
    [Parameter]
    public bool? WetInk { get; set; }

    /// <summary>Content rendered above the canvas, for toolbars or overlays positioned by the application.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Additional attributes applied to the container element.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    // ---- Events -----------------------------------------------------------------------------------------------

    /// <summary>
    /// Raised when a stroke begins. Raised only for drawing tools (<see cref="BinkDrawTool"/> and subclasses other
    /// than the eraser), the same tools whose strokes <see cref="StrokeCompleted"/> reports; selecting, panning,
    /// placing a shape or editing text does not raise it.
    /// </summary>
    [Parameter]
    public EventCallback<BinkStrokeEventArgs> StrokeStarted { get; set; }

    /// <summary>Raised when a stroke is committed to the document.</summary>
    [Parameter]
    public EventCallback<BinkStrokeEventArgs> StrokeCompleted { get; set; }

    /// <summary>Raised after any change to the document.</summary>
    [Parameter]
    public EventCallback<BinkDocumentChangedEventArgs> DocumentChanged { get; set; }

    /// <summary>Raised when the selection changes.</summary>
    [Parameter]
    public EventCallback SelectionChanged { get; set; }

    /// <summary>Raised when the undo or redo stack changes.</summary>
    [Parameter]
    public EventCallback HistoryChanged { get; set; }

    /// <summary>Raised once the canvas is attached and the device capabilities are known.</summary>
    [Parameter]
    public EventCallback<BinkInputCapabilities> Ready { get; set; }

    /// <summary>
    /// Raised instead of <see cref="Ready"/> when the browser layer cannot start, for example because a content
    /// security policy blocks its script, with the reason. The canvas stays inert; <see cref="AttachFailed"/>
    /// holds the same reason afterwards.
    /// </summary>
    [Parameter]
    public EventCallback<string> Unavailable { get; set; }

    // ---- Public API -------------------------------------------------------------------------------------------

    /// <summary>The document being edited.</summary>
    public BinkDocument CurrentDocument => _document;

    /// <summary>Undo and redo for this canvas.</summary>
    public BinkHistoryManager History => _history;

    /// <summary>The current selection.</summary>
    public BinkSelection Selection => _selection;

    /// <summary>What the current device and browser support. Valid once <see cref="Ready"/> has been raised.</summary>
    public BinkInputCapabilities Capabilities => _capabilities;

    /// <summary>The active tool.</summary>
    public IBinkTool ActiveTool => _activeTool;

    /// <summary>The current viewport.</summary>
    public BinkViewport CurrentViewport => _viewport;

    /// <summary>The canvas size in CSS pixels.</summary>
    public Vector2 ViewSize => new((float)_cssWidth, (float)_cssHeight);

    /// <summary>True when the zero-copy WebAssembly rendering path is in use.</summary>
    public bool UsesFastRenderPath => _interop?.UsesFastPath ?? false;

    /// <summary>The id of the container element, useful for labelling and testing.</summary>
    public string ElementId => _elementId;

    /// <summary>Undoes the last change. Returns false when there is nothing to undo.</summary>
    public bool Undo()
    {
        if (ReadOnly || !_history.Undo()) return false;
        Announce(_history.CanUndo ? "Undone." : "Undone. Nothing left to undo.");
        InvalidateAll();
        return true;
    }

    /// <summary>Redoes the last undone change. Returns false when there is nothing to redo.</summary>
    public bool Redo()
    {
        if (ReadOnly || !_history.Redo()) return false;
        Announce("Redone.");
        InvalidateAll();
        return true;
    }

    /// <summary>Removes every element, as one undoable step.</summary>
    public void Clear()
    {
        if (ReadOnly || _document.IsEmpty) return;
        _document.Clear();
        _selection.Clear();
        Announce("Canvas cleared.");
        InvalidateAll();
    }

    /// <summary>Deletes the selected elements.</summary>
    public void DeleteSelection()
    {
        if (ReadOnly || _selection.IsEmpty) return;
        int count = _selection.Count;
        _document.RemoveElements(_selection.Ids);
        Announce($"Deleted {count} elements.");
        InvalidateAll();
    }

    /// <summary>Replaces the document being edited and resets history and selection.</summary>
    public void LoadDocument(BinkDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        // Same as replacing the Document parameter: a gesture in flight refers to the old document's elements and
        // history, and finishing it afterwards would commit it into the new document.
        if (!ReferenceEquals(document, _document)) CancelActiveGesture();
        AttachDocument(document);
        InvalidateAll();
    }

    /// <summary>Sets the viewport, notifying <see cref="ViewportChanged"/>.</summary>
    public void SetViewport(BinkViewport viewport)
    {
        if (_viewport == viewport) return;
        float previousZoom = _viewport.Zoom;
        _viewport = viewport;
        // The provisional trail is drawn in the browser in view pixels, so its width has to follow the zoom.
        if (previousZoom != viewport.Zoom) _ = UpdateWetInkStyleAsync();
        InvalidateAll();
        if (ViewportChanged.HasDelegate) _ = ViewportChanged.InvokeAsync(viewport);
    }

    /// <summary>Fits the drawing into the visible area.</summary>
    public void ZoomToFit(float padding = 24f)
    {
        BinkRect bounds = _document.ContentBounds;
        if (bounds.IsEmpty) bounds = _document.Settings.PageRect;
        if (bounds.IsEmpty || _cssWidth <= 0) return;
        BinkViewport fit = BinkViewport.Fit(bounds, ViewSize, padding, ZoomCeiling);
        // A drawing too large for the view at the smallest zoom is shown at that zoom, centred.
        if (fit.Zoom < ZoomFloor) fit = fit.ZoomTo(ZoomFloor, ViewSize / 2f);
        SetViewport(fit);
        Announce("Zoomed to fit.");
    }

    /// <summary>Resets pan and zoom.</summary>
    public void ResetViewport() => SetViewport(BinkViewport.Identity);

    /// <summary>Zooms around the center of the view, within <see cref="MinZoom"/> and <see cref="MaxZoom"/>.</summary>
    public void ZoomBy(float factor) => SetViewport(_viewport.ZoomBy(factor, ViewSize / 2f, ZoomFloor, ZoomCeiling));

    // A parameter set to nonsense (zero, a minimum above the maximum) must not produce a zoom that inverts the view.
    private float ZoomFloor => MinZoom > 0 && float.IsFinite(MinZoom) ? MathF.Min(MinZoom, ZoomCeiling) : 0.05f;

    private float ZoomCeiling => MaxZoom > 0 && float.IsFinite(MaxZoom) ? MaxZoom : 64f;

    // ---- Clipboard and arrangement ----------------------------------------------------------------------------

    /// <summary>The largest clipboard payload read, in bytes. A paste is untrusted input of any size.</summary>
    private const long MaxPasteBytes = 32L * 1024 * 1024;

    // What this canvas copied last. Kept per canvas, never statically: on Blazor Server a static would hand one
    // user's drawing to every other user's paste.
    private string? _clipboard;
    private int _pasteCount;

    /// <summary>
    /// Copies the selected elements to the system clipboard, and keeps a copy for this canvas in case the browser
    /// refuses the write. Returns false when nothing is selected.
    /// </summary>
    public async Task<bool> CopySelectionAsync()
    {
        string? payload = CopySelectionCore();
        if (payload is null) return false;
        await ProvideClipboardAsync(payload).ConfigureAwait(true);
        Announce($"Copied {_selection.Count} elements.");
        return true;
    }

    /// <summary>Copies the selected elements to the clipboard and deletes them, as one undoable step.</summary>
    public async Task<bool> CutSelectionAsync()
    {
        if (ReadOnly) return false;
        string? payload = CopySelectionCore();
        if (payload is null) return false;
        await ProvideClipboardAsync(payload).ConfigureAwait(true);
        int count = _selection.Count;
        _document.RemoveElements(_selection.Ids);
        Announce($"Cut {count} elements.");
        InvalidateAll();
        return true;
    }

    private string? CopySelectionCore()
    {
        if (_selection.IsEmpty) return null;
        // Bottom-most first, so a paste stacks the copies the way the originals were.
        var ordered = new List<BinkElement>(_selection.Count);
        foreach (BinkElement element in _document.AllElements) if (_selection.Contains(element.Id)) ordered.Add(element);
        _clipboard = BinkClipboard.Serialize(ordered);
        _pasteCount = 0;
        return _clipboard;
    }

    private async Task ProvideClipboardAsync(string? payload)
    {
        if (_interop is null || _disposed) return;
        try
        {
            await _interop.ProvideClipboardAsync(payload).ConfigureAwait(true);
        }
        catch (JSDisconnectedException)
        {
        }
        catch (JSException)
        {
        }
    }

    /// <summary>
    /// Pastes from the system clipboard, falling back to what this canvas copied last when the browser refuses to
    /// read it. Use it for a paste button; Ctrl+V reaches the canvas without it when <see cref="EnableClipboard"/>
    /// is on.
    /// </summary>
    /// <returns>The number of elements added.</returns>
    public async Task<int> PasteAsync()
    {
        if (ReadOnly) return 0;
        string? text = _interop is null ? null : await _interop.ReadClipboardAsync(MaxPasteBytes).ConfigureAwait(true);
        if (_disposed) return 0;
        return Paste(BinkClipboard.IsPayload(text) ? text! : _clipboard ?? string.Empty);
    }

    /// <summary>
    /// Pastes copied elements onto the active layer and selects them, as one undoable step. A repeated paste of the
    /// same copy steps down and to the right, and a paste that would land outside the view lands in its middle.
    /// </summary>
    /// <param name="payload">A <see cref="BinkClipboard"/> payload, or a whole saved document.</param>
    /// <param name="position">Where the centre of the pasted content goes, in document units, or null.</param>
    /// <returns>The number of elements added; zero when the payload is not Bink content or was refused.</returns>
    public int Paste(string payload, Vector2? position = null)
    {
        if (ReadOnly || string.IsNullOrEmpty(payload)) return 0;
        var options = new BinkDeserializerOptions { Limits = _document.Limits };
        if (!BinkClipboard.TryDeserialize(payload, out IReadOnlyList<BinkElement> elements, out string? error, options) || elements.Count == 0)
        {
            if (error is not null) Announce("Nothing could be pasted: " + error);
            return 0;
        }

        BinkRect bounds = BinkClipboard.Bounds(elements);
        BinkRect visible = _viewport.VisibleRect(ViewSize);
        Vector2 offset = Vector2.Zero;
        if (position is { } at) offset = at - bounds.Center;
        else
        {
            // The same copy pasted again steps away from the last one, a screen distance whatever the zoom.
            if (payload == _clipboard) offset = new Vector2(++_pasteCount * 16f / _viewport.Zoom);
            if (!visible.IsEmpty && !bounds.Offset(offset).Intersects(visible)) offset = visible.Center - bounds.Center;
        }
        elements = BinkClipboard.Offset(elements, offset);

        Guid layer = ActiveLayer;
        var ops = new List<BinkDrawingOperation>(elements.Count);
        foreach (BinkElement element in elements) ops.Add(new BinkAddElementOperation(layer, element));
        try
        {
            _document.Apply(new BinkBatchOperation(ops, "Paste"));
        }
        catch (InvalidOperationException ex)
        {
            Announce("Nothing could be pasted: " + ex.Message);
            return 0;
        }
        _selection.Set(elements.Select(static e => e.Id));
        Announce($"Pasted {elements.Count} elements.");
        InvalidateAll();
        return elements.Count;
    }

    /// <summary>
    /// Duplicates the selected elements a short distance away and selects the copies, as one undoable step.
    /// </summary>
    /// <param name="offset">How far the copies move, in document units; null steps them down and to the right.</param>
    /// <returns>The number of copies made.</returns>
    public int DuplicateSelection(Vector2? offset = null)
    {
        if (ReadOnly || _selection.IsEmpty) return 0;
        IReadOnlyList<BinkElement> copies;
        try
        {
            copies = _document.DuplicateElements(_selection.Ids, offset ?? new Vector2(16f / _viewport.Zoom));
        }
        catch (InvalidOperationException ex)
        {
            Announce("The selection could not be duplicated: " + ex.Message);
            return 0;
        }
        _selection.Set(copies.Select(static e => e.Id));
        Announce($"Duplicated {copies.Count} elements.");
        InvalidateAll();
        return copies.Count;
    }

    /// <summary>Changes the stacking order of the selected elements within their layers, as one undoable step.</summary>
    /// <returns>True when anything moved.</returns>
    public bool ArrangeSelection(BinkArrangement arrangement)
    {
        if (ReadOnly || _selection.IsEmpty) return false;
        if (!_document.Arrange(_selection.Ids, arrangement)) return false;
        Announce(arrangement switch
        {
            BinkArrangement.BringToFront => "Brought to front.",
            BinkArrangement.BringForward => "Brought forward.",
            BinkArrangement.SendBackward => "Sent backward.",
            _ => "Sent to back.",
        });
        InvalidateAll();
        return true;
    }

    /// <summary>
    /// Exports the drawing as SVG. This is synchronous work: SVG generation happens entirely in .NET and needs
    /// nothing from the browser.
    /// </summary>
    public string ExportSvg(BinkSvgExportOptions? options = null)
        => BinkSvgExporter.Export(_document, ApplySelection(options ?? BinkSvgExportOptions.Default));

    /// <summary>
    /// Exports the drawing as SVG. Provided for symmetry with <see cref="ExportPngAsync"/>; the work itself is
    /// synchronous, so <see cref="ExportSvg"/> is equivalent and avoids a needless state machine.
    /// </summary>
    public Task<string> ExportSvgAsync(BinkSvgExportOptions? options = null) => Task.FromResult(ExportSvg(options));

    private BinkSvgExportOptions ApplySelection(BinkSvgExportOptions options)
        => options.Region == BinkExportRegion.Selection && options.SelectedIds is null
            ? options with { SelectedIds = _selection.Ids }
            : options;

    /// <summary>
    /// Exports the drawing as a PNG. Rendering happens in the browser, so real fonts and every image format the
    /// browser supports are included.
    /// </summary>
    /// <param name="options">Region, scale and background.</param>
    /// <param name="cancellationToken">Cancels the export.</param>
    public Task<byte[]> ExportPngAsync(BinkPngExportOptions? options = null, CancellationToken cancellationToken = default)
        => ExportImageAsync(BinkImageFormat.Png, options, cancellationToken: cancellationToken);

    /// <summary>
    /// Exports the drawing as a PNG, JPEG or WebP image, encoded by the browser. JPEG has no transparency, so a
    /// transparent background is exported as white.
    /// </summary>
    /// <param name="format">The image format.</param>
    /// <param name="options">Region, scale and background.</param>
    /// <param name="quality">Quality from 0 to 1 for JPEG and WebP; ignored for PNG.</param>
    /// <param name="cancellationToken">Cancels the export.</param>
    /// <exception cref="InvalidOperationException">A JPEG or WebP export was asked for before the browser layer attached; only PNG can be produced without it.</exception>
    /// <exception cref="NotSupportedException">The browser cannot encode the format (Safari has no WebP encoder).</exception>
    public async Task<byte[]> ExportImageAsync(BinkImageFormat format, BinkPngExportOptions? options = null, double quality = 0.92, CancellationToken cancellationToken = default)
    {
        options ??= BinkPngExportOptions.Default;
        // Without an attached surface the browser has nothing to render into: a failed or blocked attach would
        // otherwise return an empty array, or throw from the module, instead of using the managed rasterizer.
        if (_interop is null || !_attached)
        {
            if (format != BinkImageFormat.Png) throw new InvalidOperationException("JPEG and WebP are encoded by the browser, which is not attached. Wait for the Ready event, or export a PNG.");
            return BinkPngExporter.Export(_document, options.Region == BinkExportRegion.Selection && options.SelectedIds is null
                ? options with { SelectedIds = _selection.Ids }
                : options);
        }
        if (format == BinkImageFormat.Jpeg && (options.Background ?? _document.Settings.Background).IsTransparent)
        {
            options = options with { Background = BinkColor.White };
        }

        BinkRect region = ResolveExportRegion(options);
        float scale = options.Scale > 0 ? options.Scale : 1f;
        int width = Math.Max((int)MathF.Ceiling(region.Width * scale), 1);
        int height = Math.Max((int)MathF.Ceiling(region.Height * scale), 1);
        if ((long)width * height > options.MaxPixels)
            throw new InvalidOperationException($"Exporting {width} by {height} pixels exceeds the limit of {options.MaxPixels} pixels. Lower the scale or export a smaller region.");

        // Pre-sizing avoids repeated growth on a typical export. It is capped because the buffer grows on demand
        // anyway, and sizing it from the element count alone would put a very large document straight on the
        // large object heap before a single command has been recorded.
        var context = new BinkCanvasRenderContext(Math.Clamp(_document.ElementCount * 64, 4096, 1 << 20));
        BinkColor background = options.Background ?? _document.Settings.Background;
        if (!background.IsTransparent) context.Clear(background);
        context.SetTransform(Matrix3x2.CreateTranslation(-region.X, -region.Y) * Matrix3x2.CreateScale(scale));
        // An empty selection filters nothing, exactly as BinkPngExporter treats it: the renderer takes any non-null
        // filter as "draw only these", so passing an empty one produced a blank image for the same options that
        // draw the whole document before the surface attaches.
        IReadOnlyCollection<Guid>? filter = options.SelectedIds ?? (options.Region == BinkExportRegion.Selection ? _selection.Ids : null);
        BinkDocumentRenderer.Render(_document, context, new BinkRenderOptions
        {
            ClipBounds = region,
            IncludeHiddenLayers = options.IncludeHiddenLayers,
            ElementFilter = filter is { Count: > 0 } ? filter : null,
        });

        await EnsureImagesLoadedAsync(cancellationToken).ConfigureAwait(true);
        string mimeType = format switch
        {
            BinkImageFormat.Jpeg => "image/jpeg",
            BinkImageFormat.Webp => "image/webp",
            _ => "image/png",
        };
        byte[] bytes = await _interop.RenderToImageAsync(width, height, context, mimeType, Math.Clamp(quality, 0d, 1d), cancellationToken).ConfigureAwait(true);
        // A browser that cannot encode a format quietly produces a PNG instead, which would then be saved or sent
        // under the wrong name.
        if (format != BinkImageFormat.Png && bytes is [0x89, (byte)'P', (byte)'N', (byte)'G', ..])
            throw new NotSupportedException($"This browser cannot encode {format} images.");
        return bytes;
    }

    /// <summary>Exports the drawing as an image and offers it to the user as a file download.</summary>
    /// <exception cref="InvalidOperationException">The browser layer is not available, so nothing can be downloaded.</exception>
    public async Task SaveImageAsync(string fileName, BinkImageFormat format, BinkPngExportOptions? options = null, double quality = 0.92, CancellationToken cancellationToken = default)
    {
        RequireBrowser();
        byte[] bytes = await ExportImageAsync(format, options, quality, cancellationToken).ConfigureAwait(true);
        string mimeType = format switch
        {
            BinkImageFormat.Jpeg => "image/jpeg",
            BinkImageFormat.Webp => "image/webp",
            _ => "image/png",
        };
        if (bytes.Length > 0) await _interop!.SaveFileAsync(fileName, bytes, mimeType).ConfigureAwait(true);
    }

    private BinkRect ResolveExportRegion(BinkPngExportOptions options)
    {
        if (options.Region == BinkExportRegion.Selection && options.SelectedIds is null)
        {
            BinkRect bounds = _selection.Bounds;
            return bounds.IsEmpty ? options.ResolveRegion(_document) : bounds.Inflate(options.Padding);
        }
        return options.ResolveRegion(_document);
    }

    /// <summary>Exports the drawing as a PNG and offers it to the user as a file download.</summary>
    /// <exception cref="InvalidOperationException">The browser layer is not available, so nothing can be downloaded.</exception>
    public async Task SavePngAsync(string fileName = "drawing.png", BinkPngExportOptions? options = null, CancellationToken cancellationToken = default)
    {
        RequireBrowser();
        byte[] bytes = await ExportPngAsync(options, cancellationToken).ConfigureAwait(true);
        if (bytes.Length > 0) await _interop!.SaveFileAsync(fileName, bytes, "image/png").ConfigureAwait(true);
    }

    /// <summary>Exports the drawing as SVG and offers it to the user as a file download.</summary>
    /// <exception cref="InvalidOperationException">The browser layer is not available, so nothing can be downloaded.</exception>
    public async Task SaveSvgAsync(string fileName = "drawing.svg", BinkSvgExportOptions? options = null)
    {
        RequireBrowser();
        string svg = ExportSvg((options ?? BinkSvgExportOptions.Default) with { IncludeXmlDeclaration = true });
        await _interop!.SaveFileAsync(fileName, System.Text.Encoding.UTF8.GetBytes(svg), "image/svg+xml").ConfigureAwait(true);
    }

    /// <summary>
    /// Throws when the browser layer is unavailable. Downloading is the one operation that cannot degrade
    /// quietly: silently doing nothing would look to the user like a broken button.
    /// </summary>
    private void RequireBrowser()
    {
        // The interop object exists from the first render on, whether or not the module ever loaded, so its mere
        // presence would let a blocked module turn every save into a silent no-op.
        if (_interop is not { IsModuleLoaded: true } || _disposed)
        {
            throw new InvalidOperationException(
                AttachFailed is { } reason
                    ? "The canvas could not attach to the browser, so the file cannot be saved: " + reason
                    : "The canvas is not attached to a browser yet. Wait for the Ready event before saving a file.");
        }
    }

    /// <summary>
    /// Decodes an image in the browser without adding it to the document, and returns the source with its natural
    /// size filled in. Use it before a history group or batch that places the image: the decode is a browser
    /// round trip, and a group left open across one disables undo until it closes.
    /// </summary>
    /// <param name="source">The image.</param>
    /// <returns>The source with its natural size, or the source unchanged when the browser layer is not available.</returns>
    public async Task<BinkImageSource> LoadImageAsync(BinkImageSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (_interop is null || source.IsOmitted) return source;
        // Loaded even when the size is already known: the canvas can only draw an image the browser has
        // decoded, and loading it first means it appears with the element rather than a frame later.
        BinkImageMetrics? metrics = await _interop.LoadImageAsync(source.Uri).ConfigureAwait(true);
        return metrics is not null && (source.Width == 0 || source.Height == 0) ? source.WithSize(metrics.Width, metrics.Height) : source;
    }

    /// <summary>Adds an image to the document, sized to its natural dimensions.</summary>
    /// <param name="source">The image.</param>
    /// <param name="position">Top-left position in document units, or null to center it in the view.</param>
    /// <param name="maxWidth">Maximum width in document units; larger images are scaled down.</param>
    public async Task<BinkImageElement?> AddImageAsync(BinkImageSource source, Vector2? position = null, float maxWidth = 600f)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (ReadOnly) return null;

        BinkImageSource resolved = await LoadImageAsync(source).ConfigureAwait(true);

        Vector2 topLeft = position ?? _viewport.ToDocument(ViewSize / 2f);
        BinkImageElement element = BinkImageElement.AtNaturalSize(resolved, topLeft, maxWidth);
        if (position is null) element = element with { Rect = element.Rect.Offset(-element.Rect.Size / 2f) };

        _document.AddElement(element, ActiveLayer);
        Announce("Image added.");
        InvalidateAll();
        return element;
    }

    /// <summary>Selects every element on unlocked, visible layers.</summary>
    public void SelectAll()
    {
        _selection.SelectAll();
        Announce($"{_selection.Count} elements selected.");
    }

    /// <summary>Requests a redraw of the whole canvas.</summary>
    public void Refresh() => InvalidateAll();

    /// <summary>Announces a message to screen readers through the canvas live region.</summary>
    public void Announce(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;
        // An identical string would not change the live region's text node, and screen readers announce changes.
        // Repeating an action (erasing three strokes twice) must not fall silent, so the text is alternated with
        // a trailing space that is invisible to a reader but is a change to the DOM.
        string text = _announcement is not null && _announcement.TrimEnd() == message ? message + ' ' : message;
        _announcement = text;
        _uiDirty = true;
        ScheduleRender();
    }

    // ---- Lifecycle --------------------------------------------------------------------------------------------

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        AttachDocument(Document ?? new BinkDocument());
        _activeTool = Tool ?? new BinkDrawTool();
        _previousToolParameter = Tool;
        AttachTextTool();
        _activeTool.Activate(_toolContext);
        _cursor = _activeTool.Cursor;
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (Document is not null && !ReferenceEquals(Document, _previousDocumentParameter))
        {
            _previousDocumentParameter = Document;
            if (!ReferenceEquals(Document, _document))
            {
                // A gesture in flight refers to the old document's elements and history; finish it first.
                CancelActiveGesture();
                AttachDocument(Document);
            }
            InvalidateAll();
        }

        if (Tool is not null && !ReferenceEquals(Tool, _previousToolParameter))
        {
            _previousToolParameter = Tool;
            SetTool(Tool);
        }

        if (Brush is not null && !Equals(Brush, _brush))
        {
            _brush = Brush;
            _ = UpdateWetInkStyleAsync();
        }
        if (ShapeStyle is not null) _shapeStyle = ShapeStyle;
        if (ActiveLayerId is { } layerId && _document.TryGetLayer(layerId, out _)) _activeLayerId = layerId;
        // Only follow the parameter when the parent actually changed it. Comparing against the current viewport
        // instead would revert the user's own panning every time the parent re-rendered.
        if (Viewport != _previousViewportParameter)
        {
            _previousViewportParameter = Viewport;
            if (Viewport != default && Viewport != _viewport)
            {
                SetViewport(Viewport);
            }
        }
        if (_history.Limit != HistoryLimit) _history.Limit = HistoryLimit;
        _touchGesture.MinZoom = ZoomFloor;
        _touchGesture.MaxZoom = ZoomCeiling;

        // The browser layer was configured from these parameters at attach and acts on them synchronously inside
        // its own event handlers, so a change has to reach it too.
        if (_attached)
        {
            _ = UpdateOptionsAsync();
            _ = UpdateWetInkStyleAsync();
        }
    }

    private BinkInteropOptions BuildInteropOptions() => new()
    {
        RejectTouchWhilePen = RejectTouchWhilePen,
        HandleKeyboard = EnableKeyboardShortcuts,
        TrackHover = false,
        WetInk = WetInk ?? !OperatingSystem.IsBrowser(),
        WheelMode = (int)WheelMode,
        Clipboard = EnableClipboard && EnableKeyboardShortcuts,
        Predict = PredictInk ?? OperatingSystem.IsBrowser(),
    };

    private async Task UpdateOptionsAsync()
    {
        if (_interop is null || _disposed) return;
        BinkInteropOptions options = BuildInteropOptions();
        if (options == _optionsSent) return;
        _optionsSent = options;
        try
        {
            await _interop.SetOptionsAsync(options).ConfigureAwait(true);
        }
        catch (JSDisconnectedException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
        catch (TaskCanceledException)
        {
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _firstRenderDone = true;
            await AttachAsync().ConfigureAwait(true);
        }
        if (_focusTextEditor && _textEditor is not null)
        {
            _focusTextEditor = false;
            try
            {
                await _textEditorElement.FocusAsync().ConfigureAwait(true);
            }
            catch (JSDisconnectedException)
            {
            }
            catch (JSException)
            {
            }
        }
    }

    private async Task AttachAsync()
    {
        if (_disposed) return;
        _interop = new BinkInterop(JSRuntime, this, _elementId, Navigation.BaseUri);
        try
        {
            BinkInteropOptions options = BuildInteropOptions();
            _optionsSent = options;
            BinkAttachResult? result = await _interop.AttachAsync(_container, options).ConfigureAwait(true);
            if (_disposed) return;

            // A host that cannot run the module (a blocked script, a prerender pass, a test double) reports
            // nothing. The canvas then stays inert rather than throwing and taking the page down with it.
            if (result is null)
            {
                _capabilities = BinkInputCapabilities.None;
                return;
            }

            _capabilities = result.ToCapabilities();
            _cssWidth = result.Width;
            _cssHeight = result.Height;
            _devicePixelRatio = result.DevicePixelRatio <= 0 ? 1 : result.DevicePixelRatio;
            _attached = true;

            await _interop.SetSizeAsync(_cssWidth, _cssHeight, _devicePixelRatio).ConfigureAwait(true);
            if (_disposed) return;
            await _interop.SetCursorAsync(_cursor).ConfigureAwait(true);
            // Parameters that changed while the attach was in flight were not sent, because it had not finished.
            await UpdateOptionsAsync().ConfigureAwait(true);
            await UpdateWetInkStyleAsync().ConfigureAwait(true);
            if (_disposed) return;
            InvalidateAll();
            await RenderDirtyAsync().ConfigureAwait(true);

            if (Ready.HasDelegate) await Ready.InvokeAsync(_capabilities).ConfigureAwait(true);
        }
        catch (JSDisconnectedException)
        {
            // The circuit closed while attaching; nothing to do.
        }
        catch (TaskCanceledException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
        catch (JSException ex)
        {
            // The module failed to load or threw. Drawing is unavailable, but the application keeps running.
            AttachFailed = ex.Message;
            _capabilities = BinkInputCapabilities.None;
            await ReportUnavailableAsync(ex.Message).ConfigureAwait(true);
        }
    }

    /// <summary>
    /// Tells the application, and anyone using a screen reader, that drawing is unavailable. Ready is never raised
    /// in this case, so without this an application waiting for it to show the reason would wait forever.
    /// </summary>
    private async Task ReportUnavailableAsync(string reason)
    {
        if (_disposed) return;
        try
        {
            // Back on the renderer's dispatcher: the failure surfaces in a continuation that may not be on it.
            await InvokeAsync(async () =>
            {
                StateHasChanged();
                if (Unavailable.HasDelegate) await Unavailable.InvokeAsync(reason).ConfigureAwait(true);
            }).ConfigureAwait(true);
        }
        catch (ObjectDisposedException)
        {
            // The renderer went away first.
        }
    }

    /// <summary>
    /// Ends whatever the active tool is doing, without committing it. Called whenever the canvas is about to
    /// change underneath a gesture: a stranded gesture would hold pointer capture and an open history group,
    /// which silently disables undo.
    /// </summary>
    private void CancelActiveGesture()
    {
        if (_activeTool is null) return;
        _activeTool.Deactivate(_toolContext);
        _activeTool.Activate(_toolContext);
        _history?.EndOpenGroups();
        _overlayDirty = true;
    }

    /// <summary>
    /// The reason the browser layer could not be attached, or null when everything is working. Applications can
    /// surface this to explain why the canvas is not responding.
    /// </summary>
    public string? AttachFailed { get; private set; }

    private void AttachDocument(BinkDocument document)
    {
        if (_document is not null) _document.Changed -= OnDocumentChanged;
        // Detach before disposing: disposing a history manager closes any open group, which can push an entry and
        // raise Changed. Handling that while the fields are mid-swap would report the old manager's state.
        if (_history is not null) _history.Changed -= OnHistoryChanged;
        if (_selection is not null) _selection.Changed -= OnSelectionChanged;
        _history?.Dispose();
        _selection?.Dispose();

        _document = document;
        // Order matters: history and selection must observe the document before this component does, so that by
        // the time the host is notified of a change, undo state and the selection already reflect it. Subscribing
        // the component first would make every host re-render one step stale.
        _history = new BinkHistoryManager(document, HistoryLimit);
        _history.Changed += OnHistoryChanged;
        _selection = new BinkSelection(document);
        _selection.Changed += OnSelectionChanged;
        _document.Changed += OnDocumentChanged;
        _activeLayerId = ActiveLayerId is { } id && document.TryGetLayer(id, out _) ? id : document.ActiveLayerCandidate.Id;
    }

    private void OnDocumentChanged(object? sender, BinkDocumentChangedEventArgs e)
    {
        if (_watchForAddedStroke) TrackAddedStroke(e.Operation);
        _documentDirty = true;
        _uiDirty = true;
        ScheduleRender();
        RaiseAsync(() => DocumentChanged.InvokeAsync(e), DocumentChanged.HasDelegate);
    }

    private void OnHistoryChanged(object? sender, EventArgs e)
    {
        _uiDirty = true;
        ScheduleRender();
        RaiseAsync(() => HistoryChanged.InvokeAsync(), HistoryChanged.HasDelegate);
    }

    private void OnSelectionChanged(object? sender, EventArgs e)
    {
        _overlayDirty = true;
        _uiDirty = true;
        ScheduleRender();
        RaiseAsync(() => SelectionChanged.InvokeAsync(), SelectionChanged.HasDelegate);
    }

    /// <summary>
    /// Raises an event callback on the renderer's dispatcher. Engine events can originate from a JavaScript
    /// callback or a background continuation, and invoking a callback off the dispatcher throws silently.
    /// </summary>
    private void RaiseAsync(Func<Task> raise, bool hasDelegate)
    {
        if (!hasDelegate || _disposed) return;
        try
        {
            _ = InvokeAsync(async () =>
            {
                try
                {
                    await raise().ConfigureAwait(true);
                }
                catch (ObjectDisposedException)
                {
                }
                catch (JSDisconnectedException)
                {
                }
            });
        }
        catch (ObjectDisposedException)
        {
            // The renderer is being torn down. There is nobody left to raise the event to.
        }
    }

    /// <summary>Replaces the active tool.</summary>
    public void SetTool(IBinkTool tool)
    {
        ArgumentNullException.ThrowIfNull(tool);
        if (ReferenceEquals(tool, _activeTool)) return;
        DetachTextTool();
        _activeTool.Deactivate(_toolContext);
        _activeTool = tool;
        AttachTextTool();
        _activeTool.Activate(_toolContext);
        _overlayDirty = true;
        _ = SetCursorAsync(tool.Cursor);
        ScheduleRender();
    }

    private async Task SetCursorAsync(string cursor)
    {
        if (_cursor == cursor) return;
        _cursor = cursor;
        if (_interop is not null) await _interop.SetCursorAsync(cursor).ConfigureAwait(true);
    }

    private Guid ActiveLayer
    {
        get
        {
            if (_document.TryGetLayer(_activeLayerId, out BinkLayer? layer) && !layer.IsLocked && layer.IsVisible) return _activeLayerId;
            return _document.ActiveLayerCandidate.Id;
        }
    }

    // ---- Input ------------------------------------------------------------------------------------------------

    void IBinkInteropClient.OnSamples(ReadOnlySpan<double> data, int count)
    {
        if (_disposed) return;

        // A read-only canvas still pans and zooms: only tools that change the document are blocked. Touch
        // gestures are still routed, because they only move the viewport.
        bool toolBlocked = ReadOnly && _activeTool.ModifiesDocument;
        if (toolBlocked && !EnableTouchGestures)
        {
            CancelActiveGesture();
            return;
        }
        int stride = BinkInputSample.PackedSize;
        bool wantsAll = _activeTool.WantsCoalescedInput;

        for (int i = 0; i < count; i++)
        {
            int offset = i * stride;
            if (offset + stride > data.Length) break;
            BinkInputSample sample = BinkInputSample.ReadFrom(data.Slice(offset, stride));
            _capabilities = _capabilities.Observe(sample);
            if (sample.IsPredicted)
            {
                // Only a drawing tool knows a prediction from a measured point. Anything else, a gesture included,
                // would act on where the pointer is not.
                if (!toolBlocked && !_gestureRouting && _activeTool is BinkDrawTool) _activeTool.OnPointerMove(_toolContext, sample);
                continue;
            }
            // A tool that does not want intermediate samples still needs the last one before an up event,
            // so only interior coalesced move samples are dropped.
            if (!wantsAll && sample.IsCoalesced && sample.Phase == BinkPointerPhase.Move && i < count - 1) continue;
            if (EnableTouchGestures && RouteTouchGesture(sample)) continue;
            if (toolBlocked) continue;
            Dispatch(sample);
        }

        _ = RenderDirtyAsync();
    }

    /// <summary>
    /// Handles the second finger. A single contact belongs to whatever tool is active, but once a second one
    /// arrives the gesture is almost always a pan or a pinch, whatever the tool is. The in-flight tool gesture is
    /// cancelled and every remaining touch sample goes to an internal pan tool until the last finger lifts.
    /// </summary>
    /// <returns>True when the sample was consumed by the gesture and must not reach the active tool.</returns>
    private bool RouteTouchGesture(in BinkInputSample sample)
    {
        if (sample.Kind != BinkPointerKind.Touch) return false;

        switch (sample.Phase)
        {
            case BinkPointerPhase.Down:
                _touchPoints[sample.PointerId] = sample.Position;
                if (!_gestureRouting && _touchPoints.Count >= 2)
                {
                    // Whatever the first finger started is abandoned rather than left half drawn.
                    CancelActiveGesture();
                    _gestureRouting = true;
                    _touchGesture.Activate(_toolContext);
                }
                break;
            case BinkPointerPhase.Move:
                if (_touchPoints.ContainsKey(sample.PointerId)) _touchPoints[sample.PointerId] = sample.Position;
                break;
            case BinkPointerPhase.Up:
            case BinkPointerPhase.Cancel:
                _touchPoints.Remove(sample.PointerId);
                break;
        }

        if (!_gestureRouting) return false;

        switch (sample.Phase)
        {
            case BinkPointerPhase.Down:
                _touchGesture.OnPointerDown(_toolContext, sample);
                break;
            case BinkPointerPhase.Move:
                _touchGesture.OnPointerMove(_toolContext, sample);
                break;
            case BinkPointerPhase.Up:
                _touchGesture.OnPointerUp(_toolContext, sample);
                break;
            case BinkPointerPhase.Cancel:
                _touchGesture.OnPointerCancel(_toolContext, sample);
                break;
        }

        // The gesture ends when the last finger lifts, not when it drops back to one: continuing to pan with the
        // remaining finger is what every touch application does, and switching back to drawing mid-gesture would
        // leave a stray mark.
        if (_touchPoints.Count == 0)
        {
            _gestureRouting = false;
            _touchGesture.Deactivate(_toolContext);
        }
        return true;
    }

    private void Dispatch(in BinkInputSample sample)
    {
        switch (sample.Phase)
        {
            case BinkPointerPhase.Down:
                // Every tool takes the gesture on pointer down; only a drawing tool starts a stroke. The eraser is
                // a drawing tool too, but StrokeCompleted ignores eraser strokes, and a start without a completion
                // would mislead a host that pairs the two.
                if (_activeTool.OnPointerDown(_toolContext, sample) && _activeTool is BinkDrawTool and not BinkEraserTool)
                {
                    BinkStrokeEventArgs started = new(null, sample.Kind);
                    RaiseAsync(() => StrokeStarted.InvokeAsync(started), StrokeStarted.HasDelegate);
                }
                break;
            case BinkPointerPhase.Move:
            case BinkPointerPhase.Hover:
                _activeTool.OnPointerMove(_toolContext, sample);
                break;
            case BinkPointerPhase.Up:
                HandlePointerUp(sample);
                break;
            case BinkPointerPhase.Cancel:
                _activeTool.OnPointerCancel(_toolContext, sample);
                break;
        }
    }

    private void HandlePointerUp(in BinkInputSample sample)
    {
        // The document reports exactly which element an operation added, so the event carries the right stroke
        // rather than whichever element happens to be last in some layer.
        _lastAddedStroke = null;
        _watchForAddedStroke = StrokeCompleted.HasDelegate;
        try
        {
            _activeTool.OnPointerUp(_toolContext, sample);
        }
        finally
        {
            _watchForAddedStroke = false;
        }

        if (_lastAddedStroke is { } stroke)
        {
            BinkStrokeEventArgs completed = new(stroke, sample.Kind);
            _lastAddedStroke = null;
            RaiseAsync(() => StrokeCompleted.InvokeAsync(completed), StrokeCompleted.HasDelegate);
        }
    }

    /// <summary>Records the stroke an operation added, so <see cref="StrokeCompleted"/> reports the real element.</summary>
    private void TrackAddedStroke(BinkDrawingOperation operation)
    {
        switch (operation)
        {
            case BinkAddElementOperation { Element: BinkStroke { IsEraser: false } stroke }:
                _lastAddedStroke = stroke;
                break;
            case BinkBatchOperation batch:
                foreach (BinkDrawingOperation inner in batch.Operations) TrackAddedStroke(inner);
                break;
        }
    }

    void IBinkInteropClient.OnResized(double width, double height, double devicePixelRatio)
    {
        if (_disposed) return;
        bool changed = Math.Abs(width - _cssWidth) > 0.5 || Math.Abs(height - _cssHeight) > 0.5 || Math.Abs(devicePixelRatio - _devicePixelRatio) > 0.001;
        if (!changed) return;
        _cssWidth = width;
        _cssHeight = height;
        _devicePixelRatio = devicePixelRatio <= 0 ? 1 : devicePixelRatio;
        _capabilities = _capabilities with { DevicePixelRatio = _devicePixelRatio };
        _ = ResizeAsync();
    }

    void IBinkInteropClient.OnRedrawRequested()
    {
        if (_disposed) return;
        // The browser decoded an image the last frame had to skip, or restored a discarded canvas context. The
        // pixels are gone or incomplete but nothing in the model changed, so only a repaint is needed.
        InvalidateAll();
    }

    void IBinkInteropClient.OnResourcesLost()
    {
        if (_disposed) return;
        // A frame referred to a path or string the browser does not hold. That should never happen, but a frame
        // that silently drew nothing for it would be worse than a full redraw that defines everything again.
        _resources.Clear();
        InvalidateAll();
    }

    private async Task ResizeAsync()
    {
        if (_interop is null || _disposed) return;
        await _interop.SetSizeAsync(_cssWidth, _cssHeight, _devicePixelRatio).ConfigureAwait(true);
        InvalidateAll();
        await RenderDirtyAsync().ConfigureAwait(true);
    }

    void IBinkInteropClient.OnKey(string key, bool ctrl, bool shift, bool alt)
    {
        if (_disposed || !EnableKeyboardShortcuts) return;

        // A read-only canvas still pans and zooms, so the view shortcuts stay live; only editing is refused. Copying
        // changes nothing, so it stays live too.
        if (ReadOnly)
        {
            switch (key)
            {
                case "0" when ctrl: ResetViewport(); break;
                case "9" when ctrl: ZoomToFit(); break;
                case "+" or "=" when ctrl: ZoomBy(1.25f); break;
                case "-" when ctrl: ZoomBy(0.8f); break;
                case "c" or "C" when ctrl && !alt: _ = CopyFromKeyboardAsync(); break;
                case "x" or "X" when ctrl && !alt: _ = ProvideClipboardAsync(null); break;
                case "Escape": _selection.Clear(); break;
                default: return;
            }
            _ = RenderDirtyAsync();
            return;
        }

        var keyEvent = new BinkToolKeyEvent(key, ctrl, shift, alt);
        if (_activeTool.OnKeyDown(_toolContext, keyEvent))
        {
            _ = RenderDirtyAsync();
            return;
        }

        switch (key)
        {
            case "z" or "Z" when ctrl && !shift:
                Undo();
                break;
            case "z" or "Z" when ctrl && shift:
            case "y" or "Y" when ctrl:
                Redo();
                break;
            case "a" or "A" when ctrl:
                SelectAll();
                break;
            case "Delete" or "Backspace":
                DeleteSelection();
                break;
            case "Escape":
                _selection.Clear();
                break;
            case "0" when ctrl:
                ResetViewport();
                break;
            case "9" when ctrl:
                ZoomToFit();
                break;
            case "+" or "=" when ctrl:
                ZoomBy(1.25f);
                break;
            case "-" when ctrl:
                ZoomBy(0.8f);
                break;
            case "c" or "C" when ctrl && !alt:
                _ = CopyFromKeyboardAsync();
                break;
            case "x" or "X" when ctrl && !alt:
                _ = CutFromKeyboardAsync();
                break;
            case "d" or "D" when ctrl && !alt:
                DuplicateSelection();
                break;
            // Shift turns ] and [ into } and { on most layouts, so both spellings are matched.
            case "]" or "}" when ctrl:
                ArrangeSelection(shift || key == "}" ? BinkArrangement.BringToFront : BinkArrangement.BringForward);
                break;
            case "[" or "{" when ctrl:
                ArrangeSelection(shift || key == "{" ? BinkArrangement.SendToBack : BinkArrangement.SendBackward);
                break;
            default:
                return;
        }

        _ = RenderDirtyAsync();
    }

    // The browser layer opened a clipboard write for this key press and waits for the payload; it has to be settled
    // either way, or the write would hang until it times out.
    private Task CopyFromKeyboardAsync()
    {
        if (!EnableClipboard) return Task.CompletedTask;
        string? payload = CopySelectionCore();
        if (payload is not null) Announce($"Copied {_selection.Count} elements.");
        return ProvideClipboardAsync(payload);
    }

    private async Task CutFromKeyboardAsync()
    {
        if (!EnableClipboard) return;
        string? payload = CopySelectionCore();
        await ProvideClipboardAsync(payload).ConfigureAwait(true);
        if (payload is null || _disposed) return;
        int count = _selection.Count;
        _document.RemoveElements(_selection.Ids);
        Announce($"Cut {count} elements.");
        InvalidateAll();
    }

    void IBinkInteropClient.OnWheel(float panX, float panY, float zoom, float x, float y)
    {
        if (_disposed || WheelMode == BinkWheelMode.None) return;
        BinkViewport viewport = _viewport;
        if (panX != 0 || panY != 0) viewport = viewport.Pan(new Vector2(-panX, -panY));
        if (zoom != 0) viewport = viewport.ZoomBy(MathF.Exp(zoom), new Vector2(x, y), ZoomFloor, ZoomCeiling);
        SetViewport(viewport);
        _ = RenderDirtyAsync();
    }

    async Task IBinkInteropClient.OnPasteAsync(string kind, string? text, IJSStreamReference? data, string? mediaType)
    {
        if (_disposed || ReadOnly || !EnableClipboard)
        {
            if (data is not null) await data.DisposeAsync().ConfigureAwait(true);
            return;
        }
        try
        {
            switch (kind)
            {
                case "image" when data is not null:
                {
                    byte[] bytes = await BinkInterop.ReadBytesAsync(data, MaxPasteBytes).ConfigureAwait(true);
                    if (_disposed) return;
                    BinkImageSource source;
                    try
                    {
                        // Raster types only: an SVG is a document that can reference other resources.
                        source = BinkImageSource.FromBytes(bytes, (mediaType ?? string.Empty).ToLowerInvariant(), 0, 0);
                    }
                    catch (ArgumentException)
                    {
                        Announce("That image type cannot be pasted.");
                        return;
                    }
                    await InvokeAsync(() => AddImageAsync(source)).ConfigureAwait(true);
                    return;
                }
                case "bink" or "text":
                {
                    string content = data is not null ? await BinkInterop.ReadTextAsync(data, MaxPasteBytes).ConfigureAwait(true) : text ?? string.Empty;
                    if (_disposed) return;
                    await InvokeAsync(() => PasteText(content)).ConfigureAwait(true);
                    return;
                }
                default:
                    if (data is not null) await data.DisposeAsync().ConfigureAwait(true);
                    await InvokeAsync(() => Paste(_clipboard ?? string.Empty)).ConfigureAwait(true);
                    return;
            }
        }
        catch (IOException)
        {
            Announce("That is too large to paste.");
        }
        catch (JSDisconnectedException)
        {
        }
    }

    private async Task PasteText(string content)
    {
        if (BinkClipboard.IsPayload(content))
        {
            Paste(content);
            return;
        }
        if (string.IsNullOrWhiteSpace(content)) return;
        if (TextPasted.HasDelegate)
        {
            await TextPasted.InvokeAsync(content).ConfigureAwait(true);
            return;
        }
        // Plain text becomes a text element in the middle of the view, styled like the text tool's when it is active.
        if (content.Length > BinkTextElement.MaxLength) content = content[..BinkTextElement.MaxLength];
        BinkTextStyle style = _activeTool is BinkTextTool textTool ? textTool.Style : BinkTextStyle.Default with { Color = _brush.Color };
        var element = new BinkTextElement(_viewport.ToDocument(ViewSize / 2f), content) { Style = style };
        element = element with { Position = element.Position - element.LocalBounds.Size / 2f };
        try
        {
            _document.AddElement(element, ActiveLayer);
        }
        catch (InvalidOperationException ex)
        {
            Announce("The text could not be pasted: " + ex.Message);
            return;
        }
        _selection.Set(element.Id);
        Announce("Text pasted.");
        InvalidateAll();
    }

    // ---- Text editing --------------------------------------------------------------------------------------------

    /// <summary>State of the inline text editor while a text element is being edited.</summary>
    private sealed record TextEditorState(Guid ElementId, string Text, string Style);

    /// <summary>
    /// Text editing is the one interaction a canvas cannot own. A real text control is needed for selection,
    /// input methods, autocorrect, dictation and screen readers, so <see cref="BinkTextTool"/> asks the component to
    /// place one over the element and the component reports the result back.
    /// </summary>
    private void AttachTextTool()
    {
        if (_activeTool is not BinkTextTool tool) return;
        tool.EditRequested += OnTextEditRequested;
        tool.EditFinished += OnTextEditFinished;
    }

    private void DetachTextTool()
    {
        if (_activeTool is not BinkTextTool tool) return;
        tool.EditRequested -= OnTextEditRequested;
        tool.EditFinished -= OnTextEditFinished;
        // Unsubscribed before the tool is deactivated, so the EditFinished that would have closed the editor never
        // arrives here. The markup has to be refreshed explicitly or the textarea stays on screen.
        if (_textEditor is not null) _uiDirty = true;
        _textEditor = null;
    }

    private void OnTextEditRequested(BinkTextEditRequest request)
    {
        _textEditor = new TextEditorState(request.Element.Id, request.Element.Text, BuildEditorStyle(request));
        _focusTextEditor = true;
        _uiDirty = true;
        _ = InvokeAsync(StateHasChanged);
    }

    private string BuildEditorStyle(BinkTextEditRequest request)
    {
        BinkTextStyle style = request.Element.Style;
        BinkRect box = request.ViewRect;
        float fontSize = style.FontSize * _viewport.Zoom;
        string family = SanitizeFontFamily(style.FontFamily);
        string weight = style.Bold ? "bold " : string.Empty;
        string italic = style.Italic ? "italic " : string.Empty;
        var culture = System.Globalization.CultureInfo.InvariantCulture;

        return string.Create(culture,
            $"left:{box.X:0.##}px;top:{box.Y:0.##}px;width:{MathF.Max(box.Width, fontSize * 4):0.##}px;height:{MathF.Max(box.Height, fontSize * style.LineHeight):0.##}px;font:{italic}{weight}{fontSize:0.##}px {family};line-height:{fontSize * style.LineHeight:0.##}px;color:{style.Color.ToCss()};");
    }

    /// <summary>
    /// Keeps a font family to characters that cannot end a CSS declaration. The value is written into the inline
    /// style of the editor, so a family containing a semicolon or a url() would inject declarations of its own.
    /// Documents loaded through <see cref="BinkSerializer"/> are already filtered; this covers styles
    /// an application builds from its own user input.
    /// </summary>
    private static string SanitizeFontFamily(string family)
    {
        if (string.IsNullOrWhiteSpace(family)) return "sans-serif";
        if (family.Length > 200) family = family[..200];
        Span<char> buffer = family.Length <= 256 ? stackalloc char[family.Length] : new char[family.Length];
        int n = 0;
        foreach (char c in family)
        {
            if (char.IsLetterOrDigit(c) || c is ' ' or ',' or '-' or '_' or '.') buffer[n++] = c;
        }
        ReadOnlySpan<char> cleaned = buffer[..n].Trim();
        return cleaned.IsEmpty ? "sans-serif" : new string(cleaned);
    }

    private void OnTextEditFinished()
    {
        _textEditor = null;
        _uiDirty = true;
        _ = InvokeAsync(async () =>
        {
            StateHasChanged();
            await RestoreFocusAsync().ConfigureAwait(true);
        });
    }

    /// <summary>
    /// Returns focus to the canvas after the inline editor closes. Without this the textarea is removed from the
    /// DOM and focus falls to the document body, leaving every canvas shortcut dead until the user tabs back.
    /// </summary>
    private async Task RestoreFocusAsync()
    {
        if (_disposed || !Focusable || !_attached) return;
        try
        {
            await _container.FocusAsync(preventScroll: true).ConfigureAwait(true);
        }
        catch (JSDisconnectedException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
        catch (InvalidOperationException)
        {
            // The element reference is gone because the component is being removed. Nothing to focus.
        }
    }

    /// <summary>
    /// Handles a commit, cancel or blur from the inline editor. The value comes from the browser rather than
    /// from a framework binding, so what is saved is exactly what the user sees in the field.
    /// </summary>
    void IBinkInteropClient.OnTextEditorKey(string key, string value)
    {
        if (_disposed || _textEditor is null || _activeTool is not BinkTextTool tool) return;

        if (key == "Escape")
        {
            tool.CancelEdit(_toolContext);
            _textEditor = null;
            _uiDirty = true;
            ScheduleRender();
            _ = InvokeAsync(RestoreFocusAsync);
            return;
        }

        _textEditor = _textEditor with { Text = value };
        _ = InvokeAsync(() => CommitTextAsync(tool, value));
    }

    private async Task CommitTextAsync(BinkTextTool tool, string value)
    {
        Vector2? measured = await MeasureTextAsync(value, tool).ConfigureAwait(true);
        // Measuring is a browser round trip. On Blazor Server the user can navigate away during it, and writing
        // to a disposed document and history manager afterwards would throw on a fire-and-forget task.
        if (_disposed) return;
        tool.CommitEdit(_toolContext, value, measured);
        _textEditor = null;
        _uiDirty = true;
        await RenderDirtyAsync().ConfigureAwait(true);
        await RestoreFocusAsync().ConfigureAwait(true);
    }

    private async Task<Vector2?> MeasureTextAsync(string text, BinkTextTool tool)
    {
        if (_interop is null || string.IsNullOrEmpty(text) || tool.Editing is null) return null;
        BinkTextStyle style = tool.Editing.Style;
        BinkTextMetrics? metrics = await _interop.MeasureTextAsync(text, BinkCanvasRenderContext.ToCssFont(style)).ConfigureAwait(true);
        if (metrics is null) return null;
        return new Vector2((float)metrics.Width, Math.Max(metrics.Lines, 1) * style.FontSize * style.LineHeight);
    }

    // ---- Rendering --------------------------------------------------------------------------------------------

    private void InvalidateAll()
    {
        _documentDirty = true;
        _overlayDirty = true;
        ScheduleRender();
    }

    private void ScheduleRender()
    {
        // Gated on the first render rather than on a successful attach: a canvas whose browser module was blocked
        // still has to re-render its own markup, which is where the accessible description, the inline text editor
        // and the live region live. Gating on _attached made that path unreachable and left them frozen.
        if (_renderScheduled || _disposed || !_firstRenderDone) return;
        _renderScheduled = true;
        // Coalesces bursts of programmatic changes into one redraw without touching Blazor's render cycle.
        _ = InvokeAsync(async () =>
        {
            _renderScheduled = false;
            await RenderDirtyAsync().ConfigureAwait(true);
        });
    }

    private async Task RenderDirtyAsync()
    {
        if (_disposed) return;
        if (!_documentDirty && !_overlayDirty && !_uiDirty) return;

        if (_interop is null || !_attached)
        {
            // Nothing can be painted, but the component still has to re-render: the inline text editor, the
            // accessible description and the AttachFailed message all live in the component's own markup, and a
            // canvas whose module was blocked would otherwise be silent as well as blank.
            _documentDirty = false;
            _overlayDirty = false;
            if (!_uiDirty) return;
            _uiDirty = false;
            await InvokeAsync(StateHasChanged).ConfigureAwait(true);
            return;
        }

        try
        {
            if (_documentDirty)
            {
                _documentDirty = false;
                RecordDocument();
                await _interop.ExecuteAsync(BinkCanvasLayer.Document, _documentContext).ConfigureAwait(true);
            }
            if (_overlayDirty)
            {
                _overlayDirty = false;
                RecordOverlay();
                await _interop.ExecuteAsync(BinkCanvasLayer.Overlay, _overlayContext).ConfigureAwait(true);
            }
            if (_selectionActiveSent != !_selection.IsEmpty)
            {
                // The browser layer decides synchronously, inside the key event, whether Delete and the arrow
                // keys belong to the canvas or to the page. It can only do that if it knows about the selection.
                _selectionActiveSent = !_selection.IsEmpty;
                await _interop.SetSelectionActiveAsync(_selectionActiveSent).ConfigureAwait(true);
            }
            if (_uiDirty)
            {
                // The accessible description and anything bound to the document must follow the model. This
                // happens at most once per frame, and never for a plain pointer move.
                _uiDirty = false;
                StateHasChanged();
            }
        }
        catch (JSDisconnectedException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
        catch (TaskCanceledException)
        {
        }
    }

    private Matrix3x2 DeviceTransform => _viewport.DocumentToView * Matrix3x2.CreateScale((float)_devicePixelRatio);

    private void RecordDocument()
    {
        BinkCanvasRenderContext context = _documentContext;
        context.Reset();
        context.Clear(_document.Settings.Background);
        context.SetTransform(DeviceTransform);

        BinkRect visible = _viewport.VisibleRect(ViewSize);
        BinkDocumentRenderer.Render(_document, context, new BinkRenderOptions { ClipBounds = visible });
        context.EndFrame();
    }

    private void RecordOverlay()
    {
        BinkCanvasRenderContext context = _overlayContext;
        context.Reset();
        context.Clear(BinkColor.Transparent);
        context.SetTransform(DeviceTransform);
        _activeTool.DrawOverlay(_toolContext, context);
    }

    /// <summary>
    /// Pushes the provisional-trail style to the browser. Called whenever the brush or the zoom changes, not only
    /// at attach: the trail is drawn by JavaScript with whatever style it was last told, so a stale one would
    /// draw the previous colour and width until the round trip to the server replaced it.
    /// </summary>
    private async Task UpdateWetInkStyleAsync()
    {
        if (_interop is null || _disposed) return;
        bool enabled = WetInk ?? !OperatingSystem.IsBrowser();
        BinkColor? color = enabled ? _brush.Color : null;
        float width = enabled ? MathF.Max(_brush.Width * _viewport.Zoom * 0.8f, 1f) : 0f;
        float opacity = enabled ? _brush.Opacity : 0f;
        if (_wetInkSent == (color, width, opacity)) return;
        _wetInkSent = (color, width, opacity);
        try
        {
            await _interop.SetWetInkStyleAsync(color, width, opacity).ConfigureAwait(true);
        }
        catch (JSDisconnectedException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
        catch (TaskCanceledException)
        {
        }
    }

    private async Task EnsureImagesLoadedAsync(CancellationToken cancellationToken)
    {
        if (_interop is null) return;
        foreach (BinkElement element in _document.AllElements)
        {
            if (element is BinkImageElement { Source.IsOmitted: false } image) await _interop.LoadImageAsync(image.Source.Uri, cancellationToken).ConfigureAwait(true);
        }
    }

    private string ContainerClass => string.Join(' ', new[] { "bit-bink-canvas", Class }.Where(static c => !string.IsNullOrWhiteSpace(c)));

    private string ContainerStyle
    {
        get
        {
            string touchAction = EnableTouchGestures ? "none" : "pan-x pan-y";
            return $"width:{Width};height:{Height};touch-action:{touchAction};{Style}";
        }
    }

    /// <summary>
    /// Builds the text exposed to assistive technology. Handwriting itself cannot be described automatically, so
    /// this reports the structure and any descriptions the application attached to elements.
    /// </summary>
    private string GetContentDescription()
    {
        // A canvas that cannot draw is still focusable; saying why is the difference between broken and silent.
        if (AttachFailed is { } reason) return "Drawing is unavailable: " + reason;
        if (_document is null) return string.Empty;
        if (_document.IsEmpty) return "Empty drawing canvas.";

        var described = new List<string>();
        int total = _document.ElementCount;
        const int maxDescribed = 25;
        foreach (BinkElement element in _document.AllElements)
        {
            if (described.Count >= maxDescribed) break;
            // Every element contributes something: its own description when the application supplied one, and
            // otherwise the structural summary the element knows how to produce.
            described.Add(element.GetAccessibleDescription());
        }

        string summary = $"Drawing with {total} elements on {_document.Layers.Count} layers.";
        if (described.Count == 0) return summary;
        string listed = summary + " " + string.Join(". ", described) + ".";
        return total > described.Count ? listed + $" {total - described.Count} more elements not listed." : listed;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        DetachTextTool();
        _activeTool?.Deactivate(_toolContext);
        if (_document is not null) _document.Changed -= OnDocumentChanged;
        if (_history is not null)
        {
            _history.Changed -= OnHistoryChanged;
            _history.Dispose();
        }
        if (_selection is not null)
        {
            _selection.Changed -= OnSelectionChanged;
            _selection.Dispose();
        }
        if (_interop is not null) await _interop.DisposeAsync().ConfigureAwait(true);
        _interop = null;
    }

    /// <summary>The tool context handed to tools. Kept private so tools cannot reach the component itself.</summary>
    private sealed class ToolContext(BinkCanvas canvas) : IBinkToolContext
    {
        public BinkDocument Document => canvas._document;

        public BinkSelection Selection => canvas._selection;

        public BinkHistoryManager History => canvas._history;

        public BinkViewport Viewport
        {
            get => canvas._viewport;
            set => canvas.SetViewport(value);
        }

        public Guid ActiveLayerId => canvas.ActiveLayer;

        public BinkBrush Brush => canvas._brush;

        public BinkShapeStyle ShapeStyle => canvas._shapeStyle;

        public Vector2 ViewSize => canvas.ViewSize;

        public BinkSnapOptions Snapping => canvas.Snapping ?? BinkSnapOptions.None;

        public void InvalidateOverlay()
        {
            canvas._overlayDirty = true;
        }

        public void InvalidateDocument(BinkRect? region = null)
        {
            canvas._documentDirty = true;
        }

        public void CapturePointer(int pointerId)
        {
            // The browser layer captures on pointer down already; tools call this to express intent.
        }

        public void ReleasePointer(int pointerId)
        {
        }

        public void SetCursor(string cursor) => _ = canvas.SetCursorAsync(cursor);

        public void Announce(string message) => canvas.Announce(message);
    }
}

/// <summary>Information about a stroke event.</summary>
/// <param name="Stroke">The stroke, once it exists. Null when a stroke has only just started.</param>
/// <param name="Device">The device that drew it.</param>
public readonly record struct BinkStrokeEventArgs(BinkStroke? Stroke, BinkPointerKind Device);

/// <summary>What the mouse wheel and a trackpad do over a <see cref="BinkCanvas"/>.</summary>
public enum BinkWheelMode : byte
{
    /// <summary>Nothing: the page scrolls as usual. Right for a canvas embedded in a form.</summary>
    None = 0,

    /// <summary>
    /// The wheel and two-finger trackpad scrolling pan the view; Ctrl (Command on macOS) with the wheel, and a
    /// trackpad pinch, zoom around the pointer. Shift with the wheel pans sideways. This is the convention of design
    /// tools and whiteboards.
    /// </summary>
    PanAndZoom = 1,

    /// <summary>The wheel zooms around the pointer; Shift with the wheel pans. This is the convention of maps.</summary>
    Zoom = 2,
}

/// <summary>An image format the browser encodes for <see cref="BinkCanvas.ExportImageAsync"/>.</summary>
public enum BinkImageFormat : byte
{
    /// <summary>Lossless, with transparency. Also available without a browser, through <see cref="BinkPngExporter"/>.</summary>
    Png = 0,
    /// <summary>Lossy and small, without transparency.</summary>
    Jpeg = 1,
    /// <summary>Lossy or lossless with transparency. Not encoded by Safari.</summary>
    Webp = 2,
}
