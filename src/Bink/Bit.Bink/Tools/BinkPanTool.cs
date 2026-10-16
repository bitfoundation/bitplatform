using System.Numerics;

namespace Bit.Bink;

/// <summary>
/// Pans the viewport by dragging, and pinch-zooms with two pointers. Dragging changes only the viewport, never
/// the document, so panning and zooming are not undoable and produce no operations.
/// </summary>
public sealed class BinkPanTool : BinkToolBase
{
    private readonly Dictionary<int, Vector2> _pointers = new();
    private Vector2 _lastCentroid;
    private float _lastSpread;

    /// <inheritdoc />
    public override string Id => "pan";

    /// <inheritdoc />
    public override string DisplayName => "Pan";

    /// <inheritdoc />
    public override string Cursor => "grab";

    /// <inheritdoc />
    /// <remarks>Panning and zooming change only the viewport, so this tool works on a read-only canvas.</remarks>
    public override bool ModifiesDocument => false;

    /// <summary>Minimum zoom.</summary>
    public float MinZoom { get; set; } = 0.05f;

    /// <summary>Maximum zoom.</summary>
    public float MaxZoom { get; set; } = 64f;

    /// <summary>Whether two-pointer pinch zooming is enabled.</summary>
    public bool EnablePinchZoom { get; set; } = true;

    /// <inheritdoc />
    public override bool OnPointerDown(IBinkToolContext context, in BinkInputSample sample)
    {
        ArgumentNullException.ThrowIfNull(context);
        _pointers[sample.PointerId] = sample.Position;
        UpdateGesture();
        context.CapturePointer(sample.PointerId);
        context.SetCursor("grabbing");
        return true;
    }

    /// <inheritdoc />
    public override void OnPointerMove(IBinkToolContext context, in BinkInputSample sample)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!_pointers.ContainsKey(sample.PointerId))
        {
            // A contact can join a gesture that is already running, either because a host routed an existing
            // touch here after deciding it was a two-finger gesture, or because the down event was consumed
            // elsewhere. Adopting it silently and re-basing the gesture avoids a jump on the next move.
            _pointers[sample.PointerId] = sample.Position;
            UpdateGesture();
            return;
        }
        _pointers[sample.PointerId] = sample.Position;

        (Vector2 centroid, float spread) = GetGesture();
        BinkViewport viewport = context.Viewport;
        viewport = viewport.Pan(centroid - _lastCentroid);
        if (EnablePinchZoom && _pointers.Count >= 2 && _lastSpread > 1f && spread > 1f)
        {
            viewport = viewport.ZoomBy(spread / _lastSpread, centroid, MinZoom, MaxZoom);
        }
        context.Viewport = viewport;
        _lastCentroid = centroid;
        _lastSpread = spread;
        context.InvalidateDocument();
        context.InvalidateOverlay();
    }

    /// <inheritdoc />
    public override void OnPointerUp(IBinkToolContext context, in BinkInputSample sample)
    {
        // Apply the release position before letting go, so a quick flick does not lose its last movement.
        OnPointerMove(context, sample);
        Release(context, sample.PointerId);
    }

    /// <inheritdoc />
    public override void OnPointerCancel(IBinkToolContext context, in BinkInputSample sample) => Release(context, sample.PointerId);

    private void Release(IBinkToolContext context, int pointerId)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!_pointers.Remove(pointerId)) return;
        context.ReleasePointer(pointerId);
        UpdateGesture();
        if (_pointers.Count == 0) context.SetCursor(Cursor);
    }

    /// <inheritdoc />
    public override void Deactivate(IBinkToolContext context)
    {
        _pointers.Clear();
        _lastSpread = 0;
    }

    private void UpdateGesture()
    {
        (Vector2 centroid, float spread) = GetGesture();
        _lastCentroid = centroid;
        _lastSpread = spread;
    }

    private (Vector2 Centroid, float Spread) GetGesture()
    {
        if (_pointers.Count == 0) return (Vector2.Zero, 0f);
        Vector2 sum = Vector2.Zero;
        foreach (Vector2 p in _pointers.Values) sum += p;
        Vector2 centroid = sum / _pointers.Count;
        if (_pointers.Count < 2) return (centroid, 0f);
        float spread = 0f;
        foreach (Vector2 p in _pointers.Values) spread += Vector2.Distance(p, centroid);
        return (centroid, spread / _pointers.Count);
    }
}
