using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// BitAiChatMessage is one message of a conversation, laid out by role: a user turn as a bubble at the end of the line,
/// an assistant turn as full-width prose with its parts, sources and actions, and a system note as a centered pill.
/// </summary>
/// <remarks>
/// The message is subscribed to for structural changes only. Content changes inside a part are handled by that part's
/// own component, so a streaming token does not re-render this subtree.
/// </remarks>
public partial class BitAiChatMessage : BitAiComponentBase
{
    private bool _editing;
    private bool _awaitingContent;
    private string? _draft = string.Empty;
    private BitAiMessage? _subscribed;
    private BitAiIconClassesCache<BitButtonClassStyles> _editButtonClasses;



    /// <summary>
    /// Gets or sets the cascading parameters for the chat message component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple chat message components
    /// through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitAiChatMessageParams.ParamName)]
    public BitAiChatMessageParams? CascadingParameters { get; set; }



    /// <summary>
    /// Plays the entrance animation of the message. Off for the messages restored from history.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    [Parameter, ResetClassBuilder] public bool Animate { get; set; }

    /// <summary>
    /// The extra content rendered at the end of an assistant message.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the chat message.
    /// </summary>
    [Parameter, ResetClassBuilder] public BitAiChatMessageClassStyles? Classes { get; set; }

    /// <summary>
    /// The conversation the message belongs to, used to resolve artifacts.
    /// </summary>
    [Parameter] public BitAiConversation? Conversation { get; set; }

    /// <summary>
    /// Whether this is the newest message in the transcript. Regeneration is offered only for the newest reply, because
    /// regenerating an older one discards everything after it.
    /// <br />
    /// The default value is <strong>true</strong>.
    /// </summary>
    [Parameter] public bool IsLatest { get; set; } = true;

    /// <summary>
    /// The message to render.
    /// </summary>
    [Parameter, EditorRequired, ResetClassBuilder] public BitAiMessage Message { get; set; } = default!;

    /// <summary>
    /// The callback for when the user activates a citation.
    /// </summary>
    [Parameter] public EventCallback<BitAiCitation> OnCitationClick { get; set; }

    /// <summary>
    /// The callback for when the user records feedback on this message, so a host can persist it.
    /// </summary>
    [Parameter] public EventCallback<BitAiMessageFeedback> OnFeedback { get; set; }

    /// <summary>
    /// The callback for when the user activates a source card.
    /// </summary>
    [Parameter] public EventCallback<BitAiCitationSource> OnSourceClick { get; set; }

    /// <summary>
    /// The session driving the conversation, which enables edit, retry, and approval.
    /// </summary>
    [Parameter] public BitAiChatSession? Session { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the chat message.
    /// </summary>
    [Parameter, ResetStyleBuilder] public BitAiChatMessageClassStyles? Styles { get; set; }



    protected override string RootElementClass => "bit-aicms";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root)
                    .Register(() => Message is null ? string.Empty
                                  : Message.Role.IsUser ? "bit-aicms-usr"
                                  : Message.Role.IsSystem ? "bit-aicms-sys"
                                  : "bit-aicms-ast")
                    .Register(() => Animate ? "bit-aicms-ent" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitAiChatMessageParams))]
    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        base.OnParametersSet();

        if (ReferenceEquals(_subscribed, Message)) return;

        Unsubscribe();

        _subscribed = Message;
        _subscribed.Changed += OnMessageChanged;
        _subscribed.PartChanged += OnPartChanged;
    }



    // The sources a reply cites are listed under it, unless one of its parts lists them already.
    private bool _ShowSourceList => Options.ShowSourceList
                                    && Message.Citations.Count > 0
                                    && Message.Parts.Any(p => p is BitAiSourcesPart) is false;

    // The glyph is not one of the core font's, so it is drawn from this package's own subset (see BitAiIconClasses),
    // and the button is handed the same classes while the app's own stay the same.
    private BitButtonClassStyles _EditButtonClasses =>
        _editButtonClasses.Get("bit-icon-ai bit-icon-ai--Edit", Classes?.EditButton, BitAiIconClasses.With);

    private void OnMessageChanged(BitAiMessage message) => RequestRender();

    // The parts render their own growth, so the only change of one that matters here is the first text reaching a reply
    // drawn as thinking: a part a transport opens before its first delta is added empty, and filling it in changes no
    // structure of the message. Every other delta costs a field read.
    private void OnPartChanged(BitAiMessage message, BitAiMessagePart part)
    {
        if (_awaitingContent && IsAwaitingContent() is false) RequestRender();
    }

    // Whether the reply is being generated and has nothing to show yet: no part, or only prose no text has reached. A
    // reasoning part, a code block, a tool call, an image and the rest draw something of their own from the start, and
    // so does an agent run, which shows its own progress.
    private bool IsAwaitingContent()
    {
        if (Message.IsBusy is false || Message.Run is not null) return false;

        foreach (var part in Message.Parts)
        {
            if (part is not BitAiTextPart { IsEmpty: true } || part is BitAiReasoningPart or BitAiCodePart) return false;
        }

        return true;
    }

    private void Unsubscribe()
    {
        if (_subscribed is null) return;

        _subscribed.Changed -= OnMessageChanged;
        _subscribed.PartChanged -= OnPartChanged;
    }

    private Task OnFeedbackRecorded(BitAiFeedbackKind feedback) =>
        OnFeedback.HasDelegate ? OnFeedback.InvokeAsync(new BitAiMessageFeedback(Message, feedback)) : Task.CompletedTask;

    private void StartEdit()
    {
        if (Disabled) return;

        _draft = Message.GetText();
        _editing = true;
    }

    private void CancelEdit()
    {
        _editing = false;
    }

    private async Task SaveEditAsync()
    {
        if (Disabled) return;

        _editing = false;

        if (Session is not null) await Session.EditAsync(Message.MessageId, _draft ?? string.Empty);
    }

    private async Task RetryAsync()
    {
        if (Disabled) return;

        if (Session is not null) await Session.RetryAsync();
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        Unsubscribe();

        _subscribed = null;

        await base.DisposeAsync(disposing);
    }
}
