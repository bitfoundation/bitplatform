using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// BitAiAgentStepView is one step of an agent run, drawn as an entry of a timeline so the sequence and the current
/// position read at a glance.
/// </summary>
/// <remarks>
/// The step shows its title and how long it took (or has been taking), its detail and summary, and the tool calls it
/// made - as tags, by name and status when the <see cref="Conversation"/> is given - so "search the docs" shows which
/// searches actually ran. A failed step shows the message of its error, with the detail of the error behind a
/// disclosure. It follows the step's own change notifications, coalesced into one render per frame interval, and a clock
/// moves the time of a running step on while nothing else changes. Its status, which the marker draws, is said in words
/// to assistive technologies.
/// </remarks>
public partial class BitAiAgentStepView : BitAiComponentBase
{
    // How often the time of a running step moves on when nothing else re-renders it: once a second, the pace the clock
    // of a BitAiAgentRunView keeps by default.
    private static readonly TimeSpan _ClockInterval = TimeSpan.FromSeconds(1);

    private readonly object _gate = new();
    private System.Threading.Timer? _clock;
    private bool _showErrorDetail;
    private BitAiAgentStep? _subscribed;
    private BitAiAgentStepStatus? _classStatus;
    private BitAiIconClassesCache<BitTagClassStyles> _toolCallTagClasses;

    // The dots a running tool call shows after its name. They draw what the step around them already says, so the
    // loader is a decorative one that announces nothing of its own.
    private static readonly RenderFragment _runningDots = builder =>
    {
        builder.OpenComponent<BitBouncingDotsLoading>(0);
        builder.AddComponentParameter(1, nameof(BitBouncingDotsLoading.Inline), true);
        builder.AddComponentParameter(2, nameof(BitBouncingDotsLoading.Role), "none");
        builder.AddComponentParameter(3, nameof(BitBouncingDotsLoading.Class), "bit-aiasv-tld");
        builder.CloseComponent();
    };



    /// <summary>
    /// Gets or sets the cascading parameters for the agent step view component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple agent step view
    /// components through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitAiAgentStepViewParams.ParamName)]
    public BitAiAgentStepViewParams? CascadingParameters { get; set; }



    /// <summary>
    /// Custom CSS classes for different parts of the agent step view.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitAiAgentStepViewClassStyles? Classes { get; set; }

    /// <summary>
    /// The conversation the run belongs to, used to show the tool calls the step made by name and status. Without it
    /// the calls are listed by their identifiers.
    /// </summary>
    [Parameter] public BitAiConversation? Conversation { get; set; }

    /// <summary>
    /// The step to render.
    /// </summary>
    [Parameter, EditorRequired]
    public BitAiAgentStep Step { get; set; } = default!;

    /// <summary>
    /// Custom CSS styles for different parts of the agent step view.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public BitAiAgentStepViewClassStyles? Styles { get; set; }



    protected override string RootElementClass => "bit-aiasv";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => _classStatus switch
        {
            BitAiAgentStepStatus.Running => "bit-aiasv-run",
            BitAiAgentStepStatus.Blocked => "bit-aiasv-blk",
            BitAiAgentStepStatus.Completed => "bit-aiasv-cmp",
            BitAiAgentStepStatus.Failed => "bit-aiasv-fld",
            BitAiAgentStepStatus.Skipped => "bit-aiasv-skp",
            _ => "bit-aiasv-pnd"
        });
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitAiAgentStepViewParams))]
    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        if (ReferenceEquals(_subscribed, Step) is false)
        {
            if (_subscribed is not null) _subscribed.Changed -= OnStepChanged;
            _subscribed = Step;
            _subscribed.Changed += OnStepChanged;
            _showErrorDetail = false;
        }

        SyncClock();

        base.OnParametersSet();
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        lock (_gate)
        {
            _clock?.Dispose();
            _clock = null;
        }

        if (_subscribed is not null) _subscribed.Changed -= OnStepChanged;
        _subscribed = null;

        await base.DisposeAsync(disposing);
    }



    // The classes of the tool tags: the ones the app gave through Classes.ToolCallTag, with the class of the wrench added
    // to its Icon - no glyph of the core font, so it comes from the package's own icon font (see BitAiIconClasses), which
    // draws it whether or not the app loads Bit.BlazorUI.Icons. Built again only when the app hands over another set, so
    // the tags are given the same object on every render and do not build their classes again for nothing.
    private BitTagClassStyles _ToolCallTagClasses =>
        _toolCallTagClasses.Get("bit-icon-ai bit-icon-ai--Repair", Classes?.ToolCallTag, BitAiIconClasses.With);

    private string _ErrorDetailId => $"{_Id}-edt";

    private void OnStepChanged(BitAiAgentStep step)
    {
        SyncClock();
        RequestRender();
    }

    /// <summary>
    /// Runs a clock while the step is running, so the time beside its title moves on when the view is drawn on its own -
    /// inside a <see cref="BitAiAgentRunView"/> the clock of the run re-renders it as well - and stops it the moment the
    /// step stops running, so a finished step costs nothing to keep on screen.
    /// </summary>
    private void SyncClock()
    {
        var running = IsDisposing is false && _subscribed is { Status: BitAiAgentStepStatus.Running, StartedAt: not null };

        lock (_gate)
        {
            if (running && _clock is null && IsDisposing is false)
            {
                _clock = new System.Threading.Timer(_ => RequestRender(), null, _ClockInterval, _ClockInterval);
            }
            else if (running is false && _clock is not null)
            {
                _clock.Dispose();
                _clock = null;
            }
        }
    }

    private void ToggleErrorDetail()
    {
        if (Disabled) return;

        _showErrorDetail = _showErrorDetail is false;
    }

    private BitAiToolCallPart? FindToolCall(string toolCallId)
    {
        if (Conversation is not { } conversation) return null;

        var messages = conversation.Messages;
        for (var i = messages.Count - 1; i >= 0; i--)
        {
            if (messages[i].FindToolCall(toolCallId) is { } call) return call;
        }

        return null;
    }
}
