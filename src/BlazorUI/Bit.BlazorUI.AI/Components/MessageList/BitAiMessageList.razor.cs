namespace Bit.BlazorUI;

/// <summary>
/// BitAiMessageList is the scrolling transcript of a conversation: every message of it, laid out by role, with
/// auto-scroll that follows new content only while the reader is at the newest message.
/// </summary>
/// <remarks>
/// Two behaviours make or break this component. Auto-scroll follows new content only while the user is already at the
/// bottom, so scrolling up to re-read is never yanked away. And a long transcript is not drawn whole: past
/// <see cref="BitAiUiOptions.VirtualizeThreshold"/> messages only that many of the newest are, and the older ones a page
/// at a time as the reader scrolls up to them, with what they are reading kept in place as each page comes in above it.
/// Nothing is estimated - every message drawn is drawn at its own height - so the scrollbar never jumps and the view
/// never goes blank, and once the reader is back at the newest message the pages above it are let go of again.
/// <br />
/// The list subscribes to the conversation for the messages added to and removed from it, which arrive from whatever
/// thread the stream is on, and re-renders at most once per <see cref="BitAiUiOptions.RenderInterval"/>. What a reply
/// streams into it is drawn by the view of that message's part, so a token re-renders that part, never the transcript.
/// </remarks>
public partial class BitAiMessageList : BitAiComponentBase
{
    private readonly List<BitAiMessage> _messages = [];
    // The messages added since the list was opened on its conversation, the only ones that play the entrance: the history
    // it opened on (restored, or there before the first render) keeps still through every later render.
    private readonly HashSet<BitAiMessage> _entering = new(ReferenceEqualityComparer.Instance);
    // The newest message of the list, kept beside it for the one handler that runs off the renderer's dispatcher
    // (OnMessageUpdated): a read of the list itself there would race the mutations made on it.
    private BitAiMessage? _latest;
    private DotNetObjectReference<BitAiMessageList>? _dotnetObj;
    private BitAiConversation? _subscribed;
    private bool? _appliedHideSystemMessages;
    private bool _prerendering;
    private bool _pinned = true;
    private bool _initialised;
    private bool _observing;
    private bool _observedAutoScroll;
    // The index in the list of the first message drawn: every message from it on is, and the ones before it are not.
    private int _windowStart;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Gets or sets the cascading parameters for the message list component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple message list components
    /// through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitAiMessageListParams.ParamName)]
    public BitAiMessageListParams? CascadingParameters { get; set; }



    /// <summary>
    /// Custom CSS classes for different parts of the message list.
    /// </summary>
    [Parameter, ResetClassBuilder] public BitAiMessageListClassStyles? Classes { get; set; }

    /// <summary>
    /// The conversation to render.
    /// </summary>
    [Parameter, EditorRequired] public BitAiConversation Conversation { get; set; } = default!;

    /// <summary>
    /// The content rendered instead of an empty list.
    /// </summary>
    [Parameter] public RenderFragment? EmptyTemplate { get; set; }

    /// <summary>
    /// The content rendered below the last message, inside the scroll area.
    /// </summary>
    [Parameter] public RenderFragment? FooterTemplate { get; set; }

    /// <summary>
    /// The content rendered above the first message, inside the scroll area.
    /// </summary>
    /// <remarks>
    /// Past <see cref="BitAiUiOptions.VirtualizeThreshold"/> messages it is drawn once the reader has scrolled up to the
    /// first message, along with it.
    /// </remarks>
    [Parameter] public RenderFragment? HeaderTemplate { get; set; }

    /// <summary>
    /// Hides the messages the user did not write and the model did not produce (the system and developer ones).
    /// <br />
    /// The default value is <strong>true</strong>.
    /// </summary>
    [Parameter] public bool HideSystemMessages { get; set; } = true;

    /// <summary>
    /// The template that replaces the built-in rendering of a message.
    /// </summary>
    /// <remarks>
    /// The template is keyed by the message, so removing one message never recreates the ones after it, and it adds no
    /// element of its own around what it renders. The list draws it again as the newest message changes, so a template
    /// showing the text of a reply follows it as it streams.
    /// </remarks>
    [Parameter] public RenderFragment<BitAiMessage>? MessageTemplate { get; set; }

    /// <summary>
    /// The callback for when the user activates a citation.
    /// </summary>
    [Parameter] public EventCallback<BitAiCitation> OnCitationClick { get; set; }

    /// <summary>
    /// The callback for when the user records feedback on a reply, so a host can persist it.
    /// </summary>
    [Parameter] public EventCallback<BitAiMessageFeedback> OnFeedback { get; set; }

    /// <summary>
    /// The callback for when the view stops or starts following the newest message: false when the user scrolls away
    /// from it, true when they scroll back to it or <see cref="ScrollToLatestAsync"/> brings them there.
    /// </summary>
    [Parameter] public EventCallback<bool> OnPinnedChange { get; set; }

