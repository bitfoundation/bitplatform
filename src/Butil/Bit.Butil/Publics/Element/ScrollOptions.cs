namespace Bit.Butil;

public class ScrollOptions
{
    public ScrollBehavior? Behavior { get; set; }

    public double? Top { get; set; }

    public double? Left { get; set; }

    internal ScrollJsOptions ToJsObject()
    {
        var behavior = Behavior switch
        {
            ScrollBehavior.Instant => "instant",
            ScrollBehavior.Smooth => "smooth",
            _ => "auto",
        };

        return new()
        {
            Behavior = behavior,
            Top = Top,
            Left = Left
        };
    }
}
