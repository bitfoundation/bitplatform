namespace Bit.BlazorUI;

/// <summary>
/// BitAiAgentStatusBadge is a one-word summary of where an agent run stands, drawn as a <see cref="BitTag"/> and
/// announced politely, so a screen reader user hears every transition without being interrupted.
/// </summary>
/// <remarks>
/// The words come from the string table (<see cref="BitAiStrings.AgentRunning"/> and its siblings). The color of the
/// tag says the same thing the word does: a working run keeps the default look of a tag, a run waiting for the user is
/// a warning, a completed one a success and a failed one an error, while an idle or cancelled run is a neutral one (the
/// host-default tertiary role) - so the default look of the tags of an app (the --bit-Tag-* variables) still reaches the
/// working, the idle and the cancelled runs.
/// </remarks>
public partial class BitAiAgentStatusBadge : BitAiComponentBase
{
    /// <summary>
    /// Gets or sets the cascading parameters for the agent status badge component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple agent status badge
    /// components through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitAiAgentStatusBadgeParams.ParamName)]
    public BitAiAgentStatusBadgeParams? CascadingParameters { get; set; }



    /// <summary>
    /// Custom CSS classes for different parts of the agent status badge.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitAiAgentStatusBadgeClassStyles? Classes { get; set; }

    /// <summary>
    /// The status to describe.
    /// </summary>
    [Parameter, EditorRequired]
    public BitAiAgentRunStatus Status { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the agent status badge.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public BitAiAgentStatusBadgeClassStyles? Styles { get; set; }



    protected override string RootElementClass => "bit-aiasb";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        base.OnParametersSet();
    }



    private string _Label => Status switch
    {
        BitAiAgentRunStatus.Idle => Strings.AgentIdle,
        BitAiAgentRunStatus.Planning => Strings.AgentPlanning,
        BitAiAgentRunStatus.Running => Strings.AgentRunning,
        BitAiAgentRunStatus.WaitingForInput => Strings.WaitingForYou,
        BitAiAgentRunStatus.Completed => Strings.AgentCompleted,
        BitAiAgentRunStatus.Failed => Strings.AgentFailed,
        BitAiAgentRunStatus.Cancelled => Strings.AgentCancelled,
        _ => Status.ToString(),
    };

    // A run at work is what a tag is for by default, so it keeps the default look (and with it the --bit-Tag-*
    // variables of the app); a waiting, completed or failed one means something a color says too.
    private BitColor? _Color => Status switch
    {
        BitAiAgentRunStatus.WaitingForInput => BitColor.Warning,
        BitAiAgentRunStatus.Completed => BitColor.Success,
        BitAiAgentRunStatus.Failed => BitColor.Error,
        _ => null,
    };

    // An idle or cancelled run is not at work, nor does it mean anything a color would say: it is drawn neutral through
    // the host-default class of the tag rather than a Color, which would be a choice outranking the app's variables.
    private string? _DefaultClass => Status is BitAiAgentRunStatus.Idle or BitAiAgentRunStatus.Cancelled ? "bit-tag-dft-ter" : null;
}
