namespace Bit.BlazorUI;

public partial class BitMessageBar
{
    private static Dictionary<BitMessageBarType, string> _IconMap = new()
    {
        [BitMessageBarType.Info] = "Info",
        [BitMessageBarType.Warning] = "Info",
        [BitMessageBarType.Error] = "ErrorBadge",
        [BitMessageBarType.Blocked] = "Blocked2",
        [BitMessageBarType.SevereWarning] = "Warning",
        [BitMessageBarType.Success] = "Completed",
    };

    private bool _isExpanded;
    private string? _messageBarIconName;
    private BitMessageBarType _messageBarType = BitMessageBarType.Info;

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
        get => _messageBarType;
        set
        {
            _messageBarType = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// Custom Fabric icon name to replace the dismiss icon. If unset, default will be the Fabric Clear icon
    /// </summary>
    [Parameter] public string DismissIconName { get; set; } = "Cancel";

    /// <summary>
    /// Custom icon to replace the message bar icon. If unset, default will be the icon set by messageBarType.
    /// </summary>
    [Parameter] public string? MessageBarIconName { get; set; }

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

    /// <summary>
    /// Prevents rendering the icon of the message bar.
    /// </summary>
    [Parameter] public bool HideIcon { get; set; }


    public string LabelId => $"MessageBar-Label-{UniqueId}";

    protected override string RootElementClass => "bit-msb";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => IsEnabled is false
                                    ? string.Empty
                                    : MessageBarType switch
                                    {
                                        BitMessageBarType.Info => $"{RootElementClass}-info",
                                        BitMessageBarType.Success => $"{RootElementClass}-success",
                                        BitMessageBarType.Warning => $"{RootElementClass}-warning",
                                        BitMessageBarType.SevereWarning => $"{RootElementClass}-severe-warning",
                                        BitMessageBarType.Error => $"{RootElementClass}-error",
                                        BitMessageBarType.Blocked => $"{RootElementClass}-blocked",
                                        _ => $"{RootElementClass}-info"
                                    });
    }

    protected override Task OnParametersSetAsync()
    {
        _messageBarIconName = MessageBarIconName ?? _IconMap[MessageBarType];

        return base.OnParametersSetAsync();
    }

    private void ToggleExpand() => _isExpanded = !_isExpanded;

    private string GetTextRole()
    {
        return _messageBarType switch
        {
            BitMessageBarType.Error or BitMessageBarType.Blocked or BitMessageBarType.SevereWarning => "alert",
            _ => "status",
        };
    }

    private string GetAnnouncementPriority()
    {
        return _messageBarType switch
        {
            BitMessageBarType.Blocked or BitMessageBarType.Error or BitMessageBarType.SevereWarning => "assertive",
            _ => "polite",
        };
    }

    private bool HasDismiss { get => (OnDismiss.HasDelegate); }
    private string? AriaDescribedby => Actions is not null || HasDismiss ? LabelId : null;
    private string? RootElementRole => Actions is not null || HasDismiss ? "region" : null;
}
