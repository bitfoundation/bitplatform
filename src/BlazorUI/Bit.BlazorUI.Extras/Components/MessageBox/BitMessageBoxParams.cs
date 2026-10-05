namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitMessageBox"/> component.
/// </summary>
/// <remarks>
/// What belongs here is what every message box of an app or a section of it agrees on - the words on the buttons
/// and on the close button (one place to localize them), their colors and order, the size, what the focus lands on -
/// rather than what makes one message box the one it is. The title, the body, the set of buttons, the color (the
/// severity of the message), the icon and the templates and callbacks are deliberately not here: they belong to a
/// single message box, and cascading them would give every message box under the <see cref="BitParams"/> the same one.
/// <br/>
/// A <see cref="BitParams"/> around the <see cref="BitModalContainer"/> reaches the message boxes the
/// <see cref="BitMessageBoxService"/> shows as well, filling in whatever the <see cref="BitMessageBoxParameters"/> of
/// a showing left unset. The service always sets <see cref="BitMessageBox.AutoFocus"/> itself, so the cascaded
/// <see cref="AutoFocus"/> only reaches the message boxes written in a page.
/// </remarks>
public class BitMessageBoxParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitMessageBox"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitMessageBox value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitMessageBox)}";



    public string Name => ParamName;



    /// <summary>
    /// Moves the focus onto the default action button once the message box is rendered.
    /// </summary>
    public bool? AutoFocus { get; set; }

    /// <summary>
    /// Enables the loading state of the action button that was pressed for as long as its callback runs.
    /// </summary>
    public bool? AutoLoading { get; set; }

    /// <summary>
    /// The color of the action buttons of the message box.
    /// </summary>
    public BitColor? ButtonColor { get; set; }

    /// <summary>
    /// The text of the Cancel button.
    /// </summary>
    public string? CancelText { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the message box.
    /// </summary>
    public BitMessageBoxClassStyles? Classes { get; set; }

    /// <summary>
    /// The title (and aria-label) of the close button.
    /// </summary>
    public string? CloseButtonTitle { get; set; }

    /// <summary>
    /// The icon of the close button, provided as custom CSS classes of an external icon library.
    /// </summary>
    public BitIconInfo? CloseIcon { get; set; }

    /// <summary>
    /// The name of the icon of the close button, from the built-in Fluent UI icons.
    /// </summary>
    public string? CloseIconName { get; set; }

    /// <summary>
    /// The action button the focus is moved onto, or <see cref="BitMessageBoxResult.None"/> for the close button.
    /// A button that is not part of a message box's set of buttons is ignored by that message box.
    /// </summary>
    public BitMessageBoxResult? DefaultButton { get; set; }

    /// <summary>
    /// Removes the leading icon of the message box.
    /// </summary>
    public bool? HideIcon { get; set; }

    /// <summary>
    /// The text of the No button.
    /// </summary>
    public string? NoText { get; set; }

    /// <summary>
    /// The text of the Ok button.
    /// </summary>
    public string? OkText { get; set; }

    /// <summary>
    /// The color of the affirmative action button (Ok, or Yes), which falls back to <see cref="ButtonColor"/>.
    /// </summary>
    public BitColor? PrimaryButtonColor { get; set; }

    /// <summary>
    /// Renders the action buttons in the reverse order.
    /// </summary>
    public bool? Reversed { get; set; }

    /// <summary>
    /// Renders the close button in the header of the message box.
    /// </summary>
    public bool? ShowCloseButton { get; set; }

    /// <summary>
    /// The size of the message box.
    /// </summary>
    public BitSize? Size { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the message box.
    /// </summary>
    public BitMessageBoxClassStyles? Styles { get; set; }

    /// <summary>
    /// The HTML element the title of the message box is rendered as.
    /// </summary>
    public string? TitleElement { get; set; }

    /// <summary>
    /// The text of the Yes button.
    /// </summary>
    public string? YesText { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitMessageBox"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitMessageBox"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitMessageBox"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitMessageBox"/>.
    /// </remarks>
    /// <param name="bitMessageBox">
    /// The <see cref="BitMessageBox"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitMessageBox bitMessageBox)
    {
        if (bitMessageBox is null) return;

        UpdateBaseParameters(bitMessageBox);

        if (AutoFocus.HasValue && bitMessageBox.HasNotBeenSet(nameof(AutoFocus)))
        {
            bitMessageBox.AutoFocus = AutoFocus.Value;
        }

        if (AutoLoading.HasValue && bitMessageBox.HasNotBeenSet(nameof(AutoLoading)))
        {
            bitMessageBox.AutoLoading = AutoLoading.Value;
        }

        if (ButtonColor.HasValue && bitMessageBox.HasNotBeenSet(nameof(ButtonColor)))
        {
            bitMessageBox.ButtonColor = ButtonColor.Value;
        }

        if (CancelText.HasValue() && bitMessageBox.HasNotBeenSet(nameof(CancelText)))
        {
            bitMessageBox.CancelText = CancelText;
        }

        // This runs on every render of every message box under the BitParams, so a value that drives the class or
        // the style of the root only resets the builders when it differs from the one the message box already holds.
        if (Classes is not null && bitMessageBox.HasNotBeenSet(nameof(Classes)) && ReferenceEquals(bitMessageBox.Classes, Classes) is false)
        {
            bitMessageBox.Classes = Classes;

            bitMessageBox.ClassBuilder.Reset();
        }

        if (CloseButtonTitle.HasValue() && bitMessageBox.HasNotBeenSet(nameof(CloseButtonTitle)))
        {
            bitMessageBox.CloseButtonTitle = CloseButtonTitle;
        }

        if (CloseIcon is not null && bitMessageBox.HasNotBeenSet(nameof(CloseIcon)))
        {
            bitMessageBox.CloseIcon = CloseIcon;
        }

        if (CloseIconName.HasValue() && bitMessageBox.HasNotBeenSet(nameof(CloseIconName)))
        {
            bitMessageBox.CloseIconName = CloseIconName;
        }

        if (DefaultButton.HasValue && bitMessageBox.HasNotBeenSet(nameof(DefaultButton)))
        {
            bitMessageBox.DefaultButton = DefaultButton.Value;
        }

        if (HideIcon.HasValue && bitMessageBox.HasNotBeenSet(nameof(HideIcon)))
        {
            bitMessageBox.HideIcon = HideIcon.Value;
        }

        if (NoText.HasValue() && bitMessageBox.HasNotBeenSet(nameof(NoText)))
        {
            bitMessageBox.NoText = NoText;
        }

        if (OkText.HasValue() && bitMessageBox.HasNotBeenSet(nameof(OkText)))
        {
            bitMessageBox.OkText = OkText;
        }

        if (PrimaryButtonColor.HasValue && bitMessageBox.HasNotBeenSet(nameof(PrimaryButtonColor)))
        {
            bitMessageBox.PrimaryButtonColor = PrimaryButtonColor.Value;
        }

        if (Reversed.HasValue && bitMessageBox.HasNotBeenSet(nameof(Reversed)))
        {
            bitMessageBox.Reversed = Reversed.Value;
        }

        if (ShowCloseButton.HasValue && bitMessageBox.HasNotBeenSet(nameof(ShowCloseButton)))
        {
            bitMessageBox.ShowCloseButton = ShowCloseButton.Value;
        }

        if (Size.HasValue && bitMessageBox.HasNotBeenSet(nameof(Size)) && bitMessageBox.Size != Size.Value)
        {
            bitMessageBox.Size = Size.Value;

            bitMessageBox.ClassBuilder.Reset();
        }

        if (Styles is not null && bitMessageBox.HasNotBeenSet(nameof(Styles)) && ReferenceEquals(bitMessageBox.Styles, Styles) is false)
        {
            bitMessageBox.Styles = Styles;

            bitMessageBox.StyleBuilder.Reset();
        }

        if (TitleElement.HasValue() && bitMessageBox.HasNotBeenSet(nameof(TitleElement)))
        {
            bitMessageBox.TitleElement = TitleElement;
        }

        if (YesText.HasValue() && bitMessageBox.HasNotBeenSet(nameof(YesText)))
        {
            bitMessageBox.YesText = YesText;
        }
    }
}
