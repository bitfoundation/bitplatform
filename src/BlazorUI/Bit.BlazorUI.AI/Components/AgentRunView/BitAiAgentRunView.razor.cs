using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// BitAiAgentRunView makes a multi-step agent run legible: what it is trying to do, which step it is on, what each step
/// produced, and what it needs from the user.
/// </summary>
/// <remarks>
/// The whole point of this component is that a spinner is not an acceptable answer to "what is it doing". Every step
/// is visible (a <see cref="BitAiAgentStepView"/> each), with its own state and timing, and the clock keeps moving while
/// the run does, so a stalled run looks stalled. When the run asks the user something it shows the question with the
/// suggested answers and a field for an answer of the user's own, and hands the answer to <see cref="OnRespond"/> - or,
/// without one, to the <see cref="Session"/>.
/// </remarks>
public partial class BitAiAgentRunView : BitAiComponentBase
{
    private readonly object _gate = new();
    private readonly Dictionary<string, object> _answerInputAttributes;
    private BitAiAgentRun? _subscribed;
    private System.Threading.Timer? _clock;
    private bool _answering;
    private string? _draft;
    private string _promptId = string.Empty;



    public BitAiAgentRunView()
    {
        // The field answering the question is named by the question itself, whose id is the component's own.
        _answerInputAttributes = new() { ["aria-labelledby"] = string.Empty };
    }



    /// <summary>
    /// Gets or sets the cascading parameters for the agent run view component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings (the pace of the clock, and the look) to be applied
    /// to multiple agent run view components through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitAiAgentRunViewParams.ParamName)]
    public BitAiAgentRunViewParams? CascadingParameters { get; set; }



    /// <summary>
    /// Custom CSS classes for different parts of the agent run view.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitAiAgentRunViewClassStyles? Classes { get; set; }

    /// <summary>
    /// How often the elapsed time is refreshed while the run is active.
    /// <br />
    /// The default value is <strong>one second</strong>.
    /// </summary>
    [Parameter] public TimeSpan ClockInterval { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// The callback for when the user answers a pending question. When set, the answer is handed to the callback alone
    /// and the <see cref="Session"/> is not called, so one answer is never delivered twice.
    /// </summary>
    [Parameter] public EventCallback<string> OnRespond { get; set; }

    /// <summary>
    /// The run to render.
    /// </summary>
    [Parameter, EditorRequired]
    public BitAiAgentRun Run { get; set; } = default!;

    /// <summary>
    /// The session, used to answer a blocked run when <see cref="OnRespond"/> is not set, and to show the tool calls of
    /// the steps by name.
    /// </summary>
    [Parameter] public BitAiChatSession? Session { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the agent run view.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public BitAiAgentRunViewClassStyles? Styles { get; set; }



    protected override string RootElementClass => "bit-aiarv";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override void OnInitialized()
    {
        _promptId = $"{UniqueId}-prm";
        _answerInputAttributes["aria-labelledby"] = _promptId;

        base.OnInitialized();
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitAiAgentRunViewParams))]
    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        if (ReferenceEquals(_subscribed, Run) is false)
        {
            if (_subscribed is not null) _subscribed.Changed -= OnRunChanged;
            _subscribed = Run;
            _subscribed.Changed += OnRunChanged;
            _draft = string.Empty;
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

        if (_subscribed is not null) _subscribed.Changed -= OnRunChanged;
        _subscribed = null;

        await base.DisposeAsync(disposing);
    }



    private void OnRunChanged(BitAiAgentRun run)
    {
        SyncClock();
        RequestRender();
    }

    /// <summary>
    /// Runs a clock while the run is in progress so the elapsed time moves, and stops it the moment the run ends so a
    /// finished run costs nothing to keep on screen.
    /// </summary>
    private void SyncClock()
    {
        var run = _subscribed;
        var active = IsDisposing is false && run is not null && run.CompletedAt is null && run.Status is not BitAiAgentRunStatus.Idle;

        lock (_gate)
        {
            if (active && _clock is null && IsDisposing is false)
            {
                _clock = new System.Threading.Timer(_ => RequestRender(), null, ClockInterval, ClockInterval);
            }
            else if (active is false && _clock is not null)
            {
                _clock.Dispose();
                _clock = null;
            }
        }
    }

    private Task HandleOnAnswerEnter(KeyboardEventArgs e) =>
        string.IsNullOrWhiteSpace(_draft) ? Task.CompletedTask : RespondAsync(_draft);

    private async Task RespondAsync(string? answer)
    {
        if (Disabled || _answering || string.IsNullOrWhiteSpace(answer)) return;

        _answering = true;

        try
        {
            // Either the application's callback or the session, never both: with both wired a single tap would resolve
            // the run twice and append the answer twice.
            if (OnRespond.HasDelegate)
            {
                await OnRespond.InvokeAsync(answer);
            }
            else if (Session is not null)
            {
                // A session still streaming the turn that asked refuses the answer (null): the draft is kept, so the
                // answer is not lost and can be sent again once the turn has ended.
                if (await Session.RespondToAgentAsync(Run.RunId, answer) is null) return;
            }

            _draft = string.Empty;
        }
        finally
        {
            _answering = false;
        }
    }
}
