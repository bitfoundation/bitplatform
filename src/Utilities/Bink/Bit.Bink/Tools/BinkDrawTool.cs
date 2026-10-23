
namespace Bit.Bink;

/// <summary>
/// The freehand drawing tool. Captures pointer input into a live stroke, draws it on the overlay while the pen is
/// down, and commits it to the document as one undoable operation when the pointer lifts.
/// </summary>
/// <remarks>
/// The live stroke is rendered on the overlay rather than the document surface, so nothing in the document has to be
/// redrawn while drawing. On commit the stroke moves to the document surface and its already-built geometry is reused.
/// </remarks>
public class BinkDrawTool : BinkToolBase
{
    private readonly BinkStrokeBuilder _builder = new();
    private readonly BinkPathBuilder _pathBuilder = new(512);
    private BinkStrokeGeometry? _liveGeometry;
    private Guid _targetLayerId;
    private int _lastGeometryCount = -1;
    private BinkRect _liveBounds = BinkRect.Empty;
    // Where the browser predicts the pointer is going, in view coordinates, since the last real sample.
    private readonly List<System.Numerics.Vector2> _prediction = new(4);

    /// <inheritdoc />
    public override string Id => "draw";

    /// <inheritdoc />
    public override string DisplayName => "Draw";

    /// <inheritdoc />
    public override bool WantsCoalescedInput => true;

    /// <summary>True while a stroke is being drawn.</summary>
    public bool IsDrawing => _builder.IsActive;

    /// <summary>
    /// Simplification tolerance in document units applied when the stroke is committed. Removes points that do not
    /// change the shape, which shrinks documents noticeably. Zero disables simplification.
    /// </summary>
    public float SimplifyTolerance { get; set; } = 0.12f;

    /// <summary>
    /// Pressure tolerance used when simplifying, as a fraction of the 0..1 pressure range. Simplification treats
    /// pressure as its own channel so a straight stroke drawn with changing pressure keeps its width variation.
    /// </summary>
    public float SimplifyPressureTolerance { get; set; } = 0.04f;

    /// <summary>Whether a pen's eraser button switches this tool to erasing for the duration of the stroke.</summary>
    public bool HonorPenEraserButton { get; set; } = true;

    /// <summary>The brush used for the current stroke (the context brush, or the eraser when the pen is flipped).</summary>
    protected BinkBrush? ActiveBrush { get; private set; }

    /// <inheritdoc />
    public override bool OnPointerDown(IBinkToolContext context, in BinkInputSample sample)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (_builder.IsActive) return false;
        if (!BinkLayerGate.Admits(context)) return false;

