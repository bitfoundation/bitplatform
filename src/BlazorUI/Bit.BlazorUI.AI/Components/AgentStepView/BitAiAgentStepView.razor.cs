using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// BitAiAgentStepView is one step of an agent run, drawn as an entry of a timeline so the sequence and the current
/// position read at a glance.
/// </summary>
/// <remarks>
/// The step shows its title and how long it took (or has been taking), its detail and summary, and the tool calls it
/// made - as tags, by name and status when the <see cref="Conversation"/> is given - so "search the docs" shows which
/// searches actually ran. It follows the step's own change notifications, coalesced into one render per frame interval.
/// </remarks>
public partial class BitAiAgentStepView : BitAiComponentBase
{
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
        }

        base.OnParametersSet();
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

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

    private void OnStepChanged(BitAiAgentStep step) => RequestRender();

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
