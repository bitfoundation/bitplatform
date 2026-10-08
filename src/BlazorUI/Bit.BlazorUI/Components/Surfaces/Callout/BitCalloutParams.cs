namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitCallout"/> component.
/// </summary>
/// <remarks>
/// The open state (IsOpen and DefaultIsOpen), the anchor (Anchor, AnchorEl and AnchorId), the content and its parts
/// (ChildContent, Content, Header, Footer and the ids that wire them up by hand), the ARIA role and the ids it is
/// named and described by, and the event callbacks are left out on purpose: they belong to a single callout rather
/// than to a group of them.
/// </remarks>
public class BitCalloutParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitCallout"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitCallout value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitCallout)}";



    public string Name => ParamName;



    /// <summary>
    /// How the callout is lined up with its anchor along the axis it is not placed on.
    /// </summary>
    public BitPlacement? Alignment { get; set; }

    /// <summary>
    /// The distance in pixels the callout is slid along the axis it is aligned on, inwards from the edge of the anchor
    /// the Alignment lined it up with.
    /// </summary>
    public int? AlignmentOffset { get; set; }

    /// <summary>
    /// The distance in pixels the arrow is kept away from the corners of the callout.
    /// </summary>
    public int? ArrowPadding { get; set; }

    /// <summary>
    /// The size in pixels of the arrow drawn by ShowArrow.
    /// </summary>
    public int? ArrowSize { get; set; }

    /// <summary>
    /// Closes the callout as soon as a click lands anywhere inside it.
    /// </summary>
    public bool? AutoClose { get; set; }

    /// <summary>
    /// Moves the focus into the callout as soon as it opens.
    /// </summary>
    public bool? AutoFocus { get; set; }

    /// <summary>
    /// The color kind of the background of the callout.
    /// </summary>
    public BitColorKind? Background { get; set; }

    /// <summary>
    /// The color kind of the border of the callout.
    /// </summary>
    public BitColorKind? Border { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the callout.
    /// </summary>
    public BitCalloutClassStyles? Classes { get; set; }

    /// <summary>
    /// The distance in pixels the callout keeps from the edges of the screen.
    /// </summary>
    public int? CollisionPadding { get; set; }

    /// <summary>
    /// Determines the allowed directions in which the callout should decide to be opened.
    /// </summary>
    public BitDropDirection? Direction { get; set; }

    /// <summary>
    /// Holds the callout to the width of its anchor.
    /// </summary>
    public bool? FixedCalloutWidth { get; set; }

    /// <summary>
    /// The distance in pixels between the anchor and the callout.
    /// </summary>
    public int? Gap { get; set; }

    /// <summary>
    /// The delay in milliseconds before the callout closes once the pointer leaves the callout and its anchor in the
    /// OpenOnHover mode.
    /// </summary>
    public int? HoverCloseDelay { get; set; }

    /// <summary>
    /// The delay in milliseconds before the callout opens once the pointer enters the anchor in the OpenOnHover mode.
    /// </summary>
    public int? HoverOpenDelay { get; set; }

    /// <summary>
    /// Keeps the content of the callout out of the page until the callout is opened for the first time.
    /// </summary>
    public bool? LazyRender { get; set; }

    /// <summary>
    /// The maximum height of the callout as a CSS value, beyond which its content scrolls.
    /// </summary>
    public string? MaxHeight { get; set; }

    /// <summary>
    /// The maximum width of the callout as a CSS value, beyond which its content wraps.
    /// </summary>
    public string? MaxWidth { get; set; }

    /// <summary>
    /// The window width in pixels below which the callout is allowed to hang off the end of the screen.
    /// </summary>
    public int? MaxWindowWidth { get; set; }

    /// <summary>
    /// The minimum width of the callout as a CSS value.
    /// </summary>
    public string? MinWidth { get; set; }

    /// <summary>
    /// Dims the page behind the callout and holds it still while the callout is open.
    /// </summary>
    public bool? Modal { get; set; }

    /// <summary>
    /// Keeps the Escape key from dismissing the callout.
    /// </summary>
    public bool? NoDismissOnEscape { get; set; }

    /// <summary>
    /// Keeps the callout open when a click lands outside of it, and when the page is scrolled or resized under it.
    /// </summary>
    public bool? NoDismissOnOutsideClick { get; set; }

    /// <summary>
    /// Keeps the callout open when the page is scrolled or resized under it.
    /// </summary>
    public bool? NoDismissOnScroll { get; set; }

    /// <summary>
    /// Keeps the callout on the Placement it was asked for even when there is not enough room for it there.
    /// </summary>
    public bool? NoFlip { get; set; }

    /// <summary>
    /// Leaves the page its own clicks while the callout is open, by not rendering the overlay that otherwise covers it.
    /// </summary>
    public bool? NoOverlay { get; set; }

    /// <summary>
    /// Removes the box-shadow from the callout.
    /// </summary>
    public bool? NoShadow { get; set; }

    /// <summary>
    /// Opens the callout when the pointer enters the anchor and closes it when the pointer leaves both the anchor and
    /// the callout.
    /// </summary>
    public bool? OpenOnHover { get; set; }

    /// <summary>
    /// The edge of the screen the responsive panel slides in from, for a ResponsiveMode of Panel.
    /// </summary>
    public BitPlacement? PanelPlacement { get; set; }

    /// <summary>
    /// Configures the responsive mode of the callout for the small screens.
    /// </summary>
    public BitResponsiveMode? ResponsiveMode { get; set; }

    /// <summary>
    /// The vertical offset of the scroll container to consider in the positioning and height calculation of the callout.
    /// </summary>
    public int? ScrollOffset { get; set; }

    /// <summary>
    /// Widens the callout to at least the width of its anchor.
    /// </summary>
    public bool? SetCalloutWidth { get; set; }

    /// <summary>
    /// Draws an arrow on the edge of the callout that faces the anchor, pointing at it.
    /// </summary>
    public bool? ShowArrow { get; set; }

    /// <summary>
    /// The side of the anchor the callout is placed on when there is room for it there.
    /// </summary>
    public BitPlacement? Placement { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the callout.
    /// </summary>
    public BitCalloutClassStyles? Styles { get; set; }

    /// <summary>
    /// Keeps the keyboard inside the callout while it is open and reports it as a modal dialog to the screen readers.
    /// </summary>
    public bool? TrapFocus { get; set; }

    /// <summary>
    /// The width of the callout as a CSS value.
    /// </summary>
    public string? Width { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitCallout"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitCallout"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitCallout"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitCallout"/>.
    /// </remarks>
    /// <param name="bitCallout">
    /// The <see cref="BitCallout"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitCallout bitCallout)
    {
        if (bitCallout is null) return;

        UpdateBaseParameters(bitCallout);

        if (Alignment.HasValue)
        {
            bitCallout.TakeFromCascade(nameof(Alignment), Alignment.Value, static c => c.Alignment, static (c, v) => c.Alignment = v);
        }

        if (AlignmentOffset.HasValue)
        {
            bitCallout.TakeFromCascade(nameof(AlignmentOffset), AlignmentOffset.Value, static c => c.AlignmentOffset, static (c, v) => c.AlignmentOffset = v);
        }

        if (ArrowPadding.HasValue)
        {
            bitCallout.TakeFromCascade(nameof(ArrowPadding), ArrowPadding.Value, static c => c.ArrowPadding, static (c, v) => c.ArrowPadding = v);
        }

        if (ArrowSize.HasValue)
        {
            bitCallout.TakeFromCascade(nameof(ArrowSize), ArrowSize.Value, static c => c.ArrowSize, static (c, v) => c.ArrowSize = v);
        }

        if (AutoClose.HasValue)
        {
            bitCallout.TakeFromCascade(nameof(AutoClose), AutoClose.Value, static c => c.AutoClose, static (c, v) => c.AutoClose = v);
        }

        if (AutoFocus.HasValue)
        {
            bitCallout.TakeFromCascade(nameof(AutoFocus), AutoFocus.Value, static c => c.AutoFocus, static (c, v) => c.AutoFocus = v);
        }

        if (Background.HasValue)
        {
            bitCallout.TakeFromCascade(nameof(Background), Background.Value, static c => c.Background, static (c, v) => c.Background = v);
        }

        if (Border.HasValue)
        {
            bitCallout.TakeFromCascade(nameof(Border), Border.Value, static c => c.Border, static (c, v) => c.Border = v);
        }

        if (Classes is not null)
        {
            bitCallout.TakeFromCascade(nameof(Classes), Classes, static c => c.Classes, static (c, v) => c.Classes = v);
        }

        if (CollisionPadding.HasValue)
        {
            bitCallout.TakeFromCascade(nameof(CollisionPadding), CollisionPadding.Value, static c => c.CollisionPadding, static (c, v) => c.CollisionPadding = v);
        }

        if (Direction.HasValue)
        {
            bitCallout.TakeFromCascade(nameof(Direction), Direction.Value, static c => c.Direction, static (c, v) => c.Direction = v);
        }

        if (FixedCalloutWidth.HasValue)
        {
            bitCallout.TakeFromCascade(nameof(FixedCalloutWidth), FixedCalloutWidth.Value, static c => c.FixedCalloutWidth, static (c, v) => c.FixedCalloutWidth = v);
        }

        if (Gap.HasValue)
        {
            bitCallout.TakeFromCascade(nameof(Gap), Gap.Value, static c => c.Gap, static (c, v) => c.Gap = v);
        }

        if (HoverCloseDelay.HasValue)
        {
            bitCallout.TakeFromCascade(nameof(HoverCloseDelay), HoverCloseDelay.Value, static c => c.HoverCloseDelay, static (c, v) => c.HoverCloseDelay = v);
        }

        if (HoverOpenDelay.HasValue)
        {
            bitCallout.TakeFromCascade(nameof(HoverOpenDelay), HoverOpenDelay.Value, static c => c.HoverOpenDelay, static (c, v) => c.HoverOpenDelay = v);
        }

        if (LazyRender.HasValue)
        {
            bitCallout.TakeFromCascade(nameof(LazyRender), LazyRender.Value, static c => c.LazyRender, static (c, v) => c.LazyRender = v);
        }

        if (MaxHeight.HasValue())
        {
            bitCallout.TakeFromCascade(nameof(MaxHeight), MaxHeight, static c => c.MaxHeight, static (c, v) => c.MaxHeight = v);
        }

        if (MaxWidth.HasValue())
        {
            bitCallout.TakeFromCascade(nameof(MaxWidth), MaxWidth, static c => c.MaxWidth, static (c, v) => c.MaxWidth = v);
        }

        if (MaxWindowWidth.HasValue)
        {
            bitCallout.TakeFromCascade(nameof(MaxWindowWidth), MaxWindowWidth.Value, static c => c.MaxWindowWidth, static (c, v) => c.MaxWindowWidth = v);
        }

        if (MinWidth.HasValue())
        {
            bitCallout.TakeFromCascade(nameof(MinWidth), MinWidth, static c => c.MinWidth, static (c, v) => c.MinWidth = v);
        }

        if (Modal.HasValue)
        {
            bitCallout.TakeFromCascade(nameof(Modal), Modal.Value, static c => c.Modal, static (c, v) => c.Modal = v);
        }

        if (NoDismissOnEscape.HasValue)
        {
            bitCallout.TakeFromCascade(nameof(NoDismissOnEscape), NoDismissOnEscape.Value, static c => c.NoDismissOnEscape, static (c, v) => c.NoDismissOnEscape = v);
        }

        if (NoDismissOnOutsideClick.HasValue)
        {
            bitCallout.TakeFromCascade(nameof(NoDismissOnOutsideClick), NoDismissOnOutsideClick.Value, static c => c.NoDismissOnOutsideClick, static (c, v) => c.NoDismissOnOutsideClick = v);
        }

        if (NoDismissOnScroll.HasValue)
        {
            bitCallout.TakeFromCascade(nameof(NoDismissOnScroll), NoDismissOnScroll.Value, static c => c.NoDismissOnScroll, static (c, v) => c.NoDismissOnScroll = v);
        }

        if (NoFlip.HasValue)
        {
            bitCallout.TakeFromCascade(nameof(NoFlip), NoFlip.Value, static c => c.NoFlip, static (c, v) => c.NoFlip = v);
        }

        if (NoOverlay.HasValue)
        {
            bitCallout.TakeFromCascade(nameof(NoOverlay), NoOverlay.Value, static c => c.NoOverlay, static (c, v) => c.NoOverlay = v);
        }

        if (NoShadow.HasValue)
        {
            bitCallout.TakeFromCascade(nameof(NoShadow), NoShadow.Value, static c => c.NoShadow, static (c, v) => c.NoShadow = v);
        }

        if (OpenOnHover.HasValue)
        {
            bitCallout.TakeFromCascade(nameof(OpenOnHover), OpenOnHover.Value, static c => c.OpenOnHover, static (c, v) => c.OpenOnHover = v);
        }

        if (PanelPlacement.HasValue)
        {
            bitCallout.TakeFromCascade(nameof(PanelPlacement), PanelPlacement.Value, static c => c.PanelPlacement, static (c, v) => c.PanelPlacement = v);
        }

        if (ResponsiveMode.HasValue)
        {
            bitCallout.TakeFromCascade(nameof(ResponsiveMode), ResponsiveMode.Value, static c => c.ResponsiveMode, static (c, v) => c.ResponsiveMode = v);
        }

        if (ScrollOffset.HasValue)
        {
            bitCallout.TakeFromCascade(nameof(ScrollOffset), ScrollOffset.Value, static c => c.ScrollOffset, static (c, v) => c.ScrollOffset = v);
        }

        if (SetCalloutWidth.HasValue)
        {
            bitCallout.TakeFromCascade(nameof(SetCalloutWidth), SetCalloutWidth.Value, static c => c.SetCalloutWidth, static (c, v) => c.SetCalloutWidth = v);
        }

        if (ShowArrow.HasValue)
        {
            bitCallout.TakeFromCascade(nameof(ShowArrow), ShowArrow.Value, static c => c.ShowArrow, static (c, v) => c.ShowArrow = v);
        }

        if (Placement.HasValue)
        {
            bitCallout.TakeFromCascade(nameof(Placement), Placement.Value, static c => c.Placement, static (c, v) => c.Placement = v);
        }

        if (Styles is not null)
        {
            bitCallout.TakeFromCascade(nameof(Styles), Styles, static c => c.Styles, static (c, v) => c.Styles = v);
        }

        if (TrapFocus.HasValue)
        {
            bitCallout.TakeFromCascade(nameof(TrapFocus), TrapFocus.Value, static c => c.TrapFocus, static (c, v) => c.TrapFocus = v);
        }

        if (Width.HasValue())
        {
            bitCallout.TakeFromCascade(nameof(Width), Width, static c => c.Width, static (c, v) => c.Width = v);
        }
    }
}
