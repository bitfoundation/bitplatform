// HTML renderer. Unlike the SVG backend, this emits plain HTML DOM:
// <div> with CSS `clip-path: path()` for vector fills and clips, filled <div>
// outlines for strokes, <img> for rasters, <span> for selectable text and CSS
// gradients for shadings.

using System.Text;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Bit.BlazorUI;

/// <summary>
/// Renders a <see cref="BitPdfPage"/> to an HTML fragment. The page is a single
/// positioned <c>&lt;div&gt;</c> sized in PDF points; all content is laid out
/// in device pixels and scaled to fit via the <c>--bit-pdv-scale</c> CSS variable so
/// text stays selectable and graphics stay resolution independent.
/// </summary>
public sealed class BitPdfHtmlRenderer
{
    private const int MaxFormDepth = 12;
    private const int CurveSegments = 12;
    private const double AscentFactor = 0.8; // approximate baseline offset

    private readonly BitPdfPage _page;
    private readonly IBitPdfXRef _xref;
    private readonly Dictionary<object, BitPdfFont> _fontCache;

    /// <summary>
    /// Optional resolver mapping a GoTo/named destination to a 1-based page
    /// number, used to render internal (intra-document) link annotations. When
    /// null, only external URI links are emitted.
    /// </summary>
    public Func<object?, int?>? DestinationResolver { get; set; }

    private BitPdfGraphicsState _state = new();
    private readonly Stack<BitPdfGraphicsState> _stack = new();
    private readonly Stack<int> _groupDepthStack = new();
    private BitPdfDict? _resources;
    private BitPdfMatrix _baseMatrix = BitPdfMatrix.Identity;
    private double _viewW;
    private double _viewH;

    // Current path under construction (device space). The SVG-syntax string is
    // reused for CSS clip-path; the flattened subpaths drive stroke outlining.
    private readonly StringBuilder _pathData = new();
    private readonly List<List<(double X, double Y)>> _subpaths = new();
    private List<(double X, double Y)>? _currentSub;
    private double _curX, _curY;
    private double _startX, _startY;
    private bool? _pendingClipEvenOdd;

    private BitPdfMatrix _textMatrix = BitPdfMatrix.Identity;
    private BitPdfMatrix _textLineMatrix = BitPdfMatrix.Identity;

    // Coalesced selection/text layer (the pdf.js model): text is accumulated here
    // separately from the painted glyph spans and emitted once, on top, at page end
    // - decoupled from paint order. Adjacent runs on a baseline merge into a single
    // transparent, selectable span per visual line (with spaces inserted for gaps),
    // so double-click words, triple-click lines, click-drag and copy behave like
    // normal text instead of fragmenting per glyph.
    private readonly StringBuilder _selLayer = new();
    private readonly StringBuilder _selText = new();
    private bool _selActive;
    private double _selLeft, _selTop, _selFontHeight, _selBaseY, _selStartX, _selEndX;

    /// <summary>
    /// How painted text runs are emitted. <see cref="BitPdfTextCoalescing.Compact"/>
    /// merges same-line, same-style runs - including embedded-font (PUA
    /// glyph-mapped) ones - into one span per visual line; only non-upright
    /// (rotated, mirrored, or skewed) text stays per-run exact. Default is
    /// <see cref="BitPdfTextCoalescing.Exact"/>.
    /// </summary>
    public BitPdfTextCoalescing TextCoalescing { get; set; } = BitPdfTextCoalescing.Exact;

    // Pending coalesced PAINTED line (Compact mode only). Unlike the selection
    // layer (emitted at page end), painted text must keep its place in paint order,
    // so the pending line is flushed into _html whenever any non-text operator
    // executes, whenever an ineligible run arrives, and at page end.
    private readonly StringBuilder _paintText = new();
    private bool _paintActive;
    private bool _paintGlyph; // any merged run painted via PUA codepoints
    private double _paintLeft, _paintTop, _paintFontHeight, _paintBaseY, _paintStartX, _paintEndX;
    private double _paintXScale = 1;
    private string _paintTail = string.Empty;

    /// <summary>
    /// When set, painted output is emitted as a canvas display list (retrieved
    /// via <see cref="CanvasOpsJson"/> after <see cref="Render"/>) instead of
    /// painted DOM. The returned HTML then carries only the page shell: a
    /// <c>&lt;canvas&gt;</c> placeholder, the selectable text layer, and link
    /// overlays. Used through <see cref="BitPdfCanvasRenderer"/>.
    /// </summary>
    public bool EmitCanvasOps { get; set; }

    /// <summary>The display list JSON after a canvas-mode render, else null.</summary>
    public string? CanvasOpsJson { get; private set; }

    /// <summary>Page width in CSS pixels after <see cref="Render"/>.</summary>
    public double ViewWidth => _viewW;

    /// <summary>Page height in CSS pixels after <see cref="Render"/>.</summary>
    public double ViewHeight => _viewH;

    // Canvas display list under construction (canvas mode only). Each op is a
    // compact array whose first element is the op code, replayed in order by the
    // viewer's JS interpreter (paintCanvasPages). Kept in lockstep with the HTML
    // backend by emitting from the same paint funnels.
    private List<object?[]>? _ops;

    /// <summary>
    /// Appends one display-list op (canvas mode). Content inside a hidden
    /// optional-content group is dropped, mirroring the HTML backend's buffer
    /// diversion.
    /// </summary>
    private void Op(params object?[] op)
    {
        if (_ocHiddenAtDepth < 0)
        {
            _ops!.Add(op);
        }
    }

    /// <summary>Rounds a display-list coordinate to keep the ops JSON compact.</summary>
    private static double R(double v) => Math.Round(v, 2);

    /// <summary>Rounds a matrix component (scales need more precision than coords).</summary>
    private static double R4(double v) => Math.Round(v, 4);

    /// <summary>
    /// Closes open clip groups down to <paramref name="target"/>: nested
    /// <c>&lt;/div&gt;</c>s in HTML mode, <c>restore</c> ops in canvas mode.
    /// </summary>
    private void CloseGroupsTo(int target)
    {
        while (_openGroups > target)
        {
            if (_ops is not null)
            {
                Op("G");
            }
            else
            {
                _html.Append("</div>");
            }
            _openGroups--;
        }
    }

    private readonly StringBuilder _fontFaces;         // document-wide accumulator (shared)
    private readonly HashSet<string> _emittedFamilies; // dedup across pages (shared)
    private readonly StringBuilder _pageFaces = new();  // faces first seen on THIS page
    private readonly bool _ownFontFaces;                // emit faces inline (no shared store)
    private readonly Dictionary<object, string?> _patternCache = new();
    private StringBuilder _html = new();
    private int _openGroups;

    // Marked-content / optional-content state. While OC content is hidden, output
    // is diverted to a scratch buffer that is discarded at the matching EMC.
    private int _mcDepth;
    private int _ocHiddenAtDepth = -1;
    private StringBuilder? _realHtml;
    private HashSet<string>? _ocgOff;
    private int _formDepth;

    // Set by a Type3 glyph's `d1` operator: the glyph is a shape only, so its
    // colour comes from the text-showing context and colour operators inside the
    // glyph description are ignored (PDF 32000-1 §9.6.5.3). `d0` leaves it clear.
    private bool _type3ColorLocked;

    private readonly int _rotationOffset;

    public BitPdfHtmlRenderer(BitPdfPage page, IBitPdfXRef xref, int rotationOffset = 0)
        : this(page, xref, null, rotationOffset)
    {
    }

    /// <summary>
    /// Creates a renderer that shares an embedded-font store across pages, so each
    /// font's <c>@font-face</c> base64 is emitted once for the whole document
    /// (retrieve it via <see cref="BitPdfFontStore.FontFaceStyle"/>). When
    /// <paramref name="fontStore"/> is <c>null</c> the page is self-contained.
    /// </summary>
    public BitPdfHtmlRenderer(BitPdfPage page, IBitPdfXRef xref, BitPdfFontStore? fontStore, int rotationOffset = 0)
    {
        _page = page;
        _xref = xref;
        _resources = page.Resources;
        _rotationOffset = ((rotationOffset % 360) + 360) % 360;

        if (fontStore is not null)
        {
            _fontCache = fontStore.Fonts;
            _emittedFamilies = fontStore.EmittedFamilies;
            _fontFaces = fontStore.FontFaces;
            _ownFontFaces = false; // the viewer emits the shared @font-face style
        }
        else
        {
            _fontCache = new Dictionary<object, BitPdfFont>();
            _emittedFamilies = new HashSet<string>();
            _fontFaces = new StringBuilder();
            _ownFontFaces = true;
        }
    }

    /// <summary>Renders the page and returns a single positioned <c>&lt;div&gt;</c>.</summary>
    public string Render()
    {
        double[] mb = _page.MediaBox;
        double x0 = Math.Min(mb[0], mb[2]);
        double y0 = Math.Min(mb[1], mb[3]);
        double x1 = Math.Max(mb[0], mb[2]);
        double y1 = Math.Max(mb[1], mb[3]);
        double w = x1 - x0;
        double h = y1 - y0;

        (BitPdfMatrix baseMatrix, double viewW, double viewH) = BuildViewport(
            ((_page.Rotate + _rotationOffset) % 360 + 360) % 360, x0, y0, x1, y1, w, h);
        _state.Ctm = baseMatrix;
        _baseMatrix = baseMatrix;
        _viewW = viewW;
        _viewH = viewH;
        _ops = EmitCanvasOps ? new List<object?[]>() : null;

        List<BitPdfOperation> ops;
        try
        {
            ops = new BitPdfContentParser(_page.GetContentBytes()).Parse();
        }
        catch (Exception ex)
        {
            ops = new List<BitPdfOperation>();
            _html.Append($"<!-- content parse error: {Escape(ex.Message)} -->");
        }

        RunOps(ops);

        FlushPaintedLine(); // pending Compact-mode text, inside any open groups

        CloseGroupsTo(0);

        FlushSelectionLine(); // emit the final accumulated line of selectable text

        RenderAnnotations();

        var sb = new StringBuilder();
        sb.Append(string.Create(CultureInfo.InvariantCulture,
            $"<div class=\"bit-pdv-html-page\" style=\"position:absolute;left:0;top:0;width:{viewW:0.##}px;height:{viewH:0.##}px;overflow:hidden;background:#fff;color:#000;transform:scale(var(--bit-pdv-scale,1));transform-origin:top left\">"));
        // A self-contained page inlines its own @font-face rules. With a shared
        // document store the viewer emits them in a persistent <style> instead, so
        // they survive page eviction (an evicted page's inline <style> would be
        // removed while the dedup set still thinks the font was emitted → tofu).
        if (_ownFontFaces && _pageFaces.Length > 0)
        {
            sb.Append("<style>").Append(_pageFaces).Append("</style>");
        }
        // Canvas mode: painted content went to the display list; the page carries
        // a canvas placeholder (painted by the viewer's JS) below the remaining
        // HTML (link overlays) and the selection layer.
        if (_ops is not null)
        {
            sb.Append(string.Create(CultureInfo.InvariantCulture,
                $"<canvas data-bit-pdv-canvas width=\"{(int)Math.Ceiling(viewW)}\" height=\"{(int)Math.Ceiling(viewH)}\" " +
                $"style=\"position:absolute;left:0;top:0;width:{viewW:0.##}px;height:{viewH:0.##}px\"></canvas>"));
            CanvasOpsJson = System.Text.Json.JsonSerializer.Serialize(_ops);
        }
        sb.Append(_html);
        // The coalesced selection/text layer sits on top of the painted content so
        // it captures selection. The container itself is transparent to pointer
        // events (clicks pass through to link annotations); only its spans opt back
        // in, so dragging over text selects while empty areas stay clickable.
        // font-size:0 collapses the flow-level <br> separators (the spans set their
        // own size): the brs still put line breaks in copied text, but their empty
        // line boxes - which stack at the container's top-left - become zero-sized,
        // so a multi-line selection no longer paints stray highlight blocks along
        // the page's left edge.
        if (_selLayer.Length > 0)
        {
            sb.Append("<div class=\"bit-pdv-text-layer\" style=\"position:absolute;inset:0;line-height:1;font-size:0;pointer-events:none\">");
            sb.Append(_selLayer);
            sb.Append("</div>");
        }
        sb.Append("</div>");
        return sb.ToString();
    }