        BinkBrush brush = SelectBrush(context, sample);
        ActiveBrush = brush;
        _targetLayerId = context.ActiveLayerId;
        _builder.Begin(sample, brush, context.Viewport.ViewToDocument);
        _liveGeometry = null;
        _lastGeometryCount = -1;
        _prediction.Clear();
        _liveBounds = BinkRect.Empty;
        context.CapturePointer(sample.PointerId);
        context.InvalidateOverlay();
        return true;
    }

    /// <summary>Chooses the brush for a stroke. Override to vary the brush by device or button.</summary>
    protected virtual BinkBrush SelectBrush(IBinkToolContext context, in BinkInputSample sample)
    {
        if (HonorPenEraserButton && sample.Kind == BinkPointerKind.Pen && sample.IsEraserButton)
        {
            return new BinkPixelEraserBrush { Width = MathF.Max(context.Brush.Width * 4f, 16f) };
        }
        return context.Brush;
    }

    /// <inheritdoc />
    public override void OnPointerMove(IBinkToolContext context, in BinkInputSample sample)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!_builder.IsActive || sample.PointerId != _builder.PointerId) return;
        if (sample.IsPredicted)
        {
            // A prediction is drawn, never stored: it extends the live stroke to where the pen probably is by the
            // time the frame reaches the screen, hiding a frame of latency, and the next real sample replaces it.
            if (!UsePrediction) return;
            _prediction.Add(sample.Position);
            context.InvalidateOverlay();
            return;
        }
        bool hadPrediction = _prediction.Count > 0;
        _prediction.Clear();
        if (_builder.Add(sample) || hadPrediction) context.InvalidateOverlay();
    }

    /// <summary>
    /// Whether the live stroke is extended by the browser's prediction of where the pointer is going, when the
    /// canvas delivers predictions. The prediction is only ever drawn on the overlay; the committed stroke contains
    /// measured points alone.
    /// </summary>
    /// <remarks>
    /// The canvas hands predictions to drawing tools only, through <see cref="OnPointerMove"/> with
    /// <see cref="BinkInputSample.IsPredicted"/> set. A subclass that overrides it and acts on positions itself
    /// must ignore predicted samples, as the eraser does: they are where the pointer may be, not where it was.
    /// </remarks>
    public bool UsePrediction { get; set; } = true;

    /// <inheritdoc />
    public override void OnPointerUp(IBinkToolContext context, in BinkInputSample sample)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!_builder.IsActive || sample.PointerId != _builder.PointerId) return;
        context.ReleasePointer(sample.PointerId);
        BinkStrokePoints points = _builder.End(sample);
        BinkBrush brush = ActiveBrush ?? context.Brush;
        ActiveBrush = null;
        _liveGeometry = null;
        _lastGeometryCount = -1;
        _prediction.Clear();
        CommitStroke(context, points, brush);
        context.InvalidateOverlay();
    }

    /// <summary>Creates and adds the stroke element. Override to attach metadata or post-process points.</summary>
    protected virtual void CommitStroke(IBinkToolContext context, BinkStrokePoints points, BinkBrush brush)
    {
        if (points.Count == 0) return;
        BinkStrokePoints final = SimplifyTolerance > 0 ? points.Simplify(SimplifyTolerance, SimplifyPressureTolerance) : points;
        var stroke = new BinkStroke(final, brush) { Input = _builder.InputInfo };

        // The stroke belongs to the layer it started on: re-reading the active layer here would silently move it if the
        // active layer changed while the pen was down. Only a layer deleted meanwhile hands it to the active one. A
        // layer locked or hidden meanwhile refuses it, as it would have refused the stroke had it been so from the start.
        Guid layerId = context.Document.TryGetLayer(_targetLayerId, out _) ? _targetLayerId : context.ActiveLayerId;
        if (!BinkLayerGate.Admits(context, layerId)) return;

        if (!TryApply(context, () => context.Document.AddElement(stroke, layerId))) return;
        context.InvalidateDocument(stroke.Bounds);
    }

    /// <summary>
    /// Applies a document change, reporting a refusal instead of letting it escape. Document limits throw, and an
    /// exception here would unwind through the browser callback and strand the gesture.
    /// </summary>
    protected static bool TryApply(IBinkToolContext context, Action change)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(change);
        try
        {
            change();
            return true;
        }
        catch (InvalidOperationException ex)
        {
            context.Announce("That change could not be applied: " + ex.Message);
            return false;
        }
    }

    /// <inheritdoc />
    public override void OnPointerCancel(IBinkToolContext context, in BinkInputSample sample)
    {
        ArgumentNullException.ThrowIfNull(context);
        // Only the contact that started the stroke can end it. A resting palm, a second finger the browser
        // rejects, or a stray pointer leaving the element all raise a cancel of their own, and acting on those
        // would throw away the stroke the user is still drawing.
        if (!_builder.IsActive || sample.PointerId != _builder.PointerId) return;
        context.ReleasePointer(sample.PointerId);
        _builder.Cancel();
        ActiveBrush = null;
        _liveGeometry = null;
        _lastGeometryCount = -1;
        _prediction.Clear();
        context.InvalidateOverlay();
    }

    /// <inheritdoc />
    public override void Deactivate(IBinkToolContext context)
    {
        // A tool that is swapped out mid-stroke must not leave a live builder behind: the next activation would
        // resume a stroke the user abandoned, and the overlay would keep showing it.
        if (_builder.IsActive)
        {
            if (_builder.PointerId >= 0) context?.ReleasePointer(_builder.PointerId);
            _builder.Cancel();
        }
        ActiveBrush = null;
        _liveGeometry = null;
        _lastGeometryCount = -1;
        _prediction.Clear();
        context?.InvalidateOverlay();
    }

    /// <inheritdoc />
    public override void DrawOverlay(IBinkToolContext context, IBinkRenderContext render)
    {
        ArgumentNullException.ThrowIfNull(render);
        if (!_builder.IsActive || _builder.Count == 0) return;
        BinkBrush brush = ActiveBrush ?? context.Brush;
        BinkStrokeGeometry geometry = GetLiveGeometry(brush);
        _liveBounds = geometry.Bounds;
        if (brush.BlendMode == BinkBlendMode.DestinationOut)
        {
            // An eraser cannot erase on the overlay (there is nothing under it), so show its outline instead.
            render.StrokePath(geometry.Path, new BinkStrokePaint(new BinkColor(120, 120, 120), 1f, 0.9f, BinkBlendMode.Normal, BinkLineCap.Round, BinkLineJoin.Round));
            return;
        }
        geometry.Draw(render, brush.Color, brush.Opacity, brush.BlendMode);
    }

    private BinkStrokeGeometry GetLiveGeometry(BinkBrush brush)
    {
        // The live stroke changes every frame, so geometry is rebuilt into a reused path builder rather than cached.
        if (_prediction.Count > 0)
        {
            // The predicted points are appended for this one build and taken off again, so the stroke's own points,
            // and what is committed when the pen lifts, never contain them.
            int predicted = _builder.AppendPrediction(System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_prediction));
            try
            {
                _liveGeometry = brush.BuildGeometry(_builder.Points.AsView(), _pathBuilder);
            }
            finally
            {
                _builder.RemovePrediction(predicted);
            }
            _lastGeometryCount = -1;
            return _liveGeometry;
        }
        if (_liveGeometry is not null && _lastGeometryCount == _builder.Count) return _liveGeometry;
        _liveGeometry = brush.BuildGeometry(_builder.Points.AsView(), _pathBuilder);
        _lastGeometryCount = _builder.Count;
        return _liveGeometry;
    }

    /// <summary>Bounds of the live stroke in document units, used by the host to size overlay redraws.</summary>
    public BinkRect LiveBounds => _liveBounds;

    /// <inheritdoc />
    public override bool OnKeyDown(IBinkToolContext context, BinkToolKeyEvent key)
    {
        if (key.Key == "Escape" && _builder.IsActive)
        {
            _builder.Cancel();
            ActiveBrush = null;
            _liveGeometry = null;
            context?.InvalidateOverlay();
            return true;
        }
        return false;
    }
}