    /// <summary>
    /// The callback for when the user activates a source card.
    /// </summary>
    [Parameter] public EventCallback<BitAiCitationSource> OnSourceClick { get; set; }

    /// <summary>
    /// The session driving the conversation, which enables the message-level actions.
    /// </summary>
    [Parameter] public BitAiChatSession? Session { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the message list.
    /// </summary>
    [Parameter, ResetStyleBuilder] public BitAiMessageListClassStyles? Styles { get; set; }



    /// <summary>
    /// Whether the view is following new content.
    /// </summary>
    public bool IsPinned => _pinned;



    /// <summary>
    /// Scrolls to the newest message and resumes following new content.
    /// </summary>
    /// <param name="smooth">Animates the scroll, unless the reader asked for reduced motion.</param>
    public async Task ScrollToLatestAsync(bool smooth = true)
    {
        var changed = _pinned is false;

        _pinned = true;

        if (IsRendered && IsDisposed is false)
        {
            await _js.BitAiMessageListScrollToBottom(UniqueId, RootElement, smooth);
        }

        if (changed)
        {
            await OnPinnedChange.InvokeAsync(true);
        }
    }

    /// <summary>
    /// Called by the scroll script of the message list when the user scrolls to or away from the newest message.
    /// <br />
    /// <strong>This method is not intended to be called from application code.</strong>
    /// </summary>
    /// <param name="pinned">Whether the view is at the newest message.</param>
    [JSInvokable("OnPinnedChange")]
    public async Task _OnPinnedChange(bool pinned)
    {
        if (IsDisposing || _pinned == pinned) return;

        _pinned = pinned;

        await OnPinnedChange.InvokeAsync(pinned);

        // Back at the newest message, the reader has left the older pages they scrolled up through behind them: drawn
        // above the view, they would cost every render of the list.
        if (pinned && IsDisposing is false && TrimWindow())
        {
            await InvokeAsync(StateHasChanged);
        }
    }

    /// <summary>
    /// Called by the scroll script of the message list when the user nears the first message it draws while older ones
    /// are left out (past <see cref="BitAiUiOptions.VirtualizeThreshold"/> messages), to draw the next page of them.
    /// <br />
    /// <strong>This method is not intended to be called from application code.</strong>
    /// </summary>
    [JSInvokable("OnStartReached")]
    public async Task _OnStartReached()
    {
        if (IsDisposing || _windowStart == 0) return;

        _windowStart = Math.Max(0, _windowStart - PageSize);

        await InvokeAsync(StateHasChanged);
    }



    protected override string RootElementClass => "bit-aimsl";

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
        // A static render (a prerender, or a page rendered by static SSR) stays one for the life of the list.
        _prerendering = IsStaticRender(_js);

        base.OnInitialized();
    }

    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        base.OnParametersSet();

        if (ReferenceEquals(_subscribed, Conversation) is false)
        {
            Detach();

            _subscribed = Conversation;
            _subscribed.MessageAdded += OnMessageAdded;
            _subscribed.MessageRemoved += OnMessageRemoved;
            _subscribed.MessageUpdated += OnMessageUpdated;

            Rebuild();

            // The first render of another conversation starts at its newest message, without animating.
            _initialised = false;
            _entering.Clear();

            return;
        }

        // Which messages are shown is decided as they arrive, so a change of the filter rebuilds the list.
        if (_appliedHideSystemMessages != HideSystemMessages)
        {
            Rebuild();
            return;
        }

        // Options with a higher threshold draw more of the newest messages at once.
        ClampWindow();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (IsDisposed) return;

        if (_initialised is false)
        {
            // The first render, or the first after switching conversations: start at the newest message without
            // animating, then follow from here. The script takes the list as pinned with this call; an app told the
            // reader had left the newest message of the conversation before is told they are at the newest one again.
            _initialised = true;

            await _js.BitAiMessageListScrollToBottom(UniqueId, RootElement, false);

            if (IsDisposed) return;

            if (_pinned is false)
            {
                _pinned = true;

                await OnPinnedChange.InvokeAsync(true);

                if (IsDisposed) return;
            }
        }

        if (firstRender)
        {
            // Observed only once it is at the newest message, so the script starts where this side does - pinned -
            // rather than reading the top of a transcript about to be scrolled away from as the reader having left the
            // newest message, which reported a false and then a true on every open.
            _dotnetObj = DotNetObjectReference.Create(this);
            _observing = true;
            _observedAutoScroll = Options.AutoScroll;

            await _js.BitAiMessageListObserve(UniqueId, RootElement, _dotnetObj, _observedAutoScroll);
        }
        else if (_observing && _observedAutoScroll != Options.AutoScroll)
        {
            // The script follows the content as it grows on its own, so it is told when the option changes.
            _observedAutoScroll = Options.AutoScroll;

            await _js.BitAiMessageListSetAutoScroll(UniqueId, _observedAutoScroll);
        }