    private void RunOps(List<BitPdfOperation> ops)
    {
        foreach (var op in ops)
        {
            try
            {
                Execute(op);
            }
            catch
            {
                // A single malformed operator must not abort the whole page;
                // skip it and continue with the rest of the content stream.
            }
        }
    }

    private static (BitPdfMatrix, double, double) BuildViewport(
        int rotate, double x0, double y0, double x1, double y1, double w, double h)
    {
        return rotate switch
        {
            90 => (new BitPdfMatrix(0, 1, 1, 0, -y0, -x0), h, w),
            180 => (new BitPdfMatrix(-1, 0, 0, 1, x1, -y0), w, h),
            270 => (new BitPdfMatrix(0, -1, -1, 0, y1, x1), h, w),
            _ => (new BitPdfMatrix(1, 0, 0, -1, -x0, y1), w, h),
        };
    }

    private void Execute(BitPdfOperation op)
    {
        BitPdfOpCode code = op.Code;

        // Inside a Type3 `d1` glyph, colour-setting operators have no effect. The
        // colour operators are one contiguous enum block, so this is a range test.
        if (_type3ColorLocked && code is >= BitPdfOpCode.FillGray and <= BitPdfOpCode.StrokeColorN)
        {
            return;
        }
        // Anything that could paint or mutate _html ends the pending coalesced
        // painted line so it lands in correct paint order. Because the flush
        // happens before the dispatch below, it also writes to the correct buffer
        // around a BDC/EMC optional-content diversion. Text operators (a
        // contiguous enum block) are exactly the ones it may survive across: they
        // touch nothing painted, and the show-text ops manage the pending line
        // themselves inside EmitText.
        if (_paintActive && code is not (>= BitPdfOpCode.BeginText and <= BitPdfOpCode.NextLineShowTextSpacing))
        {
            FlushPaintedLine();
        }
        switch (code)
        {
            // Graphics state.
            case BitPdfOpCode.SaveState:
                _stack.Push(_state.Clone());
                _groupDepthStack.Push(_openGroups);
                break;
            case BitPdfOpCode.RestoreState:
                if (_stack.Count > 0)
                {
                    _state = _stack.Pop();
                    CloseGroupsTo(_groupDepthStack.Count > 0 ? _groupDepthStack.Pop() : 0);
                }
                break;
            case BitPdfOpCode.ConcatMatrix:
                // Require all six operands: a short/garbled cm would otherwise
                // build an all-zero singular matrix and blank everything after it.
                if (op.Operands.Count >= 6)
                {
                    _state.Ctm = BitPdfMatrix.Concat(_state.Ctm,
                        new BitPdfMatrix(op.Num(0), op.Num(1), op.Num(2), op.Num(3), op.Num(4), op.Num(5)));
                }
                break;
            case BitPdfOpCode.LineWidth: _state.LineWidth = op.Num(0); break;
            case BitPdfOpCode.Dash: SetDash(op); break;
            case BitPdfOpCode.LineCap: _state.LineCap = (int)op.Num(0); break;
            case BitPdfOpCode.LineJoin: _state.LineJoin = (int)op.Num(0); break;
            case BitPdfOpCode.MiterLimit: _state.MiterLimit = op.Num(0); break;
            case BitPdfOpCode.RenderingIntent: case BitPdfOpCode.Flatness: break; // no-op
            case BitPdfOpCode.ExtGState: ApplyExtGState(op); break;

            // Colors.
            case BitPdfOpCode.FillColorSpace: _state.FillColorSpace = ResolveColorSpace(op); SetDefaultColor(false); break;
            case BitPdfOpCode.StrokeColorSpace: _state.StrokeColorSpace = ResolveColorSpace(op); SetDefaultColor(true); break;
            case BitPdfOpCode.FillGray: _state.FillColorSpace = BitPdfColorSpace.Gray; _state.FillColor = Gray(op.Num(0)); _state.FillPattern = null; break;
            case BitPdfOpCode.StrokeGray: _state.StrokeColorSpace = BitPdfColorSpace.Gray; _state.StrokeColor = Gray(op.Num(0)); break;
            case BitPdfOpCode.FillRgb: _state.FillColorSpace = BitPdfColorSpace.Rgb; _state.FillColor = Rgb(op.Num(0), op.Num(1), op.Num(2)); _state.FillPattern = null; break;
            case BitPdfOpCode.StrokeRgb: _state.StrokeColorSpace = BitPdfColorSpace.Rgb; _state.StrokeColor = Rgb(op.Num(0), op.Num(1), op.Num(2)); break;
            case BitPdfOpCode.FillCmyk: _state.FillColorSpace = BitPdfColorSpace.Cmyk; _state.FillColor = Cmyk(op.Num(0), op.Num(1), op.Num(2), op.Num(3)); _state.FillPattern = null; break;
            case BitPdfOpCode.StrokeCmyk: _state.StrokeColorSpace = BitPdfColorSpace.Cmyk; _state.StrokeColor = Cmyk(op.Num(0), op.Num(1), op.Num(2), op.Num(3)); break;
            case BitPdfOpCode.FillColorN: SetFillColorN(op); break;
            case BitPdfOpCode.StrokeColorN: SetStrokeColorN(op); break;

            // Path construction.
            case BitPdfOpCode.MoveTo: MoveTo(op.Num(0), op.Num(1)); break;
            case BitPdfOpCode.LineTo: LineTo(op.Num(0), op.Num(1)); break;
            case BitPdfOpCode.CurveTo: CurveTo(op.Num(0), op.Num(1), op.Num(2), op.Num(3), op.Num(4), op.Num(5)); break;
            case BitPdfOpCode.CurveToV: CurveTo(_curX, _curY, op.Num(0), op.Num(1), op.Num(2), op.Num(3)); break;
            case BitPdfOpCode.CurveToY: CurveTo(op.Num(0), op.Num(1), op.Num(2), op.Num(3), op.Num(2), op.Num(3)); break;
            case BitPdfOpCode.Rectangle: Rectangle(op.Num(0), op.Num(1), op.Num(2), op.Num(3)); break;
            case BitPdfOpCode.ClosePath: ClosePath(); break;

            // Path painting.
            case BitPdfOpCode.Stroke: PaintPath(true, false, false); break;
            case BitPdfOpCode.CloseStroke: ClosePath(); PaintPath(true, false, false); break;
            case BitPdfOpCode.Fill: PaintPath(false, true, false); break;
            case BitPdfOpCode.FillEvenOdd: PaintPath(false, true, true); break;
            case BitPdfOpCode.FillStroke: PaintPath(true, true, false); break;
            case BitPdfOpCode.FillStrokeEvenOdd: PaintPath(true, true, true); break;
            case BitPdfOpCode.CloseFillStroke: ClosePath(); PaintPath(true, true, false); break;
            case BitPdfOpCode.CloseFillStrokeEvenOdd: ClosePath(); PaintPath(true, true, true); break;
            case BitPdfOpCode.EndPath: EndPathNoPaint(); break;
            case BitPdfOpCode.Clip: _pendingClipEvenOdd = false; break;
            case BitPdfOpCode.ClipEvenOdd: _pendingClipEvenOdd = true; break;

            // Shadings and XObjects.
            case BitPdfOpCode.Shading: PaintShading(op); break;
            case BitPdfOpCode.XObject: DoXObject(op); break;
            case BitPdfOpCode.InlineImage: DrawInlineImage(op); break;

            // Type3 glyph metrics. `d0` sets the advance only; `d1` also declares a
            // colour-independent glyph, so later colour operators are suppressed.
            case BitPdfOpCode.Type3Width: break;
            case BitPdfOpCode.Type3WidthBox: _type3ColorLocked = true; break;

            // Marked content / optional content groups.
            case BitPdfOpCode.BeginMarkedContentDict: BeginMarkedContent(op); break;
            case BitPdfOpCode.BeginMarkedContent: _mcDepth++; break;
            case BitPdfOpCode.EndMarkedContent: EndMarkedContent(); break;

            // Text objects.
            case BitPdfOpCode.BeginText:
                _textMatrix = BitPdfMatrix.Identity;
                _textLineMatrix = BitPdfMatrix.Identity;
                break;
            case BitPdfOpCode.EndText: break;
            case BitPdfOpCode.CharSpacing: _state.CharSpacing = op.Num(0); break;
            case BitPdfOpCode.WordSpacing: _state.WordSpacing = op.Num(0); break;
            case BitPdfOpCode.HorizScale: _state.HorizScale = op.Num(0) / 100.0; break;
            case BitPdfOpCode.Leading: _state.Leading = op.Num(0); break;
            case BitPdfOpCode.TextRise: _state.TextRise = op.Num(0); break;
            case BitPdfOpCode.RenderMode: _state.RenderMode = (int)op.Num(0); break;
            case BitPdfOpCode.SetFont: SetFont(op); break;
            case BitPdfOpCode.TextMove: TextMove(op.Num(0), op.Num(1)); break;
            case BitPdfOpCode.TextMoveSetLeading: _state.Leading = -op.Num(1); TextMove(op.Num(0), op.Num(1)); break;
            case BitPdfOpCode.TextMatrix:
                _textLineMatrix = new BitPdfMatrix(op.Num(0), op.Num(1), op.Num(2), op.Num(3), op.Num(4), op.Num(5));
                _textMatrix = _textLineMatrix;
                break;
            case BitPdfOpCode.TextNextLine: TextMove(0, -_state.Leading); break;
            case BitPdfOpCode.ShowText: ShowText(op.Operands.Count > 0 ? op.Operands[0] : null); break;
            case BitPdfOpCode.ShowTextArray: ShowTextArray(op.Operands.Count > 0 ? op.Operands[0] as List<object?> : null); break;
            case BitPdfOpCode.NextLineShowText:
                TextMove(0, -_state.Leading);
                ShowText(op.Operands.Count > 0 ? op.Operands[0] : null);
                break;
            case BitPdfOpCode.NextLineShowTextSpacing:
                _state.WordSpacing = op.Num(0);
                _state.CharSpacing = op.Num(1);
                TextMove(0, -_state.Leading);
                ShowText(op.Operands.Count > 2 ? op.Operands[2] : null);
                break;
        }
    }

    // ----- Path building (coordinates transformed to device space) -----

    private void StartSub(double dx, double dy)
    {
        _currentSub = new List<(double X, double Y)> { (dx, dy) };
        _subpaths.Add(_currentSub);
    }

    private void MoveTo(double x, double y)
    {
        _curX = _startX = x;
        _curY = _startY = y;
        var (dx, dy) = _state.Ctm.Apply(x, y);
        StartSub(dx, dy);
        _pathData.Append(string.Create(CultureInfo.InvariantCulture, $"M{dx:0.##} {dy:0.##} "));
    }

    private void LineTo(double x, double y)
    {
        _curX = x;
        _curY = y;
        var (dx, dy) = _state.Ctm.Apply(x, y);
        if (_currentSub is null)
        {
            StartSub(dx, dy);
        }
        else
        {
            _currentSub.Add((dx, dy));
        }
        _pathData.Append(string.Create(CultureInfo.InvariantCulture, $"L{dx:0.##} {dy:0.##} "));
    }

