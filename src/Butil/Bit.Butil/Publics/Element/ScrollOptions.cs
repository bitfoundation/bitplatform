namespace Bit.Butil;

/// <summary>
/// The options bag for a scroll to an absolute or relative offset.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/scroll">Element.scroll()</see>
/// </summary>
public class ScrollOptions
{
    /// <summary>
    /// Whether the scroll animates. <c>null</c> is the same as <see cref="ScrollBehavior.Auto"/>.
    /// </summary>
    public ScrollBehavior? Behavior { get; set; }

    /// <summary>The vertical offset in CSS pixels. <c>null</c> leaves the current offset alone.</summary>
    public double? Top { get; set; }

    /// <summary>The horizontal offset in CSS pixels. <c>null</c> leaves the current offset alone.</summary>
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
