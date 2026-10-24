using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// BitAiMessageList is the scrolling transcript of a conversation: every message of it, laid out by role, with
/// auto-scroll that follows new content only while the reader is at the newest message.
/// </summary>
/// <remarks>
/// Two behaviours make or break this component. Auto-scroll follows new content only while the user is already at the
/// bottom, so scrolling up to re-read is never yanked away. Virtualization turns on only past a threshold
/// (<see cref="BitAiUiOptions.VirtualizeThreshold"/>), because below it the variable heights of chat messages make a
/// virtualized scrollbar jumpier than simply rendering everything.
/// <br />
/// The list subscribes to the conversation for the messages added to and removed from it, which arrive from whatever
/// thread the stream is on, and re-renders at most once per <see cref="BitAiUiOptions.RenderInterval"/>.
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
    private bool _virtualize;
    private bool _prerendering;
    private bool _pinned = true;
    private bool _initialised;
    private bool _observing;
    private bool _observedAutoScroll;
    private int _renderedCount;



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
    /// element of its own around what it renders.
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
        _pinned = true;

        if (IsRendered && IsDisposed is false)
        {
            await _js.BitAiMessageListScrollToBottom(RootElement, smooth);
        }

        await OnPinnedChange.InvokeAsync(true);
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

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitAiMessageListParams))]
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
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (IsDisposed) return;

        var opening = _initialised is false;

        if (opening)
        {
            // The first render, or the first after switching conversations: start at the newest message without
            // animating, then follow from here.
            _initialised = true;
            _renderedCount = _messages.Count;

            await _js.BitAiMessageListScrollToBottom(RootElement, false);

            if (IsDisposed) return;
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

        if (opening) return;

        if (Options.AutoScroll is false || _pinned is false) return;
        if (_renderedCount == _messages.Count && IsGenerating is false) return;

        _renderedCount = _messages.Count;

        await _js.BitAiMessageListScrollToBottom(RootElement, false);
    }



    private bool IsGenerating => _messages.Count > 0 && _messages[^1].IsBusy;

    private bool IsLatestMessage(BitAiMessage message) => _messages.Count > 0 && ReferenceEquals(message, _messages[^1]);

    private void Rebuild()
    {
        _messages.Clear();
        _appliedHideSystemMessages = HideSystemMessages;

        foreach (var message in Conversation.Messages)
        {
            if (HideSystemMessages && message.Role.IsSystem) continue;

            _messages.Add(message);
        }

        UpdateLatest();
        UpdateVirtualization();
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
        UpdateVirtualization();
        RequestRender();
    });

    private void OnMessageRemoved(BitAiConversation conversation, BitAiMessage message) => _ = InvokeAsync(() =>
    {
        if (ReferenceEquals(conversation, _subscribed) is false) return;
        if (_messages.Remove(message) is false) return;

        _entering.Remove(message);

        UpdateLatest();
        UpdateVirtualization();
        RequestRender();
    });

    // Runs on the thread of the stream, on every token: it reads nothing of the list (mutated on the dispatcher), only
    // the newest message kept beside it, and asks for a render, which is safe from any thread.
    private void OnMessageUpdated(BitAiConversation conversation, BitAiMessage message)
    {
        // Only the last message can change the scroll height enough to matter while streaming; everything else is
        // handled by the part components re-rendering themselves.
        if (ReferenceEquals(message, Volatile.Read(ref _latest))) RequestRender();
    }

    /// <summary>
    /// Virtualization needs a measured viewport, which only exists once the component is interactive. During static
    /// rendering a bounded tail is drawn instead, so a prerendered transcript shows its most recent messages rather than
    /// an empty box.
    /// </summary>
    private void UpdateVirtualization()
    {
        _prerendering = IsStaticRender();
        _virtualize = _prerendering is false && _messages.Count >= Options.VirtualizeThreshold;
    }

    private IEnumerable<BitAiMessage> Rendered() =>
        _prerendering && _messages.Count > Options.VirtualizeThreshold
            ? _messages.Skip(_messages.Count - Options.VirtualizeThreshold)
            : _messages;

    /// <summary>
    /// Whether this render is a static one (a prerender, or a page rendered by static SSR) rather than an interactive one.
    /// </summary>
    /// <remarks>
    /// <c>RendererInfo</c> answers it from ASP.NET Core 9 on. On 8 the JS runtime does: it cannot be called during a
    /// prerender or a static render, which is what <see cref="IJSRuntimeExtensions.IsRuntimeInvalid(IJSRuntime)"/> reads.
    /// </remarks>
    private bool IsStaticRender()
    {
#if NET9_0_OR_GREATER
        return RendererInfo.IsInteractive is false;
#else
        return _js.IsRuntimeInvalid();
#endif
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
