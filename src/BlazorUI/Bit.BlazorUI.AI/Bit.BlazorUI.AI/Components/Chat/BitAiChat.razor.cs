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
    // Whether a turn was in flight when the chat last asked to render for its session, and for which session: read and
    // written under the lock, since the session raises its changes from the threads of the turn and its tools at once.
    private readonly object _sessionStateGate = new();
    private BitAiChatSession? _renderedSession;
    private bool _renderedGenerating;
    private BitAiReadAloud? _readAloud;
    private IBitAiTextToSpeech? _textToSpeech;



    [Inject] private IServiceProvider _services { get; set; } = default!;
    [Inject] private IJSRuntime _js { get; set; } = default!;



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
    /// Offers a "Read aloud" action under each reply, which reads it through the app's text-to-speech - the
    /// <see cref="Synthesizer"/>, or an <see cref="IBitAiTextToSpeech"/> service registered through
    /// <c>AddBitBlazorUIAIServices(ai =&gt; ai.UseTextToSpeech&lt;T&gt;())</c>. With neither, nothing is offered.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// The chat cascades the <see cref="BitAiReadAloud"/> that drives the action to everything inside it, so a
    /// <see cref="MessageActionsTemplate"/> can use it too. A dictation in the composer silences a reading while it
    /// listens, so the microphone does not pick it up.
    /// </remarks>
    [Parameter] public bool AllowReadAloud { get; set; }

    /// <summary>
    /// Uploads each file as the composer stages it, and returns the attachment to send in its place (see
    /// <see cref="BitAiPromptInput.AttachmentUploader"/>).
    /// </summary>
    [Parameter] public Func<BitAiAttachment, IProgress<double>, CancellationToken, Task<BitAiAttachment>>? AttachmentUploader { get; set; }

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
    /// Goes on reading each reply that finishes after the one a reading was started on, until the reading is stopped -
    /// for a user who listens instead of reading. Only used when <see cref="AllowReadAloud"/> is on.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    [Parameter] public bool KeepReadingAloud { get; set; }

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
    /// The buttons of the app appended to the actions of each message, given the message: after copy, regenerate and
    /// feedback under a reply, after copy and edit beside a user message.
    /// </summary>
    [Parameter] public RenderFragment<BitAiMessage>? MessageActionsTemplate { get; set; }

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
    /// <remarks>
    /// A handler that could not send the prompt calls <see cref="BitAiPromptSubmission.Reject"/>, and the composer puts
    /// the text and the attachments back. Unhandled, a submission the session does not take - a turn is in flight - is
    /// put back the same way.
    /// </remarks>
    [Parameter] public EventCallback<BitAiPromptSubmission> OnSubmit { get; set; }

    /// <summary>
    /// The placeholder of the composer.
    /// </summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>
    /// The language the replies are read aloud in, as a BCP 47 language tag such as <c>en-US</c>, handed to the
    /// <see cref="IBitAiTextToSpeech"/> service; null leaves it to the service (or to the <see cref="BitAiReadAloud.Language"/>
    /// set on <see cref="ReadAloud"/>). A <see cref="Synthesizer"/> reads the language it wants itself. Only used when
    /// <see cref="AllowReadAloud"/> is on.
    /// </summary>
    [Parameter] public string? ReadAloudLanguage { get; set; }

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
    /// The app's text-to-speech the replies are read aloud through, given the reply - its text
    /// (<see cref="BitAiMessage.GetText"/>, Markdown), and whatever else the service needs of it, such as a signature it
    /// carries - and a token cancelled when the reading is stopped; it returns the audio to play, or null when there is
    /// nothing to read. When null, an <see cref="IBitAiTextToSpeech"/> service registered through
    /// <c>AddBitBlazorUIAIServices</c> is used, if there is one. Only used when <see cref="AllowReadAloud"/> is on.
    /// </summary>
    [Parameter] public Func<BitAiMessage, CancellationToken, Task<BitAiSpeechAudio?>>? Synthesizer { get; set; }

    /// <summary>
    /// The example prompts offered in the empty state.
    /// </summary>
    /// <remarks>
    /// The prompts offered under a reply, to go on with, are the reply's own (<see cref="BitAiMessage.FollowUps"/>, which a
    /// transport sends with a <see cref="BitAiFollowUpsEvent"/>), and are sent the way these are.
    /// </remarks>
    [Parameter] public IReadOnlyList<string>? Suggestions { get; set; }

    /// <summary>
    /// The app's speech-to-text the composer's dictation transcribes a recording of the microphone with, in place of the
    /// speech API of the browser (see <see cref="BitAiVoiceInput.Transcriber"/>). When null, an
    /// <see cref="IBitAiSpeechToText"/> service registered through <c>AddBitBlazorUIAIServices</c> is used, if there is one.
    /// Only used when <see cref="AllowDictation"/> is on.
    /// </summary>
    [Parameter] public Func<Stream, string, CancellationToken, Task<string?>>? Transcriber { get; set; }

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
    /// What reads the replies aloud, while <see cref="AllowReadAloud"/> is on and there is a text-to-speech to read
    /// through.
    /// </summary>
    public BitAiReadAloud? ReadAloud => _readAloud;



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

    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        base.OnParametersSet();

        if (UiOptions is not null) CascadedOptions = UiOptions;

        UpdateReadAloud();

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
                Detach(stale);
                _attached = null;
            }

            _ = stale.DisposeAsync().AsTask();
        }

        var session = ActiveSession;

        if (ReferenceEquals(_attached, session)) return;

        if (_attached is not null) Detach(_attached);

        // What was being read belongs to the conversation the chat no longer shows.
        if (_attached is not null && _readAloud is { } reader) _ = reader.StopAsync();

        _attached = session;
        _attached.StateChanged += OnSessionStateChanged;
        _attached.TurnCompleted += OnTurnCompleted;

        // The render this attaches it for draws the session as it is now.
        lock (_sessionStateGate)
        {
            _renderedSession = session;
            _renderedGenerating = session.IsGenerating;
        }
    }



    private BitAiChatSession CreateSession()
    {
        if (_services.GetService(typeof(BitAiChatSessionFactory)) is BitAiChatSessionFactory factory)
        {
            return factory.Create(Conversation, WithUiOptions(SessionOptions ?? factory.Defaults));
        }

        if (_services.GetService(typeof(IBitAiTransport)) is IBitAiTransport transport)
        {
            return new BitAiChatSession(transport, Conversation, WithUiOptions(SessionOptions ?? new BitAiChatSessionOptions()));
        }

        throw new InvalidOperationException(
            "BitAiChat needs a session or a registered IBitAiTransport. Call services.AddBitBlazorUIAIServices(ai => ai.UseTransport<T>()) " +
            "during startup, or pass a Session parameter.");
    }

    // A session of this chat's own follows the UI options in force here: the errors it writes read in their words, which
    // is what the transcript renders them with, unless the session options name their own; and its text arrives as the
    // transport sends it, unpaced, where they turn SmoothStreaming off.
    private BitAiChatSessionOptions WithUiOptions(BitAiChatSessionOptions options)
    {
        if (options.Strings is null) options = options with { Strings = Strings };

        if (Options.SmoothStreaming is false && options.Pacing is not null) options = options with { Pacing = null };

        return options;
    }

    // The session raises this on every event of a stream. What a stream changes is drawn by the views of the messages and
    // their parts, each following its own changes; what the chat itself hands down from the session is whether a turn is
    // in flight (the composer's Stop, the actions of the messages), so it re-renders when that flips rather than per
    // token - a render of the chat re-renders the transcript and every message in it.
    private void OnSessionStateChanged()
    {
        var session = _attached;
        if (session is null) return;

        lock (_sessionStateGate)
        {
            var generating = session.IsGenerating;

            if (ReferenceEquals(session, _renderedSession) && generating == _renderedGenerating) return;

            _renderedSession = session;
            _renderedGenerating = generating;
        }

        RequestRender();
    }

    // A turn that has ended hands over its reply, which is read when a reading waits for it or keep reading follows the
    // conversation - the reader reads only a reply that finished normally, not one stopped or failed half way. Raised on
    // the thread of the turn, and read on the renderer's dispatcher, where the reader is driven from.
    private void OnTurnCompleted(BitAiMessage reply)
    {
        var session = _attached;

        if (session is null || _readAloud is null) return;

        _ = InvokeAsync(() => ReadFinishedReplyAsync(session, reply));
    }

    private async Task ReadFinishedReplyAsync(BitAiChatSession session, BitAiMessage reply)
    {
        if (IsDisposing || _readAloud is not { } reader || ReferenceEquals(session, _attached) is false) return;

        await reader.ReadNextAsync(reply);
    }

    private void Detach(BitAiChatSession session)
    {
        session.StateChanged -= OnSessionStateChanged;
        session.TurnCompleted -= OnTurnCompleted;
    }

    // The reader is created once read aloud is on and there is something to synthesize through, and goes only when
    // either goes. It synthesizes through whatever the chat is handed at the time of each reading, so a synthesizer that
    // is a new delegate on every render of the host (a lambda capturing a local) neither stops what plays nor loses what
    // the app set on the reader (its Language, PlaybackRate, handlers).
    private void UpdateReadAloud()
    {
        _textToSpeech = AllowReadAloud && Synthesizer is null ? _services.GetService(typeof(IBitAiTextToSpeech)) as IBitAiTextToSpeech : null;

        if (AllowReadAloud is false || (Synthesizer is null && _textToSpeech is null))
        {
            ReleaseReadAloud();
            return;
        }

        _readAloud ??= new BitAiReadAloud(_js, SynthesizeAsync);
        _readAloud.KeepReading = KeepReadingAloud;
    }

    private Task<BitAiSpeechAudio?> SynthesizeAsync(BitAiMessage reply, CancellationToken cancellationToken)
    {
        if (Synthesizer is { } synthesizer) return synthesizer(reply, cancellationToken);

        if (_textToSpeech is { } service) return service.SynthesizeAsync(reply.GetText(), ReadAloudLanguage ?? _readAloud?.Language, cancellationToken);

        return Task.FromResult<BitAiSpeechAudio?>(null);
    }

    private void ReleaseReadAloud()
    {
        var reader = _readAloud;
        _readAloud = null;

        if (reader is not null) _ = reader.DisposeAsync().AsTask();
    }

    private void HandleOnPinnedChange(bool pinned)
    {
        _pinned = pinned;

        RequestRender();
    }

    // The button goes as the transcript reaches the newest message, and the focus would go with it to the page: the
    // composer takes it, where a reader who asked for the newest message goes on from.
    private async Task ScrollToLatestAsync()
    {
        if (_list is not null) await _list.ScrollToLatestAsync();

        await FocusComposerSafelyAsync();
    }

    private async Task OnComposerSubmitAsync(BitAiPromptSubmission submission)
    {
        if (OnSubmit.HasDelegate)
        {
            await OnSubmit.InvokeAsync(submission);
            return;
        }

        // A session that took nothing - a turn it could not interrupt was in flight - leaves the prompt the user's.
        if (await ActiveSession.SubmitAsync(submission.Text, submission.Attachments) is null)
        {
            submission.Reject();
        }
    }

    // A suggestion goes with the empty state as soon as its message is in, and the focus would go with it to the page:
    // the composer takes it first, since the submission is over only once the whole turn is. A follow-up under a reply
    // goes the same way, with the reply it was offered under no longer the newest. Either goes only while no turn is in
    // flight, whatever the session's InterruptOnSubmit says: a second click on the button before the render takes it away
    // would stop the turn the first click started and send the same prompt again.
    private async Task SubmitTextAsync(string text)
    {
        await FocusComposerSafelyAsync();

        if (ActiveSession.IsGenerating) return;

        if (OnSubmit.HasDelegate)
        {
            await OnSubmit.InvokeAsync(new BitAiPromptSubmission(text, []));
            return;
        }

        await ActiveSession.SubmitIfIdleAsync(text);
    }

    // Moving the focus on the chat's own behalf, after a control the user pressed has gone, never fails what it follows.
    private async Task FocusComposerSafelyAsync()
    {
        if (IsDisposing) return;

        try
        {
            await FocusComposerAsync();
        }
        catch (JSDisconnectedException) { } // the circuit is gone, and the page with it
        catch (OperationCanceledException) { }
        catch (JSException) { } // a composer taken off the page in the meantime has nothing to focus
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        if (_attached is not null) Detach(_attached);

        _attached = null;

        if (_readAloud is { } reader)
        {
            _readAloud = null;
            await reader.DisposeAsync();
        }

        // Only a session this chat created is its to end; a supplied one belongs to the host.
        var owned = _owned;
        _owned = null;

        if (owned is not null) await owned.DisposeAsync();

        await base.DisposeAsync(disposing);
    }
}