        // Nothing is scrolled from here after a render: the script follows every change of the content the moment the
        // browser makes it, and knows - as this side, a round trip behind, cannot - whether the reader has just left.
    }



    private bool IsLatestMessage(BitAiMessage message) => _messages.Count > 0 && ReferenceEquals(message, _messages[^1]);

    // How many of the newest messages are drawn once the list holds more (BitAiUiOptions.VirtualizeThreshold), and how many
    // older ones each step up towards the start adds.
    private int TailSize => Math.Max(1, Options.VirtualizeThreshold);

    private int PageSize => Math.Max(1, TailSize / 2);

    // The number of older messages left out, written on the root for the script (data-bit-aimsl-old): it asks for the
    // next page of them as the reader nears the start of what is drawn, and reads the number changing as that page drawn.
    private int? OlderCount => _windowStart > 0 ? _windowStart : null;

    private void Rebuild()
    {
        _messages.Clear();
        _appliedHideSystemMessages = HideSystemMessages;

        foreach (var message in Conversation.Messages)
        {
            if (HideSystemMessages && message.Role.IsSystem) continue;

            _messages.Add(message);
        }

        // A conversation opens on its newest messages, interactive or prerendered alike: a static render has no reader
        // to scroll up, so the bounded tail is all it ever draws, and the interactive render that follows it starts on
        // the same messages rather than flashing the whole transcript.
        _windowStart = Math.Max(0, _messages.Count - TailSize);

        UpdateLatest();
    }

    // At least the tail is drawn, so a message removed from it brings an older one in rather than leaving the view short.
    private void ClampWindow() => _windowStart = Math.Clamp(_windowStart, 0, Math.Max(0, _messages.Count - TailSize));

    // Draws the tail alone again once more than a page of older messages is drawn above it. The page of slack keeps a
    // tail too short to fill the view - which the script then extends at once - from being let go of and drawn again
    // with every message.
    private bool TrimWindow()
    {
        var start = Math.Max(0, _messages.Count - TailSize);

        if (start - _windowStart <= PageSize) return false;

        // Drawn again later, they are history: they come back without the entrance.
        for (var i = _windowStart; i < start; i++)
        {
            _entering.Remove(_messages[i]);
        }

        _windowStart = start;

        return true;
    }

    private void UpdateLatest() => Volatile.Write(ref _latest, _messages.Count > 0 ? _messages[^1] : null);

    // The session raises these from whatever thread the stream is on. The list is enumerated during rendering, so it is
    // only ever mutated on the renderer's dispatcher.
    private void OnMessageAdded(BitAiConversation conversation, BitAiMessage message) => _ = InvokeAsync(() =>
    {
        if (ReferenceEquals(conversation, _subscribed) is false) return;
        if (HideSystemMessages && message.Role.IsSystem) return;
        if (_messages.Contains(message)) return;

        _messages.Add(message);
        if (_initialised && _prerendering is false) _entering.Add(message);
        UpdateLatest();

        // A reader at the newest message sees the new one with the tail above it; one scrolled up keeps every page
        // they have scrolled through.
        if (_pinned) TrimWindow();

        RequestRender();
    });

    private void OnMessageRemoved(BitAiConversation conversation, BitAiMessage message) => _ = InvokeAsync(() =>
    {
        if (ReferenceEquals(conversation, _subscribed) is false) return;

        var index = _messages.IndexOf(message);
        if (index < 0) return;

        _messages.RemoveAt(index);
        _entering.Remove(message);

        // The first message drawn stays the one it was when an older one goes.
        if (index < _windowStart) _windowStart--;

        ClampWindow();
        UpdateLatest();
        RequestRender();
    });

    // Runs on the thread of the stream, on every token. A message the list draws as a BitAiChatMessage follows its own
    // changes, part by part, so nothing here re-renders the transcript for it. A MessageTemplate is markup of the app's
    // that only the list draws, so the newest message - the one a stream changes - is drawn through it again. It reads
    // nothing of the list (mutated on the dispatcher), only the newest message kept beside it, and asks for a render,
    // which is safe from any thread.
    private void OnMessageUpdated(BitAiConversation conversation, BitAiMessage message)
    {
        if (MessageTemplate is null) return;

        if (ReferenceEquals(message, Volatile.Read(ref _latest))) RequestRender();
    }

    private void Detach()
    {
        if (_subscribed is null) return;

        _subscribed.MessageAdded -= OnMessageAdded;
        _subscribed.MessageRemoved -= OnMessageRemoved;
        _subscribed.MessageUpdated -= OnMessageUpdated;
        _subscribed = null;
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        Detach();

        if (_observing)
        {
            _observing = false;

            try
            {
                await _js.BitAiMessageListDispose(UniqueId);
            }
            catch (JSDisconnectedException) { } // the circuit is gone along with the element it observed
            catch (ObjectDisposedException) { } // the JS runtime of a WebAssembly host may go first
        }

        // Released only once the script has let go of it, so a report it sent in the meantime still finds the list -
        // which then ignores it, being on its way out (IsDisposing).
        _dotnetObj?.Dispose();
        _dotnetObj = null;

        await base.DisposeAsync(disposing);
    }
}