    private void CurveTo(double x1, double y1, double x2, double y2, double x3, double y3)
    {
        var (a, b) = _state.Ctm.Apply(x1, y1);
        var (c, d) = _state.Ctm.Apply(x2, y2);
        var (e, f) = _state.Ctm.Apply(x3, y3);

        (double X, double Y) p0 = _currentSub is { Count: > 0 }
            ? _currentSub[^1]
            : _state.Ctm.Apply(_curX, _curY);
        if (_currentSub is null)
        {
            StartSub(p0.X, p0.Y);
        }

        // Flatten the cubic Bezier so strokes can be outlined.
        for (int i = 1; i <= CurveSegments; i++)
        {
            double t = (double)i / CurveSegments;
            double mt = 1 - t;
            double bx = mt * mt * mt * p0.X + 3 * mt * mt * t * a + 3 * mt * t * t * c + t * t * t * e;
            double by = mt * mt * mt * p0.Y + 3 * mt * mt * t * b + 3 * mt * t * t * d + t * t * t * f;
            _currentSub!.Add((bx, by));
        }

        _curX = x3;
        _curY = y3;
        _pathData.Append(string.Create(CultureInfo.InvariantCulture,
            $"C{a:0.##} {b:0.##} {c:0.##} {d:0.##} {e:0.##} {f:0.##} "));
    }

    private void Rectangle(double x, double y, double w, double h)
    {
        MoveTo(x, y);
        LineTo(x + w, y);
        LineTo(x + w, y + h);
        LineTo(x, y + h);
        ClosePath();
    }

    private void ClosePath()
    {
        _curX = _startX;
        _curY = _startY;
        if (_currentSub is { Count: > 0 })
        {
            _currentSub.Add(_currentSub[0]); // close the polyline for stroking
        }
        _pathData.Append("Z ");
    }

    private void ResetPath()
    {
        _pathData.Clear();
        _subpaths.Clear();
        _currentSub = null;
    }

    private void PaintPath(bool stroke, bool fill, bool evenOdd)
    {
        if (_pathData.Length == 0)
        {
            ApplyPendingClip();
            return;
        }

        string data = _pathData.ToString().Trim();

        if (fill)
        {
            // Canvas mode approximates pattern fills with the current fill colour
            // (tiling replay and CSS-gradient paints are HTML-backend constructs).
            if (_ops is null && _state.FillPattern is not null && TryRenderTilingFill(data, evenOdd))
            {
                // Tiling pattern painted its cells into a clipped group.
            }
            else
            {
                string fillPaint = _ops is null && _state.FillPattern is not null
                    ? ResolveFillPaint(_state.FillPattern) ?? _state.FillColor
                    : _state.FillColor;
                EmitFill(data, evenOdd, fillPaint, _state.FillAlpha);
            }
        }

        if (stroke)
        {
            double deviceWidth = Math.Max(0.75, _state.LineWidth * _state.Ctm.ScaleFactor);
            EmitStroke(data, deviceWidth);
        }

        ApplyPendingClip(data);
        ResetPath();
    }

    private void EmitFill(string pathData, bool evenOdd, string paint, double alpha)
    {
        if (_ops is not null)
        {
            Op("f", pathData, evenOdd, paint, R(alpha), _state.BlendMode);
            return;
        }
        _html.Append("<div style=\"position:absolute;inset:0;background:");
        _html.Append(paint);
        _html.Append(";clip-path:path(");
        if (evenOdd)
        {
            _html.Append("evenodd,");
        }
        _html.Append('\'').Append(pathData).Append("')");
        if (alpha < 1)
        {
            _html.Append(string.Create(CultureInfo.InvariantCulture, $";opacity:{alpha:0.###}"));
        }
        if (_state.BlendMode.Length > 0)
        {
            _html.Append(";mix-blend-mode:").Append(_state.BlendMode);
        }
        _html.Append("\"></div>");
    }

    /// <summary>
    /// Strokes the current path by emitting an inline SVG <c>&lt;path&gt;</c>.
    /// The path data preserves the original cubic Béziers (the <c>C</c> commands
    /// built by <see cref="CurveTo"/>), so the browser draws true smooth curves
    /// with correct caps and joins instead of a flattened polygonal outline.
    /// </summary>
    private void EmitStroke(string pathData, double deviceWidth)
    {
        if (_ops is not null)
        {
            double[]? dashes = _state.DashArray is { Length: > 0 } da
                ? Array.ConvertAll(da, static v => R(Math.Max(0, v)))
                : null;
            Op("s", pathData, _state.StrokeColor, R(deviceWidth), _state.LineCap, _state.LineJoin,
                R(_state.MiterLimit), dashes, R(_state.DashPhase), R(_state.StrokeAlpha), _state.BlendMode);
            return;
        }
        _html.Append(string.Create(CultureInfo.InvariantCulture,
            $"<svg width=\"{_viewW:0.##}\" height=\"{_viewH:0.##}\" viewBox=\"0 0 {_viewW:0.##} {_viewH:0.##}\" style=\"position:absolute;left:0;top:0;overflow:visible;pointer-events:none"));
        if (_state.BlendMode.Length > 0)
        {
            _html.Append(";mix-blend-mode:").Append(_state.BlendMode);
        }
        _html.Append("\"><path d=\"").Append(pathData).Append("\" fill=\"none\" stroke=\"");
        _html.Append(_state.StrokeColor).Append('"');
        _html.Append(string.Create(CultureInfo.InvariantCulture,
            $" stroke-width=\"{deviceWidth:0.###}\""));
        _html.Append(" stroke-linecap=\"").Append(LineCapName(_state.LineCap)).Append('"');
        _html.Append(" stroke-linejoin=\"").Append(LineJoinName(_state.LineJoin)).Append('"');
        if (_state.LineJoin == 0)
        {
            // Emit the PDF miter limit explicitly; SVG's default (4) differs from
            // PDF's (10), so miter joins would otherwise bevel too eagerly.
            _html.Append(string.Create(CultureInfo.InvariantCulture,
                $" stroke-miterlimit=\"{_state.MiterLimit:0.###}\""));
        }
        if (_state.StrokeAlpha < 1)
        {
            _html.Append(string.Create(CultureInfo.InvariantCulture,
                $" stroke-opacity=\"{_state.StrokeAlpha:0.###}\""));
        }
        if (_state.DashArray is { Length: > 0 } dash)
        {
            _html.Append(" stroke-dasharray=\"");
            for (int i = 0; i < dash.Length; i++)
            {
                if (i > 0)
                {
                    _html.Append(',');
                }
                _html.Append(string.Create(CultureInfo.InvariantCulture, $"{Math.Max(0, dash[i]):0.###}"));
            }
            _html.Append('"');
            if (_state.DashPhase != 0)
            {
                _html.Append(string.Create(CultureInfo.InvariantCulture,
                    $" stroke-dashoffset=\"{_state.DashPhase:0.###}\""));
            }
        }
        _html.Append("/></svg>");
    }

    private static string LineCapName(int cap) => cap switch
    {
        1 => "round",
        2 => "square",
        _ => "butt",
    };

    private static string LineJoinName(int join) => join switch
    {
        1 => "round",
        2 => "bevel",
        _ => "miter",
    };

    private void EndPathNoPaint()
    {
        ApplyPendingClip(_pathData.Length > 0 ? _pathData.ToString().Trim() : null);
        ResetPath();
    }

    private void ApplyPendingClip(string? data = null)
    {
        if (_pendingClipEvenOdd is null)
        {
            return;
        }
        data ??= _pathData.Length > 0 ? _pathData.ToString().Trim() : null;
        bool evenOdd = _pendingClipEvenOdd.Value;
        _pendingClipEvenOdd = null;

        if (string.IsNullOrEmpty(data))
        {
            return;
        }

        if (_ops is not null)
        {
            Op("g", data, evenOdd); // save + clip, restored by the matching "G"
        }
        else
        {
            _html.Append("<div style=\"position:absolute;inset:0;clip-path:path(");
            if (evenOdd)
            {
                _html.Append("evenodd,");
            }
            _html.Append('\'').Append(data).Append("')\">");
        }
        _openGroups++;
    }

    // ----- Shadings -----

    private void PaintShading(BitPdfOperation op)
    {
        if (op.Operands.Count == 0 || op.Operands[0] is not BitPdfName name)
        {
            return;
        }
        if (_resources?.Get("Shading") is not BitPdfDict shadings)
        {
            return;
        }
        object? shadingObj = shadings.Get(name.Value);
        BitPdfDict? shading = shadingObj as BitPdfDict ?? (shadingObj as BitPdfStream)?.Dict;
        if (shading is null)
        {
            return;
        }

        if (_ops is not null)
        {
            // ["sh", kind, coords, stops, alpha, blend, bboxPath?]: kind 2/3 map to
            // native canvas linear/radial gradients filling the current clip; kind 0
            // is the sampled solid fallback for unsupported shading types.
            object?[]? grad = BitPdfCssShadingBuilder.BuildCanvasOp(shading, _xref, _resources, _state.Ctm);
            if (grad is not null)
            {
                string? bboxPath = shading.Get("BBox") is List<object?> bb && bb.Count >= 4
                    ? BBoxPathData(bb)
                    : null;
                Op("sh", grad[0], grad[1], grad[2], R(_state.FillAlpha), _state.BlendMode, bboxPath);
            }
            return;
        }

        string? background = BitPdfCssShadingBuilder.Build(shading, _xref, _resources, _state.Ctm, _viewW, _viewH);
        if (background is null)
        {
            return;
        }
        // Fill the current clip region (or the whole page when unclipped),
        // honoring the current fill alpha and blend mode.
        _html.Append("<div style=\"position:absolute;inset:0;background:");
        _html.Append(background);
        if (_state.FillAlpha < 1)
        {
            _html.Append(string.Create(CultureInfo.InvariantCulture, $";opacity:{_state.FillAlpha:0.###}"));
        }
        if (_state.BlendMode.Length > 0)
        {
            _html.Append(";mix-blend-mode:").Append(_state.BlendMode);
        }
        // A shading /BBox bounds the painted region to a rectangle in shading space
        // (PDF 32000-1 §8.7.4.3); clip the fill to it when present.
        if (shading.Get("BBox") is List<object?> bbox && bbox.Count >= 4)
        {
            _html.Append(';').Append(BBoxClipPath(bbox));
        }
        _html.Append("\"></div>");
    }

    /// <summary>
    /// A CSS <c>clip-path</c> for a shading/form <c>/BBox</c> rectangle, its four
    /// corners transformed by the current CTM into device space.
    /// </summary>
    private string BBoxClipPath(List<object?> bbox)
        => $"clip-path:path('{BBoxPathData(bbox)}')";

    /// <summary>SVG path data for a <c>/BBox</c> rectangle in device space.</summary>
    private string BBoxPathData(List<object?> bbox)
    {
        var (c0x, c0y) = _state.Ctm.Apply(Num(bbox[0]), Num(bbox[1]));
        var (c1x, c1y) = _state.Ctm.Apply(Num(bbox[2]), Num(bbox[1]));
        var (c2x, c2y) = _state.Ctm.Apply(Num(bbox[2]), Num(bbox[3]));
        var (c3x, c3y) = _state.Ctm.Apply(Num(bbox[0]), Num(bbox[3]));
        return string.Create(CultureInfo.InvariantCulture,
            $"M{c0x:0.##} {c0y:0.##} L{c1x:0.##} {c1y:0.##} L{c2x:0.##} {c2y:0.##} L{c3x:0.##} {c3y:0.##} Z");
    }

    // ----- XObjects -----

    private void DoXObject(BitPdfOperation op)
    {
        if (op.Operands.Count == 0 || op.Operands[0] is not BitPdfName name)
        {
            return;
        }
        if (_resources?.Get("XObject") is not BitPdfDict xobjects || xobjects.Get(name.Value) is not BitPdfStream stream
            || stream.Dict is null)
        {
            return;
        }

        string subtype = (stream.Dict.Get("Subtype") as BitPdfName)?.Value ?? "";
        if (subtype == "Image")
        {
            DrawImage(stream);
        }
        else if (subtype == "Form")
        {
            DrawForm(stream);
        }
    }

    private void DrawImage(BitPdfStream stream)
    {
        var fill = ParseRgb(_state.FillColor);
        string? uri = BitPdfImage.BuildDataUri(stream, _xref, _resources, fill);
        if (uri is null)
        {
            return;
        }
        EmitImage(uri, PixelSize(stream.Dict!, "Width", "W"), PixelSize(stream.Dict!, "Height", "H"),
            Interpolate(stream.Dict!));
    }

