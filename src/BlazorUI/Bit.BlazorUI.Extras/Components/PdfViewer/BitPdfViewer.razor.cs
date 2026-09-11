using System.Net.Http;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Forms;

namespace Bit.BlazorUI;

/// <summary>
/// BitPdfViewer is a pure-C# PDF viewer component with a full toolbar (page navigation, zoom,
/// fit modes, rotation, search, download, print, fullscreen and optional thumbnail and bookmark sidebars).
/// The rendering pipeline emits plain HTML DOM per page (vector graphics as
/// &lt;div&gt; clip-paths, selectable text as &lt;span&gt;, rasters as &lt;img&gt;).
/// </summary>
public partial class BitPdfViewer : BitComponentBase
{
    private BitPdfSource? _source;
    // The last value the Source PARAMETER carried, kept apart from _source so a
    // document opened from the toolbar survives a host re-render.
    private BitPdfSource? _sourceParam;
    private BitPdfDocument? _document;
    private BitPdfFontStore? _fontStore;
    // The resolved bytes of the current document, kept after the load so Download
    // and the properties dialog work for URL sources too (not just in-memory ones).
    private byte[]? _bytes;
    private bool _correctWidthsPending; // run the JS text width-correction after render
    private string?[]? _pageText; // lazily-built per-page text index for search
    private int _loadVersion; // bumped per load; guards against a superseded load committing
    private int _renderEpoch; // bumped whenever page slots are rebuilt (load, rotation, mode change)
    private string _status = string.Empty;
    private bool _loading;

    // Serializes page/thumbnail renders so a BackgroundRendering build (which may run
    // on a worker thread) never runs concurrently with another render against the
    // shared document and font store.
    private readonly SemaphoreSlim _renderGate = new(1, 1);

    // A UI-thread snapshot of the document-wide @font-face CSS. Rendering mutates the
    // font store's builder (possibly on a worker thread); the UI reads only this
    // snapshot, refreshed after each render, so it never reads the builder while a
    // background render is mutating it.
    private string _fontFaceStyle = string.Empty;

    // One slot per page. A null slot is a not-yet-rendered page shown as a
    // light placeholder; it is rendered on demand when it nears the viewport.
    private readonly List<MarkupString?> _pages = [];
    // Pages whose build threw. Their slots stay empty, so without this set the
    // scroll-driven pump would ask for them again on every pass - re-running a failing
    // render and re-raising OnError forever. Cleared whenever the slots are rebuilt.
    private readonly HashSet<int> _failedPages = [];
    private readonly HashSet<int> _failedThumbs = [];
    private readonly List<double> _pageWidths = [];  // points, display orientation
    private readonly List<double> _pageHeights = [];

    // Pages waiting to be lazily rendered, drained one page per event-loop turn
    // by the pump in EnsurePagesRendered so scrolling stays responsive on WASM.
    // A linked list (not a Queue) so each incoming viewport batch can be inserted
    // ahead of older pending work: the newest batch reflects where the viewport is
    // NOW and must not wait behind pages queued for a viewport already scrolled past.
    private readonly LinkedList<int> _renderQueue = new();
    // Maps a queued index to its list node so a page re-appearing in a newer viewport
    // batch can be promoted (removed from its stale position and re-inserted at the
    // front) in O(1), not just deduplicated in place.
    private readonly Dictionary<int, LinkedListNode<int>> _renderQueued = new();
    private bool _renderPumpActive;
    private bool _printing; // suspends page eviction while Print() catches up all pages

    // The thumbnail sidebar's counterpart of the render queue/pump.
    private readonly LinkedList<int> _thumbQueue = new();
    private readonly Dictionary<int, LinkedListNode<int>> _thumbQueued = new(); // index -> node, as _renderQueued
    private bool _thumbPumpActive;

    // The thumbnail sidebar owns its own render slots, decoupled from _pages, so
    // it can lazy-render only the thumbnails scrolled into the sidebar viewport
    // instead of mirroring whatever the main surface happens to have rendered.
    // A null slot is a not-yet-rendered thumbnail placeholder.
    private readonly List<MarkupString?> _thumbs = [];

    private BitPdfZoomMode _zoomMode = BitPdfZoomMode.FitWidth;
    private BitPdfTextCoalescing _textCoalescing; // last applied; changes re-render pages
    private BitPdfRenderMode _renderMode;         // last applied; changes re-render pages
    // Layout state. These start from their parameters but the toolbar (and the
    // public API) can move them afterwards, so they are held rather than read.
    private BitPdfScrollMode _scrollMode;
    private BitPdfSpreadMode _spreadMode;
    private BitPdfCursorTool _cursorTool;
    // The last values the PARAMETERS carried, so a host re-render that does not
    // change them cannot undo a choice the reader made in the toolbar.
    private BitPdfScrollMode _scrollModeParam;
    private BitPdfSpreadMode _spreadModeParam;
    private BitPdfCursorTool _cursorToolParam;
    // The last values the viewer itself put into the two-way parameters, so a set
    // that did NOT come from the viewer is recognized as the host driving it.
    private int _appliedPage = 1;
    private double _appliedZoom = 1;
    private int _appliedRotation;

    // Canvas mode: per-page display lists, and the pages whose freshly (re)created
    // canvases still need a JS replay after the current render.
    private readonly Dictionary<int, string> _canvasOps = [];
    private readonly List<int> _canvasDirty = [];
    private double _paintedZoom = 1; // zoom the canvases were last rasterized at
    // A canvas-mode print parks here until OnAfterRenderAsync has actually painted the
    // freshly rendered canvases, so the print dialog never opens over blank pages. Also
    // completed on disposal so a parked print can't hang.
    private TaskCompletionSource? _canvasPaintSignal;
    // Generation of canvas-paint work: CommitPage bumps _canvasDirtyGen when a canvas is
    // dirtied; OnAfterRenderAsync advances _canvasPaintedGen to the generation it
    // snapshotted only after that paint's interop completes. The parked print records the
    // generation it needs in _canvasPaintSignalGen, so an earlier or empty render pass
    // (which never advances _canvasPaintedGen) can't release it before its canvases land.
    private int _canvasDirtyGen;
    private int _canvasPaintedGen;
    private int _canvasPaintSignalGen;
    private bool _showThumbnails;
    private bool _showOutline;
    private bool _showAttachments;
    private bool _showLayers;
    private IReadOnlyList<BitPdfOutlineItem> _outline = [];
    private IReadOnlyList<BitPdfAttachment> _attachments = [];
    private IReadOnlyList<BitPdfLayer> _layers = [];
    // The layers the reader has switched off. Non-null once a document declares any,
    // so the renderer knows to consult it rather than the document's own default.
    private HashSet<string>? _hiddenLayers;
    // A frozen copy of the above, replaced (never mutated) on every change, so a
    // background page build can read it without racing the UI thread.
    private IReadOnlySet<string>? _hiddenLayersView;

    private bool _showSearch;
    private string _searchQuery = "";
    private bool _matchCase;  // find option: case-sensitive matching
    private bool _wholeWord;  // find option: match whole words only
    // find option: when off, a letter and its accented form match each other. Off by
    // default, which is what a reader typing on a plain keyboard expects.
    private bool _matchDiacritics;
    // find option: paint every match, not just the one being walked to. On by default,
    // as in every desktop viewer's find bar.
    private bool _highlightAll = true;
    private int _searchTotal;
    private int _searchIndex = -1;
    private int _searchGeneration; // bumped per query so an in-flight search abandons when a newer query starts
    private int[]? _matchCounts;   // matches per page for the current query, counted in C#
    private bool _highlightPending; // re-paint the highlights after a page render brought new text in

    private bool _showProperties; // the document-properties dialog
    private ElementReference _propertiesRef;
    private bool _focusPropertiesPending; // move focus into the dialog once it is in the DOM
    private bool _propertiesTrapped;      // focus is inside the dialog and owed back to its opener

    // Whether the viewer currently fills the screen, mirrored from the browser's own
    // fullscreenchange event so the toolbar button can report its state.
    private bool _fullscreen;

    // The polite live region's text. Screen readers announce page moves from here:
    // the page box is an <input>, and changing its value announces nothing.
    private string _announcement = string.Empty;

    // Presentation mode, and the layout it replaced so leaving restores it.
    private bool _presenting;
    private BitPdfScrollMode _presentingScrollMode;
    private BitPdfZoomMode _presentingZoomMode;
    private BitPdfSidebar _presentingSidebar;

    // The built-in password dialog. The load parks on this completion source while
    // the reader types; submitting or cancelling completes it.
    private TaskCompletionSource<string?>? _passwordRequest;
    private string _passwordInput = "";
    private bool _passwordRejected; // a password was supplied and turned out wrong
    private ElementReference _passwordInputRef;
    private ElementReference _passwordDialogRef;
    private bool _focusPasswordPending;
    private bool _passwordTrapped;        // focus is inside the dialog and owed back to its opener

    private DotNetObjectReference<BitPdfViewer>? _dotnetObj;
    private ElementReference _containerRef;
    private ElementReference _thumbsRef;
    private ElementReference _searchInputRef;
    private bool _spyPending;
    // A wheel/pinch zoom stashed an anchor point in JS; put it back after the pages
    // have been laid out at the new scale.
    private bool _zoomAnchorPending;
    private bool _thumbSpyPending; // (re)attach the sidebar's lazy-render spy after render
    private bool _keyboardPending = true; // (re)attach the keyboard-shortcut listener after render
    private bool _keyboardAttached;
    private bool _dropZonePending = true; // (re)attach the drag-and-drop listeners after render
    private bool _dropZoneAttached;
    private bool _focusSearchPending; // focus the find box once it is in the DOM



    [Inject] private IJSRuntime _js { get; set; } = default!;

    [Inject] private IServiceProvider _services { get; set; } = default!;



    /// <summary>
    /// The document to display.
    /// </summary>
    [Parameter] public BitPdfSource? Source { get; set; }

    /// <summary>
    /// Custom CSS classes for the different parts of the viewer.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitPdfViewerClassStyles? Classes { get; set; }

    /// <summary>
    /// Custom CSS styles for the different parts of the viewer.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public BitPdfViewerClassStyles? Styles { get; set; }

    /// <summary>
    /// The texts of the viewer UI. Defaults to English; assign a
    /// <see cref="BitPdfViewerTexts"/> with the properties you want to override
    /// to localize the toolbar, the sidebars and the status messages.
    /// </summary>
    [Parameter] public BitPdfViewerTexts? Texts { get; set; }

    /// <summary>
    /// The CSS height of the viewer container. When not set, the viewer height is
    /// responsive: capped at 780px and shrinking to fit the viewport on small screens.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public string? Height { get; set; }

    /// <summary>
    /// The CSS width of the viewer container. When not set, the viewer fills the
    /// width its host gives it.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public string? Width { get; set; }

    /// <summary>
    /// Whether the toolbar is shown.
    /// </summary>
    [Parameter] public bool ShowToolbar { get; set; } = true;

    /// <summary>
    /// Which controls the toolbar offers. Combine the flags to build a reduced
    /// toolbar (e.g. navigation and zoom only). Default is
    /// <see cref="BitPdfToolbarItems.All"/>.
    /// </summary>
    [Parameter] public BitPdfToolbarItems ToolbarItems { get; set; } = BitPdfToolbarItems.All;

    /// <summary>
    /// The side panel open when a document first loads. Default is
    /// <see cref="BitPdfSidebar.None"/>. The panel can be changed afterwards from
    /// the toolbar or through <see cref="ShowSidebar"/>.
    /// </summary>
    [Parameter] public BitPdfSidebar DefaultSidebar { get; set; } = BitPdfSidebar.None;

    /// <summary>
    /// The focused page (1-based), two-way bindable. Reading it gives the page the
    /// reader is on; assigning it navigates there.
    /// <br />
    /// Bound one way (without <c>CurrentPageChanged</c>) the page becomes the host's
    /// to control: the viewer then reports scroll and toolbar navigation through
    /// <see cref="OnPageChanged"/> but does not move the value itself.
    /// </summary>
    [Parameter, TwoWayBound] public int CurrentPage { get; set; } = 1;

    /// <summary>
    /// The zoom factor (1 means 100%), two-way bindable. Assigning it switches the
    /// viewer to <see cref="BitPdfZoomMode.Custom"/> and clamps the value to
    /// <see cref="MinZoom"/>..<see cref="MaxZoom"/>.
    /// </summary>
    [Parameter, TwoWayBound] public double Zoom { get; set; } = 1;

    /// <summary>
    /// The rotation applied to every page, in degrees, two-way bindable. Assigned
    /// values are normalized to the nearest quarter turn (0, 90, 180 or 270).
    /// </summary>
    [Parameter, TwoWayBound] public int Rotation { get; set; }

    /// <summary>
    /// The initial zoom behavior.
    /// </summary>
    [Parameter] public BitPdfZoomMode InitialZoomMode { get; set; } = BitPdfZoomMode.FitWidth;

    /// <summary>
    /// How the pages are laid out on the scrollable surface: stacked vertically
    /// (the default), side by side on one horizontally scrolling row, wrapped into
    /// rows that fill the width, or one page (or spread) at a time.
    /// </summary>
    [Parameter] public BitPdfScrollMode ScrollMode { get; set; } = BitPdfScrollMode.Vertical;

    /// <summary>
    /// How pages are paired into spreads, the way a printed book falls open.
    /// Default is <see cref="BitPdfSpreadMode.None"/>.
    /// </summary>
    [Parameter] public BitPdfSpreadMode SpreadMode { get; set; } = BitPdfSpreadMode.None;

    /// <summary>
    /// What dragging on the document surface does: select text (the default) or pan
    /// the document, as the hand tool of a desktop viewer does.
    /// </summary>
    [Parameter] public BitPdfCursorTool CursorTool { get; set; } = BitPdfCursorTool.Select;

    /// <summary>
    /// The smallest zoom factor the viewer allows (1 means 100%). Default is <c>0.1</c>.
    /// </summary>
    [Parameter] public double MinZoom { get; set; } = 0.1;

    /// <summary>
    /// The largest zoom factor the viewer allows (1 means 100%). Default is <c>8</c>.
    /// </summary>
    [Parameter] public double MaxZoom { get; set; } = 8;

    /// <summary>
    /// The multiplier applied by <see cref="ZoomIn"/> and <see cref="ZoomOut"/>
    /// (and by the toolbar's zoom buttons). Default is <c>1.2</c>, i.e. 20% a step.
    /// </summary>
    [Parameter] public double ZoomStep { get; set; } = 1.2;

    /// <summary>
    /// The explicit zoom factors the toolbar's zoom dropdown offers (1 means 100%).
    /// Values outside <see cref="MinZoom"/>..<see cref="MaxZoom"/> are dropped.
    /// Defaults to 50%, 75%, 100%, 125%, 150%, 200%, 300% and 400%.
    /// </summary>
    [Parameter] public IEnumerable<double>? ZoomPresets { get; set; }

    /// <summary>
    /// How many pages stay materialized in the DOM at once. Pages outside the window
    /// centered on the current one revert to placeholders and are re-rendered when
    /// scrolled back to, which is what keeps a long document from growing the DOM
    /// (and a Blazor Server circuit's memory) without bound. Raise it to keep more
    /// pages warm at the cost of memory. Default is <c>24</c>.
    /// </summary>
    [Parameter] public int MaxRenderedPageCount { get; set; } = 24;

    /// <summary>
    /// How many thumbnails stay materialized in the sidebar at once. A thumbnail
    /// fragment is as heavy as a full page, so this bounds the sidebar the way
    /// <see cref="MaxRenderedPageCount"/> bounds the document surface.
    /// Default is <c>40</c>.
    /// </summary>
    [Parameter] public int MaxRenderedThumbnailCount { get; set; } = 40;

    /// <summary>
    /// Whether the viewer handles keyboard shortcuts while it has focus:
    /// page navigation (<c>n</c>/<c>j</c>, <c>p</c>/<c>k</c>, Home/End), zoom
    /// (<c>Ctrl +</c>, <c>Ctrl -</c>, <c>Ctrl 0</c>), rotation (<c>r</c>,
    /// <c>Shift+r</c>), find (<c>Ctrl+F</c>, <c>Ctrl+G</c>, <c>Shift+Ctrl+G</c>),
    /// print (<c>Ctrl+P</c>), download (<c>Ctrl+S</c>), presentation mode (<c>Ctrl+Alt+P</c>)
    /// and the sidebar (<c>F4</c>).
    /// Default is <c>true</c>.
    /// </summary>
    [Parameter] public bool EnableKeyboardShortcuts { get; set; } = true;

    /// <summary>
    /// How painted text is emitted. <see cref="BitPdfTextCoalescing.Compact"/> merges
    /// same-line, same-style runs into one span per visual line - far fewer DOM
    /// nodes on per-glyph PDFs, with small intra-line position drift (explicit
    /// kerning between runs is approximated). Rotated text always stays exact.
    /// Default is <see cref="BitPdfTextCoalescing.Exact"/>.
    /// </summary>
    [Parameter] public BitPdfTextCoalescing TextCoalescing { get; set; } = BitPdfTextCoalescing.Exact;

    /// <summary>
    /// How page content is painted. <see cref="BitPdfRenderMode.Canvas"/> replays a
    /// display list onto a per-page <c>&lt;canvas&gt;</c> (far fewer DOM nodes;
    /// selection/search/links stay DOM), while <see cref="BitPdfRenderMode.Html"/>
    /// (the default) renders prerenderable positioned DOM.
    /// </summary>
    [Parameter] public BitPdfRenderMode RenderMode { get; set; } = BitPdfRenderMode.Html;

    /// <summary>
    /// Offloads document parsing and page rendering to a background thread instead of
    /// running them on the UI thread, so scrolling and navigation stay responsive
    /// while a complex page is being rendered. This only has an effect when the
    /// runtime actually provides a spare thread - Blazor Server, or a Blazor
    /// WebAssembly app built with multi-threading enabled
    /// (<c>&lt;WasmEnableThreads&gt;true&lt;/WasmEnableThreads&gt;</c>). On the default
    /// single-threaded WebAssembly runtime it is a safe no-op: the work still runs on
    /// the one available thread (renders are serialized, so results stay correct).
    /// Default is <c>false</c>.
    /// </summary>
    [Parameter] public bool BackgroundRendering { get; set; }

    /// <summary>
    /// The callback for when a document has finished loading.
    /// </summary>
    [Parameter] public EventCallback OnDocumentLoaded { get; set; }

