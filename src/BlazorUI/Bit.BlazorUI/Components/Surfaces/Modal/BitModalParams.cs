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

        if (AbsolutePosition.HasValue)
        {
            bitModal.TakeFromCascade(nameof(AbsolutePosition), AbsolutePosition.Value, static m => m.AbsolutePosition, static (m, v) => m.AbsolutePosition = v);
        }

        if (AriaModal.HasValue)
        {
            bitModal.TakeFromCascade(nameof(AriaModal), AriaModal.Value, static m => m.AriaModal, static (m, v) => m.AriaModal = v);
        }

        if (AutoToggleScroll.HasValue)
        {
            bitModal.TakeFromCascade(nameof(AutoToggleScroll), AutoToggleScroll.Value, static m => m.AutoToggleScroll, static (m, v) => m.AutoToggleScroll = v);
        }

        if (Blocking.HasValue)
        {
            bitModal.TakeFromCascade(nameof(Blocking), Blocking.Value, static m => m.Blocking, static (m, v) => m.Blocking = v);
        }

        if (Classes is not null)
        {
            bitModal.TakeFromCascade(nameof(Classes), Classes, static m => m.Classes, static (m, v) => m.Classes = v);
        }

        if (CloseButtonTitle.HasValue())
        {
            bitModal.TakeFromCascade(nameof(CloseButtonTitle), CloseButtonTitle, static m => m.CloseButtonTitle, static (m, v) => m.CloseButtonTitle = v);
        }

        if (CloseIcon is not null)
        {
            bitModal.TakeFromCascade(nameof(CloseIcon), CloseIcon, static m => m.CloseIcon, static (m, v) => m.CloseIcon = v);
        }

        if (CloseIconName.HasValue())
        {
            bitModal.TakeFromCascade(nameof(CloseIconName), CloseIconName, static m => m.CloseIconName, static (m, v) => m.CloseIconName = v);
        }

        if (DragElementSelector.HasValue())
        {
            bitModal.TakeFromCascade(nameof(DragElementSelector), DragElementSelector, static m => m.DragElementSelector, static (m, v) => m.DragElementSelector = v);
        }

        if (Draggable.HasValue)
        {
            bitModal.TakeFromCascade(nameof(Draggable), Draggable.Value, static m => m.Draggable, static (m, v) => m.Draggable = v);
        }

        if (FullHeight.HasValue)
        {
            bitModal.TakeFromCascade(nameof(FullHeight), FullHeight.Value, static m => m.FullHeight, static (m, v) => m.FullHeight = v);
        }

        if (FullSize.HasValue)
        {
            bitModal.TakeFromCascade(nameof(FullSize), FullSize.Value, static m => m.FullSize, static (m, v) => m.FullSize = v);
        }

        if (FullWidth.HasValue)
        {
            bitModal.TakeFromCascade(nameof(FullWidth), FullWidth.Value, static m => m.FullWidth, static (m, v) => m.FullWidth = v);
        }

        if (Height.HasValue())
        {
            bitModal.TakeFromCascade(nameof(Height), Height, static m => m.Height, static (m, v) => m.Height = v);
        }

        if (IsAlert.HasValue)
        {
            bitModal.TakeFromCascade(nameof(IsAlert), IsAlert.Value, static m => m.IsAlert, static (m, v) => m.IsAlert = v);
        }

        if (KeepMounted.HasValue)
        {
            bitModal.TakeFromCascade(nameof(KeepMounted), KeepMounted.Value, static m => m.KeepMounted, static (m, v) => m.KeepMounted = v);
        }

        if (MaxHeight.HasValue())
        {
            bitModal.TakeFromCascade(nameof(MaxHeight), MaxHeight, static m => m.MaxHeight, static (m, v) => m.MaxHeight = v);
        }

        if (MaxWidth.HasValue())
        {
            bitModal.TakeFromCascade(nameof(MaxWidth), MaxWidth, static m => m.MaxWidth, static (m, v) => m.MaxWidth = v);
        }

        if (ModeFull.HasValue)
        {
            bitModal.TakeFromCascade(nameof(ModeFull), ModeFull.Value, static m => m.ModeFull, static (m, v) => m.ModeFull = v);
        }

        if (Modeless.HasValue)
        {
            bitModal.TakeFromCascade(nameof(Modeless), Modeless.Value, static m => m.Modeless, static (m, v) => m.Modeless = v);
        }

        if (NoAutoFocus.HasValue)
        {
            bitModal.TakeFromCascade(nameof(NoAutoFocus), NoAutoFocus.Value, static m => m.NoAutoFocus, static (m, v) => m.NoAutoFocus = v);
        }

        if (NoBorder.HasValue)
        {
            bitModal.TakeFromCascade(nameof(NoBorder), NoBorder.Value, static m => m.NoBorder, static (m, v) => m.NoBorder = v);
        }

        if (NoDismissOnEscape.HasValue)
        {
            bitModal.TakeFromCascade(nameof(NoDismissOnEscape), NoDismissOnEscape.Value, static m => m.NoDismissOnEscape, static (m, v) => m.NoDismissOnEscape = v);
        }

        if (NoFocusTrap.HasValue)
        {
            bitModal.TakeFromCascade(nameof(NoFocusTrap), NoFocusTrap.Value, static m => m.NoFocusTrap, static (m, v) => m.NoFocusTrap = v);
        }

        if (NoRestoreFocus.HasValue)
        {
            bitModal.TakeFromCascade(nameof(NoRestoreFocus), NoRestoreFocus.Value, static m => m.NoRestoreFocus, static (m, v) => m.NoRestoreFocus = v);
        }

        if (NoScrollLock.HasValue)
        {
            bitModal.TakeFromCascade(nameof(NoScrollLock), NoScrollLock.Value, static m => m.NoScrollLock, static (m, v) => m.NoScrollLock = v);
        }

        if (Position.HasValue)
        {
            bitModal.TakeFromCascade(nameof(Position), Position.Value, static m => m.Position, static (m, v) => m.Position = v);
        }

        if (ScrollerSelector.HasValue())
        {
            bitModal.TakeFromCascade(nameof(ScrollerSelector), ScrollerSelector, static m => m.ScrollerSelector, static (m, v) => m.ScrollerSelector = v);
        }

        if (ShowCloseButton.HasValue)
        {
            bitModal.TakeFromCascade(nameof(ShowCloseButton), ShowCloseButton.Value, static m => m.ShowCloseButton, static (m, v) => m.ShowCloseButton = v);
        }

        if (Styles is not null)
        {
            bitModal.TakeFromCascade(nameof(Styles), Styles, static m => m.Styles, static (m, v) => m.Styles = v);
        }

        if (Width.HasValue())
        {
            bitModal.TakeFromCascade(nameof(Width), Width, static m => m.Width, static (m, v) => m.Width = v);
        }
    }
}