    private void DrawInlineImage(BitPdfOperation op)
    {
        if (op.Operands.Count < 2 || op.Operands[0] is not BitPdfDict dict || op.Operands[1] is not byte[] data)
        {
            return;
        }
        var stream = new BitPdfStream(data, 0, data.Length, dict);
        var fill = ParseRgb(_state.FillColor);
        string? uri = BitPdfImage.BuildDataUri(stream, _xref, _resources, fill, inline: true);
        if (uri is not null)
        {
            EmitImage(uri, PixelSize(dict, "Width", "W"), PixelSize(dict, "Height", "H"), Interpolate(dict));
        }
    }

    private static bool Interpolate(BitPdfDict dict) => dict.Get("Interpolate", "I") is bool b && b;

    private void EmitImage(string uri, int pixelW, int pixelH, bool interpolate)
    {
        if (pixelW <= 0)
        {
            pixelW = 1;
        }
        if (pixelH <= 0)
        {
            pixelH = 1;
        }

        // The image occupies the unit square; flip Y so row 0 is at the top. The
        // <img> element is sized to its native pixels so the browser samples at
        // full resolution before the matrix scales it into place.
        BitPdfMatrix unit = BitPdfMatrix.Concat(_state.Ctm, new BitPdfMatrix(1, 0, 0, -1, 0, 1));
        BitPdfMatrix m = BitPdfMatrix.Concat(unit, new BitPdfMatrix(1.0 / pixelW, 0, 0, 1.0 / pixelH, 0, 0));

        if (_ops is not null)
        {
            // The matrix maps image pixel space to device space; JS draws the
            // decoded image at natural size under this transform.
            Op("i", uri, R4(m.A), R4(m.B), R4(m.C), R4(m.D), R(m.E), R(m.F),
                R(_state.FillAlpha), _state.BlendMode, !interpolate && IsUpscaled(pixelW, pixelH));
            return;
        }

        _html.Append("<img src=\"");
        _html.Append(uri);
        _html.Append(string.Create(CultureInfo.InvariantCulture,
            $"\" style=\"position:absolute;left:0;top:0;width:{pixelW}px;height:{pixelH}px;transform:{m.ToSvg()};transform-origin:0 0"));
        if (_state.FillAlpha < 1)
        {
            _html.Append(string.Create(CultureInfo.InvariantCulture, $";opacity:{_state.FillAlpha:0.###}"));
        }
        if (_state.BlendMode.Length > 0)
        {
            _html.Append(";mix-blend-mode:").Append(_state.BlendMode);
        }
        // When the image declares no interpolation (the default) and it is being
        // scaled up, render its samples crisply instead of smoothing them (1.15).
        if (!interpolate && IsUpscaled(pixelW, pixelH))
        {
            _html.Append(";image-rendering:pixelated");
        }
        _html.Append("\"/>");
    }

    // True when the CTM stretches the unit-square image beyond its native pixels
    // on either axis (each source pixel then covers more than one device pixel).
    private bool IsUpscaled(int pixelW, int pixelH)
    {
        double displayW = Math.Sqrt(_state.Ctm.A * _state.Ctm.A + _state.Ctm.B * _state.Ctm.B);
        double displayH = Math.Sqrt(_state.Ctm.C * _state.Ctm.C + _state.Ctm.D * _state.Ctm.D);
        return displayW > pixelW || displayH > pixelH;
    }

    private static int PixelSize(BitPdfDict dict, string key1, string key2)
        => dict.Get(key1, key2) is double d ? (int)d : 0;

    // ----- Optional content (OCG) -----

    private void BeginMarkedContent(BitPdfOperation op)
    {
        _mcDepth++;
        // A "/OC <tag>" marked-content section is hidden when its optional-content
        // group is switched off in the default configuration.
        if (_ocHiddenAtDepth < 0 && op.Operands.Count >= 2
            && op.Operands[0] is BitPdfName tag && tag.Value == "OC"
            && IsOptionalContentHidden(op.Operands[1]))
        {
            _ocHiddenAtDepth = _mcDepth;
            _realHtml = _html;
            _html = new StringBuilder(); // divert & discard until the matching EMC
        }
    }

    private void EndMarkedContent()
    {
        if (_ocHiddenAtDepth == _mcDepth && _realHtml is not null)
        {
            _html = _realHtml; // drop the hidden content
            _realHtml = null;
            _ocHiddenAtDepth = -1;
        }
        if (_mcDepth > 0)
        {
            _mcDepth--;
        }
    }

    private bool IsOptionalContentHidden(object? operand)
    {
        object? raw = operand;
        if (operand is BitPdfName propName && _resources?.Get("Properties") is BitPdfDict props)
        {
            raw = props.GetRaw(propName.Value);
        }
        if (_xref.FetchIfRef(raw) is not BitPdfDict ocg)
        {
            return false;
        }

        // An OCMD references one or more OCGs; hidden only when every member is off.
        if (BitPdfPrimitives.IsName(ocg.Get("Type"), "OCMD"))
        {
            object? ocgs = ocg.GetRaw("OCGs");
            if (ocgs is List<object?> list)
            {
                if (list.Count == 0)
                {
                    return false;
                }
                foreach (var g in list)
                {
                    if (!IsOcgOff(g))
                    {
                        return false;
                    }
                }
                return true;
            }
            return IsOcgOff(ocgs);
        }
        return IsOcgOff(raw);
    }

    private bool IsOcgOff(object? raw)
    {
        if (_ocgOff is null)
        {
            _ocgOff = new HashSet<string>();
            if ((_xref as BitPdfXRef)?.Root?.Get("OCProperties") is BitPdfDict ocp && ocp.Get("D") is BitPdfDict cfg
                && cfg.Get("OFF") is List<object?> off)
            {
                foreach (var item in off)
                {
                    if (item is BitPdfRef r)
                    {
                        _ocgOff.Add(r.ToRefString());
                    }
                }
            }
        }
        return raw is BitPdfRef rf && _ocgOff.Contains(rf.ToRefString());
    }

    private void DrawForm(BitPdfStream stream)
    {
        if (_formDepth >= MaxFormDepth)
        {
            return;
        }
        _formDepth++;

        // Emulate q ... Q around the form.
        _stack.Push(_state.Clone());
        _groupDepthStack.Push(_openGroups);
        BitPdfDict? savedResources = _resources;

        if (stream.Dict!.Get("Matrix") is List<object?> mtx && mtx.Count >= 6)
        {
            _state.Ctm = BitPdfMatrix.Concat(_state.Ctm, new BitPdfMatrix(
                Num(mtx[0]), Num(mtx[1]), Num(mtx[2]), Num(mtx[3]), Num(mtx[4]), Num(mtx[5])));
        }

        if (stream.Dict.Get("Resources") is BitPdfDict formResources)
        {
            _resources = formResources;
        }

        // Transparency group: composite the group's content as a unit, then apply
        // the group-level alpha/blend once (isolation:isolate approximates a
        // non-knockout group). Reset the inner alpha so member objects don't get
        // the group's alpha applied twice.
        // Canvas mode has no group compositing surface: skip the wrap and leave
        // the group alpha/blend on the state so members apply it per object (an
        // approximation that differs only where group members overlap).
        bool groupWrap = false;
        if (_ops is null
            && stream.Dict.Get("Group") is BitPdfDict grp && BitPdfPrimitives.IsName(grp.Get("S"), "Transparency")
            && (_state.FillAlpha < 1 || _state.BlendMode.Length > 0))
        {
            _html.Append("<div style=\"position:absolute;inset:0;isolation:isolate");
            if (_state.FillAlpha < 1)
            {
                _html.Append(string.Create(CultureInfo.InvariantCulture, $";opacity:{_state.FillAlpha:0.###}"));
            }
            if (_state.BlendMode.Length > 0)
            {
                _html.Append(";mix-blend-mode:").Append(_state.BlendMode);
            }
            _html.Append("\">");
            _state.FillAlpha = 1;
            _state.StrokeAlpha = 1;
            _state.BlendMode = "";
            groupWrap = true;
        }

        // Clip the form to its /BBox transformed into device space (spec §8.10.1):
        // form content must not paint outside its bounding box.
        bool bboxClip = false;
        if (stream.Dict.Get("BBox") is List<object?> bbox && bbox.Count >= 4)
        {
            if (_ops is not null)
            {
                Op("g", BBoxPathData(bbox), false);
            }
            else
            {
                _html.Append("<div style=\"position:absolute;inset:0;")
                    .Append(BBoxClipPath(bbox))
                    .Append("\">");
            }
            bboxClip = true;
        }

        try
        {
            byte[] content = BitPdfStreamDecoder.Decode(stream);
            RunOps(new BitPdfContentParser(content).Parse());
        }
        catch
        {
            // Ignore malformed form content.
        }

        if (bboxClip)
        {
            if (_ops is not null)
            {
                Op("G");
            }
            else
            {
                _html.Append("</div>");
            }
        }
        if (groupWrap)
        {
            _html.Append("</div>");
        }

        _resources = savedResources;
        if (_stack.Count > 0)
        {
            _state = _stack.Pop();
            CloseGroupsTo(_groupDepthStack.Count > 0 ? _groupDepthStack.Pop() : 0);
        }
        _formDepth--;
    }

    // ----- Annotations -----

    private void RenderAnnotations()
    {
        if (_page.Dict.Get("Annots") is not List<object?> annots)
        {
            return;
        }

        // Clear any dangling path / clip state left by an unterminated content
        // stream so it cannot leak into annotation appearance rendering.
        _pathData.Clear();
        _subpaths.Clear();
        _currentSub = null;
        _pendingClipEvenOdd = null;

        foreach (var item in annots)
        {
            if (_xref.FetchIfRef(item) is not BitPdfDict annot)
            {
                continue;
            }

            int flags = annot.Get("F") is double f ? (int)f : 0;
            if ((flags & 0x2) != 0 || (flags & 0x20) != 0) // Hidden or NoView
            {
                continue;
            }

            DrawAnnotationAppearance(annot);
            DrawLinkOverlay(annot);
        }
    }

    private void DrawAnnotationAppearance(BitPdfDict annot)
    {
        if (annot.Get("Rect") is not List<object?> rectArr || rectArr.Count < 4)
        {
            return;
        }
        BitPdfStream? appearance = ResolveAppearance(annot);
        if (appearance?.Dict is null)
        {
            return;
        }

        double[] rect = ToRect(rectArr);
        double[] bbox = appearance.Dict.Get("BBox") is List<object?> bb && bb.Count >= 4
            ? ToRect(bb)
            : [0, 0, rect[2] - rect[0], rect[3] - rect[1]];
        BitPdfMatrix formMatrix = appearance.Dict.Get("Matrix") is List<object?> m && m.Count >= 6
            ? new BitPdfMatrix(Num(m[0]), Num(m[1]), Num(m[2]), Num(m[3]), Num(m[4]), Num(m[5]))
            : BitPdfMatrix.Identity;

        BitPdfMatrix a = ComputeAppearanceMatrix(bbox, formMatrix, rect);

        // Reset to a clean state anchored at the page base transform.
        _stack.Clear();
        _groupDepthStack.Clear();
        _state = new BitPdfGraphicsState { Ctm = BitPdfMatrix.Concat(_baseMatrix, a) };
        _resources = _page.Resources;

        DrawForm(appearance);

        CloseGroupsTo(0);
    }

    private BitPdfStream? ResolveAppearance(BitPdfDict annot)
    {
        if (annot.Get("AP") is not BitPdfDict ap)
        {
            return null;
        }
        object? normal = ap.Get("N");
        if (normal is BitPdfStream stream)
        {
            return stream;
        }
        // Appearance sub-dictionary keyed by the current appearance state (/AS).
        if (normal is BitPdfDict states)
        {
            string? state = (annot.Get("AS") as BitPdfName)?.Value;
            if (state is not null && states.Get(state) is BitPdfStream selected)
            {
                return selected;
            }
            foreach (var key in states.Keys)
            {
                if (states.Get(key) is BitPdfStream first)
                {
                    return first;
                }
            }
        }
        return null;
    }

