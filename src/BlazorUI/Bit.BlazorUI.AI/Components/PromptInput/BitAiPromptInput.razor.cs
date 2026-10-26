using Microsoft.AspNetCore.Components.Forms;

namespace Bit.BlazorUI;

/// <summary>
/// BitAiPromptInput is the composer of a chat: the text, its attachments, slash commands and mentions, dictation, and the
/// send or stop button.
/// </summary>
/// <remarks>
/// Enter sends and Shift+Enter inserts a newline, which is what people expect from a chat box and the opposite of what a
/// plain textarea does (<see cref="BitAiUiOptions.SubmitOnEnter"/> turns Enter alone off, leaving Ctrl or Cmd with Enter).
/// The send button becomes a stop button while a turn is in flight rather than sitting disabled beside one, because
/// stopping is the action a user actually wants at that moment.
/// <br />
/// Files come in through the attach button, a paste or a drop, and each one is checked against
/// <see cref="Accept"/>, <see cref="MaxAttachmentSize"/> and <see cref="MaxAttachments"/>; a file that is turned away is
/// never dropped silently, but named in a notice and raised through <see cref="OnAttachmentRejected"/>.
/// </remarks>
public partial class BitAiPromptInput : BitAiComponentBase
{
    /// <summary>
    /// The default for <see cref="MaxAttachmentSize"/>: 10 MB.
    /// </summary>
    public const long DefaultMaxAttachmentSize = 10 * 1024 * 1024;

    /// <summary>
    /// The default for <see cref="MaxAttachments"/>.
    /// </summary>
    public const int DefaultMaxAttachments = 20;

    /// <summary>
    /// The default for <see cref="MaxInlineSize"/>: 256 KB.
    /// </summary>
    public const long DefaultMaxInlineSize = 256 * 1024;

    private const string SendButtonKey = "send";

    // Enough to explain a drop of several files without the notice outgrowing the composer.
    private const int MaxNotices = 3;

    private readonly List<BitAiAttachment> _attachments = [];
    private readonly Dictionary<string, StagedResources> _staged = [];
    private readonly List<BitAiAttachmentRejection> _rejections = [];
    private readonly List<MenuEntry> _menuItems = [];

    private BitAiIconClassesCache<BitButtonClassStyles> _attachButtonClasses;
    private BitAiIconClassesCache<BitButtonClassStyles> _sendButtonClasses;
    private BitAiIconClassesCache<BitButtonClassStyles> _stopButtonClasses;

    private ElementReference _textarea;
    private InputFile? _fileInput;
    private DotNetObjectReference<BitAiPromptInput>? _dotnetObj;
    private long? _registeredMaxSize;
    private bool? _registeredDisabled;
    private bool? _registeredSubmitOnEnter;
    private string? _restoredDraftKey;
    private string? _resizedValue;
    private int? _resizedRows;
    private int? _pendingCaret;
    private bool _focusText;
    private bool? _acceptedShow;
    private string? _acceptedTypes;
    private bool _dragging;
    private string? _interim;
    private int _menuIndex;
    private char _menuTrigger;
    private int _menuStart = -1;
    private int _menuEnd = -1;
    private string? _menuText;
    private int _menuVersion;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Gets or sets the cascading parameters for the prompt input component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple prompt input components
    /// through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitAiPromptInputParams.ParamName)]
    public BitAiPromptInputParams? CascadingParameters { get; set; }



    /// <summary>
    /// The file types to accept, in the syntax of the HTML <c>accept</c> attribute: extensions such as <c>.pdf</c>, MIME
    /// types such as <c>image/png</c>, or wildcards such as <c>image/*</c>.
    /// </summary>
    /// <remarks>
    /// Applied to the file picker and also checked for pasted and dropped files, which the browser does not filter. When
    /// unset, the accepted types follow the capabilities of the model if a <see cref="Model"/> or
    /// <see cref="Capabilities"/> is given, and every type is accepted otherwise. A file already staged that the accepted
    /// types no longer take - another model was picked - is taken off with the same notice as a file turned away.
    /// </remarks>
    [Parameter] public string? Accept { get; set; }

    /// <summary>
    /// Offers the attach button and accepts dropped or pasted files.
    /// <br />
    /// The default value is <strong>true</strong>.
    /// </summary>
    /// <remarks>
    /// Also subject to the model: when <see cref="Model"/> or <see cref="Capabilities"/> is set and declares neither
    /// <see cref="BitAiModelCapabilities.Vision"/>, <see cref="BitAiModelCapabilities.Documents"/> nor
    /// <see cref="BitAiModelCapabilities.Audio"/>, attachments are hidden whatever this says.
    /// </remarks>
    [Parameter] public bool AllowAttachments { get; set; } = true;

    /// <summary>
    /// Offers the dictation button, when the browser supports it.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// Hidden when <see cref="Model"/> or <see cref="Capabilities"/> is set and does not declare
    /// <see cref="BitAiModelCapabilities.Audio"/>.
    /// </remarks>
    [Parameter] public bool AllowDictation { get; set; }

