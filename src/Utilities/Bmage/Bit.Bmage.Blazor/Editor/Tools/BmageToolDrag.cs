using System.Drawing;
using System.Numerics;

namespace Bit.Bmage;

/// <summary>The state of an in-progress drag on the canvas, in image coordinates.</summary>
public sealed class BmageToolDrag
{
    /// <summary>Where the drag began. Pan gestures rebase this on every move so the delta stays incremental.</summary>
    public required Vector2 Start { get; set; }

    /// <summary>The latest pointer position.</summary>
    public Vector2 Current { get; set; }
    /// <summary>The sampled path, for freehand strokes.</summary>
    public List<Vector2> Points { get; } = [];

    /// <summary>The pen pressure at each of <see cref="Points"/>, when a pen drew them; empty otherwise.</summary>
    public List<float> Pressures { get; } = [];

    /// <summary>True when shift is held, which constrains shapes to squares and angles to 15 degree steps.</summary>
    public bool ShiftKey { get; set; }

    /// <summary>True when alt is held.</summary>
    public bool AltKey { get; set; }

    /// <summary>The rectangle between the start and current points, normalised so width and height are positive.</summary>
    public RectangleF Rectangle
    {
        get
        {
            var x = MathF.Min(Start.X, Current.X);
            var y = MathF.Min(Start.Y, Current.Y);
            var w = MathF.Abs(Current.X - Start.X);
            var h = MathF.Abs(Current.Y - Start.Y);
            if (ShiftKey)
            {
                // Constrain to a square, keeping the corner under the pointer.
                var size = MathF.Max(w, h);
                if (Current.X < Start.X) x = Start.X - size;
                if (Current.Y < Start.Y) y = Start.Y - size;
                w = h = size;
            }
            return new RectangleF(x, y, w, h);
        }
    }

    /// <summary>The end point, snapped to 15° increments when shift is held.</summary>
    public Vector2 ConstrainedEnd
    {
        get
        {
            if (!ShiftKey) return Current;
            var delta = Current - Start;
            if (delta.LengthSquared() < 1e-6f) return Current;
            var angle = MathF.Atan2(delta.Y, delta.X);
            var step = MathF.PI / 12;
            var snapped = MathF.Round(angle / step) * step;
            var length = delta.Length();
            return Start + new Vector2(MathF.Cos(snapped), MathF.Sin(snapped)) * length;
        }
    }
}
