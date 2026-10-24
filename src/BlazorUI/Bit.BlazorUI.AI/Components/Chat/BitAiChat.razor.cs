using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// BitAiChat is the whole chat surface in one component: the transcript, the composer, the empty state and the
/// scroll-to-latest button, wired to a session.
/// </summary>
/// <remarks>
/// Supply a <see cref="Session"/> and it drives that one. Supply nothing and it creates one from the registered transport
/// (<c>services.AddBitBlazorUIAIServices(ai => ai.UseTransport&lt;T&gt;())</c>), which is what makes a working AI chat
/// three lines of markup. Every piece it composes - <see cref="BitAiMessageList"/>, <see cref="BitAiPromptInput"/>,
/// <see cref="BitAiEmptyState"/>, <see cref="BitAiScrollToLatest"/> - is public, so anything this component does can
/// also be assembled by hand.
/// </remarks>
public partial class BitAiChat : BitAiComponentBase
{
    private BitAiMessageList? _list;
    private BitAiPromptInput? _composer;
    private BitAiChatSession? _owned;
    private BitAiChatSession? _attached;
    private bool _pinned = true;



    [Inject] private IServiceProvider _services { get; set; } = default!;



    /// <summary>
    /// Gets or sets the cascading parameters for the chat component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple chat components through
    /// the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitAiChatParams.ParamName)]
    public BitAiChatParams? CascadingParameters { get; set; }



    /// <summary>
    /// Offers attachments in the composer.
    /// <br />
    /// The default value is <strong>true</strong>.
    /// </summary>
    [Parameter] public bool AllowAttachments { get; set; } = true;

    /// <summary>
    /// Offers dictation in the composer.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    [Parameter] public bool AllowDictation { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the chat.
    /// </summary>
    [Parameter, ResetClassBuilder] public BitAiChatClassStyles? Classes { get; set; }

    /// <summary>
    /// The commands offered when the user types a forward slash in the composer.
    /// </summary>
    [Parameter] public IReadOnlyList<BitAiSlashCommand>? Commands { get; set; }

    /// <summary>
    /// A hint rendered under the composer.
    /// </summary>
    [Parameter] public RenderFragment? ComposerHintTemplate { get; set; }

    /// <summary>
    /// The content placed before the text of the composer, such as a model picker.
    /// </summary>
    [Parameter] public RenderFragment? ComposerPrefixTemplate { get; set; }

    /// <summary>
    /// The content placed beside the send button of the composer.
    /// </summary>
    [Parameter] public RenderFragment? ComposerSuffixTemplate { get; set; }

    /// <summary>
    /// The content that replaces the whole composer.
    /// </summary>
    [Parameter] public RenderFragment? ComposerTemplate { get; set; }

    /// <summary>
    /// The conversation to show. Used only when no <see cref="Session"/> is supplied; otherwise the session owns it.
    /// </summary>
    [Parameter] public BitAiConversation? Conversation { get; set; }

    /// <summary>
    /// The supporting line of the empty state.
    /// </summary>
    [Parameter] public string? EmptyBody { get; set; }

    /// <summary>
    /// The content that replaces the empty state.
    /// </summary>
    [Parameter] public RenderFragment? EmptyTemplate { get; set; }

    /// <summary>
    /// The heading of the empty state.
    /// </summary>
    [Parameter] public string? EmptyTitle { get; set; }

    /// <summary>
    /// The content below the composer.
    /// </summary>
    [Parameter] public RenderFragment? FooterTemplate { get; set; }

    /// <summary>
    /// The content above the transcript, outside the scroll area.
    /// </summary>
    [Parameter] public RenderFragment? HeaderTemplate { get; set; }

    /// <summary>
    /// Hides the system messages from the transcript.
    /// <br />
    /// The default value is <strong>true</strong>.
    /// </summary>
    [Parameter] public bool HideSystemMessages { get; set; } = true;

    /// <summary>
    /// The content at the bottom of the scroll area, below the last message.
    /// </summary>
    [Parameter] public RenderFragment? ListFooterTemplate { get; set; }

    /// <summary>
    /// The content at the top of the scroll area, above the first message.
    /// </summary>
    [Parameter] public RenderFragment? ListHeaderTemplate { get; set; }

    /// <summary>
    /// The largest file the composer will stage, in bytes.
    /// <br />
    /// The default value is <strong>10 MB</strong>.
    /// </summary>
    [Parameter] public long MaxAttachmentSize { get; set; } = BitAiPromptInput.DefaultMaxAttachmentSize;

    /// <summary>
    /// The items offered when the user types an at sign in the composer.
    /// </summary>
    [Parameter] public IReadOnlyList<BitAiMentionItem>? Mentions { get; set; }

    /// <summary>
    /// The template that replaces how a message is rendered.
    /// </summary>
    [Parameter] public RenderFragment<BitAiMessage>? MessageTemplate { get; set; }

    /// <summary>
    /// The callback for each file the composer turns away. The user is told either way.
    /// </summary>
    [Parameter] public EventCallback<BitAiAttachmentRejection> OnAttachmentRejected { get; set; }

    /// <summary>
    /// The callback for when the user activates a citation.
    /// </summary>
    [Parameter] public EventCallback<BitAiCitation> OnCitationClick { get; set; }

