namespace Bit.BlazorUI;

/// <summary>
/// The set of parameters a <see cref="BitMessageBox"/> shown through the
/// <see cref="BitMessageBoxService"/> is customized with.
/// </summary>
/// <remarks>
/// Every member is nullable and <c>null</c> means "not set": the corresponding
/// <see cref="BitMessageBox"/> default stands. <see cref="Modal"/> is the way down to the
/// <see cref="BitModal"/> the box is shown in - the values the service works out for it (the accessible
/// name, the described-by, the alert role of an urgent message) are only defaults, and anything set
/// there wins over them.
/// </remarks>
public class BitMessageBoxParameters
{
    /// <summary>
    /// Moves the focus onto the default action button once the message box is rendered.
    /// Defaults to <c>true</c> for a message box shown through the service.
    /// </summary>
    public bool? AutoFocus { get; set; }

    /// <summary>
    /// Enables the loading state of the action button that was pressed for as long as its callback runs.
    /// </summary>
    public bool? AutoLoading { get; set; }

    /// <summary>
    /// The body of the message box.
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// The template of the body of the message box, which takes the place of <see cref="Body"/>.
    /// </summary>
    public RenderFragment? BodyTemplate { get; set; }

    /// <summary>
    /// The set of buttons the message box renders in its footer.
    /// </summary>
    public BitMessageBoxButtons? Buttons { get; set; }

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
    /// The title (and aria-label) of the close button, for accessibility and localization.
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
    /// The general color of the message box, which is the severity of its message.
    /// </summary>
    public BitColor? Color { get; set; }

    /// <summary>
    /// The action button the focus is moved onto.
    /// </summary>
    public BitMessageBoxResult? DefaultButton { get; set; }

    /// <summary>
    /// The general directionality of the message box and of the modal it is shown in.
    /// </summary>
    public BitDir? Dir { get; set; }

    /// <summary>
    /// The template used to render the footer of the message box, which takes the place of its action buttons.
    /// </summary>
    /// <remarks>
    /// The controls in it are the caller's own, so nothing in them answers the showing: only the close
    /// button still does, with <see cref="BitMessageBoxResult.None"/>. Show the message box through the
    /// <see cref="BitModalService"/> directly where a footer of your own has to close the modal with an
    /// answer of its own.
    /// </remarks>
    public RenderFragment? FooterTemplate { get; set; }

    /// <summary>
    /// The template used to render the header of the message box.
    /// </summary>
    public RenderFragment? HeaderTemplate { get; set; }

    /// <summary>
    /// Removes the leading icon of the message box.
    /// </summary>
    public bool? HideIcon { get; set; }

    /// <summary>
    /// The leading icon of the message box, provided as custom CSS classes of an external icon library.
    /// </summary>
    public BitIconInfo? Icon { get; set; }

    /// <summary>
    /// The accessible name of the leading icon.
    /// </summary>
    public string? IconAriaLabel { get; set; }

    /// <summary>
    /// The name of the leading icon of the message box, from the built-in Fluent UI icons.
    /// </summary>
    public string? IconName { get; set; }

    /// <summary>
    /// The template used to render the leading icon of the message box.
    /// </summary>
    public RenderFragment? IconTemplate { get; set; }

    /// <summary>
    /// The id of the rendered message box.
    /// </summary>
    /// <remarks>
    /// The service generates one when none is given, since the ids of the title and the body are derived
    /// from it and they are what the modal points its accessible name and description at.
    /// </remarks>
    public string? Id { get; set; }

    /// <summary>
    /// The parameters of the <see cref="BitModal"/> the message box is shown in.
    /// </summary>
    public BitModalParameters? Modal { get; set; }

    /// <summary>
    /// The text of the No button.
    /// </summary>
    public string? NoText { get; set; }

    /// <summary>
    /// The text of the Ok button.
    /// </summary>
    public string? OkText { get; set; }

    /// <summary>
    /// Keeps the message box alive through the lifecycle of the application until it is closed, rather than
    /// only for as long as the modal container that renders it.
    /// </summary>
    public bool? Persistent { get; set; }

/// <summary>
    /// The color of the affirmative action button (Ok, or Yes), which falls back to ButtonColor.
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
    /// The title of the message box.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// The text of the Yes button.
    /// </summary>
    public string? YesText { get; set; }
}
