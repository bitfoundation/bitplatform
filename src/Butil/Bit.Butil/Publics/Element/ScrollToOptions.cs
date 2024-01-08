namespace Bit.Butil;

public class ScrollToOptions
{
    public ScrollBehavior? Behavior { get; set; }

    public double? Top { get; set; }

    public double? Left { get; set; }

    public object ToJsObject()
    {
        var behavior = Behavior switch
        {
            ScrollBehavior.Instant => "instant",
            ScrollBehavior.Smooth => "smooth",
            _ => "auto",
        };

        return new
        {
            Behavior = behavior,
            Top,
            Left
        };
    }
}
