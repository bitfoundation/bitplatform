namespace Bit.BlazorUI;

/// <summary>
/// A Message displays errors, warnings, or important information. For example, if a file failed to upload an error message should appear.
/// </summary>
public partial class BitMessage : BitComponentBase
{
    private bool _isExpanded;
    private CancellationTokenSource? _autoDismissCts;



    /// <summary>
    /// The content of the action to show on the message.
    /// </summary>
    [Parameter] public RenderFragment? Actions { get; set; }

    /// <summary>
    /// Determines the alignment of the content section of the message.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public BitAlignment? Alignment { get; set; }

    /// <summary>
    /// Enables the auto-dismiss feature and sets the time to automatically call the OnDismiss callback.
    /// </summary>
    [Parameter] public TimeSpan? AutoDismissTime { get; set; }

    /// <summary>
    /// The content of message.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitMessage.
    /// </summary>
    [Parameter] public BitMessageClassStyles? Classes { get; set; }

    /// <summary>
    /// Gets or sets the icon for the collapse button in Truncate mode using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="CollapseIconName"/> when both are set.
    /// </summary>
    /// <remarks>
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="CollapseIconName"/> instead.
    /// </remarks>
    /// <example>
    /// Bootstrap: <c>CollapseIcon="BitIconInfo.Bi(\"gear-fill\")"</c>
    /// FontAwesome: <c>CollapseIcon="BitIconInfo.Fa(\"solid house\")"</c>
    /// Custom CSS: <c>CollapseIcon="BitIconInfo.Css(\"my-icon-class\")"</c>
    /// </example>
    [Parameter] public BitIconInfo? CollapseIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the collapse icon in Truncate mode from the built-in Fluent UI icons.
    /// </summary>
    /// <remarks>
    /// The icon name should be from the Fluent UI icon set (e.g., <c>BitIconName.ChevronUp</c>).
    /// <br />
    /// Browse available names in <c>BitIconName</c> of the <c>Bit.BlazorUI.Icons</c> nuget package or the gallery:
    /// <see href="https://blazorui.bitplatform.dev/iconography"/>.
    /// <br />
    /// For external icon libraries, use <see cref="CollapseIcon"/> instead.
    /// </remarks>
    [Parameter] public string? CollapseIconName { get; set; }

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
    /// The icon for the dismiss button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="DismissIconName"/> when both are set.
    /// </summary>
    /// <remarks>
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="DismissIconName"/> instead.
    /// </remarks>
    /// <example>
    /// Bootstrap: <c>DismissIcon="BitIconInfo.Bi(\"x-circle-fill\")"</c>
    /// FontAwesome: <c>DismissIcon="BitIconInfo.Fa(\"solid xmark\")"</c>
    /// Custom CSS: <c>DismissIcon="BitIconInfo.Css(\"my-dismiss-icon\")"</c>
    /// </example>
    [Parameter] public BitIconInfo? DismissIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the dismiss icon from the built-in Fluent UI icons. If unset, default will be the Fluent UI <c>Cancel</c> icon.
    /// </summary>
    /// <remarks>
    /// The icon name should be from the Fluent UI icon set (e.g., <c>BitIconName.Blocked2Solid</c>).
    /// <br />
    /// Browse available names in <c>BitIconName</c> of the <c>Bit.BlazorUI.Icons</c> nuget package or the gallery:
    /// <see href="https://blazorui.bitplatform.dev/iconography"/>.
    /// <br />
    /// For external icon libraries, use <see cref="DismissIcon"/> instead.
    /// </remarks>
    [Parameter] public string? DismissIconName { get; set; }

    /// <summary>
    /// Determines the elevation of the message, a scale from 1 to 24.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public int? Elevation { get; set; }

