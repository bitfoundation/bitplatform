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
    private string _status = "Idle.";
    private bool _loading;

    // One slot per page. A null slot is a not-yet-rendered page shown as a
    // light placeholder; it is rendered on demand when it nears the viewport.
    private readonly List<MarkupString?> _pages = [];
    private readonly List<double> _pageWidths = [];  // points, display orientation
    private readonly List<double> _pageHeights = [];

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
    private bool _showThumbnails;
    private bool _showOutline;
    private IReadOnlyList<BitPdfOutlineItem> _outline = [];

    private bool _showSearch;
    private string _searchQuery = "";
    private int _searchTotal;
    private int _searchIndex = -1;

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
    /// The CSS height of the viewer container.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public string? Height { get; set; } = "780px";

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

        int target = Math.Clamp(pageNumber, 1, _pages.Count);
        if (target != _currentPage)
        {
            _currentPage = target;
            await OnPageChanged.InvokeAsync(_currentPage);
        }

        await _js.BitPdfViewerScrollToPage(_containerRef, _currentPage);
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
    public Task RotateClockwise()
    {
        _rotation = (_rotation + 90) % 360;
        PreparePages();
        _spyPending = true;
        return Task.CompletedTask;
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

        // Render every page before printing so the output includes all pages, not
        // just the ones scrolled into view. Show progress while catching up.
        bool rendered = false;
        for (int i = 0; i < _pages.Count; i++)
        {
            if (_pages[i] is null)
            {
                if (!rendered)
                {
                    _loading = true;
                    _status = "Preparing all pages for printing…";
                    StateHasChanged();
                    await Task.Yield();
                    rendered = true;
                }
                _pages[i] = RenderPageContent(i);
            }
        }
        if (rendered)
        {
            _loading = false;
            StateHasChanged();
            await Task.Yield(); // let the DOM paint the freshly rendered pages
        }

        await _js.BitPdfViewerPrint(_containerRef);
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
    /// the requested pages that have not been rendered yet.
    /// </summary>
    [JSInvokable]
    public void EnsurePagesRendered(int[] pageNumbers)
    {
        if (_document is null || pageNumbers is null) return;

        bool changed = false;
        foreach (int n in pageNumbers)
        {
            int idx = n - 1;
            if (idx >= 0 && idx < _pages.Count && _pages[idx] is null)
            {
                _pages[idx] = RenderPageContent(idx);
                changed = true;
            }
        }

        if (changed)
        {
            EvictDistantPages();
            StateHasChanged();
        }
    }

    /// <summary>
    /// Invoked from JavaScript as thumbnails approach the sidebar viewport.
    /// Renders any requested thumbnails that are still placeholders. This is the
    /// sidebar's counterpart to <see cref="EnsurePagesRendered"/> and runs on the
    /// sidebar's own scroll, so opening the panel on a 500-page document renders
    /// only the handful of thumbnails on screen.
    /// </summary>
    [JSInvokable]
    public void EnsureThumbsRendered(int[] pageNumbers)
    {
        if (_document is null || pageNumbers is null) return;

        bool changed = false;
        int lo = int.MaxValue, hi = int.MinValue;
        foreach (int n in pageNumbers)
        {
            int idx = n - 1;
            if (idx >= 0 && idx < _thumbs.Count)
            {
                lo = Math.Min(lo, idx);
                hi = Math.Max(hi, idx);
                if (_thumbs[idx] is null)
                {
                    _thumbs[idx] = RenderThumbContent(idx);
                    changed = true;
                }
            }
        }

        if (changed)
        {
            // Evict around the range just requested (what is visible in the
            // sidebar), not the current page — scrolling the sidebar leaves the
            // current page put, so centering on it would blank the very
            // thumbnails the user just scrolled to.
            EvictDistantThumbs(lo, hi);
            StateHasChanged();
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
    }



    // Parse off the UI thread on Blazor Server so a large document doesn't freeze
    // the circuit; on single-threaded WASM this runs inline (Task.Run offers no
    // parallelism there, and the surrounding Task.Yield already lets the bar paint).
    private static Task<BitPdfDocument> ParseAsync(byte[] bytes, string? password)
        => OperatingSystem.IsBrowser()
            ? Task.FromResult(BitPdfDocument.Load(bytes, password))
            : Task.Run(() => BitPdfDocument.Load(bytes, password));

    private async Task LoadAsync()
    {
        int version = ++_loadVersion; // supersedes any load still in flight
        _pages.Clear();
        _pageWidths.Clear();
        _pageHeights.Clear();
        _document = null;
        _fontStore = null; // fresh embedded-font store per document
        _pageText = null;  // invalidate the search text index
        _searchTotal = 0;
        _searchIndex = -1;
        _outline = [];

        if (_source is null)
        {
            _status = "No document loaded.";
            return;
        }

        // Show the progress bar and let it paint before the synchronous parse
        // work begins. The bar animates on the compositor so it keeps moving
        // even while the WASM thread is busy parsing.
        _loading = true;
        StateHasChanged();
        await Task.Yield();

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
            try
            {
                _document = await ParseAsync(bytes, _source.Password);
            }
            catch (BitPdfPasswordException) when (OnPasswordRequested is not null)
            {
                // Ask the host for a password and retry once. The callback returns
                // null to cancel.
                string? entered = await OnPasswordRequested();
                if (string.IsNullOrEmpty(entered))
                {
                    throw;
                }
                _document = await ParseAsync(bytes, entered);
            }
            // A password prompt may have awaited long enough for a newer Source.
            if (version != _loadVersion) return;

            PreparePages();
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
            }
            await OnDocumentLoaded.InvokeAsync();
        }
        catch (Exception ex)
        {
            _status = $"Error: {ex.Message}";
            await OnError.InvokeAsync(ex.Message);
        }
        finally
        {
            _loading = false;
        }
    }

    /// <summary>
    /// Measures every page (cheap) and creates an empty render slot for each so
    /// the document surface, scrollbar and page count are correct immediately.
    /// Only a small window around the current page is rendered up front; the
    /// rest are rendered on demand as they approach the viewport.
    /// </summary>
    private void PreparePages()
    {
        _pages.Clear();
        _thumbs.Clear();
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

        // Eagerly render a small window around the current page so something is
        // visible instantly (page 1 on load, or the viewed page after rotation).
        int center = Math.Clamp(_currentPage - 1, 0, _pages.Count - 1);
        for (int i = Math.Max(0, center - 1); i <= Math.Min(_pages.Count - 1, center + 1); i++)
        {
            _pages[i] = RenderPageContent(i);
        }

        // If the sidebar is open, its slots were just reset; let its spy re-fill
        // the visible thumbnails on the next render.
        if (_showThumbnails)
        {
            _thumbSpyPending = true;
        }
    }

    /// <summary>The document-wide embedded-font <c>@font-face</c> stylesheet,
    /// rendered in a persistent element so it survives page eviction.</summary>
    private MarkupString FontFaceStyleMarkup => new(_fontStore?.FontFaceStyle ?? string.Empty);

    /// <summary>Renders a single page to its HTML fragment.</summary>
    private MarkupString RenderPageContent(int index)
    {
        var page = _document!.Pages[index];
        _fontStore ??= new BitPdfFontStore();
        _correctWidthsPending = true; // measure/scale text runs after this render
        var renderer = new BitPdfHtmlRenderer(page, _document.XRef, _fontStore, _rotation)
        {
            DestinationResolver = dest => _document.ResolveDestinationPage(dest),
            TextCoalescing = TextCoalescing,
            EmitCanvasOps = RenderMode == BitPdfRenderMode.Canvas,
        };
        string html = renderer.Render();
        // Canvas mode: hold the display list until the fragment's <canvas> exists
        // in the DOM, then OnAfterRenderAsync replays it via JS.
        if (renderer.CanvasOpsJson is { } ops)
        {
            _canvasOps[index] = ops;
            if (_canvasDirty.Contains(index) is false)
            {
                _canvasDirty.Add(index);
            }
        }
        return new MarkupString(html);
    }

    // Cap how many pages stay materialized so a large document does not grow the
    // DOM (and Blazor Server circuit memory) unbounded. Evicted pages revert to
    // placeholders and are re-rendered lazily when scrolled back into view.
    private const int MaxRenderedPages = 24;

    private void EvictDistantPages()
    {
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
    /// Renders the fragment shown in a thumbnail. The markup is identical to the
    /// full page (only the enclosing <c>--bit-pdv-scale</c> differs), so when the main
    /// surface has already rendered this page we reuse its immutable fragment;
    /// otherwise we render one just for the sidebar. Either way the thumbnail is
    /// cached in its own slot and survives the main page's eviction.
    /// </summary>
    private MarkupString RenderThumbContent(int index)
    {
        // Canvas mode: page fragments are canvas placeholders whose pixels are
        // painted by JS into the MAIN surface only — a reused fragment would show
        // a blank thumbnail. Render sidebar thumbnails as self-contained HTML
        // (Compact text keeps the tiny fragments light).
        if (RenderMode == BitPdfRenderMode.Canvas)
        {
            _fontStore ??= new BitPdfFontStore();
            var renderer = new BitPdfHtmlRenderer(
                _document!.Pages[index], _document.XRef, _fontStore, _rotation)
            {
                TextCoalescing = BitPdfTextCoalescing.Compact,
            };
            return new MarkupString(renderer.Render());
        }
        return _pages[index] ?? RenderPageContent(index);
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

        if (string.IsNullOrEmpty(_searchQuery))
        {
            await ClearSearchAsync();
            return;
        }

        _loading = true;
        StateHasChanged();
        await Task.Yield();

        // Search a per-page extracted-text index (built lazily) rather than the
        // rendered DOM, so we only render the pages that actually contain matches
        // — a 500-page document with matches on 3 pages renders 3, not 500.
        _pageText ??= new string?[_document.PageCount];
        string needle = _searchQuery;
        bool rendered = false;
        for (int i = 0; i < _document.PageCount; i++)
        {
            _pageText[i] ??= _document.Pages[i].ExtractText();
            if (_pageText[i]!.Contains(needle, StringComparison.OrdinalIgnoreCase)
                && i < _pages.Count && _pages[i] is null)
            {
                _pages[i] = RenderPageContent(i);
                rendered = true;
            }
        }

        _loading = false;
        if (rendered)
        {
            StateHasChanged();
            await Task.Yield(); // let the freshly rendered pages paint before highlighting
        }

        _searchTotal = await _js.BitPdfViewerSearchAll(_containerRef, _searchQuery);
        _searchIndex = _searchTotal > 0 ? 0 : -1;
        if (_searchTotal > 0)
        {
            await _js.BitPdfViewerGotoMatch(_containerRef, _searchIndex);
        }
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

        try
        {
            await _js.BitPdfViewerDisposeScrollSpy(_containerRef);
            await _js.BitPdfViewerDisposeThumbSpy(_thumbsRef);
        }
        catch (JSDisconnectedException) { } // Circuit already gone; nothing to clean up.
        catch (TaskCanceledException) { } // Disposal raced an in-flight interop call; safe to ignore.

        _dotnetObj?.Dispose();

        await base.DisposeAsync(disposing);
    }
}
