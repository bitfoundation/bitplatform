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
    public BitPlacement? Placement { get; set; }

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

        if (AbsolutePosition.HasValue)
        {
            bitPanel.TakeFromCascade(nameof(AbsolutePosition), AbsolutePosition.Value, static p => p.AbsolutePosition, static (p, v) => p.AbsolutePosition = v);
        }

        if (AutoToggleScroll.HasValue)
        {
            bitPanel.TakeFromCascade(nameof(AutoToggleScroll), AutoToggleScroll.Value, static p => p.AutoToggleScroll, static (p, v) => p.AutoToggleScroll = v);
        }

        if (Blocking.HasValue)
        {
            bitPanel.TakeFromCascade(nameof(Blocking), Blocking.Value, static p => p.Blocking, static (p, v) => p.Blocking = v);
        }

        if (Classes is not null)
        {
            bitPanel.TakeFromCascade(nameof(Classes), Classes, static p => p.Classes, static (p, v) => p.Classes = v);
        }

        if (CloseButtonTitle.HasValue())
        {
            bitPanel.TakeFromCascade(nameof(CloseButtonTitle), CloseButtonTitle, static p => p.CloseButtonTitle, static (p, v) => p.CloseButtonTitle = v);
        }


        if (CloseIcon is not null)
        {
            bitPanel.TakeFromCascade(nameof(CloseIcon), CloseIcon, static p => p.CloseIcon, static (p, v) => p.CloseIcon = v);
        }

        if (CloseIconName.HasValue())
        {
            bitPanel.TakeFromCascade(nameof(CloseIconName), CloseIconName, static p => p.CloseIconName, static (p, v) => p.CloseIconName = v);
        }

        if (FullSize.HasValue)
        {
            bitPanel.TakeFromCascade(nameof(FullSize), FullSize.Value, static p => p.FullSize, static (p, v) => p.FullSize = v);
        }

        if (IsAlert.HasValue)
        {
            bitPanel.TakeFromCascade(nameof(IsAlert), IsAlert.Value, static p => p.IsAlert, static (p, v) => p.IsAlert = v);
        }

        if (KeepMounted.HasValue)
        {
            bitPanel.TakeFromCascade(nameof(KeepMounted), KeepMounted.Value, static p => p.KeepMounted, static (p, v) => p.KeepMounted = v);
        }

        if (ModeFull.HasValue)
        {
            bitPanel.TakeFromCascade(nameof(ModeFull), ModeFull.Value, static p => p.ModeFull, static (p, v) => p.ModeFull = v);
        }

        if (Modeless.HasValue)
        {
            bitPanel.TakeFromCascade(nameof(Modeless), Modeless.Value, static p => p.Modeless, static (p, v) => p.Modeless = v);
        }

        if (NoAutoFocus.HasValue)
        {
            bitPanel.TakeFromCascade(nameof(NoAutoFocus), NoAutoFocus.Value, static p => p.NoAutoFocus, static (p, v) => p.NoAutoFocus = v);
        }

        if (NoDismissOnEscape.HasValue)
        {
            bitPanel.TakeFromCascade(nameof(NoDismissOnEscape), NoDismissOnEscape.Value, static p => p.NoDismissOnEscape, static (p, v) => p.NoDismissOnEscape = v);
        }

        if (NoFocusTrap.HasValue)
        {
            bitPanel.TakeFromCascade(nameof(NoFocusTrap), NoFocusTrap.Value, static p => p.NoFocusTrap, static (p, v) => p.NoFocusTrap = v);
        }

        if (NoRestoreFocus.HasValue)
        {
            bitPanel.TakeFromCascade(nameof(NoRestoreFocus), NoRestoreFocus.Value, static p => p.NoRestoreFocus, static (p, v) => p.NoRestoreFocus = v);
        }

        if (NoScrollLock.HasValue)
        {
            bitPanel.TakeFromCascade(nameof(NoScrollLock), NoScrollLock.Value, static p => p.NoScrollLock, static (p, v) => p.NoScrollLock = v);
        }

        if (NoSwipe.HasValue)
        {
            bitPanel.TakeFromCascade(nameof(NoSwipe), NoSwipe.Value, static p => p.NoSwipe, static (p, v) => p.NoSwipe = v);
        }

        if (Placement.HasValue)
        {
            bitPanel.TakeFromCascade(nameof(Placement), Placement.Value, static p => p.Placement, static (p, v) => p.Placement = v);
        }

        if (Role.HasValue())
        {
            bitPanel.TakeFromCascade(nameof(Role), Role, static p => p.Role, static (p, v) => p.Role = v);
        }

        if (ScrollerElement.HasValue)
        {
            bitPanel.TakeFromCascade(nameof(ScrollerElement), ScrollerElement.Value, static p => p.ScrollerElement, static (p, v) => p.ScrollerElement = v);
        }

        if (ScrollerSelector.HasValue())
        {
            bitPanel.TakeFromCascade(nameof(ScrollerSelector), ScrollerSelector, static p => p.ScrollerSelector, static (p, v) => p.ScrollerSelector = v);
        }

        if (ShowCloseButton.HasValue)
        {
            bitPanel.TakeFromCascade(nameof(ShowCloseButton), ShowCloseButton.Value, static p => p.ShowCloseButton, static (p, v) => p.ShowCloseButton = v);
        }

        if (Size.HasValue)
        {
            bitPanel.TakeFromCascade(nameof(Size), Size.Value, static p => p.Size, static (p, v) => p.Size = v);
        }

        if (Styles is not null)
        {
            bitPanel.TakeFromCascade(nameof(Styles), Styles, static p => p.Styles, static (p, v) => p.Styles = v);
        }

        if (SwipeTrigger.HasValue)
        {
            bitPanel.TakeFromCascade(nameof(SwipeTrigger), SwipeTrigger.Value, static p => p.SwipeTrigger, static (p, v) => p.SwipeTrigger = v);
        }

        if (ZIndex.HasValue)
        {
            bitPanel.TakeFromCascade(nameof(ZIndex), ZIndex.Value, static p => p.ZIndex, static (p, v) => p.ZIndex = v);
        }
    }
}