    /// <summary>
    /// Gets or sets the icon for the expand button in Truncate mode using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="ExpandIconName"/> when both are set.
    /// </summary>
    /// <remarks>
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="ExpandIconName"/> instead.
    /// </remarks>
    /// <example>
    /// Bootstrap: <c>ExpandIcon="BitIconInfo.Bi(\"chevron-double-down\")"</c>
    /// FontAwesome: <c>ExpandIcon="BitIconInfo.Fa(\"solid chevron-down\")"</c>
    /// Custom CSS: <c>ExpandIcon="BitIconInfo.Css(\"my-expand-icon\")"</c>
    /// </example>
    [Parameter] public BitIconInfo? ExpandIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the expand icon in Truncate mode from the built-in Fluent UI icons.
    /// </summary>
    /// <remarks>
    /// The icon name should be from the Fluent UI icon set (e.g., <c>BitIconName.ChevronDown</c>).
    /// <br />
    /// Browse available names in <c>BitIconName</c> of the <c>Bit.BlazorUI.Icons</c> nuget package or the gallery:
    /// <see href="https://blazorui.bitplatform.dev/iconography"/>.
    /// <br />
    /// For external icon libraries, use <see cref="ExpandIcon"/> instead.
    /// </remarks>
    [Parameter] public string? ExpandIconName { get; set; }

    /// <summary>
    /// Prevents rendering the icon of the message.
    /// </summary>
    [Parameter] public bool HideIcon { get; set; }

    /// <summary>
    /// Gets or sets the icon to display using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="IconName"/> when both are set.
    /// </summary>
    /// <remarks>
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="IconName"/> instead.
    /// </remarks>
    /// <example>
    /// Bootstrap: <c>Icon="BitIconInfo.Bi(\"info-circle-fill\")"</c>
    /// FontAwesome: <c>Icon="BitIconInfo.Fa(\"solid circle-info\")"</c>
    /// Custom CSS: <c>Icon="BitIconInfo.Css(\"my-message-icon\")"</c>
    /// </example>
    [Parameter] public BitIconInfo? Icon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to display from the built-in Fluent UI icons.
    /// </summary>
    /// <remarks>
    /// The icon name should be from the Fluent UI icon set (e.g., <c>BitIconName.Info</c>).
    /// <br />
    /// Browse available names in <c>BitIconName</c> of the <c>Bit.BlazorUI.Icons</c> nuget package or the gallery:
    /// <see href="https://blazorui.bitplatform.dev/iconography"/>.
    /// <br />
    /// If unset, the icon will be selected automatically based on <see cref="Color"/>.
    /// <br />
    /// For external icon libraries, use <see cref="Icon"/> instead.
    /// </remarks>
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

        StyleBuilder.Register(() => Alignment switch
        {
            BitAlignment.Start => "--bit-msg-justifycontent:flex-start",
            BitAlignment.End => "--bit-msg-justifycontent:flex-end",
            BitAlignment.Center => "--bit-msg-justifycontent:center",
            BitAlignment.SpaceBetween => "--bit-msg-justifycontent:space-between",
            BitAlignment.SpaceAround => "--bit-msg-justifycontent:space-around",
            BitAlignment.SpaceEvenly => "--bit-msg-justifycontent:space-evenly",
            BitAlignment.Baseline => "--bit-msg-justifycontent:baseline",
            BitAlignment.Stretch => "--bit-msg-justifycontent:stretch",
            _ => "--bit-msg-justifycontent:flex-start"
        });

        StyleBuilder.Register(() => Elevation is > 0 or < 25 ? $"--bit-msg-boxshadow:var(--bit-shd-{Elevation.Value})" : string.Empty);
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

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender is false) return;
        if (OnDismiss.HasDelegate is false) return;
        if (AutoDismissTime is not { } delay || delay <= TimeSpan.Zero) return;

        _autoDismissCts?.Cancel();
        _autoDismissCts = new CancellationTokenSource();
        _ =  AutoDismissAsync(delay, _autoDismissCts.Token);
    }



    private async Task AutoDismissAsync(TimeSpan delay, CancellationToken ct)
    {
        try
        {
            await Task.Delay(delay, ct);

            if (ct.IsCancellationRequested || OnDismiss.HasDelegate is false) return;

            await InvokeAsync(OnDismiss.InvokeAsync);
        }
        catch (TaskCanceledException) { }
    }

    private void ToggleExpand() => _isExpanded = _isExpanded is false;

    private string GetTextRole() => Role ?? (Color is BitColor.Success or BitColor.Info ? "status" : "alert");

    private BitIconInfo? GetIcon() => BitIconInfo.From(Icon, GetIconName());

    private string GetIconName() => IconName ?? _IconMap[Color ?? BitColor.Info];



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        _autoDismissCts?.Cancel();
        _autoDismissCts?.Dispose();

        await base.DisposeAsync(disposing);
    }



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
