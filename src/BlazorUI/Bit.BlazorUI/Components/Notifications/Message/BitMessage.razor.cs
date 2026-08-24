namespace Bit.BlazorUI;

/// <summary>
/// A Message displays errors, warnings, or important information. For example, if a file failed to upload an error message should appear.
/// </summary>
public partial class BitMessage : BitComponentBase
{
    // The countdown of the auto-dismiss feature is stepped rather than awaited in one go, so that hovering
    // or focusing the message can hold it (WCAG 2.2.1 Timing Adjustable). This is the length of one step.
    private static readonly TimeSpan _AutoDismissTick = TimeSpan.FromMilliseconds(250);

    // Written from the render loop and read from the countdown task, so the read has to see the write.
    private volatile bool _isAutoDismissPaused;
    private TimeSpan? _armedAutoDismissTime;
    private CancellationTokenSource? _autoDismissCts;

    // Held as fields so re-registering them on every parameter set keeps handing the renderer the same
    // delegate instance, which is what lets the diff leave the listener alone.
    private readonly Action _onPauseAutoDismiss;
    private readonly Action _onResumeAutoDismiss;
    private readonly Func<KeyboardEventArgs, Task> _onRootKeyDown;



    public BitMessage()
    {
        _onPauseAutoDismiss = PauseAutoDismiss;
        _onResumeAutoDismiss = ResumeAutoDismiss;
        _onRootKeyDown = HandleOnKeyDown;
    }



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
    /// <remarks>
    /// The countdown only runs while <see cref="OnDismiss"/> has a handler, and it is held for as long as the
    /// pointer is over the message or the focus is inside it, so the message cannot vanish while it is being
    /// read or acted upon (WCAG 2.2.1 Timing Adjustable). Assigning a different value re-arms the countdown.
    /// </remarks>
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
    /// The aria-label and the tooltip of the expander button of the message in Truncate mode while it is expanded.
    /// </summary>
    [Parameter] public string CollapseAriaLabel { get; set; } = "Collapse";

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
    /// The aria-label and the tooltip of the dismiss button of the message.
    /// </summary>
    [Parameter] public string DismissAriaLabel { get; set; } = "Dismiss";

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
    /// Invokes the <see cref="OnDismiss"/> callback when the Escape key is pressed while the focus is inside the message.
    /// </summary>
    /// <remarks>
    /// The shortcut is only wired up while <see cref="OnDismiss"/> has a handler. Since the message itself is not
    /// focusable by default, set <see cref="BitComponentBase.TabIndex"/> to <c>0</c> to let a keyboard user reach it
    /// without first landing on the dismiss button or one of the action buttons.
    /// </remarks>
    [Parameter] public bool DismissOnEscape { get; set; }

    /// <summary>
    /// Determines the elevation of the message, a scale from 1 to 24.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public int? Elevation { get; set; }

    /// <summary>
    /// The aria-label and the tooltip of the expander button of the message in Truncate mode while it is collapsed.
    /// </summary>
    [Parameter] public string ExpandAriaLabel { get; set; } = "Expand";

    /// <summary>
    /// Determines whether the truncated content of the message is expanded, which is two-way bindable.
    /// </summary>
    /// <remarks>
    /// Only meaningful together with <see cref="Truncate"/>. Binding this parameter lets the expanded state be
    /// controlled and observed from outside, for example to expand every message of a list at once.
    /// </remarks>
    [Parameter, TwoWayBound]
    public bool Expanded { get; set; }

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
    /// <remarks>
    /// If unset, the role is derived from <see cref="Color"/>: the colors that carry urgency
    /// (Warning, SevereWarning and Error) announce as <c>alert</c>, every other color as <c>status</c>.
    /// </remarks>
    [Parameter] public string? Role { get; set; }

    /// <summary>
    /// The size of Message, Possible values: Small | Medium | Large
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSize? Size { get; set; }

    /// <summary>
    /// Removes the rounded corners of the message so it can sit flush against the edges of its container as a banner.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Square { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitMessage.
    /// </summary>
    [Parameter] public BitMessageClassStyles? Styles { get; set; }

    /// <summary>
    /// The title (heading) of the message, rendered above the content in multiline mode and ahead of it otherwise.
    /// </summary>
    /// <remarks>
    /// A short title makes a message scannable, which matters most where several of them stack up.
    /// Use <see cref="TitleTemplate"/> instead to render markup as the title.
    /// </remarks>
    [Parameter] public string? Title { get; set; }

    /// <summary>
    /// The custom template to render as the title (heading) of the message, which takes precedence over <see cref="Title"/>.
    /// </summary>
    [Parameter] public RenderFragment? TitleTemplate { get; set; }

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

        // The shadow scale only has the 24 steps the theme declares, so anything outside it would resolve
        // to an undefined custom property and drop the whole declaration.
        StyleBuilder.Register(() => Elevation is >= 1 and <= 24 ? $"--bit-msg-boxshadow:var(--bit-shd-{Elevation.Value})" : string.Empty);
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

        ClassBuilder.Register(() => Square ? "bit-msg-sqr" : string.Empty);
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        RegisterInteractionHandlers();