    /// <summary>
    /// The callback for when a page has been rendered into the document surface,
    /// with its 1-based page number. Lazy rendering means this is raised as the
    /// reader reaches a page, not once per page up front.
    /// </summary>
    [Parameter] public EventCallback<int> OnPageRendered { get; set; }

    /// <summary>
    /// The callback for when the reader picks a file with the toolbar's open-file
    /// button, with the source built from it. Handle it to drive <see cref="Source"/>
    /// yourself; when unset the viewer opens the file on its own.
    /// </summary>
    [Parameter] public EventCallback<BitPdfSource> OnFileOpened { get; set; }

    /// <summary>
    /// The callback for when the focused page changes (with the 1-based page number).
    /// </summary>
    [Parameter] public EventCallback<int> OnPageChanged { get; set; }

    /// <summary>
    /// The callback for when the zoom factor changes (1 means 100%), whatever
    /// caused it: the toolbar, a fit mode, Ctrl+wheel or the public API.
    /// </summary>
    [Parameter] public EventCallback<double> OnZoomChanged { get; set; }

    /// <summary>
    /// The callback for when the page rotation changes, with the new angle in
    /// degrees (0, 90, 180 or 270).
    /// </summary>
    [Parameter] public EventCallback<int> OnRotationChanged { get; set; }

    /// <summary>
    /// The callback for when the open side panel changes.
    /// </summary>
    [Parameter] public EventCallback<BitPdfSidebar> OnSidebarChanged { get; set; }

    /// <summary>
    /// The callback for the download progress of a URL source, as a fraction from
    /// <c>0</c> to <c>1</c>. Only raised when the server declares a content length;
    /// a chunked response has no total to report against.
    /// </summary>
    [Parameter] public EventCallback<double> OnProgress { get; set; }

    /// <summary>
    /// The callback for when loading or rendering fails, with the error message.
    /// </summary>
    [Parameter] public EventCallback<string> OnError { get; set; }

    /// <summary>
    /// The callback raised after a document loads with any non-fatal diagnostics (e.g. the
    /// file was damaged and its cross-reference table had to be rebuilt).
    /// </summary>
    [Parameter] public EventCallback<IReadOnlyList<string>> OnWarnings { get; set; }

    /// <summary>
    /// Invoked when an encrypted document needs a password. Return the password to
    /// retry, or <c>null</c>/empty to cancel. When unset, the viewer's own password
    /// dialog asks instead (see <see cref="ShowPasswordPrompt"/>).
    /// </summary>
    [Parameter] public Func<Task<string?>>? OnPasswordRequested { get; set; }

    /// <summary>
    /// Whether the document's own user access permissions are enforced. With it set,
    /// a document that forbids printing or copying has the corresponding toolbar
    /// control disabled, <see cref="Print()"/> and <see cref="Download"/> refuse, and
    /// its text cannot be selected. Off by default, as in every browser pdf viewer:
    /// the flags are advisory, not a security boundary, and a reader who can open the
    /// file can always read it.
    /// </summary>
    [Parameter] public bool RespectPermissions { get; set; }

    /// <summary>
    /// Whether a pdf dropped onto the viewer opens in it. The dropped file goes
    /// through the same path as the toolbar's open-file button, so
    /// <see cref="OnFileOpened"/> and <see cref="MaxOpenFileSize"/> apply to it too.
    /// Default is <c>false</c>.
    /// </summary>
    [Parameter] public bool AllowDropFile { get; set; }

    /// <summary>
    /// The largest file the toolbar's open-file button - and a drop, when
    /// <see cref="AllowDropFile"/> allows one - accepts, in bytes. Default is
    /// <c>64</c> MB; the whole file is read into memory, and on Blazor Server it also
    /// travels the circuit.
    /// </summary>
    [Parameter] public long MaxOpenFileSize { get; set; } = 64L * 1024 * 1024;

    /// <summary>
    /// Whether the viewer asks for the password of an encrypted document with a
    /// dialog of its own. Ignored when <see cref="OnPasswordRequested"/> is set,
    /// which takes over the asking. Set to <c>false</c> to let a password failure
    /// surface through <see cref="OnError"/> instead. Default is <c>true</c>.
    /// </summary>
    [Parameter] public bool ShowPasswordPrompt { get; set; } = true;



    /// <summary>
    /// The largest factor <see cref="BitPdfZoomMode.Automatic"/> magnifies a page to.
    /// Fit-width below it, capped at it above - so a narrow page stays readable on a
    /// wide screen instead of filling it.
    /// </summary>
    public const double AutomaticZoomCap = 1.25;

    /// <summary>
    /// The number of pages of the current document.
    /// </summary>
    public int PageCount => _pages.Count;

    /// <summary>
    /// The current zoom behavior (fit-width, fit-page, actual size or custom).
    /// </summary>
    public BitPdfZoomMode ZoomMode => _zoomMode;

    /// <summary>
    /// The current page layout on the scrollable surface.
    /// </summary>
    public BitPdfScrollMode CurrentScrollMode => _scrollMode;

    /// <summary>
    /// The current spread pairing.
    /// </summary>
    public BitPdfSpreadMode CurrentSpreadMode => _spreadMode;

    /// <summary>
    /// What dragging on the document surface currently does.
    /// </summary>
    public BitPdfCursorTool CurrentCursorTool => _cursorTool;

    /// <summary>
    /// Which side panel is currently open.
    /// </summary>
    public BitPdfSidebar Sidebar => _showThumbnails ? BitPdfSidebar.Thumbnails
        : _showOutline ? BitPdfSidebar.Bookmarks
        : _showAttachments ? BitPdfSidebar.Attachments
        : _showLayers ? BitPdfSidebar.Layers
        : BitPdfSidebar.None;

    /// <summary>
    /// Whether the document exposes any bookmarks.
    /// </summary>
    public bool HasOutline => _outline.Count > 0;

    /// <summary>
    /// The document outline (bookmarks) as a tree, empty when the document has none.
    /// </summary>
    public IReadOnlyList<BitPdfOutlineItem> Outline => _outline;

    /// <summary>
    /// Whether the document carries any embedded file.
    /// </summary>
    public bool HasAttachments => _attachments.Count > 0;

    /// <summary>
    /// The files embedded in the document (the <c>/EmbeddedFiles</c> name tree plus
    /// any <c>/FileAttachment</c> annotation), empty when it carries none.
    /// </summary>
    public IReadOnlyList<BitPdfAttachment> Attachments => _attachments;

    /// <summary>
    /// Whether the document declares any optional-content group (layer).
    /// </summary>
    public bool HasLayers => _layers.Count > 0;

    /// <summary>
    /// The optional-content groups (layers) the document declares, empty when it
    /// declares none.
    /// </summary>
    public IReadOnlyList<BitPdfLayer> Layers => _layers;

    /// <summary>
    /// Whether the layer is currently painted.
    /// </summary>
    public bool IsLayerVisible(BitPdfLayer layer)
        => layer is not null && _hiddenLayers?.Contains(layer.Id) is not true;

    /// <summary>
    /// Shows or hides an optional-content group and re-renders the pages, the way a
    /// desktop viewer's layers panel does.
    /// </summary>
    public async Task SetLayerVisible(BitPdfLayer layer, bool visible)
    {
        if (layer is null || _hiddenLayers is null) return;

        bool changed = visible ? _hiddenLayers.Remove(layer.Id) : _hiddenLayers.Add(layer.Id);
        if (changed is false) return;

        _hiddenLayersView = new HashSet<string>(_hiddenLayers);

        // Layer visibility is baked into the page fragments, so switching one is a
        // re-render - the same path a rotation takes.
        PreparePages();
        await RenderCurrentPageEagerlyAsync();
        if (IsDisposed) return;

        _spyPending = true;
        Repaint();
    }

    private Task ToggleLayer(BitPdfLayer layer) => SetLayerVisible(layer, IsLayerVisible(layer) is false);

    /// <summary>
    /// Saves an embedded file to the reader's machine.
    /// </summary>
    public async Task DownloadAttachment(BitPdfAttachment attachment)
    {
        if (attachment is null || attachment.Content.Length == 0) return;

        using var stream = new MemoryStream(attachment.Content, writable: false);
        using var streamRef = new DotNetStreamReference(stream, leaveOpen: true);
        await _js.BitPdfViewerDownload(attachment.Name, streamRef);
    }

    /// <summary>
    /// The parsed document model, or <c>null</c> when nothing is loaded. Exposes the
    /// full engine surface (catalog, pages, cross-reference table) for callers that
    /// need more than the viewer's own API.
    /// </summary>
    public BitPdfDocument? Document => _document;

    /// <summary>
    /// The document metadata (<c>/Info</c> fields plus the raw XMP packet), or
    /// <c>null</c> when nothing is loaded.
    /// </summary>
    public BitPdfMetadata? Metadata => _document?.Metadata;

    /// <summary>
    /// The user access permissions of the loaded document. Every permission is
    /// granted for an unencrypted document, and for no document at all.
    /// </summary>
    public BitPdfPermissions Permissions => _document?.Permissions ?? new BitPdfPermissions(-1, false);

    /// <summary>
    /// The document-defined page labels (e.g. "i", "ii", "1", "A-1"), one per page
    /// in document order, or an empty list when nothing is loaded.
    /// </summary>
    public IReadOnlyList<string> PageLabels => _document?.PageLabels ?? [];

    /// <summary>
    /// The interactive form fields (<c>/AcroForm</c>) of the loaded document as a
    /// flat list of name/type/value, or an empty list when there is no form.
    /// </summary>
    public IReadOnlyList<BitPdfFormField> FormFields => _document?.FormFields ?? [];

    /// <summary>
    /// The tagged-PDF logical structure tree of the loaded document, or an empty
    /// list when the document is untagged.
    /// </summary>
    public IReadOnlyList<BitPdfStructElement> StructureTree => _document?.StructureTree ?? [];

    /// <summary>
    /// The PDF version the loaded document declares (e.g. "1.7"), or <c>null</c>.
    /// </summary>
    public string? PdfVersion => _document?.Version;

    /// <summary>
    /// Whether the loaded document declares an encryption dictionary.
    /// </summary>
    public bool IsEncrypted => _document?.IsEncrypted ?? false;

    /// <summary>
    /// The size in bytes of the loaded document, or <c>0</c> when nothing is loaded.
    /// </summary>
    public long FileSize => _bytes?.LongLength ?? 0;

    /// <summary>
    /// The non-fatal diagnostics collected while the current document was parsed
    /// (e.g. a damaged cross-reference table that had to be rebuilt), empty when
    /// there were none or nothing is loaded.
    /// </summary>
    public IReadOnlyList<string> Warnings => _document?.Warnings ?? [];

    /// <summary>
    /// Whether the viewer currently fills the screen.
    /// </summary>
    public bool IsFullscreen => _fullscreen;

    /// <summary>
    /// The 1-based numbers of the pages whose rendering failed, in ascending order.
    /// Such a page shows an error note in place of its content and is not retried
    /// (retrying a build that already threw only repeats the failure); a reload,
    /// rotation or render-mode change gives every page a fresh attempt.
    /// </summary>
    public IReadOnlyList<int> FailedPages => [.. _failedPages.Order().Select(i => i + 1)];

    /// <summary>
    /// Whether the find box is open.
    /// </summary>
    public bool IsSearchOpen => _showSearch;

    /// <summary>
    /// The current find query, or an empty string when nothing is being searched for.
    /// </summary>
    public string SearchQuery => _searchQuery;

    /// <summary>
    /// The 1-based ordinal of the find match the reader is on, or <c>0</c> when
    /// there is no match.
    /// </summary>
    public int SearchMatchIndex => _searchIndex + 1;

    /// <summary>
    /// The number of matches of the current find query, counted over the whole
    /// document (<c>0</c> when there is no query, or no match).
    /// </summary>
    public int SearchMatchCount => _searchTotal;

    /// <summary>
    /// The raw bytes of the loaded document (fetched ones included), or <c>null</c>
    /// when nothing is loaded.
    /// </summary>
    public byte[]? GetBytes() => _bytes;

    /// <summary>
    /// Navigates to the next page.
    /// </summary>
    public Task NextPage() => GoToPage(CurrentPage + 1);

    /// <summary>
    /// Navigates to the previous page.
    /// </summary>
    public Task PrevPage() => GoToPage(CurrentPage - 1);

    /// <summary>
    /// Navigates to the first page.
    /// </summary>
    public Task FirstPage() => GoToPage(1);

    /// <summary>
    /// Navigates to the last page.
    /// </summary>
    public Task LastPage() => GoToPage(_pages.Count);

    /// <summary>
    /// Navigates to the provided page number (1-based).
    /// </summary>
    public async Task GoToPage(int pageNumber)
    {
        if (_pages.Count == 0) return;

        int version = _loadVersion; // a reload during the awaits below supersedes this navigation
        int target = Math.Clamp(pageNumber, 1, _pages.Count);
        if (target != CurrentPage)
        {
            // A host that bound the page one way owns it: the assignment is refused
            // and this navigation stops rather than desynchronizing the two.
            if (await AssignCurrentPage(target) is false) return;
            _appliedPage = CurrentPage;
            AnnouncePage();
            await OnPageChanged.InvokeAsync(CurrentPage);
            // OnPageChanged is user code: a reload (or new Source) during it makes
            // this navigation stale, and a newer GoToPage or a scroll-spy update
            // (OnPageVisible) may have moved CurrentPage on - either way this
            // navigation is superseded, so don't render or scroll for it.
            if (version != _loadVersion || CurrentPage != target) return;
        }

        // Render the destination before scrolling so jumps (toolbar, thumbnails,
        // outline) land on content instead of a placeholder.
        if (await RenderPageAsync(target - 1) && version == _loadVersion && CurrentPage == target)
        {
            EvictDistantPages();
            StateHasChanged();
        }

        // A fit mode follows the page in front of the reader, so a jump onto a page of
        // another size re-fits before the scroll lands on it.
        if (IsDisposed is false && version == _loadVersion && CurrentPage == target)
        {
            await RefitForCurrentPageAsync();
        }

        // RenderPageAsync may have yielded; if the component was disposed, this load
        // was superseded, or a newer navigation moved on in that window, don't drive
        // JS for this stale target. Scroll to the captured target, not the mutable
        // CurrentPage, so a concurrent update can't redirect this call's scroll.
        if (IsDisposed || version != _loadVersion || CurrentPage != target) return;

        await _js.BitPdfViewerScrollToPage(_containerRef, target);
        if (_showThumbnails)
        {
            await ScrollActiveThumbIntoViewAsync();
        }
    }

    /// <summary>
    /// Navigates to a destination: its page, and - when the destination names a
    /// vertical position - that position within the page, so a bookmark pointing at
    /// the middle of a long page lands there instead of at its top.
    /// <br />
    /// The in-page offset is applied only while the pages are unrotated; a rotated
    /// page's coordinates no longer run down the screen, so navigation falls back to
    /// the page itself.
    /// </summary>
    public async Task GoToDestination(BitPdfDestination? destination)
    {
        if (destination?.PageNumber is not int pageNo) return;

        // An XYZ destination may also name the scale to read it at; 0 (or no value)
        // is the spec's "retain the current zoom", which is what most files carry.
        if (destination.Zoom is > 0.01 and var wanted && Math.Abs(wanted - Zoom) > 0.0001)
        {
            await SetZoom(wanted);
        }

        await GoToPage(pageNo);
        if (IsDisposed) return;

        int index = pageNo - 1;
        if (destination.Top is not double top || Rotation != 0
            || index < 0 || index >= _pageHeights.Count) return;

        // PDF coordinates run up from the bottom-left, CSS ones down from the top, so
        // the offset into the page is what is left above the destination.
        double offset = (_pageHeights[index] - top) * Zoom;
        if (offset <= 0.5) return;

        try
        {
            await _js.BitPdfViewerScrollToPageOffset(_containerRef, pageNo, offset);
        }
        catch (JSDisconnectedException) { }
    }

    /// <summary>
    /// Navigates to a named destination (the <c>/Dests</c> entry a link or an
    /// external anchor refers to). Does nothing when the document does not declare it.
    /// </summary>
    public async Task GoToNamedDestination(string name)
    {
        if (_document is null || string.IsNullOrEmpty(name)) return;

        BitPdfDestination? destination = null;
        try
        {
            destination = _document.ResolveDestination(name);
        }
        catch { /* a damaged name tree is not a navigation failure */ }

        await GoToDestination(destination);
    }

    /// <summary>
    /// Zooms in by one <see cref="ZoomStep"/> (20% by default).
    /// </summary>
    public Task ZoomIn() => SetZoom(Zoom * EffectiveZoomStep);

    /// <summary>
    /// Zooms out by one <see cref="ZoomStep"/> (20% by default).
    /// </summary>
    public Task ZoomOut() => SetZoom(Zoom / EffectiveZoomStep);

    /// <summary>
    /// Sets an explicit zoom factor (1 means 100%), switching the viewer to
    /// <see cref="BitPdfZoomMode.Custom"/>. The value is clamped to
    /// <see cref="MinZoom"/>..<see cref="MaxZoom"/>.
    /// </summary>
    public async Task SetZoom(double zoom)
    {
        _zoomMode = BitPdfZoomMode.Custom;
        await SetZoomValueAsync(zoom);
        Repaint();
    }

    /// <summary>
    /// Sets the zoom mode (fit-width, fit-page, actual size or custom).
    /// </summary>
    public async Task SetZoomMode(BitPdfZoomMode mode)
    {
        _zoomMode = mode;
        if (mode == BitPdfZoomMode.ActualSize)
        {
            await SetZoomValueAsync(1.0);
        }
        else
        {
            await ApplyFitAsync();
        }
        Repaint();
    }

    /// <summary>
    /// Changes how the pages are laid out on the scrollable surface.
    /// </summary>
    public async Task SetScrollMode(BitPdfScrollMode mode)
    {
        if (mode == _scrollMode) return;

        _scrollMode = mode;
        // The layout axis changed under the scroll spy: re-register it (and re-fit)
        // so lazy rendering measures against the new geometry.
        _spyPending = true;
        Repaint();
        await GoToPage(CurrentPage);
    }

    /// <summary>
    /// Changes how pages are paired into spreads.
    /// </summary>
    public async Task SetSpreadMode(BitPdfSpreadMode mode)
    {
        if (mode == _spreadMode) return;

        _spreadMode = mode;
        _spyPending = true;
        Repaint();
        await GoToPage(CurrentPage);
    }

