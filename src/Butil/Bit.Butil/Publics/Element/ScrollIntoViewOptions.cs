namespace Bit.Butil;

/// <summary>
/// The options bag for scrolling an element into view.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/scrollIntoView">Element.scrollIntoView()</see>
/// </summary>
public class ScrollIntoViewOptions
{
    /// <summary>
    /// Whether the scroll animates. <c>null</c> is the same as <see cref="ScrollBehavior.Auto"/>.
    /// </summary>
    public ScrollBehavior? Behavior { get; set; }

    /// <summary>
    /// Where the element lands along the block axis. <c>null</c> is the same as
    /// <see cref="ScrollLogicalPosition.Start"/>.
    /// </summary>
    public ScrollLogicalPosition? Block { get; set; }

    /// <summary>
    /// Where the element lands along the inline axis. <c>null</c> is the same as
    /// <see cref="ScrollLogicalPosition.Start"/>.
    /// </summary>
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
