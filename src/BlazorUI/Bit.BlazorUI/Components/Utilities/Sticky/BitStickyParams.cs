namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitSticky"/> component.
/// </summary>
/// <remarks>
/// What belongs here is what every sticky of a page or of an app agrees on - which edge they pin to,
/// how far from it, how they look while pinned, what they pass over. The content and the callbacks
/// are deliberately not here: they are what makes one sticky the one it is, and cascading them would
/// give every sticky on the page the same content and the same observer.
/// </remarks>
public class BitStickyParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitSticky"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitSticky value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitSticky)}";



    public string Name => ParamName;



    /// <summary>
    /// Gets or sets the vertical offset the element pins at from the bottom edge.
    /// </summary>
    public string? Bottom { get; set; }

    /// <summary>
    /// Gets or sets the custom html element used for the root node.
    /// </summary>
    public string? Element { get; set; }

    /// <summary>
    /// Gets or sets whether the element takes a surface and a theme shadow while it is stuck.
    /// </summary>
    public bool? ElevateOnStuck { get; set; }

    /// <summary>
    /// Gets or sets the horizontal offset the element pins at from the left edge.
    /// </summary>
    public string? Left { get; set; }

    /// <summary>
    /// Gets or sets the edge of the scrolling container the element pins to.
    /// </summary>
    public BitPlacement? Placement { get; set; }

    /// <summary>
    /// Gets or sets the horizontal offset the element pins at from the right edge.
    /// </summary>
    public string? Right { get; set; }

    /// <summary>
    /// Gets or sets whether the element reserves the room it covers as the scroll padding of its scrolling container.
    /// </summary>
    public bool? ScrollPadding { get; set; }

    /// <summary>
    /// Gets or sets the CSS class applied to the root element only while the component is stuck.
    /// </summary>
    public string? StuckClass { get; set; }

    /// <summary>
    /// Gets or sets the CSS style applied to the root element only while the component is stuck.
    /// </summary>
    public string? StuckStyle { get; set; }

    /// <summary>
    /// Gets or sets the vertical offset the element pins at from the top edge.
    /// </summary>
    public string? Top { get; set; }

    /// <summary>
    /// Gets or sets the z-index of the root element.
    /// </summary>
    public int? ZIndex { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitSticky"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitSticky"/> itself.
    /// </summary>
    /// <param name="bitSticky">
    /// The <see cref="BitSticky"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitSticky bitSticky)
    {
        if (bitSticky is null) return;

        UpdateBaseParameters(bitSticky);

        if (Bottom.HasValue())
        {
            bitSticky.TakeFromCascade(nameof(Bottom), Bottom, static s => s.Bottom, static (s, v) => s.Bottom = v);
        }

        if (Element.HasValue())
        {
            bitSticky.TakeFromCascade(nameof(Element), Element, static s => s.Element, static (s, v) => s.Element = v);
        }

        if (ElevateOnStuck.HasValue)
        {
            bitSticky.TakeFromCascade(nameof(ElevateOnStuck), ElevateOnStuck.Value, static s => s.ElevateOnStuck, static (s, v) => s.ElevateOnStuck = v);
        }

        if (Left.HasValue())
        {
            bitSticky.TakeFromCascade(nameof(Left), Left, static s => s.Left, static (s, v) => s.Left = v);
        }

        if (Placement.HasValue)
        {
            bitSticky.TakeFromCascade(nameof(Placement), Placement.Value, static s => s.Placement, static (s, v) => s.Placement = v);
        }

        if (Right.HasValue())
        {
            bitSticky.TakeFromCascade(nameof(Right), Right, static s => s.Right, static (s, v) => s.Right = v);
        }

        if (ScrollPadding.HasValue)
        {
            bitSticky.TakeFromCascade(nameof(ScrollPadding), ScrollPadding.Value, static s => s.ScrollPadding, static (s, v) => s.ScrollPadding = v);
        }

        if (StuckClass.HasValue())
        {
            bitSticky.TakeFromCascade(nameof(StuckClass), StuckClass, static s => s.StuckClass, static (s, v) => s.StuckClass = v);
        }

        if (StuckStyle.HasValue())
        {
            bitSticky.TakeFromCascade(nameof(StuckStyle), StuckStyle, static s => s.StuckStyle, static (s, v) => s.StuckStyle = v);
        }

        if (Top.HasValue())
        {
            bitSticky.TakeFromCascade(nameof(Top), Top, static s => s.Top, static (s, v) => s.Top = v);
        }

        if (ZIndex.HasValue)
        {
            bitSticky.TakeFromCascade(nameof(ZIndex), ZIndex.Value, static s => s.ZIndex, static (s, v) => s.ZIndex = v);
        }
    }
}
