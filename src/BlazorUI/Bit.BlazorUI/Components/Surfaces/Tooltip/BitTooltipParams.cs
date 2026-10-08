namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitTooltip"/> component.
/// </summary>
/// <remarks>
/// It carries what a set of tooltips shares - their look, their placement, their triggers and their timing - so a
/// toolbar or a whole page sets it once. The text, the template, the anchor and the shown state stay on the tooltip
/// itself, since each tooltip says something of its own about the control it belongs to. A delay set by a
/// <see cref="BitTooltipGroup"/> around the tooltip still wins over the one cascaded from here, the way it wins over
/// the default: the group is the nearer of the two.
/// </remarks>
public class BitTooltipParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitTooltip"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitTooltip value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitTooltip)}";



    public string Name => ParamName;



    /// <summary>
    /// Where along the placement the tooltip lines up with its anchor.
    /// </summary>
    public BitPlacement? Alignment { get; set; }

    /// <summary>
    /// The size in pixels of the arrow that points at the anchor.
    /// </summary>
    public int? ArrowSize { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the tooltip.
    /// </summary>
    public BitTooltipClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the tooltip, which colors its surface and the arrow along with it.
    /// </summary>
    public BitColor? Color { get; set; }

    /// <summary>
    /// Expands the element the tooltip wraps its anchor in to the full available width.
    /// </summary>
    public bool? FullWidth { get; set; }

    /// <summary>
    /// Hides the arrow of the tooltip.
    /// </summary>
    public bool? HideArrow { get; set; }

    /// <summary>
    /// The delay in milliseconds before hiding the tooltip.
    /// </summary>
    public int? HideDelay { get; set; }

    /// <summary>
    /// Hides the tooltip when the anchor is pressed.
    /// </summary>
    public bool? HideOnClick { get; set; }

    /// <summary>
    /// Lets the pointer travel into the tooltip and stay there without it being hidden.
    /// </summary>
    public bool? Interactive { get; set; }

    /// <summary>
    /// Holds the content of the tooltip out of the DOM until the tooltip is first shown.
    /// </summary>
    public bool? LazyRender { get; set; }

    /// <summary>
    /// The maximum width of the tooltip as a CSS value.
    /// </summary>
    public string? MaxWidth { get; set; }

    /// <summary>
    /// Removes the fade the tooltip is shown and hidden with.
    /// </summary>
    public bool? NoAnimation { get; set; }

    /// <summary>
    /// Keeps the Escape key from dismissing the tooltip.
    /// </summary>
    public bool? NoDismissOnEscape { get; set; }

    /// <summary>
    /// Keeps a touch or a pen from showing the tooltip at all.
    /// </summary>
    public bool? NoTouch { get; set; }

    /// <summary>
    /// The distance in pixels between the anchor and the tooltip.
    /// </summary>
    public int? Offset { get; set; }

    /// <summary>
    /// The side of the anchor the tooltip is placed on.
    /// </summary>
    public BitPlacement? Placement { get; set; }

    /// <summary>
    /// What the tooltip is to the anchor it belongs to: its description, its name, or nothing at all.
    /// </summary>
    public BitTooltipRelationship? Relationship { get; set; }

    /// <summary>
    /// The delay in milliseconds before showing the tooltip.
    /// </summary>
    public int? ShowDelay { get; set; }

    /// <summary>
    /// Turns the anchor into a toggle for the tooltip.
    /// </summary>
    public bool? ShowOnClick { get; set; }

    /// <summary>
    /// Shows the tooltip when the anchor takes the keyboard focus.
    /// </summary>
    public bool? ShowOnFocus { get; set; }

    /// <summary>
    /// Shows the tooltip while the pointer is over the anchor.
    /// </summary>
    public bool? ShowOnHover { get; set; }

    /// <summary>
    /// The size of the tooltip, which sets the size of its text and the padding around it.
    /// </summary>
    public BitSize? Size { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the tooltip.
    /// </summary>
    public BitTooltipClassStyles? Styles { get; set; }

    /// <summary>
    /// The time in milliseconds a tooltip shown by a touch stays before it hides itself.
    /// </summary>
    public int? TouchHideDelay { get; set; }

    /// <summary>
    /// The time in milliseconds a touch has to rest on the anchor before the tooltip is shown.
    /// </summary>
    public int? TouchShowDelay { get; set; }

    /// <summary>
    /// The stacking order of the tooltip surface and its arrow.
    /// </summary>
    public int? ZIndex { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitTooltip"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitTooltip"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitTooltip"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitTooltip"/>.
    /// </remarks>
    /// <param name="bitTooltip">
    /// The <see cref="BitTooltip"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitTooltip bitTooltip)
    {
        if (bitTooltip is null) return;

        UpdateBaseParameters(bitTooltip);

        // This runs on every render of every tooltip under the BitParams, so a value that drives the class or
        // the style of its root only resets the builder when it differs from the one it already holds: an
        // unchanged one would rebuild both strings on every render for nothing.
        if (Alignment.HasValue && bitTooltip.HasNotBeenSet(nameof(Alignment)))
        {
            bitTooltip.Alignment = Alignment.Value;
        }

        if (ArrowSize.HasValue && bitTooltip.HasNotBeenSet(nameof(ArrowSize)) && bitTooltip.ArrowSize != ArrowSize)
        {
            bitTooltip.ArrowSize = ArrowSize.Value;

            bitTooltip.StyleBuilder.Reset();
        }

        if (Classes is not null && bitTooltip.HasNotBeenSet(nameof(Classes)) && ReferenceEquals(bitTooltip.Classes, Classes) is false)
        {
            bitTooltip.Classes = Classes;

            bitTooltip.ClassBuilder.Reset();
        }

        if (Color.HasValue && bitTooltip.HasNotBeenSet(nameof(Color)) && bitTooltip.Color != Color)
        {
            bitTooltip.Color = Color.Value;

            bitTooltip.ClassBuilder.Reset();
        }

        if (FullWidth.HasValue && bitTooltip.HasNotBeenSet(nameof(FullWidth)) && bitTooltip.FullWidth != FullWidth)
        {
            bitTooltip.FullWidth = FullWidth.Value;

            bitTooltip.ClassBuilder.Reset();
        }

        if (HideArrow.HasValue && bitTooltip.HasNotBeenSet(nameof(HideArrow)))
        {
            bitTooltip.HideArrow = HideArrow.Value;
        }

        // The delays are only filled in, never marked as set: a BitTooltipGroup around the tooltip reads the
        // same "has not been set" to decide whether its own delay applies, and the group is the nearer of the two.
        if (HideDelay.HasValue && bitTooltip.HasNotBeenSet(nameof(HideDelay)))
        {
            bitTooltip.HideDelay = HideDelay.Value;
        }

        if (HideOnClick.HasValue && bitTooltip.HasNotBeenSet(nameof(HideOnClick)))
        {
            bitTooltip.HideOnClick = HideOnClick.Value;
        }

        if (Interactive.HasValue && bitTooltip.HasNotBeenSet(nameof(Interactive)) && bitTooltip.Interactive != Interactive)
        {
            bitTooltip.Interactive = Interactive.Value;

            bitTooltip.ClassBuilder.Reset();
        }

        if (LazyRender.HasValue && bitTooltip.HasNotBeenSet(nameof(LazyRender)))
        {
            bitTooltip.LazyRender = LazyRender.Value;
        }

        if (MaxWidth.HasValue() && bitTooltip.HasNotBeenSet(nameof(MaxWidth)) && bitTooltip.MaxWidth != MaxWidth)
        {
            bitTooltip.MaxWidth = MaxWidth;

            bitTooltip.StyleBuilder.Reset();
        }

        if (NoAnimation.HasValue && bitTooltip.HasNotBeenSet(nameof(NoAnimation)) && bitTooltip.NoAnimation != NoAnimation)
        {
            bitTooltip.NoAnimation = NoAnimation.Value;

            bitTooltip.ClassBuilder.Reset();
        }

        if (NoDismissOnEscape.HasValue && bitTooltip.HasNotBeenSet(nameof(NoDismissOnEscape)))
        {
            bitTooltip.NoDismissOnEscape = NoDismissOnEscape.Value;
        }

        if (NoTouch.HasValue && bitTooltip.HasNotBeenSet(nameof(NoTouch)))
        {
            bitTooltip.NoTouch = NoTouch.Value;
        }

        if (Offset.HasValue && bitTooltip.HasNotBeenSet(nameof(Offset)) && bitTooltip.Offset != Offset)
        {
            bitTooltip.Offset = Offset.Value;

            bitTooltip.StyleBuilder.Reset();
        }

        if (Placement.HasValue && bitTooltip.HasNotBeenSet(nameof(Placement)))
        {
            bitTooltip.Placement = Placement.Value;
        }

        if (Relationship.HasValue && bitTooltip.HasNotBeenSet(nameof(Relationship)))
        {
            bitTooltip.Relationship = Relationship.Value;
        }

        if (ShowDelay.HasValue && bitTooltip.HasNotBeenSet(nameof(ShowDelay)))
        {
            bitTooltip.ShowDelay = ShowDelay.Value;
        }

        if (ShowOnClick.HasValue && bitTooltip.HasNotBeenSet(nameof(ShowOnClick)))
        {
            bitTooltip.ShowOnClick = ShowOnClick.Value;
        }

        if (ShowOnFocus.HasValue && bitTooltip.HasNotBeenSet(nameof(ShowOnFocus)))
        {
            bitTooltip.ShowOnFocus = ShowOnFocus.Value;
        }

        if (ShowOnHover.HasValue && bitTooltip.HasNotBeenSet(nameof(ShowOnHover)))
        {
            bitTooltip.ShowOnHover = ShowOnHover.Value;
        }

        if (Size.HasValue && bitTooltip.HasNotBeenSet(nameof(Size)) && bitTooltip.Size != Size)
        {
            bitTooltip.Size = Size.Value;

            bitTooltip.ClassBuilder.Reset();
        }

        if (Styles is not null && bitTooltip.HasNotBeenSet(nameof(Styles)) && ReferenceEquals(bitTooltip.Styles, Styles) is false)
        {
            bitTooltip.Styles = Styles;

            bitTooltip.StyleBuilder.Reset();
        }

        if (TouchHideDelay.HasValue && bitTooltip.HasNotBeenSet(nameof(TouchHideDelay)))
        {
            bitTooltip.TouchHideDelay = TouchHideDelay.Value;
        }

        if (TouchShowDelay.HasValue && bitTooltip.HasNotBeenSet(nameof(TouchShowDelay)))
        {
            bitTooltip.TouchShowDelay = TouchShowDelay.Value;
        }

        if (ZIndex.HasValue && bitTooltip.HasNotBeenSet(nameof(ZIndex)) && bitTooltip.ZIndex != ZIndex)
        {
            bitTooltip.ZIndex = ZIndex.Value;

            bitTooltip.StyleBuilder.Reset();
        }
    }
}
