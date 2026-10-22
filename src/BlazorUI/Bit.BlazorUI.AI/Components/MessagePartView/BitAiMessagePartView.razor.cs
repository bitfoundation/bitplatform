using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// BitAiMessagePartView renders one part of a message with the view of its kind, and re-renders only when that part
/// changes.
/// </summary>
/// <remarks>
/// This component is where the streaming performance of a conversation comes from. It subscribes to the part rather
/// than to the message, so a token arriving in the last paragraph of a long answer re-renders one paragraph, not the
/// tool cards, code blocks and citations above it - and at most once per <see cref="BitAiUiOptions.RenderInterval"/>.
/// <br />
/// A renderer registered for the kind of the part with
/// <see cref="BitAiRendererRegistry.RegisterPart(string, BitAiMessagePartRenderer)"/> replaces the built-in view,
/// inside a <see cref="BitAiRendererBoundary"/>. A part of a kind this view does not know is shown as its text when it
/// carries one.
/// <br />
/// A part there is nothing to draw for - a <see cref="BitAiCitationPart"/> with no renderer registered for its kind, or
/// a part of an unknown kind that carries no text - renders nothing at all, not even the root element, so it leaves no
/// empty box among the parts of the message.
/// </remarks>
public partial class BitAiMessagePartView : BitAiComponentBase
{
    private RenderFragment? _custom;
    private BitAiMessagePart? _subscribed;



    [Inject] private BitAiRendererRegistry _renderers { get; set; } = default!;



    /// <summary>
    /// Gets or sets the cascading parameters for the message part view component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple message part view
    /// components through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitAiMessagePartViewParams.ParamName)]
    public BitAiMessagePartViewParams? CascadingParameters { get; set; }



    /// <summary>
    /// Custom CSS classes for different parts of the message part view.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitAiMessagePartViewClassStyles? Classes { get; set; }

    /// <summary>
    /// The conversation, when rendering inside one. An artifact part is resolved against it.
    /// </summary>
    [Parameter] public BitAiConversation? Conversation { get; set; }

    /// <summary>
    /// The message the part belongs to.
    /// </summary>
    [Parameter, EditorRequired]
    public BitAiMessage Message { get; set; } = default!;

    /// <summary>
    /// Raised when the user activates a citation inside this part.
    /// </summary>
    [Parameter] public EventCallback<BitAiCitation> OnCitationClick { get; set; }

    /// <summary>
    /// The part to render.
    /// </summary>
    [Parameter, EditorRequired]
    public BitAiMessagePart Part { get; set; } = default!;

    /// <summary>
    /// The session driving the conversation, when there is one. A tool call hands its approval decisions to it.
    /// </summary>
    [Parameter] public BitAiChatSession? Session { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the message part view.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public BitAiMessagePartViewClassStyles? Styles { get; set; }



    protected override string RootElementClass => "bit-aimpv";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitAiMessagePartViewParams))]
    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        if (ReferenceEquals(_subscribed, Part) is false)
        {
            if (_subscribed is not null)
            {
                _subscribed.Changed -= OnPartChanged;
            }

            _subscribed = Part;
            _subscribed.Changed += OnPartChanged;
        }

        _custom = _renderers.FindPart(Part.Kind)?.Invoke(new BitAiMessagePartContext(Part, Message, Conversation, Session));

        base.OnParametersSet();
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        if (_subscribed is not null)
        {
            _subscribed.Changed -= OnPartChanged;
            _subscribed = null;
        }

        await base.DisposeAsync(disposing);
    }



    // Raised on whatever thread the transport streams on, so it only asks for a render, which is coalesced with every
    // other change of the same frame interval.
    private void OnPartChanged(BitAiMessagePart part) => RequestRender();

    private IReadOnlyList<BitAiCitation> GetCitations(BitAiSourcesPart part) =>
        part.CitationIds is { Count: > 0 } ids
            ? Message.Citations.Where(c => ids.Contains(c.CitationId)).ToArray()
            : Message.Citations;
}
