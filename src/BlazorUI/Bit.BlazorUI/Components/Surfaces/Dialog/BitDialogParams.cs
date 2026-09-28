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
    public bool? IsBlocking { get; set; }

    /// <summary>
    /// Lets the dialog be dragged around by its header.
    /// </summary>
    public bool? IsDraggable { get; set; }

    /// <summary>
    /// Renders the dialog without an overlay, leaving the page behind it usable.
    /// </summary>
    public bool? IsModeless { get; set; }

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
    public BitDialogPosition? Position { get; set; }

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

        if (AbsolutePosition.HasValue && bitDialog.HasNotBeenSet(nameof(AbsolutePosition)))
        {
            bitDialog.AbsolutePosition = AbsolutePosition.Value;

            bitDialog.ClassBuilder.Reset();
        }

        if (AutoFocus.HasValue && bitDialog.HasNotBeenSet(nameof(AutoFocus)))
        {
            bitDialog.AutoFocus = AutoFocus.Value;
        }

        if (AutoFocusButton.HasValue && bitDialog.HasNotBeenSet(nameof(AutoFocusButton)))
        {
            bitDialog.AutoFocusButton = AutoFocusButton.Value;
        }

        if (AutoToggleScroll.HasValue && bitDialog.HasNotBeenSet(nameof(AutoToggleScroll)))
        {
            bitDialog.AutoToggleScroll = AutoToggleScroll.Value;
        }

        if (CancelText.HasValue() && bitDialog.HasNotBeenSet(nameof(CancelText)))
        {
            bitDialog.CancelText = CancelText;
        }

        if (Classes is not null && bitDialog.HasNotBeenSet(nameof(Classes)))
        {
            bitDialog.Classes = Classes;

            bitDialog.ClassBuilder.Reset();
        }

        if (CloseButtonTitle.HasValue() && bitDialog.HasNotBeenSet(nameof(CloseButtonTitle)))
        {
            bitDialog.CloseButtonTitle = CloseButtonTitle;
        }

        // The two are one setting - which icon the close button shows - and CloseIcon wins over CloseIconName,
        // so a Dialog that picked its icon through either of them keeps it: a cascaded CloseIcon filled in beside
        // a CloseIconName of its own would otherwise replace the icon the Dialog asked for.
        if (bitDialog.HasNotBeenSet(nameof(CloseIcon)) && bitDialog.HasNotBeenSet(nameof(CloseIconName)))
        {
            if (CloseIcon is not null)
            {
                bitDialog.CloseIcon = CloseIcon;
            }

            if (CloseIconName.HasValue())
            {
                bitDialog.CloseIconName = CloseIconName;
            }
        }

        if (CloseOnEscape.HasValue && bitDialog.HasNotBeenSet(nameof(CloseOnEscape)))
        {
            bitDialog.CloseOnEscape = CloseOnEscape.Value;
        }

        if (CloseOnOverlayClick.HasValue && bitDialog.HasNotBeenSet(nameof(CloseOnOverlayClick)))
        {
            bitDialog.CloseOnOverlayClick = CloseOnOverlayClick.Value;
        }

        if (Color.HasValue && bitDialog.HasNotBeenSet(nameof(Color)))
        {
            bitDialog.Color = Color.Value;

            bitDialog.ClassBuilder.Reset();
        }

        if (FullHeight.HasValue && bitDialog.HasNotBeenSet(nameof(FullHeight)))
        {
            bitDialog.FullHeight = FullHeight.Value;

            bitDialog.ClassBuilder.Reset();
        }

        if (FullSize.HasValue && bitDialog.HasNotBeenSet(nameof(FullSize)))
        {
            bitDialog.FullSize = FullSize.Value;

            bitDialog.ClassBuilder.Reset();
        }

        if (FullWidth.HasValue && bitDialog.HasNotBeenSet(nameof(FullWidth)))
        {
            bitDialog.FullWidth = FullWidth.Value;

            bitDialog.ClassBuilder.Reset();
        }

        if (Height.HasValue() && bitDialog.HasNotBeenSet(nameof(Height)))
        {
            bitDialog.Height = Height;
        }

        if (IsBlocking.HasValue && bitDialog.HasNotBeenSet(nameof(IsBlocking)))
        {
            bitDialog.IsBlocking = IsBlocking.Value;
        }

        if (IsDraggable.HasValue && bitDialog.HasNotBeenSet(nameof(IsDraggable)))
        {
            bitDialog.IsDraggable = IsDraggable.Value;
        }

        if (IsModeless.HasValue && bitDialog.HasNotBeenSet(nameof(IsModeless)))
        {
            bitDialog.IsModeless = IsModeless.Value;

            bitDialog.ClassBuilder.Reset();
        }

        if (KeepMounted.HasValue && bitDialog.HasNotBeenSet(nameof(KeepMounted)))
        {
            bitDialog.KeepMounted = KeepMounted.Value;
        }

        if (MaxHeight.HasValue() && bitDialog.HasNotBeenSet(nameof(MaxHeight)))
        {
            bitDialog.MaxHeight = MaxHeight;
        }

        if (MaxWidth.HasValue() && bitDialog.HasNotBeenSet(nameof(MaxWidth)))
        {
            bitDialog.MaxWidth = MaxWidth;
        }

        if (MinHeight.HasValue() && bitDialog.HasNotBeenSet(nameof(MinHeight)))
        {
            bitDialog.MinHeight = MinHeight;
        }

        if (MinWidth.HasValue() && bitDialog.HasNotBeenSet(nameof(MinWidth)))
        {
            bitDialog.MinWidth = MinWidth;
        }

        if (NoDismissPreventedAnimation.HasValue && bitDialog.HasNotBeenSet(nameof(NoDismissPreventedAnimation)))
        {
            bitDialog.NoDismissPreventedAnimation = NoDismissPreventedAnimation.Value;
        }

        if (OkText.HasValue() && bitDialog.HasNotBeenSet(nameof(OkText)))
        {
            bitDialog.OkText = OkText;
        }

        if (Position.HasValue && bitDialog.HasNotBeenSet(nameof(Position)))
        {
            bitDialog.Position = Position.Value;
        }

        if (RestoreFocus.HasValue && bitDialog.HasNotBeenSet(nameof(RestoreFocus)))
        {
            bitDialog.RestoreFocus = RestoreFocus.Value;
        }

        if (ScrollerSelector.HasValue() && bitDialog.HasNotBeenSet(nameof(ScrollerSelector)))
        {
            bitDialog.ScrollerSelector = ScrollerSelector;
        }

        if (ShowCancelButton.HasValue && bitDialog.HasNotBeenSet(nameof(ShowCancelButton)))
        {
            bitDialog.ShowCancelButton = ShowCancelButton.Value;
        }

        if (ShowCloseButton.HasValue && bitDialog.HasNotBeenSet(nameof(ShowCloseButton)))
        {
            bitDialog.ShowCloseButton = ShowCloseButton.Value;
        }

        if (ShowOkButton.HasValue && bitDialog.HasNotBeenSet(nameof(ShowOkButton)))
        {
            bitDialog.ShowOkButton = ShowOkButton.Value;
        }

        if (Styles is not null && bitDialog.HasNotBeenSet(nameof(Styles)))
        {
            bitDialog.Styles = Styles;

            bitDialog.StyleBuilder.Reset();
        }

        if (TrapFocus.HasValue && bitDialog.HasNotBeenSet(nameof(TrapFocus)))
        {
            bitDialog.TrapFocus = TrapFocus.Value;
        }

        if (Width.HasValue() && bitDialog.HasNotBeenSet(nameof(Width)))
        {
            bitDialog.Width = Width;
        }
    }
}