    private void DrawLinkOverlay(BitPdfDict annot)
    {
        if (!BitPdfPrimitives.IsName(annot.Get("Subtype"), "Link"))
        {
            return;
        }
        if (annot.Get("Rect") is not List<object?> rectArr || rectArr.Count < 4)
        {
            return;
        }

        string? uri = null;
        object? action = _xref.FetchIfRef(annot.Get("A"));
        if (action is BitPdfDict actionDict && BitPdfPrimitives.IsName(actionDict.Get("S"), "URI")
            && actionDict.Get("URI") is BitPdfString u)
        {
            uri = u.AsLatin1();
        }

        // Resolve an internal GoTo/named destination to a target page number.
        int? destPage = null;
        if (uri is null && DestinationResolver is not null)
        {
            object? dest = annot.Get("Dest");
            if (dest is null && action is BitPdfDict a && BitPdfPrimitives.IsName(a.Get("S"), "GoTo"))
            {
                dest = a.Get("D");
            }
            if (dest is not null)
            {
                destPage = DestinationResolver(dest);
            }
        }

        // A link may carry /QuadPoints delimiting the individual clickable quads
        // (e.g. a link that wraps across several lines); emit one hotspot per quad,
        // falling back to the whole /Rect when they are absent (4.8).
        var regions = QuadPointRegions(annot) ?? new List<double[]> { ToRect(rectArr) };
        foreach (double[] r in regions)
        {
            EmitLinkHotspot(r, uri, destPage);
        }
    }

    private void EmitLinkHotspot(double[] r, string? uri, int? destPage)
    {
        BitPdfMatrix transform = BitPdfMatrix.Concat(_baseMatrix, new BitPdfMatrix(1, 0, 0, 1, r[0], r[1]));
        string style = string.Create(CultureInfo.InvariantCulture,
            $"position:absolute;left:0;top:0;width:{r[2] - r[0]:0.##}px;height:{r[3] - r[1]:0.##}px;transform:{transform.ToSvg()};transform-origin:0 0");

        if (uri is not null && IsAllowedUri(uri))
        {
            _html.Append($"<a href=\"{Escape(uri)}\" target=\"_blank\" rel=\"noopener noreferrer\" style=\"{style}\"></a>");
        }
        else if (destPage is int page)
        {
            // Internal link: the viewer delegates clicks on [data-bit-pdv-page] to
            // page navigation. Emitted as a div (no href) so nothing navigates away.
            _html.Append(string.Create(CultureInfo.InvariantCulture,
                $"<div data-bit-pdv-page=\"{page}\" style=\"{style};cursor:pointer\"></div>"));
        }
        // Otherwise (unknown/unsafe scheme, unresolved dest): drop the hotspot.
    }

    /// <summary>
    /// The clickable rectangles from a link's <c>/QuadPoints</c> (8 numbers per
    /// quad: four corners), or <c>null</c> when the entry is missing or malformed.
    /// </summary>
    private List<double[]>? QuadPointRegions(BitPdfDict annot)
    {
        if (_xref.FetchIfRef(annot.Get("QuadPoints")) is not List<object?> qp || qp.Count < 8)
        {
            return null;
        }

        var regions = new List<double[]>();
        int quads = qp.Count / 8;
        for (int i = 0; i < quads; i++)
        {
            int b = i * 8;
            double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
            for (int c = 0; c < 4; c++)
            {
                double x = Num(_xref.FetchIfRef(qp[b + c * 2]));
                double y = Num(_xref.FetchIfRef(qp[b + c * 2 + 1]));
                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
            }
            regions.Add([minX, minY, maxX, maxY]);
        }
        return regions.Count > 0 ? regions : null;
    }

    private static BitPdfMatrix ComputeAppearanceMatrix(double[] bbox, in BitPdfMatrix formMatrix, double[] rect)
    {
        // Transform the BBox corners by the form matrix, then map the resulting
        // bounding box onto the annotation Rect (PDF spec §12.5.5).
        Span<(double X, double Y)> corners =
        [
            formMatrix.Apply(bbox[0], bbox[1]),
            formMatrix.Apply(bbox[2], bbox[1]),
            formMatrix.Apply(bbox[2], bbox[3]),
            formMatrix.Apply(bbox[0], bbox[3]),
        ];

        double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
        foreach (var (cx, cy) in corners)
        {
            minX = Math.Min(minX, cx);
            minY = Math.Min(minY, cy);
            maxX = Math.Max(maxX, cx);
            maxY = Math.Max(maxY, cy);
        }

        double bw = maxX - minX;
        double bh = maxY - minY;
        double rw = rect[2] - rect[0];
        double rh = rect[3] - rect[1];
        double sx = bw != 0 ? rw / bw : 1;
        double sy = bh != 0 ? rh / bh : 1;

        return new BitPdfMatrix(sx, 0, 0, sy, rect[0] - minX * sx, rect[1] - minY * sy);
    }

    private static double[] ToRect(List<object?> arr)
    {
        double x0 = Num(arr[0]), y0 = Num(arr[1]), x1 = Num(arr[2]), y1 = Num(arr[3]);
        return [Math.Min(x0, x1), Math.Min(y0, y1), Math.Max(x0, x1), Math.Max(y0, y1)];
    }

    // Schemes considered safe to emit as clickable links, mirroring pdf.js
    // `createValidAbsoluteUrl`. Anything else (notably javascript: and data:)
    // is dropped so a crafted /URI cannot execute script in the host app.
    private static readonly string[] AllowedUriSchemes = { "http", "https", "mailto", "ftp", "tel" };

    private static bool IsAllowedUri(string uri)
    {
        int colon = uri.IndexOf(':');
        if (colon <= 0)
        {
            return false; // no scheme, or leading ':' - treat as unsafe
        }
        // A URI scheme is letters/digits/+/-/. and must precede any '/', '?' or '#'.
        for (int i = 0; i < colon; i++)
        {
            char ch = uri[i];
            if (!(char.IsAsciiLetterOrDigit(ch) || ch is '+' or '-' or '.'))
            {
                return false;
            }
        }
        string scheme = uri[..colon].ToLowerInvariant();
        return Array.IndexOf(AllowedUriSchemes, scheme) >= 0;
    }

    // ----- Text -----

    private void SetFont(BitPdfOperation op)
    {
        if (op.Operands.Count < 2 || op.Operands[0] is not BitPdfName fontName)
        {
            return;
        }
        _state.FontSize = op.Num(1);
        _state.FontResourceName = fontName.Value;
        _state.Font = ResolveFont(fontName.Value);
    }

    private BitPdfFont? ResolveFont(string name)
    {
        if (_resources?.Get("Font") is not BitPdfDict fonts)
        {
            return null;
        }
        // Key the cache by the font's object identity - the indirect reference if
        // present (value-equal), otherwise the dictionary instance - rather than
        // "depth:name". Different resource dictionaries can reuse a resource name
        // for different fonts, so a name-based key returned the wrong font.
        object? raw = fonts.GetRaw(name);
        object cacheKey = raw is BitPdfRef r ? r : _xref.FetchIfRef(raw) ?? name;
        if (_fontCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }
        if (_xref.FetchIfRef(raw) is BitPdfDict fontDict)
        {
            var font = BitPdfFont.Create(fontDict, _xref);
            _fontCache[cacheKey] = font;
            return font;
        }
        return null;
    }

    private void TextMove(double tx, double ty)
    {
        _textLineMatrix = BitPdfMatrix.Concat(_textLineMatrix, new BitPdfMatrix(1, 0, 0, 1, tx, ty));
        _textMatrix = _textLineMatrix;
    }

    private void ShowTextArray(List<object?>? array)
    {
        if (array is null)
        {
            return;
        }
        foreach (var item in array)
        {
            if (item is BitPdfString)
            {
                ShowText(item);
            }
            else if (item is double adjust)
            {
                double tx = -adjust / 1000.0 * _state.FontSize * _state.HorizScale;
                _textMatrix = BitPdfMatrix.Concat(_textMatrix, new BitPdfMatrix(1, 0, 0, 1, tx, 0));
            }
        }
    }

    private void ShowText(object? operand)
    {
        if (operand is not BitPdfString s || _state.Font is null || _state.FontSize == 0)
        {
            return;
        }

        var glyphs = _state.Font.Decode(s.Bytes);

        // Type3 glyphs are content-stream procedures, drawn as graphics rather
        // than emitted as selectable text.
        if (_state.Font.IsType3)
        {
            ShowType3Text(glyphs);
            return;
        }

        // `render` is what the browser paints; `real` is the selectable/searchable
        // text. For a glyph-mapped embedded font they differ: render uses a
        // per-code Private-Use-Area codepoint so the exact glyph is painted (no
        // shaping, no Unicode collisions), while real keeps the true Unicode.
        var render = new StringBuilder();
        var real = new StringBuilder();
        bool glyphMapped = _state.Font.UsesGlyphMap;
        double displacement = 0;

        foreach (ref readonly var g in CollectionsMarshal.AsSpan(glyphs))
        {
            real.Append(g.Unicode);
            int pua = glyphMapped ? _state.Font.GlyphPuaChar(g.Code) : -1;
            if (pua >= 0)
            {
                render.Append((char)pua);
            }
            else
            {
                render.Append(g.Unicode);
            }
            double w0 = g.Width1000 / 1000.0 * _state.FontSize;
            double spacing = _state.CharSpacing + (g.IsSpace ? _state.WordSpacing : 0);
            displacement += (w0 + spacing) * _state.HorizScale;
        }

        // Emit every non-empty run, including invisible modes 3/7 (OCR layers on
        // scanned PDFs). `displacement` is the PDF-computed advance, used for width
        // correction.
        if (render.Length > 0)
        {
            EmitText(render.ToString(), real.ToString(), displacement);
        }

        _textMatrix = BitPdfMatrix.Concat(_textMatrix, new BitPdfMatrix(1, 0, 0, 1, displacement, 0));
    }

    /// <summary>
    /// Renders a run of Type3 glyphs by executing each glyph's content-stream
    /// procedure at the current text position, advancing the text matrix between
    /// glyphs (so a subsequent glyph is drawn to the right of the previous one).
    /// </summary>
    private void ShowType3Text(List<BitPdfGlyph> glyphs)
    {
        // Type3 glyphs paint graphics directly (below), reached via a whitelisted
        // show-text operator - flush any pending coalesced line to keep paint order.
        FlushPaintedLine();

        BitPdfFont font = _state.Font!;
        BitPdfMatrix fontMatrix = font.Type3!.FontMatrix;
        bool visible = _state.RenderMode is not (3 or 7);

        foreach (ref readonly var g in CollectionsMarshal.AsSpan(glyphs))
        {
            if (visible && font.Type3.GetGlyphProcedure(g.Code) is { } proc)
            {
                RenderType3Glyph(proc, fontMatrix, font.Type3.Resources);
            }

            double w0 = g.Width1000 / 1000.0 * _state.FontSize;
            double spacing = _state.CharSpacing + (g.IsSpace ? _state.WordSpacing : 0);
            double displacement = (w0 + spacing) * _state.HorizScale;
            _textMatrix = BitPdfMatrix.Concat(_textMatrix, new BitPdfMatrix(1, 0, 0, 1, displacement, 0));
        }
    }

