namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitModal"/> component.
/// </summary>
/// <remarks>
/// It carries the parameters that shape how a Modal looks and behaves, so one object can give every Modal under it the
/// same size, position, chrome and dismissal rules. The content, the templates, the callbacks and the open state stay
/// on the Modal itself: they belong to the code around the one Modal that shows them.
/// <br />
/// The values are defaults, not overrides: a parameter a Modal sets itself wins, and so does one a Modal shown
/// through the <see cref="BitModalService"/> is given for that one showing.
/// </remarks>
public class BitModalParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitModal"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitModal value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitModal)}";



    public string Name => ParamName;



    /// <summary>
    /// When true, the Modal is positioned absolute instead of fixed, so that it covers the element it was declared
    /// inside of rather than the screen.
    /// </summary>
    public bool? AbsolutePosition { get; set; }

    /// <summary>
    /// Whether the Modal should be announced as modal to assistive technologies.
    /// </summary>
    public bool? AriaModal { get; set; }

    /// <summary>
    /// Enables the auto scrollbar toggle behavior of the Modal.
    /// </summary>
    public bool? AutoToggleScroll { get; set; }

    /// <summary>
    /// When enabled, prevents the Modal from being light dismissed by clicking outside the Modal (on the overlay).
    /// </summary>
    public bool? Blocking { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the Modal.
    /// </summary>
    public BitModalClassStyles? Classes { get; set; }

    /// <summary>
    /// The title (and aria-label) of the close button for accessibility and localization.
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
    /// The CSS selector of the drag element, which is the content of the Modal by default.
    /// </summary>
    public string? DragElementSelector { get; set; }

    /// <summary>
    /// Whether the Modal can be dragged around.
    /// </summary>
    public bool? Draggable { get; set; }

    /// <summary>
    /// Makes the Modal height 100% of its parent container.
    /// </summary>
    public bool? FullHeight { get; set; }

    /// <summary>
    /// Makes the Modal width and height 100% of its parent container.
    /// </summary>
    public bool? FullSize { get; set; }

    /// <summary>
    /// Makes the Modal width 100% of its parent container.
    /// </summary>
    public bool? FullWidth { get; set; }

    /// <summary>
    /// The CSS height of the Modal (any CSS length).
    /// </summary>
    public string? Height { get; set; }

    /// <summary>
    /// Determines the ARIA role of the Modal (alertdialog/dialog).
    /// </summary>
    public bool? IsAlert { get; set; }

    /// <summary>
    /// Keeps the Modal in the page while it is closed instead of building it again the next time it opens.
    /// </summary>
    public bool? KeepMounted { get; set; }

    /// <summary>
    /// The CSS height the Modal is not to grow past (any CSS length).
    /// </summary>
    public string? MaxHeight { get; set; }

    /// <summary>
    /// The CSS width the Modal is not to grow past (any CSS length).
    /// </summary>
    public string? MaxWidth { get; set; }

    /// <summary>
    /// Renders the overlay in full mode that gives it an opaque background.
    /// </summary>
    public bool? ModeFull { get; set; }

    /// <summary>
    /// Whether the Modal should be modeless: no overlay, no focus trap and no scroll lock.
    /// </summary>
    public bool? Modeless { get; set; }

    /// <summary>
    /// Prevents the Modal from moving the focus into itself when it opens.
    /// </summary>
    public bool? NoAutoFocus { get; set; }

    /// <summary>
    /// Removes the default top border of the Modal.
    /// </summary>
    public bool? NoBorder { get; set; }

    /// <summary>
    /// Prevents the Modal from being dismissed by pressing the Escape key.
    /// </summary>
    public bool? NoDismissOnEscape { get; set; }

    /// <summary>
    /// Prevents the Modal from keeping the keyboard focus inside itself while it is open.
    /// </summary>
    public bool? NoFocusTrap { get; set; }

    /// <summary>
    /// Prevents the Modal from handing the focus back to the element that had it before the Modal opened.
    /// </summary>
    public bool? NoRestoreFocus { get; set; }

    /// <summary>
    /// Prevents the Modal from holding the page still while it is open.
    /// </summary>
    public bool? NoScrollLock { get; set; }

    /// <summary>
    /// Position of the Modal on the screen.
    /// </summary>
    public BitPosition? Position { get; set; }

    /// <summary>
    /// The CSS selector of the element whose scrolling the Modal holds while it is open, for the layouts whose scroller
    /// is not the page itself.
    /// </summary>
    public string? ScrollerSelector { get; set; }

    /// <summary>
    /// Shows the close button of the Modal.
    /// </summary>
    public bool? ShowCloseButton { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the Modal.
    /// </summary>
    public BitModalClassStyles? Styles { get; set; }

    /// <summary>
    /// The CSS width of the Modal (any CSS length).
    /// </summary>
    public string? Width { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitModal"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitModal"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitModal"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitModal"/>.
    /// </remarks>
    /// <param name="bitModal">
    /// The <see cref="BitModal"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitModal bitModal)
    {
        if (bitModal is null) return;

        UpdateBaseParameters(bitModal);

        if (AbsolutePosition.HasValue && bitModal.HasNotBeenSet(nameof(AbsolutePosition)))
        {
            bitModal.AbsolutePosition = AbsolutePosition.Value;
        }

        if (AriaModal.HasValue && bitModal.HasNotBeenSet(nameof(AriaModal)))
        {
            bitModal.AriaModal = AriaModal.Value;
        }

        if (AutoToggleScroll.HasValue && bitModal.HasNotBeenSet(nameof(AutoToggleScroll)))
        {
            bitModal.AutoToggleScroll = AutoToggleScroll.Value;
        }

        if (Blocking.HasValue && bitModal.HasNotBeenSet(nameof(Blocking)))
        {
            bitModal.Blocking = Blocking.Value;
        }

        if (Classes is not null && bitModal.HasNotBeenSet(nameof(Classes)))
        {
            bitModal.Classes = Classes;
        }

        if (CloseButtonTitle.HasValue() && bitModal.HasNotBeenSet(nameof(CloseButtonTitle)))
        {
            bitModal.CloseButtonTitle = CloseButtonTitle;
        }

        if (CloseIcon is not null && bitModal.HasNotBeenSet(nameof(CloseIcon)))
        {
            bitModal.CloseIcon = CloseIcon;
        }

        if (CloseIconName.HasValue() && bitModal.HasNotBeenSet(nameof(CloseIconName)))
        {
            bitModal.CloseIconName = CloseIconName;
        }

        if (DragElementSelector.HasValue() && bitModal.HasNotBeenSet(nameof(DragElementSelector)))
        {
            bitModal.DragElementSelector = DragElementSelector;
        }

        if (Draggable.HasValue && bitModal.HasNotBeenSet(nameof(Draggable)))
        {
            bitModal.Draggable = Draggable.Value;
        }

        if (FullHeight.HasValue && bitModal.HasNotBeenSet(nameof(FullHeight)))
        {
            bitModal.FullHeight = FullHeight.Value;
        }

        if (FullSize.HasValue && bitModal.HasNotBeenSet(nameof(FullSize)))
        {
            bitModal.FullSize = FullSize.Value;
        }

        if (FullWidth.HasValue && bitModal.HasNotBeenSet(nameof(FullWidth)))
        {
            bitModal.FullWidth = FullWidth.Value;
        }

        if (Height.HasValue() && bitModal.HasNotBeenSet(nameof(Height)))
        {
            bitModal.Height = Height;
        }

        if (IsAlert.HasValue && bitModal.HasNotBeenSet(nameof(IsAlert)))
        {
            bitModal.IsAlert = IsAlert.Value;
        }

        if (KeepMounted.HasValue && bitModal.HasNotBeenSet(nameof(KeepMounted)))
        {
            bitModal.KeepMounted = KeepMounted.Value;
        }

        if (MaxHeight.HasValue() && bitModal.HasNotBeenSet(nameof(MaxHeight)))
        {
            bitModal.MaxHeight = MaxHeight;
        }

        if (MaxWidth.HasValue() && bitModal.HasNotBeenSet(nameof(MaxWidth)))
        {
            bitModal.MaxWidth = MaxWidth;
        }

        if (ModeFull.HasValue && bitModal.HasNotBeenSet(nameof(ModeFull)))
        {
            bitModal.ModeFull = ModeFull.Value;
        }

        if (Modeless.HasValue && bitModal.HasNotBeenSet(nameof(Modeless)))
        {
            bitModal.Modeless = Modeless.Value;
        }

        if (NoAutoFocus.HasValue && bitModal.HasNotBeenSet(nameof(NoAutoFocus)))
        {
            bitModal.NoAutoFocus = NoAutoFocus.Value;
        }

        if (NoBorder.HasValue && bitModal.HasNotBeenSet(nameof(NoBorder)))
        {
            bitModal.NoBorder = NoBorder.Value;
        }

        if (NoDismissOnEscape.HasValue && bitModal.HasNotBeenSet(nameof(NoDismissOnEscape)))
        {
            bitModal.NoDismissOnEscape = NoDismissOnEscape.Value;
        }

        if (NoFocusTrap.HasValue && bitModal.HasNotBeenSet(nameof(NoFocusTrap)))
        {
            bitModal.NoFocusTrap = NoFocusTrap.Value;
        }

        if (NoRestoreFocus.HasValue && bitModal.HasNotBeenSet(nameof(NoRestoreFocus)))
        {
            bitModal.NoRestoreFocus = NoRestoreFocus.Value;
        }

        if (NoScrollLock.HasValue && bitModal.HasNotBeenSet(nameof(NoScrollLock)))
        {
            bitModal.NoScrollLock = NoScrollLock.Value;
        }

        if (Position.HasValue && bitModal.HasNotBeenSet(nameof(Position)))
        {
            bitModal.Position = Position.Value;
        }

        if (ScrollerSelector.HasValue() && bitModal.HasNotBeenSet(nameof(ScrollerSelector)))
        {
            bitModal.ScrollerSelector = ScrollerSelector;
        }

        if (ShowCloseButton.HasValue && bitModal.HasNotBeenSet(nameof(ShowCloseButton)))
        {
            bitModal.ShowCloseButton = ShowCloseButton.Value;
        }

        if (Styles is not null && bitModal.HasNotBeenSet(nameof(Styles)))
        {
            bitModal.Styles = Styles;
        }

        if (Width.HasValue() && bitModal.HasNotBeenSet(nameof(Width)))
        {
            bitModal.Width = Width;
        }
    }
}