        // Before the first render the timer is armed by OnAfterRenderAsync instead, so that a message that
        // never makes it to the DOM never starts counting down.
        if (IsRendered) ArmAutoDismiss();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender is false) return;

        ArmAutoDismiss();
    }



    private bool _HasTitle => TitleTemplate is not null || Title.HasValue();

    // Expanded only means anything where there is something folded away to unfold, so it is read through
    // the same condition that decides whether the expander button renders at all.
    private bool _IsExpanded => Truncate && Multiline is false && Expanded;

    // A title sits on the same line as the content unless the message has room for a second line. The row
    // layout is only opted into when there is actually a title, so a message without one renders as before.
    private bool _IsTitleInline => _HasTitle && Multiline is false && _IsExpanded is false;

    private bool _HasAutoDismiss => OnDismiss.HasDelegate && AutoDismissTime is { } delay && delay > TimeSpan.Zero;

    private bool _HandlesEscape => DismissOnEscape && OnDismiss.HasDelegate && IsEnabled;

    private string _ExpanderLabel => Expanded ? CollapseAriaLabel : ExpandAriaLabel;



    // The pointer, focus and keyboard listeners go into the splatted attributes rather than into the markup
    // so that a message needing neither the hold nor the shortcut renders without them at all: an
    // always-present mouseenter would turn every hover of every message into a round trip on a Server circuit.
    private void RegisterInteractionHandlers()
    {
        if (_HasAutoDismiss)
        {
            AddHandler("onmouseenter", _onPauseAutoDismiss);
            AddHandler("onmouseleave", _onResumeAutoDismiss);
            AddHandler("onfocusin", _onPauseAutoDismiss);
            AddHandler("onfocusout", _onResumeAutoDismiss);
        }

        if (_HandlesEscape)
        {
            AddHandler("onkeydown", _onRootKeyDown);
        }
    }

    private void AddHandler(string name, object handler)
    {
        // A handler written on the component itself wins - it was put there on purpose.
        if (HtmlAttributes.ContainsKey(name)) return;

        HtmlAttributes[name] = handler;
    }

    private void ArmAutoDismiss()
    {
        var delay = OnDismiss.HasDelegate ? AutoDismissTime : null;

        if (delay is not { } value || value <= TimeSpan.Zero)
        {
            StopAutoDismiss();
            _armedAutoDismissTime = null;
            return;
        }

        // Re-arming on every parameter set would restart the countdown whenever the parent re-renders, and
        // would revive one the reader has already dismissed, so it is only (re)started when the requested
        // duration itself changes.
        if (_armedAutoDismissTime == value) return;

        StopAutoDismiss();

        _armedAutoDismissTime = value;
        _autoDismissCts = new CancellationTokenSource();
        _ = AutoDismissAsync(value, _autoDismissCts.Token);
    }

    // Stops the running countdown but keeps the duration it was started for, which is what tells a later
    // parameter set that this message has had its countdown and is not owed another one.
    private void StopAutoDismiss()
    {
        _isAutoDismissPaused = false;

        if (_autoDismissCts is null) return;

        _autoDismissCts.Cancel();
        _autoDismissCts.Dispose();
        _autoDismissCts = null;
    }

    private async Task AutoDismissAsync(TimeSpan delay, CancellationToken ct)
    {
        try
        {
            var remaining = delay;

            while (remaining > TimeSpan.Zero)
            {
                var step = remaining < _AutoDismissTick ? remaining : _AutoDismissTick;

                await Task.Delay(step, ct);

                // A held countdown keeps ticking but stops spending, so the remaining time is preserved
                // for as long as the pointer or the focus stays on the message.
                if (_isAutoDismissPaused is false) remaining -= step;
            }

            if (ct.IsCancellationRequested || IsDisposed || OnDismiss.HasDelegate is false) return;

            await InvokeAsync(OnDismiss.InvokeAsync);
        }
        catch (OperationCanceledException) { }
        catch (ObjectDisposedException) { }
    }

    private void PauseAutoDismiss() => _isAutoDismissPaused = true;

    private void ResumeAutoDismiss() => _isAutoDismissPaused = false;

    private async Task ToggleExpand()
    {
        if (IsEnabled is false) return;

        await AssignExpanded(Expanded is false);
    }

    private async Task HandleOnDismiss()
    {
        if (IsEnabled is false) return;

        StopAutoDismiss();

        await OnDismiss.InvokeAsync();
    }

    private async Task HandleOnKeyDown(KeyboardEventArgs e)
    {
        if (_HandlesEscape is false) return;
        if (e.Key != "Escape") return;

        StopAutoDismiss();

        await OnDismiss.InvokeAsync();
    }

    private string? GetTextRole()
    {
        if (Role.HasValue()) return Role;

        // Only the colors that carry urgency interrupt the screen reader; the rest are announced politely.
        return Color is BitColor.Warning or BitColor.SevereWarning or BitColor.Error ? "alert" : "status";
    }

    private BitIconInfo? GetIcon() => BitIconInfo.From(Icon, GetIconName());

    private string GetIconName() => IconName ?? _IconMap[Color ?? BitColor.Info];



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        StopAutoDismiss();

        await base.DisposeAsync(disposing);
    }



    private static readonly Dictionary<BitColor, string> _IconMap = new()
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