    private void RenderType3Glyph(BitPdfStream proc, in BitPdfMatrix fontMatrix, BitPdfDict? glyphResources)
    {
        if (_formDepth >= MaxFormDepth)
        {
            return;
        }
        _formDepth++;

        // Compose the glyph-space -> device transform from the current text
        // rendering matrix before mutating the graphics state.
        BitPdfMatrix trm = BitPdfMatrix.Concat(BitPdfMatrix.Concat(_state.Ctm, _textMatrix),
            new BitPdfMatrix(_state.FontSize * _state.HorizScale, 0, 0, _state.FontSize, 0, _state.TextRise));
        BitPdfMatrix glyphToDevice = BitPdfMatrix.Concat(trm, fontMatrix);

        _stack.Push(_state.Clone());
        _groupDepthStack.Push(_openGroups);
        BitPdfDict? savedResources = _resources;
        bool savedColorLocked = _type3ColorLocked;
        // Each glyph starts unlocked; a `d1` inside it locks colour for that glyph only.
        _type3ColorLocked = false;

        _state.Ctm = glyphToDevice;
        // Glyph procedures may reference their own resources; fall back to the
        // page resources so shared fonts/images still resolve.
        _resources = glyphResources ?? _page.Resources;

        try
        {
            byte[] content = BitPdfStreamDecoder.Decode(proc);
            RunOps(new BitPdfContentParser(content).Parse());
        }
        catch
        {
            // Ignore malformed glyph procedures.
        }

        _resources = savedResources;
        _type3ColorLocked = savedColorLocked;
        if (_stack.Count > 0)
        {
            _state = _stack.Pop();
            CloseGroupsTo(_groupDepthStack.Count > 0 ? _groupDepthStack.Pop() : 0);
        }
        _formDepth--;
    }

    private void EmitText(string renderText, string realText, double runAdvance)
    {
        // Map em-space (origin at the baseline, y up) to device pixels.
        BitPdfMatrix trm = BitPdfMatrix.Concat(BitPdfMatrix.Concat(_state.Ctm, _textMatrix),
            new BitPdfMatrix(_state.FontSize * _state.HorizScale, 0, 0, _state.FontSize, 0, _state.TextRise));

        double fontHeight = Math.Sqrt(trm.C * trm.C + trm.D * trm.D);
        if (fontHeight < 1e-3)
        {
            return;
        }

        // Target width in the span's LOCAL space (1em = fontHeight CSS px); the
        // viewer scales each run to this via --bit-pdv-sx so a substitute font's metrics
        // don't shift neighbouring runs.
        double denom = _state.FontSize * _state.HorizScale;
        double targetWidth = denom != 0 ? Math.Abs(runAdvance) * fontHeight / denom : 0;

        double a = trm.A / fontHeight;
        double b = trm.B / fontHeight;
        double c = -trm.C / fontHeight;
        double d = -trm.D / fontHeight;
        // The run's top-left origin in device space (its baseline lifted by the
        // ascent). Emitted as CSS left/top so the span's layout box coincides with
        // where it paints, instead of every run being anchored at (0,0) and moved
        // purely by a transform. The browser then hit-tests selection against real
        // geometry, so click-drag stays coherent (the pdf.js text-layer model).
        double left = trm.C * AscentFactor + trm.E;
        double top = trm.D * AscentFactor + trm.F;
        // Only the linear part (scale/rotation/skew) stays in `transform`; with
        // transform-origin:0 0 this is exactly equivalent to the old single matrix
        // that also carried the translation. scaleX(--bit-pdv-sx) width-corrects runs.
        string linear = string.Create(CultureInfo.InvariantCulture,
            $"matrix({a:0.####},{b:0.####},{c:0.####},{d:0.####},0,0) scaleX(var(--bit-pdv-sx,1))");

        int mode = _state.RenderMode;
        bool invisible = mode is 3 or 7;
        bool doFill = mode is 0 or 2 or 4 or 6;
        bool doStroke = mode is 1 or 2 or 5 or 6;

        // Feed the coalesced selection layer with the real Unicode (unless this run
        // is inside a hidden optional-content group, whose painted output is
        // discarded and so must not be selectable either).
        if (_ocHiddenAtDepth < 0)
        {
            AccumulateSelectionText(realText, trm, left, top, fontHeight, targetWidth, linear);
        }

        // Emit only the PAINTED layer. It is never selectable or searchable - all
        // selection and find-in-page run against the coalesced layer above.
        if (invisible)
        {
            // Render modes 3/7 (e.g. an OCR text layer over a scanned image) paint
            // nothing, so there is no glyph layer and no embedded @font-face to
            // inline - the coalesced selection span alone carries the text.
            return;
        }

        // Canvas mode: one fillText/strokeText op per run at its exact device
        // matrix (no DOM cost, so no coalescing) - the selection layer above
        // already carries the real Unicode.
        if (_ops is not null)
        {
            EmitCanvasText(renderText, trm, fontHeight, targetWidth, doFill, doStroke);
            return;
        }

        // Compact mode: coalesce painted runs into one span per visual line.
        // Embedded fonts - including PUA glyph-mapped ones, which is how every
        // embedded font in this engine paints - coalesce too: the browser lays the
        // merged run out with the font's own advance widths, so only explicit TJ
        // kerning between runs is approximated while data-w pins the line's total
        // advance. (A gap-bridging space missing from a subset font falls through
        // to the generic fallback in the font stack.) Only non-upright text
        // (rotated/mirrored/skewed) is excluded - its geometry can't be reduced
        // to a horizontal line.
        if (TextCoalescing == BitPdfTextCoalescing.Compact
            && a > 1e-3 && d > 1e-3
            && Math.Abs(b) < 1e-3 && Math.Abs(c) < 1e-3)
        {
            AccumulatePaintedRun(renderText, glyphMapped: renderText != realText,
                trm, left, top, fontHeight, targetWidth, a,
                BuildPaintedStyleTail(fontHeight, doFill, doStroke, linear));
            return;
        }

        // A glyph-mapped run paints exact glyphs via PUA codepoints; a plain run
        // paints its real text. Either way it is presentational only. An ineligible
        // run first flushes any pending coalesced line to preserve paint order.
        FlushPaintedLine();
        AppendTextSpan(renderText, fontHeight, targetWidth, left, top, linear,
            doFill, doStroke, glyphLayer: renderText != realText);
    }

    /// <summary>
    /// Emits one canvas text op. The matrix's linear part maps the run's local em
    /// space (x right, y down, baseline at the origin) to device space - identical
    /// to the HTML span's transform - and (e, f) is the baseline origin, matching
    /// canvas's alphabetic textBaseline. The interpreter measures the drawn text
    /// and applies scaleX width-correction to <paramref name="targetWidth"/>, the
    /// run's PDF-computed advance (the data-w mechanism, done inline).
    /// </summary>
    private void EmitCanvasText(string text, in BitPdfMatrix trm, double fontHeight, double targetWidth,
        bool doFill, bool doStroke)
    {
        BitPdfFont font = _state.Font!;
        string family = FontFamilyList(font); // registers the @font-face on first use
        double a = trm.A / fontHeight;
        double b = trm.B / fontHeight;
        double c = -trm.C / fontHeight;
        double d = -trm.D / fontHeight;
        double ls = 0, ws = 0;
        if (_state.FontSize != 0)
        {
            ls = _state.CharSpacing * fontHeight / _state.FontSize;
            ws = _state.WordSpacing * fontHeight / _state.FontSize;
        }
        double sw = doStroke ? Math.Max(_state.LineWidth * _state.Ctm.ScaleFactor, 0.1) : 0;
        Op("t", text, R(fontHeight), family, font.Bold, font.Italic,
            R4(a), R4(b), R4(c), R4(d), R(trm.E), R(trm.F),
            doFill ? _state.FillColor : null, doStroke ? _state.StrokeColor : null, R(sw),
            R(targetWidth), R(_state.FillAlpha), _state.BlendMode, R(ls), R(ws));
    }

    /// <summary>
    /// Compact mode: merges a painted substitute-font run into the pending visual
    /// line, or flushes it and starts a new one when the baseline, style, or
    /// horizontal continuity breaks. Word gaps are bridged with a space; anything
    /// wider (table cells, columns) or overlapping breaks the span so glyphs never
    /// paint far from their true positions.
    /// </summary>
    private void AccumulatePaintedRun(string text, bool glyphMapped,
        in BitPdfMatrix trm, double left, double top,
        double fontHeight, double targetWidth, double xScale, string tail)
    {
        double baseX = trm.E;
        double baseY = trm.F;

        if (_paintActive
            && tail == _paintTail
            && Math.Abs(fontHeight - _paintFontHeight) <= _paintFontHeight * 0.01
            && Math.Abs(baseY - _paintBaseY) <= fontHeight * 0.05)
        {
            // Gap to the previous run in the span's local space (1 local px = 1 CSS
            // px before the transform; the transform scales x by xScale).
            double gap = (baseX - _paintEndX) / xScale;
            if (gap >= -0.15 * fontHeight && gap <= 0.9 * fontHeight)
            {
                if (gap > 0.15 * fontHeight && _paintText.Length > 0
                    && _paintText[^1] != ' ' && text[0] != ' ')
                {
                    _paintText.Append(' ');
                }
                _paintText.Append(text);
                _paintGlyph |= glyphMapped;
                _paintEndX = baseX + targetWidth * xScale;
                return;
            }
        }

        FlushPaintedLine();
        _paintActive = true;
        _paintGlyph = glyphMapped;
        _paintText.Append(text);
        _paintLeft = left;
        _paintTop = top;
        _paintFontHeight = fontHeight;
        _paintBaseY = baseY;
        _paintStartX = baseX;
        _paintEndX = baseX + targetWidth * xScale;
        _paintXScale = xScale;
        _paintTail = tail;
    }

    /// <summary>
    /// Emits the pending coalesced painted line (Compact mode) as a single span,
    /// width-corrected via <c>data-w</c> to the line's total PDF advance.
    /// </summary>
    private void FlushPaintedLine()
    {
        if (!_paintActive)
        {
            return;
        }
        // Unlike per-run glyph spans (no correction: their advances are already
        // exact), a merged glyph line lost the TJ kerning between its runs, so it
        // carries data-w and gets scaleX-pinned to the line's true total advance.
        double width = (_paintEndX - _paintStartX) / _paintXScale;
        EmitPaintedSpan(_paintText.ToString(), _paintLeft, _paintTop, _paintFontHeight,
            glyphLayer: _paintGlyph, dataW: width, _paintTail);
        _paintActive = false;
        _paintGlyph = false;
        _paintText.Clear();
    }

    /// <summary>
    /// Merges a text run into the current visual line of the coalesced selection
    /// layer, or flushes the line and starts a new one when the run drops to a new
    /// baseline, changes size, or breaks horizontal continuity. Rotated runs are
    /// flushed individually (their geometry can't be reduced to a horizontal line).
    /// </summary>
    private void AccumulateSelectionText(string text, in BitPdfMatrix trm, double left, double top,
        double fontHeight, double targetWidth, string linear)
    {
        if (text.Length == 0)
        {
            return;
        }
        double baseX = trm.E;
        double baseY = trm.F;
        bool upright = Math.Abs(trm.B) < 1e-3 && Math.Abs(trm.C) < 1e-3;

        if (!upright)
        {
            // Non-horizontal text: emit as its own selection span, carrying the full
            // linear transform (matrix + scaleX) so the highlight box stays aligned
            // with the rotated glyphs.
            FlushSelectionLine();
            AppendSelectionSpan(text, left, top, fontHeight, targetWidth, linear);
            return;
        }

        bool sameLine = _selActive
            && Math.Abs(baseY - _selBaseY) <= fontHeight * 0.3
            && Math.Abs(fontHeight - _selFontHeight) <= _selFontHeight * 0.25
            && baseX >= _selEndX - fontHeight * 0.5;

        if (!sameLine)
        {
            FlushSelectionLine();
            _selActive = true;
            _selText.Clear();
            _selText.Append(text);
            _selLeft = left;
            _selTop = top;
            _selFontHeight = fontHeight;
            _selBaseY = baseY;
            _selStartX = baseX;
            _selEndX = baseX + targetWidth;
            return;
        }

        // Same line: bridge a horizontal gap wider than a fraction of the em with a
        // single space, so word boundaries survive and copied text stays readable.
        double gap = baseX - _selEndX;
        if (gap > fontHeight * 0.2 && _selText.Length > 0
            && _selText[^1] != ' ' && text[0] != ' ')
        {
            _selText.Append(' ');
        }
        _selText.Append(text);
        _selEndX = baseX + targetWidth;
    }

