using System.Numerics;

namespace Bit.Bink;

/// <summary>The handles drawn around a selection.</summary>
public enum BinkSelectionHandle : byte
{
    /// <summary>No handle.</summary>
    None = 0,
    /// <summary>Top-left corner.</summary>
    TopLeft = 1,
    /// <summary>Top-center edge.</summary>
    Top = 2,
    /// <summary>Top-right corner.</summary>
    TopRight = 3,
    /// <summary>Right-center edge.</summary>
    Right = 4,
    /// <summary>Bottom-right corner.</summary>
    BottomRight = 5,
    /// <summary>Bottom-center edge.</summary>
    Bottom = 6,
    /// <summary>Bottom-left corner.</summary>
    BottomLeft = 7,
    /// <summary>Left-center edge.</summary>
    Left = 8,
    /// <summary>The rotation handle above the selection.</summary>
    Rotate = 9,
}

/// <summary>
/// Select, move, resize, rotate, duplicate and delete elements. Dragging on empty space draws a marquee;
/// dragging an element moves it; dragging a handle resizes or rotates the whole selection.
/// </summary>
/// <remarks>
/// Manipulation only changes each element's transform, so stroke points, text and images are never rewritten and
/// the operation is exactly reversible. The whole gesture is one undo step.
/// </remarks>
public sealed class BinkSelectTool : BinkToolBase
{
    private enum DragKind { None, Marquee, Move, Resize, Rotate }

    private DragKind _drag;
    private int _pointerId = -1;
    private Vector2 _startDocument;
    private Vector2 _currentDocument;
    private BinkRect _startBounds;
    private BinkSelectionHandle _handle;
    private IDisposable? _historyGroup;
    private readonly Dictionary<Guid, Matrix3x2> _startTransforms = new();
    private readonly List<BinkElement> _pending = new();
    private bool _addToSelection;
    private bool _movedPastThreshold;
    private bool _constrain;

    /// <inheritdoc />
    public override string Id => "select";

    /// <inheritdoc />
    public override string DisplayName => "Select";

    /// <inheritdoc />
    public override string Cursor => "default";

    /// <summary>Size of the handles in view (CSS) pixels.</summary>
    public float HandleSize { get; set; } = 9f;

    /// <summary>Distance in view pixels between the selection and its rotation handle.</summary>
    public float RotateHandleDistance { get; set; } = 24f;

    /// <summary>Extra hit distance in view pixels when clicking thin elements.</summary>
    public float HitTolerance { get; set; } = 5f;

    /// <summary>
    /// How much the handle and element hit areas grow for a finger. A fingertip covers far more than a mouse
    /// cursor and cannot be aimed as precisely, so the drawn handles stay small while what they respond to gets
    /// close to the 24 by 24 pixel target that WCAG 2.5.8 asks for.
    /// </summary>
    public float TouchTargetScale { get; set; } = 2.8f;

    /// <summary>Whether resizing keeps the aspect ratio unless Shift is held (the default inverts this for corners).</summary>
    public bool PreserveAspectByDefault { get; set; }

    /// <summary>The marquee rectangle in document coordinates while dragging one, otherwise empty.</summary>
    public BinkRect Marquee => _drag == DragKind.Marquee ? BinkRect.FromPoints(_startDocument, _currentDocument) : BinkRect.Empty;

    /// <inheritdoc />
    public override bool OnPointerDown(IBinkToolContext context, in BinkInputSample sample)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (_drag != DragKind.None) return false;

        BinkViewport viewport = context.Viewport;
        Vector2 doc = viewport.ToDocument(sample.Position);
        _startDocument = doc;
        _currentDocument = doc;
        _pointerId = sample.PointerId;
        _addToSelection = sample.Shift || sample.Ctrl || sample.IsSecondaryButton;
        _movedPastThreshold = false;

        BinkSelection selection = context.Selection;
        float targetScale = sample.Kind == BinkPointerKind.Touch ? TouchTargetScale : 1f;
        float tolerance = HitTolerance * targetScale / viewport.Zoom;

        // A handle of the current selection takes priority over anything under the cursor.
        if (!selection.IsEmpty)
        {
            BinkSelectionHandle handle = HitHandle(context, doc, targetScale);
            if (handle != BinkSelectionHandle.None)
            {
                _handle = handle;
                _drag = handle == BinkSelectionHandle.Rotate ? DragKind.Rotate : DragKind.Resize;
                _startBounds = selection.Bounds;
                CaptureTransforms(context);
                _historyGroup = context.History.BeginGroup(_drag == DragKind.Rotate ? "Rotate" : "Resize");
                context.CapturePointer(sample.PointerId);
                return true;
            }
        }