    /// <summary>
    /// Changes what dragging on the document surface does.
    /// </summary>
    public void SetCursorTool(BitPdfCursorTool tool)
    {
        if (tool == _cursorTool) return;

        _cursorTool = tool;
        Repaint();
    }

    /// <summary>
    /// Rotates all pages 90 degrees clockwise.
    /// </summary>
    public Task RotateClockwise() => SetRotation(Rotation + 90);

    /// <summary>
    /// Rotates all pages 90 degrees counter-clockwise.
    /// </summary>
    public Task RotateCounterClockwise() => SetRotation(Rotation - 90);

    /// <summary>
    /// Rotates all pages to the given absolute angle in degrees. The value is
    /// normalized to the nearest quarter turn (0, 90, 180 or 270).
    /// </summary>
    public async Task SetRotation(int degrees)
    {
        int normalized = NormalizeRotation(degrees);
        if (normalized == Rotation) return;

        if (await AssignRotation(normalized) is false) return;
        await OnRotationChanged.InvokeAsync(Rotation);
        if (IsDisposed) return;

        await ApplyRotationAsync();
    }

    // Nearest quarter turn, so an off-axis angle lands on the turn it is closest to
    // rather than being truncated toward zero.
    private static int NormalizeRotation(int degrees) => (((int)Math.Round(degrees / 90.0) * 90) % 360 + 360) % 360;

    /// <summary>Re-prepares and re-renders the pages for the angle <see cref="Rotation"/>
    /// ALREADY holds. This is the host-driven half of <see cref="SetRotation"/>, which has
    /// nothing to assign when the parameter arrived carrying the new angle itself.</summary>
    private async Task ApplyRotationAsync()
    {
        // Taken before the await below, so a re-render caused by the assignment does not
        // come back through here for the same angle.
        _appliedRotation = Rotation;

        // Best effort: an off-axis angle from the host is normalized in place when the
        // parameter is two-way bound, and rendered as given when the host owns it.
        int normalized = NormalizeRotation(Rotation);
        if (normalized != Rotation && await AssignRotation(normalized)) _appliedRotation = Rotation;
        if (IsDisposed) return;

        PreparePages();
        await RenderCurrentPageEagerlyAsync();
        _spyPending = true;
        Repaint();
    }

    /// <summary>
    /// Loads a document without going through the <see cref="Source"/> parameter -
    /// what the toolbar's open-file button uses, and what host code can call to swap
    /// the document imperatively. Passing <c>null</c> closes the current one.
    /// <br />
    /// The <see cref="Source"/> parameter still wins: a later host render that changes
    /// it replaces whatever was opened this way.
    /// </summary>
    public async Task OpenAsync(BitPdfSource? source)
    {
        await OpenCoreAsync(source);
        Repaint();
    }

    /// <summary>The shared load path of the <see cref="Source"/> parameter and
    /// <see cref="OpenAsync"/>.</summary>
    private async Task OpenCoreAsync(BitPdfSource? source)
    {
        // REPLACING a document starts it unrotated on page one - a page number
        // belonging to the file just closed means nothing in the new one. The
        // FIRST document is different: a host that opened the viewer at page 12
        // asked for page 12, and resetting it would throw that away.
        bool replacing = _pages.Count > 0;
        _source = source;
        if (replacing)
        {
            // Through the two-way setters, so a bound host sees the reset.
            await AssignRotation(0);
            await AssignCurrentPage(1);
        }
        int wanted = Math.Max(1, CurrentPage);
        _appliedRotation = Rotation;
        _appliedPage = CurrentPage;
        _textCoalescing = TextCoalescing;
        _renderMode = RenderMode;
        await LoadAsync();
        if (IsDisposed) return;

        // The load renders around whatever CurrentPage says, but the requested
        // page may be past the end of a shorter document; land on a real one.
        if (wanted > 1 && _pages.Count > 0)
        {
            await GoToPage(wanted);
            _appliedPage = CurrentPage;
        }
        AnnouncePage();
    }

    /// <summary>Reads the file the reader picked with the toolbar's open-file button.
    /// A host that handles <see cref="OnFileOpened"/> drives <see cref="Source"/>
    /// itself; otherwise the viewer opens it.</summary>
    // Bumped after every pick. An <input type="file"> keeps the file it holds, so
    // choosing the SAME file again raises no change event; keying the element on this
    // counter gives each pick a fresh input, which is what makes a re-pick work.
    private int _filePickerGeneration;

    private async Task OnFilePicked(InputFileChangeEventArgs e)
    {
        _filePickerGeneration++;
        var file = e.FileCount > 0 ? e.File : null;
        if (file is null) return;

        try
        {
            // OpenReadStream caps the size it will hand over, so the cap is the
            // parameter rather than the framework's 512 KB default.
            using var stream = file.OpenReadStream(MaxOpenFileSize);
            var source = await BitPdfSource.FromStreamAsync(stream, file.Name);
            if (IsDisposed) return;

            if (OnFileOpened.HasDelegate)
            {
                await OnFileOpened.InvokeAsync(source);
                return;
            }
            await OpenCoreAsync(source);
        }
        catch (Exception ex)
        {
            if (IsDisposed) return;

            _status = string.Format(ActiveTexts.ErrorFormat, ex.Message);
            _loading = false;
            await OnError.InvokeAsync(ex.Message);
        }
    }

    /// <summary>
    /// Requests a re-render after a state change made through the public API. The
    /// toolbar's own handlers repaint on their own; a call from host code does not,
    /// so every public mutator ends here.
    /// </summary>
    private void Repaint()
    {
        if (IsDisposed) return;

        StateHasChanged();
    }

    /// <summary>
    /// Downloads the original document bytes. Works for URL sources too: the bytes
    /// fetched for the current document are reused, so nothing is downloaded twice.
    /// </summary>
    public async Task Download()
    {
        if (CanDownload is false) return;

        // _bytes holds whatever the current document was parsed from, whether it
        // came in as a buffer or was fetched from a URL.
        byte[]? bytes = _bytes ?? _source?.Bytes;
        if (bytes is null) return;

        // Stream the bytes as a Blob rather than pushing a base64 data: URI (which
        // on Blazor Server would traverse SignalR as one huge string).
        using var stream = new MemoryStream(bytes, writable: false);
        using var streamRef = new DotNetStreamReference(stream, leaveOpen: true);
        await _js.BitPdfViewerDownload(DownloadFileName, streamRef);
    }

    /// <summary>The file name a download is offered under: the source's own name,
    /// then the document title, then a generic fallback.</summary>
    private string DownloadFileName
    {
        get
        {
            string? name = _source?.FileName;
            if (string.IsNullOrWhiteSpace(name))
            {
                string? title = null;
                try
                {
                    title = _document?.Metadata.Title;
                }
                catch { /* a damaged /Info dictionary must not break downloading */ }
                name = string.IsNullOrWhiteSpace(title) ? null : $"{title}.pdf";
            }
            return string.IsNullOrWhiteSpace(name) ? "document.pdf" : name;
        }
    }

    /// <summary>
    /// Opens the browser print dialog with all pages of the document.
    /// </summary>
    public Task Print() => Print(1, _pages.Count);

    /// <summary>
    /// Opens the browser print dialog with just the page the reader is on.
    /// </summary>
    public Task PrintCurrentPage() => Print(CurrentPage, CurrentPage);

    /// <summary>
    /// Opens the browser print dialog with the pages from <paramref name="from"/> to
    /// <paramref name="to"/> inclusive (1-based). The range is clamped to the
    /// document and reordered if it arrives backwards.
    /// </summary>
    public async Task Print(int from, int to)
    {
        if (_document is null || _pages.Count == 0 || CanPrint is false) return;

        if (from > to)
        {
            (from, to) = (to, from);
        }
        from = Math.Clamp(from, 1, _pages.Count);
        to = Math.Clamp(to, 1, _pages.Count);
        // A print pass is already catching up (its yields let this reentrant call
        // in); a second one would race it and clear _printing while it still runs.
        if (_printing) return;

        // Render every page before printing so the output includes all pages, not
        // just the ones scrolled into view. Show progress while catching up. A new
        // Source arriving while this pass yields supersedes it: bail at every yield
        // so a stale print never renders against, or prints, the newer document.
        int version = _loadVersion;
        // A rotation or render-mode change rebuilds the page slots (bumping _renderEpoch)
        // without changing _loadVersion; capture the epoch too so such a change aborts
        // the print rather than letting it render into - or print - cleared slots.
        int epoch = _renderEpoch;
        bool rendered = false;
        // Suspend eviction while catching up: the lazy-render pump can run during
        // the yields below and would otherwise evict pages this pass has already
        // rendered (the loop only moves forward), printing placeholders.
        _printing = true;
        try
        {
            for (int i = from - 1; i < to; i++)
            {
                if (_pages[i] is null)
                {
                    if (!rendered)
                    {
                        _loading = true;
                        _status = ActiveTexts.PreparingPrint;
                        StateHasChanged();
                        await Task.Delay(1);
                        if (IsDisposed || version != _loadVersion || epoch != _renderEpoch) return;
                        rendered = true;
                    }
                    bool ok = await RenderPageAsync(i);
                    // Yield between page renders so the browser can paint the progress
                    // bar and stay responsive while a large document is prepared on the
                    // single WASM thread (mirrors the lazy-render pumps).
                    await Task.Delay(1);
                    if (IsDisposed || version != _loadVersion || epoch != _renderEpoch) return;
                    // A page that failed to build (e.g. malformed) leaves its slot empty
                    // and RenderPageAsync returns false; the failure is surfaced via
                    // OnError. Abort rather than open the print dialog with a blank page
                    // mid-document. A false result whose slot was meanwhile filled by the
                    // lazy pump is fine - only a still-empty slot means a real failure.
                    if (!ok && _pages[i] is null)
                    {
                        _status = ActiveTexts.PrintAborted;
                        return;
                    }
                }
            }
            if (rendered)
            {
                _loading = false;
                int paintTarget = _canvasDirtyGen;
                if (RenderMode == BitPdfRenderMode.Canvas && _canvasPaintedGen < paintTarget)
                {
                    // Canvas pixels are painted by JS in OnAfterRenderAsync, which a
                    // fixed delay cannot reliably outwait while a large document paints.
                    // Park until the paint pass for this exact generation completes, so
                    // no page prints blank (and an earlier/empty pass can't release us).
                    _canvasPaintSignalGen = paintTarget;
                    _canvasPaintSignal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    StateHasChanged();
                    await _canvasPaintSignal.Task;
                }
                else
                {
                    StateHasChanged();
                    await Task.Delay(1); // let the DOM paint the freshly rendered pages
                }
                if (IsDisposed || version != _loadVersion || epoch != _renderEpoch) return;
            }

            // Re-validate immediately before printing: a rotation or mode change during
            // the canvas-paint wait above (which has no other checkpoint) must not print
            // cleared slots.
            if (IsDisposed || version != _loadVersion || epoch != _renderEpoch) return;
            await _js.BitPdfViewerPrint(_containerRef, from, to);
        }
        finally
        {
            // Resume eviction even for superseded or failed prints.
            _printing = false;
            // Print rendered ALL pages with eviction suspended; trim them back to the
            // normal window now that the snapshot is built (EvictDistantPages no-ops
            // until _printing is cleared above). Runs for superseded and failed prints
            // too, so a large document doesn't stay fully materialized in the DOM.
            EvictDistantPages();
            // Only the print that still owns _loadVersion may clear the progress bar
            // and request the trim render; a superseded one must not touch the newer
            // load's state (it repaints on its own).
            if (version == _loadVersion)
            {
                _loading = false;
                StateHasChanged();
            }
        }
    }

    /// <summary>
    /// Toggles the fullscreen mode of the viewer.
    /// </summary>
    public async Task ToggleFullscreen()
    {
        await _js.BitPdfViewerToggleFullscreen(RootElement);
    }

    /// <summary>
    /// Whether the viewer is in presentation mode: fullscreen, one page at a time,
    /// scaled to fit, with the chrome out of the way.
    /// </summary>
    public bool IsPresenting => _presenting;

    /// <summary>
    /// Enters or leaves presentation mode. Entering remembers the layout the reader
    /// had, so leaving - however it happens, including the browser's own Escape -
    /// puts it back.
    /// </summary>
    public Task TogglePresentationMode()
        => _presenting ? ExitPresentationMode() : EnterPresentationMode();

    /// <summary>
    /// Shows the document fullscreen, one page at a time, scaled to fit the screen.
    /// </summary>
    public async Task EnterPresentationMode()
    {
        if (_presenting || _pages.Count == 0) return;

        _presentingScrollMode = _scrollMode;
        _presentingZoomMode = _zoomMode;
        _presentingSidebar = Sidebar;
        _presenting = true;
        ClassBuilder.Reset(); // the presenting modifier is on the root class

        await ShowSidebar(BitPdfSidebar.None);
        await SetScrollMode(BitPdfScrollMode.Page);
        await SetZoomMode(BitPdfZoomMode.FitPage);
        if (IsDisposed) return;

        Repaint();
        // Toggle, not request: asking for fullscreen while the viewer is ALREADY
        // fullscreen would leave it, which is the opposite of entering presentation.
        if (_fullscreen is false)
        {
            await _js.BitPdfViewerToggleFullscreen(RootElement);
        }
    }

    /// <summary>
    /// Leaves presentation mode and restores the layout it replaced.
    /// </summary>
    public async Task ExitPresentationMode()
    {
        if (_presenting is false) return;

        _presenting = false;
        ClassBuilder.Reset();
        await SetScrollMode(_presentingScrollMode);
        await SetZoomMode(_presentingZoomMode);
        await ShowSidebar(_presentingSidebar);
        if (IsDisposed) return;

        Repaint();
        // Only leave fullscreen when we are still in it: an Escape (or the browser's
        // own exit) already did, and this call is the notification's aftermath.
        try
        {
            await _js.BitPdfViewerExitFullscreen();
        }
        catch (JSDisconnectedException) { }
    }

    /// <summary>
    /// Renders a single page (1-based) to self-contained HTML, or an
    /// empty string when no document is loaded or the number is out of range.
    /// </summary>
    public string RenderPageHtml(int pageNumber)
    {
        if (_document is null || pageNumber < 1 || pageNumber > _document.PageCount)
        {
            return string.Empty;
        }
        return new BitPdfHtmlRenderer(_document.Pages[pageNumber - 1], _document.XRef, Rotation)
        {
            TextCoalescing = TextCoalescing,
            // The rendered fragment matches what the viewer is showing, layers included.
            HiddenLayers = _hiddenLayersView,
        }.Render();
    }

    /// <summary>
    /// The text the reader currently has selected in the document, or an empty string
    /// when nothing inside the viewer is selected. Reads the live DOM selection over
    /// the page's text layer, so it is the words the reader sees - in reading order,
    /// and only the ones they actually highlighted.
    /// </summary>
    public async Task<string> GetSelectedText()
    {
        try
        {
            return await _js.BitPdfViewerGetSelectedText(_containerRef) ?? string.Empty;
        }
        catch (JSDisconnectedException)
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Drops the reader's selection inside the document. A selection made elsewhere on
    /// the hosting page is left alone.
    /// </summary>
    public async Task ClearSelection()
    {
        try
        {
            await _js.BitPdfViewerClearSelection(_containerRef);
        }
        catch (JSDisconnectedException) { }
    }

    /// <summary>
    /// Extracts the visible text of a single page (1-based) for search or
    /// copy, or an empty string when unavailable.
    /// </summary>
    public string ExtractPageText(int pageNumber)
    {
        if (_document is null || pageNumber < 1 || pageNumber > _document.PageCount)
        {
            return string.Empty;
        }
        return _document.Pages[pageNumber - 1].ExtractText();
    }

    /// <summary>
    /// Extracts the visible text of the whole document, one page per entry of the
    /// returned sequence joined by <paramref name="pageSeparator"/>. Reuses the
    /// index the find box builds, so a second call after a search is free.
    /// </summary>
    public string ExtractText(string pageSeparator = "\n\n")
    {
        if (_document is null) return string.Empty;

        _pageText ??= new string?[_document.PageCount];
        var builder = new System.Text.StringBuilder();
        for (int i = 0; i < _document.PageCount; i++)
        {
            _pageText[i] ??= _document.Pages[i].ExtractText();
            if (i > 0)
            {
                builder.Append(pageSeparator);
            }
            builder.Append(_pageText[i]);
        }
        return builder.ToString();
    }

    /// <summary>
    /// Opens the given side panel, or closes the open one when
    /// <see cref="BitPdfSidebar.None"/> is passed. Opening the bookmarks panel of a
    /// document without an outline closes the sidebar instead.
    /// </summary>
    public async Task ShowSidebar(BitPdfSidebar sidebar)
    {
        // A panel with nothing to show closes the sidebar rather than opening empty.
        if ((sidebar == BitPdfSidebar.Bookmarks && HasOutline is false)
            || (sidebar == BitPdfSidebar.Attachments && HasAttachments is false)
            || (sidebar == BitPdfSidebar.Layers && HasLayers is false))
        {
            sidebar = BitPdfSidebar.None;
        }
        if (sidebar == Sidebar) return;

        bool wasThumbnails = _showThumbnails;
        _showThumbnails = sidebar == BitPdfSidebar.Thumbnails;
        _showOutline = sidebar == BitPdfSidebar.Bookmarks;
        _showAttachments = sidebar == BitPdfSidebar.Attachments;
        _showLayers = sidebar == BitPdfSidebar.Layers;

        if (_showThumbnails)
        {
            // Attach the sidebar spy after its element renders; it fills the
            // visible thumbnails on its own.
            _thumbSpyPending = true;
        }
        else if (wasThumbnails)
        {
            // The sidebar element is leaving the DOM; drop its scroll listener.
            try
            {
                await _js.BitPdfViewerDisposeThumbSpy(_thumbsRef);
            }
            catch (JSDisconnectedException) { }
        }

        Repaint();
        await OnSidebarChanged.InvokeAsync(sidebar);
    }

    /// <summary>
    /// Opens the find box (when it is closed) and searches the document for
    /// <paramref name="query"/>. An empty query just clears the current matches.
    /// </summary>
    public async Task Search(string? query)
    {
        _showSearch = true;
        _searchQuery = query ?? "";
        Repaint(); // the find box may not be in the DOM yet
        await RunSearchAsync();
    }

    /// <summary>
    /// Sets the find options and re-runs the current query against them. A
    /// <c>null</c> leaves that option as it is.
    /// </summary>
    public async Task SetSearchOptions(bool? matchCase = null, bool? wholeWord = null,
        bool? matchDiacritics = null, bool? highlightAll = null)
    {
        bool recount = false;
        if (matchCase is bool mc && mc != _matchCase) { _matchCase = mc; recount = true; }
        if (wholeWord is bool ww && ww != _wholeWord) { _wholeWord = ww; recount = true; }
        if (matchDiacritics is bool md && md != _matchDiacritics)
        {
            _matchDiacritics = md;
            _pageSearch = null; // the canonical index belongs to the previous setting
            recount = true;
        }
        bool repaint = highlightAll is bool ha && ha != _highlightAll;
        if (repaint) _highlightAll = highlightAll!.Value;

        if (recount)
        {
            await RunSearchAsync();
        }
        else if (repaint)
        {
            // Only the decoration changed; the matches are the ones already counted.
            await ApplyHighlightsAsync(scrollToCurrent: false);
        }
        Repaint();
    }

    /// <summary>Whether the find box compares case-sensitively.</summary>
    public bool SearchMatchCase => _matchCase;

    /// <summary>Whether the find box matches whole words only.</summary>
    public bool SearchWholeWord => _wholeWord;

    /// <summary>Whether the find box tells an accented letter apart from its bare form.</summary>
    public bool SearchMatchDiacritics => _matchDiacritics;

    /// <summary>Whether the find box paints every match, not just the current one.</summary>
    public bool SearchHighlightAll => _highlightAll;

    /// <summary>
    /// Moves to the next find match, wrapping around at the end.
    /// </summary>
    public Task FindNext() => GotoMatch(_searchIndex + 1);

    /// <summary>
    /// Moves to the previous find match, wrapping around at the start.
    /// </summary>
    public Task FindPrevious() => GotoMatch(_searchIndex - 1);

    /// <summary>
    /// Clears the current find query and its highlights.
    /// </summary>
    public async Task ClearSearch()
    {
        _searchQuery = "";
        await ClearSearchAsync();
        Repaint();
    }

    // ----- Built-in password prompt -----

    /// <summary>Parks the load until the reader submits a password or cancels. The
    /// progress bar is hidden while waiting - the viewer is not working, it is
    /// asking - and restored by the caller for the retry parse.</summary>
    private Task<string?> PromptForPasswordAsync(bool rejected)
    {
        _passwordRejected = rejected;
        _passwordInput = "";
        _loading = false;
        _focusPasswordPending = true;
        // One dialog at a time: an unanswered request would otherwise be dropped on
        // the floor, leaving whoever awaits it parked forever.
        CompletePasswordRequest(null);
        _passwordRequest = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        StateHasChanged();
        return _passwordRequest.Task;
    }

    private void SubmitPassword() => CompletePasswordRequest(_passwordInput);

    private void CancelPassword() => CompletePasswordRequest(null);

    private void CompletePasswordRequest(string? password)
    {
        var request = _passwordRequest;
        _passwordRequest = null;
        _passwordInput = "";
        request?.TrySetResult(password);
    }

    /// <summary>Closes the password dialog on Escape from anywhere inside it - the
    /// input's own handler only sees the keys typed into the box, and the global
    /// shortcut listener is off whenever <see cref="EnableKeyboardShortcuts"/> is.</summary>
    private void OnPasswordDialogKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Escape")
        {
            CancelPassword();
        }
    }