    /// <summary>Emits the accumulated visual line as one selection span.</summary>
    private void FlushSelectionLine()
    {
        if (!_selActive)
        {
            return;
        }
        double width = _selEndX - _selStartX;
        AppendSelectionSpan(_selText.ToString(), _selLeft, _selTop, _selFontHeight, width,
            "scaleX(var(--bit-pdv-sx,1))");
        _selActive = false;
        _selText.Clear();
    }

    /// <summary>
    /// Appends one transparent, selectable span to the selection layer buffer. A
    /// presentational <c>&lt;br&gt;</c> separates lines so clipboard copy preserves
    /// them. The span carries <c>data-w</c> so the viewer width-corrects it to the
    /// run's true device advance (<c>--bit-pdv-sx</c>), matching the painted glyphs.
    /// </summary>
    private void AppendSelectionSpan(string text, double left, double top,
        double fontHeight, double width, string transform)
    {
        if (_selLayer.Length > 0)
        {
            _selLayer.Append("<br>");
        }
        _selLayer.Append("<span data-bit-pdv-sel");
        if (width > 0.01)
        {
            _selLayer.Append(string.Create(CultureInfo.InvariantCulture, $" data-w=\"{width:0.###}\""));
        }
        _selLayer.Append(string.Create(CultureInfo.InvariantCulture,
            $" style=\"position:absolute;left:{left:0.##}px;top:{top:0.##}px;white-space:pre;line-height:1;font-size:{fontHeight:0.###}px;color:transparent;font-family:sans-serif;transform:{transform};transform-origin:0 0\">"));
        _selLayer.Append(Escape(text));
        _selLayer.Append("</span>");
    }

    private void AppendTextSpan(string text, double fontHeight, double targetWidth,
        double left, double top, string linear,
        bool doFill, bool doStroke, bool glyphLayer)
    {
        // The painted glyph layer uses the embedded font's real advance metrics, so
        // it must NOT be width-corrected (that would re-stretch correct glyphs). Only
        // substitute-font runs get scaleX correction.
        double dataW = glyphLayer ? 0 : targetWidth;
        EmitPaintedSpan(text, left, top, fontHeight, glyphLayer, dataW,
            BuildPaintedStyleTail(fontHeight, doFill, doStroke, linear));
    }

    /// <summary>
    /// Builds the style suffix (everything after <c>font-size</c>) of a painted
    /// text span from the current graphics state. In Compact mode this string
    /// doubles as the run's style identity: runs coalesce only while it is
    /// byte-identical, so any colour/stroke/font/transform change breaks the span.
    /// </summary>
    private string BuildPaintedStyleTail(double fontHeight, bool doFill, bool doStroke, string linear)
    {
        var sb = new StringBuilder();
        // Character/word spacing (Tc/Tw) are real inter-glyph gaps, not a stretch:
        // emit them as letter-/word-spacing so the run's natural width matches its
        // PDF advance (keeping the width-correction scaleX at ~1, no glyph spread).
        if (_state.FontSize != 0)
        {
            if (_state.CharSpacing != 0)
            {
                double ls = _state.CharSpacing * fontHeight / _state.FontSize;
                sb.Append(string.Create(CultureInfo.InvariantCulture, $";letter-spacing:{ls:0.###}px"));
            }
            if (_state.WordSpacing != 0)
            {
                double ws = _state.WordSpacing * fontHeight / _state.FontSize;
                sb.Append(string.Create(CultureInfo.InvariantCulture, $";word-spacing:{ws:0.###}px"));
            }
        }
        sb.Append(";color:").Append(!doFill ? "transparent" : _state.FillColor);
        if (doStroke)
        {
            double sw = Math.Max(_state.LineWidth * _state.Ctm.ScaleFactor, 0.1);
            sb.Append(string.Create(CultureInfo.InvariantCulture,
                $";-webkit-text-stroke:{sw:0.###}px ")).Append(_state.StrokeColor);
        }
        // The painted layer never participates in selection or hit-testing; the
        // coalesced selection layer above owns both.
        sb.Append(";user-select:none;-webkit-user-select:none;pointer-events:none");
        AppendFontStyle(sb, _state.Font!);
        sb.Append(";transform:").Append(linear).Append(";transform-origin:0 0");
        if (doFill && _state.FillAlpha < 1)
        {
            sb.Append(string.Create(CultureInfo.InvariantCulture, $";opacity:{_state.FillAlpha:0.###}"));
        }
        if (_state.BlendMode.Length > 0)
        {
            sb.Append(";mix-blend-mode:").Append(_state.BlendMode);
        }
        return sb.ToString();
    }

    /// <summary>Writes one painted span (per-run or coalesced) into the page.</summary>
    private void EmitPaintedSpan(string text, double left, double top, double fontHeight,
        bool glyphLayer, double dataW, string tail)
    {
        _html.Append("<span");
        if (glyphLayer)
        {
            _html.Append(" data-bit-pdv-glyph"); // painted glyphs; excluded from search
        }
        if (dataW > 0.01)
        {
            _html.Append(string.Create(CultureInfo.InvariantCulture, $" data-w=\"{dataW:0.###}\""));
        }
        _html.Append(string.Create(CultureInfo.InvariantCulture,
            $" style=\"position:absolute;left:{left:0.##}px;top:{top:0.##}px;white-space:pre;line-height:1"));
        _html.Append(string.Create(CultureInfo.InvariantCulture, $";font-size:{fontHeight:0.###}px"));
        _html.Append(tail);
        _html.Append("\">");
        _html.Append(Escape(text));
        _html.Append("</span>");
    }

    private void AppendFontStyle(StringBuilder sb, BitPdfFont font)
    {
        sb.Append(";font-family:").Append(FontFamilyList(font));
        if (font.Bold)
        {
            sb.Append(";font-weight:bold");
        }
        if (font.Italic)
        {
            sb.Append(";font-style:italic");
        }
    }

    /// <summary>
    /// The CSS font-family list for <paramref name="font"/>, registering the
    /// embedded face's <c>@font-face</c> rule on first use. Canvas text uses the
    /// same registered faces - <c>document.fonts</c> serves them to
    /// <c>ctx.fillText</c> once loaded.
    /// </summary>
    private string FontFamilyList(BitPdfFont font)
    {
        if (!font.HasEmbedded)
        {
            return font.GenericFamily;
        }
        string family = font.FontFaceFamily;
        if (_emittedFamilies.Add(family))
        {
            string b64 = Convert.ToBase64String(font.EmbeddedProgram!);
            string fmt = font.EmbeddedFormat!;
            string face =
                $"@font-face{{font-family:'{family}';src:url(data:font/{fmt};base64,{b64}) format('{fmt}');}}";
            _fontFaces.Append(face);
            _pageFaces.Append(face);
        }
        return $"{family},{font.GenericFamily}";
    }

    private void ApplyExtGState(BitPdfOperation op)
    {
        if (op.Operands.Count == 0 || op.Operands[0] is not BitPdfName name)
        {
            return;
        }
        if (_resources?.Get("ExtGState") is BitPdfDict ext && ext.Get(name.Value) is BitPdfDict gs)
        {
            if (gs.Get("ca") is double ca)
            {
                _state.FillAlpha = ca;
            }
            if (gs.Get("CA") is double strokeAlpha)
            {
                _state.StrokeAlpha = strokeAlpha;
            }
            if (gs.Get("LW") is double lw)
            {
                _state.LineWidth = lw;
            }
            if (gs.Get("LC") is double lc)
            {
                _state.LineCap = (int)lc;
            }
            if (gs.Get("LJ") is double lj)
            {
                _state.LineJoin = (int)lj;
            }
            if (gs.Get("ML") is double ml)
            {
                _state.MiterLimit = ml;
            }
            // /D is [dashArray phase]; convert to device space like the `d` operator.
            if (gs.Get("D") is List<object?> dashSpec && dashSpec.Count >= 1
                && dashSpec[0] is List<object?> dashArr)
            {
                var pattern = dashArr.Where(static o => o is double).Cast<double>()
                    .Select(v => v * _state.Ctm.ScaleFactor).ToArray();
                _state.DashArray = pattern.Length > 0 && pattern.Any(static v => v > 0) ? pattern : null;
                _state.DashPhase = dashSpec.Count >= 2 && dashSpec[1] is double ph ? ph * _state.Ctm.ScaleFactor : 0;
            }
            object? bm = gs.Get("BM");
            string? bmName = bm switch
            {
                BitPdfName n => n.Value,
                List<object?> arr when arr.Count > 0 && arr[0] is BitPdfName first => first.Value,
                _ => null,
            };
            if (bmName is not null)
            {
                _state.BlendMode = BlendCss(bmName);
            }
        }
    }

    // ----- Patterns & blend modes -----

    private void SetFillColorN(BitPdfOperation op)
    {
        // scn/sc with a trailing name selects a pattern; otherwise it's a color.
        if (op.Operands.Count > 0 && op.Operands[^1] is BitPdfName pattern)
        {
            _state.FillPattern = pattern.Value;
            // Uncolored (PaintType 2) patterns carry their paint color in the
            // leading numeric operands; keep it as the current fill color so the
            // cell content (which sets no color of its own) paints with it.
            string? color = ColorViaSpace(_state.FillColorSpace, op);
            if (color is not null)
            {
                _state.FillColor = color;
            }
        }
        else
        {
            _state.FillPattern = null;
            _state.FillColor = ColorViaSpace(_state.FillColorSpace, op) ?? _state.FillColor;
        }
    }

    private void SetStrokeColorN(BitPdfOperation op)
    {
        if (op.Operands.Count > 0 && op.Operands[^1] is BitPdfName)
        {
            return; // stroke patterns are not modeled; keep the prior color
        }
        _state.StrokeColor = ColorViaSpace(_state.StrokeColorSpace, op) ?? _state.StrokeColor;
    }

    /// <summary>Resolves the operand of a <c>cs</c>/<c>CS</c> operator to a color space.</summary>
    private BitPdfColorSpace? ResolveColorSpace(BitPdfOperation op)
    {
        if (op.Operands.Count == 0 || op.Operands[0] is not BitPdfName name)
        {
            return null;
        }
        return name.Value switch
        {
            "DeviceGray" or "G" => BitPdfColorSpace.Gray,
            "DeviceRGB" or "RGB" => BitPdfColorSpace.Rgb,
            "DeviceCMYK" or "CMYK" => BitPdfColorSpace.Cmyk,
            "Pattern" => null,
            _ => BitPdfColorSpace.Create(name, _xref, _resources),
        };
    }

    /// <summary>Sets the current color to the initial value of the selected space.</summary>
    private void SetDefaultColor(bool stroke)
    {
        BitPdfColorSpace? cs = stroke ? _state.StrokeColorSpace : _state.FillColorSpace;
        string color = cs is null ? "rgb(0,0,0)" : RgbString(cs.GetRgb(cs.DefaultComponents()));
        if (stroke)
        {
            _state.StrokeColor = color;
        }
        else
        {
            _state.FillColor = color;
            _state.FillPattern = null;
        }
    }

    /// <summary>
    /// Converts the numeric operands of <c>sc</c>/<c>scn</c> through the current
    /// color space. Falls back to inferring the space from the operand count.
    /// </summary>
    private static string? ColorViaSpace(BitPdfColorSpace? cs, BitPdfOperation op)
    {
        var nums = op.Operands.Where(static o => o is double).Cast<double>().ToArray();
        if (nums.Length == 0)
        {
            return null;
        }
        if (cs is not null)
        {
            return RgbString(cs.GetRgb(nums));
        }
        return ColorFromComponents(op);
    }

    private static string RgbString((byte R, byte G, byte B) c) => $"rgb({c.R},{c.G},{c.B})";

    private void SetDash(BitPdfOperation op)
    {
        _state.DashArray = null;
        _state.DashPhase = 0;
        if (op.Operands.Count >= 1 && op.Operands[0] is List<object?> arr && arr.Count > 0)
        {
            var pattern = arr.Where(static o => o is double).Cast<double>()
                .Select(v => v * _state.Ctm.ScaleFactor).ToArray();
            if (pattern.Length > 0 && pattern.Any(static v => v > 0))
            {
                _state.DashArray = pattern;
                if (op.Operands.Count >= 2 && op.Operands[1] is double phase)
                {
                    _state.DashPhase = phase * _state.Ctm.ScaleFactor;
                }
            }
        }
    }

