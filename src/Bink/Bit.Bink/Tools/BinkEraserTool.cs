using System.Numerics;

namespace Bit.Bink;

/// <summary>How the eraser removes ink. The three modes behave differently on purpose; pick the one that matches the application.</summary>
public enum BinkEraserMode : byte
{
    /// <summary>
    /// Deletes any stroke the eraser touches, whole. Predictable and cheap, and the behaviour users expect from
    /// note-taking apps. Erasing is exactly reversible.
    /// </summary>
    Stroke = 0,

    /// <summary>
    /// Removes only the touched portion, splitting strokes into fragments. Feels like a real eraser and keeps the
    /// result fully vector, at the cost of creating additional elements.
    /// </summary>
    Segment = 1,

    /// <summary>
    /// Draws an eraser stroke that masks out everything below it within the layer (destination-out compositing).
    /// Nothing is deleted, so the erase is itself an object that can be undone, re-ordered or removed later, and it
    /// can erase parts of images. Exports as an SVG mask.
    /// </summary>
    Pixel = 2,
}

/// <summary>
/// The eraser. Supports three genuinely different strategies (see <see cref="BinkEraserMode"/>) and erases continuously
/// while the pointer is down, grouping the whole gesture into one undo step.
/// </summary>
public sealed class BinkEraserTool : BinkDrawTool
{
    private readonly List<Vector2> _path = new(64);
    private readonly List<BinkStroke> _fragments = new(4);
    private readonly List<Guid> _erasedThisGesture = new();
    // Reused across the many move samples of one gesture: erasing runs on every pointer move, and a fresh list
    // per sample is pure garbage on the hottest path the tool has.
    private readonly List<BinkDrawingOperation> _operations = new(8);
    private IDisposable? _historyGroup;
    private bool _erasing;
    private int _pointerId = -1;
    private Vector2 _lastPosition;
    private BinkEraserMode _activeMode;

    /// <inheritdoc />
    public override string Id => "eraser";

    /// <inheritdoc />
    public override string DisplayName => "Eraser";

    /// <inheritdoc />
    public override string Cursor => "cell";

    /// <summary>The erasing strategy.</summary>
    public BinkEraserMode Mode { get; set; } = BinkEraserMode.Stroke;

    /// <summary>Eraser radius in document units.</summary>
    public float Radius { get; set; } = 10f;

    /// <summary>The last eraser position in document units, for cursor rendering.</summary>
    public Vector2 LastPosition => _lastPosition;

    /// <summary>True while erasing.</summary>
    public bool IsErasing => _erasing;

    /// <inheritdoc />
    protected override BinkBrush SelectBrush(IBinkToolContext context, in BinkInputSample sample)
        => new BinkPixelEraserBrush { Width = Radius * 2f };

    /// <inheritdoc />
    public override bool OnPointerDown(IBinkToolContext context, in BinkInputSample sample)
    {
        ArgumentNullException.ThrowIfNull(context);
        // The mode is fixed for the whole gesture: switching it half way through would route the release to a
        // different code path and leave the history group open, which would disable undo. That includes a second
        // pointer landing while a gesture is running: latching the mode for it would re-route the first pointer's
        // release, so it is refused before the mode is read.
        if (_erasing || IsDrawing) return false;
        _activeMode = Mode;
        if (_activeMode == BinkEraserMode.Pixel) return base.OnPointerDown(context, sample);

        _erasing = true;
        _pointerId = sample.PointerId;
        _path.Clear();
        _erasedThisGesture.Clear();
        Vector2 p = context.Viewport.ToDocument(sample.Position);
        _lastPosition = p;
        _path.Add(p);
        _historyGroup = context.History.BeginGroup("Erase");
        context.CapturePointer(sample.PointerId);
        EraseAt(context);
        context.InvalidateOverlay();
        return true;
    }

