namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitDialog"/> component.
/// </summary>
/// <remarks>
/// It carries the look and the behavior shared by the dialogs under a <see cref="BitParams"/>, not what belongs
/// to one of them: the content (<see cref="BitDialog.Title"/>, <see cref="BitDialog.Subtitle"/>,
/// <see cref="BitDialog.Message"/>, <see cref="BitDialog.Body"/>, <see cref="BitDialog.ChildContent"/>,
/// <see cref="BitDialog.HeaderTemplate"/> and <see cref="BitDialog.FooterTemplate"/>), the state
/// (<see cref="BitDialog.IsOpen"/>, <see cref="BitDialog.DefaultIsOpen"/>, <see cref="BitDialog.IsOkButtonEnabled"/>
/// and <see cref="BitDialog.IsCancelButtonEnabled"/>), the callbacks, and what points at elements of one dialog
/// or one page (<see cref="BitDialog.AutoFocusSelector"/>, <see cref="BitDialog.DragElementSelector"/>,
/// <see cref="BitDialog.ScrollerElement"/>, <see cref="BitDialog.TitleAriaId"/>,
/// <see cref="BitDialog.SubtitleAriaId"/> and <see cref="BitDialog.IsAlert"/>) are left off, since they are the
/// business of the dialog they belong to.
/// </remarks>
public class BitDialogParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitDialog"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitDialog value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitDialog)}";



    public string Name => ParamName;



    /// <summary>
    /// Positions the dialog absolute instead of fixed, so it covers its nearest positioned ancestor.
    /// </summary>
    public bool? AbsolutePosition { get; set; }

    /// <summary>
    /// Moves the focus into the dialog when it opens.
    /// </summary>
    public bool? AutoFocus { get; set; }

    /// <summary>
    /// Which of the dialog's own buttons AutoFocus lands on, e.g. the safe answer of a destructive confirmation.
    /// </summary>
    public BitDialogButton? AutoFocusButton { get; set; }

    /// <summary>
    /// Holds the scrolling of the page (or of the scroller named by ScrollerSelector) while the dialog is open.
    /// </summary>
    public bool? AutoToggleScroll { get; set; }

    /// <summary>
    /// The text of the cancel button.
    /// </summary>
    public string? CancelText { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the dialog.
    /// </summary>
    public BitDialogClassStyles? Classes { get; set; }

    /// <summary>
    /// The title (and aria-label) of the close button.
    /// </summary>
    public string? CloseButtonTitle { get; set; }

    /// <summary>
    /// The icon of the close button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="CloseIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? CloseIcon { get; set; }

    /// <summary>
    /// The name of the icon of the close button from the built-in Fluent UI icons.
    /// </summary>
    public string? CloseIconName { get; set; }

    /// <summary>
    /// Dismisses the dialog when the Escape key is pressed while the focus is inside it.
    /// </summary>
    public bool? CloseOnEscape { get; set; }

    /// <summary>
    /// Dismisses the dialog when its overlay is clicked.
    /// </summary>
    public bool? CloseOnOverlayClick { get; set; }

    /// <summary>
    /// The general color of the dialog, which its Ok and Cancel buttons are painted in.
    /// </summary>
    public BitColor? Color { get; set; }

    /// <summary>
    /// Makes the dialog height 100% of the area it is positioned in.
    /// </summary>
    public bool? FullHeight { get; set; }

    /// <summary>
    /// Makes the dialog width and height 100% of the area it is positioned in.
    /// </summary>
    public bool? FullSize { get; set; }

    /// <summary>
    /// Makes the dialog width 100% of the area it is positioned in.
    /// </summary>
    public bool? FullWidth { get; set; }

    /// <summary>
    /// The CSS height of the dialog surface.
    /// </summary>
    public string? Height { get; set; }

    /// <summary>
    /// Keeps the dialog from being light dismissed by the overlay or the Escape key.
    /// </summary>
    public bool? Blocking { get; set; }

    /// <summary>
    /// Lets the dialog be dragged around by its header.
    /// </summary>
    public bool? IsDraggable { get; set; }

    /// <summary>
    /// Renders the dialog without an overlay, leaving the page behind it usable.
    /// </summary>
    public bool? Modeless { get; set; }

    /// <summary>
    /// Keeps the dialog in the DOM while it is closed, hidden, instead of removing it.
    /// </summary>
    public bool? KeepMounted { get; set; }

    /// <summary>
    /// The CSS maximum height of the dialog surface.
    /// </summary>
    public string? MaxHeight { get; set; }

    /// <summary>
    /// The CSS maximum width of the dialog surface.
    /// </summary>
    public string? MaxWidth { get; set; }

    /// <summary>
    /// The CSS minimum height of the dialog surface.
    /// </summary>
    public string? MinHeight { get; set; }

    /// <summary>
    /// The CSS minimum width of the dialog surface.
    /// </summary>
    public string? MinWidth { get; set; }

    /// <summary>
    /// Turns off the shake the dialog plays when a dismissal is refused.
    /// </summary>
    public bool? NoDismissPreventedAnimation { get; set; }

    /// <summary>
    /// The text of the ok button.
    /// </summary>
    public string? OkText { get; set; }

    /// <summary>
    /// The position of the dialog on the screen.
    /// </summary>
    public BitPosition? Position { get; set; }

    /// <summary>
    /// Hands the focus back to whatever held it when the dialog opened, once the dialog closes.
    /// </summary>
    public bool? RestoreFocus { get; set; }

    /// <summary>
    /// The CSS selector of the element whose scrolling the dialog holds while it is open.
    /// </summary>
    public string? ScrollerSelector { get; set; }

    /// <summary>
    /// Shows or hides the cancel button of the dialog.
    /// </summary>
    public bool? ShowCancelButton { get; set; }

    /// <summary>
    /// Shows or hides the close button of the dialog.
    /// </summary>
    public bool? ShowCloseButton { get; set; }

    /// <summary>
    /// Shows or hides the ok button of the dialog.
    /// </summary>
    public bool? ShowOkButton { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the dialog.
    /// </summary>
    public BitDialogClassStyles? Styles { get; set; }

    /// <summary>
    /// Keeps Tab and Shift+Tab cycling inside the dialog while it is open.
    /// </summary>
    public bool? TrapFocus { get; set; }

    /// <summary>
    /// The CSS width of the dialog surface.
    /// </summary>
    public string? Width { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitDialog"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitDialog"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitDialog"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitDialog"/>.
    /// </remarks>
    /// <param name="bitDialog">
    /// The <see cref="BitDialog"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitDialog bitDialog)
    {
        if (bitDialog is null) return;

        UpdateBaseParameters(bitDialog);

        if (AbsolutePosition.HasValue)
        {
            bitDialog.TakeFromCascade(nameof(AbsolutePosition), AbsolutePosition.Value, static d => d.AbsolutePosition, static (d, v) => d.AbsolutePosition = v);
        }

        if (AutoFocus.HasValue)
        {
            bitDialog.TakeFromCascade(nameof(AutoFocus), AutoFocus.Value, static d => d.AutoFocus, static (d, v) => d.AutoFocus = v);
        }

        if (AutoFocusButton.HasValue)
        {
            bitDialog.TakeFromCascade(nameof(AutoFocusButton), AutoFocusButton.Value, static d => d.AutoFocusButton, static (d, v) => d.AutoFocusButton = v);
        }

        if (AutoToggleScroll.HasValue)
        {
            bitDialog.TakeFromCascade(nameof(AutoToggleScroll), AutoToggleScroll.Value, static d => d.AutoToggleScroll, static (d, v) => d.AutoToggleScroll = v);
        }

        if (CancelText.HasValue())
        {
            bitDialog.TakeFromCascade(nameof(CancelText), CancelText, static d => d.CancelText, static (d, v) => d.CancelText = v);
        }

        if (Classes is not null)
        {
            bitDialog.TakeFromCascade(nameof(Classes), Classes, static d => d.Classes, static (d, v) => d.Classes = v);
        }

        if (CloseButtonTitle.HasValue())
        {
            bitDialog.TakeFromCascade(nameof(CloseButtonTitle), CloseButtonTitle, static d => d.CloseButtonTitle, static (d, v) => d.CloseButtonTitle = v);
        }

        var ownCloseIcon = bitDialog.HasSetAnyOf(nameof(CloseIcon), nameof(CloseIconName));

        if (CloseIcon is not null)
        {
            bitDialog.TakeFromCascade(nameof(CloseIcon), CloseIcon, static d => d.CloseIcon, static (d, v) => d.CloseIcon = v, outranked: ownCloseIcon);
        }

        if (CloseIconName.HasValue())
        {
            bitDialog.TakeFromCascade(nameof(CloseIconName), CloseIconName, static d => d.CloseIconName, static (d, v) => d.CloseIconName = v, outranked: ownCloseIcon);
        }

        if (CloseOnEscape.HasValue)
        {
            bitDialog.TakeFromCascade(nameof(CloseOnEscape), CloseOnEscape.Value, static d => d.CloseOnEscape, static (d, v) => d.CloseOnEscape = v);
        }

        if (CloseOnOverlayClick.HasValue)
        {
            bitDialog.TakeFromCascade(nameof(CloseOnOverlayClick), CloseOnOverlayClick.Value, static d => d.CloseOnOverlayClick, static (d, v) => d.CloseOnOverlayClick = v);
        }

        if (Color.HasValue)
        {
            bitDialog.TakeFromCascade(nameof(Color), Color.Value, static d => d.Color, static (d, v) => d.Color = v);
        }

        if (FullHeight.HasValue)
        {
            bitDialog.TakeFromCascade(nameof(FullHeight), FullHeight.Value, static d => d.FullHeight, static (d, v) => d.FullHeight = v);
        }

        if (FullSize.HasValue)
        {
            bitDialog.TakeFromCascade(nameof(FullSize), FullSize.Value, static d => d.FullSize, static (d, v) => d.FullSize = v);
        }

        if (FullWidth.HasValue)
        {
            bitDialog.TakeFromCascade(nameof(FullWidth), FullWidth.Value, static d => d.FullWidth, static (d, v) => d.FullWidth = v);
        }

        if (Height.HasValue())
        {
            bitDialog.TakeFromCascade(nameof(Height), Height, static d => d.Height, static (d, v) => d.Height = v);
        }

        if (Blocking.HasValue)
        {
            bitDialog.TakeFromCascade(nameof(Blocking), Blocking.Value, static d => d.Blocking, static (d, v) => d.Blocking = v);
        }

        if (IsDraggable.HasValue)
        {
            bitDialog.TakeFromCascade(nameof(IsDraggable), IsDraggable.Value, static d => d.IsDraggable, static (d, v) => d.IsDraggable = v);
        }

        if (Modeless.HasValue)
        {
            bitDialog.TakeFromCascade(nameof(Modeless), Modeless.Value, static d => d.Modeless, static (d, v) => d.Modeless = v);
        }

        if (KeepMounted.HasValue)
        {
            bitDialog.TakeFromCascade(nameof(KeepMounted), KeepMounted.Value, static d => d.KeepMounted, static (d, v) => d.KeepMounted = v);
        }

        if (MaxHeight.HasValue())
        {
            bitDialog.TakeFromCascade(nameof(MaxHeight), MaxHeight, static d => d.MaxHeight, static (d, v) => d.MaxHeight = v);
        }

        if (MaxWidth.HasValue())
        {
            bitDialog.TakeFromCascade(nameof(MaxWidth), MaxWidth, static d => d.MaxWidth, static (d, v) => d.MaxWidth = v);
        }

        if (MinHeight.HasValue())
        {
            bitDialog.TakeFromCascade(nameof(MinHeight), MinHeight, static d => d.MinHeight, static (d, v) => d.MinHeight = v);
        }

        if (MinWidth.HasValue())
        {
            bitDialog.TakeFromCascade(nameof(MinWidth), MinWidth, static d => d.MinWidth, static (d, v) => d.MinWidth = v);
        }

        if (NoDismissPreventedAnimation.HasValue)
        {
            bitDialog.TakeFromCascade(nameof(NoDismissPreventedAnimation), NoDismissPreventedAnimation.Value, static d => d.NoDismissPreventedAnimation, static (d, v) => d.NoDismissPreventedAnimation = v);
        }

        if (OkText.HasValue())
        {
            bitDialog.TakeFromCascade(nameof(OkText), OkText, static d => d.OkText, static (d, v) => d.OkText = v);
        }

        if (Position.HasValue)
        {
            bitDialog.TakeFromCascade(nameof(Position), Position.Value, static d => d.Position, static (d, v) => d.Position = v);
        }

        if (RestoreFocus.HasValue)
        {
            bitDialog.TakeFromCascade(nameof(RestoreFocus), RestoreFocus.Value, static d => d.RestoreFocus, static (d, v) => d.RestoreFocus = v);
        }

        if (ScrollerSelector.HasValue())
        {
            bitDialog.TakeFromCascade(nameof(ScrollerSelector), ScrollerSelector, static d => d.ScrollerSelector, static (d, v) => d.ScrollerSelector = v);
        }

        if (ShowCancelButton.HasValue)
        {
            bitDialog.TakeFromCascade(nameof(ShowCancelButton), ShowCancelButton.Value, static d => d.ShowCancelButton, static (d, v) => d.ShowCancelButton = v);
        }

        if (ShowCloseButton.HasValue)
        {
            bitDialog.TakeFromCascade(nameof(ShowCloseButton), ShowCloseButton.Value, static d => d.ShowCloseButton, static (d, v) => d.ShowCloseButton = v);
        }

        if (ShowOkButton.HasValue)
        {
            bitDialog.TakeFromCascade(nameof(ShowOkButton), ShowOkButton.Value, static d => d.ShowOkButton, static (d, v) => d.ShowOkButton = v);
        }

        if (Styles is not null)
        {
            bitDialog.TakeFromCascade(nameof(Styles), Styles, static d => d.Styles, static (d, v) => d.Styles = v);
        }

        if (TrapFocus.HasValue)
        {
            bitDialog.TakeFromCascade(nameof(TrapFocus), TrapFocus.Value, static d => d.TrapFocus, static (d, v) => d.TrapFocus = v);
        }

        if (Width.HasValue())
        {
            bitDialog.TakeFromCascade(nameof(Width), Width, static d => d.Width, static (d, v) => d.Width = v);
        }
    }
}
