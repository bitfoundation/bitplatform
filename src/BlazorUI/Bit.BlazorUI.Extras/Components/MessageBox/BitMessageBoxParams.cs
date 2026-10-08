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

        if (AutoFocus.HasValue)
        {
            bitMessageBox.TakeFromCascade(nameof(AutoFocus), AutoFocus.Value, static m => m.AutoFocus, static (m, v) => m.AutoFocus = v);
        }

        if (AutoLoading.HasValue)
        {
            bitMessageBox.TakeFromCascade(nameof(AutoLoading), AutoLoading.Value, static m => m.AutoLoading, static (m, v) => m.AutoLoading = v);
        }

        if (ButtonColor.HasValue)
        {
            bitMessageBox.TakeFromCascade(nameof(ButtonColor), ButtonColor.Value, static m => m.ButtonColor, static (m, v) => m.ButtonColor = v);
        }

        if (CancelText.HasValue())
        {
            bitMessageBox.TakeFromCascade(nameof(CancelText), CancelText, static m => m.CancelText, static (m, v) => m.CancelText = v);
        }

        if (Classes is not null)
        {
            bitMessageBox.TakeFromCascade(nameof(Classes), Classes, static m => m.Classes, static (m, v) => m.Classes = v);
        }

        if (CloseButtonTitle.HasValue())
        {
            bitMessageBox.TakeFromCascade(nameof(CloseButtonTitle), CloseButtonTitle, static m => m.CloseButtonTitle, static (m, v) => m.CloseButtonTitle = v);
        }

        if (CloseIcon is not null)
        {
            bitMessageBox.TakeFromCascade(nameof(CloseIcon), CloseIcon, static m => m.CloseIcon, static (m, v) => m.CloseIcon = v);
        }

        if (CloseIconName.HasValue())
        {
            bitMessageBox.TakeFromCascade(nameof(CloseIconName), CloseIconName, static m => m.CloseIconName, static (m, v) => m.CloseIconName = v);
        }

        if (DefaultButton.HasValue)
        {
            bitMessageBox.TakeFromCascade(nameof(DefaultButton), DefaultButton.Value, static m => m.DefaultButton, static (m, v) => m.DefaultButton = v);
        }

        if (HideIcon.HasValue)
        {
            bitMessageBox.TakeFromCascade(nameof(HideIcon), HideIcon.Value, static m => m.HideIcon, static (m, v) => m.HideIcon = v);
        }

        if (NoText.HasValue())
        {
            bitMessageBox.TakeFromCascade(nameof(NoText), NoText, static m => m.NoText, static (m, v) => m.NoText = v);
        }

        if (OkText.HasValue())
        {
            bitMessageBox.TakeFromCascade(nameof(OkText), OkText, static m => m.OkText, static (m, v) => m.OkText = v);
        }

        if (PrimaryButtonColor.HasValue)
        {
            bitMessageBox.TakeFromCascade(nameof(PrimaryButtonColor), PrimaryButtonColor.Value, static m => m.PrimaryButtonColor, static (m, v) => m.PrimaryButtonColor = v);
        }

        if (Reversed.HasValue)
        {
            bitMessageBox.TakeFromCascade(nameof(Reversed), Reversed.Value, static m => m.Reversed, static (m, v) => m.Reversed = v);
        }

        if (ShowCloseButton.HasValue)
        {
            bitMessageBox.TakeFromCascade(nameof(ShowCloseButton), ShowCloseButton.Value, static m => m.ShowCloseButton, static (m, v) => m.ShowCloseButton = v);
        }

        if (Size.HasValue)
        {
            bitMessageBox.TakeFromCascade(nameof(Size), Size.Value, static m => m.Size, static (m, v) => m.Size = v);
        }

        if (Styles is not null)
        {
            bitMessageBox.TakeFromCascade(nameof(Styles), Styles, static m => m.Styles, static (m, v) => m.Styles = v);
        }

        if (TitleElement.HasValue())
        {
            bitMessageBox.TakeFromCascade(nameof(TitleElement), TitleElement, static m => m.TitleElement, static (m, v) => m.TitleElement = v);
        }

        if (YesText.HasValue())
        {
            bitMessageBox.TakeFromCascade(nameof(YesText), YesText, static m => m.YesText, static (m, v) => m.YesText = v);
        }
    }
}