    private string? ResolveFillPaint(string name)
    {
        // Key by the pattern's object identity (indirect ref or dict/stream
        // instance), not the bare resource name, so patterns in different
        // resource dictionaries that share a name don't alias each other.
        object cacheKey = name;
        if (_resources?.Get("Pattern") is BitPdfDict patterns)
        {
            object? raw = patterns.GetRaw(name);
            cacheKey = raw is BitPdfRef r ? r : _xref.FetchIfRef(raw) ?? name;
        }
        if (_patternCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }
        _patternCache[cacheKey] = null; // guard against recursion

        string? result = BuildPattern(name);
        _patternCache[cacheKey] = result;
        return result;
    }

    private string? BuildPattern(string name)    {
        if (_resources?.Get("Pattern") is not BitPdfDict patterns)
        {
            return null;
        }
        object? obj = patterns.Get(name);
        BitPdfDict? patDict = obj as BitPdfDict ?? (obj as BitPdfStream)?.Dict;
        if (patDict is null)
        {
            return null;
        }

        BitPdfMatrix matrix = patDict.Get("Matrix") is List<object?> m && m.Count >= 6
            ? new BitPdfMatrix(Num(m[0]), Num(m[1]), Num(m[2]), Num(m[3]), Num(m[4]), Num(m[5]))
            : BitPdfMatrix.Identity;
        int patternType = patDict.Get("PatternType") is double pt ? (int)pt : 0;

        // Shading patterns (type 2) map to CSS gradients. Tiling patterns
        // (type 1) have no simple HTML equivalent and fall back to a solid color.
        if (patternType == 2)
        {
            object? shadingObj = patDict.Get("Shading");
            BitPdfDict? shading = shadingObj as BitPdfDict ?? (shadingObj as BitPdfStream)?.Dict;
            if (shading is null)
            {
                return null;
            }
            return BitPdfCssShadingBuilder.Build(shading, _xref, _resources,
                BitPdfMatrix.Concat(_baseMatrix, matrix), _viewW, _viewH);
        }
        return null;
    }

    // Maximum number of pattern cells to emit for a single tiling fill; beyond
    // this the fill degrades to a solid color to bound the generated DOM.
    private const int MaxTiles = 4000;

    /// <summary>
    /// Renders a tiling pattern (PatternType 1) fill: the pattern cell's content
    /// stream is replayed across an XStep/YStep grid that covers the fill path's
    /// bounding box, all clipped to the fill path. Returns <c>false</c> (so the
    /// caller can fall back to a solid fill) when the pattern is not tiling or
    /// would require too many cells.
    /// </summary>
    private bool TryRenderTilingFill(string clipData, bool evenOdd)
    {
        if (_resources?.Get("Pattern") is not BitPdfDict patterns
            || patterns.Get(_state.FillPattern!) is not BitPdfStream patStream
            || patStream.Dict is null)
        {
            return false;
        }

        BitPdfDict pd = patStream.Dict;
        if ((pd.Get("PatternType") is double pt ? (int)pt : 0) != 1)
        {
            return false;
        }

        double xStep = Num(pd.Get("XStep"));
        double yStep = Num(pd.Get("YStep"));
        if (Math.Abs(xStep) < 1e-6 || Math.Abs(yStep) < 1e-6)
        {
            return false;
        }

        BitPdfMatrix patternMatrix = pd.Get("Matrix") is List<object?> m && m.Count >= 6
            ? new BitPdfMatrix(Num(m[0]), Num(m[1]), Num(m[2]), Num(m[3]), Num(m[4]), Num(m[5]))
            : BitPdfMatrix.Identity;
        BitPdfMatrix patToDevice = BitPdfMatrix.Concat(_baseMatrix, patternMatrix);
        if (patToDevice.Invert() is not BitPdfMatrix inv)
        {
            return false;
        }

        // Device-space bounding box of the fill region.
        if (!TryPathBounds(out double dMinX, out double dMinY, out double dMaxX, out double dMaxY))
        {
            return false;
        }

        // Map the device bbox corners into pattern space to find the cell range.
        double pMinX = double.MaxValue, pMinY = double.MaxValue;
        double pMaxX = double.MinValue, pMaxY = double.MinValue;
        foreach (var (dx, dy) in new[] { (dMinX, dMinY), (dMaxX, dMinY), (dMaxX, dMaxY), (dMinX, dMaxY) })
        {
            var (px, py) = inv.Apply(dx, dy);
            pMinX = Math.Min(pMinX, px); pMaxX = Math.Max(pMaxX, px);
            pMinY = Math.Min(pMinY, py); pMaxY = Math.Max(pMaxY, py);
        }

        (int iStart, int iEnd) = CellRange(pMinX, pMaxX, xStep);
        (int jStart, int jEnd) = CellRange(pMinY, pMaxY, yStep);
        long tileCount = (long)(iEnd - iStart + 1) * (jEnd - jStart + 1);
        if (tileCount is <= 0 or > MaxTiles)
        {
            return false;
        }

        double[] bbox = pd.Get("BBox") is List<object?> bb && bb.Count >= 4
            ? [Num(bb[0]), Num(bb[1]), Num(bb[2]), Num(bb[3])]
            : [0, 0, xStep, yStep];
        BitPdfDict? patRes = pd.Get("Resources") as BitPdfDict;

        // Clip the whole tiled group to the fill path.
        _html.Append("<div style=\"position:absolute;inset:0;clip-path:path(");
        if (evenOdd)
        {
            _html.Append("evenodd,");
        }
        _html.Append('\'').Append(clipData).Append("')\">");

        for (int j = jStart; j <= jEnd; j++)
        {
            for (int i = iStart; i <= iEnd; i++)
            {
                BitPdfMatrix cellCtm = BitPdfMatrix.Concat(patToDevice, new BitPdfMatrix(1, 0, 0, 1, i * xStep, j * yStep));
                RunPatternCell(patStream, cellCtm, patRes, bbox);
            }
        }

        _html.Append("</div>");
        return true;
    }

    private static (int, int) CellRange(double min, double max, double step)
    {
        double lo = Math.Min(min / step, max / step);
        double hi = Math.Max(min / step, max / step);
        return ((int)Math.Floor(lo) - 1, (int)Math.Ceiling(hi) + 1);
    }

    private bool TryPathBounds(out double minX, out double minY, out double maxX, out double maxY)
    {
        minX = minY = double.MaxValue;
        maxX = maxY = double.MinValue;
        bool any = false;
        foreach (var sub in _subpaths)
        {
            foreach (var (x, y) in sub)
            {
                minX = Math.Min(minX, x); maxX = Math.Max(maxX, x);
                minY = Math.Min(minY, y); maxY = Math.Max(maxY, y);
                any = true;
            }
        }
        return any;
    }

    /// <summary>Replays a single tiling-pattern cell at <paramref name="cellCtm"/>, clipped to its BBox.</summary>
    private void RunPatternCell(BitPdfStream patStream, in BitPdfMatrix cellCtm, BitPdfDict? patRes, double[] bbox)
    {
        if (_formDepth >= MaxFormDepth)
        {
            return;
        }
        _formDepth++;

        // Clip the cell to its BBox (transformed to device space) so content does
        // not bleed past the tile when XStep/YStep are smaller than the BBox.
        var (c0x, c0y) = cellCtm.Apply(bbox[0], bbox[1]);
        var (c1x, c1y) = cellCtm.Apply(bbox[2], bbox[1]);
        var (c2x, c2y) = cellCtm.Apply(bbox[2], bbox[3]);
        var (c3x, c3y) = cellCtm.Apply(bbox[0], bbox[3]);
        _html.Append(string.Create(CultureInfo.InvariantCulture,
            $"<div style=\"position:absolute;inset:0;clip-path:path('M{c0x:0.##} {c0y:0.##} L{c1x:0.##} {c1y:0.##} L{c2x:0.##} {c2y:0.##} L{c3x:0.##} {c3y:0.##} Z')\">"));

        _stack.Push(_state.Clone());
        _groupDepthStack.Push(_openGroups);
        BitPdfDict? savedResources = _resources;

        _state.Ctm = cellCtm;
        // Clear the pattern from the cell's own state: a cell that issues a fill
        // before setting a color would otherwise re-enter this same pattern and
        // blow up the DOM / CPU with unbounded recursion.
        _state.FillPattern = null;
        _resources = patRes ?? _resources;

        try
        {
            byte[] content = BitPdfStreamDecoder.Decode(patStream);
            RunOps(new BitPdfContentParser(content).Parse());
        }
        catch
        {
            // Ignore malformed pattern content.
        }

        _resources = savedResources;
        if (_stack.Count > 0)
        {
            _state = _stack.Pop();
            int target = _groupDepthStack.Count > 0 ? _groupDepthStack.Pop() : 0;
            while (_openGroups > target)
            {
                _html.Append("</div>");
                _openGroups--;
            }
        }
        _formDepth--;

        _html.Append("</div>");
    }

    private static string BlendCss(string pdfMode) => pdfMode switch
    {
        "Multiply" => "multiply",
        "Screen" => "screen",
        "Overlay" => "overlay",
        "Darken" => "darken",
        "Lighten" => "lighten",
        "ColorDodge" => "color-dodge",
        "ColorBurn" => "color-burn",
        "HardLight" => "hard-light",
        "SoftLight" => "soft-light",
        "Difference" => "difference",
        "Exclusion" => "exclusion",
        "Hue" => "hue",
        "Saturation" => "saturation",
        "Color" => "color",
        "Luminosity" => "luminosity",
        _ => "", // Normal / Compatible
    };

    // ----- Colors -----

    private static string Gray(double v)
    {
        int c = Clamp255(v);
        return $"rgb({c},{c},{c})";
    }

    private static string Rgb(double r, double g, double b)
        => $"rgb({Clamp255(r)},{Clamp255(g)},{Clamp255(b)})";

    private static string Cmyk(double c, double m, double y, double k)
    {
        // Single CMYK→RGB implementation lives in ColorSpace (pdf.js polynomial).
        var (r, g, b) = BitPdfColorSpace.CmykToRgb(c, m, y, k);
        return $"rgb({r},{g},{b})";
    }

    private static string? ColorFromComponents(BitPdfOperation op)
    {
        var nums = op.Operands.Where(static o => o is double).Cast<double>().ToList();
        return nums.Count switch
        {
            1 => Gray(nums[0]),
            3 => Rgb(nums[0], nums[1], nums[2]),
            4 => Cmyk(nums[0], nums[1], nums[2], nums[3]),
            _ => null,
        };
    }

    private static (byte R, byte G, byte B) ParseRgb(string rgb)
    {
        // Parses "rgb(r,g,b)" produced by the color helpers above.
        int start = rgb.IndexOf('(');
        int end = rgb.IndexOf(')');
        if (start < 0 || end <= start)
        {
            return (0, 0, 0);
        }
        var parts = rgb[(start + 1)..end].Split(',');
        byte P(int i) => i < parts.Length && int.TryParse(parts[i].Trim(), out int v) ? (byte)Math.Clamp(v, 0, 255) : (byte)0;
        return (P(0), P(1), P(2));
    }

    private static int Clamp255(double v) => (int)Math.Round(Math.Clamp(v, 0, 1) * 255);

    private static double Num(object? value) => value is double d ? d : 0;

    private static string Escape(string text)
    {
        var sb = new StringBuilder(text.Length);
        foreach (char ch in text)
        {
            switch (ch)
            {
                case '&': sb.Append("&amp;"); break;
                case '<': sb.Append("&lt;"); break;
                case '>': sb.Append("&gt;"); break;
                case '"': sb.Append("&quot;"); break;
                default:
                    if (ch < 0x20 && ch is not ('\t' or '\n' or '\r'))
                    {
                        break;
                    }
                    sb.Append(ch);
                    break;
            }
        }
        return sb.ToString();
    }
}