    /// <inheritdoc />
    public override void OnPointerMove(IBinkToolContext context, in BinkInputSample sample)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (_activeMode == BinkEraserMode.Pixel) { base.OnPointerMove(context, sample); return; }
        if (!_erasing || sample.PointerId != _pointerId) return;
        Vector2 p = context.Viewport.ToDocument(sample.Position);
        // Keep the swept path short: only the newest segment is needed for each erase pass.
        if (_path.Count > 0 && Vector2.DistanceSquared(_path[^1], p) < 0.01f) return;
        _path.Add(p);
        _lastPosition = p;
        EraseAt(context);
        if (_path.Count > 32) _path.RemoveRange(0, _path.Count - 2);
        context.InvalidateOverlay();
    }

    /// <inheritdoc />
    public override void OnPointerUp(IBinkToolContext context, in BinkInputSample sample)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (_activeMode == BinkEraserMode.Pixel) { base.OnPointerUp(context, sample); return; }
        if (!_erasing || sample.PointerId != _pointerId) return;
        context.ReleasePointer(sample.PointerId);
        EndGesture(context);
    }

    /// <inheritdoc />
    public override void OnPointerCancel(IBinkToolContext context, in BinkInputSample sample)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (_activeMode == BinkEraserMode.Pixel) { base.OnPointerCancel(context, sample); return; }
        // Only the contact that started the gesture can end it; see DrawTool.OnPointerCancel.
        if (!_erasing || sample.PointerId != _pointerId) return;
        context.ReleasePointer(sample.PointerId);
        EndGesture(context);
    }

    private void EndGesture(IBinkToolContext context)
    {
        _erasing = false;
        _pointerId = -1;
        _historyGroup?.Dispose();
        _historyGroup = null;
        if (_erasedThisGesture.Count > 0) context.Announce($"Erased {_erasedThisGesture.Count} strokes.");
        _erasedThisGesture.Clear();
        _path.Clear();
        context.InvalidateOverlay();
    }

    /// <inheritdoc />
    public override void Deactivate(IBinkToolContext context)
    {
        if (_erasing) EndGesture(context);
        // Always release the history group, even if the gesture flag was already cleared by an earlier path.
        _historyGroup?.Dispose();
        _historyGroup = null;
        base.Deactivate(context);
    }

    private void EraseAt(IBinkToolContext context)
    {
        BinkDocument document = context.Document;
        ReadOnlySpan<Vector2> path = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_path);
        // Only the newest segment is tested each pass; earlier ones were handled on previous moves.
        ReadOnlySpan<Vector2> segment = path.Length >= 2 ? path[^2..] : path;
        BinkRect area = BinkGeometryMath.Bounds(segment).Inflate(Radius);

        List<BinkDrawingOperation> operations = _operations;
        operations.Clear();
        foreach (BinkLayer layer in document.Layers)
        {
            if (layer.IsLocked || !layer.IsVisible) continue;
            IReadOnlyList<BinkElement> elements = layer.Elements;
            for (int i = elements.Count - 1; i >= 0; i--)
            {
                BinkElement element = elements[i];
                if (!element.Bounds.Intersects(area)) continue;

                if (element is BinkStroke stroke && _activeMode == BinkEraserMode.Segment)
                {
                    // A pixel-eraser stroke is a mask, not ink. Cutting it would bring back the ink it hides, the
                    // opposite of erasing; the stroke eraser leaves masks alone for the same reason.
                    if (stroke.IsEraser) continue;
                    if (!BinkStrokeEditor.EraseSegments(stroke, segment, Radius, _fragments)) continue;
                    if (_fragments.Count == 0)
                    {
                        operations.Add(new BinkRemoveElementOperation(stroke.Id));
                    }
                    else
                    {
                        operations.Add(new BinkReplaceElementOperation(_fragments[0]));
                        for (int f = 1; f < _fragments.Count; f++) operations.Add(new BinkAddElementOperation(layer.Id, _fragments[f], i + f));
                    }
                    _erasedThisGesture.Add(stroke.Id);
                }
                else if (HitsElement(element, segment))
                {
                    operations.Add(new BinkRemoveElementOperation(element.Id));
                    _erasedThisGesture.Add(element.Id);
                }
            }
        }

        if (operations.Count == 0) return;
        try
        {
            // BatchOperation keeps the list it is given, so the reused buffer is copied before handing it over.
            document.Apply(operations.Count == 1 ? operations[0] : new BinkBatchOperation([.. operations], "Erase"));
        }
        catch (InvalidOperationException ex)
        {
            // Segment erasing can split a stroke into more elements than the document allows. Report it and let
            // the gesture end cleanly rather than unwinding through the browser callback.
            context.Announce("Erasing was refused: " + ex.Message);
            return;
        }
        context.InvalidateDocument(area);
    }

    private bool HitsElement(BinkElement element, ReadOnlySpan<Vector2> segment)
    {
        if (!element.IsSelectable) return false;
        foreach (Vector2 p in segment)
        {
            if (element.HitTest(p, Radius)) return true;
        }
        if (segment.Length >= 2)
        {
            // Sample along the swept segment so a fast drag does not skip over thin strokes.
            float length = Vector2.Distance(segment[0], segment[^1]);
            int steps = (int)MathF.Min(MathF.Ceiling(length / MathF.Max(Radius, 1f)), 32);
            for (int s = 1; s < steps; s++)
            {
                Vector2 p = Vector2.Lerp(segment[0], segment[^1], s / (float)steps);
                if (element.HitTest(p, Radius)) return true;
            }
        }
        return false;
    }

    /// <inheritdoc />
    public override void DrawOverlay(IBinkToolContext context, IBinkRenderContext render)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(render);
        if (_activeMode == BinkEraserMode.Pixel)
        {
            base.DrawOverlay(context, render);
            return;
        }
        if (!_erasing) return;
        // Show the eraser's reach so users understand what will be removed.
        var circle = new BinkPathBuilder(8).AddCircle(_lastPosition, Radius).ToGeometry();
        render.FillPath(circle, new BinkFillPaint(new BinkColor(255, 255, 255, 40)));
        render.StrokePath(circle, new BinkStrokePaint(new BinkColor(90, 90, 90), 1f, 0.9f));
    }
}
