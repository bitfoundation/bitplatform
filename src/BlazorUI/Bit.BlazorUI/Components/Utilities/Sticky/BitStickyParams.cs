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
    /// Gets or sets the horizontal offset the element pins at from the left edge.
    /// </summary>
    public string? Left { get; set; }

    /// <summary>
    /// Gets or sets the edge of the scrolling container the element pins to.
    /// </summary>
    public BitSide? Position { get; set; }

    /// <summary>
    /// Gets or sets the horizontal offset the element pins at from the right edge.
    /// </summary>
    public string? Right { get; set; }

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

        if (Bottom.HasValue() && bitSticky.HasNotBeenSet(nameof(Bottom)))
        {
            bitSticky.Bottom = Bottom;

            bitSticky.ClassBuilder.Reset();
            bitSticky.StyleBuilder.Reset();
        }

        if (Element.HasValue() && bitSticky.HasNotBeenSet(nameof(Element)))
        {
            bitSticky.Element = Element;
        }

        if (Left.HasValue() && bitSticky.HasNotBeenSet(nameof(Left)))
        {
            bitSticky.Left = Left;

            bitSticky.ClassBuilder.Reset();
            bitSticky.StyleBuilder.Reset();
        }

        if (Position.HasValue && bitSticky.HasNotBeenSet(nameof(Position)))
        {
            bitSticky.Position = Position.Value;

            bitSticky.ClassBuilder.Reset();
        }

        if (Right.HasValue() && bitSticky.HasNotBeenSet(nameof(Right)))
        {
            bitSticky.Right = Right;

            bitSticky.ClassBuilder.Reset();
            bitSticky.StyleBuilder.Reset();
        }

        if (StuckClass.HasValue() && bitSticky.HasNotBeenSet(nameof(StuckClass)))
        {
            bitSticky.StuckClass = StuckClass;

            bitSticky.ClassBuilder.Reset();
        }

        if (StuckStyle.HasValue() && bitSticky.HasNotBeenSet(nameof(StuckStyle)))
        {
            bitSticky.StuckStyle = StuckStyle;
        }

        if (Top.HasValue() && bitSticky.HasNotBeenSet(nameof(Top)))
        {
            bitSticky.Top = Top;

            bitSticky.ClassBuilder.Reset();
            bitSticky.StyleBuilder.Reset();
        }

        if (ZIndex.HasValue && bitSticky.HasNotBeenSet(nameof(ZIndex)))
        {
            bitSticky.ZIndex = ZIndex.Value;

            bitSticky.StyleBuilder.Reset();
        }
    }
}
