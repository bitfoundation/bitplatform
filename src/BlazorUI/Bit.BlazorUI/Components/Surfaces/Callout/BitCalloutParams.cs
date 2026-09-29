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
    public BitCalloutAlignment? Alignment { get; set; }

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
    /// Keeps the callout on the Side it was asked for even when there is not enough room for it there.
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
    public BitPanelPosition? PanelPosition { get; set; }

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
    public BitCalloutSide? Side { get; set; }

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

        if (Alignment.HasValue && bitCallout.HasNotBeenSet(nameof(Alignment)))
        {
            bitCallout.Alignment = Alignment.Value;
        }

        if (AlignmentOffset.HasValue && bitCallout.HasNotBeenSet(nameof(AlignmentOffset)))
        {
            bitCallout.AlignmentOffset = AlignmentOffset.Value;
        }

        if (ArrowPadding.HasValue && bitCallout.HasNotBeenSet(nameof(ArrowPadding)))
        {
            bitCallout.ArrowPadding = ArrowPadding.Value;
        }

        if (ArrowSize.HasValue && bitCallout.HasNotBeenSet(nameof(ArrowSize)))
        {
            bitCallout.ArrowSize = ArrowSize.Value;
        }

        if (AutoClose.HasValue && bitCallout.HasNotBeenSet(nameof(AutoClose)))
        {
            bitCallout.AutoClose = AutoClose.Value;
        }

        if (AutoFocus.HasValue && bitCallout.HasNotBeenSet(nameof(AutoFocus)))
        {
            bitCallout.AutoFocus = AutoFocus.Value;
        }

        if (Background.HasValue && bitCallout.HasNotBeenSet(nameof(Background)))
        {
            bitCallout.Background = Background.Value;
        }

        if (Border.HasValue && bitCallout.HasNotBeenSet(nameof(Border)))
        {
            bitCallout.Border = Border.Value;
        }

        if (Classes is not null && bitCallout.HasNotBeenSet(nameof(Classes)))
        {
            bitCallout.Classes = Classes;

            bitCallout.ClassBuilder.Reset();
        }

        if (CollisionPadding.HasValue && bitCallout.HasNotBeenSet(nameof(CollisionPadding)))
        {
            bitCallout.CollisionPadding = CollisionPadding.Value;
        }

        if (Direction.HasValue && bitCallout.HasNotBeenSet(nameof(Direction)))
        {
            bitCallout.Direction = Direction.Value;
        }

        if (FixedCalloutWidth.HasValue && bitCallout.HasNotBeenSet(nameof(FixedCalloutWidth)))
        {
            bitCallout.FixedCalloutWidth = FixedCalloutWidth.Value;
        }

        if (Gap.HasValue && bitCallout.HasNotBeenSet(nameof(Gap)))
        {
            bitCallout.Gap = Gap.Value;
        }

        if (HoverCloseDelay.HasValue && bitCallout.HasNotBeenSet(nameof(HoverCloseDelay)))
        {
            bitCallout.HoverCloseDelay = HoverCloseDelay.Value;
        }

        if (HoverOpenDelay.HasValue && bitCallout.HasNotBeenSet(nameof(HoverOpenDelay)))
        {
            bitCallout.HoverOpenDelay = HoverOpenDelay.Value;
        }

        if (LazyRender.HasValue && bitCallout.HasNotBeenSet(nameof(LazyRender)))
        {
            bitCallout.LazyRender = LazyRender.Value;
        }

        if (MaxHeight.HasValue() && bitCallout.HasNotBeenSet(nameof(MaxHeight)))
        {
            bitCallout.MaxHeight = MaxHeight;
        }

        if (MaxWidth.HasValue() && bitCallout.HasNotBeenSet(nameof(MaxWidth)))
        {
            bitCallout.MaxWidth = MaxWidth;
        }

        if (MaxWindowWidth.HasValue && bitCallout.HasNotBeenSet(nameof(MaxWindowWidth)))
        {
            bitCallout.MaxWindowWidth = MaxWindowWidth.Value;
        }

        if (MinWidth.HasValue() && bitCallout.HasNotBeenSet(nameof(MinWidth)))
        {
            bitCallout.MinWidth = MinWidth;
        }

        if (Modal.HasValue && bitCallout.HasNotBeenSet(nameof(Modal)))
        {
            bitCallout.Modal = Modal.Value;
        }

        if (NoDismissOnEscape.HasValue && bitCallout.HasNotBeenSet(nameof(NoDismissOnEscape)))
        {
            bitCallout.NoDismissOnEscape = NoDismissOnEscape.Value;
        }

        if (NoDismissOnOutsideClick.HasValue && bitCallout.HasNotBeenSet(nameof(NoDismissOnOutsideClick)))
        {
            bitCallout.NoDismissOnOutsideClick = NoDismissOnOutsideClick.Value;
        }

        if (NoDismissOnScroll.HasValue && bitCallout.HasNotBeenSet(nameof(NoDismissOnScroll)))
        {
            bitCallout.NoDismissOnScroll = NoDismissOnScroll.Value;
        }

        if (NoFlip.HasValue && bitCallout.HasNotBeenSet(nameof(NoFlip)))
        {
            bitCallout.NoFlip = NoFlip.Value;
        }

        if (NoOverlay.HasValue && bitCallout.HasNotBeenSet(nameof(NoOverlay)))
        {
            bitCallout.NoOverlay = NoOverlay.Value;
        }

        if (NoShadow.HasValue && bitCallout.HasNotBeenSet(nameof(NoShadow)))
        {
            bitCallout.NoShadow = NoShadow.Value;
        }

        if (OpenOnHover.HasValue && bitCallout.HasNotBeenSet(nameof(OpenOnHover)))
        {
            bitCallout.OpenOnHover = OpenOnHover.Value;
        }

        if (PanelPosition.HasValue && bitCallout.HasNotBeenSet(nameof(PanelPosition)))
        {
            bitCallout.PanelPosition = PanelPosition.Value;
        }

        if (ResponsiveMode.HasValue && bitCallout.HasNotBeenSet(nameof(ResponsiveMode)))
        {
            bitCallout.ResponsiveMode = ResponsiveMode.Value;
        }

        if (ScrollOffset.HasValue && bitCallout.HasNotBeenSet(nameof(ScrollOffset)))
        {
            bitCallout.ScrollOffset = ScrollOffset.Value;
        }

        if (SetCalloutWidth.HasValue && bitCallout.HasNotBeenSet(nameof(SetCalloutWidth)))
        {
            bitCallout.SetCalloutWidth = SetCalloutWidth.Value;
        }

        if (ShowArrow.HasValue && bitCallout.HasNotBeenSet(nameof(ShowArrow)))
        {
            bitCallout.ShowArrow = ShowArrow.Value;
        }

        if (Side.HasValue && bitCallout.HasNotBeenSet(nameof(Side)))
        {
            bitCallout.Side = Side.Value;
        }

        if (Styles is not null && bitCallout.HasNotBeenSet(nameof(Styles)))
        {
            bitCallout.Styles = Styles;

            bitCallout.StyleBuilder.Reset();
        }

        if (TrapFocus.HasValue && bitCallout.HasNotBeenSet(nameof(TrapFocus)))
        {
            bitCallout.TrapFocus = TrapFocus.Value;
        }

        if (Width.HasValue() && bitCallout.HasNotBeenSet(nameof(Width)))
        {
            bitCallout.Width = Width;
        }
    }
}
