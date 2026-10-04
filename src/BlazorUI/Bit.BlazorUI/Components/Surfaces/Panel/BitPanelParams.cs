namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitPanel"/> component.
/// </summary>
/// <remarks>
/// It carries the parameters that shape how a panel looks and behaves, so one object can give every panel under it
/// the same edge, size, overlay, dismissal and keyboard behavior. What belongs to one panel stays on the panel itself:
/// whether it is open, its content and the texts of its header and footer, the ids of the elements that name and
/// describe it, and its callbacks.
/// </remarks>
public class BitPanelParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitPanel"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitPanel value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitPanel)}";



    public string Name => ParamName;



    /// <summary>
    /// Lays the panel out against the nearest positioned ancestor instead of against the screen.
    /// </summary>
    public bool? AbsolutePosition { get; set; }

    /// <summary>
    /// Takes the overflow off the scroller while the panel is open and hands it back once it closes.
    /// </summary>
    public bool? AutoToggleScroll { get; set; }

    /// <summary>
    /// Keeps a click on the overlay from dismissing the panel.
    /// </summary>
    public bool? Blocking { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the panel.
    /// </summary>
    public BitPanelClassStyles? Classes { get; set; }

    /// <summary>
    /// The accessible name and tooltip of the close button.
    /// </summary>
    public string? CloseButtonTitle { get; set; }

    /// <summary>
    /// The icon of the close button using custom CSS classes for external icon libraries.
    /// </summary>
    public BitIconInfo? CloseIcon { get; set; }

    /// <summary>
    /// The name of the icon of the close button from the built-in Fluent UI icons.
    /// </summary>
    public string? CloseIconName { get; set; }

    /// <summary>
    /// Stretches the panel over the whole of the screen.
    /// </summary>
    public bool? FullSize { get; set; }

    /// <summary>
    /// Reports the panel to assistive technologies as an alert dialog.
    /// </summary>
    public bool? IsAlert { get; set; }

    /// <summary>
    /// Keeps the content of the panel in the page once it has been opened.
    /// </summary>
    public bool? KeepMounted { get; set; }

    /// <summary>
    /// Renders the overlay in full mode that gives it an opaque background.
    /// </summary>
    public bool? ModeFull { get; set; }

    /// <summary>
    /// Renders no overlay, which leaves the page behind the panel usable.
    /// </summary>
    public bool? Modeless { get; set; }

    /// <summary>
    /// Leaves the focus where it is when the panel opens.
    /// </summary>
    public bool? NoAutoFocus { get; set; }

    /// <summary>
    /// Keeps the Escape key from dismissing the panel.
    /// </summary>
    public bool? NoDismissOnEscape { get; set; }

    /// <summary>
    /// Lets the keyboard leave the panel while it is open.
    /// </summary>
    public bool? NoFocusTrap { get; set; }

    /// <summary>
    /// Leaves the focus where it is when the panel closes, instead of handing it back.
    /// </summary>
    public bool? NoRestoreFocus { get; set; }

    /// <summary>
    /// Prevents the panel from holding the page still while it is open.
    /// </summary>
    public bool? NoScrollLock { get; set; }

    /// <summary>
    /// Turns off the swipe gesture that dismisses the panel.
    /// </summary>
    public bool? NoSwipe { get; set; }

    /// <summary>
    /// The edge of the screen the panel slides in from.
    /// </summary>
    public BitPanelPosition? Position { get; set; }

    /// <summary>
    /// The ARIA role the panel reports itself under, instead of dialog.
    /// </summary>
    public string? Role { get; set; }

    /// <summary>
    /// The element reference of the scroller the panel holds while it is open.
    /// </summary>
    public ElementReference? ScrollerElement { get; set; }

    /// <summary>
    /// The CSS selector of the scroller the panel holds while it is open.
    /// </summary>
    public string? ScrollerSelector { get; set; }

    /// <summary>
    /// Shows the close button of the panel, at the end of the header row.
    /// </summary>
    public bool? ShowCloseButton { get; set; }

    /// <summary>
    /// The size of the panel in pixels along the axis it slides on.
    /// </summary>
    public double? Size { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the panel.
    /// </summary>
    public BitPanelClassStyles? Styles { get; set; }

    /// <summary>
    /// How far the panel has to be dragged before it is dismissed, as a fraction of its own size.
    /// </summary>
    public decimal? SwipeTrigger { get; set; }

    /// <summary>
    /// The layer the panel and its overlay are stacked at.
    /// </summary>
    public int? ZIndex { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitPanel"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitPanel"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitPanel"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitPanel"/>.
    /// </remarks>
    /// <param name="bitPanel">
    /// The <see cref="BitPanel"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitPanel bitPanel)
    {
        if (bitPanel is null) return;

        UpdateBaseParameters(bitPanel);

        if (AbsolutePosition.HasValue && bitPanel.HasNotBeenSet(nameof(AbsolutePosition)))
        {
            bitPanel.AbsolutePosition = AbsolutePosition.Value;

            bitPanel.ClassBuilder.Reset();
        }

        if (AutoToggleScroll.HasValue && bitPanel.HasNotBeenSet(nameof(AutoToggleScroll)))
        {
            bitPanel.AutoToggleScroll = AutoToggleScroll.Value;
        }

        if (Blocking.HasValue && bitPanel.HasNotBeenSet(nameof(Blocking)))
        {
            bitPanel.Blocking = Blocking.Value;
        }

        if (Classes is not null && bitPanel.HasNotBeenSet(nameof(Classes)))
        {
            bitPanel.Classes = Classes;

            bitPanel.ClassBuilder.Reset();
        }

        if (CloseButtonTitle.HasValue() && bitPanel.HasNotBeenSet(nameof(CloseButtonTitle)))
        {
            bitPanel.CloseButtonTitle = CloseButtonTitle;
        }

        if (CloseIcon is not null && bitPanel.HasNotBeenSet(nameof(CloseIcon)))
        {
            bitPanel.CloseIcon = CloseIcon;
        }

        if (CloseIconName.HasValue() && bitPanel.HasNotBeenSet(nameof(CloseIconName)))
        {
            bitPanel.CloseIconName = CloseIconName;
        }

        if (FullSize.HasValue && bitPanel.HasNotBeenSet(nameof(FullSize)))
        {
            bitPanel.FullSize = FullSize.Value;
        }

        if (IsAlert.HasValue && bitPanel.HasNotBeenSet(nameof(IsAlert)))
        {
            bitPanel.IsAlert = IsAlert.Value;
        }

        if (KeepMounted.HasValue && bitPanel.HasNotBeenSet(nameof(KeepMounted)))
        {
            bitPanel.KeepMounted = KeepMounted.Value;
        }

        if (ModeFull.HasValue && bitPanel.HasNotBeenSet(nameof(ModeFull)))
        {
            bitPanel.ModeFull = ModeFull.Value;
        }

        if (Modeless.HasValue && bitPanel.HasNotBeenSet(nameof(Modeless)))
        {
            bitPanel.Modeless = Modeless.Value;
        }

        if (NoAutoFocus.HasValue && bitPanel.HasNotBeenSet(nameof(NoAutoFocus)))
        {
            bitPanel.NoAutoFocus = NoAutoFocus.Value;
        }

        if (NoDismissOnEscape.HasValue && bitPanel.HasNotBeenSet(nameof(NoDismissOnEscape)))
        {
            bitPanel.NoDismissOnEscape = NoDismissOnEscape.Value;
        }

        if (NoFocusTrap.HasValue && bitPanel.HasNotBeenSet(nameof(NoFocusTrap)))
        {
            bitPanel.NoFocusTrap = NoFocusTrap.Value;
        }

        if (NoRestoreFocus.HasValue && bitPanel.HasNotBeenSet(nameof(NoRestoreFocus)))
        {
            bitPanel.NoRestoreFocus = NoRestoreFocus.Value;
        }

        if (NoScrollLock.HasValue && bitPanel.HasNotBeenSet(nameof(NoScrollLock)))
        {
            bitPanel.NoScrollLock = NoScrollLock.Value;
        }

        if (NoSwipe.HasValue && bitPanel.HasNotBeenSet(nameof(NoSwipe)))
        {
            bitPanel.NoSwipe = NoSwipe.Value;
        }

        if (Position.HasValue && bitPanel.HasNotBeenSet(nameof(Position)))
        {
            bitPanel.Position = Position.Value;
        }

        if (Role.HasValue() && bitPanel.HasNotBeenSet(nameof(Role)))
        {
            bitPanel.Role = Role;
        }

        if (ScrollerElement.HasValue && bitPanel.HasNotBeenSet(nameof(ScrollerElement)))
        {
            bitPanel.ScrollerElement = ScrollerElement.Value;
        }

        if (ScrollerSelector.HasValue() && bitPanel.HasNotBeenSet(nameof(ScrollerSelector)))
        {
            bitPanel.ScrollerSelector = ScrollerSelector;
        }

        if (ShowCloseButton.HasValue && bitPanel.HasNotBeenSet(nameof(ShowCloseButton)))
        {
            bitPanel.ShowCloseButton = ShowCloseButton.Value;
        }

        if (Size.HasValue && bitPanel.HasNotBeenSet(nameof(Size)))
        {
            bitPanel.Size = Size.Value;
        }

        if (Styles is not null && bitPanel.HasNotBeenSet(nameof(Styles)))
        {
            bitPanel.Styles = Styles;

            bitPanel.StyleBuilder.Reset();
        }

        if (SwipeTrigger.HasValue && bitPanel.HasNotBeenSet(nameof(SwipeTrigger)))
        {
            bitPanel.SwipeTrigger = SwipeTrigger.Value;
        }

        if (ZIndex.HasValue && bitPanel.HasNotBeenSet(nameof(ZIndex)))
        {
            bitPanel.ZIndex = ZIndex.Value;

            bitPanel.StyleBuilder.Reset();
        }
    }
}
