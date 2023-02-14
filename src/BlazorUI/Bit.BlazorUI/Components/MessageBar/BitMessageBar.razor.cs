namespace Bit.BlazorUI;

public partial class BitMessageBar
{
    protected override bool UseVisual => false;

    private static Dictionary<BitMessageBarType, BitIconName> IconMap = new()
    {
        [BitMessageBarType.Info] = BitIconName.Info,
        [BitMessageBarType.Warning] = BitIconName.Info,
        [BitMessageBarType.Error] = BitIconName.ErrorBadge,
        [BitMessageBarType.Blocked] = BitIconName.Blocked2,
        [BitMessageBarType.SevereWarning] = BitIconName.Warning,
        [BitMessageBarType.Success] = BitIconName.Completed
    };

    private bool ExpandSingleLine;
    private BitIconName? messageBarIcon;
    private BitMessageBarType messageBarType = BitMessageBarType.Info;

    /// <summary>
    /// Determines if the message bar is multi lined. If false, and the text overflows over buttons or to another line, it is clipped
    /// </summary>
    [Parameter] public bool IsMultiline { get; set; } = true;

    /// <summary>
    /// The type of message bar to render
    /// </summary>
    [Parameter]
    public BitMessageBarType MessageBarType
    {
        get => messageBarType;
        set
        {
            messageBarType = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// Custom Fabric icon name to replace the dismiss icon. If unset, default will be the Fabric Clear icon
    /// </summary>
    [Parameter] public BitIconName DismissIconName { get; set; } = BitIconName.Clear;

    /// <summary>
    /// Custom icon to replace the message bar icon. If unset, default will be the icon set by messageBarType.
    /// </summary>
    [Parameter] public BitIconName? MessageBarIconName { get; set; }

    /// <summary>
    /// Determines if the message bar text is truncated. If true, a button will render to toggle between a single line view and multiline view. This parameter is for single line message bars with no buttons only in a limited space scenario
    /// </summary>
    [Parameter] public bool Truncated { get; set; }

    /// <summary>
    /// The content of message bar
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The content of the action to show on the message bar
    /// </summary>
    [Parameter] public RenderFragment? Actions { get; set; }

    /// <summary>
    /// Aria label on dismiss button if onDismiss is defined
    /// </summary>
    [Parameter] public string? DismissButtonAriaLabel { get; set; }

    /// <summary>
    /// Aria label on overflow button if truncated is true
    /// </summary>
    [Parameter] public string? OverflowButtonAriaLabel { get; set; }

    /// <summary>
    /// Custom role to apply to the message bar
    /// </summary>
    [Parameter] public string? Role { get; set; }

    /// <summary>
    /// Whether the message bar has a dismiss button and its callback. If null, dismiss button won't show
    /// </summary>
    [Parameter] public EventCallback OnDismiss { get; set; }


    public string LabelId => $"MessageBar-Label-{UniqueId}";

    protected override string RootElementClass => "bit-msb";

    protected override void RegisterComponentClasses()
    {
        ClassBuilder.Register(() => IsEnabled is false
                                    ? string.Empty
                                    : MessageBarType switch
                                    {
                                        BitMessageBarType.Info => "info",
                                        BitMessageBarType.Warning => "warning",
                                        BitMessageBarType.Error => "error",
                                        BitMessageBarType.Blocked => "blocked",
                                        BitMessageBarType.SevereWarning => "severe-warning",
                                        _ => "success"
                                    });
    }

    protected override Task OnParametersSetAsync()
    {
        messageBarIcon = MessageBarIconName ?? IconMap[MessageBarType];

        return base.OnParametersSetAsync();
    }

    private void ToggleExpandSingleLine() => ExpandSingleLine = !ExpandSingleLine;

    private string GetTextRole()
    {
        return messageBarType switch
        {
            BitMessageBarType.Error or BitMessageBarType.Blocked or BitMessageBarType.SevereWarning => "alert",
            _ => "status",
        };
    }

    private string GetAnnouncementPriority()
    {
        return messageBarType switch
        {
            BitMessageBarType.Blocked or BitMessageBarType.Error or BitMessageBarType.SevereWarning => "assertive",
            _ => "polite",
        };
    }

    private bool HasDismiss { get => (OnDismiss.HasDelegate); }
    private string? AriaDescribedby => Actions is not null || HasDismiss ? LabelId : null;
    private string? RootElementRole => Actions is not null || HasDismiss ? "region" : null;
}
