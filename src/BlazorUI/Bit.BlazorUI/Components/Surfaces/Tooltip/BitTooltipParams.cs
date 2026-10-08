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

        if (Alignment.HasValue)
        {
            bitTooltip.TakeFromCascade(nameof(Alignment), Alignment.Value, static t => t.Alignment, static (t, v) => t.Alignment = v);
        }

        if (ArrowSize.HasValue)
        {
            bitTooltip.TakeFromCascade(nameof(ArrowSize), ArrowSize.Value, static t => t.ArrowSize, static (t, v) => t.ArrowSize = v);
        }

        if (Classes is not null)
        {
            bitTooltip.TakeFromCascade(nameof(Classes), Classes, static t => t.Classes, static (t, v) => t.Classes = v);
        }

        if (Color.HasValue)
        {
            bitTooltip.TakeFromCascade(nameof(Color), Color.Value, static t => t.Color, static (t, v) => t.Color = v);
        }

        if (FullWidth.HasValue)
        {
            bitTooltip.TakeFromCascade(nameof(FullWidth), FullWidth.Value, static t => t.FullWidth, static (t, v) => t.FullWidth = v);
        }

        if (HideArrow.HasValue)
        {
            bitTooltip.TakeFromCascade(nameof(HideArrow), HideArrow.Value, static t => t.HideArrow, static (t, v) => t.HideArrow = v);
        }

        // The delays are only filled in, never marked as set: a BitTooltipGroup around the tooltip reads the
        // same "has not been set" to decide whether its own delay applies, and the group is the nearer of the two.
        if (HideDelay.HasValue)
        {
            bitTooltip.TakeFromCascade(nameof(HideDelay), HideDelay.Value, static t => t.HideDelay, static (t, v) => t.HideDelay = v);
        }

        if (HideOnClick.HasValue)
        {
            bitTooltip.TakeFromCascade(nameof(HideOnClick), HideOnClick.Value, static t => t.HideOnClick, static (t, v) => t.HideOnClick = v);
        }

        if (Interactive.HasValue)
        {
            bitTooltip.TakeFromCascade(nameof(Interactive), Interactive.Value, static t => t.Interactive, static (t, v) => t.Interactive = v);
        }

        if (LazyRender.HasValue)
        {
            bitTooltip.TakeFromCascade(nameof(LazyRender), LazyRender.Value, static t => t.LazyRender, static (t, v) => t.LazyRender = v);
        }

        if (MaxWidth.HasValue())
        {
            bitTooltip.TakeFromCascade(nameof(MaxWidth), MaxWidth, static t => t.MaxWidth, static (t, v) => t.MaxWidth = v);
        }

        if (NoAnimation.HasValue)
        {
            bitTooltip.TakeFromCascade(nameof(NoAnimation), NoAnimation.Value, static t => t.NoAnimation, static (t, v) => t.NoAnimation = v);
        }

        if (NoDismissOnEscape.HasValue)
        {
            bitTooltip.TakeFromCascade(nameof(NoDismissOnEscape), NoDismissOnEscape.Value, static t => t.NoDismissOnEscape, static (t, v) => t.NoDismissOnEscape = v);
        }

        if (NoTouch.HasValue)
        {
            bitTooltip.TakeFromCascade(nameof(NoTouch), NoTouch.Value, static t => t.NoTouch, static (t, v) => t.NoTouch = v);
        }

        if (Offset.HasValue)
        {
            bitTooltip.TakeFromCascade(nameof(Offset), Offset.Value, static t => t.Offset, static (t, v) => t.Offset = v);
        }

        if (Placement.HasValue)
        {
            bitTooltip.TakeFromCascade(nameof(Placement), Placement.Value, static t => t.Placement, static (t, v) => t.Placement = v);
        }

        if (Relationship.HasValue)
        {
            bitTooltip.TakeFromCascade(nameof(Relationship), Relationship.Value, static t => t.Relationship, static (t, v) => t.Relationship = v);
        }

        if (ShowDelay.HasValue)
        {
            bitTooltip.TakeFromCascade(nameof(ShowDelay), ShowDelay.Value, static t => t.ShowDelay, static (t, v) => t.ShowDelay = v);
        }

        if (ShowOnClick.HasValue)
        {
            bitTooltip.TakeFromCascade(nameof(ShowOnClick), ShowOnClick.Value, static t => t.ShowOnClick, static (t, v) => t.ShowOnClick = v);
        }

        if (ShowOnFocus.HasValue)
        {
            bitTooltip.TakeFromCascade(nameof(ShowOnFocus), ShowOnFocus.Value, static t => t.ShowOnFocus, static (t, v) => t.ShowOnFocus = v);
        }

        if (ShowOnHover.HasValue)
        {
            bitTooltip.TakeFromCascade(nameof(ShowOnHover), ShowOnHover.Value, static t => t.ShowOnHover, static (t, v) => t.ShowOnHover = v);
        }

        if (Size.HasValue)
        {
            bitTooltip.TakeFromCascade(nameof(Size), Size.Value, static t => t.Size, static (t, v) => t.Size = v);
        }

        if (Styles is not null)
        {
            bitTooltip.TakeFromCascade(nameof(Styles), Styles, static t => t.Styles, static (t, v) => t.Styles = v);
        }

        if (TouchHideDelay.HasValue)
        {
            bitTooltip.TakeFromCascade(nameof(TouchHideDelay), TouchHideDelay.Value, static t => t.TouchHideDelay, static (t, v) => t.TouchHideDelay = v);
        }

        if (TouchShowDelay.HasValue)
        {
            bitTooltip.TakeFromCascade(nameof(TouchShowDelay), TouchShowDelay.Value, static t => t.TouchShowDelay, static (t, v) => t.TouchShowDelay = v);
        }

        if (ZIndex.HasValue)
        {
            bitTooltip.TakeFromCascade(nameof(ZIndex), ZIndex.Value, static t => t.ZIndex, static (t, v) => t.ZIndex = v);
        }
    }
}