        BinkElement? hit = BinkDocumentQuery.HitTest(context.Document, doc, tolerance);
        if (hit is null)
        {
            if (!_addToSelection) selection.Clear();
            _drag = DragKind.Marquee;
            context.CapturePointer(sample.PointerId);
            context.InvalidateOverlay();
            return true;
        }

        bool wasSelected = selection.Contains(hit.Id);
        if (_addToSelection) selection.Toggle(hit.Id);
        else if (!wasSelected) selection.Set(hit.Id);

        if (selection.IsEmpty)
        {
            context.Announce("Selection cleared.");
            return true;
        }
        // Picking something up has to be announced too, not only marquee and keyboard selection, or a screen
        // reader user who taps an element is told nothing about what they now have.
        if (!wasSelected || _addToSelection)
        {
            context.Announce(selection.Count == 1
                ? "Selected: " + hit.GetAccessibleDescription()
                : $"{selection.Count} elements selected.");
        }
        _drag = DragKind.Move;
        _startBounds = selection.Bounds;
        CaptureTransforms(context);
        _historyGroup = context.History.BeginGroup("Move");
        context.CapturePointer(sample.PointerId);
        context.SetCursor("grabbing");
        return true;
    }

    private void CaptureTransforms(IBinkToolContext context)
    {
        _startTransforms.Clear();
        foreach (BinkElement e in context.Selection.Elements) _startTransforms[e.Id] = e.Transform;
    }

    /// <inheritdoc />
    public override void OnPointerMove(IBinkToolContext context, in BinkInputSample sample)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (_drag == DragKind.None || sample.PointerId != _pointerId) return;
        _currentDocument = context.Viewport.ToDocument(sample.Position);
        _constrain = sample.Shift;
        Vector2 delta = _currentDocument - _startDocument;
        if (!_movedPastThreshold && delta.Length() * context.Viewport.Zoom < 3f) return;
        _movedPastThreshold = true;

        switch (_drag)
        {
            case DragKind.Marquee:
                context.InvalidateOverlay();
                break;
            case DragKind.Move:
                ApplyTransform(context, Matrix3x2.CreateTranslation(delta));
                break;
            case DragKind.Resize:
                ApplyTransform(context, BuildResize(delta, _constrain));
                break;
            case DragKind.Rotate:
                ApplyTransform(context, BuildRotate());
                break;
        }
    }

    /// <inheritdoc />
    public override void OnPointerUp(IBinkToolContext context, in BinkInputSample sample)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (_drag == DragKind.None || sample.PointerId != _pointerId) return;
        context.ReleasePointer(sample.PointerId);
        // Apply the release position so the gesture ends exactly where the pointer was let go.
        if (_drag is DragKind.Move or DragKind.Resize or DragKind.Rotate) OnPointerMove(context, sample);
        _currentDocument = context.Viewport.ToDocument(sample.Position);

        if (_drag == DragKind.Marquee && _movedPastThreshold)
        {
            BinkRect rect = BinkRect.FromPoints(_startDocument, _currentDocument);
            List<BinkElement> found = BinkDocumentQuery.FindInRect(context.Document, rect);
            var ids = found.Select(static e => e.Id).ToList();
            if (_addToSelection) foreach (Guid id in ids) context.Selection.Add(id);
            else context.Selection.Set(ids);
            context.Announce(ids.Count == 0 ? "Nothing selected." : $"{ids.Count} elements selected.");
        }

        Finish(context);
    }

    /// <inheritdoc />
    public override void OnPointerCancel(IBinkToolContext context, in BinkInputSample sample)
    {
        ArgumentNullException.ThrowIfNull(context);
        // Only the contact that started the drag can end it; see DrawTool.OnPointerCancel.
        if (_drag == DragKind.None || sample.PointerId != _pointerId) return;
        context.ReleasePointer(sample.PointerId);
        Cancel(context);
    }

    /// <inheritdoc />
    public override void Deactivate(IBinkToolContext context)
    {
        // Being swapped out mid-drag must not leave the history group open: that would disable undo entirely.
        if (_drag != DragKind.None) Cancel(context);
    }

    /// <summary>Restores the original transforms and ends the gesture, leaving no history step behind.</summary>
    private void Cancel(IBinkToolContext context)
    {
        // The restore is collected into the same group as the move, and the group is then discarded: a cancelled
        // drag should leave the document unchanged and the undo stack untouched.
        RestoreTransforms(context);
        context.History.CancelGroup();
        Finish(context);
    }

    private void Finish(IBinkToolContext context)
    {
        _drag = DragKind.None;
        _pointerId = -1;
        _handle = BinkSelectionHandle.None;
        _startTransforms.Clear();
        _historyGroup?.Dispose();
        _historyGroup = null;
        context.SetCursor(Cursor);
        context.InvalidateOverlay();
    }

    private void RestoreTransforms(IBinkToolContext context)
    {
        if (_startTransforms.Count == 0) return;
        _pending.Clear();
        foreach (BinkElement e in context.Selection.Elements)
        {
            if (_startTransforms.TryGetValue(e.Id, out Matrix3x2 original) && e.Transform != original) _pending.Add(e with { Transform = original });
        }
        if (_pending.Count == 0) return;
        try
        {
            context.Document.ReplaceElements(_pending, "Cancel");
        }
        catch (InvalidOperationException)
        {
            // Nothing useful to do: the elements are already where the document will allow them.
        }
    }

    private Matrix3x2 BuildResize(Vector2 delta, bool shift)
    {
        BinkRect b = _startBounds;
        if (b.Width <= 0 || b.Height <= 0) return Matrix3x2.Identity;

        float left = b.Left, top = b.Top, right = b.Right, bottom = b.Bottom;
        switch (_handle)
        {
            case BinkSelectionHandle.TopLeft: left += delta.X; top += delta.Y; break;
            case BinkSelectionHandle.Top: top += delta.Y; break;
            case BinkSelectionHandle.TopRight: right += delta.X; top += delta.Y; break;
            case BinkSelectionHandle.Right: right += delta.X; break;
            case BinkSelectionHandle.BottomRight: right += delta.X; bottom += delta.Y; break;
            case BinkSelectionHandle.Bottom: bottom += delta.Y; break;
            case BinkSelectionHandle.BottomLeft: left += delta.X; bottom += delta.Y; break;
            case BinkSelectionHandle.Left: left += delta.X; break;
        }

        float sx = b.Width > BinkGeometryMath.Epsilon ? (right - left) / b.Width : 1f;
        float sy = b.Height > BinkGeometryMath.Epsilon ? (bottom - top) / b.Height : 1f;
        bool corner = _handle is BinkSelectionHandle.TopLeft or BinkSelectionHandle.TopRight or BinkSelectionHandle.BottomLeft or BinkSelectionHandle.BottomRight;
        if (corner && (PreserveAspectByDefault ^ shift))
        {
            float s = MathF.Abs(sx) > MathF.Abs(sy) ? sx : sy;
            sx = MathF.CopySign(MathF.Abs(s), sx);
            sy = MathF.CopySign(MathF.Abs(s), sy);
        }
        // Never collapse to zero: that would be an unrecoverable transform.
        if (MathF.Abs(sx) < 0.01f) sx = MathF.CopySign(0.01f, sx == 0 ? 1 : sx);
        if (MathF.Abs(sy) < 0.01f) sy = MathF.CopySign(0.01f, sy == 0 ? 1 : sy);

        // The anchor is the corner or edge opposite the dragged handle.
        Vector2 anchor = _handle switch
        {
            BinkSelectionHandle.TopLeft => new Vector2(b.Right, b.Bottom),
            BinkSelectionHandle.Top => new Vector2(b.Left, b.Bottom),
            BinkSelectionHandle.TopRight => new Vector2(b.Left, b.Bottom),
            BinkSelectionHandle.Right => new Vector2(b.Left, b.Top),
            BinkSelectionHandle.BottomRight => new Vector2(b.Left, b.Top),
            BinkSelectionHandle.Bottom => new Vector2(b.Left, b.Top),
            BinkSelectionHandle.BottomLeft => new Vector2(b.Right, b.Top),
            BinkSelectionHandle.Left => new Vector2(b.Right, b.Top),
            _ => b.Center,
        };
        return Matrix3x2.CreateTranslation(-anchor) * Matrix3x2.CreateScale(sx, sy) * Matrix3x2.CreateTranslation(anchor);
    }

    private Matrix3x2 BuildRotate()
    {
        Vector2 center = _startBounds.Center;
        float a0 = MathF.Atan2(_startDocument.Y - center.Y, _startDocument.X - center.X);
        float a1 = MathF.Atan2(_currentDocument.Y - center.Y, _currentDocument.X - center.X);
        return Matrix3x2.CreateRotation(a1 - a0, center);
    }

    private void ApplyTransform(IBinkToolContext context, Matrix3x2 matrix)
    {
        _pending.Clear();
        foreach (BinkElement e in context.Selection.Elements)
        {
            if (!_startTransforms.TryGetValue(e.Id, out Matrix3x2 original)) continue;
            _pending.Add(e with { Transform = original * matrix });
        }
        if (_pending.Count == 0) return;

        string label = _drag == DragKind.Rotate ? "Rotate" : _drag == DragKind.Resize ? "Resize" : "Move";
        try
        {
            context.Document.ReplaceElements(_pending, label);
        }
        catch (InvalidOperationException ex)
        {
            // Dragging an element past the document's coordinate limit is refused. Reporting it and carrying on
            // is right; letting it escape would unwind through the browser callback and strand the gesture.
            context.Announce("That move was refused: " + ex.Message);
            return;
        }
        context.InvalidateDocument();
        context.InvalidateOverlay();
    }

    /// <summary>Returns the handle at a document point, or <see cref="BinkSelectionHandle.None"/>.</summary>
    public BinkSelectionHandle HitHandle(IBinkToolContext context, Vector2 documentPoint) => HitHandle(context, documentPoint, 1f);

    /// <summary>Returns the handle at a document point, with hit areas grown by a factor.</summary>
    /// <param name="context">The tool context.</param>
    /// <param name="documentPoint">The point to test, in document coordinates.</param>
    /// <param name="targetScale">Multiplier for the hit radius; greater than one for touch.</param>
    public BinkSelectionHandle HitHandle(IBinkToolContext context, Vector2 documentPoint, float targetScale)
    {
        ArgumentNullException.ThrowIfNull(context);
        BinkRect bounds = context.Selection.Bounds;
        if (bounds.IsEmpty) return BinkSelectionHandle.None;
        float zoom = context.Viewport.Zoom;
        float radius = (HandleSize / 2f + 3f) * MathF.Max(targetScale, 1f) / zoom;
        Span<(BinkSelectionHandle Handle, Vector2 Position)> handles = stackalloc (BinkSelectionHandle, Vector2)[9];
        int count = GetHandlePositions(bounds, RotateHandleDistance / zoom, handles);
        for (int i = 0; i < count; i++)
        {
            if (Vector2.DistanceSquared(documentPoint, handles[i].Position) <= radius * radius) return handles[i].Handle;
        }
        return BinkSelectionHandle.None;
    }

    /// <summary>Fills <paramref name="handles"/> with the handle positions for a selection rectangle. Returns the count (9).</summary>
    public static int GetHandlePositions(BinkRect bounds, float rotateDistance, Span<(BinkSelectionHandle Handle, Vector2 Position)> handles)
    {
        float l = bounds.Left, t = bounds.Top, r = bounds.Right, b = bounds.Bottom;
        float cx = bounds.Center.X, cy = bounds.Center.Y;
        handles[0] = (BinkSelectionHandle.TopLeft, new Vector2(l, t));
        handles[1] = (BinkSelectionHandle.Top, new Vector2(cx, t));
        handles[2] = (BinkSelectionHandle.TopRight, new Vector2(r, t));
        handles[3] = (BinkSelectionHandle.Right, new Vector2(r, cy));
        handles[4] = (BinkSelectionHandle.BottomRight, new Vector2(r, b));
        handles[5] = (BinkSelectionHandle.Bottom, new Vector2(cx, b));
        handles[6] = (BinkSelectionHandle.BottomLeft, new Vector2(l, b));
        handles[7] = (BinkSelectionHandle.Left, new Vector2(l, cy));
        handles[8] = (BinkSelectionHandle.Rotate, new Vector2(cx, t - rotateDistance));
        return 9;
    }

    /// <inheritdoc />
    public override void DrawOverlay(IBinkToolContext context, IBinkRenderContext render)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(render);
        float zoom = MathF.Max(context.Viewport.Zoom, 0.001f);
        var accent = new BinkColor(37, 99, 235);

        if (_drag == DragKind.Marquee && _movedPastThreshold)
        {
            BinkRect rect = BinkRect.FromPoints(_startDocument, _currentDocument);
            BinkPathGeometry path = new BinkPathBuilder(6).AddRect(rect).ToGeometry();
            render.FillPath(path, new BinkFillPaint(accent.WithAlpha(0.12f)));
            render.StrokePath(path, new BinkStrokePaint(accent, 1f / zoom, 1f, BinkBlendMode.Normal, BinkLineCap.Butt, BinkLineJoin.Miter, [4f / zoom, 3f / zoom]));
            return;
        }

        BinkRect bounds = context.Selection.Bounds;
        if (bounds.IsEmpty) return;

        // Outline each selected element so multi-selections are readable, then the overall box and handles.
        if (context.Selection.Count > 1)
        {
            foreach (BinkElement e in context.Selection.Elements)
            {
                render.StrokePath(new BinkPathBuilder(6).AddRect(e.Bounds).ToGeometry(), new BinkStrokePaint(accent.WithAlpha(0.4f), 1f / zoom, 1f, BinkBlendMode.Normal, BinkLineCap.Butt, BinkLineJoin.Miter));
            }
        }

        render.StrokePath(new BinkPathBuilder(6).AddRect(bounds).ToGeometry(), new BinkStrokePaint(accent, 1.5f / zoom, 1f, BinkBlendMode.Normal, BinkLineCap.Butt, BinkLineJoin.Miter));

        float handleSize = HandleSize / zoom;
        float rotateDistance = RotateHandleDistance / zoom;
        Span<(BinkSelectionHandle Handle, Vector2 Position)> handles = stackalloc (BinkSelectionHandle, Vector2)[9];
        int count = GetHandlePositions(bounds, rotateDistance, handles);

        render.StrokePath(new BinkPathBuilder(4).MoveTo(bounds.Center.X, bounds.Top).LineTo(handles[8].Position).ToGeometry(), new BinkStrokePaint(accent, 1f / zoom));

        var builder = new BinkPathBuilder(count * 6);
        for (int i = 0; i < count; i++)
        {
            if (handles[i].Handle == BinkSelectionHandle.Rotate) builder.AddCircle(handles[i].Position, handleSize / 2f);
            else builder.AddRect(BinkRect.FromCenter(handles[i].Position, handleSize, handleSize));
        }
        BinkPathGeometry handlePath = builder.ToGeometry();
        render.FillPath(handlePath, new BinkFillPaint(BinkColor.White));
        render.StrokePath(handlePath, new BinkStrokePaint(accent, 1.5f / zoom, 1f, BinkBlendMode.Normal, BinkLineCap.Butt, BinkLineJoin.Miter));
    }

    /// <inheritdoc />
    public override bool OnKeyDown(IBinkToolContext context, BinkToolKeyEvent key)
    {
        ArgumentNullException.ThrowIfNull(context);
        BinkSelection selection = context.Selection;
        switch (key.Key)
        {
            case "Escape":
                if (_drag != DragKind.None) { Cancel(context); return true; }
                if (!selection.IsEmpty) { selection.Clear(); return true; }
                return false;
            case "Delete":
            case "Backspace":
            {
                if (selection.IsEmpty) return false;
                int n = selection.Count;
                using (context.History.BeginGroup("Delete"))
                {
                    context.Document.RemoveElements(selection.Ids);
                }
                context.Announce($"Deleted {n} elements.");
                return true;
            }
            case "Enter":
                // Entering the canvas's own navigation mode. Until something is selected, Tab still moves browser
                // focus out of the canvas, so this is a way in rather than a keyboard trap.
                return SelectByOffset(context, 0);
            case "Tab":
                if (selection.IsEmpty) return false;
                return SelectByOffset(context, key.Shift ? -1 : 1);
            case "ArrowLeft":
            case "ArrowRight":
            case "ArrowUp":
            case "ArrowDown":
            {
                if (selection.IsEmpty) return false;
                float step = key.Shift ? 10f : 1f;
                if (key.Ctrl) return ResizeSelection(context, key.Key, step);
                Vector2 delta = key.Key switch
                {
                    "ArrowLeft" => new Vector2(-step, 0),
                    "ArrowRight" => new Vector2(step, 0),
                    "ArrowUp" => new Vector2(0, -step),
                    _ => new Vector2(0, step),
                };
                var moved = selection.Elements.Select(e => e.Translate(delta)).ToList();
                if (!TryReplace(context, moved, "Move")) return true;
                context.Announce($"Moved {moved.Count} elements.");
                return true;
            }
            case "r":
            case "R":
                if (selection.IsEmpty) return false;
                return RotateSelection(context, key.Shift ? -KeyboardRotationStep : KeyboardRotationStep);
            case "a" when key.Ctrl:
                selection.SelectAll();
                context.Announce($"{selection.Count} elements selected.");
                return true;
            default:
                return false;
        }
    }

    /// <summary>Rotation applied by one press of the rotate key, in radians.</summary>
    private const float KeyboardRotationStep = MathF.PI / 12f;

    /// <summary>
    /// Moves the selection to another element in document order. Offset 0 selects the first element, which is how
    /// a keyboard user gets into the canvas without a pointer.
    /// </summary>
    private static bool SelectByOffset(IBinkToolContext context, int offset)
    {
        List<BinkElement> candidates = Selectable(context.Document);
        if (candidates.Count == 0)
        {
            context.Announce("Nothing here to select.");
            return true;
        }

        int index = 0;
        if (offset != 0 && context.Selection.Count > 0)
        {
            Guid current = context.Selection.Ids[^1];
            int found = candidates.FindIndex(e => e.Id == current);
            // Modulo of a negative index is negative in C#, so the count is added back before wrapping.
            index = found < 0 ? 0 : ((found + offset) % candidates.Count + candidates.Count) % candidates.Count;
        }

        BinkElement target = candidates[index];
        context.Selection.Set(target.Id);
        context.Announce($"{index + 1} of {candidates.Count}: {target.GetAccessibleDescription()}");
        context.InvalidateOverlay();
        return true;
    }

    private static List<BinkElement> Selectable(BinkDocument document)
    {
        var result = new List<BinkElement>();
        foreach (BinkLayer layer in document.Layers)
        {
            if (!layer.IsVisible || layer.IsLocked) continue;
            foreach (BinkElement element in layer.Elements)
            {
                if (element.IsSelectable) result.Add(element);
            }
        }
        return result;
    }

    private static bool ResizeSelection(IBinkToolContext context, string key, float step)
    {
        BinkRect bounds = context.Selection.Bounds;
        if (bounds.IsUnset || bounds.Width <= 0 || bounds.Height <= 0) return true;

        // The anchor is the top-left corner, so the selection grows and shrinks the way dragging the opposite
        // handle would, and repeated presses do not drift.
        float sx = key is "ArrowLeft" or "ArrowRight" ? (bounds.Width + (key == "ArrowRight" ? step : -step)) / bounds.Width : 1f;
        float sy = key is "ArrowUp" or "ArrowDown" ? (bounds.Height + (key == "ArrowDown" ? step : -step)) / bounds.Height : 1f;
        if (sx < 0.01f || sy < 0.01f) return true;

        Vector2 anchor = bounds.TopLeft;
        Matrix3x2 matrix = Matrix3x2.CreateTranslation(-anchor) * Matrix3x2.CreateScale(sx, sy) * Matrix3x2.CreateTranslation(anchor);
        var scaled = context.Selection.Elements.Select(e => e.TransformBy(matrix)).ToList();
        if (!TryReplace(context, scaled, "Resize")) return true;
        BinkRect after = context.Selection.Bounds;
        context.Announce(FormattableString.Invariant($"Resized to {after.Width:0} by {after.Height:0}."));
        return true;
    }

    private static bool RotateSelection(IBinkToolContext context, float radians)
    {
        BinkRect bounds = context.Selection.Bounds;
        if (bounds.IsUnset) return true;
        Matrix3x2 matrix = Matrix3x2.CreateRotation(radians, bounds.Center);
        var rotated = context.Selection.Elements.Select(e => e.TransformBy(matrix)).ToList();
        if (!TryReplace(context, rotated, "Rotate")) return true;
        context.Announce(FormattableString.Invariant($"Rotated {radians * 180f / MathF.PI:0} degrees."));
        return true;
    }

    private static bool TryReplace(IBinkToolContext context, List<BinkElement> elements, string label)
    {
        if (elements.Count == 0) return false;
        // A keyboard edit is grouped exactly like a drag. Anything an application applies in response, such as
        // re-routing a connector attached to the element that moved, then lands in the same undo step.
        using IDisposable group = context.History.BeginGroup(label);
        try
        {
            context.Document.ReplaceElements(elements, label);
        }
        catch (InvalidOperationException ex)
        {
            context.Announce($"That {label.ToLowerInvariant()} was refused: " + ex.Message);
            return false;
        }
        context.InvalidateDocument();
        context.InvalidateOverlay();
        return true;
    }
}
