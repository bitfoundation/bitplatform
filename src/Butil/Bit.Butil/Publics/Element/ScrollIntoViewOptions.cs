namespace Bit.Butil;

public class ScrollIntoViewOptions
{
    public ScrollBehavior? Behavior { get; set; }

    public ScrollLogicalPosition? Block { get; set; }

    public ScrollLogicalPosition? Inline { get; set; }

    internal ScrollIntoViewJsOptions ToJsObject()
    {
        var behavior = Behavior switch
        {
            ScrollBehavior.Instant => "instant",
            ScrollBehavior.Smooth => "smooth",
            _ => "auto",
        };

        var block = Block switch
        {
            ScrollLogicalPosition.Center => "center",
            ScrollLogicalPosition.End => "end",
            ScrollLogicalPosition.Nearest => "nearest",
            _ => "start",
        };

        var inline = Inline switch
        {
            ScrollLogicalPosition.Center => "center",
            ScrollLogicalPosition.End => "end",
            ScrollLogicalPosition.Nearest => "nearest",
            _ => "start",
        };

        return new()
        {
            Behavior = behavior,
            Block = block,
            Inline = inline
        };
    }
}
