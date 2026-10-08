namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitMessage"/> component.
/// </summary>
/// <remarks>
/// It carries the look and the behavior shared by the messages under a <see cref="BitParams"/>, not what belongs
/// to one of them: the content (<see cref="BitMessage.ChildContent"/>, <see cref="BitMessage.Content"/>,
/// <see cref="BitMessage.Title"/>, <see cref="BitMessage.TitleTemplate"/>, <see cref="BitMessage.Actions"/> and
/// <see cref="BitMessage.IconTemplate"/>), the state (<see cref="BitMessage.Dismissed"/> and
/// <see cref="BitMessage.Expanded"/>), the callbacks (<see cref="BitMessage.OnDismiss"/> and
/// <see cref="BitMessage.OnDismissing"/>) and what a message says about itself
/// (<see cref="BitMessage.AutoFocus"/>, <see cref="BitMessage.IconAriaLabel"/>, <see cref="BitMessage.Politeness"/>
/// and <see cref="BitMessage.Role"/>) are left off, since they are the business of the message they belong to.
/// </remarks>
public class BitMessageParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitMessage"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitMessage value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitMessage)}";



    public string Name => ParamName;



    /// <summary>
    /// Determines the alignment of the content section of the message.
    /// </summary>
    public BitAlignment? Alignment { get; set; }

    /// <summary>
    /// Enables the auto-dismiss feature and sets the time to automatically dismiss the message.
    /// </summary>
    public TimeSpan? AutoDismissTime { get; set; }

    /// <summary>
    /// Switches a single-line message to the Multiline layout for as long as its content does not fit on one line.
    /// </summary>
    public bool? AutoMultiline { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the message.
    /// </summary>
    public BitMessageClassStyles? Classes { get; set; }

    /// <summary>
    /// The aria-label and the tooltip of the expander button of the message in Truncate mode while it is expanded.
    /// </summary>
    public string? CollapseAriaLabel { get; set; }

    /// <summary>
    /// The icon for the collapse button in Truncate mode using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="CollapseIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? CollapseIcon { get; set; }

    /// <summary>
    /// The name of the collapse icon in Truncate mode from the built-in Fluent UI icons.
    /// </summary>
    public string? CollapseIconName { get; set; }

    /// <summary>
    /// The general color of the message.
    /// </summary>
    public BitColor? Color { get; set; }

    /// <summary>
    /// Holds the content of the message back for one render, so its live region is already on the page when the
    /// text lands in it.
    /// </summary>
    public bool? DelayedAnnouncement { get; set; }

    /// <summary>
    /// The aria-label and the tooltip of the dismiss button of the message.
    /// </summary>
    public string? DismissAriaLabel { get; set; }

    /// <summary>
    /// Renders the dismiss button and lets the message dismiss itself.
    /// </summary>
    public bool? Dismissible { get; set; }

    /// <summary>
    /// The icon for the dismiss button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="DismissIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? DismissIcon { get; set; }

    /// <summary>
    /// The name of the dismiss icon from the built-in Fluent UI icons.
    /// </summary>
    public string? DismissIconName { get; set; }

    /// <summary>
    /// Dismisses the message when the Escape key is pressed while the focus is inside it.
    /// </summary>
    public bool? DismissOnEscape { get; set; }

    /// <summary>
    /// Determines the elevation of the message, a scale from 1 to 24.
    /// </summary>
    public int? Elevation { get; set; }

    /// <summary>
    /// The aria-label and the tooltip of the expander button of the message in Truncate mode while it is collapsed.
    /// </summary>
    public string? ExpandAriaLabel { get; set; }

    /// <summary>
    /// The icon for the expand button in Truncate mode using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="ExpandIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? ExpandIcon { get; set; }

    /// <summary>
    /// The name of the expand icon in Truncate mode from the built-in Fluent UI icons.
    /// </summary>
    public string? ExpandIconName { get; set; }

    /// <summary>
    /// Prevents rendering the icon of the message.
    /// </summary>
    public bool? HideIcon { get; set; }

    /// <summary>
    /// The icon of the message using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="IconName"/> when both are set.
    /// </summary>
    /// <remarks>
    /// It replaces the icon every color picks for itself, so leave it unset where the messages under the
    /// <see cref="BitParams"/> do not all report the same kind of thing.
    /// </remarks>
    public BitIconInfo? Icon { get; set; }

    /// <summary>
    /// The name of the icon of the message from the built-in Fluent UI icons.
    /// </summary>
    /// <remarks>
    /// It replaces the icon every color picks for itself, so leave it unset where the messages under the
    /// <see cref="BitParams"/> do not all report the same kind of thing.
    /// </remarks>
    public string? IconName { get; set; }

    /// <summary>
    /// Caps how many lines the content of the message may wrap over before it is clipped, in Multiline mode.
    /// </summary>
    public int? MaxLines { get; set; }

    /// <summary>
    /// Determines if the message is multi-lined.
    /// </summary>
    public bool? Multiline { get; set; }

    /// <summary>
    /// Holds the AutoDismissTime countdown while the page is hidden.
    /// </summary>
    public bool? PauseOnPageHidden { get; set; }

    /// <summary>
    /// Holds the AutoDismissTime countdown while the window does not have the focus.
    /// </summary>
    public bool? PauseOnWindowBlur { get; set; }

    /// <summary>
    /// Renders a bar along the bottom edge of the message that runs down as its AutoDismissTime does.
    /// </summary>
    public bool? ShowAutoDismissProgress { get; set; }

    /// <summary>
    /// The size of the message.
    /// </summary>
    public BitSize? Size { get; set; }

    /// <summary>
    /// Removes the rounded corners of the message so it can sit flush against the edges of its container as a banner.
    /// </summary>
    public bool? Square { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the message.
    /// </summary>
    public BitMessageClassStyles? Styles { get; set; }

    /// <summary>
    /// Washes the surface of an Outline or a Text message with a faint tint of its color.
    /// </summary>
    public bool? Tinted { get; set; }

    /// <summary>
    /// The HTML element the title of the message is rendered as, e.g. the heading level that fits the outline of
    /// the page the messages are on.
    /// </summary>
    public string? TitleElement { get; set; }

    /// <summary>
    /// Determines if the message text is truncated, with a button that unfolds it.
    /// </summary>
    public bool? Truncate { get; set; }

    /// <summary>
    /// The variant of the message.
    /// </summary>
    public BitVariant? Variant { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitMessage"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitMessage"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitMessage"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitMessage"/>.
    /// </remarks>
    /// <param name="bitMessage">
    /// The <see cref="BitMessage"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitMessage bitMessage)
    {
        if (bitMessage is null) return;

        UpdateBaseParameters(bitMessage);

        if (Alignment.HasValue)
        {
            bitMessage.TakeFromCascade(nameof(Alignment), Alignment.Value, static m => m.Alignment, static (m, v) => m.Alignment = v);
        }

        if (AutoDismissTime.HasValue)
        {
            bitMessage.TakeFromCascade(nameof(AutoDismissTime), AutoDismissTime.Value, static m => m.AutoDismissTime, static (m, v) => m.AutoDismissTime = v);
        }

        if (AutoMultiline.HasValue)
        {
            bitMessage.TakeFromCascade(nameof(AutoMultiline), AutoMultiline.Value, static m => m.AutoMultiline, static (m, v) => m.AutoMultiline = v);
        }

        if (Classes is not null)
        {
            bitMessage.TakeFromCascade(nameof(Classes), Classes, static m => m.Classes, static (m, v) => m.Classes = v);
        }

        if (CollapseAriaLabel.HasValue())
        {
            bitMessage.TakeFromCascade(nameof(CollapseAriaLabel), CollapseAriaLabel!, static m => m.CollapseAriaLabel, static (m, v) => m.CollapseAriaLabel = v);
        }

        if (CollapseIcon is not null)
        {
            bitMessage.TakeFromCascade(nameof(CollapseIcon), CollapseIcon, static m => m.CollapseIcon, static (m, v) => m.CollapseIcon = v);
        }

        if (CollapseIconName.HasValue())
        {
            bitMessage.TakeFromCascade(nameof(CollapseIconName), CollapseIconName, static m => m.CollapseIconName, static (m, v) => m.CollapseIconName = v);
        }

        if (Color.HasValue)
        {
            bitMessage.TakeFromCascade(nameof(Color), Color.Value, static m => m.Color, static (m, v) => m.Color = v);
        }

        if (DelayedAnnouncement.HasValue)
        {
            bitMessage.TakeFromCascade(nameof(DelayedAnnouncement), DelayedAnnouncement.Value, static m => m.DelayedAnnouncement, static (m, v) => m.DelayedAnnouncement = v);
        }

        if (DismissAriaLabel.HasValue())
        {
            bitMessage.TakeFromCascade(nameof(DismissAriaLabel), DismissAriaLabel!, static m => m.DismissAriaLabel, static (m, v) => m.DismissAriaLabel = v);
        }

        if (Dismissible.HasValue)
        {
            bitMessage.TakeFromCascade(nameof(Dismissible), Dismissible.Value, static m => m.Dismissible, static (m, v) => m.Dismissible = v);
        }

        if (DismissIcon is not null)
        {
            bitMessage.TakeFromCascade(nameof(DismissIcon), DismissIcon, static m => m.DismissIcon, static (m, v) => m.DismissIcon = v);
        }

        if (DismissIconName.HasValue())
        {
            bitMessage.TakeFromCascade(nameof(DismissIconName), DismissIconName, static m => m.DismissIconName, static (m, v) => m.DismissIconName = v);
        }

        if (DismissOnEscape.HasValue)
        {
            bitMessage.TakeFromCascade(nameof(DismissOnEscape), DismissOnEscape.Value, static m => m.DismissOnEscape, static (m, v) => m.DismissOnEscape = v);
        }

        if (Elevation.HasValue)
        {
            bitMessage.TakeFromCascade(nameof(Elevation), Elevation.Value, static m => m.Elevation, static (m, v) => m.Elevation = v);
        }

        if (ExpandAriaLabel.HasValue())
        {
            bitMessage.TakeFromCascade(nameof(ExpandAriaLabel), ExpandAriaLabel!, static m => m.ExpandAriaLabel, static (m, v) => m.ExpandAriaLabel = v);
        }

        if (ExpandIcon is not null)
        {
            bitMessage.TakeFromCascade(nameof(ExpandIcon), ExpandIcon, static m => m.ExpandIcon, static (m, v) => m.ExpandIcon = v);
        }

        if (ExpandIconName.HasValue())
        {
            bitMessage.TakeFromCascade(nameof(ExpandIconName), ExpandIconName, static m => m.ExpandIconName, static (m, v) => m.ExpandIconName = v);
        }

        if (HideIcon.HasValue)
        {
            bitMessage.TakeFromCascade(nameof(HideIcon), HideIcon.Value, static m => m.HideIcon, static (m, v) => m.HideIcon = v);
        }

        if (Icon is not null)
        {
            bitMessage.TakeFromCascade(nameof(Icon), Icon, static m => m.Icon, static (m, v) => m.Icon = v);
        }

        if (IconName.HasValue())
        {
            bitMessage.TakeFromCascade(nameof(IconName), IconName, static m => m.IconName, static (m, v) => m.IconName = v);
        }

        if (MaxLines.HasValue)
        {
            bitMessage.TakeFromCascade(nameof(MaxLines), MaxLines.Value, static m => m.MaxLines, static (m, v) => m.MaxLines = v);
        }

        if (Multiline.HasValue)
        {
            bitMessage.TakeFromCascade(nameof(Multiline), Multiline.Value, static m => m.Multiline, static (m, v) => m.Multiline = v);
        }

        if (PauseOnPageHidden.HasValue)
        {
            bitMessage.TakeFromCascade(nameof(PauseOnPageHidden), PauseOnPageHidden.Value, static m => m.PauseOnPageHidden, static (m, v) => m.PauseOnPageHidden = v);
        }

        if (PauseOnWindowBlur.HasValue)
        {
            bitMessage.TakeFromCascade(nameof(PauseOnWindowBlur), PauseOnWindowBlur.Value, static m => m.PauseOnWindowBlur, static (m, v) => m.PauseOnWindowBlur = v);
        }

        if (ShowAutoDismissProgress.HasValue)
        {
            bitMessage.TakeFromCascade(nameof(ShowAutoDismissProgress), ShowAutoDismissProgress.Value, static m => m.ShowAutoDismissProgress, static (m, v) => m.ShowAutoDismissProgress = v);
        }

        if (Size.HasValue)
        {
            bitMessage.TakeFromCascade(nameof(Size), Size.Value, static m => m.Size, static (m, v) => m.Size = v);
        }

        if (Square.HasValue)
        {
            bitMessage.TakeFromCascade(nameof(Square), Square.Value, static m => m.Square, static (m, v) => m.Square = v);
        }

        if (Styles is not null)
        {
            bitMessage.TakeFromCascade(nameof(Styles), Styles, static m => m.Styles, static (m, v) => m.Styles = v);
        }

        if (Tinted.HasValue)
        {
            bitMessage.TakeFromCascade(nameof(Tinted), Tinted.Value, static m => m.Tinted, static (m, v) => m.Tinted = v);
        }

        if (TitleElement.HasValue())
        {
            bitMessage.TakeFromCascade(nameof(TitleElement), TitleElement, static m => m.TitleElement, static (m, v) => m.TitleElement = v);
        }

        if (Truncate.HasValue)
        {
            bitMessage.TakeFromCascade(nameof(Truncate), Truncate.Value, static m => m.Truncate, static (m, v) => m.Truncate = v);
        }

        if (Variant.HasValue)
        {
            bitMessage.TakeFromCascade(nameof(Variant), Variant.Value, static m => m.Variant, static (m, v) => m.Variant = v);
        }
    }
}
