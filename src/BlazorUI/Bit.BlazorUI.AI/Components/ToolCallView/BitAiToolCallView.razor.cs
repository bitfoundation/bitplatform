using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// BitAiToolCallView shows a tool call as one card: what is being called, what it was given, how it is going, and what
/// came back.
/// </summary>
/// <remarks>
/// The card is collapsed by default (<see cref="BitAiUiOptions.CollapseToolCalls"/>), because most calls are plumbing
/// the reader does not need, and it expands by itself when something needs attention: a call that failed, or one that
/// waits for an approval. Its header says what the call is doing or how it ended (running, used, failed,
/// declined, cancelled - the tool-call texts of <see cref="BitAiStrings"/>), with the icon the definition of the tool
/// names (<see cref="BitAiToolDefinition.Icon"/>) or, when it names none, a wrench. The arguments are shown as they
/// stream in, parsed leniently, and the result as text or as a
/// <see cref="BitAiJsonView"/>; a running call shows its progress as a <see cref="BitProgress"/>, and one awaiting a
/// decision a <see cref="BitAiToolApproval"/>.
/// <br />
/// The card listens to its call rather than to the message around it, so a call streaming its arguments re-renders
/// this card alone, at most once per <see cref="BitAiUiOptions.RenderInterval"/>. A renderer registered for the tool
/// with <see cref="BitAiRendererRegistry.RegisterTool(string, BitAiToolCallRenderer)"/> replaces the built-in card,
/// inside a <see cref="BitAiRendererBoundary"/>.
/// <br />
/// <see cref="BitComponentBase.Disabled"/> turns the expander and the approval buttons off; the arguments and the
/// result stay readable.
/// </remarks>
public partial class BitAiToolCallView : BitAiComponentBase
{
    private static readonly string _ChevronIcon = BitIconInfo.Bit("ChevronRight").GetCssClasses();
    // Repair is not one of the glyphs of the core font, so it is drawn from the subset font of this package.
    private const string _DefaultToolIcon = "bit-icon-ai bit-icon-ai--Repair";

    // The classes of the icon the definition of the tool names, built again only when that name changes rather than on
    // every render of a card streaming its arguments.
    private string? _iconName;
    private string? _iconClasses;

    private bool _open;
    private bool _initialized;
    private string? _stateClasses;
    private RenderFragment? _custom;
    private BitAiToolCallPart? _subscribed;
    private BitAiToolCallStatus _lastStatus = BitAiToolCallStatus.Streaming;





    /// <summary>
    /// Gets or sets the cascading parameters for the tool call view component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple tool call view
    /// components through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitAiToolCallViewParams.ParamName)]
    public BitAiToolCallViewParams? CascadingParameters { get; set; }



    /// <summary>
    /// The call to render.
    /// </summary>
    [Parameter, EditorRequired]
    public BitAiToolCallPart Call { get; set; } = default!;

    /// <summary>
    /// Custom CSS classes for different parts of the tool call view.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitAiToolCallViewClassStyles? Classes { get; set; }

    /// <summary>
    /// The message the call belongs to, which a custom tool renderer is handed along with the call.
    /// </summary>
    [Parameter] public BitAiMessage? Message { get; set; }

    /// <summary>
    /// The session driving the conversation, which the approval decisions are handed to.
    /// </summary>
    [Parameter] public BitAiChatSession? Session { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the tool call view.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public BitAiToolCallViewClassStyles? Styles { get; set; }



    protected override string RootElementClass => "bit-aitcv";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root)
                    .Register(() => _stateClasses);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitAiToolCallViewParams))]
    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        // Subscribing here rather than relying on a parent re-render keeps the card correct when it is used on its
        // own, which is how custom layouts and tests use it.
        if (ReferenceEquals(_subscribed, Call) is false)
        {
            if (_subscribed is not null)
            {
                _subscribed.Changed -= OnCallChanged;
            }

            _subscribed = Call;
            _subscribed.Changed += OnCallChanged;
        }

        _custom = Renderers.FindTool(Call.ToolName)
                           ?.Invoke(new BitAiToolCallContext(Call, Message ?? new BitAiMessage(BitAiRole.Assistant), Session));

        if (_initialized is false)
        {
            _initialized = true;
            _open = Options.CollapseToolCalls is false;
        }

        base.OnParametersSet();
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        if (_subscribed is not null)
        {
            _subscribed.Changed -= OnCallChanged;
            _subscribed = null;
        }

        await base.DisposeAsync(disposing);
    }



    private string _BodyId => $"{_Id}-bdy";

    private string _DisplayName => Call.Definition?.DisplayName ?? Call.ToolName;

    private string _Heading => Call.Status switch
    {
        BitAiToolCallStatus.Failed => Strings.ToolFailed(_DisplayName),
        BitAiToolCallStatus.Completed => Strings.CalledTool(_DisplayName),
        BitAiToolCallStatus.AwaitingApproval => Strings.ApproveTool(_DisplayName),
        BitAiToolCallStatus.Rejected => Strings.ToolRejected(_DisplayName),
        BitAiToolCallStatus.Cancelled => Strings.ToolCancelled(_DisplayName),
        _ => Strings.CallingTool(_DisplayName),
    };

    // The icon the definition of the tool names, drawn the way a core component draws its IconName, or the wrench of
    // this package's own subset when the tool names none.
    private string _ToolIcon
    {
        get
        {
            var name = Call.Definition?.Icon;

            if (name.HasNoValue()) return _DefaultToolIcon;

            if (string.Equals(name, _iconName, StringComparison.Ordinal) is false || _iconClasses is null)
            {
                _iconName = name;
                _iconClasses = BitIconInfo.Bit(name!).GetCssClasses();
            }

            return _iconClasses;
        }
    }

    // Raised on whatever thread the transport streams on, so it only asks for a render; what the change means for the
    // card is worked out by the render itself (SyncState).
    private void OnCallChanged(BitAiMessagePart part) => RequestRender();

    private void HandleOnHeaderClick()
    {
        if (Disabled) return;

        _open = _open is false;
    }

    /// <summary>
    /// Brings the expansion and the root classes up to date with the status the call is in now. Called at the start of
    /// every render, on the renderer's own thread.
    /// </summary>
    private void SyncState()
    {
        // A failure or an approval request is not something to leave hidden behind a chevron - but only as the call
        // moves into it, so a reader who collapses the card again is left with it collapsed.
        if (_lastStatus != Call.Status)
        {
            _lastStatus = Call.Status;

            if (Call.Status is BitAiToolCallStatus.Failed or BitAiToolCallStatus.AwaitingApproval)
            {
                _open = true;
            }
        }

        // A declined call is outlined as a failed one (the call did not happen, and the reader may want to know why);
        // a cancelled one, stopped by the user, goes back to the outline at rest.
        var stateClasses = _custom is not null
            ? "bit-aitcv-cst"
            : string.Join(' ', Call.Status switch
            {
                BitAiToolCallStatus.Running or BitAiToolCallStatus.Streaming => "bit-aitcv-run",
                BitAiToolCallStatus.Failed or BitAiToolCallStatus.Rejected => "bit-aitcv-fld",
                BitAiToolCallStatus.AwaitingApproval => "bit-aitcv-apr",
                _ => string.Empty,
            }, _open ? "bit-aitcv-opn" : string.Empty).Trim();

        if (_stateClasses != stateClasses)
        {
            _stateClasses = stateClasses;

            ClassBuilder.Reset();
        }
    }
}
