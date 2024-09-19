using System.Drawing;

namespace Bit.BlazorUI;

public partial class BitMessage : BitComponentBase
{
    private bool _isExpanded;



    /// <summary>
    /// The content of the action to show on the message.
    /// </summary>
    [Parameter] public RenderFragment? Actions { get; set; }

    /// <summary>
    /// The content of message.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitMessage.
    /// </summary>
    [Parameter] public BitMessageClassStyles? Classes { get; set; }

    /// <summary>
    /// Custom Fabric icon name for the collapse icon in Truncate mode. If unset, default will be the Fabric DoubleChevronUp icon.
    /// </summary>
    [Parameter] public string CollapseIconName { get; set; } = "DoubleChevronUp";

    /// <summary>
    /// The general color of the message.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColor? Color { get; set; }

    /// <summary>
    /// The alias for ChildContent.
    /// </summary>
    [Parameter] public RenderFragment? Content { get; set; }

    /// <summary>
    /// Determines the alignment of the content section of the message.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public BitAlignment? ContentAlignment { get; set; }

    /// <summary>
    /// Custom Fabric icon name to replace the dismiss icon. If unset, default will be the Fabric Cancel icon.
    /// </summary>
    [Parameter] public string DismissIconName { get; set; } = "Cancel";

    /// <summary>
    /// Determines the elevation of the message, a scale from 1 to 24.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public int? Elevation { get; set; }

    /// <summary>
    /// Custom Fabric icon name for the expand icon in Truncate mode. If unset, default will be the Fabric DoubleChevronDown icon.
    /// </summary>
    [Parameter] public string ExpandIconName { get; set; } = "DoubleChevronDown";

    /// <summary>
    /// Prevents rendering the icon of the message.
    /// </summary>
    [Parameter] public bool HideIcon { get; set; }

    /// <summary>
    /// Custom icon to replace the message icon. If unset, default will be the icon set by Type.
    /// </summary>
    [Parameter] public string? IconName { get; set; }

    /// <summary>
    /// Determines if the message is multi-lined. If false, and the text overflows over buttons or to another line, it is clipped.
    /// </summary>
    [Parameter] public bool Multiline { get; set; }

    /// <summary>
    ///  Whether the message has a dismiss button and its callback. If null, dismiss button won't show.
    /// </summary>
    [Parameter] public EventCallback OnDismiss { get; set; }

    /// <summary>
    /// Custom role to apply to the message text.
    /// </summary>
    [Parameter] public string? Role { get; set; }

    /// <summary>
    /// The size of Message, Possible values: Small | Medium | Large
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSize? Size { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitMessage.
    /// </summary>
    [Parameter] public BitMessageClassStyles? Styles { get; set; }

    /// <summary>
    /// Determines if the message text is truncated.
    /// If true, a button will render to toggle between a single line view and multiline view.
    /// This parameter is for single line messages with no buttons only in a limited space scenario.
    /// </summary>
    [Parameter] public bool Truncate { get; set; }

    /// <summary>
    /// The variant of the message.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitVariant? Variant { get; set; }



    protected override string RootElementClass => "bit-msg";

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        StyleBuilder.Register(() => ContentAlignment switch
        {
            BitAlignment.Start => "--bit-msg-con:flex-start",
            BitAlignment.End => "--bit-msg-con:flex-end",
            BitAlignment.Center => "--bit-msg-con:center",
            BitAlignment.SpaceBetween => "--bit-msg-con:space-between",
            BitAlignment.SpaceAround => "--bit-msg-con:space-around",
            BitAlignment.SpaceEvenly => "--bit-msg-con:space-evenly",
            BitAlignment.Baseline => "--bit-msg-con:baseline",
            BitAlignment.Stretch => "--bit-msg-con:stretch",
            _ => "--bit-msg-con:flex-start"
        });

        StyleBuilder.Register(() => Elevation is > 0 or < 25 ? $"--bit-msg-elv:var(--bit-shd-{Elevation.Value})" : string.Empty);
    }

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => Variant switch
        {
            BitVariant.Fill => "bit-msg-fil",
            BitVariant.Outline => "bit-msg-otl",
            BitVariant.Text => "bit-msg-txt",
            _ => "bit-msg-fil"
        });

        ClassBuilder.Register(() => Color switch
        {
            BitColor.Primary => "bit-msg-pri",
            BitColor.Secondary => "bit-msg-sec",
            BitColor.Tertiary => "bit-msg-ter",
            BitColor.Info => "bit-msg-inf",
            BitColor.Success => "bit-msg-suc",
            BitColor.Warning => "bit-msg-wrn",
            BitColor.SevereWarning => "bit-msg-swr",
            BitColor.Error => "bit-msg-err",
            BitColor.PrimaryBackground => "bit-msg-pbg",
            BitColor.SecondaryBackground => "bit-msg-sbg",
            BitColor.TertiaryBackground => "bit-msg-tbg",
            BitColor.PrimaryForeground => "bit-msg-pfg",
            BitColor.SecondaryForeground => "bit-msg-sfg",
            BitColor.TertiaryForeground => "bit-msg-tfg",
            BitColor.PrimaryBorder => "bit-msg-pbr",
            BitColor.SecondaryBorder => "bit-msg-sbr",
            BitColor.TertiaryBorder => "bit-msg-tbr",
            _ => "bit-msg-inf"
        });

        ClassBuilder.Register(() => Size switch
        {
            BitSize.Small => "bit-msg-sm",
            BitSize.Medium => "bit-msg-md",
            BitSize.Large => "bit-msg-lg",
            _ => "bit-msg-md"
        });
    }

    private void ToggleExpand() => _isExpanded = _isExpanded is false;

    private string GetTextRole() => Role ?? (Color is BitColor.Success or BitColor.Info ? "status" : "alert");

    private string GetIconName() => IconName ?? _IconMap[Color ?? BitColor.Info];

    private static Dictionary<BitColor, string> _IconMap = new()
    {
        [BitColor.Primary] = "Info",
        [BitColor.Secondary] = "Info",
        [BitColor.Tertiary] = "Info",
        [BitColor.Info] = "Info",
        [BitColor.Success] = "Completed",
        [BitColor.Warning] = "Info",
        [BitColor.SevereWarning] = "Warning",
        [BitColor.Error] = "ErrorBadge",
        [BitColor.PrimaryBackground] = "Info",
        [BitColor.SecondaryBackground] = "Info",
        [BitColor.TertiaryBackground] = "Info",
        [BitColor.PrimaryForeground] = "Info",
        [BitColor.SecondaryForeground] = "Info",
        [BitColor.TertiaryForeground] = "Info",
        [BitColor.PrimaryBorder] = "Info",
        [BitColor.SecondaryBorder] = "Info",
        [BitColor.TertiaryBorder] = "Info"
    };
}
