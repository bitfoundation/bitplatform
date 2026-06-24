namespace Bit.Bmotion;

public class BmotionDragConstraints
{
    public double? Left { get; set; }
    public double? Right { get; set; }
    public double? Top { get; set; }
    public double? Bottom { get; set; }

    public static BmotionDragConstraints Horizontal(double left, double right)
    {
        ValidateHorizontal(left, right);
        return new() { Left = left, Right = right };
    }

    public static BmotionDragConstraints Vertical(double top, double bottom)
    {
        ValidateVertical(top, bottom);
        return new() { Top = top, Bottom = bottom };
    }

    public static BmotionDragConstraints Box(double left, double right, double top, double bottom)
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
        return d;
    }
}
