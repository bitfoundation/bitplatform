using System.Globalization;

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
    private readonly RenderFragment _renderTitle;



    public BitMessage()
    {
        _onPauseAutoDismiss = PauseAutoDismiss;
        _onResumeAutoDismiss = ResumeAutoDismiss;
        _onRootKeyDown = HandleOnKeyDown;
        _renderTitle = RenderTitle;
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
    /// The countdown only runs while the message can be dismissed at all - that is, while <see cref="OnDismiss"/>
    /// has a handler or <see cref="Dismissible"/> is set - and while the message is enabled. It is held for as long
    /// as the pointer is over the message or the focus is inside it, so the message cannot vanish while it is being
    /// read or acted upon (WCAG 2.2.1 Timing Adjustable). Assigning a different value re-arms the countdown.
    /// </remarks>
    [Parameter] public TimeSpan? AutoDismissTime { get; set; }

    /// <summary>
    /// Moves the focus to the message as soon as it is rendered.
    /// </summary>
    /// <remarks>
    /// A message that reports the outcome of something the reader just did is worth being taken to, but moving
    /// the focus is also an interruption: reserve it for the messages that have to be acted upon, and leave the
    /// rest to be announced by the live region. The root is made focusable
    /// (<c>tabindex="-1"</c>) while no explicit <see cref="BitComponentBase.TabIndex"/> is given, so the focus has
    /// somewhere to land.
    /// </remarks>
    [Parameter] public bool AutoFocus { get; set; }

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
    /// Determines whether the message has been dismissed, which is two-way bindable. A dismissed message renders nothing.
    /// </summary>
    /// <remarks>
    /// The message only sets this itself while it owns its dismissal - that is, while <see cref="Dismissible"/> is
    /// set or this parameter is bound - so a message that only reports the dismissal through <see cref="OnDismiss"/>
    /// keeps being rendered until its owner takes it off the page, the way it always has. Setting it back to
    /// <c>false</c> brings the message back and re-arms its <see cref="AutoDismissTime"/> countdown.
    /// </remarks>
    [Parameter, TwoWayBound, CallOnSet(nameof(HandleDismissedChanged))]
    public bool Dismissed { get; set; }

    /// <summary>
    /// Renders the dismiss button and lets the message dismiss itself, without an <see cref="OnDismiss"/> handler
    /// having to take it off the page.
    /// </summary>
    /// <remarks>
    /// Dismissing sets <see cref="Dismissed"/>, so the message stops rendering on its own, and still invokes
    /// <see cref="OnDismiss"/> for whoever needs to know about it.
    /// </remarks>
    [Parameter] public bool Dismissible { get; set; }

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
    /// The shortcut is only wired up while the message can be dismissed at all - that is, while
    /// <see cref="OnDismiss"/> has a handler or <see cref="Dismissible"/> is set. Since the message itself is not
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
    /// The text that says out loud what the icon of the message means, for readers who never see it.
    /// </summary>
    /// <remarks>
    /// The icon itself is decorative markup and is hidden from assistive technology, so the severity it stands for
    /// is lost unless the text of the message says it too. Where it does not - a bare "Your session expires in 5
    /// minutes." on a Warning message - set this to a word like "Warning": it is rendered invisibly at the start of
    /// the announced region, so the announcement leads with it. Leave it unset where the message already reads as
    /// what it is, so the announcement is not padded with a word that adds nothing.
    /// </remarks>
    [Parameter] public string? IconAriaLabel { get; set; }

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
    /// The custom template to render in place of the icon of the message, which takes precedence over
    /// <see cref="Icon"/> and <see cref="IconName"/>.
    /// </summary>
    /// <remarks>
    /// Use it where the leading glyph is more than an icon font can carry - a spinner while something is still
    /// running, an avatar, an inline image. It renders inside the same icon container, so it keeps the spacing and
    /// the alignment of the icon it replaces, and it is hidden from assistive technology just as the icon is.
    /// </remarks>
    [Parameter] public RenderFragment? IconTemplate { get; set; }

    /// <summary>
    /// Determines if the message is multi-lined. If false, and the text overflows over buttons or to another line, it is clipped.
    /// </summary>
    [Parameter] public bool Multiline { get; set; }

    /// <summary>
    ///  Whether the message has a dismiss button and its callback. If null, dismiss button won't show.
    /// </summary>
    /// <remarks>
    /// Taking the message off the page is left to this callback. Use <see cref="Dismissible"/> instead - on its own
    /// or alongside this one - to have the message do that itself.
    /// </remarks>
    [Parameter] public EventCallback OnDismiss { get; set; }

    /// <summary>
    /// Callback invoked before the message is dismissed, letting the dismissal be cancelled.
    /// </summary>
    /// <remarks>
    /// Set <c>Cancel</c> on the provided <see cref="BitMessageDismissArgs"/> to keep the message where it is, and
    /// read its <c>Reason</c> to tell the dismiss button, the Escape key, the countdown and a
    /// <see cref="DismissAsync()"/> call apart - refusing to let a countdown take away a message that has not been
    /// read yet is not the same as refusing the button the reader just pressed. Since the callback is awaited, it
    /// can also run asynchronous work like a confirmation prompt.
    /// </remarks>
    [Parameter] public EventCallback<BitMessageDismissArgs> OnDismissing { get; set; }

    /// <summary>
    /// Custom role to apply to the message text.
    /// </summary>
    /// <remarks>
    /// If unset, the role is derived from <see cref="Color"/>: the colors that carry urgency
    /// (Warning, SevereWarning and Error) announce as <c>alert</c>, every other color as <c>status</c>.
    /// </remarks>
    [Parameter] public string? Role { get; set; }

    /// <summary>
    /// Renders a bar along the bottom edge of the message that runs down as its <see cref="AutoDismissTime"/> does.
    /// </summary>
    /// <remarks>
    /// It turns the countdown into something the reader can see coming instead of having the message vanish out of
    /// nowhere, and it holds exactly where the countdown holds - while the pointer is over the message or the focus
    /// is inside it. It only renders where there is a countdown to show.
    /// </remarks>
    [Parameter] public bool ShowAutoDismissProgress { get; set; }

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
    /// The HTML element the title of the message is rendered as. The default is a <c>div</c>.
    /// </summary>
    /// <remarks>
    /// Set it to a heading (<c>h2</c> ... <c>h6</c>) where the message is a section of the page a reader should be
    /// able to jump to, the way a screen reader's heading list works. Pick the level that fits the outline of the
    /// page around it rather than the look: the title keeps the type of the message whatever element it is
    /// rendered as.
    /// </remarks>
    [Parameter] public string? TitleElement { get; set; }

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



    /// <summary>
    /// Dismisses the message the same way its dismiss button does: <see cref="OnDismissing"/> gets its say, the
    /// countdown is stopped, the message takes itself off the page while it owns its dismissal, and
    /// <see cref="OnDismiss"/> is invoked.
    /// </summary>
    public Task DismissAsync() => DismissAsync(BitMessageDismissReason.Programmatic);

    /// <summary>
    /// Holds the <see cref="AutoDismissTime"/> countdown where it is, the way hovering the message does.
    /// </summary>
    /// <remarks>
    /// The countdown is already held while the pointer is over the message or the focus is inside it; this is for
    /// holding it over something the message cannot see, such as a menu of its own that opened somewhere else.
    /// </remarks>
    public void PauseAutoDismiss() => _isAutoDismissPaused = true;

    /// <summary>
    /// Lets the <see cref="AutoDismissTime"/> countdown spend its time again after a
    /// <see cref="PauseAutoDismiss"/>, from wherever it was held.
    /// </summary>
    public void ResumeAutoDismiss() => _isAutoDismissPaused = false;

    /// <summary>
    /// Moves the focus to the message.
    /// </summary>
    /// <remarks>
    /// The focus only lands where there is something to land on, so the message has to be focusable: either give it
    /// a <see cref="BitComponentBase.TabIndex"/> or set <see cref="AutoFocus"/>, which makes it focusable on its own.
    /// </remarks>
    public ValueTask FocusAsync() => Dismissed ? ValueTask.CompletedTask : RootElement.FocusAsync();



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

    // The title is written out by hand rather than in the markup because the element it is rendered as is the
    // consumer's to choose: a plain div by default, a heading where the message is a part of the page a reader
    // should be able to jump to. The fragment is held in a field rather than rebuilt per render, so the diff
    // keeps being handed the same delegate and leaves the region alone until something in it actually changes.
    private void RenderTitle(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, TitleElement.HasValue() ? TitleElement! : "div");
        builder.AddAttribute(1, "style", Styles?.Title);
        builder.AddAttribute(2, "class", $"bit-msg-ttl {Classes?.Title}");
        builder.AddAttribute(3, "id", $"{_Id}-ttl");

        if (TitleTemplate is not null)
        {
            builder.AddContent(4, TitleTemplate);
        }
        else
        {
            builder.AddContent(5, Title);
        }

        builder.CloseElement();
    }

    // Expanded only means anything where there is something folded away to unfold, so it is read through
    // the same condition that decides whether the expander button renders at all.
    private bool _IsExpanded => Truncate && Multiline is false && Expanded;

    // A title sits on the same line as the content unless the message has room for a second line. The row
    // layout is only opted into when there is actually a title, so a message without one renders as before.
    private bool _IsTitleInline => _HasTitle && Multiline is false && _IsExpanded is false;

    // A dismiss button is worth rendering as soon as pressing it would do something: either it is reported to
    // someone, or the message takes itself off the page.
    private bool _IsDismissable => OnDismiss.HasDelegate || Dismissible;

    // The message only hides itself where its dismissal was handed to it - through the parameter that asks for it,
    // or through a binding that follows it. Everything else keeps the previous contract, where OnDismiss reports
    // the dismissal and its owner decides what to do about it.
    private bool _OwnsDismissal => Dismissible || DismissedChanged.HasDelegate;

    private bool _HasAutoDismiss => _IsDismissable && IsEnabled && AutoDismissTime is { } delay && delay > TimeSpan.Zero;

    private bool _HandlesEscape => DismissOnEscape && _IsDismissable && IsEnabled;

    // There is nothing to count down where nothing is counting down, so the bar follows the countdown itself
    // rather than the parameter that asks for it.
    private bool _ShowsAutoDismissProgress => ShowAutoDismissProgress && _HasAutoDismiss;

    private string? _AutoDismissDuration => _ShowsAutoDismissProgress
        ? $"animation-duration:{AutoDismissTime!.Value.TotalMilliseconds.ToString(CultureInfo.InvariantCulture)}ms"
        : null;

    private string _ExpanderLabel => Expanded ? CollapseAriaLabel : ExpandAriaLabel;

    // The focus has to have somewhere to land before it can be moved there, and a div is not focusable of its own
    // accord. An explicit TabIndex is left alone - it was put there on purpose.
    private string? _TabIndex => TabIndex ?? (AutoFocus ? "-1" : null);

    // A name turns the message into something a screen reader can announce as one unit and step over as one,
    // which is what a group is for. Without a name there is nothing to announce, and an unnamed group would only
    // add a boundary to walk in and out of, so the root stays a plain div.
    private bool _HasRootName => AriaLabel.HasValue() || _HasTitle;

    // A role written on the component itself wins - it was put there on purpose. It is handed back out rather
    // than left to the splatted attributes: this one is written after them, and an attribute written after the
    // splat replaces what the splat put there, null included.
    private string? _RootRole => HtmlAttributes.TryGetValue("role", out var role)
        ? role?.ToString()
        : (_HasRootName ? "group" : null);

    // An explicit label wins; otherwise the title of the message is the name of the group, the way the heading
    // of a section names the section.
    private string? _RootLabelledBy => (AriaLabel.HasValue() is false && _HasTitle) ? $"{_Id}-ttl" : null;



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

    // A dismissed message is not on the page any more, and a re-shown one is a new sighting of it, so the countdown
    // it may carry starts over rather than picking up where the previous showing left off.
    private void HandleDismissedChanged()
    {
        StopAutoDismiss();

        _armedAutoDismissTime = null;
    }

    private void ArmAutoDismiss()
    {
        var delay = (_IsDismissable && IsEnabled && Dismissed is false) ? AutoDismissTime : null;

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

            if (ct.IsCancellationRequested || IsDisposed || _IsDismissable is false) return;

            await InvokeAsync(() => DismissAsync(BitMessageDismissReason.AutoDismiss));
        }
        catch (OperationCanceledException) { }
        catch (ObjectDisposedException) { }
    }

    private async Task DismissAsync(BitMessageDismissReason reason)
    {
        if (Dismissed) return;

        if (OnDismissing.HasDelegate)
        {
            var args = new BitMessageDismissArgs(reason);

            await OnDismissing.InvokeAsync(args);

            if (args.Cancel) return;
        }

        StopAutoDismiss();

        if (_OwnsDismissal)
        {
            await AssignDismissed(true);

            // The dismissal can come from the countdown or from a call of its own, neither of which is a render
            // the component was already going to do, so the re-render is asked for rather than assumed. It goes
            // through the dispatcher so a call from off the render loop is safe.
            await InvokeAsync(StateHasChanged);
        }

        await OnDismiss.InvokeAsync();
    }

    private async Task ToggleExpand()
    {
        if (IsEnabled is false) return;

        await AssignExpanded(Expanded is false);
    }

    private async Task HandleOnDismiss()
    {
        if (IsEnabled is false) return;

        await DismissAsync(BitMessageDismissReason.Button);
    }

    private async Task HandleOnKeyDown(KeyboardEventArgs e)
    {
        if (_HandlesEscape is false) return;
        // "Esc" is what the older browsers report for the same key.
        if (e.Key is not ("Escape" or "Esc")) return;

        await DismissAsync(BitMessageDismissReason.Escape);
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
