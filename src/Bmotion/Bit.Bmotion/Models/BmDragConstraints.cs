namespace Bit.Bmotion;

/// <summary>
/// Bounds for a drag gesture. Two forms:
/// <list type="bullet">
///   <item><description><b>Pixel bounds</b> relative to the element's resting position -
///   <see cref="Horizontal"/>, <see cref="Vertical"/>, <see cref="Box"/> or the
///   <see cref="Left"/>/<see cref="Right"/>/<see cref="Top"/>/<see cref="Bottom"/> properties.</description></item>
///   <item><description><b>Element bounds</b> - <see cref="Parent"/> keeps the element inside its
///   parent, <see cref="Within"/> inside any element matched by a CSS selector (motion.dev's
///   <c>dragConstraints={ref}</c>). The container is measured at each drag start, so layout
///   changes between drags are picked up automatically.</description></item>
/// </list>
/// </summary>
public class BmDragConstraints
{
    public double? Left { get; set; }
    public double? Right { get; set; }
    public double? Top { get; set; }
    public double? Bottom { get; set; }

    /// <summary>CSS selector of the container element to constrain within (element-bounds mode).</summary>
    public string? Selector { get; private set; }

    /// <summary>When true, constrains within the dragged element's direct parent (element-bounds mode).</summary>
    public bool FromParent { get; private set; }

    /// <summary>Constrains the drag inside the element's direct parent.</summary>
    public static BmDragConstraints Parent() => new() { FromParent = true };

    /// <summary>Constrains the drag inside the first element matching <paramref name="selector"/>.</summary>
    public static BmDragConstraints Within(string selector)
    {
        if (string.IsNullOrWhiteSpace(selector))
            throw new ArgumentException("Selector must not be null or whitespace.", nameof(selector));
        return new() { Selector = selector };
    }

    public static BmDragConstraints Horizontal(double left, double right)
    {
        ValidateHorizontal(left, right);
        return new() { Left = left, Right = right };
    }

    public static BmDragConstraints Vertical(double top, double bottom)
    {
        ValidateVertical(top, bottom);
        return new() { Top = top, Bottom = bottom };
    }

    public static BmDragConstraints Box(double left, double right, double top, double bottom)
    {
        ValidateHorizontal(left, right);
        ValidateVertical(top, bottom);
        return new() { Left = left, Right = right, Top = top, Bottom = bottom };
    }

    private static void ValidateHorizontal(double left, double right)
    {
        // Inverted bounds (left > right) describe an empty range the drag layer can't satisfy,
        // so reject them up front instead of silently producing constraints that never apply.
        if (left > right)
            throw new ArgumentException(
                $"Drag constraint 'left' ({left}) must be less than or equal to 'right' ({right}).");
    }

    private static void ValidateVertical(double top, double bottom)
    {
        if (top > bottom)
            throw new ArgumentException(
                $"Drag constraint 'top' ({top}) must be less than or equal to 'bottom' ({bottom}).");
    }

    internal object ToJsObject()
    {
        var d = new Dictionary<string, object?>();
        if (Left.HasValue)   d["left"]   = Left.Value;
        if (Right.HasValue)  d["right"]  = Right.Value;
        if (Top.HasValue)    d["top"]    = Top.Value;
        if (Bottom.HasValue) d["bottom"] = Bottom.Value;
        if (Selector is not null) d["selector"] = Selector;
        if (FromParent)      d["parent"] = true;
        return d;
    }
}
