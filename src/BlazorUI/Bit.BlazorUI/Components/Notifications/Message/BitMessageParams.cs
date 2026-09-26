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

        if (Alignment.HasValue && bitMessage.HasNotBeenSet(nameof(Alignment)))
        {
            bitMessage.Alignment = Alignment.Value;

            bitMessage.StyleBuilder.Reset();
        }

        if (AutoDismissTime.HasValue && bitMessage.HasNotBeenSet(nameof(AutoDismissTime)))
        {
            bitMessage.AutoDismissTime = AutoDismissTime.Value;
        }

        if (Classes is not null && bitMessage.HasNotBeenSet(nameof(Classes)))
        {
            bitMessage.Classes = Classes;

            bitMessage.ClassBuilder.Reset();
        }

        if (CollapseAriaLabel.HasValue() && bitMessage.HasNotBeenSet(nameof(CollapseAriaLabel)))
        {
            bitMessage.CollapseAriaLabel = CollapseAriaLabel!;
        }

        if (CollapseIcon is not null && bitMessage.HasNotBeenSet(nameof(CollapseIcon)))
        {
            bitMessage.CollapseIcon = CollapseIcon;
        }

        if (CollapseIconName.HasValue() && bitMessage.HasNotBeenSet(nameof(CollapseIconName)))
        {
            bitMessage.CollapseIconName = CollapseIconName;
        }

        if (Color.HasValue && bitMessage.HasNotBeenSet(nameof(Color)))
        {
            bitMessage.Color = Color.Value;

            bitMessage.ClassBuilder.Reset();
        }

        if (DelayedAnnouncement.HasValue && bitMessage.HasNotBeenSet(nameof(DelayedAnnouncement)))
        {
            bitMessage.DelayedAnnouncement = DelayedAnnouncement.Value;
        }

        if (DismissAriaLabel.HasValue() && bitMessage.HasNotBeenSet(nameof(DismissAriaLabel)))
        {
            bitMessage.DismissAriaLabel = DismissAriaLabel!;
        }

        if (Dismissible.HasValue && bitMessage.HasNotBeenSet(nameof(Dismissible)))
        {
            bitMessage.Dismissible = Dismissible.Value;
        }

        if (DismissIcon is not null && bitMessage.HasNotBeenSet(nameof(DismissIcon)))
        {
            bitMessage.DismissIcon = DismissIcon;
        }

        if (DismissIconName.HasValue() && bitMessage.HasNotBeenSet(nameof(DismissIconName)))
        {
            bitMessage.DismissIconName = DismissIconName;
        }

        if (DismissOnEscape.HasValue && bitMessage.HasNotBeenSet(nameof(DismissOnEscape)))
        {
            bitMessage.DismissOnEscape = DismissOnEscape.Value;
        }

        if (Elevation.HasValue && bitMessage.HasNotBeenSet(nameof(Elevation)))
        {
            bitMessage.Elevation = Elevation.Value;

            bitMessage.StyleBuilder.Reset();
        }

        if (ExpandAriaLabel.HasValue() && bitMessage.HasNotBeenSet(nameof(ExpandAriaLabel)))
        {
            bitMessage.ExpandAriaLabel = ExpandAriaLabel!;
        }

        if (ExpandIcon is not null && bitMessage.HasNotBeenSet(nameof(ExpandIcon)))
        {
            bitMessage.ExpandIcon = ExpandIcon;
        }

        if (ExpandIconName.HasValue() && bitMessage.HasNotBeenSet(nameof(ExpandIconName)))
        {
            bitMessage.ExpandIconName = ExpandIconName;
        }

        if (HideIcon.HasValue && bitMessage.HasNotBeenSet(nameof(HideIcon)))
        {
            bitMessage.HideIcon = HideIcon.Value;
        }

        if (Icon is not null && bitMessage.HasNotBeenSet(nameof(Icon)))
        {
            bitMessage.Icon = Icon;
        }

        if (IconName.HasValue() && bitMessage.HasNotBeenSet(nameof(IconName)))
        {
            bitMessage.IconName = IconName;
        }

        if (MaxLines.HasValue && bitMessage.HasNotBeenSet(nameof(MaxLines)))
        {
            bitMessage.MaxLines = MaxLines.Value;

            bitMessage.StyleBuilder.Reset();
        }

        if (Multiline.HasValue && bitMessage.HasNotBeenSet(nameof(Multiline)))
        {
            bitMessage.Multiline = Multiline.Value;

            bitMessage.StyleBuilder.Reset();
        }

        if (ShowAutoDismissProgress.HasValue && bitMessage.HasNotBeenSet(nameof(ShowAutoDismissProgress)))
        {
            bitMessage.ShowAutoDismissProgress = ShowAutoDismissProgress.Value;
        }

        if (Size.HasValue && bitMessage.HasNotBeenSet(nameof(Size)))
        {
            bitMessage.Size = Size.Value;

            bitMessage.ClassBuilder.Reset();
        }

        if (Square.HasValue && bitMessage.HasNotBeenSet(nameof(Square)))
        {
            bitMessage.Square = Square.Value;

            bitMessage.ClassBuilder.Reset();
        }

        if (Styles is not null && bitMessage.HasNotBeenSet(nameof(Styles)))
        {
            bitMessage.Styles = Styles;

            bitMessage.StyleBuilder.Reset();
        }

        if (TitleElement.HasValue() && bitMessage.HasNotBeenSet(nameof(TitleElement)))
        {
            bitMessage.TitleElement = TitleElement;
        }

        if (Truncate.HasValue && bitMessage.HasNotBeenSet(nameof(Truncate)))
        {
            bitMessage.Truncate = Truncate.Value;
        }

        if (Variant.HasValue && bitMessage.HasNotBeenSet(nameof(Variant)))
        {
            bitMessage.Variant = Variant.Value;

            bitMessage.ClassBuilder.Reset();
        }
    }
}
