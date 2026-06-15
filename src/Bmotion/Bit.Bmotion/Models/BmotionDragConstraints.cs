namespace Bit.Bmotion;

public class BmotionDragConstraints
{
    public double? Left { get; set; }
    public double? Right { get; set; }
    public double? Top { get; set; }
    public double? Bottom { get; set; }

    public static BmotionDragConstraints Horizontal(double left, double right)
        => new() { Left = left, Right = right };

    public static BmotionDragConstraints Vertical(double top, double bottom)
        => new() { Top = top, Bottom = bottom };

    public static BmotionDragConstraints Box(double left, double right, double top, double bottom)
        => new() { Left = left, Right = right, Top = top, Bottom = bottom };

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