    /// <summary>
    /// The capabilities to assume, when there is no <see cref="Model"/> record or its capabilities should be overridden.
    /// </summary>
    [Parameter] public BitAiModelCapabilities? Capabilities { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the prompt input.
    /// </summary>
    [Parameter, ResetClassBuilder] public BitAiPromptInputClassStyles? Classes { get; set; }

    /// <summary>
    /// The commands offered when the user types a forward slash at the start of the text.
    /// </summary>
    [Parameter] public IReadOnlyList<BitAiSlashCommand>? Commands { get; set; }

    /// <summary>
    /// The dictation language, as a BCP 47 language tag such as <c>en-US</c>. When null, the language of the browser is
    /// used.
    /// </summary>
    [Parameter] public string? DictationLanguage { get; set; }

    /// <summary>
    /// A key under which the draft text is kept in the session storage of the browser, so it survives a reload or a
    /// dropped circuit. Drafts are not persisted when it is null.
    /// </summary>
    /// <remarks>
    /// Restored when the composer first renders with an empty value, saved on every change, and cleared on submit. Use a
    /// key that identifies the conversation, so drafts do not bleed between them.
    /// </remarks>
    [Parameter] public string? DraftKey { get; set; }

    /// <summary>
    /// Whether a turn is in flight. When null, the state of the <see cref="Session"/> is used.
    /// </summary>
    [Parameter] public bool? Generating { get; set; }

    /// <summary>
    /// A hint rendered under the composer.
    /// </summary>
    [Parameter] public RenderFragment? HintTemplate { get; set; }

    /// <summary>
    /// The largest file the composer stages, in bytes.
    /// <br />
    /// The default value is <strong>10 MB</strong> (<see cref="DefaultMaxAttachmentSize"/>).
    /// </summary>
    /// <remarks>
    /// Checked in the browser before a pasted or dropped file is read, so an oversized file costs nothing to turn away.
    /// The files within the limit are streamed rather than sent in one message, so the limit is not tied to the SignalR
    /// message size of Blazor Server.
    /// </remarks>
    [Parameter] public long MaxAttachmentSize { get; set; } = DefaultMaxAttachmentSize;

    /// <summary>
    /// The most files that can be staged at once.
    /// <br />
    /// The default value is <strong>20</strong> (<see cref="DefaultMaxAttachments"/>).
    /// </summary>
    [Parameter] public int MaxAttachments { get; set; } = DefaultMaxAttachments;

    /// <summary>
    /// The largest file whose bytes are held in memory, in bytes.
    /// <br />
    /// The default value is <strong>256 KB</strong> (<see cref="DefaultMaxInlineSize"/>).
    /// </summary>
    /// <remarks>
    /// A file within this size is buffered and carries <see cref="BitAiAttachment.Data"/> and a data URL. A larger file is
    /// kept in the browser and read through <see cref="BitAiAttachment.ContentFactory"/> when something opens it, so a
    /// circuit never holds megabytes per attachment. Image thumbnails are object URLs made in the browser either way.
    /// </remarks>
    [Parameter] public long MaxInlineSize { get; set; } = DefaultMaxInlineSize;

    /// <summary>
    /// The most characters the text may hold. When null, the text is unlimited.
    /// </summary>
    /// <remarks>
    /// Enforced by the textarea and shown as a counter under the composer, which is announced politely once the limit is
    /// close.
    /// </remarks>
    [Parameter] public int? MaxLength { get; set; }

    /// <summary>
    /// The items offered when the user types an at sign.
    /// </summary>
    [Parameter] public IReadOnlyList<BitAiMentionItem>? Mentions { get; set; }

    /// <summary>
    /// The model the prompt is for. Its capabilities decide whether attachments and dictation are offered and which file
    /// types the picker accepts.
    /// </summary>
    [Parameter] public BitAiModel? Model { get; set; }

    /// <summary>
    /// Raised for each file the composer turns away: too large, too many, not an accepted type, or unreadable.
    /// </summary>
    /// <remarks>
    /// The composer already tells the user with an inline message. Handle this to log the rejection or to offer another
    /// route, such as uploading the file to the application's own storage.
    /// </remarks>
    [Parameter] public EventCallback<BitAiAttachmentRejection> OnAttachmentRejected { get; set; }

    /// <summary>
    /// Raised when the user asks to stop the current turn.
    /// </summary>
    [Parameter] public EventCallback OnStop { get; set; }

    /// <summary>
    /// Raised when the user submits. Receives the text and the attachments staged in the composer.
    /// </summary>
    /// <remarks>
    /// When it is not set, the submission goes to the <see cref="Session"/>.
    /// </remarks>
    [Parameter] public EventCallback<BitAiPromptSubmission> OnSubmit { get; set; }

    /// <summary>
    /// The placeholder of the text, which also names it for assistive technologies unless an
    /// <see cref="BitComponentBase.AriaLabel"/> is given. When null, the PromptPlaceholder of the strings is used.
    /// </summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>
    /// The content placed before the text, such as a model picker.
    /// </summary>
    [Parameter] public RenderFragment? PrefixTemplate { get; set; }

    /// <summary>
    /// The session, which supplies the generation state and receives the submissions.
    /// </summary>
    [Parameter] public BitAiChatSession? Session { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the prompt input.
    /// </summary>
    [Parameter, ResetStyleBuilder] public BitAiPromptInputClassStyles? Styles { get; set; }

    /// <summary>
    /// The content placed between the tools and the send button.
    /// </summary>
    [Parameter] public RenderFragment? SuffixTemplate { get; set; }

    /// <summary>
    /// The text of the composer.
    /// </summary>
    [Parameter] public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Raised whenever the text changes, so the value can be bound.
    /// </summary>
    [Parameter] public EventCallback<string> ValueChanged { get; set; }



    /// <summary>
    /// The attachments currently staged.
    /// </summary>
    public IReadOnlyList<BitAiAttachment> Attachments => _attachments;

    /// <summary>
    /// The capabilities in force: <see cref="Capabilities"/>, else those of <see cref="Model"/>.
    /// </summary>
    public BitAiModelCapabilities? EffectiveCapabilities => Capabilities ?? Model?.Capabilities;



    /// <summary>
    /// Puts the caret in the composer.
    /// </summary>
    /// <returns>A task that completes once the focus has been requested.</returns>
    public ValueTask FocusAsync() => _js.BitAiPromptInputFocus(_textarea);

    /// <summary>
    /// Called by the paste script of the composer when the user pastes a file into it.
    /// <br />
    /// <strong>This method is not intended to be called from application code.</strong>
    /// </summary>
    /// <param name="file">
    /// The pasted file. Its content arrives as a stream, and is withheld when the browser has already found the file too
    /// large.
    /// </param>
    /// <returns>A task that completes when the file has been staged or rejected.</returns>
    [JSInvokable("OnPastedFile")]
    public async Task _OnPastedFile(BitAiComposerFile file)
    {
        ArgumentNullException.ThrowIfNull(file);

        if (IsDisposing) return;

        // A disabled composer takes no file, and says nothing about one it was handed anyway: the script refuses the
        // paste and the drop itself, but one already on its way when the composer was disabled still arrives here.
        if (Disabled)
        {
            await ReleaseAsync(file.Content);
            await RevokePreviewAsync(file.PreviewUrl);
            return;
        }

        if (Check(file.Name, file.ContentType, file.Size) is { } reason)
        {
            await ReleaseAsync(file.Content);
            await RevokePreviewAsync(file.PreviewUrl);
            await RejectAsync(file.Name, file.ContentType, file.Size, reason);
        }
        else
        {
            try
            {
                var attachment = await file.ToAttachmentAsync(MaxAttachmentSize, MaxInlineSize);
                _attachments.Add(attachment);

                // A buffered file has released its stream already; a lazy one still needs it.
                _staged[attachment.AttachmentId] = new StagedResources(attachment.ContentFactory is not null ? file.Content : null,
                                                                       file.PreviewUrl);
            }
            catch (Exception exception) when (IsReadFailure(exception))
            {
                await ReleaseAsync(file.Content);
                await RevokePreviewAsync(file.PreviewUrl);
                await RejectAsync(file.Name, file.ContentType, file.Size, BitAiAttachmentRejectionReason.ReadFailed);
            }
        }

        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Called by the drop script of the composer when the user drops a file onto it.
    /// <br />
    /// <strong>This method is not intended to be called from application code.</strong>
    /// </summary>
    /// <param name="file">The dropped file.</param>
    /// <returns>A task that completes when the file has been staged or rejected.</returns>
    [JSInvokable("OnDroppedFile")]
    public Task _OnDroppedFile(BitAiComposerFile file) => _OnPastedFile(file);

    /// <summary>
    /// Called by the drop script of the composer as a drag of files enters or leaves it.
    /// <br />
    /// <strong>This method is not intended to be called from application code.</strong>
    /// </summary>
    /// <param name="dragging">Whether a drag of files is over the composer.</param>
    /// <returns>A task that completes when the state has been applied.</returns>
    [JSInvokable("OnDragStateChanged")]
    public Task _OnDragStateChanged(bool dragging)
    {
        // A disabled composer offers no drop, so a drag over it raises no overlay; one ending is still let through.
        if (IsDisposing || _dragging == dragging || (dragging && Disabled)) return Task.CompletedTask;

        _dragging = dragging;
        ClassBuilder.Reset();

        return InvokeAsync(StateHasChanged);
    }



    protected override string RootElementClass => "bit-aipri";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => _dragging ? "bit-aipri-dnd" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        // Disabling the composer while files are dragged over it takes the overlay down with the drop it offered.
        if (Disabled && _dragging)
        {
            _dragging = false;
            ClassBuilder.Reset();
        }

        base.OnParametersSet();
    }

    protected override async Task OnParametersSetAsync()
    {
        // A file staged for one model can be one the next cannot take: switching to a model without vision leaves the
        // images staged for the one before with nowhere to go, and submitting them anyway would hand the new model what it
        // cannot read. So when what the composer accepts moves, the files it no longer would are taken off and named in
        // the notice, as a file turned away at the door is.
        var show = ShowAttachments;
        var accept = EffectiveAccept;

        if (_acceptedShow != show || string.Equals(_acceptedTypes, accept, StringComparison.Ordinal) is false)
        {
            _acceptedShow = show;
            _acceptedTypes = accept;

            await DropUnacceptedAttachmentsAsync();
        }

        await base.OnParametersSetAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (IsDisposing) return;

        // The remove button of a staged file goes with its chip, which would leave the focus on the body of the page; the
        // text is where the user carries on from.
        if (_focusText)
        {
            _focusText = false;

            try
            {
                await _textarea.FocusAsync();
            }
            catch (JSDisconnectedException) { } // the circuit is gone, and the page with it
            catch (OperationCanceledException) { }
            catch (JSException) { } // a composer taken off the page in the meantime has nothing to focus
        }

        // Registered again whenever the limit changes, because the browser checks it before reading a file, and whenever
        // the composer is disabled or enabled: a disabled one refuses a file pasted or dropped onto it in the browser, with
        // no overlay and nothing read, rather than leaving the drop to the browser, which would open the file in place of
        // the page.
        if (ShowAttachments && (_registeredMaxSize != MaxAttachmentSize || _registeredDisabled != Disabled))
        {
            _dotnetObj ??= DotNetObjectReference.Create(this);
            _registeredMaxSize = MaxAttachmentSize;
            _registeredDisabled = Disabled;

            await _js.BitAiPromptInputRegisterPaste(UniqueId, _textarea, _dotnetObj, MaxAttachmentSize, Disabled);
            await _js.BitAiPromptInputRegisterDrop(UniqueId, RootElement, _dotnetObj, MaxAttachmentSize, Disabled);
        }
        else if (ShowAttachments is false && _registeredMaxSize is not null)
        {
            _registeredMaxSize = null;
            _registeredDisabled = null;

            await _js.BitAiPromptInputUnregisterPaste(UniqueId);
            await _js.BitAiPromptInputUnregisterDrop(UniqueId);
        }

        // The key guard is registered again when the submit key changes, not only once.
        if (_registeredSubmitOnEnter != Options.SubmitOnEnter)
        {
            _registeredSubmitOnEnter = Options.SubmitOnEnter;

            await _js.BitAiPromptInputRegisterSubmitKey(UniqueId, _textarea, Options.SubmitOnEnter);
        }

        if (string.Equals(_restoredDraftKey, DraftKey, StringComparison.Ordinal) is false)
        {
            _restoredDraftKey = DraftKey;

            if (DraftKey is not null && string.IsNullOrEmpty(Value))
            {
                var draft = await _js.BitAiPromptInputReadDraft(DraftKey);

                if (string.IsNullOrEmpty(draft) is false && IsDisposing is false)
                {
                    Value = draft;
                    await ValueChanged.InvokeAsync(Value);
                    StateHasChanged();
                }
            }
        }

        if (IsDisposing) return;

        // The height follows the text and the row cap, so it is measured again only when one of them has moved.
        if (_resizedValue != Value || _resizedRows != Options.MaxComposerRows)
        {
            _resizedValue = Value;
            _resizedRows = Options.MaxComposerRows;

            await _js.BitAiPromptInputAutoResize(_textarea, Options.MaxComposerRows);
        }

        // Set after the render that wrote the new text, which otherwise leaves the caret at its end.
        if (_pendingCaret is { } caret)
        {
            _pendingCaret = null;

            await _js.BitAiPromptInputSetCaret(_textarea, caret);
        }
    }



    private string MenuId => $"{_Id}-mnu";

    private string CounterId => $"{_Id}-ctr";

    private bool IsGenerating => Generating ?? Session?.IsGenerating ?? false;

    private bool MenuOpen => _menuItems.Count > 0;

    // The text is a combobox only where it has a menu to offer: without commands or mentions it is a plain text box, and
    // announcing a popup that never opens would only mislead.
    private bool HasMenu => Commands is { Count: > 0 } || Mentions is { Count: > 0 };

    // Whether there is anything to offer and the text holds a trigger for it at all, so ordinary typing reads no caret
    // and opens nothing.
    private bool MenuCanOpen => Value.Length > 0 && HasMenu && Value.AsSpan().IndexOfAny('/', '@') >= 0;

    private bool ShowAttachments => AllowAttachments
                                    && (EffectiveCapabilities is not { } capabilities
                                        || (capabilities & (BitAiModelCapabilities.Vision | BitAiModelCapabilities.Documents | BitAiModelCapabilities.Audio)) != 0);

    private bool ShowDictation => AllowDictation
                                  && (EffectiveCapabilities is not { } capabilities || capabilities.HasFlag(BitAiModelCapabilities.Audio));

    private string? EffectiveAccept => Accept ?? AcceptFromCapabilities();

    private bool NearLimit => MaxLength is { } max && max - Value.Length <= Math.Max(10, max / 10);

    // The counter warns as the limit gets close, and says so plainly once the text is past it.
    private string CounterClass => MaxLength is { } max && Value.Length > max
                                   ? "bit-aipri-ctr bit-aipri-ctn bit-aipri-cto"
                                   : NearLimit ? "bit-aipri-ctr bit-aipri-ctn" : "bit-aipri-ctr";

    private bool CanSubmit => Disabled is false
                              && IsGenerating is false
                              && (string.IsNullOrWhiteSpace(Value) is false || _attachments.Count > 0)
                              && (MaxLength is not { } max || Value.Length <= max);

    // Whether a plain Escape pressed in the text does something - closing the menu, or else stopping the turn in flight -
    // the one rule HandleOnKeyDown and the claim written onto the textarea both go by, so the key is claimed exactly when
    // the press is the composer's and left to the surface around it (a dialog) otherwise.
    private bool ActsOnEscape => Disabled is false && (MenuOpen || IsGenerating);

    private string? EscapeClaim => ActsOnEscape ? "claim" : null;

    private string OptionId(int index) => $"{_Id}-opt-{index}";

    // The glyphs of the buttons come from the icon font of this package, since the one of the core carries only the few
    // glyphs its own components draw (see BitAiIconClasses). Built again only when the classes of the app for a button
    // are other ones, so the button is not handed a new object, and rebuilds its classes, on every render.
    private BitButtonClassStyles _AttachButtonClasses =>
        _attachButtonClasses.Get("bit-icon-ai bit-icon-ai--Attach", Classes?.AttachButton, BitAiIconClasses.With);

    private BitButtonClassStyles _SendButtonClasses =>
        _sendButtonClasses.Get("bit-icon-ai bit-icon-ai--Send", Classes?.SendButton, BitAiIconClasses.With);

    private BitButtonClassStyles _StopButtonClasses =>
        _stopButtonClasses.Get("bit-icon-ai bit-icon-ai--StopSolid", Classes?.SendButton, BitAiIconClasses.With);



    private async Task HandleOnInput(string? value)
    {
        if (Disabled) return;

        var previous = Value;
        Value = value ?? string.Empty;

        // The menu follows the new text at once, before anything below waits: an Enter or a Tab arriving meanwhile then
        // picks from the options of what was typed, and replaces what was typed. A menu not open yet opens at once as
        // well, at the caret the edit implies, rather than only once the caret has been read back: every keystroke sends
        // a newer read and discards the older ones, so while the typing goes on none of them would open it, and a
        // "/clear" sent with Enter straight away would go out as text. UpdateMenuAsync refines it once the caret is known.
        if (MenuOpen)
        {
            RefreshStaleMenu();
        }
        else if (MenuCanOpen)
        {
            UpdateMenu(CaretAfterEdit(previous, Value));
        }

        await ValueChanged.InvokeAsync(Value);
        await SaveDraftAsync();
        await UpdateMenuAsync();
    }

    private async Task HandleOnKeyDown(KeyboardEventArgs e)
    {
        if (Disabled) return;

        // Enter during an IME composition confirms the candidate; it is never a submit or a menu choice.
        if (IsComposing(e)) return;

        RefreshStaleMenu();

        if (MenuOpen)
        {
            switch (e.Key)
            {
                case "ArrowDown":
                    _menuIndex = (_menuIndex + 1) % _menuItems.Count;
                    return;
                case "ArrowUp":
                    _menuIndex = (_menuIndex - 1 + _menuItems.Count) % _menuItems.Count;
                    return;
                // Only a plain Enter or Tab takes the option: with a modifier each keeps its own meaning - Shift+Enter
                // breaks the line, Ctrl or Cmd with Enter sends, Shift+Tab moves the focus back - as the script lets it.
                case "Enter" or "Tab" when e.ShiftKey is false && e.CtrlKey is false && e.AltKey is false && e.MetaKey is false:
                    await AcceptMenuItemAsync(_menuIndex);
                    return;
            }
        }

        if (e.Key is "Escape")
        {
            // An Escape with a modifier is the surface's around the composer, which the claim leaves it to.
            if (ActsOnEscape is false || e.IsPlainEscape() is false) return;

            // The menu closes first; with no menu open, the press stops the turn in flight.
            if (MenuOpen)
            {
                DismissMenu();
            }
            else
            {
                await StopAsync();
            }

            return;
        }

        if (e.Key is not "Enter" || e.ShiftKey || e.AltKey) return;

        var withModifier = e.CtrlKey || e.MetaKey;
        if (withModifier is false && Options.SubmitOnEnter is false) return;

        // The newline was already suppressed in the browser by the registered submit-key guard.
        await SubmitAsync();
    }

    // Moving the caret with the keyboard can carry it into or out of a slash or mention token, so the menu is
    // reconsidered once the caret has settled.
    private Task HandleOnKeyUp(KeyboardEventArgs e)
    {
        if (Disabled) return Task.CompletedTask;

        return e.Key is "ArrowLeft" or "ArrowRight" or "Home" or "End"
               ? UpdateMenuAsync()
               : Task.CompletedTask;
    }

    // The menu is driven from the text, so it closes once the focus has left it - a click elsewhere, a Tab out - rather
    // than staying open with nothing to drive it and the text still saying it is expanded. A press on an option keeps the
    // focus in the text (see the razor), so taking one with the pointer still works.
    private void HandleOnBlur()
    {
        if (MenuOpen)
        {
            DismissMenu();
        }
    }

    // The keydown of a key an IME takes is what the guard of the script reads off the browser event; .NET 8 has no
    // IsComposing, and the "Process" key the browsers report for such a keystroke is what is left to go by there.
    private static bool IsComposing(KeyboardEventArgs e) =>
#if NET9_0_OR_GREATER
        e.IsComposing ||
#endif
        e.Key is "Process";

    private Task SendOrStopAsync() => IsGenerating ? StopAsync() : SubmitAsync();

    private async Task SubmitAsync()
    {
        if (CanSubmit is false) return;

        var submission = new BitAiPromptSubmission(Value.Trim(), _attachments.ToArray());

        Value = string.Empty;
        _attachments.Clear();
        // The message owns the attachments now: their browser streams and previews go with them.
        _staged.Clear();
        _rejections.Clear();
        _interim = null;
        DismissMenu();

        await ValueChanged.InvokeAsync(Value);

        if (DraftKey is not null)
        {
            await _js.BitAiPromptInputClearDraft(DraftKey);
        }

        await OnSubmit.InvokeAsync(submission);

        if (Session is not null && OnSubmit.HasDelegate is false)
        {
            await Session.SubmitAsync(submission.Text, submission.Attachments);
        }
    }

    private async Task StopAsync()
    {
        Session?.Stop();

        await OnStop.InvokeAsync();
    }

    private async Task HandleOnTranscript(string transcript)
    {
        _interim = null;

        // The dictated text goes at the end, past whatever token the menu was opened for.
        DismissMenu();

        Value = string.IsNullOrEmpty(Value) ? transcript : $"{Value} {transcript}";
        if (MaxLength is { } max && Value.Length > max)
        {
            Value = Value[..max];
        }

        await ValueChanged.InvokeAsync(Value);
        await SaveDraftAsync();
    }

    private void HandleOnInterim(string interim)
    {
        _interim = interim;
    }

    private void HandleOnListeningChanged(bool listening)
    {
        if (listening is false)
        {
            _interim = null;
        }
    }

    private ValueTask SaveDraftAsync()
    {
        return DraftKey is null ? ValueTask.CompletedTask : _js.BitAiPromptInputWriteDraft(DraftKey, Value);
    }

    private async Task OpenFilePickerAsync()
    {
        if (Disabled) return;

        if (_fileInput?.Element is { } input)
        {
            await _js.BitAiPromptInputClick(input);
        }
    }

    private async Task HandleOnFilesSelected(InputFileChangeEventArgs args)
    {
        if (Disabled) return;

        IReadOnlyList<IBrowserFile> files;
        try
        {
            // Every selected file is considered: asking for fewer than were picked throws, and would lose the whole selection.
            files = args.GetMultipleFiles(Math.Max(1, args.FileCount));
        }
        catch (InvalidOperationException)
        {
            await RejectAsync(string.Empty, string.Empty, 0, BitAiAttachmentRejectionReason.TooMany);
            return;
        }

        var position = -1;
        foreach (var file in files)
        {
            position++;

            if (Check(file.Name, file.ContentType, file.Size) is { } reason)
            {
                await RejectAsync(file.Name, file.ContentType, file.Size, reason);
                continue;
            }

            string? preview = null;
            if (BitAiAttachment.InferKind(file.ContentType, file.Name) is BitAiAttachmentKind.Image && _fileInput?.Element is { } input)
            {
                preview = await _js.BitAiPromptInputPreviewFromInput(input, position, file.Name);
            }

            try
            {
                // InputFile streams in chunks, so this is bounded by the limit and not by the message size of the circuit.
                await StageAsync(file.Name,
                                 file.ContentType,
                                 file.Size,
                                 ct => ValueTask.FromResult(file.OpenReadStream(MaxAttachmentSize, ct)),
                                 preview,
                                 owner: null);
            }
            catch (Exception exception) when (IsReadFailure(exception))
            {
                await RevokePreviewAsync(preview);
                await RejectAsync(file.Name, file.ContentType, file.Size, BitAiAttachmentRejectionReason.ReadFailed);
            }
        }
    }

    private async Task RemoveAttachmentAsync(BitAiAttachment attachment)
    {
        _attachments.Remove(attachment);
        _focusText = true;

        if (_staged.Remove(attachment.AttachmentId, out var staged))
        {
            await ReleaseAsync(staged);
        }
    }

    // Takes off the staged files the composer would not take any more, after what it accepts has moved under them.
    private async Task DropUnacceptedAttachmentsAsync()
    {
        if (_attachments.Count == 0) return;

        foreach (var attachment in _attachments.ToArray())
        {
            var contentType = attachment.ContentType ?? string.Empty;
            if (ShowAttachments && IsAccepted(attachment.Name, contentType)) continue;

            _attachments.Remove(attachment);

            if (_staged.Remove(attachment.AttachmentId, out var staged))
            {
                await ReleaseAsync(staged);
            }

            await RejectAsync(attachment.Name, contentType, attachment.Size ?? 0, BitAiAttachmentRejectionReason.TypeNotAllowed);
        }
    }

    /// <remarks>
    /// Buffers a small file and keeps a large one in the browser, to be read when the attachment is opened.
    /// <paramref name="owner"/> is the browser stream behind <paramref name="open"/>, when there is one, so it can be
    /// released with the attachment.
    /// </remarks>
    private async Task StageAsync(string name,
                                  string contentType,
                                  long size,
                                  Func<CancellationToken, ValueTask<Stream>> open,
                                  string? previewUrl,
                                  IJSStreamReference? owner)
    {
        BitAiAttachment attachment;
        if (size <= MaxInlineSize)
        {
            await using var stream = await open(CancellationToken.None);
            using var buffer = new MemoryStream((int)Math.Clamp(size, 0, int.MaxValue));
            await stream.CopyToAsync(buffer);

            attachment = BitAiComposerFile.CreateAttachment(name, contentType, buffer.ToArray(), previewUrl);

            await ReleaseAsync(owner);
            owner = null;
        }
        else
        {
            attachment = BitAiComposerFile.CreateLazyAttachment(name, contentType, size, open, previewUrl);
        }

        _attachments.Add(attachment);
        _staged[attachment.AttachmentId] = new StagedResources(owner, previewUrl);
    }

    private BitAiAttachmentRejectionReason? Check(string name, string contentType, long size)
    {
        if (_attachments.Count >= MaxAttachments) return BitAiAttachmentRejectionReason.TooMany;
        if (IsAccepted(name, contentType) is false) return BitAiAttachmentRejectionReason.TypeNotAllowed;
        if (size > MaxAttachmentSize) return BitAiAttachmentRejectionReason.TooLarge;

        return null;
    }

    // Mirrors the reading of the accept attribute by the browser, which the picker applies but a paste or a drop does not.
    private bool IsAccepted(string name, string contentType)
    {
        var accept = EffectiveAccept;
        if (string.IsNullOrWhiteSpace(accept)) return true;

        foreach (var token in accept.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var matches = token.StartsWith('.')
                          ? name.EndsWith(token, StringComparison.OrdinalIgnoreCase)
                          : token.EndsWith("/*", StringComparison.Ordinal)
                            ? contentType.StartsWith(token[..^1], StringComparison.OrdinalIgnoreCase)
                            : string.Equals(token, contentType, StringComparison.OrdinalIgnoreCase);

            if (matches) return true;
        }

        return false;
    }

    private string? AcceptFromCapabilities()
    {
        if (EffectiveCapabilities is not { } capabilities) return null;

        var accepted = new List<string>(4);

        if (capabilities.HasFlag(BitAiModelCapabilities.Vision))
        {
            accepted.Add("image/*");
        }

        if (capabilities.HasFlag(BitAiModelCapabilities.Audio))
        {
            accepted.Add("audio/*");
        }

        if (capabilities.HasFlag(BitAiModelCapabilities.Documents))
        {
            accepted.Add("application/pdf");
            accepted.Add(".pdf");
            accepted.Add("text/*");
        }

        return accepted.Count == 0 ? null : string.Join(',', accepted);
    }

    private static bool IsReadFailure(Exception exception) => exception is IOException or JSException or JSDisconnectedException
                                                                        or InvalidOperationException or ArgumentException
                                                                        or FormatException or OperationCanceledException;

    private static async Task ReleaseAsync(IJSStreamReference? content)
    {
        if (content is null) return;

        try
        {
            await content.DisposeAsync();
        }
        catch (JSDisconnectedException) { }
        catch (JSException) { }
        catch (ObjectDisposedException) { }
        catch (OperationCanceledException) { }
    }

    private async Task ReleaseAsync(StagedResources staged)
    {
        await ReleaseAsync(staged.Content);
        await RevokePreviewAsync(staged.PreviewUrl);
    }

    // A preview the browser cannot revoke any more - its page gone, its script failing - is nothing worth failing the
    // removal or the disposal of an attachment over.
    private async Task RevokePreviewAsync(string? previewUrl)
    {
        if (string.IsNullOrEmpty(previewUrl)) return;

        try
        {
            await _js.BitAiPromptInputRevokePreview(previewUrl);
        }
        catch (JSDisconnectedException) { }
        catch (JSException) { }
        catch (OperationCanceledException) { }
    }

    private async Task RejectAsync(string name, string contentType, long size, BitAiAttachmentRejectionReason reason)
    {
        var rejection = new BitAiAttachmentRejection(name, contentType, size, reason);

        _rejections.Add(rejection);
        if (_rejections.Count > MaxNotices)
        {
            _rejections.RemoveAt(0);
        }

        await OnAttachmentRejected.InvokeAsync(rejection);
    }

    private string RejectionMessage(BitAiAttachmentRejection rejection) => rejection.Reason switch
    {
        BitAiAttachmentRejectionReason.TooLarge => Strings.AttachmentTooLarge(rejection.FileName, MaxAttachmentSize),
        BitAiAttachmentRejectionReason.TooMany => Strings.AttachmentTooMany(rejection.FileName, MaxAttachments),
        BitAiAttachmentRejectionReason.TypeNotAllowed => Strings.AttachmentTypeNotAllowed(rejection.FileName),
        _ => Strings.AttachmentReadFailed(rejection.FileName),
    };

    private void DismissRejections()
    {
        _rejections.Clear();
    }

    /// <remarks>
    /// Only the latest call applies what it finds: a keystroke arriving while the caret of an earlier one is still being
    /// read starts a call of its own, and the earlier one, answered late, would otherwise add the options of an older text
    /// to the menu and the range of an older token to replace.
    /// </remarks>
    private async Task UpdateMenuAsync()
    {
        var version = ++_menuVersion;

        if (MenuCanOpen is false)
        {
            CloseMenu();
            return;
        }

        // The caret is only asked for when a menu could open, so ordinary typing costs no round trip. Without a browser
        // to ask, the caret is taken to be at the end, where typing usually is.
        var caret = await _js.BitAiPromptInputGetCaret(_textarea) ?? Value.Length;

        if (IsDisposing || version != _menuVersion) return;

        // The option the arrow keys reached in the menu the edit opened, while the caret was being read, stays the one
        // reached when the menu worked out from the caret still offers it.
        var highlighted = MenuOpen ? _menuItems[_menuIndex] : null;

        UpdateMenu(Math.Clamp(caret, 0, Value.Length));

        if (highlighted is { } reached)
        {
            var index = _menuItems.FindIndex(e => ReferenceEquals(e.Command, reached.Command) && ReferenceEquals(e.Mention, reached.Mention));
            if (index >= 0)
            {
                _menuIndex = index;
            }
        }
    }

    // Where the caret is once an edit has turned one text into the other: right after what the edit put in, which is the
    // end of the part the two texts do not share - typing, a paste or a cut changes the text at the caret and leaves
    // what is around it alone.
    private static int CaretAfterEdit(string before, string after)
    {
        var prefix = before.AsSpan().CommonPrefixLength(after.AsSpan());
        var max = Math.Min(before.Length, after.Length) - prefix;

        var suffix = 0;
        while (suffix < max && before[before.Length - 1 - suffix] == after[after.Length - 1 - suffix])
        {
            suffix++;
        }

        return after.Length - suffix;
    }

    /// <remarks>
    /// The menu open for a text the composer no longer holds - typed into since, while the caret of the new text is still
    /// being read - is worked out again from the text it holds now, with the caret taken to have moved on with the typing,
    /// so the options and the range accepting one replaces are never those of an older text.
    /// </remarks>
    private void RefreshStaleMenu()
    {
        if (MenuOpen is false || string.Equals(_menuText, Value, StringComparison.Ordinal)) return;

        var caret = Math.Clamp(_menuEnd + Value.Length - (_menuText?.Length ?? 0), 0, Value.Length);
        var index = _menuIndex;

        UpdateMenu(caret);

        if (index < _menuItems.Count)
        {
            _menuIndex = index;
        }
    }

    private void UpdateMenu(int caret)
    {
        CloseMenu();

        var start = caret - 1;
        while (start >= 0 && char.IsWhiteSpace(Value[start]) is false && Value[start] is not ('/' or '@'))
        {
            start--;
        }

        if (start < 0) return;

        var trigger = Value[start];
        if (trigger is not ('/' or '@')) return;
        if (start > 0 && char.IsWhiteSpace(Value[start - 1]) is false) return;
        if (trigger == '/' && start != 0) return;

        var query = Value[(start + 1)..caret];
        if (query.Contains(' ')) return;

        if (trigger == '/' && Commands is { Count: > 0 } commands)
        {
            foreach (var command in commands)
            {
                if (command.Name.Contains(query, StringComparison.OrdinalIgnoreCase) is false) continue;

                _menuItems.Add(new MenuEntry($"/{command.Name}", command.Description, command.Icon, command, null));
            }
        }
        else if (trigger == '@' && Mentions is { Count: > 0 } mentions)
        {
            foreach (var mention in mentions)
            {
                if (mention.Label.Contains(query, StringComparison.OrdinalIgnoreCase) is false) continue;

                _menuItems.Add(new MenuEntry(mention.Label, mention.Description, mention.Icon, null, mention));
            }
        }

        if (_menuItems.Count == 0) return;

        _menuTrigger = trigger;
        _menuStart = start;
        _menuEnd = caret;
        _menuText = Value;
        _menuIndex = 0;
    }

    /// <remarks>
    /// Only the trigger and the query typed after it, up to the caret, are replaced. The text after the caret is kept, so
    /// a mention inserted into the middle of a sentence does not cut it short, and the caret lands right after the
    /// inserted text.
    /// </remarks>
    private async Task AcceptMenuItemAsync(int index)
    {
        if (Disabled || index < 0 || index >= _menuItems.Count) return;

        var entry = _menuItems[index];

        // An option pressed in a menu of an older text is taken from the menu of the text there is now, if it still is one.
        if (string.Equals(_menuText, Value, StringComparison.Ordinal) is false)
        {
            RefreshStaleMenu();

            index = _menuItems.FindIndex(e => ReferenceEquals(e.Command, entry.Command) && ReferenceEquals(e.Mention, entry.Mention));
            if (index < 0)
            {
                DismissMenu();
                return;
            }

            entry = _menuItems[index];
        }

        var start = _menuStart >= 0 ? Math.Min(_menuStart, Value.Length) : Value.Length;
        var end = Math.Clamp(_menuEnd, start, Value.Length);
        var prefix = Value[..start];
        var suffix = Value[end..];

        DismissMenu();

        if (entry.Command is { Handler: { } handler })
        {
            Value = prefix + suffix;

            await ValueChanged.InvokeAsync(Value);
            await SaveDraftAsync();
            await handler();

            return;
        }

        var inserted = entry.Command is { } command
                       ? command.Template ?? $"/{command.Name} "
                       : $"@{entry.Mention!.Label} ";

        // The inserted text ends in a space so typing can carry on; the one that follows it is not doubled.
        if (inserted.EndsWith(' ') && suffix.StartsWith(' '))
        {
            suffix = suffix[1..];
        }

        Value = prefix + inserted + suffix;
        _pendingCaret = prefix.Length + inserted.Length;

        await ValueChanged.InvokeAsync(Value);
        await SaveDraftAsync();
    }

    // Closes the menu for good rather than for the next caret to reopen: a caret still being read for an earlier keystroke
    // would otherwise bring back the menu that was just dismissed or accepted.
    private void DismissMenu()
    {
        _menuVersion++;
        CloseMenu();
    }

    private void CloseMenu()
    {
        _menuItems.Clear();
        _menuIndex = 0;
        _menuStart = -1;
        _menuEnd = -1;
        _menuText = null;
        _menuTrigger = '\0';
    }



    /// <remarks>
    /// The listeners of the browser, the staged files and the object reference are all released here. The files already
    /// submitted belong to their message and are left alone.
    /// </remarks>
    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        try
        {
            if (_registeredMaxSize is not null)
            {
                _registeredMaxSize = null;

                await _js.BitAiPromptInputUnregisterPaste(UniqueId);
                await _js.BitAiPromptInputUnregisterDrop(UniqueId);
            }

            if (_registeredSubmitOnEnter is not null)
            {
                _registeredSubmitOnEnter = null;

                await _js.BitAiPromptInputUnregisterSubmitKey(UniqueId);
            }
        }
        catch (JSDisconnectedException) { } // the circuit is gone, and the listeners of its page with it
        catch (ObjectDisposedException) { } // the JS runtime of a WebAssembly host may go first
        finally
        {
            foreach (var staged in _staged.Values)
            {
                await ReleaseAsync(staged);
            }

            _staged.Clear();

            _dotnetObj?.Dispose();
            _dotnetObj = null;
        }

        await base.DisposeAsync(disposing);
    }



    private sealed record MenuEntry(string Label, string? Description, string? Icon, BitAiSlashCommand? Command, BitAiMentionItem? Mention);

    /// <summary>
    /// What a staged attachment holds in the browser, to be released when it is discarded.
    /// </summary>
    private sealed record StagedResources(IJSStreamReference? Content, string? PreviewUrl);
}
