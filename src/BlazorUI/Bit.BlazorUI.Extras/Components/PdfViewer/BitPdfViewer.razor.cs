using System.Net.Http;
using System.Diagnostics.CodeAnalysis;

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
    private BitPdfDocument? _document;
    private BitPdfFontStore? _fontStore;
    private bool _correctWidthsPending; // run the JS text width-correction after render
    private string?[]? _pageText; // lazily-built per-page text index for search
    private int _loadVersion; // bumped per load; guards against a superseded load committing
    private int _renderEpoch; // bumped whenever page slots are rebuilt (load, rotation, mode change)
    private string _status = "Idle.";
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

    private int _currentPage = 1;
    private double _zoom = 1.0;
    private BitPdfZoomMode _zoomMode = BitPdfZoomMode.FitWidth;
    private BitPdfTextCoalescing _textCoalescing; // last applied; changes re-render pages
    private BitPdfRenderMode _renderMode;         // last applied; changes re-render pages
    private int _rotation;

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
    private IReadOnlyList<BitPdfOutlineItem> _outline = [];

    private bool _showSearch;
    private string _searchQuery = "";
    private int _searchTotal;
    private int _searchIndex = -1;
    private int _searchGeneration; // bumped per query so an in-flight search abandons when a newer query starts

    private DotNetObjectReference<BitPdfViewer>? _dotnetObj;
    private ElementReference _containerRef;
    private ElementReference _thumbsRef;
    private bool _spyPending;
    private bool _thumbSpyPending; // (re)attach the sidebar's lazy-render spy after render



    [Inject] private IJSRuntime _js { get; set; } = default!;

    [Inject] private IServiceProvider _services { get; set; } = default!;



    /// <summary>
    /// The document to display.
    /// </summary>
    [Parameter] public BitPdfSource? Source { get; set; }

    /// <summary>
    /// The CSS height of the viewer container. When not set, the viewer height is
    /// responsive: capped at 780px and shrinking to fit the viewport on small screens.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public string? Height { get; set; }

    /// <summary>
    /// Whether the toolbar is shown.
    /// </summary>
    [Parameter] public bool ShowToolbar { get; set; } = true;

    /// <summary>
    /// The initial zoom behavior.
    /// </summary>
    [Parameter] public BitPdfZoomMode InitialZoomMode { get; set; } = BitPdfZoomMode.FitWidth;

    /// <summary>
    /// How painted text is emitted. <see cref="BitPdfTextCoalescing.Compact"/> merges
    /// same-line, same-style runs into one span per visual line — far fewer DOM
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
    /// runtime actually provides a spare thread — Blazor Server, or a Blazor
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
    /// The callback for when the focused page changes (with the 1-based page number).
    /// </summary>
    [Parameter] public EventCallback<int> OnPageChanged { get; set; }

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
    /// retry, or <c>null</c>/empty to cancel. If unset, a password error surfaces
    /// through <see cref="OnError"/> instead.
    /// </summary>
    [Parameter] public Func<Task<string?>>? OnPasswordRequested { get; set; }



    /// <summary>
    /// The number of pages of the current document.
    /// </summary>
    public int PageCount => _pages.Count;

    /// <summary>
    /// The currently focused page (1-based).
    /// </summary>
    public int CurrentPage => _currentPage;

    /// <summary>
    /// The current zoom factor (1 means 100%).
    /// </summary>
    public double Zoom => _zoom;

    /// <summary>
    /// Whether the document exposes any bookmarks.
    /// </summary>
    public bool HasOutline => _outline.Count > 0;

    /// <summary>
    /// Navigates to the next page.
    /// </summary>
    public Task NextPage() => GoToPage(_currentPage + 1);

    /// <summary>
    /// Navigates to the previous page.
    /// </summary>
    public Task PrevPage() => GoToPage(_currentPage - 1);

    /// <summary>
    /// Navigates to the provided page number (1-based).
    /// </summary>
    public async Task GoToPage(int pageNumber)
    {
        if (_pages.Count == 0) return;

        int version = _loadVersion; // a reload during the awaits below supersedes this navigation
        int target = Math.Clamp(pageNumber, 1, _pages.Count);
        if (target != _currentPage)
        {
            _currentPage = target;
            await OnPageChanged.InvokeAsync(_currentPage);
            // OnPageChanged is user code: a reload (or new Source) during it makes
            // this navigation stale, and a newer GoToPage or a scroll-spy update
            // (OnPageVisible) may have moved _currentPage on — either way this
            // navigation is superseded, so don't render or scroll for it.
            if (version != _loadVersion || _currentPage != target) return;
        }

        // Render the destination before scrolling so jumps (toolbar, thumbnails,
        // outline) land on content instead of a placeholder.
        if (await RenderPageAsync(target - 1) && version == _loadVersion && _currentPage == target)
        {
            EvictDistantPages();
            StateHasChanged();
        }

        // RenderPageAsync may have yielded; if the component was disposed, this load
        // was superseded, or a newer navigation moved on in that window, don't drive
        // JS for this stale target. Scroll to the captured target, not the mutable
        // _currentPage, so a concurrent update can't redirect this call's scroll.
        if (IsDisposed || version != _loadVersion || _currentPage != target) return;

        await _js.BitPdfViewerScrollToPage(_containerRef, target);
        if (_showThumbnails)
        {
            await ScrollActiveThumbIntoViewAsync();
        }
    }

    /// <summary>
    /// Zooms in by 20%.
    /// </summary>
    public Task ZoomIn() => SetCustomZoom(_zoom * 1.2);

    /// <summary>
    /// Zooms out by 20%.
    /// </summary>
    public Task ZoomOut() => SetCustomZoom(_zoom / 1.2);

    /// <summary>
    /// Sets the zoom mode (fit-width, fit-page, actual size or custom).
    /// </summary>
    public async Task SetZoomMode(BitPdfZoomMode mode)
    {
        _zoomMode = mode;
        if (mode == BitPdfZoomMode.ActualSize)
        {
            _zoom = 1.0;
        }
        else
        {
            await ApplyFitAsync();
        }
    }

    /// <summary>
    /// Rotates all pages 90 degrees clockwise.
    /// </summary>
    public async Task RotateClockwise()
    {
        _rotation = (_rotation + 90) % 360;
        PreparePages();
        await RenderCurrentPageEagerlyAsync();
        _spyPending = true;
    }

    /// <summary>
    /// Downloads the original document bytes.
    /// </summary>
    public async Task Download()
    {
        if (_source?.Bytes is null) return;

        // Stream the bytes as a Blob rather than pushing a base64 data: URI (which
        // on Blazor Server would traverse SignalR as one huge string).
        using var stream = new MemoryStream(_source.Bytes, writable: false);
        using var streamRef = new DotNetStreamReference(stream, leaveOpen: true);
        await _js.BitPdfViewerDownload(_source.FileName ?? "document.pdf", streamRef);
    }

    /// <summary>
    /// Opens the browser print dialog with all pages of the document.
    /// </summary>
    public async Task Print()
    {
        if (_document is null || _pages.Count == 0) return;
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
        // the print rather than letting it render into — or print — cleared slots.
        int epoch = _renderEpoch;
        bool rendered = false;
        // Suspend eviction while catching up: the lazy-render pump can run during
        // the yields below and would otherwise evict pages this pass has already
        // rendered (the loop only moves forward), printing placeholders.
        _printing = true;
        try
        {
            for (int i = 0; i < _pages.Count; i++)
            {
                if (_pages[i] is null)
                {
                    if (!rendered)
                    {
                        _loading = true;
                        _status = "Preparing all pages for printing…";
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
                    // lazy pump is fine — only a still-empty slot means a real failure.
                    if (!ok && _pages[i] is null)
                    {
                        _status = "Printing aborted: a page failed to render.";
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
            await _js.BitPdfViewerPrint(_containerRef);
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
    /// Renders a single page (1-based) to self-contained HTML, or an
    /// empty string when no document is loaded or the number is out of range.
    /// </summary>
    public string RenderPageHtml(int pageNumber)
    {
        if (_document is null || pageNumber < 1 || pageNumber > _document.PageCount)
        {
            return string.Empty;
        }
        return new BitPdfHtmlRenderer(_document.Pages[pageNumber - 1], _document.XRef, _rotation)
        {
            TextCoalescing = TextCoalescing,
        }.Render();
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
            if (idx < 0 || idx >= _pages.Count || _pages[idx] is not null) continue;
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
                    // reload or disposal in that window means the slots are torn down —
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
            if (idx < 0 || idx >= _thumbs.Count || _thumbs[idx] is not null) continue;
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
                    // showing), not the current page — scrolling the sidebar leaves the
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
    public void OnPageVisible(int pageNumber)
    {
        if (pageNumber != _currentPage && pageNumber >= 1 && pageNumber <= _pages.Count)
        {
            _currentPage = pageNumber;
            _ = OnPageChanged.InvokeAsync(pageNumber);
            // Keep the sidebar's active thumbnail in view as the main surface
            // scrolls, so lazy-loaded thumbnails follow the reader.
            if (_showThumbnails)
            {
                _ = ScrollActiveThumbIntoViewAsync();
            }
            StateHasChanged();
        }
    }

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
        await SetCustomZoom(deltaY < 0 ? _zoom * 1.1 : _zoom / 1.1);
        StateHasChanged();
    }



    protected override string RootElementClass => "bit-pdv";

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Height.HasValue() ? $"height:{Height}" : string.Empty);
    }

    protected override void OnInitialized()
    {
        _zoomMode = InitialZoomMode;

        base.OnInitialized();
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        if (ReferenceEquals(_source, Source) is false)
        {
            _source = Source;
            _rotation = 0;
            _currentPage = 1;
            _textCoalescing = TextCoalescing;
            _renderMode = RenderMode;
            await LoadAsync();
            return;
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
                await _js.BitPdfViewerScrollThumbIntoView(_thumbsRef, _currentPage);
            }
            catch (JSDisconnectedException) { } // Circuit gone mid-render; ignore.
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
                _paintedZoom = _zoom;
                try
                {
                    await _js.BitPdfViewerPaintCanvasPages(_containerRef, payload, _zoom);
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
            && Math.Abs(_zoom - _paintedZoom) > 0.001 && _pages.Count > 0)
        {
            _paintedZoom = _zoom;
            try
            {
                await _js.BitPdfViewerRezoomCanvases(_containerRef, _zoom);
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

    private async Task LoadAsync()
    {
        int version = ++_loadVersion; // supersedes any load still in flight

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
            _renderQueue.Clear(); // pending lazy renders belong to the old document
            _renderQueued.Clear();
            _thumbQueue.Clear();
            _thumbQueued.Clear();
            _pageWidths.Clear();
            _pageHeights.Clear();
            _document = null;
            _fontStore = null; // fresh embedded-font store per document
            _fontFaceStyle = string.Empty; // its @font-face snapshot belongs to the old document
            _pageText = null;  // invalidate the search text index
            _searchTotal = 0;
            _searchIndex = -1;
            _outline = [];
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
            _status = "No document loaded.";
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
                _status = "URL sources require a registered HttpClient.";
                _loading = false;
                await OnError.InvokeAsync(_status);
                return;
            }
            try
            {
                bytes = await http.GetByteArrayAsync(_source.Url);
            }
            catch (Exception ex)
            {
                // A newer Source superseded this load while the fetch was in flight;
                // its failure is not the current document's, so don't publish a stale
                // error or hide the newer load's progress bar (mirrors the parse catch).
                if (version != _loadVersion) return;
                _status = $"Failed to fetch document: {ex.Message}";
                _loading = false;
                await OnError.InvokeAsync(_status);
                return;
            }
            if (version != _loadVersion) return;
        }
        if (bytes is null)
        {
            _status = "No document loaded.";
            _loading = false;
            return;
        }

        try
        {
            BitPdfDocument document;
            try
            {
                document = await ParseAsync(bytes, _source.Password);
            }
            catch (BitPdfPasswordException) when (OnPasswordRequested is not null)
            {
                // The parse awaited (Task.Run / gate) long enough for a newer Source
                // or a disposal; don't prompt the host for a password on a load that
                // is already superseded — that user code would run for nothing.
                if (IsDisposed || version != _loadVersion) return;
                // Ask the host for a password and retry once. The callback returns
                // null to cancel.
                string? entered = await OnPasswordRequested();
                if (string.IsNullOrEmpty(entered))
                {
                    throw;
                }
                // The prompt is user code that may have awaited long enough for a
                // newer Source or a disposal; don't parse (or later publish) a
                // superseded document — the retry parse is the expensive part.
                if (IsDisposed || version != _loadVersion) return;
                document = await ParseAsync(bytes, entered);
            }
            // A password prompt (or the parse itself) may have awaited long enough
            // for a newer Source; don't clobber the newer load's document.
            if (version != _loadVersion) return;
            _document = document;

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
            _status = $"{_document.PageCount} page(s).";
            _spyPending = true;
            if (_document.Warnings.Count > 0 && OnWarnings.HasDelegate)
            {
                await OnWarnings.InvokeAsync(_document.Warnings);
                // The warnings callback is user code: it may have awaited long
                // enough for a newer Source (or even set one) or a disposal —
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
            _status = $"Error: {ex.Message}";
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
        if (_document is null) return;

        bool swap = _rotation % 180 == 90;
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
            // return an empty, discardable result — the caller's guard drops it.
            return new BitPdfPageBuild(string.Empty, null);
        }
        var store = _fontStore ??= new BitPdfFontStore();
        var page = doc.Pages[index];
        var renderer = new BitPdfHtmlRenderer(page, doc.XRef, store, rotation)
        {
            DestinationResolver = dest => doc.ResolveDestinationPage(dest),
            TextCoalescing = textCoalescing,
            EmitCanvasOps = renderMode == BitPdfRenderMode.Canvas,
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
    /// through <see cref="_renderGate"/> and — when <see cref="BackgroundRendering"/>
    /// is set and the runtime has a spare thread — with the heavy build hopped off
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
        int version = 0, epoch = 0;
        try
        {
            if (IsDisposed || index >= _pages.Count || _pages[index] is not null) return false; // filled/disposed while waiting
            version = _loadVersion; epoch = _renderEpoch;

            // Snapshot the render settings on the UI thread so a background build reads
            // a consistent set even if _rotation/TextCoalescing/RenderMode change while
            // it runs; a change also bumps _renderEpoch, so the result is discarded below.
            int rotation = _rotation;
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
                // Skip it, leaving its placeholder, and surface the failure via
                // OnError once the gate is released (below).
                buildError = ex.Message;
                return false;
            }

            // A reload, rotation, mode change or disposal while the build was in
            // flight invalidated these slots: drop the now-stale fragment.
            if (IsDisposed || version != _loadVersion || epoch != _renderEpoch || index >= _pages.Count || _pages[index] is not null)
            {
                return false;
            }
            _pages[index] = CommitPage(index, build);
            return true;
        }
        finally
        {
            _renderGate.Release();
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
            // MAIN surface only — a reused fragment would show a blank thumbnail.
            if (RenderMode != BitPdfRenderMode.Canvas && index < _pages.Count && _pages[index] is { } cached)
            {
                _thumbs[index] = cached;
                return true;
            }

            version = _loadVersion; epoch = _renderEpoch;
            int rotation = _rotation;
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
        int center = Math.Clamp(_currentPage - 1, 0, _pages.Count - 1);
        return RenderPageAsync(center);
    }

    // Cap how many pages stay materialized so a large document does not grow the
    // DOM (and Blazor Server circuit memory) unbounded. Evicted pages revert to
    // placeholders and are re-rendered lazily when scrolled back into view.
    private const int MaxRenderedPages = 24;

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
        int keepLo = Math.Max(0, _currentPage - 1 - half);
        int keepHi = Math.Min(_pages.Count - 1, _currentPage - 1 + half);
        for (int i = 0; i < _pages.Count; i++)
        {
            if ((i < keepLo || i > keepHi) && _pages[i] is not null)
            {
                _pages[i] = null;
                // The display list (with its base64 images) is regenerated when
                // the page re-renders; don't hold it for evicted pages.
                _canvasOps.Remove(i);
                _canvasDirty.Remove(i);
            }
        }
    }

    /// <summary>
    /// The heavy, offloadable half of rendering a thumbnail — <see cref="BuildPage"/>'s
    /// sidebar counterpart, with the same reload-safety contract (a null-document
    /// build returns an empty, discardable result the caller's guard drops).
    /// Canvas mode renders self-contained HTML instead of the page's canvas
    /// placeholder — the JS paint targets the MAIN surface only, so a placeholder
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
        };
        return new BitPdfPageBuild(renderer.Render(), null);
    }

    // Bound how many thumbnails stay materialized. A thumbnail fragment is as
    // heavy as a full page, so a large document scrolled end-to-end in the
    // sidebar would otherwise pin every page's markup in memory.
    private const int MaxRenderedThumbs = 40;

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
                int i = _currentPage - 1;
                if (i < 0 || i >= labels.Count) return null;

                string label = labels[i];
                return label == _currentPage.ToString(System.Globalization.CultureInfo.InvariantCulture) ? null : label;
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
            await _js.BitPdfViewerScrollThumbIntoView(_thumbsRef, _currentPage);
        }
        catch (JSDisconnectedException) { }
    }

    private async Task OnPageInput(ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out int n))
        {
            await GoToPage(n);
        }
    }

    private Task SetCustomZoom(double zoom)
    {
        _zoomMode = BitPdfZoomMode.Custom;
        _zoom = Math.Clamp(zoom, 0.1, 8.0);
        return Task.CompletedTask;
    }

    private async Task ApplyFitAsync()
    {
        if (_pages.Count == 0 || _zoomMode is BitPdfZoomMode.Custom or BitPdfZoomMode.ActualSize) return;

        var vp = await _js.BitPdfViewerGetViewport(_containerRef);
        if (vp.Width <= 0) return;

        double maxW = _pageWidths.Count > 0 ? _pageWidths.Max() : 612;
        double maxH = _pageHeights.Count > 0 ? _pageHeights.Max() : 792;
        const double padding = 32; // surface padding + page margin

        double fitWidth = (vp.Width - padding) / maxW;
        _zoom = _zoomMode == BitPdfZoomMode.FitPage
            ? Math.Min(fitWidth, (vp.Height - padding) / maxH)
            : fitWidth;
        _zoom = Math.Clamp(_zoom, 0.1, 8.0);
    }

    private async Task ToggleThumbnails()
    {
        _showThumbnails = !_showThumbnails;
        if (_showThumbnails)
        {
            _showOutline = false;
            // Attach the sidebar spy after its element renders; it fills the
            // visible thumbnails on its own.
            _thumbSpyPending = true;
        }
        else
        {
            // The sidebar element is leaving the DOM; drop its scroll listener.
            try
            {
                await _js.BitPdfViewerDisposeThumbSpy(_thumbsRef);
            }
            catch (JSDisconnectedException) { }
        }
    }

    private void ToggleOutline()
    {
        _showOutline = !_showOutline;
        if (_showOutline)
        {
            _showThumbnails = false;
        }
    }

    private async Task OnOutlineClick(BitPdfOutlineItem item)
    {
        if (item.PageNumber is int pageNo)
        {
            await GoToPage(pageNo);
        }
    }

    // ----- Search -----

    private string SearchLabel => _searchTotal switch
    {
        < 0 => "n/a",
        0 => string.IsNullOrEmpty(_searchQuery) ? "" : "0/0",
        _ => $"{_searchIndex + 1}/{_searchTotal}",
    };

    private async Task ToggleSearch()
    {
        _showSearch = !_showSearch;
        if (_showSearch is false)
        {
            _searchQuery = "";
            await ClearSearchAsync();
        }
    }

    private async Task OnSearchInput(ChangeEventArgs e)
    {
        _searchQuery = e.Value?.ToString() ?? "";
        await RunSearchAsync();
    }

    /// <summary>Activates a control on Enter or Space, so keyboard users can
    /// operate the thumbnail list and outline tree like buttons.</summary>
    private async Task OnActivateKey(KeyboardEventArgs e, Func<Task> action)
    {
        if (e.Key is "Enter" or " " or "Spacebar")
        {
            await action();
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

        // Search a per-page extracted-text index (built lazily) rather than the
        // rendered DOM, so we only render the pages that actually contain matches
        // — a 500-page document with matches on 3 pages renders 3, not 500.
        _pageText ??= new string?[_document.PageCount];
        string needle = _searchQuery;
        int pageCount = _document.PageCount; // captured so the loop condition never reads a nulled _document
        bool rendered = false;
        for (int i = 0; i < pageCount; i++)
        {
            // A reload, disposal or newer query during a yield supersedes this run;
            // stop before touching shared state (mirrors the render pumps' guard).
            if (IsDisposed || version != _loadVersion || generation != _searchGeneration) return;

            _pageText[i] ??= _document.Pages[i].ExtractText();
            if (_pageText[i]!.Contains(needle, StringComparison.OrdinalIgnoreCase)
                && i < _pages.Count && _pages[i] is null)
            {
                await RenderPageAsync(i);
                rendered = true;
                // Yield between match renders so a large result set doesn't
                // monopolize the WASM UI thread (mirrors the lazy-render pumps).
                await Task.Delay(1);
            }
            else if ((i & 31) == 31)
            {
                // No render happened this page, but the first search over a large
                // document extracts text for every page synchronously; yield every
                // 32 pages so that extraction sweep doesn't freeze the UI thread.
                await Task.Delay(1);
            }
        }

        // A reload, disposal or newer query during the final yield supersedes this
        // search; don't touch shared state or JS on a torn-down/stale component.
        if (IsDisposed || version != _loadVersion || generation != _searchGeneration) return;

        _loading = false;
        if (rendered)
        {
            StateHasChanged();
            await Task.Delay(1); // let the freshly rendered pages paint before highlighting
            // A reload, disposal or newer query during the paint delay supersedes
            // this search; don't highlight the newer state for the old query.
            if (IsDisposed || version != _loadVersion || generation != _searchGeneration) return;
        }

        // Guard the interop against a disposal racing these calls (as
        // ScrollActiveThumbIntoViewAsync does).
        try
        {
            int total = await _js.BitPdfViewerSearchAll(_containerRef, _searchQuery);
            // The interop awaited: re-validate ownership before publishing the
            // (now possibly stale) result or scrolling a torn-down viewer.
            if (IsDisposed || version != _loadVersion || generation != _searchGeneration) return;
            _searchTotal = total;
            _searchIndex = total > 0 ? 0 : -1;
            if (total > 0)
            {
                await _js.BitPdfViewerGotoMatch(_containerRef, _searchIndex);
            }
        }
        catch (JSDisconnectedException) { }
    }

    private Task SearchNext() => GotoMatch(_searchIndex + 1);

    private Task SearchPrev() => GotoMatch(_searchIndex - 1);

    private async Task GotoMatch(int index)
    {
        if (_searchTotal <= 0) return;

        _searchIndex = ((index % _searchTotal) + _searchTotal) % _searchTotal;
        await _js.BitPdfViewerGotoMatch(_containerRef, _searchIndex);
    }

    private async Task ClearSearchAsync()
    {
        // Shared invalidation for every "clear" path (empty query, closing the box):
        // bump the generation so any search still in flight abandons at its next
        // checkpoint, and clear its progress bar here — the abandoning run returns
        // early via the generation guard and so never runs its own `_loading = false`.
        _searchGeneration++;
        _loading = false;
        _searchTotal = 0;
        _searchIndex = -1;
        await _js.BitPdfViewerClearSearch(_containerRef);
    }

    private string PageStyle(int index)
    {
        double pw = index < _pageWidths.Count ? _pageWidths[index] : 612;
        double ph = index < _pageHeights.Count ? _pageHeights[index] : 792;
        return string.Create(System.Globalization.CultureInfo.InvariantCulture,
            $"width:{pw * _zoom:0.#}px;height:{ph * _zoom:0.#}px;--bit-pdv-scale:{_zoom:0.####}");
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
        // of this method — set it up front so those guards take effect during disposal.
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
        }
        catch (JSDisconnectedException) { } // Circuit already gone; nothing to clean up.
        catch (TaskCanceledException) { } // Disposal raced an in-flight interop call; safe to ignore.

        _dotnetObj?.Dispose();
        _renderGate.Dispose();

        await base.DisposeAsync(disposing);
    }
}
