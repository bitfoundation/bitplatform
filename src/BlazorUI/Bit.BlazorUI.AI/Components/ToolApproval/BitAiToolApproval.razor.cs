using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// BitAiToolApproval is the prompt shown when a tool call needs a human decision: allow once, allow always, or decline.
/// </summary>
/// <remarks>
/// Three choices rather than two: allowing once and allowing always are meaningfully different, and collapsing them
/// into one button forces users to either re-approve constantly or grant more than they meant to.
/// <br />
/// The prompt follows the risk the tool declares (<see cref="BitAiToolDefinition.Risk"/>): the allow button of a
/// <see cref="BitAiToolRisk.Destructive"/> tool is drawn in the error role and the prompt is washed with it, so the
/// answer that runs it never looks like the safe one. The decision goes to the <see cref="Session"/> when there is
/// one, and onto the <see cref="Call"/> itself otherwise.
/// </remarks>
public partial class BitAiToolApproval : BitAiComponentBase
{
    private bool _isDestructive;



    /// <summary>
    /// Gets or sets the cascading parameters for the tool approval component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple tool approval components
    /// through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitAiToolApprovalParams.ParamName)]
    public BitAiToolApprovalParams? CascadingParameters { get; set; }



    /// <summary>
    /// The call awaiting a decision.
    /// </summary>
    [Parameter, EditorRequired]
    public BitAiToolCallPart Call { get; set; } = default!;

    /// <summary>
    /// Custom CSS classes for different parts of the tool approval.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitAiToolApprovalClassStyles? Classes { get; set; }

    /// <summary>
    /// Raised after a decision, whether or not a session was supplied.
    /// </summary>
    [Parameter] public EventCallback<BitAiApprovalDecision> OnDecision { get; set; }

    /// <summary>
    /// The session that is waiting. Required for the decision to take effect.
    /// </summary>
    /// <remarks>
    /// Without one, the decision is recorded on the <see cref="Call"/> itself, which is what a custom layout that
    /// drives its own tool loop reads it from.
    /// </remarks>
    [Parameter] public BitAiChatSession? Session { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the tool approval.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public BitAiToolApprovalClassStyles? Styles { get; set; }



    protected override string RootElementClass => "bit-aitap";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root)
                    .Register(() => _isDestructive ? "bit-aitap-dst" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitAiToolApprovalParams))]
    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        // The definition is resolved onto the call as it starts, which may be after the call was first handed here,
        // so the risk is read again on every render the parent asks for rather than only when the call changes.
        var isDestructive = Call?.Definition?.Risk is BitAiToolRisk.Destructive;
        if (_isDestructive != isDestructive)
        {
            _isDestructive = isDestructive;

            ClassBuilder.Reset();
        }

        base.OnParametersSet();
    }



    private string _DisplayName => Call.Definition?.DisplayName ?? Call.ToolName;

    private Task HandleOnReject() => DecideAsync(BitAiApprovalDecision.Rejected);

    private Task HandleOnApproveAlways() => DecideAsync(BitAiApprovalDecision.ApprovedAlways);

    private Task HandleOnApprove() => DecideAsync(BitAiApprovalDecision.Approved);

    private async Task DecideAsync(BitAiApprovalDecision decision)
    {
        if (Disabled) return;

        if (Session is null)
        {
            Call.SetApproval(decision);
        }
        else
        {
            Session.Approve(Call.ToolCallId, decision);
        }

        await OnDecision.InvokeAsync(decision);
    }
}