    /// <summary>
    /// The callback for when the user records feedback on a reply, so a host can persist it.
    /// </summary>
    [Parameter] public EventCallback<BitAiMessageFeedback> OnFeedback { get; set; }

    /// <summary>
    /// The callback for when the user activates a source card.
    /// </summary>
    [Parameter] public EventCallback<BitAiCitationSource> OnSourceClick { get; set; }

    /// <summary>
    /// The callback for when the user submits. Handle it to submit yourself, for example after uploading the
    /// attachments; leave it unset and the chat submits to the session.
    /// </summary>
    [Parameter] public EventCallback<BitAiPromptSubmission> OnSubmit { get; set; }

    /// <summary>
    /// The placeholder of the composer.
    /// </summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>
    /// The session to drive. When unset, one is created from the registered transport.
    /// </summary>
    [Parameter] public BitAiChatSession? Session { get; set; }

    /// <summary>
    /// The settings of a session this component creates.
    /// </summary>
    [Parameter] public BitAiChatSessionOptions? SessionOptions { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the chat.
    /// </summary>
    [Parameter, ResetStyleBuilder] public BitAiChatClassStyles? Styles { get; set; }

    /// <summary>
    /// The example prompts offered in the empty state.
    /// </summary>
    [Parameter] public IReadOnlyList<string>? Suggestions { get; set; }

    /// <summary>
    /// The presentation settings of everything inside this chat, in place of the cascaded or registered ones.
    /// </summary>
    [Parameter] public BitAiUiOptions? UiOptions { get; set; }



    /// <summary>
    /// The session in use, whether supplied or created here.
    /// </summary>
    public BitAiChatSession ActiveSession => Session ?? (_owned ??= CreateSession());

    /// <summary>
    /// The conversation in use.
    /// </summary>
    public BitAiConversation ActiveConversation => ActiveSession.Conversation;



    /// <summary>
    /// Puts the caret in the composer.
    /// </summary>
    public ValueTask FocusComposerAsync() => _composer?.FocusAsync() ?? ValueTask.CompletedTask;



    protected override string RootElementClass => "bit-aicht";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitAiChatParams))]
    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        base.OnParametersSet();

        if (UiOptions is not null) CascadedOptions = UiOptions;

        // A session this chat created is ended once the host supplies its own: it would otherwise live on, with its
        // conversation and its timers, for as long as the chat does, and come back as the active session - its old
        // conversation with it - were the Session parameter ever unset again. A different Conversation parameter on an
        // owned session means the host switched threads: the owned session is replaced rather than kept driving the old
        // conversation.
        if (_owned is not null
            && (Session is not null || (Conversation is not null && ReferenceEquals(_owned.Conversation, Conversation) is false)))
        {
            var stale = _owned;
            _owned = null;

            if (ReferenceEquals(_attached, stale))
            {
                stale.StateChanged -= OnSessionStateChanged;
                _attached = null;
            }

            _ = stale.DisposeAsync().AsTask();
        }

        var session = ActiveSession;

        if (ReferenceEquals(_attached, session)) return;

        if (_attached is not null) _attached.StateChanged -= OnSessionStateChanged;

        _attached = session;
        _attached.StateChanged += OnSessionStateChanged;
    }



    private BitAiChatSession CreateSession()
    {
        if (_services.GetService(typeof(BitAiChatSessionFactory)) is BitAiChatSessionFactory factory)
        {
            return factory.Create(Conversation, WithStrings(SessionOptions ?? factory.Defaults));
        }

        if (_services.GetService(typeof(IBitAiTransport)) is IBitAiTransport transport)
        {
            return new BitAiChatSession(transport, Conversation, WithStrings(SessionOptions ?? new BitAiChatSessionOptions()));
        }

        throw new InvalidOperationException(
            "BitAiChat needs a session or a registered IBitAiTransport. Call services.AddBitBlazorUIAIServices(ai => ai.UseTransport<T>()) " +
            "during startup, or pass a Session parameter.");
    }

    // The errors a session of this chat's own writes read in the words of the UI options in force here, which is what
    // the transcript renders them with, unless the session options name their own.
    private BitAiChatSessionOptions WithStrings(BitAiChatSessionOptions options) =>
        options.Strings is null ? options with { Strings = Strings } : options;

    private void OnSessionStateChanged() => RequestRender();

    private void HandleOnPinnedChange(bool pinned)
    {
        _pinned = pinned;

        RequestRender();
    }

    private Task ScrollToLatestAsync() => _list?.ScrollToLatestAsync() ?? Task.CompletedTask;

    private async Task OnComposerSubmitAsync(BitAiPromptSubmission submission)
    {
        if (OnSubmit.HasDelegate)
        {
            await OnSubmit.InvokeAsync(submission);
            return;
        }

        await ActiveSession.SubmitAsync(submission.Text, submission.Attachments);
    }

    private Task SubmitTextAsync(string text) => OnComposerSubmitAsync(new BitAiPromptSubmission(text, []));



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        if (_attached is not null) _attached.StateChanged -= OnSessionStateChanged;

        _attached = null;

        // Only a session this chat created is its to end; a supplied one belongs to the host.
        var owned = _owned;
        _owned = null;

        if (owned is not null) await owned.DisposeAsync();

        await base.DisposeAsync(disposing);
    }
}
