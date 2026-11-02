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
/// names (<see cref="BitAiToolDefinition.Icon"/>) or, when it names none, a wrench. The definition is the call's, or -
/// until a session puts it on the call as it runs it - the one registered for the tool (<see cref="IBitAiToolRegistry"/>).
/// The arguments are shown as they
/// stream in, parsed leniently, and the result as text or as a
/// <see cref="BitAiJsonView"/>; a running call shows its progress as a <see cref="BitProgress"/>, and one awaiting a
/// decision a <see cref="BitAiToolApproval"/>.
/// <br />
/// The card listens to its call rather than to the message around it, so a call streaming its arguments re-renders
/// this card alone, at most once per <see cref="BitAiUiOptions.RenderInterval"/>. A renderer registered for the tool
/// with <see cref="BitAiRendererRegistry.RegisterTool(string, BitAiToolCallRenderer)"/> replaces the built-in card,
/// inside a <see cref="BitAiRendererBoundary"/>.
/// <br />
/// <see cref="BitComponentBase.Disabled"/> turns the expander and the approval buttons off, and leaves the card as it is:
/// the arguments and the result of an open card stay readable, and a collapsed card stays collapsed.
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

    // The definition the app registered for the tool, looked up again only when the tool of the call changes (see
    // _Definition).
    private string? _registeredFor;
    private BitAiToolDefinition? _registeredDefinition;

    private bool _open;
    private bool _initialized;
    private bool _focusHeader;
    private ElementReference _header;
    private string? _stateClasses;
    private RenderFragment? _custom;
    // The message a registered renderer is handed while the card is given none, made once so the renderer sees the same
    // one on every render rather than a new id each time.
    private BitAiMessage? _fallbackMessage;
    private BitAiToolCallPart? _subscribed;
    private BitAiToolCallStatus _lastStatus = BitAiToolCallStatus.Streaming;



    [Inject] private IServiceProvider _services { get; set; } = default!;


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
                           ?.Invoke(new BitAiToolCallContext(Call, Message ?? (_fallbackMessage ??= new(BitAiRole.Assistant)), Session));

        if (_initialized is false)
        {
            _initialized = true;
            _open = Options.CollapseToolCalls is false;
        }

        base.OnParametersSet();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        // The header takes the focus once the decision has taken the prompt away (see HandleOnDecision). A call still
        // waiting keeps its prompt, and the focus where it is.
        if (_focusHeader)
        {
            _focusHeader = false;

            if (_custom is null && Call.Status is not BitAiToolCallStatus.AwaitingApproval)
            {
                await FocusHeaderAsync();
            }
        }
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

    private string _DisplayName => _Definition?.DisplayName ?? Call.ToolName;

    // The definition of the call, else the one the app registered for its tool. A session puts the definition on a call
    // only as it runs it, so the card would otherwise be headed by the raw name the model wrote - under the wrench - while
    // the call streams and waits, and for good on a call the session never runs (one a server ran). The registry is
    // looked up rather than injected: an app that cascades its renderers may register no services at all.
    private BitAiToolDefinition? _Definition
    {
        get
        {
            if (Call.Definition is { } definition) return definition;

            if (string.Equals(_registeredFor, Call.ToolName, StringComparison.Ordinal) is false)
            {
                _registeredFor = Call.ToolName;
                _registeredDefinition = (_services.GetService(typeof(IBitAiToolRegistry)) as IBitAiToolRegistry)?.Find(Call.ToolName)?.Definition;
            }

            return _registeredDefinition;
        }
    }

    private string _Heading => Call.Status switch
    {
        BitAiToolCallStatus.Failed => Strings.ToolFailed(_DisplayName),
        BitAiToolCallStatus.Completed => Strings.CalledTool(_DisplayName),
        // The prompt under the header asks the question, so the header only says what the call is waiting for.
        BitAiToolCallStatus.AwaitingApproval => Strings.ToolAwaitingApproval(_DisplayName),
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
            var name = _Definition?.Icon;

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

    // A decision moves the call on from AwaitingApproval, so the render it leads to takes the prompt away together with
    // the button that had the focus, which would fall to the page. The header, which stays, takes it instead.
    private void HandleOnDecision() => _focusHeader = true;

    private async Task FocusHeaderAsync()
    {
        if (IsDisposing) return;

        try
        {
            await _header.FocusAsync();
        }
        catch (JSDisconnectedException) { } // the circuit is gone, and the page with it
        catch (OperationCanceledException) { }
        catch (JSException) { } // a header taken off the page in the meantime has nothing to focus
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