    /// <summary>Closes the properties dialog on Escape. The global shortcut listener
    /// does the same, but it is off whenever <see cref="EnableKeyboardShortcuts"/> is,
    /// and a modal must always be closable from the keyboard.</summary>
    private void OnPropertiesKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Escape")
        {
            _showProperties = false;
        }
    }

    private void OnPasswordInput(ChangeEventArgs e) => _passwordInput = e.Value?.ToString() ?? "";

    private void OnPasswordKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            SubmitPassword();
        }
        else if (e.Key == "Escape")
        {
            CancelPassword();
        }
    }

    /// <summary>
    /// Opens or closes the document-properties dialog.
    /// </summary>
    public void ToggleProperties()
    {
        _showProperties = !_showProperties;
        // A dialog opens with focus inside it, or a keyboard reader is left behind in
        // the toolbar with a modal they cannot reach (and Escape cannot close).
        _focusPropertiesPending = _showProperties;
        Repaint();
    }



    /// <summary>
    /// Invoked from JavaScript as pages approach the viewport. Renders any of
    /// the requested pages that have not been rendered yet, one page per
    /// event-loop turn: on single-threaded WASM rendering a whole batch in one
    /// go would block scrolling and painting for the entire batch, which shows
    /// up as freezes while scrolling through the document.
    /// </summary>
    [JSInvokable]
    public async Task EnsurePagesRendered(int[] pageNumbers)
    {
        if (_document is null || pageNumbers is null || IsDisposed) return;

        // Insert this batch ahead of older pending work (preserving its own order):
        // it reflects what is near the viewport now, so it must not wait behind
        // pages queued for a viewport the user has already scrolled away from. A page
        // already queued from an older batch is promoted (moved to this front group),
        // not skipped, so a still-visible page never stays stuck behind off-screen work.
        LinkedListNode<int>? tail = null;
        foreach (int n in pageNumbers)
        {
            int idx = n - 1;
            if (idx < 0 || idx >= _pages.Count || _pages[idx] is not null || _failedPages.Contains(idx)) continue;
            if (_renderQueued.TryGetValue(idx, out var existing))
            {
                if (existing == tail) continue; // already placed at this batch's front tip
                _renderQueue.Remove(existing);
            }
            tail = tail is null ? _renderQueue.AddFirst(idx) : _renderQueue.AddAfter(tail, idx);
            _renderQueued[idx] = tail;
        }

        // A pump is already draining the queue (scroll events keep arriving
        // while it yields); the pages just enqueued are picked up by it.
        if (_renderPumpActive) return;

        _renderPumpActive = true;
        int version = _loadVersion; // a reload while yielding invalidates the queue
        try
        {
            while (_renderQueue.Count > 0)
            {
                // Disposal tears the component down: stop and drop the queue.
                if (IsDisposed)
                {
                    _renderQueue.Clear();
                    _renderQueued.Clear();
                    return;
                }
                // A reload replaced the document while this pump yielded. LoadAsync
                // already cleared the queue, so any entries here were enqueued by the
                // new load (whose EnsurePagesRendered saw this pump still active and
                // returned without starting its own). Adopt the current version and
                // keep draining rather than discarding the replacement's pending work.
                if (version != _loadVersion) version = _loadVersion;

                int idx = _renderQueue.First!.Value;
                _renderQueue.RemoveFirst();
                _renderQueued.Remove(idx);
                if (await RenderPageAsync(idx))
                {
                    // RenderPageAsync may have yielded (background build / gate wait); a
                    // reload or disposal in that window means the slots are torn down -
                    // don't evict or re-render against them.
                    if (IsDisposed)
                    {
                        _renderQueue.Clear();
                        _renderQueued.Clear();
                        return;
                    }
                    if (version != _loadVersion) version = _loadVersion;

                    EvictDistantPages();
                    StateHasChanged();
                }
                // Let the browser apply the diff, paint and process scroll input
                // before the next (expensive) page render. Task.Delay (unlike
                // Task.Yield, whose continuation may run before the browser gets
                // control back) guarantees a real event-loop turn on WASM. Runs after a
                // failed render too, so a run of unbuildable pages can't monopolize the
                // UI thread without ever yielding.
                await Task.Delay(1);
            }
        }
        finally
        {
            _renderPumpActive = false;
        }
    }

    /// <summary>
    /// Invoked from JavaScript as thumbnails approach the sidebar viewport.
    /// Renders any requested thumbnails that are still placeholders, one per
    /// event-loop turn (a thumbnail fragment is as heavy as a full page). This is
    /// the sidebar's counterpart to <see cref="EnsurePagesRendered"/> and runs on
    /// the sidebar's own scroll, so opening the panel on a 500-page document
    /// renders only the handful of thumbnails on screen.
    /// </summary>
    [JSInvokable]
    public async Task EnsureThumbsRendered(int[] pageNumbers)
    {
        if (_document is null || pageNumbers is null || IsDisposed) return;

        // Newest sidebar batch first, promoting already-queued thumbnails, mirroring
        // EnsurePagesRendered's prioritization.
        LinkedListNode<int>? tail = null;
        foreach (int n in pageNumbers)
        {
            int idx = n - 1;
            if (idx < 0 || idx >= _thumbs.Count || _thumbs[idx] is not null || _failedThumbs.Contains(idx)) continue;
            if (_thumbQueued.TryGetValue(idx, out var existing))
            {
                if (existing == tail) continue; // already placed at this batch's front tip
                _thumbQueue.Remove(existing);
            }
            tail = tail is null ? _thumbQueue.AddFirst(idx) : _thumbQueue.AddAfter(tail, idx);
            _thumbQueued[idx] = tail;
        }

        if (_thumbPumpActive) return;

        _thumbPumpActive = true;
        int version = _loadVersion;
        try
        {
            while (_thumbQueue.Count > 0)
            {
                if (IsDisposed)
                {
                    _thumbQueue.Clear();
                    _thumbQueued.Clear();
                    return;
                }
                // A reload replaced the document while this pump yielded; the queue was
                // cleared and refilled by the new load. Adopt its version and keep
                // draining rather than discarding the replacement's pending thumbnails
                // (mirrors the page pump in EnsurePagesRendered).
                if (version != _loadVersion) version = _loadVersion;

                int idx = _thumbQueue.First!.Value;
                _thumbQueue.RemoveFirst();
                _thumbQueued.Remove(idx);
                if (await RenderThumbAsync(idx))
                {
                    // A reload or disposal during RenderThumbAsync's gate wait tears the
                    // slots down; stop before touching them.
                    if (IsDisposed)
                    {
                        _thumbQueue.Clear();
                        _thumbQueued.Clear();
                        return;
                    }
                    if (version != _loadVersion) version = _loadVersion;

                    // Evict around the thumbnail just rendered (what the sidebar is
                    // showing), not the current page - scrolling the sidebar leaves the
                    // current page put, so centering on it would blank the very
                    // thumbnails the user just scrolled to.
                    EvictDistantThumbs(idx, idx);
                    StateHasChanged();
                }
                // Yield a real event-loop turn even after a failed thumbnail build so a
                // run of unbuildable pages can't monopolize the UI thread (mirrors the
                // page pump).
                await Task.Delay(1);
            }
        }
        finally
        {
            _thumbPumpActive = false;
        }
    }

    /// <summary>
    /// Invoked from JavaScript when the most-visible page changes.
    /// </summary>
    [JSInvokable]
    public async Task OnPageVisible(int pageNumber)
    {
        if (pageNumber != CurrentPage && pageNumber >= 1 && pageNumber <= _pages.Count)
        {
            // As GoToPage: a one-way-bound page belongs to the host, so a scroll must
            // not move it behind the host's back.
            if (await AssignCurrentPage(pageNumber) is false) return;
            _appliedPage = CurrentPage;
            AnnouncePage();
            await OnPageChanged.InvokeAsync(pageNumber);
            if (IsDisposed) return;

            // Keep the sidebar's active thumbnail in view as the main surface
            // scrolls, so lazy-loaded thumbnails follow the reader.
            if (_showThumbnails)
            {
                await ScrollActiveThumbIntoViewAsync();
            }
            // A document whose pages differ in size re-fits as the reader reaches one
            // of another size; a uniform document never pays for the check.
            await RefitForCurrentPageAsync();
            if (IsDisposed) return;

            StateHasChanged();
        }
    }

    /// <summary>
    /// Invoked from JavaScript when an internal link hotspot is clicked, with the
    /// target page and - when the link's destination named one - the vertical
    /// position within it, in PDF points from the page's bottom edge.
    /// </summary>
    [JSInvokable]
    public Task OnLinkNavigate(int pageNumber, double? top)
        => GoToDestination(new BitPdfDestination { PageNumber = pageNumber, Top = top });

    /// <summary>
    /// Invoked from JavaScript when the viewport size changes.
    /// </summary>
    [JSInvokable]
    public async Task OnViewportResized()
    {
        if (_zoomMode != BitPdfZoomMode.Custom)
        {
            await ApplyFitAsync();
            StateHasChanged();
        }
    }

    /// <summary>
    /// Invoked from JavaScript on Ctrl+wheel / pinch to zoom.
    /// </summary>
    [JSInvokable]
    public async Task OnWheelZoom(double deltaY)
    {
        // The point under the cursor is stashed on the JS side before this call; the
        // render pass below puts it back where it was once the pages have re-sized.
        _zoomAnchorPending = true;
        await SetZoom(deltaY < 0 ? Zoom * 1.1 : Zoom / 1.1);
        StateHasChanged();
    }

    /// <summary>
    /// Invoked from JavaScript when the browser enters or leaves fullscreen. Leaving
    /// it - by the browser's own Escape, say - also leaves presentation mode, so the
    /// two never disagree.
    /// </summary>
    [JSInvokable]
    public async Task OnFullscreenChanged(bool isFullscreen)
    {
        if (IsDisposed) return;

        _fullscreen = isFullscreen;
        if (isFullscreen is false && _presenting)
        {
            await ExitPresentationMode();
            return;
        }
        StateHasChanged(); // the toolbar button reports the state it just entered/left
    }

    /// <summary>
    /// Invoked from JavaScript for a keyboard shortcut. The key matching (and the
    /// preventDefault that stops the browser's own Ctrl+P / Ctrl+F / Ctrl+S) lives
    /// on the JS side, which is the only place that can see the event target and
    /// so tell a shortcut apart from typing into the find or page box.
    /// </summary>
    [JSInvokable]
    public async Task OnShortcut(string command)
    {
        if (IsDisposed || IsEnabled is false) return;

        switch (command)
        {
            case "next": await NextPage(); break;
            case "prev": await PrevPage(); break;
            case "first": await FirstPage(); break;
            case "last": await LastPage(); break;
            case "zoomIn": await ZoomIn(); break;
            case "zoomOut": await ZoomOut(); break;
            case "actualSize": await SetZoomMode(BitPdfZoomMode.ActualSize); break;
            case "rotateCw": await RotateClockwise(); break;
            case "rotateCcw": await RotateCounterClockwise(); break;
            case "find":
                if (HasToolbarItem(BitPdfToolbarItems.Search) is false) return;
                _showSearch = true;
                _focusSearchPending = true;
                break;
            case "findNext": await FindNext(); break;
            case "findPrev": await FindPrevious(); break;
            case "print": await Print(); break;
            case "download": await Download(); break;
            case "sidebar":
                await ShowSidebar(Sidebar == BitPdfSidebar.None ? BitPdfSidebar.Thumbnails : BitPdfSidebar.None);
                break;
            case "fullscreen": await ToggleFullscreen(); break;
            case "presentation": await TogglePresentationMode(); break;
            case "escape":
                // One Escape closes whatever overlay is open, innermost first.
                if (_showProperties)
                {
                    _showProperties = false;
                }
                else if (_showSearch)
                {
                    await ToggleSearch();
                }
                else if (_presenting)
                {
                    await ExitPresentationMode();
                }
                break;
            default: return;
        }

        if (IsDisposed is false)
        {
            StateHasChanged();
        }
    }



    protected override string RootElementClass => "bit-pdv";

    // One shared instance backs every viewer that does not localize, so the
    // default strings are not re-allocated per component.
    private static readonly BitPdfViewerTexts _defaultTexts = new();

    /// <summary>The texts in effect: the host's, or the shared English defaults.</summary>
    private BitPdfViewerTexts ActiveTexts => Texts ?? _defaultTexts;

    /// <summary>Puts the focused page into the polite live region. The page box is an
    /// <c>&lt;input&gt;</c>, and moving its value announces nothing, so scrolling or
    /// paging would otherwise be silent to a screen reader.</summary>
    private void AnnouncePage()
    {
        if (_pages.Count == 0)
        {
            _announcement = string.Empty;
            return;
        }
        _announcement = string.Format(ActiveTexts.PageAnnouncementFormat, CurrentPage, _pages.Count);
    }

    /// <summary>Whether this page's build threw, so its slot will stay empty.</summary>
    private bool HasPageFailed(int index) => _failedPages.Contains(index);

    /// <summary>Whether this thumbnail's build threw.</summary>
    private bool HasThumbFailed(int index) => _failedThumbs.Contains(index);

    /// <summary>Whether printing is offered: always, unless the document forbids it
    /// and <see cref="RespectPermissions"/> asks for that to be honoured.</summary>
    private bool CanPrint => RespectPermissions is false || Permissions.CanPrint;

    /// <summary>Whether the document may be saved, under the same rule. A pdf's
    /// "copy" permission covers extracting its content, which downloading it is.</summary>
    private bool CanDownload => RespectPermissions is false || Permissions.CanCopy;

    /// <summary>Whether the reader may select the text of the document.</summary>
    private bool CanCopy => RespectPermissions is false || Permissions.CanCopy;

    /// <summary>Whether the toolbar shows the given group of controls.</summary>
    private bool HasToolbarItem(BitPdfToolbarItems item) => (ToolbarItems & item) == item;

    /// <summary>The literal "true"/"false" an ARIA state attribute needs. A bool bound
    /// straight to an attribute is rendered as an HTML BOOLEAN attribute instead -
    /// present-but-empty when true, absent when false - which assistive technology
    /// reads as neither state.</summary>
    private static string AriaBool(bool value) => value ? "true" : "false";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => _presenting ? "bit-pdv-presenting" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        StyleBuilder.Register(() => Height.HasValue() ? $"height:{Height}" : string.Empty);

        StyleBuilder.Register(() => Width.HasValue() ? $"width:{Width}" : string.Empty);
    }

    protected override void OnInitialized()
    {
        _zoomMode = InitialZoomMode;
        _scrollMode = ScrollMode;
        _spreadMode = SpreadMode;
        _cursorTool = CursorTool;
        _showThumbnails = DefaultSidebar == BitPdfSidebar.Thumbnails;
        _showOutline = DefaultSidebar == BitPdfSidebar.Bookmarks;
        _showAttachments = DefaultSidebar == BitPdfSidebar.Attachments;
        _showLayers = DefaultSidebar == BitPdfSidebar.Layers;

        base.OnInitialized();
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        // The shortcut listener is attached on the root element, so a flip of the
        // parameter has to (de)register it rather than just gate the handler.
        if (EnableKeyboardShortcuts != _keyboardAttached)
        {
            _keyboardPending = true;
        }

        if (AllowDropFile != _dropZoneAttached)
        {
            _dropZonePending = true;
        }

        // The layout parameters seed state the toolbar can move afterwards, so only a
        // change of the PARAMETER (not of the state) is adopted - otherwise every
        // re-render of the host would snap the user's choice back to the default.
        if (_scrollModeParam != ScrollMode)
        {
            _scrollModeParam = ScrollMode;
            await SetScrollMode(ScrollMode);
        }
        if (_spreadModeParam != SpreadMode)
        {
            _spreadModeParam = SpreadMode;
            await SetSpreadMode(SpreadMode);
        }
        if (_cursorToolParam != CursorTool)
        {
            _cursorToolParam = CursorTool;
            SetCursorTool(CursorTool);
        }

        // The PARAMETER is tracked separately from the source in effect: a document the
        // reader opened from the toolbar replaces the latter but not the former, so the
        // next host render (still carrying the old Source) does not close it again.
        if (ReferenceEquals(_sourceParam, Source) is false)
        {
            _sourceParam = Source;
            await OpenCoreAsync(Source);
            return;
        }

        // A host-driven change of a two-way value navigates, zooms or rotates. The
        // applied snapshots tell a host assignment apart from the viewer's own, which
        // already moved both the value and its snapshot.
        if (CurrentPage != _appliedPage)
        {
            _appliedPage = CurrentPage;
            await GoToPage(CurrentPage);
        }
        if (Math.Abs(Zoom - _appliedZoom) > 0.0001)
        {
            _appliedZoom = Zoom;
            await SetZoom(Zoom);
        }
        if (Rotation != _appliedRotation)
        {
            // Not SetRotation: the parameter already holds the new angle, so it would
            // see the value it was asked to set in place and return without re-rendering.
            await ApplyRotationAsync();
        }

        // Same document but a rendering mode changed: invalidate and re-render
        // the page fragments (same mechanism as rotation) so the new mode takes
        // effect without reloading the document.
        if (_textCoalescing != TextCoalescing || _renderMode != RenderMode)
        {
            _textCoalescing = TextCoalescing;
            _renderMode = RenderMode;
            PreparePages();
            await RenderCurrentPageEagerlyAsync();
        }
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitPdfViewerCanvasPage))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitPdfViewerViewport))]
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            _dotnetObj = DotNetObjectReference.Create(this);
            try
            {
                await _js.BitPdfViewerRegisterFullscreenSpy(RootElement, _dotnetObj);
            }
            catch (JSDisconnectedException) { } // Circuit gone mid-render; ignore.
        }

        // Keyboard shortcuts live on the root element (so they work wherever focus
        // is inside the viewer) and are attached/detached as the parameter flips.
        if (_keyboardPending && _dotnetObj is not null)
        {
            _keyboardPending = false;
            try
            {
                if (EnableKeyboardShortcuts)
                {
                    await _js.BitPdfViewerRegisterKeyboard(RootElement, _dotnetObj);
                    _keyboardAttached = true;
                }
                else if (_keyboardAttached)
                {
                    await _js.BitPdfViewerDisposeKeyboard(RootElement);
                    _keyboardAttached = false;
                }
            }
            catch (JSDisconnectedException) { } // Circuit gone mid-render; ignore.
        }

        // The find box only exists in the DOM while it is open, so a shortcut that
        // opened it has to wait for this pass to move focus into it.
        if (_focusSearchPending && _showSearch)
        {
            _focusSearchPending = false;
            try
            {
                await _js.BitPdfViewerFocus(_searchInputRef);
            }
            catch (JSDisconnectedException) { }
        }

        // Likewise the properties dialog: a modal that opens with focus still behind it
        // is one a keyboard reader cannot reach.
        if (_focusPropertiesPending && _showProperties)
        {
            _focusPropertiesPending = false;
            try
            {
                await _js.BitPdfViewerTrapFocus(_propertiesRef);
                _propertiesTrapped = true;
            }
            catch (JSDisconnectedException) { }
        }
        // A modal that closes leaving focus on <body> drops a keyboard reader out of
        // the viewer, so the control that opened it gets focus back.
        else if (_propertiesTrapped && _showProperties is false)
        {
            _propertiesTrapped = false;
            try
            {
                await _js.BitPdfViewerReleaseFocus();
            }
            catch (JSDisconnectedException) { }
        }

        // Likewise the password box: a dialog the reader cannot type into without
        // first clicking it would be a poor way to ask.
        if (_focusPasswordPending && _passwordRequest is not null)
        {
            _focusPasswordPending = false;
            try
            {
                // The password box first, then the dialog around it keeps Tab inside.
                await _js.BitPdfViewerFocus(_passwordInputRef);
                await _js.BitPdfViewerTrapFocus(_passwordDialogRef);
                _passwordTrapped = true;
            }
            catch (JSDisconnectedException) { }
        }
        else if (_passwordTrapped && _passwordRequest is null)
        {
            _passwordTrapped = false;
            try
            {
                await _js.BitPdfViewerReleaseFocus();
            }
            catch (JSDisconnectedException) { }
        }

        // The drop zone is registered on the root, so a file can be dropped anywhere
        // on the viewer - and re-registered when the parameter flips.
        if (_dropZonePending)
        {
            _dropZonePending = false;
            try
            {
                if (AllowDropFile)
                {
                    await _js.BitPdfViewerRegisterDropZone(RootElement);
                    _dropZoneAttached = true;
                }
                else if (_dropZoneAttached)
                {
                    await _js.BitPdfViewerDisposeDropZone(RootElement);
                    _dropZoneAttached = false;
                }
            }
            catch (JSDisconnectedException) { }
        }

        if (_spyPending && _dotnetObj is not null)
        {
            _spyPending = false;
            await _js.BitPdfViewerRegisterScrollSpy(_containerRef, _dotnetObj);
            await ApplyFitAsync();
            if (string.IsNullOrEmpty(_searchQuery) is false)
            {
                await RunSearchAsync();
            }
        }

        // Attach the sidebar's own lazy-render spy once its element exists in the
        // DOM (it is only present while the thumbnail panel is open). The spy
        // fills the visible thumbnails on its own scroll, independent of the main
        // surface. Registration is idempotent, so re-running it after a reload or
        // rotation simply re-fills the freshly reset slots.
        if (_thumbSpyPending && _showThumbnails && _dotnetObj is not null)
        {
            _thumbSpyPending = false;
            try
            {
                await _js.BitPdfViewerRegisterThumbSpy(_thumbsRef, _dotnetObj);
                await _js.BitPdfViewerScrollThumbIntoView(_thumbsRef, CurrentPage);
            }
            catch (JSDisconnectedException) { } // Circuit gone mid-render; ignore.
        }

        // An entry the page box could not act on is written back over, so the box shows
        // the page that is actually in front of the reader.
        if (_pageBoxResetPending)
        {
            _pageBoxResetPending = false;
            try
            {
                await _js.BitPdfViewerSetValue(_pageInputRef,
                    CurrentPageLabel ?? CurrentPage.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
            catch (JSDisconnectedException) { }
        }

        // A wheel or pinch zoom asked for the point under the cursor to stay put; the
        // pages have their new size now, so the stashed anchor can be restored.
        if (_zoomAnchorPending)
        {
            _zoomAnchorPending = false;
            try
            {
                await _js.BitPdfViewerRestoreZoomAnchor(_containerRef);
            }
            catch (JSDisconnectedException) { }
        }

        // After any render that produced new page content, correct each text run's
        // width to its PDF advance (fixes spacing when a substitute font is used).
        if (_correctWidthsPending)
        {
            _correctWidthsPending = false;
            try
            {
                await _js.BitPdfViewerCorrectTextWidths(_containerRef);
            }
            catch (JSDisconnectedException) { } // Circuit gone mid-render; ignore.
        }

        // Pages rendered since the last pass carry unhighlighted text; repaint the
        // query's highlights over them without moving the viewport.
        if (_highlightPending)
        {
            _highlightPending = false;
            if (_searchTotal > 0)
            {
                await ApplyHighlightsAsync(scrollToCurrent: false);
            }
        }

        // Canvas mode: replay the display lists of freshly (re)created page
        // canvases at the current zoom. Fragments re-render on demand, so this
        // runs after any render that added pages.
        if (_canvasDirty.Count > 0)
        {
            // Snapshot the generation this pass flushes before clearing the dirty set,
            // so _canvasPaintedGen only advances once *this* paint's interop completes.
            int paintGen = _canvasDirtyGen;
            var payload = _canvasDirty
                .Where(i => _canvasOps.ContainsKey(i) && i < _pageWidths.Count)
                .Select(i => new BitPdfViewerCanvasPage { Page = i + 1, W = _pageWidths[i], H = _pageHeights[i], Ops = _canvasOps[i] })
                .ToArray();
            _canvasDirty.Clear();
            if (payload.Length > 0)
            {
                _paintedZoom = Zoom;
                try
                {
                    await _js.BitPdfViewerPaintCanvasPages(_containerRef, payload, Zoom);
                }
                catch (JSDisconnectedException) { } // Circuit gone mid-render; ignore.
            }
            // Painted through paintGen (guard against out-of-order completion of an
            // overlapping pass that snapshotted a later generation).
            if (paintGen > _canvasPaintedGen) _canvasPaintedGen = paintGen;
        }

        // Canvas mode: when the zoom changed, re-rasterize the already-painted
        // canvases at the new scale. The CSS-scaled bitmap is visible immediately;
        // the sharp replay swaps in when zooming settles (debounced on the JS side).
        // Uses the ops cached on each canvas element, so no display lists cross
        // the interop boundary again.
        if (RenderMode == BitPdfRenderMode.Canvas
            && Math.Abs(Zoom - _paintedZoom) > 0.001 && _pages.Count > 0)
        {
            _paintedZoom = Zoom;
            try
            {
                await _js.BitPdfViewerRezoomCanvases(_containerRef, Zoom);
            }
            catch (JSDisconnectedException) { } // Circuit gone mid-render; ignore.
        }

        // Release a print parked on the barrier only once the generation it needs has
        // actually been painted (above). An empty pass never advances _canvasPaintedGen,
        // so it leaves the signal pending for the real paint to complete.
        if (_canvasPaintSignal is { } paintSignal && _canvasPaintedGen >= _canvasPaintSignalGen)
        {
            _canvasPaintSignal = null;
            paintSignal.TrySetResult();
        }
    }



    // Parse off the UI thread on Blazor Server so a large document doesn't freeze
    // the circuit; on single-threaded WASM this runs inline (Task.Run offers no
    // parallelism there, and the surrounding Task.Delay already lets the bar paint).
    // BackgroundRendering opts WASM into Task.Run too, for the multi-threaded runtime.
    private Task<BitPdfDocument> ParseAsync(byte[] bytes, string? password)
        => OperatingSystem.IsBrowser() && BackgroundRendering is false
            ? Task.FromResult(BitPdfDocument.Load(bytes, password))
            : Task.Run(() => BitPdfDocument.Load(bytes, password));

    /// <summary>
    /// Fetches a URL source, reporting progress as the bytes arrive when the server
    /// declares a length. Returns <c>null</c> when a newer load superseded this one
    /// mid-transfer, so the caller drops it rather than parsing a document nobody
    /// asked for any more.
    /// </summary>
    private async Task<byte[]?> FetchAsync(HttpClient http, BitPdfSource source, int version)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, source.Url);
        if (source.Headers is { Count: > 0 } headers)
        {
            // A source that carries headers (an Authorization one, typically) needs a
            // request of its own; GetByteArrayAsync cannot carry them.
            foreach (var header in headers)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        // ResponseHeadersRead lets the body be read in chunks, which is what makes a
        // progress report possible at all; without it the whole file arrives at once.
        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        long? length = response.Content.Headers.ContentLength;
        if (OnProgress.HasDelegate is false || length is not > 0)
        {
            // Nothing to report against (a chunked response declares no length), so
            // take the simple path.
            return await response.Content.ReadAsByteArrayAsync();
        }

        using var body = await response.Content.ReadAsStreamAsync();
        // 64 KB is small enough that progress moves visibly on a slow link and large
        // enough that the reports don't become the cost of the download.
        const int chunk = 64 * 1024;
        const int initialCap = 1024 * 1024;

        // The declared length is what the progress fraction is reported against, but
        // NOT what is allocated: a server (or a proxy) is free to declare a gigabyte
        // it never sends, and taking that at its word would allocate it all before the
        // first byte arrives - fatal on WebAssembly. Grow with the bytes instead.
        long declared = Math.Min(length.Value, int.MaxValue);
        var buffer = new byte[(int)Math.Min(declared, initialCap)];
        int read = 0;
        while (true)
        {
            if (read == buffer.Length)
            {
                int grown = buffer.Length >= Array.MaxLength / 2
                    ? Array.MaxLength
                    : Math.Max(buffer.Length * 2, chunk);
                if (grown <= buffer.Length) break; // a PDF that no array can hold

                Array.Resize(ref buffer, grown);
            }

            int got = await body.ReadAsync(buffer.AsMemory(read, Math.Min(chunk, buffer.Length - read)));
            if (got <= 0) break; // the body ended; take what came

            read += got;
            if (IsDisposed || version != _loadVersion) return null;
            // A body longer than it declared must not report past 100%.
            await OnProgress.InvokeAsync(Math.Min(1d, read / (double)declared));
            if (IsDisposed || version != _loadVersion) return null;
        }
        return read == buffer.Length ? buffer : buffer[..read];
    }

    private async Task LoadAsync()
    {
        int version = ++_loadVersion; // supersedes any load still in flight

        // Release a load parked on the password dialog: its answer belongs to a
        // document that is no longer the Source. It resumes only to see the bumped
        // version and bail, and the dialog leaves the DOM instead of asking for a
        // password nobody is waiting for.
        CompletePasswordRequest(null);

        // Reset the shared state under the render gate so an in-flight background
        // build (which reads _document and lazily (re)creates _fontStore) cannot
        // repopulate these fields after we clear them.
        await _renderGate.WaitAsync();
        try
        {
            // A newer load superseded this one while we waited for the gate; let it
            // own the reset so we don't clobber its freshly parsed state.
            if (version != _loadVersion) return;

            _pages.Clear();
            _thumbs.Clear();       // sidebar fragments belong to the old document
            _canvasOps.Clear();    // canvas display lists (with their base64 images) too
            _canvasDirty.Clear();
            _failedPages.Clear();  // failures belonged to the old document
            _failedThumbs.Clear();
            _renderQueue.Clear(); // pending lazy renders belong to the old document
            _renderQueued.Clear();
            _thumbQueue.Clear();
            _thumbQueued.Clear();
            _pageWidths.Clear();
            _pageHeights.Clear();
            _document = null;
            _bytes = null;    // the old document's bytes; Download must not offer them for the new one
            _fontStore = null; // fresh embedded-font store per document
            _fontFaceStyle = string.Empty; // its @font-face snapshot belongs to the old document
            _hasPageLabels = null; // the labels belonged to the old document
            _pageText = null;  // invalidate the search text index
            _pageSearch = null; // and its canonical search companion
            _searchTotal = 0;
            _searchIndex = -1;
            _outline = [];
            _attachments = []; // the old document's embedded files
            _layers = [];      // and its layers
            _hiddenLayers = null;
            _hiddenLayersView = null;
            _collapsedOutline.Clear(); // fold state belongs to the old document's bookmarks
            _focusedOutline = null;    // and so does the tree's tab stop
            _announcement = string.Empty;
            _showProperties = false;   // the dialog described the old document
            // A superseded load's finally won't clear the progress bar (it no longer
            // owns _loadVersion); reset it here so e.g. Source = null while a load is
            // in flight doesn't leave the bar up forever.
            _loading = false;
        }
        finally
        {
            _renderGate.Release();
        }

        if (_source is null)
        {
            _status = ActiveTexts.NoDocument;
            return;
        }

        // Show the progress bar and let it paint before the synchronous parse
        // work begins. The bar animates on the compositor so it keeps moving
        // even while the WASM thread is busy parsing. Task.Delay guarantees the
        // browser gets an event-loop turn to paint (Task.Yield does not on WASM).
        _loading = true;
        StateHasChanged();
        await Task.Delay(1);

        // A newer Source arrived while we yielded: abandon this stale load.
        if (version != _loadVersion) return;

        // Resolve the bytes: an in-memory buffer, or a URL fetched via HttpClient.
        byte[]? bytes = _source.Bytes;
        if (bytes is null && _source.Url is not null)
        {
            if (_services.GetService(typeof(HttpClient)) is not HttpClient http)
            {
                _status = ActiveTexts.HttpClientRequired;
                _loading = false;
                await OnError.InvokeAsync(_status);
                return;
            }
            try
            {
                bytes = await FetchAsync(http, _source, version);
                if (bytes is null) return; // superseded while fetching
            }
            catch (Exception ex)
            {
                // A newer Source superseded this load while the fetch was in flight;
                // its failure is not the current document's, so don't publish a stale
                // error or hide the newer load's progress bar (mirrors the parse catch).
                if (version != _loadVersion) return;
                _status = string.Format(ActiveTexts.FetchFailedFormat, ex.Message);
                _loading = false;
                await OnError.InvokeAsync(_status);
                return;
            }
            if (version != _loadVersion) return;
        }
        if (bytes is null)
        {
            _status = ActiveTexts.NoDocument;
            _loading = false;
            return;
        }

        try
        {
            BitPdfDocument document;
            string? password = _source.Password;
            // Retry as long as a password keeps arriving: a reader who mistypes gets
            // another try instead of one shot and an error. Cancelling (an empty
            // answer, or no prompt at all) rethrows into the outer catch, which is
            // what surfaces the failure through OnError.
            while (true)
            {
                try
                {
                    document = await ParseAsync(bytes, password);
                    break;
                }
                catch (BitPdfPasswordException ex)
                {
                    // The parse awaited (Task.Run / gate) long enough for a newer
                    // Source or a disposal; don't prompt on a load already superseded.
                    if (IsDisposed || version != _loadVersion) return;

                    string? entered = OnPasswordRequested is not null
                        ? await OnPasswordRequested()
                        : ShowPasswordPrompt ? await PromptForPasswordAsync(ex.WasProvided) : null;
                    if (string.IsNullOrEmpty(entered))
                    {
                        throw;
                    }
                    // The prompt awaits a human (or user code) and may have outlived
                    // this load; don't parse - or later publish - a superseded document.
                    if (IsDisposed || version != _loadVersion) return;
                    password = entered;
                    _loading = true; // the prompt cleared the bar; the retry parse is work again
                    StateHasChanged();
                }
            }
            // A password prompt (or the parse itself) may have awaited long enough
            // for a newer Source; don't clobber the newer load's document.
            if (version != _loadVersion) return;
            _document = document;
            // Keep the bytes this document was parsed from: Download and the
            // properties dialog then work for URL sources without re-fetching.
            _bytes = bytes;

            PreparePages();
            await RenderCurrentPageEagerlyAsync();

            // A newer Source may have superseded this load while the eager render
            // awaited (or the component was disposed); stop before touching _document
            // or publishing stale outline/status/callbacks.
            if (IsDisposed || version != _loadVersion) return;

            try
            {
                _outline = _document.Outline;
            }
            catch
            {
                _outline = [];
            }
            try
            {
                // Attachments are read up front (like the outline) so the toolbar can
                // enable its toggle without the reader having to open the panel first.
                _attachments = _document.Attachments;
            }
            catch
            {
                _attachments = [];
            }
            try
            {
                _layers = _document.Layers;
                // Seed the reader's set from the document's own default configuration.
                // A document with no layers leaves it null, so the renderer keeps
                // consulting the default rather than an empty override.
                _hiddenLayers = _layers.Count > 0
                    ? [.. _layers.Where(l => l.VisibleByDefault is false).Select(l => l.Id)]
                    : null;
                _hiddenLayersView = _hiddenLayers is null ? null : new HashSet<string>(_hiddenLayers);
            }
            catch
            {
                _layers = [];
                _hiddenLayers = null;
                _hiddenLayersView = null;
            }
            _status = string.Format(ActiveTexts.PageCountFormat, _document.PageCount);
            _spyPending = true;
            if (_document.Warnings.Count > 0 && OnWarnings.HasDelegate)
            {
                await OnWarnings.InvokeAsync(_document.Warnings);
                // The warnings callback is user code: it may have awaited long
                // enough for a newer Source (or even set one) or a disposal -
                // don't announce a superseded document as loaded.
                if (IsDisposed || version != _loadVersion) return;
            }
            await OnDocumentLoaded.InvokeAsync();
        }
        catch (Exception ex)
        {
            // A superseded load's failure is not this document's failure: don't
            // publish a stale error over the newer load's state.
            if (version != _loadVersion) return;
            _status = string.Format(ActiveTexts.ErrorFormat, ex.Message);
            await OnError.InvokeAsync(ex.Message);
        }
        finally
        {
            // Only the load that still owns _loadVersion may clear the progress
            // bar; a superseded load finishing late must not hide the newer one's.
            if (version == _loadVersion)
            {
                _loading = false;
            }
        }
    }

    /// <summary>
    /// Measures every page (cheap) and creates an empty render slot for each so
    /// the document surface, scrollbar and page count are correct immediately.
    /// Only the current page is rendered eagerly (by the caller); the rest are
    /// rendered later on demand by the lazy-render pump as they approach the viewport.
    /// </summary>
    private void PreparePages()
    {
        // Invalidate any in-flight background render committing into the old slots.
        _renderEpoch++;
        _pages.Clear();
        _thumbs.Clear();
        _renderQueue.Clear();
        _renderQueued.Clear();
        _thumbQueue.Clear();
        _thumbQueued.Clear();
        _pageWidths.Clear();
        _pageHeights.Clear();
        _canvasOps.Clear();
        _canvasDirty.Clear();
        _failedPages.Clear(); // a fresh set of slots deserves a fresh attempt at each
        _failedThumbs.Clear();
        _fitPageIndex = -1;   // the page the current fit was measured against is gone
        _hasPageLabels = null; // the page count changed under the label scan
        if (_document is null) return;

        bool swap = Rotation % 180 == 90;
        foreach (var page in _document.Pages)
        {
            _pages.Add(null);
            _thumbs.Add(null);
            _pageWidths.Add(swap ? page.Height : page.Width);
            _pageHeights.Add(swap ? page.Width : page.Height);
        }

        // The current page is rendered eagerly by the caller (RenderCurrentPageEagerlyAsync)
        // so something is visible instantly; neighbors follow through the lazy-render
        // pump, which yields to the browser between pages.

        // If the sidebar is open, its slots were just reset; let its spy re-fill
        // the visible thumbnails on the next render.
        if (_showThumbnails)
        {
            _thumbSpyPending = true;
        }
    }

    /// <summary>The document-wide embedded-font <c>@font-face</c> stylesheet,
    /// rendered in a persistent element so it survives page eviction. Reads a
    /// UI-thread snapshot, never the font store's live builder (which a background
    /// render may be mutating).</summary>
    private MarkupString FontFaceStyleMarkup => new(_fontFaceStyle);

    /// <summary>The result of the heavy, offloadable part of rendering a page.</summary>
    private readonly record struct BitPdfPageBuild(string Html, string? Ops);

    /// <summary>
    /// The heavy, offloadable half of rendering a page: runs the C# renderer and
    /// returns its HTML plus any canvas display list. May run on a worker thread
    /// (see <see cref="BackgroundRendering"/>); it touches the shared document and
    /// font store, so it is only ever called while holding <see cref="_renderGate"/>.
    /// The document reference is captured into a local so a concurrent reload (which
    /// nulls the field) cannot fault an in-flight build: if the field is already null
    /// when captured, it bails with a discardable result that <see cref="RenderPageAsync"/>'s
    /// version/epoch guard drops; if it is nulled after capture, the local keeps the
    /// old document alive and the result is likewise discarded. The render settings are
    /// passed in as a UI-thread snapshot so a background build reads a consistent set
    /// even if the component's state changes while it runs.
    /// </summary>
    private BitPdfPageBuild BuildPage(int index, int rotation, BitPdfTextCoalescing textCoalescing, BitPdfRenderMode renderMode)
    {
        var doc = _document;
        if (doc is null || index < 0 || index >= doc.Pages.Count)
        {
            // A reload nulled _document before this (possibly background) build ran;
            // return an empty, discardable result - the caller's guard drops it.
            return new BitPdfPageBuild(string.Empty, null);
        }
        var store = _fontStore ??= new BitPdfFontStore();
        var page = doc.Pages[index];
        var renderer = new BitPdfHtmlRenderer(page, doc.XRef, store, rotation)
        {
            DestinationResolver = dest => doc.ResolveDestinationPage(dest),
            // Links resolve to the full destination, so one pointing into the middle of
            // a long page carries that position into the markup (see OnLinkNavigate).
            DestinationInfoResolver = dest => doc.ResolveDestination(dest),
            TextCoalescing = textCoalescing,
            EmitCanvasOps = renderMode == BitPdfRenderMode.Canvas,
            // The frozen snapshot, never the live set: a background build must not read
            // it while the UI thread is switching a layer.
            HiddenLayers = _hiddenLayersView,
        };
        return new BitPdfPageBuild(renderer.Render(), renderer.CanvasOpsJson);
    }

    /// <summary>
    /// The UI-thread half of rendering a page: records the canvas display list,
    /// refreshes the font-face snapshot and schedules the post-render width
    /// correction. Runs on the UI thread so these shared collections and the
    /// snapshot are only ever written there.
    /// </summary>
    private MarkupString CommitPage(int index, BitPdfPageBuild build)
    {
        _correctWidthsPending = true; // measure/scale text runs after this render
        // A page arriving while a query is active brings unhighlighted text with it.
        if (_searchTotal > 0)
        {
            _highlightPending = true;
        }
        // Canvas mode: hold the display list until the fragment's <canvas> exists
        // in the DOM, then OnAfterRenderAsync replays it via JS.
        if (build.Ops is { } ops)
        {
            _canvasOps[index] = ops;
            if (_canvasDirty.Contains(index) is false)
            {
                _canvasDirty.Add(index);
            }
            _canvasDirtyGen++; // new paint work; a parked print waits for this generation
        }
        _fontFaceStyle = _fontStore?.FontFaceStyle ?? string.Empty;
        return new MarkupString(build.Html);
    }

    /// <summary>
    /// Renders one page into its slot if it is still a placeholder, serialized
    /// through <see cref="_renderGate"/> and - when <see cref="BackgroundRendering"/>
    /// is set and the runtime has a spare thread - with the heavy build hopped off
    /// the UI thread. Returns <c>true</c> if it actually rendered the page. In the
    /// default foreground mode the whole method completes synchronously, so callers
    /// keep their current instant behavior.
    /// </summary>
    private async Task<bool> RenderPageAsync(int index)
    {
        if (index < 0 || index >= _pages.Count || _pages[index] is not null || IsDisposed) return false;

        try
        {
            await _renderGate.WaitAsync();
        }
        catch (ObjectDisposedException)
        {
            return false; // disposal disposed the gate while we waited for it
        }
        string? buildError = null;
        bool rendered = false;
        int version = 0, epoch = 0;
        try
        {
            if (IsDisposed || index >= _pages.Count || _pages[index] is not null) return false; // filled/disposed while waiting
            version = _loadVersion; epoch = _renderEpoch;

            // Snapshot the render settings on the UI thread so a background build reads
            // a consistent set even if Rotation/TextCoalescing/RenderMode change while
            // it runs; a change also bumps _renderEpoch, so the result is discarded below.
            int rotation = Rotation;
            var textCoalescing = TextCoalescing;
            var renderMode = RenderMode;

            BitPdfPageBuild build;
            try
            {
                build = BackgroundRendering
                    ? await Task.Run(() => BuildPage(index, rotation, textCoalescing, renderMode))
                    : BuildPage(index, rotation, textCoalescing, renderMode);
            }
            catch (Exception ex)
            {
                // A malformed page must not fault the JS-invokable render pump (an
                // unhandled exception there tears down the Blazor Server circuit).
                // Mark it failed so the scroll-driven pump stops asking for it (which
                // would re-run the same failing build on every pass), and surface the
                // failure via OnError once the gate is released (below).
                buildError = ex.Message;
                if (version == _loadVersion && epoch == _renderEpoch)
                {
                    _failedPages.Add(index);
                }
                return false;
            }

            // A reload, rotation, mode change or disposal while the build was in
            // flight invalidated these slots: drop the now-stale fragment.
            if (IsDisposed || version != _loadVersion || epoch != _renderEpoch || index >= _pages.Count || _pages[index] is not null)
            {
                return false;
            }
            _pages[index] = CommitPage(index, build);
            rendered = true;
            return true;
        }
        finally
        {
            _renderGate.Release();
            // OnPageRendered is user code (it may load, rotate or dispose); like
            // OnError it runs only after the gate is released, and only for a render
            // that still owns the current load/epoch on a live component.
            if (rendered && !IsDisposed && version == _loadVersion && epoch == _renderEpoch)
            {
                await OnPageRendered.InvokeAsync(index + 1);
            }
            // OnError is user code; invoke it only after releasing the gate so a
            // handler that triggers a reload or render cannot deadlock on it. And
            // only for a build that still owns the current load/epoch on a live
            // component: a build that threw because a reload superseded it (which
            // can null _document mid-build) is not this document's failure.
            if (buildError is not null && !IsDisposed && version == _loadVersion && epoch == _renderEpoch)
            {
                await OnError.InvokeAsync(buildError);
            }
        }
    }

    /// <summary>
    /// Renders one thumbnail into its slot, serialized through the same gate as page
    /// renders so it never overlaps a background page build against the shared font
    /// store. A thumbnail fragment is as heavy as a full page, so an uncached build
    /// follows the same <see cref="BackgroundRendering"/> path as page renders;
    /// stale results are dropped by the same version/epoch guard before committing.
    /// Returns <c>true</c> if it rendered.
    /// </summary>
    private async Task<bool> RenderThumbAsync(int index)
    {
        if (index < 0 || index >= _thumbs.Count || _thumbs[index] is not null || IsDisposed) return false;

        try
        {
            await _renderGate.WaitAsync();
        }
        catch (ObjectDisposedException)
        {
            return false; // disposal disposed the gate while we waited for it
        }
        string? buildError = null;
        int version = 0, epoch = 0;
        try
        {
            if (IsDisposed || index >= _thumbs.Count || _thumbs[index] is not null) return false;

            // The markup is identical to the full page (only the enclosing
            // --bit-pdv-scale differs), so reuse the main surface's immutable
            // fragment when it is already rendered. Not in canvas mode: page
            // fragments there are placeholders whose pixels JS paints into the
            // MAIN surface only - a reused fragment would show a blank thumbnail.
            if (RenderMode != BitPdfRenderMode.Canvas && index < _pages.Count && _pages[index] is { } cached)
            {
                _thumbs[index] = cached;
                return true;
            }

            version = _loadVersion; epoch = _renderEpoch;
            int rotation = Rotation;
            var textCoalescing = TextCoalescing;
            var renderMode = RenderMode;

            BitPdfPageBuild build;
            try
            {
                build = BackgroundRendering
                    ? await Task.Run(() => BuildThumb(index, rotation, textCoalescing, renderMode))
                    : BuildThumb(index, rotation, textCoalescing, renderMode);
            }
            catch (Exception ex)
            {
                // As RenderPageAsync: a malformed page must not fault the JS-invokable
                // thumbnail pump. Skip it and surface via OnError after releasing the gate.
                buildError = ex.Message;
                if (version == _loadVersion && epoch == _renderEpoch)
                {
                    _failedThumbs.Add(index);
                }
                return false;
            }

            // A reload, rotation, mode change or disposal while the build was in
            // flight invalidated these slots: drop the now-stale fragment.
            if (IsDisposed || version != _loadVersion || epoch != _renderEpoch || index >= _thumbs.Count || _thumbs[index] is not null)
            {
                return false;
            }
            // Commit through the shared path so fonts the thumbnail discovered
            // land in the @font-face snapshot.
            _thumbs[index] = CommitPage(index, build);
            return true;
        }
        finally
        {
            _renderGate.Release();
            // As RenderPageAsync: report only an active build's failure, never one
            // from a build superseded by a reload/epoch change or a torn-down component.
            if (buildError is not null && !IsDisposed && version == _loadVersion && epoch == _renderEpoch)
            {
                await OnError.InvokeAsync(buildError);
            }
        }
    }

    /// <summary>Renders the current page up front so something is visible immediately
    /// after a load, rotation or mode change instead of a placeholder. Completes
    /// synchronously in the default foreground mode; with <see cref="BackgroundRendering"/>
    /// the loading shimmer covers the brief hop to a worker thread.</summary>
    private Task RenderCurrentPageEagerlyAsync()
    {
        if (_pages.Count == 0) return Task.CompletedTask;
        int center = Math.Clamp(CurrentPage - 1, 0, _pages.Count - 1);
        return RenderPageAsync(center);
    }

    // Cap how many pages stay materialized so a large document does not grow the
    // DOM (and Blazor Server circuit memory) unbounded. Evicted pages revert to
    // placeholders and are re-rendered lazily when scrolled back into view.
    private int MaxRenderedPages => MaxRenderedPageCount > 0 ? MaxRenderedPageCount : 24;

    private void EvictDistantPages()
    {
        // Print() is rendering ALL pages for the print dialog; evicting any of
        // them now would print placeholders. Print resumes eviction when done.
        if (_printing) return;

        int rendered = 0;
        foreach (var p in _pages)
        {
            if (p is not null)
            {
                rendered++;
            }
        }
        if (rendered <= MaxRenderedPages) return;

        // Keep a window centered on the current page; drop everything outside it.
        int half = MaxRenderedPages / 2;
        int keepLo = Math.Max(0, CurrentPage - 1 - half);
        int keepHi = Math.Min(_pages.Count - 1, CurrentPage - 1 + half);
        for (int i = 0; i < _pages.Count; i++)
        {
            if ((i < keepLo || i > keepHi) && _pages[i] is not null)
            {
                _pages[i] = null;
                // Evicting is not a failure: a page dropped to save memory is asked for
                // again when it comes back into view.
                _failedPages.Remove(i);
                // The display list (with its base64 images) is regenerated when
                // the page re-renders; don't hold it for evicted pages.
                _canvasOps.Remove(i);
                _canvasDirty.Remove(i);
            }
        }
    }

    /// <summary>
    /// The heavy, offloadable half of rendering a thumbnail - <see cref="BuildPage"/>'s
    /// sidebar counterpart, with the same reload-safety contract (a null-document
    /// build returns an empty, discardable result the caller's guard drops).
    /// Canvas mode renders self-contained HTML instead of the page's canvas
    /// placeholder - the JS paint targets the MAIN surface only, so a placeholder
    /// would show a blank thumbnail (Compact text keeps the tiny fragments light).
    /// </summary>
    private BitPdfPageBuild BuildThumb(int index, int rotation, BitPdfTextCoalescing textCoalescing, BitPdfRenderMode renderMode)
    {
        if (renderMode != BitPdfRenderMode.Canvas)
        {
            return BuildPage(index, rotation, textCoalescing, renderMode);
        }

        var doc = _document;
        if (doc is null || index < 0 || index >= doc.Pages.Count)
        {
            return new BitPdfPageBuild(string.Empty, null);
        }
        var store = _fontStore ??= new BitPdfFontStore();
        var renderer = new BitPdfHtmlRenderer(doc.Pages[index], doc.XRef, store, rotation)
        {
            TextCoalescing = BitPdfTextCoalescing.Compact,
            HiddenLayers = _hiddenLayersView,
        };
        return new BitPdfPageBuild(renderer.Render(), null);
    }

    // Bound how many thumbnails stay materialized. A thumbnail fragment is as
    // heavy as a full page, so a large document scrolled end-to-end in the
    // sidebar would otherwise pin every page's markup in memory.
    private int MaxRenderedThumbs => MaxRenderedThumbnailCount > 0 ? MaxRenderedThumbnailCount : 40;

    private void EvictDistantThumbs(int visibleLo, int visibleHi)
    {
        int rendered = 0;
        foreach (var t in _thumbs)
        {
            if (t is not null)
            {
                rendered++;
            }
        }
        if (rendered <= MaxRenderedThumbs) return;

        // Keep the visible range plus an equal margin on each side, so nearby
        // thumbnails are already warm when the user keeps scrolling.
        int margin = Math.Max(0, (MaxRenderedThumbs - (visibleHi - visibleLo + 1)) / 2);
        int keepLo = Math.Max(0, visibleLo - margin);
        int keepHi = Math.Min(_thumbs.Count - 1, visibleHi + margin);
        for (int i = 0; i < _thumbs.Count; i++)
        {
            if ((i < keepLo || i > keepHi) && _thumbs[i] is not null)
            {
                _thumbs[i] = null;
            }
        }
    }

    // ----- Document properties dialog -----

    /// <summary>One row of the properties dialog: a label and the value to show,
    /// already reduced to text (an absent value becomes the "unknown" placeholder).</summary>
    private IEnumerable<(string Label, string Value)> PropertyRows()
    {
        var texts = ActiveTexts;
        BitPdfMetadata? meta = null;
        try
        {
            meta = _document?.Metadata;
        }
        catch { /* a damaged /Info dictionary must not break the dialog */ }

        yield return (texts.PropertyFileName, Text(_source?.FileName));
        yield return (texts.PropertyFileSize, _bytes is null ? texts.PropertyUnknown : FormatFileSize(_bytes.LongLength));
        yield return (texts.PropertyTitle, Text(meta?.Title));
        yield return (texts.PropertyAuthor, Text(meta?.Author));
        yield return (texts.PropertySubject, Text(meta?.Subject));
        yield return (texts.PropertyKeywords, Text(meta?.Keywords));
        yield return (texts.PropertyCreationDate, Text(FormatDate(meta?.CreationDate)));
        yield return (texts.PropertyModificationDate, Text(FormatDate(meta?.ModificationDate)));
        yield return (texts.PropertyCreator, Text(meta?.Creator));
        yield return (texts.PropertyProducer, Text(meta?.Producer));
        yield return (texts.PropertyVersion, Text(_document?.Version));
        yield return (texts.PropertyPageCount, _pages.Count > 0
            ? _pages.Count.ToString(System.Globalization.CultureInfo.CurrentCulture)
            : texts.PropertyUnknown);
        yield return (texts.PropertyPageSize, Text(CurrentPageSize));

        string Text(string? value) => string.IsNullOrWhiteSpace(value) ? texts.PropertyUnknown : value;
    }

    private static string? FormatDate(DateTimeOffset? value)
        => value?.LocalDateTime.ToString("g", System.Globalization.CultureInfo.CurrentCulture);

    /// <summary>The size of the current page in points and millimetres, the two
    /// units every desktop viewer shows in its properties dialog.</summary>
    private string? CurrentPageSize
    {
        get
        {
            int i = CurrentPage - 1;
            if (i < 0 || i >= _pageWidths.Count) return null;

            double w = _pageWidths[i], h = _pageHeights[i];
            const double mmPerPoint = 25.4 / 72;
            return string.Create(System.Globalization.CultureInfo.CurrentCulture,
                $"{w:0.#} × {h:0.#} pt ({w * mmPerPoint:0} × {h * mmPerPoint:0} mm)");
        }
    }

    /// <summary>A human-readable byte count (the binary units file managers use).</summary>
    private static string FormatFileSize(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB"];
        double value = bytes;
        int unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }
        var culture = System.Globalization.CultureInfo.CurrentCulture;
        return unit == 0
            ? string.Create(culture, $"{bytes} {units[0]}")
            : string.Create(culture, $"{value:0.##} {units[unit]}");
    }

    /// <summary>The document-defined label for the current page (e.g. "iv", "A-1")
    /// when it differs from the plain page number; otherwise <c>null</c>.</summary>
    private string? CurrentPageLabel
    {
        get
        {
            if (_document is null) return null;
            try
            {
                var labels = _document.PageLabels;
                int i = CurrentPage - 1;
                if (i < 0 || i >= labels.Count) return null;

                string label = labels[i];
                return label == CurrentPage.ToString(System.Globalization.CultureInfo.InvariantCulture) ? null : label;
            }
            catch
            {
                return null;
            }
        }
    }

    // Keeps the sidebar's active thumbnail in view, tolerating a torn-down circuit
    // (degrades to no auto-follow).
    private async Task ScrollActiveThumbIntoViewAsync()
    {
        try
        {
            await _js.BitPdfViewerScrollThumbIntoView(_thumbsRef, CurrentPage);
        }
        catch (JSDisconnectedException) { }
    }

    private ElementReference _pageInputRef;
    // An entry that moved nothing leaves the box showing what was typed: the value
    // Blazor would render is the one it rendered last time, so the attribute is not
    // re-emitted. Writing it back through the DOM is what puts the box right - and,
    // unlike re-creating the element, it does not take the reader's focus away.
    private bool _pageBoxResetPending;

    // Whether the document labels any page differently from its number. Worked out
    // once per document rather than per render: the answer cannot change while a
    // document is open, and the scan is one string comparison per page.
    private bool? _hasPageLabels;

    /// <summary>Whether the document labels any page differently from its number, which
    /// is what decides the page box's type and what may be typed into it.</summary>
    private bool HasPageLabels => _hasPageLabels ??= ComputeHasPageLabels();

    private bool ComputeHasPageLabels()
    {
        if (_document is null) return false;

        try
        {
            var labels = _document.PageLabels;
            for (int i = 0; i < labels.Count && i < _pages.Count; i++)
            {
                if (labels[i] != (i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture))
                {
                    return true;
                }
            }
        }
        catch { /* a damaged /PageLabels tree labels nothing */ }
        return false;
    }

    private async Task OnPageInput(ChangeEventArgs e)
    {
        int before = CurrentPage;
        string value = e.Value?.ToString() ?? string.Empty;

        // A LABEL wins over the number it looks like. A document that restarts its
        // numbering labels some page "1" while another page is the first one, and the
        // box shows labels - so what the reader types is read the way the box reads.
        if (PageOfLabel(value) is int labelled)
        {
            await GoToPage(labelled);
            RestorePageBox(before);
            return;
        }

        if (int.TryParse(value, out int n))
        {
            await GoToPage(n);
            // A number outside the document clamps, so the box may still be showing
            // something the viewer did not go to.
            RestorePageBox(before);
            return;
        }

        // Nothing usable was typed: put the box back on the page the reader is on,
        // rather than leaving it showing something that was ignored.
        RestorePageBox(before);
    }

    /// <summary>Puts the page box back in step with the page actually shown. Only needed
    /// when the entry moved nothing: the value Blazor would render is then the one it
    /// rendered last time, so the attribute is not re-emitted and the box keeps showing
    /// what was typed. Re-creating the element is what replaces it.</summary>
    private void RestorePageBox(int pageBefore)
    {
        if (CurrentPage == pageBefore)
        {
            _pageBoxResetPending = true;
        }
        StateHasChanged();
    }

    /// <summary>The 1-based page carrying <paramref name="label"/>, or <c>null</c> when
    /// the document labels no page that way.</summary>
    private int? PageOfLabel(string label)
    {
        label = label.Trim();
        if (label.Length == 0 || _document is null) return null;

        try
        {
            var labels = _document.PageLabels;
            for (int i = 0; i < labels.Count && i < _pages.Count; i++)
            {
                if (string.Equals(labels[i], label, StringComparison.OrdinalIgnoreCase))
                {
                    return i + 1;
                }
            }
        }
        catch { /* a damaged /PageLabels tree simply labels nothing */ }
        return null;
    }

    // The zoom bounds and step, sanitized: a host can pass anything, and an
    // inverted or degenerate range would otherwise clamp every zoom to nonsense.
    private double EffectiveMinZoom => Math.Min(MinZoom > 0 ? MinZoom : 0.1, EffectiveMaxZoom);

    private double EffectiveMaxZoom => MaxZoom > 0 ? MaxZoom : 8.0;

    private double EffectiveZoomStep => ZoomStep > 1 ? ZoomStep : 1.2;

    /// <summary>Applies a clamped zoom factor and raises <see cref="OnZoomChanged"/>
    /// when it actually changed. The zoom MODE is the caller's business.</summary>
    private async Task SetZoomValueAsync(double zoom)
    {
        double clamped = Math.Clamp(zoom, EffectiveMinZoom, EffectiveMaxZoom);
        if (Math.Abs(clamped - Zoom) < 0.0001) return;

        if (await AssignZoom(clamped) is false) return;
        _appliedZoom = Zoom;
        await OnZoomChanged.InvokeAsync(Zoom);
    }

    // The page index the current fit was computed for, so a document whose pages
    // differ in size can be re-fitted when the reader reaches a different one.
    private int _fitPageIndex = -1;

    private async Task ApplyFitAsync()
    {
        if (_pages.Count == 0 || _zoomMode is BitPdfZoomMode.Custom or BitPdfZoomMode.ActualSize) return;

        var vp = await _js.BitPdfViewerGetViewport(_containerRef);
        if (vp.Width <= 0) return;

        // Fit the page in front of the reader, not the biggest page anywhere in the
        // document: one oversized plate would otherwise shrink every other page.
        int index = Math.Clamp(CurrentPage - 1, 0, Math.Max(0, _pageWidths.Count - 1));
        double pw = index < _pageWidths.Count ? _pageWidths[index] : 612;
        double ph = index < _pageHeights.Count ? _pageHeights[index] : 792;
        // A spread puts two pages across the surface, so each gets half the width.
        double across = _spreadMode == BitPdfSpreadMode.None ? 1 : 2;
        const double padding = 32; // surface padding + page margin

        double fitWidth = (vp.Width - padding) / (pw * across);
        double fitHeight = (vp.Height - padding) / ph;
        _fitPageIndex = index;
        await SetZoomValueAsync(_zoomMode switch
        {
            BitPdfZoomMode.FitPage => Math.Min(fitWidth, fitHeight),
            BitPdfZoomMode.FitHeight => fitHeight,
            // Fit the width, but stop magnifying at the cap: a narrow page on a wide
            // screen reads better at 125% than blown up to the full width.
            BitPdfZoomMode.Automatic => Math.Min(fitWidth, AutomaticZoomCap),
            _ => fitWidth,
        });
    }

    /// <summary>Re-fits when the reader moved to a page of a different size while a
    /// fit mode is active. A same-sized page needs no interop round-trip, which is
    /// what keeps ordinary scrolling free of viewport measurements.</summary>
    private async Task RefitForCurrentPageAsync()
    {
        if (_zoomMode is BitPdfZoomMode.Custom or BitPdfZoomMode.ActualSize) return;

        int index = CurrentPage - 1;
        if (index < 0 || index >= _pageWidths.Count) return;
        if (_fitPageIndex >= 0 && _fitPageIndex < _pageWidths.Count
            && Math.Abs(_pageWidths[_fitPageIndex] - _pageWidths[index]) < 0.5
            && Math.Abs(_pageHeights[_fitPageIndex] - _pageHeights[index]) < 0.5)
        {
            return;
        }

        await ApplyFitAsync();
    }

    private Task ToggleThumbnails()
        => ShowSidebar(_showThumbnails ? BitPdfSidebar.None : BitPdfSidebar.Thumbnails);

    private Task ToggleOutline()
        => ShowSidebar(_showOutline ? BitPdfSidebar.None : BitPdfSidebar.Bookmarks);

    private Task ToggleAttachments()
        => ShowSidebar(_showAttachments ? BitPdfSidebar.None : BitPdfSidebar.Attachments);

    private Task ToggleLayers()
        => ShowSidebar(_showLayers ? BitPdfSidebar.None : BitPdfSidebar.Layers);

    private async Task OnOutlineClick(BitPdfOutlineItem item)
    {
        if (item.Destination is { } destination)
        {
            await GoToDestination(destination);
        }
        else if (item.PageNumber is int pageNo)
        {
            await GoToPage(pageNo);
        }
    }

    // ----- Search -----

    private string SearchLabel => _searchTotal switch
    {
        // A query that matches nothing says so in words: "0/0" reads as a counter
        // that has not run yet, which is exactly the wrong thing to tell a reader.
        <= 0 => string.IsNullOrEmpty(_searchQuery) ? "" : ActiveTexts.PhraseNotFound,
        _ => string.Format(ActiveTexts.MatchCountFormat, _searchIndex + 1, _searchTotal),
    };

    private async Task ToggleSearch()
    {
        _showSearch = !_showSearch;
        if (_showSearch)
        {
            // The box only enters the DOM with this render, so focus waits for it -
            // a find box the reader has to click before typing is half a find box.
            _focusSearchPending = true;
            return;
        }
        _searchQuery = "";
        await ClearSearchAsync();
    }

    // Bumped per keystroke so a search only starts once typing pauses; without it
    // every character would run a full index sweep on the UI thread.
    private int _searchInputGeneration;

    private async Task OnSearchInput(ChangeEventArgs e)
    {
        _searchQuery = e.Value?.ToString() ?? "";

        int generation = ++_searchInputGeneration;
        await Task.Delay(SearchDebounceMilliseconds);
        if (IsDisposed || generation != _searchInputGeneration) return;

        await RunSearchAsync();
        if (IsDisposed) return;

        StateHasChanged();
    }

    private const int SearchDebounceMilliseconds = 250;

    /// <summary>Toggles a find option and re-runs the current query against it, so
    /// the match count and highlights update without the user retyping.</summary>
    private async Task ToggleMatchCase()
    {
        _matchCase = !_matchCase;
        await RunSearchAsync();
    }

    private async Task ToggleWholeWord()
    {
        _wholeWord = !_wholeWord;
        await RunSearchAsync();
    }

    private async Task ToggleMatchDiacritics()
    {
        _matchDiacritics = !_matchDiacritics;
        // The canonical index is keyed to the previous setting; drop it so the next
        // count folds (or stops folding) the pages it re-reads.
        _pageSearch = null;
        await RunSearchAsync();
    }

    /// <summary>Toggles whether every match is painted or only the current one. No
    /// re-count is needed - the same matches are simply decorated differently.</summary>
    private async Task ToggleHighlightAll()
    {
        _highlightAll = !_highlightAll;
        await ApplyHighlightsAsync(scrollToCurrent: false);
    }

    /// <summary>Activates a control on Enter or Space, so keyboard users can
    /// operate the thumbnail list and outline tree like buttons.</summary>
    private static bool IsActivationKey(KeyboardEventArgs e) => e.Key is "Enter" or " " or "Spacebar";

    /// <summary>Keyboard handling for the thumbnail listbox: activation plus the
    /// arrow/Home/End roving the ARIA listbox pattern expects. The active option is
    /// the only one in the tab order, so arrowing moves both selection and focus.</summary>
    private async Task OnThumbKeyDown(KeyboardEventArgs e, int pageNo)
    {
        if (IsActivationKey(e))
        {
            await GoToPage(pageNo);
            return;
        }

        int target = e.Key switch
        {
            "ArrowDown" or "ArrowRight" => pageNo + 1,
            "ArrowUp" or "ArrowLeft" => pageNo - 1,
            "Home" => 1,
            "End" => _thumbs.Count,
            _ => 0,
        };
        if (target == 0) return;

        await GoToPage(target);
        // The focused element left the tab order when the active thumbnail moved;
        // follow the selection so the next arrow key still reaches this listbox.
        try
        {
            await _js.BitPdfViewerFocusThumb(_thumbsRef, CurrentPage);
        }
        catch (JSDisconnectedException) { }
    }

    /// <summary>Keyboard handling for the bookmarks tree: activation plus the
    /// left/right keys that fold and unfold a branch, as the ARIA tree pattern
    /// expects.</summary>
    private async Task OnOutlineKeyDown(KeyboardEventArgs e, BitPdfOutlineItem item)
    {
        if (IsActivationKey(e))
        {
            _focusedOutline = item;
            await OnOutlineClick(item);
            return;
        }

        bool hasChildren = item.Children.Count > 0;
        bool collapsed = IsOutlineCollapsed(item);

        // Right unfolds a folded branch and steps into an open one; left folds an open
        // branch and steps out of a leaf - the tree pattern every desktop viewer uses.
        if (e.Key == "ArrowRight" && hasChildren)
        {
            if (collapsed)
            {
                _collapsedOutline.Remove(item);
                await FocusOutlineItem(item);
            }
            else
            {
                await FocusOutlineItem(item.Children[0]);
            }
            return;
        }
        if (e.Key == "ArrowLeft")
        {
            if (hasChildren && collapsed is false)
            {
                _collapsedOutline.Add(item);
                await FocusOutlineItem(item);
            }
            else if (ParentOf(item) is { } parent)
            {
                await FocusOutlineItem(parent);
            }
            return;
        }

        var flat = VisibleOutlineItems();
        int index = flat.FindIndex(i => ReferenceEquals(i, item));
        if (index < 0) return;

        int target = e.Key switch
        {
            "ArrowDown" => index + 1,
            "ArrowUp" => index - 1,
            "Home" => 0,
            "End" => flat.Count - 1,
            _ => -1,
        };
        if (target < 0 || target >= flat.Count) return;

        await FocusOutlineItem(flat[target]);
    }

    /// <summary>The bookmark holding <paramref name="child"/>, or <c>null</c> for a
    /// top-level one.</summary>
    private BitPdfOutlineItem? ParentOf(BitPdfOutlineItem child)
    {
        return Search(_outline, null);

        BitPdfOutlineItem? Search(IReadOnlyList<BitPdfOutlineItem> items, BitPdfOutlineItem? parent)
        {
            foreach (var item in items)
            {
                if (ReferenceEquals(item, child)) return parent;

                if (Search(item.Children, item) is { } hit) return hit;
            }
            return null;
        }
    }

    private async Task OnSearchKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            if (_searchTotal > 0)
            {
                await (e.ShiftKey ? SearchPrev() : SearchNext());
            }
        }
        else if (e.Key == "Escape")
        {
            await ToggleSearch();
        }
    }

    private async Task RunSearchAsync()
    {
        if (_document is null) return;

        // Supersede any search still in flight: OnSearchInput fires per change and an
        // earlier RunSearchAsync may still be mid-await. Bump the generation so that
        // older run abandons at its next checkpoint instead of publishing stale counts.
        int generation = ++_searchGeneration;

        if (string.IsNullOrEmpty(_searchQuery))
        {
            await ClearSearchAsync();
            return;
        }

        _loading = true;
        StateHasChanged();
        await Task.Delay(1);

        // A reload or disposal during the yield may have cleared _document; bail
        // before initializing the text index or reading the page count.
        if (IsDisposed || _document is null || generation != _searchGeneration) return;
        int version = _loadVersion; // captured only after confirming the component is still valid

        // Matches are COUNTED in C# over the per-page extracted-text index, never by
        // rendering pages and asking the DOM. A 500-page document with matches on 400
        // of them therefore costs 400 string scans, not 400 page fragments in the DOM
        // (which is what an earlier "render every hit" pass cost). Rendering happens
        // one page at a time, when the reader actually walks to a match.
        _pageText ??= new string?[_document.PageCount];
        int pageCount = _document.PageCount; // captured so the loop condition never reads a nulled _document
        if (_pageSearch is null || _pageSearch.Length != pageCount)
        {
            _pageSearch = new string?[pageCount];
        }
        // The needle is canonicalized once for the whole sweep, under the same rule the
        // haystack is, so the counter and the browser-side highlighter agree.
        string needle = CanonicalizeSearchText(_searchQuery);
        var counts = new int[pageCount];
        int total = 0;
        for (int i = 0; i < pageCount; i++)
        {
            // A reload, disposal or newer query during a yield supersedes this run;
            // stop before touching shared state (mirrors the render pumps' guard).
            if (IsDisposed || version != _loadVersion || generation != _searchGeneration) return;

            _pageText[i] ??= _document.Pages[i].ExtractText();
            counts[i] = CountMatches(SearchTextOf(i), needle, _matchCase, _wholeWord);
            total += counts[i];

            if ((i & 31) == 31)
            {
                // The first search over a large document extracts text for every page
                // synchronously; yield every 32 pages so that sweep doesn't freeze the
                // UI thread.
                await Task.Delay(1);
                if (IsDisposed || version != _loadVersion || generation != _searchGeneration) return;
            }
        }

        _loading = false;
        _matchCounts = counts;
        _searchTotal = total;
        _searchIndex = total > 0 ? 0 : -1;
        StateHasChanged();

        if (total > 0)
        {
            await GotoMatch(0);
        }
        else
        {
            await ApplyHighlightsAsync(scrollToCurrent: false);
        }
    }

    // The per-page extracted text in the canonical form searches run over, built
    // lazily beside _pageText and dropped whenever the option (or the document)
    // changes.
    private string?[]? _pageSearch;

    /// <summary>The page text the current find options search over: the extracted
    /// text canonicalized by <see cref="CanonicalizeSearchText"/>.</summary>
    private string SearchTextOf(int index)
    {
        if (_pageSearch is null) return CanonicalizeSearchText(_pageText![index] ?? string.Empty);

        return _pageSearch[index] ??= CanonicalizeSearchText(_pageText![index] ?? string.Empty);
    }

    /// <summary>
    /// The one form both sides of the search compare over. The counter reads the
    /// extracted text (a content-stream replay, which marks a line with a newline and a
    /// word gap with a space) while the highlighter reads the rendered selection
    /// layer (one run per visual line, joined by a line break, its word gaps inferred
    /// from glyph positions). Collapsing every run of whitespace to a single space -
    /// on both sides - is what stops those two heuristics from disagreeing about how
    /// many matches a page holds, and so about which occurrence is the current one.
    /// Diacritic folding, when it applies, happens first so both rules compose.
    /// </summary>
    private string CanonicalizeSearchText(string text)
        => CollapseWhitespace(_matchDiacritics ? text : FoldDiacritics(text));

    /// <summary>Replaces every run of whitespace with a single space. The browser-side
    /// highlighter applies the identical rule (and keeps a map back to the original
    /// offsets), so a hit found here addresses the same characters there.</summary>
    private static string CollapseWhitespace(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        var builder = new System.Text.StringBuilder(text.Length);
        bool pendingSpace = false;
        foreach (char c in text)
        {
            if (char.IsWhiteSpace(c))
            {
                pendingSpace = true;
                continue;
            }
            if (pendingSpace)
            {
                builder.Append(' ');
                pendingSpace = false;
            }
            builder.Append(c);
        }
        // A trailing run counts too: dropping it would make "end " and "end" the same
        // haystack tail on one side only.
        if (pendingSpace)
        {
            builder.Append(' ');
        }
        return builder.ToString();
    }

    /// <summary>
    /// Strips the combining marks of <paramref name="text"/> so an accented letter and
    /// its bare form compare equal - the rule the find box applies while "match
    /// diacritics" is off. Decomposing first is what turns a precomposed "é" into
    /// "e" plus a mark to drop; the browser-side highlighter folds identically (and
    /// keeps a map back to the original offsets) so both agree on what matched.
    /// </summary>
    private static string FoldDiacritics(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        string decomposed = text.Normalize(System.Text.NormalizationForm.FormD);
        var builder = new System.Text.StringBuilder(decomposed.Length);
        foreach (char c in decomposed)
        {
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)
                is not System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                builder.Append(c);
            }
        }
        return builder.ToString();
    }

    /// <summary>
    /// Counts the occurrences of <paramref name="needle"/> in <paramref name="text"/>
    /// under the current find options. This is the same rule the browser-side
    /// highlighter applies, so the counter and the highlights agree.
    /// </summary>
    private static int CountMatches(string text, string needle, bool matchCase, bool wholeWord)
    {
        if (string.IsNullOrEmpty(needle) || string.IsNullOrEmpty(text)) return 0;

        var comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        int count = 0;
        int index = 0;
        while (index <= text.Length - needle.Length)
        {
            int hit = text.IndexOf(needle, index, comparison);
            if (hit < 0) break;

            int end = hit + needle.Length;
            if (wholeWord is false || (IsWordBoundary(text, hit - 1) && IsWordBoundary(text, end)))
            {
                count++;
                index = end;
            }
            else
            {
                // A rejected whole-word hit may still overlap a later accepted one.
                index = hit + 1;
            }
        }
        return count;
    }

    /// <summary>Whether the character at <paramref name="index"/> is absent or is not
    /// a word character, which is what makes the position a word boundary.</summary>
    private static bool IsWordBoundary(string text, int index)
    {
        if (index < 0 || index >= text.Length) return true;

        char c = text[index];
        // Letters, digits, combining marks and the underscore are word characters -
        // the same class the browser-side highlighter's \p{L}\p{N}\p{M}_ matches, so
        // an accented or non-Latin word behaves like an ASCII one.
        if (char.IsLetterOrDigit(c) || c == '_') return false;

        var category = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
        return category is not (System.Globalization.UnicodeCategory.NonSpacingMark
            or System.Globalization.UnicodeCategory.SpacingCombiningMark
            or System.Globalization.UnicodeCategory.EnclosingMark);
    }

    private Task SearchNext() => FindNext();

    private Task SearchPrev() => FindPrevious();

    private async Task GotoMatch(int index)
    {
        if (_searchTotal <= 0 || _matchCounts is null) return;

        _searchIndex = ((index % _searchTotal) + _searchTotal) % _searchTotal;

        // Resolve the flat match index to the page holding it and its ordinal within
        // that page, then bring that one page in - the rest of the document stays
        // unrendered however many matches it holds.
        int remaining = _searchIndex;
        int page = 0;
        while (page < _matchCounts.Length && remaining >= _matchCounts[page])
        {
            remaining -= _matchCounts[page];
            page++;
        }
        if (page >= _matchCounts.Length) return;

        _matchPage = page + 1;
        _matchOrdinal = remaining;
        await GoToPage(_matchPage);
        if (IsDisposed) return;

        await ApplyHighlightsAsync(scrollToCurrent: true);
    }

    // Where the current match sits: its 1-based page and its ordinal on that page.
    private int _matchPage;
    private int _matchOrdinal;

    /// <summary>
    /// Paints the query's matches across the pages that are currently in the DOM and
    /// marks the current one. Runs again after every lazy page render, so a match
    /// scrolled into view is highlighted the moment its page materializes.
    /// </summary>
    private async Task ApplyHighlightsAsync(bool scrollToCurrent)
    {
        try
        {
            await _js.BitPdfViewerHighlight(_containerRef, _searchQuery, _matchCase, _wholeWord,
                _matchDiacritics, _highlightAll,
                _searchTotal > 0 ? _matchPage : 0, _matchOrdinal, scrollToCurrent);
        }
        catch (JSDisconnectedException) { }
    }

    private async Task ClearSearchAsync()
    {
        // Shared invalidation for every "clear" path (empty query, closing the box):
        // bump the generation so any search still in flight abandons at its next
        // checkpoint, and clear its progress bar here - the abandoning run returns
        // early via the generation guard and so never runs its own `_loading = false`.
        _searchGeneration++;
        _loading = false;
        _searchTotal = 0;
        _searchIndex = -1;
        _matchCounts = null;
        _matchPage = 0;
        _matchOrdinal = 0;
        try
        {
            await _js.BitPdfViewerClearSearch(_containerRef);
        }
        catch (JSDisconnectedException) { }
    }

    private string PageStyle(int index)
    {
        double pw = index < _pageWidths.Count ? _pageWidths[index] : 612;
        double ph = index < _pageHeights.Count ? _pageHeights[index] : 792;
        string style = string.Create(System.Globalization.CultureInfo.InvariantCulture,
            $"width:{pw * Zoom:0.#}px;height:{ph * Zoom:0.#}px;--bit-pdv-scale:{Zoom:0.####}");
        // A page's size is computed, so a host style is appended to it rather than
        // replacing it - otherwise Styles.Page would strip the page's own geometry.
        return Styles?.Page is { Length: > 0 } custom ? $"{style};{custom}" : style;
    }

    // ----- Layout -----

    /// <summary>The layout modifier the pages container carries, so the whole surface
    /// re-flows from CSS rather than from a second rendering path.</summary>
    private string PagesLayoutClass
    {
        get
        {
            string axis = _scrollMode switch
            {
                BitPdfScrollMode.Horizontal => " bit-pdv-h",
                BitPdfScrollMode.Wrapped => " bit-pdv-w",
                _ => string.Empty,
            };
            // Page mode hides everything but the current page (or spread) instead of
            // rendering a second time, so the slots, the spy and eviction are untouched.
            return _scrollMode == BitPdfScrollMode.Page ? axis + " bit-pdv-single" : axis;
        }
    }

    /// <summary>The axis the JS lazy-render pass measures along. Only a horizontal
    /// run needs the other one: pages wrap downwards in every other mode, so their
    /// document order stays monotonic in <c>top</c>.</summary>
    private string? PagesAxis => _scrollMode == BitPdfScrollMode.Horizontal ? "h" : null;

    /// <summary>The pages of the document grouped into the rows the current spread
    /// mode asks for. Each row holds one page index, or the two of a spread.</summary>
    private List<List<int>> PageRows()
    {
        var rows = new List<List<int>>();
        if (_spreadMode == BitPdfSpreadMode.None)
        {
            for (int i = 0; i < _pages.Count; i++)
            {
                rows.Add([i]);
            }
            return rows;
        }

        int start = 0;
        // Even pages start a spread, so the first page falls outside the pairing and
        // stands alone - which is what makes page 1 read as a cover.
        if (_spreadMode == BitPdfSpreadMode.Even && _pages.Count > 0)
        {
            rows.Add([0]);
            start = 1;
        }
        for (int i = start; i < _pages.Count; i += 2)
        {
            rows.Add(i + 1 < _pages.Count ? [i, i + 1] : [i]);
        }
        return rows;
    }

    /// <summary>Whether a row holds the current page, which is the one row Page mode
    /// leaves visible.</summary>
    private bool IsCurrentRow(List<int> row) => row.Contains(CurrentPage - 1);

    private Task OnScrollModeChanged(ChangeEventArgs e)
        => Enum.TryParse<BitPdfScrollMode>(e.Value?.ToString(), out var mode)
            ? SetScrollMode(mode)
            : Task.CompletedTask;

    private Task OnSpreadModeChanged(ChangeEventArgs e)
        => Enum.TryParse<BitPdfSpreadMode>(e.Value?.ToString(), out var mode)
            ? SetSpreadMode(mode)
            : Task.CompletedTask;

    private void ToggleCursorTool()
        => SetCursorTool(_cursorTool == BitPdfCursorTool.Pan ? BitPdfCursorTool.Select : BitPdfCursorTool.Pan);

    // The percentages every desktop viewer offers in its zoom dropdown, so a
    // reader can jump straight to a known scale instead of stepping there.
    private static readonly double[] DefaultZoomPresets = [0.5, 0.75, 1, 1.25, 1.5, 2, 3, 4];

    /// <summary>The presets inside the configured zoom bounds, in ascending order and
    /// without duplicates - a host-supplied list is otherwise free to be neither.</summary>
    private IEnumerable<double> AvailableZoomPresets
        => (ZoomPresets ?? DefaultZoomPresets)
            .Where(z => z > 0 && z >= EffectiveMinZoom && z <= EffectiveMaxZoom)
            .Distinct()
            .OrderBy(z => z);

    private static string ZoomPresetLabel(double zoom)
        => string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{Math.Round(zoom * 100)}%");

    /// <summary>The caption of a thumbnail: the document's own page label when it
    /// differs from the page number, so a reader sees the same "iv" the pages show.</summary>
    private string ThumbLabel(int index)
    {
        int pageNo = index + 1;
        string number = pageNo.ToString(System.Globalization.CultureInfo.InvariantCulture);
        try
        {
            var labels = _document?.PageLabels;
            if (labels is not null && index < labels.Count && string.IsNullOrEmpty(labels[index]) is false)
            {
                return labels[index];
            }
        }
        catch { /* a damaged /PageLabels tree falls back to the plain number */ }
        return number;
    }

    // Bookmarks the reader has folded shut. Keyed by the item instance, which the
    // outline builder creates once per load, so a reload starts fully expanded.
    private readonly HashSet<BitPdfOutlineItem> _collapsedOutline = new(ReferenceEqualityComparer.Instance);

    private bool IsOutlineCollapsed(BitPdfOutlineItem item) => _collapsedOutline.Contains(item);

    private void ToggleOutlineItem(BitPdfOutlineItem item)
    {
        if (_collapsedOutline.Remove(item) is false)
        {
            _collapsedOutline.Add(item);
        }
    }

    // The bookmark the tree's single tab stop sits on. An ARIA tree is one tab stop
    // whose arrow keys move focus between items, so exactly one item may be tabbable.
    private BitPdfOutlineItem? _focusedOutline;

    /// <summary>The bookmarks currently visible in the tree, in the order they are
    /// painted - the order the arrow keys walk. A folded branch's children are not in
    /// it, because focus cannot land on something that is not on screen.</summary>
    private List<BitPdfOutlineItem> VisibleOutlineItems()
    {
        var flat = new List<BitPdfOutlineItem>();
        Walk(_outline);
        return flat;

        void Walk(IReadOnlyList<BitPdfOutlineItem> items)
        {
            foreach (var item in items)
            {
                flat.Add(item);
                if (item.Children.Count > 0 && IsOutlineCollapsed(item) is false)
                {
                    Walk(item.Children);
                }
            }
        }
    }

    /// <summary>Whether this bookmark is the tree's tab stop. Before anything has been
    /// focused the first one is, so Tab always reaches the tree.</summary>
    private bool IsOutlineTabStop(BitPdfOutlineItem item)
        => _focusedOutline is null
            ? ReferenceEquals(_outline.Count > 0 ? _outline[0] : null, item)
            : ReferenceEquals(_focusedOutline, item);

    /// <summary>Moves the tree's tab stop and the DOM focus with it, which is what the
    /// arrow keys of an ARIA tree do.</summary>
    private async Task FocusOutlineItem(BitPdfOutlineItem item)
    {
        var flat = VisibleOutlineItems();
        int index = flat.FindIndex(i => ReferenceEquals(i, item));
        if (index < 0) return;

        _focusedOutline = item;
        StateHasChanged();
        try
        {
            await _js.BitPdfViewerFocusOutlineItem(RootElement, index);
        }
        catch (JSDisconnectedException) { }
    }

    private string ThumbStyle(int index)
    {
        const double target = 130.0; // thumbnail content width in px
        double pw = index < _pageWidths.Count ? _pageWidths[index] : 612;
        double ph = index < _pageHeights.Count ? _pageHeights[index] : 792;
        double scale = pw > 0 ? target / pw : 0.2;
        return string.Create(System.Globalization.CultureInfo.InvariantCulture,
            $"position:relative;width:{pw * scale:0.#}px;height:{ph * scale:0.#}px;overflow:hidden;--bit-pdv-scale:{scale:0.####}");
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        // Prevent any new render work from starting: the pumps and the render methods
        // all bail on IsDisposed. base.DisposeAsync sets this too, but only at the end
        // of this method - set it up front so those guards take effect during disposal.
        IsDisposed = true;

        // Supersede any load still in flight: several LoadAsync continuations resume
        // from an await and check only `version != _loadVersion` (not IsDisposed)
        // before publishing state or invoking callbacks. Bumping the version here
        // invalidates all of them at once, so none commits against a torn-down component.
        _loadVersion++;

        // Stop the lazy-render pumps: clear pending work so a pump resuming from its
        // Task.Delay yield after disposal finds nothing left to render against the
        // torn-down component.
        _renderQueue.Clear();
        _renderQueued.Clear();
        _thumbQueue.Clear();
        _thumbQueued.Clear();

        // Release a print parked on the canvas-paint barrier: OnAfterRenderAsync won't
        // run again to complete it once disposal starts. The print resumes, sees the
        // bumped version / IsDisposed, and bails.
        _canvasPaintSignal?.TrySetResult();
        _canvasPaintSignal = null;

        // Release a load parked on the password dialog: nobody will answer it now,
        // and the load resumes only to see the bumped version and bail.
        CompletePasswordRequest(null);

        // Wait for any in-flight render to release the gate before disposing it, so a
        // background build that is mid-flight can run its finally (Release) without
        // faulting on a disposed semaphore. New renders are already blocked above.
        try
        {
            await _renderGate.WaitAsync();
        }
        catch (ObjectDisposedException) { } // Already disposed; nothing to drain.

        try
        {
            await _js.BitPdfViewerDisposeScrollSpy(_containerRef);
            await _js.BitPdfViewerDisposeThumbSpy(_thumbsRef);
            await _js.BitPdfViewerDisposeKeyboard(RootElement);
            await _js.BitPdfViewerDisposeDropZone(RootElement);
            await _js.BitPdfViewerDisposeFullscreenSpy(RootElement);
        }
        catch (JSDisconnectedException) { } // Circuit already gone; nothing to clean up.
        catch (TaskCanceledException) { } // Disposal raced an in-flight interop call; safe to ignore.

        _dotnetObj?.Dispose();
        _renderGate.Dispose();

        await base.DisposeAsync(disposing);
    }
}
