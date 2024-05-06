namespace Bit.BlazorUI;

public partial class BitMessage
{
    private BitSeverity severity = BitSeverity.Info;


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
    /// The alias for ChildContent.
    /// </summary>
    [Parameter] public RenderFragment? Content { get; set; }

    /// <summary>
    /// Custom Fabric icon name to replace the dismiss icon. If unset, default will be the Fabric Cancel icon.
    /// </summary>
    [Parameter] public string DismissIconName { get; set; } = "Cancel";

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
    /// The severity of the message. defaults to Info.
    /// </summary>
    [Parameter]
    public BitSeverity Severity
    {
        get => severity;
        set
        {
            if (severity == value) return;

            severity = value;

            ClassBuilder.Reset();
        }
    }

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



    protected override string RootElementClass => "bit-msg";

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => IsEnabled is false ? string.Empty
                                    : Severity switch
                                    {
                                        BitSeverity.Info => "bit-msg-info",
                                        BitSeverity.Success => "bit-msg-success",
                                        BitSeverity.Warning => "bit-msg-warning",
                                        BitSeverity.SevereWarning => "bit-msg-severe-warning",
                                        BitSeverity.Error => "bit-msg-error",
                                        _ => "bit-msg-info"
                                    });
    }

    private void ToggleExpand() => _isExpanded = _isExpanded is false;

    private string GetTextRole() => Role ?? (severity is BitSeverity.Success or BitSeverity.Info ? "status" : "alert");

    private string GetIconName() => IconName ?? _IconMap[Severity];

    private static Dictionary<BitSeverity, string> _IconMap = new()
    {
        [BitSeverity.Info] = "Info",
        [BitSeverity.Success] = "Completed",
        [BitSeverity.Warning] = "Info",
        [BitSeverity.SevereWarning] = "Warning",
        [BitSeverity.Error] = "ErrorBadge",
    };
}
